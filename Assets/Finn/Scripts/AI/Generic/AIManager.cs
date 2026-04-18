using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UIElements;


public class AIManager : MonoBehaviour
{
    [HideInInspector]
    public List<PathFinderAI> AIs = new List<PathFinderAI>();
    public int AINumber;
    public int EnemyNumber;
    public GameObject AIPrefab;
    public List<GameObject> targets;
    public List<GameObject> predefinedBoundingBox;
    public float nodeSize = 1;
    public int loopsBeforeUpdate;
    public float AISpeed;
    public float averageAITaskTime;
    [HideInInspector]
    public Task<CompleteObstacleMapReturn> currentObstacleMapTask;
    [HideInInspector]
    public List<PathFinderAI> AIsWaitingForTask = new List<PathFinderAI>();
    ObstacleManager obstacleManager;
    SelectTest selector;
    public int flowFieldSize = 50;
    [HideInInspector]
    public List<FlowFieldReturn>[,] flowFields;
    [HideInInspector]
    public CompleteObstacleMapReturn[,] flowFieldObstacleMapReturns;
    [HideInInspector]
    public List<FlowFieldReturn> destinationFlowFields = new List<FlowFieldReturn>();
    public List<Transform> AITargets;
    private Float2Bounds mapBounds;
    public float nudgeForce;
    public List<int2> simulatedSectors = new List<int2>();
    public CameraMovement camMovement;
    private CompleteObstacleMapReturn highQualityMapReturn;
    public int simulationRange = 2;
    //this is actually just a blank list for AStar flow field pathfinding to use because I didn't feel like making another pathfinding function
    private CompleteObstacleMapReturn lowQualityMapReturn;

    private int loops;

    public int loopsPerObstacleMap = 1000;


    private void Start()
    {
        selector = gameObject.GetComponent<SelectTest>();
        camMovement = FindFirstObjectByType(typeof(CameraMovement)).GetComponent<CameraMovement>();
        UnityEngine.Random.InitState(1);
        for (int i = 0; i < AINumber; i++)
        {
            PathFinderAI newAI = new PathFinderAI();
            newAI.faction = Faction.Freindly;
            newAI.obj = Instantiate(AIPrefab, new Vector2(UnityEngine.Random.Range(0, 40), UnityEngine.Random.Range(0, 40)), Quaternion.identity);
            newAI.obj.AddComponent<ObjectMono>();
            newAI.objectReference = newAI.obj.GetComponent<ObjectMono>().obj;
            newAI.obj.name = "AI " + i;
            newAI.instanceID = newAI.obj.gameObject.GetInstanceID();
            selector.selectableGameObjs.Add(newAI.obj);
            selector.selectableObjs.Add(newAI);
            AIs.Add(newAI);
        }
        for (int i = 0; i < EnemyNumber; i++)
        {
            PathFinderAI newAI = new PathFinderAI();
            newAI.faction = Faction.Enemy;
            newAI.obj = Instantiate(AIPrefab, new Vector2(UnityEngine.Random.Range(0, 40), UnityEngine.Random.Range(0, 40)), Quaternion.identity);
            newAI.obj.AddComponent<ObjectMono>();
            newAI.objectReference = newAI.obj.GetComponent<ObjectMono>().obj;
            newAI.objectReference.enemy = true;
            newAI.obj.name = "Enemy " + i;
            newAI.instanceID = newAI.obj.gameObject.GetInstanceID();
            AIs.Add(newAI);
        }

        obstacleManager = FindFirstObjectByType(typeof(ObstacleManager)).GetComponent<ObstacleManager>();
        mapBounds = DetectObstaclesInPosition.CompleteObstacleMap(obstacleManager.GetObstaclesInScene(), 1, DetectObstaclesInPosition.SetupMapTargets(predefinedBoundingBox), obstacleManager.GetObjectsInScene()).bounds;
        if (!DetectObstaclesInPosition.IsInteger(mapBounds.size.x / flowFieldSize))
        {
            mapBounds.size.x = Mathf.CeilToInt(mapBounds.size.x / flowFieldSize) * flowFieldSize;
        }
        if (!DetectObstaclesInPosition.IsInteger(mapBounds.size.y / flowFieldSize))
        {
            mapBounds.size.y = Mathf.CeilToInt(mapBounds.size.y / flowFieldSize) * flowFieldSize;
        }
        Vector2 currentMin = mapBounds.position - mapBounds.size;
        Vector2 currentMax = mapBounds.position + mapBounds.size;
        float snappedMinX = Mathf.Floor(currentMin.x / flowFieldSize) * flowFieldSize;
        float snappedMinY = Mathf.Floor(currentMin.y / flowFieldSize) * flowFieldSize;
        float snappedMaxX = Mathf.Ceil(currentMax.x / flowFieldSize) * flowFieldSize;
        float snappedMaxY = Mathf.Ceil(currentMax.y / flowFieldSize) * flowFieldSize;
        mapBounds.SetMinMax(
            new Float2(snappedMinX, snappedMinY),
            new Float2(snappedMaxX, snappedMaxY)
        );

        // initialize tile arrays based on snapped mapBounds
        int sectorsX = Mathf.CeilToInt(mapBounds.size.x * 2f / flowFieldSize);
        int sectorsY = Mathf.CeilToInt(mapBounds.size.y * 2f / flowFieldSize);

        flowFields = new List<FlowFieldReturn>[sectorsX, sectorsY];
        flowFieldObstacleMapReturns = new CompleteObstacleMapReturn[sectorsX, sectorsY];

        for (int x = 0; x < sectorsX; x++)
        {
            for (int y = 0; y < sectorsY; y++)
            {
                flowFields[x, y] = new List<FlowFieldReturn>();

                var obr = new CompleteObstacleMapReturn
                {
                    obstacleMap = new List<Obstacle>[flowFieldSize + 1, flowFieldSize + 1],
                    obstacleMapBool = new bool[flowFieldSize + 1, flowFieldSize + 1],
                    bounds = new Float2Bounds(),
                    size = new Float2(flowFieldSize, flowFieldSize),
                    startArea = findSectorOrigin(new Vector2(x, y))
                };
                for (int ix = 0; ix < flowFieldSize + 1; ix++)
                {
                    for (int iy = 0; iy < flowFieldSize + 1; iy++)
                    {
                        obr.obstacleMap[ix, iy] = new List<Obstacle>();
                        obr.obstacleMapBool[ix, iy] = false;
                    }
                }

                flowFieldObstacleMapReturns[x, y] = obr;
            }
        }

        int totalLowQualityMapReturnWidth = Mathf.CeilToInt(mapBounds.size.x * 2 / nodeSize);
        int totalLowQualityMapReturnHeight = Mathf.CeilToInt(mapBounds.size.y * 2 / nodeSize);
        lowQualityMapReturn = new CompleteObstacleMapReturn
        {
            bounds = mapBounds,
            obstacleMap = new List<Obstacle>[totalLowQualityMapReturnWidth, totalLowQualityMapReturnHeight],
            obstacleMapBool = new bool[totalLowQualityMapReturnWidth, totalLowQualityMapReturnHeight],
            startArea = mapBounds.position - mapBounds.size,
            size = mapBounds.size * 2
        };

        highQualityMapReturn = DetectObstaclesInPosition.CompleteObstacleMap(
            obstacleManager.GetObstaclesInScene(),
            1,
            DetectObstaclesInPosition.SetupMapTargets(predefinedBoundingBox),
            DetectObstaclesInPosition.SetupObjects(AIs.Select(a => a.obj).ToList())
        );

        for (int x = 0; x < lowQualityMapReturn.obstacleMap.GetLength(0); x++)
        {
            for (int y = 0; y < lowQualityMapReturn.obstacleMap.GetLength(1); y++)
            {
                lowQualityMapReturn.obstacleMap[x, y] = new List<Obstacle>();
                lowQualityMapReturn.obstacleMapBool[x, y] = false;
            }
        }
        ////Debug.Log($"Low quality Map Return size is currently: x: {lowQualityMapReturn.obstacleMap.GetLength(0)}, y: {lowQualityMapReturn.obstacleMap.GetLength(1)}");
        ////Debug.Log($"Mapbounds is at position: x: {mapBounds.position.x}, y: {mapBounds.position.y}, size (extents): x: {mapBounds.size.x}, y: {mapBounds.size.y}");

        //for (int i = 0; i < AIs.Count; i++)
        //{
        //    System.Random rnd = new System.Random();
        //    AIs[i].targetSet = true;
        //    AIs[i].targetPos = targets[rnd.Next(0, targets.Count)].transform.position;
        //    AIs[i].pathStatus = PathfindingStatus.Requested;
        //}
        //AIs[0].targetSet = true;
        //AIs[0].targetPos = targets[1].transform.position;
        //AIs[0].pathStatus = PathfindingStatus.Requested;

    }
    public Vector2 findCurrentSector(Vector2 position)
    {
        Vector2 currentSector = new Vector2();
        float rX = position.x - mapBounds.min.x;
        float rY = position.y - mapBounds.min.y;

        currentSector = new Vector2(Mathf.FloorToInt(rX / flowFieldSize), Mathf.FloorToInt(rY / flowFieldSize));

        return currentSector;
    }
    public void SendAI(PathFinderAI AI, Vector2 position)
    {
        int idx = AIs.FindIndex(x => x == AI);


        if (idx != -1)
        {
            if (AIs[idx].pathStatus != PathfindingStatus.CalculatingFlowField)
            {
                targets.Add(Instantiate(new GameObject(), position, Quaternion.identity));
                AIs[idx].targetPos = position;
                AIs[idx].targetSet = true;
                AIs[idx].enemyTarget = false;
                AIs[idx].stoppingDistance = UnityEngine.Random.Range(1f, 10f);
                AIs[idx].pathStatus = PathfindingStatus.Requested;
            }
        }

    }
    /// <summary>
    /// Batches the obstacle map so it finishes faster, but only updates new sectors
    /// </summary>
    /// <param name="newPoint"></param>
    public void ExpandObstacleMap(Vector2 newPoint)
    {
        if (mapBounds.Contains(newPoint)) return;

        Vector2 currentMin = mapBounds.min;
        Vector2 currentMax = mapBounds.max;

        Vector2 nextMin = Vector2.Min(currentMin, newPoint);
        Vector2 nextMax = Vector2.Max(currentMax, newPoint);
        float snappedMinX = Mathf.Floor(nextMin.x / flowFieldSize) * flowFieldSize;
        float snappedMinY = Mathf.Floor(nextMin.y / flowFieldSize) * flowFieldSize;
        float snappedMaxX = Mathf.Ceil(nextMax.x / flowFieldSize) * flowFieldSize;
        float snappedMaxY = Mathf.Ceil(nextMax.y / flowFieldSize) * flowFieldSize;

        int offsetX = Mathf.RoundToInt((currentMin.x - snappedMinX) / flowFieldSize);
        int offsetY = Mathf.RoundToInt((currentMin.y - snappedMinY) / flowFieldSize);

        int newSectorsX = Mathf.RoundToInt((snappedMaxX - snappedMinX) / flowFieldSize);
        int newSectorsY = Mathf.RoundToInt((snappedMaxY - snappedMinY) / flowFieldSize);

        mapBounds.SetMinMax(new Vector2(snappedMinX, snappedMinY), new Vector2(snappedMaxX, snappedMaxY));

        var newFlowFields = new List<FlowFieldReturn>[newSectorsX, newSectorsY];

        int oldSectorsX = flowFields.GetLength(0);
        int oldSectorsY = flowFields.GetLength(1);

        for (int x = 0; x < newSectorsX; x++)
        {
            for (int y = 0; y < newSectorsY; y++)
            {

                int oldX = x - offsetX;
                int oldY = y - offsetY;

                if (oldX >= 0 && oldX < oldSectorsX && oldY >= 0 && oldY < oldSectorsY)
                {
                    newFlowFields[x, y] = flowFields[oldX, oldY];
                }
                else
                {
                    newFlowFields[x, y] = new List<FlowFieldReturn>();
                }
            }
        }

        flowFields = newFlowFields;

        int totalLowQualityMapReturnWidth = Mathf.Abs(Mathf.CeilToInt(mapBounds.size.x * 2 / nodeSize));
        int totalLowQualityMapReturnHeight = Mathf.Abs(Mathf.CeilToInt(mapBounds.size.y * 2 / nodeSize));
        lowQualityMapReturn = new CompleteObstacleMapReturn
        {
            bounds = mapBounds,
            obstacleMap = new List<Obstacle>[totalLowQualityMapReturnWidth, totalLowQualityMapReturnHeight],
            obstacleMapBool = new bool[totalLowQualityMapReturnWidth, totalLowQualityMapReturnHeight],
            startArea = mapBounds.position - mapBounds.size,
            size = mapBounds.size * 2
        };

        highQualityMapReturn = DetectObstaclesInPosition.CompleteObstacleMap(
            obstacleManager.GetObstaclesInScene(),
            1,
            DetectObstaclesInPosition.SetupMapTargets(targets),
            DetectObstaclesInPosition.SetupObjects(AIs.Select(a => a.obj).ToList())
        );

        for (int x = 0; x < lowQualityMapReturn.obstacleMap.GetLength(0); x++)
        {
            for (int y = 0; y < lowQualityMapReturn.obstacleMap.GetLength(1); y++)
            {
                lowQualityMapReturn.obstacleMap[x, y] = new List<Obstacle>();
                lowQualityMapReturn.obstacleMapBool[x, y] = false;
            }
        }
        Debug.Log($"Expanded Obstacle Map {newSectorsX} X {newSectorsY} Sectors");

    }
    public async void SendMultipleAI(List<PathFinderAI> inputAIs, Vector2 pos, bool attacking)
    {
        List<int> AISectorIdxs = new List<int>(inputAIs.Count);
        List<Vector2> AISectors = new List<Vector2>();
        var dictionaryAIs = new Dictionary<PathFinderAI, int>();
        //ExpandObstacleMap(pos);
        for (int i = 0; i < AIs.Count; i++)
        {
            dictionaryAIs.Add(AIs[i], i);
        }
        for (int i = 0; i < inputAIs.Count; i++)
        {
            Vector2 sector = findCurrentSector(inputAIs[i].obj.transform.position);
            int sectorIdx = AISectors.FindIndex(x => x == sector);
            Debug.Log($"{i}/{inputAIs.Count}");
            if (sectorIdx == -1)
            {
                AISectors.Add(sector);

                AISectorIdxs.Add(AISectors.Count - 1);
            }
            else
            {
                AISectorIdxs.Add(sectorIdx);
            }
        }

        AISectorIdxs.Sort();
        targets.Add(Instantiate(new GameObject(), pos, Quaternion.identity));
        List<Task<Path>> tasks = new List<Task<Path>>(AISectors.Count);
        CompleteObstacleMapReturn[,] localFlowFieldObstacleMaps = flowFieldObstacleMapReturns;
        Debug.Log($"Preset: {localFlowFieldObstacleMaps.GetLength(0)}, {localFlowFieldObstacleMaps.GetLength(1)}");
        CompleteObstacleMapReturn localLowQualityObstacleMap = lowQualityMapReturn;
        List<FlowFieldReturn>[,] localFlowFields = flowFields;
        Float2Bounds localBounds = mapBounds;
        List<FlowFieldReturn> localDestinationFlowFields = destinationFlowFields;
        List<Obstacle> obstaclesInScene = obstacleManager.GetObstaclesInScene();
        List<MapTarget> mapTargetsInScene = DetectObstaclesInPosition.SetupMapTargets(targets);
        List<CustomObject> customObjects = DetectObstaclesInPosition.SetupObjects(AIs.Select(a => a.obj).ToList());

        for (int i = 0; i < AISectors.Count; i++)
        {
            Debug.Log(AISectors[i]);
            PathFindTSInput input = new PathFindTSInput
            {
                AIPos = AISectors[i],
                TargetPos = pos,
                obstaclesInScene = obstaclesInScene,
                mapTargetsInScene = mapTargetsInScene,
                objectsInScene = customObjects,
                flowFieldReturns = localFlowFields,
                flowFieldSize = flowFieldSize,
                mapbounds = localBounds,
                lowQualityMapReturn = localLowQualityObstacleMap,
                obstacleMapReturns = localFlowFieldObstacleMaps,
                nodeSize = nodeSize,
                destinationFlowFields = localDestinationFlowFields,
            };
            tasks.Add(Task.Run(() => Pathfind(input)));
        }
        await Task.WhenAll(tasks);
        List<Path> returns = new List<Path>(tasks.Count);
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].IsFaulted)
            {
                Debug.LogError("Task faulted, throwing exception and exiting function");
                return;
            }
            else
            {
                returns.Add(tasks[i].Result);
            }

        }
        for (int i = 0; i < AISectorIdxs.Count; i++)
        {
            PathFinderAI AIScript = inputAIs[i];
            int AIGlobalIdx = dictionaryAIs[AIScript];
            AIScript.finishedPathfindingTask = returns[AISectorIdxs[i]];
            AIScript.pathStatus = PathfindingStatus.FinishedFlowField;
            AIScript.targetPos = pos;
            AIScript.targetSet = true;
            if (attacking)
            {
                AIScript.enemyTarget = true;
                AIScript.stoppingDistance = UnityEngine.Random.Range(7f, 15f);
            }
            else
            {
                AIScript.enemyTarget = false;
            }
            AIs[AIGlobalIdx] = AIScript;
        }
    }
    public void Attack(GameObject AI, Vector2 target)
    {
        int idx = AIs.FindIndex(x => x.obj == AI);
        if (idx != -1)
        {
            AIs[idx].targetPos = target;
            AIs[idx].targetSet = true;
            AIs[idx].enemyTarget = true;
            AIs[idx].stoppingDistance = UnityEngine.Random.Range(7f, 15f);
            AIs[idx].pathStatus = PathfindingStatus.Requested;
        }
    }
    public Vector2 findSectorOrigin(Vector2 sector)
    {
        return new Vector2(
            mapBounds.min.x + sector.x * flowFieldSize,
            mapBounds.min.y + sector.y * flowFieldSize
        );
    }


    public void Update()
    {
        Vector2 camPos = camMovement.position;
        simulatedSectors.Clear();
        for (int x = 0; x < simulationRange; x++)
        {
            for (int y = 0; y < simulationRange; y++)
            {
                int2 sectorToAdd = new int2((int)findCurrentSector(camPos).x, (int)findCurrentSector(camPos).y) + new int2(x, y);

                if (sectorToAdd.x < flowFields.GetLength(0) && sectorToAdd.y < flowFields.GetLength(1))
                {
                    simulatedSectors.Add(sectorToAdd);
                }
            }
        }
        DrawRectangle(mapBounds.position, mapBounds.size, Color.green);
        // ////Debug.Log($"mapbounds size {mapBounds.size.x}, {mapBounds.size.y}");
        //if (loops > loopsPerObstacleMap)
        //{
        //    loops = 0;
        //    List<Obstacle> returnedObstacles = obstacleManager.GetObstaclesInScene();
        //    List<MapTarget> returnedTargets = DetectObstaclesInPosition.SetupMapTargets(targets);
        //    List<CustomObject> returnedObjects = DetectObstaclesInPosition.SetupObjects(AIs.Select(a => a.obj).ToList());
        //    currentObstacleMapTask = Task.Run(() => DetectObstaclesInPosition.CompleteObstacleMap(returnedObstacles, 1, returnedTargets, returnedObjects));
        //}

        if (currentObstacleMapTask != null)
        {
            if (currentObstacleMapTask.IsCompleted)
            {
                mapBounds = currentObstacleMapTask.Result.bounds;
                if (!DetectObstaclesInPosition.IsInteger(mapBounds.size.x / flowFieldSize))
                {
                    mapBounds.size.x = Mathf.CeilToInt(mapBounds.size.x / flowFieldSize) * flowFieldSize;
                }
                if (!DetectObstaclesInPosition.IsInteger(mapBounds.size.y / flowFieldSize))
                {
                    mapBounds.size.y = Mathf.CeilToInt(mapBounds.size.y / flowFieldSize) * flowFieldSize;
                }
                Vector2 currentMin = mapBounds.position - mapBounds.size;
                Vector2 currentMax = mapBounds.position + mapBounds.size;
                float snappedMinX = Mathf.Floor(currentMin.x / flowFieldSize) * flowFieldSize;
                float snappedMinY = Mathf.Floor(currentMin.y / flowFieldSize) * flowFieldSize;
                float snappedMaxX = Mathf.Ceil(currentMax.x / flowFieldSize) * flowFieldSize;
                float snappedMaxY = Mathf.Ceil(currentMax.y / flowFieldSize) * flowFieldSize;
                mapBounds.SetMinMax(
                    new Float2(snappedMinX, snappedMinY),
                    new Float2(snappedMaxX, snappedMaxY)
                );
                int totalLowQualityMapReturnWidth = Mathf.CeilToInt(mapBounds.size.x * 2 / nodeSize);
                int totalLowQualityMapReturnHeight = Mathf.CeilToInt(mapBounds.size.y * 2 / nodeSize);
                lowQualityMapReturn = new CompleteObstacleMapReturn
                {
                    bounds = mapBounds,
                    obstacleMap = new List<Obstacle>[totalLowQualityMapReturnWidth, totalLowQualityMapReturnHeight],
                    obstacleMapBool = new bool[totalLowQualityMapReturnWidth, totalLowQualityMapReturnHeight],
                    startArea = mapBounds.position - mapBounds.size,
                    size = mapBounds.size * 2
                };
                for (int x = 0; x < lowQualityMapReturn.obstacleMap.GetLength(0); x++)
                {
                    for (int y = 0; y < lowQualityMapReturn.obstacleMap.GetLength(1); y++)
                    {
                        lowQualityMapReturn.obstacleMap[x, y] = new List<Obstacle>();
                        lowQualityMapReturn.obstacleMapBool[x, y] = false;
                    }
                }
                for (int x = 0; x < flowFields.GetLength(0); x++)
                {
                    for (int y = 0; y < flowFields.GetLength(1); y++)
                    {
                        CompleteObstacleMapReturn selectedFlowFieldObstacleMapReturn = flowFieldObstacleMapReturns[x, y];
                        selectedFlowFieldObstacleMapReturn.bounds = new Float2Bounds(findSectorOrigin(new Vector2(x, y)), new Float2(flowFieldSize, flowFieldSize));
                        selectedFlowFieldObstacleMapReturn.size = new Float2(flowFieldSize, flowFieldSize);

                        int offsetX = (int)(findSectorOrigin(new Vector2(x, y)).x - mapBounds.min.x);
                        int offSetY = (int)(findSectorOrigin(new Vector2(x, y)).y - mapBounds.min.y);
                        float highQualityResolution = 1f;
                        var highStart = highQualityMapReturn.startArea;
                        Vector2 sectorOriginWorld = findSectorOrigin(new Vector2(x, y));

                        for (int fX = 0; fX < flowFieldSize + 1; fX++)
                        {
                            for (int fY = 0; fY < flowFieldSize + 1; fY++)
                            {
                                // world coordinates for this cell in the sector
                                float worldX = sectorOriginWorld.x + fX * nodeSize;
                                float worldY = sectorOriginWorld.y + fY * nodeSize;

                                int hx = Mathf.FloorToInt((worldX - highStart.x) / highQualityResolution);
                                int hy = Mathf.FloorToInt((worldY - highStart.y) / highQualityResolution);

                                hx = Mathf.Clamp(hx, 0, highQualityMapReturn.obstacleMap.GetLength(0) - 1);
                                hy = Mathf.Clamp(hy, 0, highQualityMapReturn.obstacleMap.GetLength(1) - 1);

                                selectedFlowFieldObstacleMapReturn.obstacleMap[fX, fY] = highQualityMapReturn.obstacleMap[hx, hy];
                                selectedFlowFieldObstacleMapReturn.obstacleMapBool[fX, fY] = highQualityMapReturn.obstacleMapBool[hx, hy];

                            }
                        }
                        flowFieldObstacleMapReturns[x, y] = selectedFlowFieldObstacleMapReturn;
                    }
                }
                currentObstacleMapTask = null;
                // ////Debug.Log($"Mapbounds is at position: x: {mapBounds.position.x}, y: {mapBounds.position.y}, size: x: {mapBounds.size.x}, y: {mapBounds.size.y}");
                List<Obstacle> returnedObstacles = obstacleManager.GetObstaclesInScene();
                List<MapTarget> returnedTargets = DetectObstaclesInPosition.SetupMapTargets(targets);
                List<CustomObject> returnedObjects = DetectObstaclesInPosition.SetupObjects(AIs.Select(a => a.obj).ToList());
                currentObstacleMapTask = Task.Run(() => DetectObstaclesInPosition.CompleteObstacleMap(returnedObstacles, 1, returnedTargets, returnedObjects));
                Debug.Log("starting new task");
            }
            else if (currentObstacleMapTask.IsFaulted)
            {
                ////Debug.LogError(currentObstacleMapTask.Exception.InnerException);
                ///            
                List<Obstacle> returnedObstacles = obstacleManager.GetObstaclesInScene();
                List<MapTarget> returnedTargets = DetectObstaclesInPosition.SetupMapTargets(targets);
                List<CustomObject> returnedObjects = DetectObstaclesInPosition.SetupObjects(AIs.Select(a => a.obj).ToList());
                currentObstacleMapTask = Task.Run(() => DetectObstaclesInPosition.CompleteObstacleMap(returnedObstacles, 1, returnedTargets, returnedObjects));
                Debug.Log("starting new task");
            }
        }
        for (int x = 0; x < flowFields.GetLength(0); x++)
        {
            for (int y = 0; y < flowFields.GetLength(1); y++)
            {
                for (int j = 0; j < flowFields[x, y].Count; j++)
                {
                    flowFields[x, y][j].loopsSinceUpdated++;
                    flowFields[x, y][j].loopsSinceUsed++;
                    if (flowFields[x, y][j].loopsSinceUpdated > 100000 || flowFields[x, y][j].loopsSinceUsed > 10000)
                    {
                        flowFields[x, y].RemoveAt(j);
                    }
                }
            }
        }
        for (int i = 0; i < destinationFlowFields.Count; i++)
        {
            destinationFlowFields[i].loopsSinceUpdated++;
            destinationFlowFields[i].loopsSinceUsed++;
            if (destinationFlowFields[i].loopsSinceUpdated > 100000 || destinationFlowFields[i].loopsSinceUsed > 10000)
            {
                destinationFlowFields.RemoveAt(i);
            }
        }
        NativeArray<float3> AIPositions = new NativeArray<float3>(AIs.Count, Allocator.TempJob);
        var results = new NativeArray<float2>(AIs.Count, Allocator.TempJob);
        for (int i = 0; i < AIs.Count; i++)
        {
            PathFinderAI AI = AIs[i];
            AI.currentSector = findCurrentSector(AI.obj.transform.position);
            if (AI.targetSet)
            {
                if (AI.pathStatus == PathfindingStatus.Faulted)
                {
                    ////Debug.LogError($"Alert! AI {i}'s pathfinding task has been marked as 'faulted'! find more info above this in the  //Debug log. Retrying pathfinding task");
                    AI.pathStatus = PathfindingStatus.Requested;
                }
                if (AI.pathStatus == PathfindingStatus.Requested)
                {
                    ////Debug.Log($"AI {i}: path has been requested");
                    AI.pathfindingTask = null;
                    AI.pathStatus = PathfindingStatus.CalculatingFlowField;
                    PathFindTSInput data = new PathFindTSInput
                    {
                        AIPos = AI.obj.transform.position,
                        TargetPos = AI.targetPos,
                        obstaclesInScene = obstacleManager.GetObstaclesInScene(),
                        mapTargetsInScene = DetectObstaclesInPosition.SetupMapTargets(targets),
                        objectsInScene = DetectObstaclesInPosition.SetupObjects(AIs.Select(a => a.obj).ToList()),
                        flowFieldReturns = flowFields,
                        flowFieldSize = flowFieldSize,
                        mapbounds = mapBounds,
                        lowQualityMapReturn = lowQualityMapReturn,
                        obstacleMapReturns = flowFieldObstacleMapReturns,
                        nodeSize = nodeSize,
                        destinationFlowFields = destinationFlowFields,
                        AINumber = i
                    };

                    AI.pathfindingTask = Task.Run(() => Pathfind(data));
                    ////Debug.Log($"AI {i}: flow field has begun calculating");
                }
                else if (AI.pathStatus == PathfindingStatus.CalculatingFlowField)
                {

                    if (AI.pathfindingTask.IsCompleted)
                    {
                        if (AI.pathfindingTask.Result == null)
                        {
                            AI.pathStatus = PathfindingStatus.Faulted;
                        }
                        else
                        {
                            ////Debug.Log($"AI {i}: flow field has been calculated");

                            AI.finishedPathfindingTask = AI.pathfindingTask.Result;
                            AI.finishedPathfindingTask.cachedSectorPath = AI.pathfindingTask.Result.sectorPath;
                            AI.finishedPathfindingTask.cachedSectorVectorPath = AI.pathfindingTask.Result.sectorVectorPath;
                            AI.pathfindingTask = null;
                            destinationFlowFields.Add(AI.finishedPathfindingTask.sectorPath[AI.finishedPathfindingTask.sectorPath.Count - 1]);
                            for (int j = 0; j < AI.finishedPathfindingTask.flowFieldIdxReferences.Count; j++)
                            {
                                if (flowFields[AI.finishedPathfindingTask.flowFieldIdxReferences[j].x, AI.finishedPathfindingTask.flowFieldIdxReferences[j].y][AI.finishedPathfindingTask.flowFieldIdxReferences[j].z] != null)
                                {
                                    flowFields[AI.finishedPathfindingTask.flowFieldIdxReferences[j].x, AI.finishedPathfindingTask.flowFieldIdxReferences[j].y][AI.finishedPathfindingTask.flowFieldIdxReferences[j].z].loopsSinceUsed = 0;
                                }
                            }
                            if (AI.finishedPathfindingTask.destinationFlowFieldIdxReference != -1)
                            {
                                destinationFlowFields[AI.finishedPathfindingTask.destinationFlowFieldIdxReference].loopsSinceUsed = 0;
                            }
                            for (int j = 0; j < AI.finishedPathfindingTask.sectorPath.Count - 1; j++)
                            {
                                int positionX = AI.finishedPathfindingTask.sectorPath[j].nodeX;
                                int positionY = AI.finishedPathfindingTask.sectorPath[j].nodeY;
                                int exit = 0;
                                for (int k = 0; k < flowFields[positionX, positionY].Count; k++)
                                {
                                    if (flowFields[positionX, positionY][k].dir == AI.finishedPathfindingTask.sectorPath[j].dir)
                                    {
                                        exit = 1;
                                        break;
                                    }
                                }
                                if (exit == 0)
                                {
                                    flowFields[positionX, positionY].Add(AI.finishedPathfindingTask.sectorPath[j]);
                                }
                            }
                            AI.pathStatus = PathfindingStatus.FinishedFlowField;
                        }

                    }
                    else if (AI.pathfindingTask.IsFaulted)
                    {
                        AI.pathStatus = PathfindingStatus.Faulted;
                        ////Debug.LogError(AI.pathfindingTask.Exception.InnerException);
                    }
                }
                else if (AI.pathStatus == PathfindingStatus.FinishedFlowField)
                {
                    if (AI.finishedPathfindingTask != null && AI.finishedPathfindingTask.sectorPath.Count > 0)
                    {
                        var currentActiveField = AI.finishedPathfindingTask.sectorPath[0];
                        Vector2 sectorOrigin = findSectorOrigin(new Vector2(currentActiveField.nodeX, currentActiveField.nodeY));
                        Vector2 localIdx = WorldToLocalFieldIndex(AI.obj.transform.position, sectorOrigin);

                        int lx = Mathf.FloorToInt(localIdx.x);
                        int ly = Mathf.FloorToInt(localIdx.y);
                        //Debug.Log($"AI position index {localIdx}, sector origin {sectorOrigin}");



                        Vector2 moveDir = new Vector2();
                        bool positionZero = true;
                        try
                        {
                            moveDir = currentActiveField.field[lx, ly].dir;
                            if (moveDir == Vector2.zero)
                            {
                                //Debug.Log($"AI {i} is currently in position {lx}, {ly} on its flow field with no move dir");

                            }
                        }
                        catch
                        {
                            moveDir = Vector2.zero;
                            positionZero = false;
                            //Debug.Log($"AI {i} is outside of current flow field");
                        }
                        if (moveDir != Vector2.zero)
                        {
                            AI.obj.transform.Translate(AISpeed * Time.deltaTime * moveDir);
                            if (moveDir.sqrMagnitude > 0.01f)
                            {
                                float targetAngle = Mathf.Atan2(moveDir.x, moveDir.y) * Mathf.Rad2Deg;

                                transform.rotation = Quaternion.Euler(0, 0, targetAngle);
                            }
                            ////Debug.Log("Moving in Dir " + moveDir);
                        }
                        else
                        {

                            if (Float2.ConvertToF2(findCurrentSector(AI.obj.transform.position)) != AI.finishedPathfindingTask.sectorVectorPath[0])
                            {

                                if (AI.finishedPathfindingTask.sectorPath.Count > 1)
                                {
                                    //Debug.Log($"AI {i} Initiating Nudge");
                                    Nudge(AI, i);
                                }
                                else if (positionZero)
                                {
                                    //Debug.Log($"AI {i} Initiating Nudge");
                                    Nudge(AI, i);
                                }
                                else
                                {
                                    //Debug.Log($"AI {i} Repathing");
                                    AI.pathStatus = PathfindingStatus.Requested;
                                }
                            }
                            else
                            {
                                //Debug.Log($"AI {i} Currently in sector");
                                Nudge(AI, i);
                            }
                        }
                        //if (AI.finishedPathfindingTask.sectorPath.Count > 1)
                        //{
                        //    Vector2 nextSectorPos = findSectorOrigin(new Vector2(AI.finishedPathfindingTask.sectorPath[1].nodeX, AI.finishedPathfindingTask.sectorPath[1].nodeY));
                        //    Vector2 handoffDir = (nextSectorPos - (Vector2)AI.obj.transform.position).normalized;
                        //    AI.obj.transform.Translate(handoffDir * Time.deltaTime * AISpeed);
                        //}

                        if (Vector2.Distance(AI.obj.transform.position, AI.targetPos) < AI.stoppingDistance)
                        {
                            AI.targetSet = false;
                            AI.pathStatus = PathfindingStatus.NotRequested;
                            ////Debug.Log($"AI {i} has arrived!");
                        }
                        for (int j = 0; j < AI.finishedPathfindingTask.sectorPath.Count; j++)
                        {

                            //Debug.DrawRay(AI.finishedPathfindingTask.sectorPath[j].pos, new Vector2(1, 0), Color.white);


                            //for (int x = 0; x < AI.finishedPathfindingTask.sectorPath[j].field.GetLength(0); x++)
                            //{
                            //    for (int y = 0; y < AI.finishedPathfindingTask.sectorPath[j].field.GetLength(1); y++)
                            //    {
                            //        Debug.DrawRay(AI.finishedPathfindingTask.sectorPath[j].field[x, y].position, AI.finishedPathfindingTask.sectorPath[j].field[x, y].dir, Color.green);
                            //    }
                            //}
                            //DebugNodePos = sectorOrigin + new Vector2(lx * nodeSize + nodeSize * 0.5f, ly * nodeSize + nodeSize * 0.5f);
                            //Debug.DrawLine(AI.obj.transform.position,  //DebugNodePos, Color.blue);
                        }
                        // ////Debug.Log(currentActiveField.field[lx, ly].dir + " lx: " + lx + " ly:" + ly + " sector origin: " + sectorOrigin);

                    }
                }

            }
            AIPositions[i] = AIs[i].obj.transform.position;
            ////Debug.Log($"AI {i} pos is {AIs[i].obj.transform.position}");

            AIs[i] = AI;
        }
        DetectNearbyJob detectNearby = new DetectNearbyJob()
        {
            allPositions = AIPositions,
            detectionRadiusSq = 2,
            nearbyMap = results
        };
        for (int i = 0; i < results.Length; i++) results[i] = float2.zero;
        JobHandle handle = detectNearby.Schedule(AIs.Count, 64);
        handle.Complete();

        for (int i = 0; i < AIs.Count; i++)
        {
            Vector2 push = results[i];
            if (push != Vector2.zero && !float.IsNaN(push.x))
            {
                Vector2 safeMove = Vector2.ClampMagnitude(push, AISpeed);
                AIs[i].obj.transform.Translate(safeMove * 3 * Time.deltaTime);
                //AIs[i].obj.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(safeMove.x, safeMove.y));
                ////Debug.Log($"AI {i} is applying force {safeMove}");
            }
        }
        AIPositions.Dispose();
        results.Dispose();
        loops++;
        //for(int x = 0; x < flowFields.GetLength(0); x++)
        //{
        //    for(int y = 0;y < flowFields.GetLength(1); y++)
        //    {
        //         //Debug.DrawLine()
        //    }
        //}

    }
    public void Nudge(PathFinderAI AI, int i)
    {
        if (AI.finishedPathfindingTask.sectorPath.Count > 0)
        {
            var currentActiveField = AI.finishedPathfindingTask.sectorPath[0];
            Vector2 sectorOrigin = findSectorOrigin(new Vector2(currentActiveField.nodeX, currentActiveField.nodeY));
            Vector2 localIdx = WorldToLocalFieldIndex(AI.obj.transform.position, sectorOrigin);
            Vector2 AIPos = AI.obj.transform.position;
            Float2 target = Float2.ConvertToF2(findCurrentSector(AI.obj.transform.position));
            ////Debug.Log($"X: {mapBounds.min.x}, Y: {mapBounds.min.y}");
            //Debug.Log(AI.obj.transform.position);
            //Debug.Log($"X: {target.x}, Y: {target.y}");
            int matchedFlowFieldIdx = AI.finishedPathfindingTask.sectorPath.FindIndex(x => x.nodeX == target.x && x.nodeY == target.y);
            if (matchedFlowFieldIdx != -1)
            {

                //Debug.Log($"Matched flow field idx : {matchedFlowFieldIdx}");
                //Debug.Log(AI.finishedPathfindingTask.sectorPath[matchedFlowFieldIdx].pathRef);
                int currentFieldIdx = AI.finishedPathfindingTask.sectorPath[matchedFlowFieldIdx].pathRef;
                int oldFieldIdx = currentActiveField.pathRef;
                //Debug.Log($"AI {i} bumping flow fields. New flow field {currentFieldIdx}, old flow field {oldFieldIdx}");
                for (int j = oldFieldIdx; j < currentFieldIdx; j++)
                {
                    AI.finishedPathfindingTask.sectorPath.RemoveAt(0);
                    AI.finishedPathfindingTask.sectorVectorPath.RemoveAt(0);
                }

                currentActiveField = AI.finishedPathfindingTask.sectorPath[0];

                //Debug.Log(currentActiveField.pos.x + " " + currentActiveField.pos.y);

                Vector2 newSectorCenter = new Vector2((currentActiveField.pos.x * currentActiveField.dir.x) + (flowFieldSize / 2), (currentActiveField.pos.y * currentActiveField.dir.y) + (flowFieldSize / 2));

                sectorOrigin = findSectorOrigin(new Vector2(currentActiveField.nodeX, currentActiveField.nodeY));
                localIdx = WorldToLocalFieldIndex(AI.obj.transform.position, sectorOrigin);
                //Debug.Log($"Current active field : {currentActiveField.pos.x}, {currentActiveField.pos.y}. Sector origin calculated at : {sectorOrigin}. localIdx calculated at : {localIdx}");
                if (localIdx.x > flowFieldSize || localIdx.x < 0 || localIdx.y > flowFieldSize || localIdx.y < 0)
                {

                    Vector2 nudgeDir = newSectorCenter - AIPos;
                    //Debug.Log($"Nudging AI {i} (Position {AI.obj.transform.position}) towards position {newSectorCenter} with nudge of {nudgeDir.normalized}");
                    AI.obj.transform.Translate(nudgeDir.normalized * nudgeForce * Time.deltaTime);
                }
            }
            else
            {
                matchedFlowFieldIdx = AI.finishedPathfindingTask.cachedSectorPath.FindIndex(x => x.nodeX == target.x && x.nodeY == target.y);
                if (matchedFlowFieldIdx != -1)
                {
                    string cachedSectorsDebug = AI.finishedPathfindingTask.cachedSectorPath[0].pathRef.ToString();
                    for (int j = 1; j < AI.finishedPathfindingTask.cachedSectorPath.Count; j++)
                    {
                        cachedSectorsDebug = cachedSectorsDebug + ", " + AI.finishedPathfindingTask.cachedSectorPath[j].pathRef.ToString();
                    }
                    //Debug.Log($"AI {i} Cached sectors: {cachedSectorsDebug}");
                    //Debug.Log($"Matched flow field idx : {matchedFlowFieldIdx}/{AI.finishedPathfindingTask.sectorVectorPath.Count}/{AI.finishedPathfindingTask.sectorPath.Count}");
                    //Debug.Log($"Sector cache count: {AI.finishedPathfindingTask.cachedSectorPath.Count}");
                    //Debug.Log($"Pathref new {AI.finishedPathfindingTask.cachedSectorPath[matchedFlowFieldIdx].pathRef}, pathref old {currentActiveField.pathRef}");
                    for (int j = currentActiveField.pathRef; j >= AI.finishedPathfindingTask.cachedSectorPath[matchedFlowFieldIdx].pathRef + 1; j--)
                    {
                        //Debug.Log(j);
                        AI.finishedPathfindingTask.sectorPath.Insert(0, AI.finishedPathfindingTask.cachedSectorPath[j - 1]);
                        AI.finishedPathfindingTask.sectorVectorPath.Insert(0, AI.finishedPathfindingTask.cachedSectorVectorPath[j - 1]);

                    }


                    currentActiveField = AI.finishedPathfindingTask.sectorPath[0];


                    Vector2 newSectorCenter = new Vector2(currentActiveField.pos.x + (flowFieldSize / 2), currentActiveField.pos.y + (flowFieldSize / 2));

                    sectorOrigin = findSectorOrigin(new Vector2(currentActiveField.pos.x, currentActiveField.pos.y));
                    localIdx = WorldToLocalFieldIndex(AI.obj.transform.position, sectorOrigin);
                    //Debug.Log($"Current active field : {currentActiveField.pos.x}, {currentActiveField.pos.y}. Sector origin calculated at : {sectorOrigin}. localIdx calculated at : {localIdx}");
                    if (localIdx.x > flowFieldSize || localIdx.x < 0 || localIdx.y > flowFieldSize || localIdx.y < 0)
                    {

                        Vector2 nudgeDir = newSectorCenter - AIPos;
                        //Debug.Log($"Nudging AI {i} (Position {AI.obj.transform.position}) towards position {newSectorCenter} with nudge of {nudgeDir.normalized}");
                        AI.obj.transform.Translate(nudgeDir.normalized * nudgeForce * Time.deltaTime);
                    }
                }
                else
                {
                    Vector2 SectorCenter = new Vector2(currentActiveField.pos.x + (flowFieldSize / 2), currentActiveField.pos.y + (flowFieldSize / 2));
                    Vector2 nudgeDir = SectorCenter - AIPos;
                    //Debug.Log($"Nudging AI {i} (Position {AI.obj.transform.position}) towards position {SectorCenter} with nudge of {nudgeDir.normalized}");
                    AI.obj.transform.Translate(nudgeDir.normalized * nudgeForce * Time.deltaTime);
                }

            }
            AIs[i] = AI;
        }

    }
    [BurstCompile]
    public struct DetectNearbyJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> allPositions;
        [ReadOnly] public float detectionRadiusSq;
        public NativeArray<float2> nearbyMap;

        public void Execute(int i)
        {
            float2 myPos = allPositions[i].xy;
            float2 separationVec = float2.zero;

            for (int j = 0; j < allPositions.Length; j++)
            {
                if (i == j) continue;

                float dSq = math.distancesq(myPos, allPositions[j].xy);

                if (dSq <= 4.0f && dSq > 0.001f)
                {
                    ////Debug.Log($"AI {i} found AI {j} nearby at position {myPos.x}, {myPos.y}. Other AI at position {allPositions[j].x}, {allPositions[j].y}");
                    float2 diff = myPos - allPositions[j].xy;

                    separationVec += diff / dSq;
                }
            }
            nearbyMap[i] = separationVec;
        }
    }
    public Path Pathfind(PathFindTSInput input)
    {
        Path path = new Path();
        AStar.PathRequestData requestData = new AStar.PathRequestData();
        Float2 AIPos = input.AIPos;
        requestData.AIPos = findSectorOrigin(findCurrentSector(AIPos));
        requestData.nodeSize = input.flowFieldSize;
        requestData.searchWidth = Mathf.CeilToInt(input.mapbounds.size.x * 2f);
        requestData.searchHeight = Mathf.CeilToInt(input.mapbounds.size.y * 2f);
        requestData.mapOrigin = input.mapbounds.min;
        requestData.rightAngles = true;
        requestData.obstacleMap = input.lowQualityMapReturn.obstacleMapBool;
        requestData.targetPos = findSectorOrigin(findCurrentSector(input.TargetPos));
        List<Node> AStarFlowMapPath = AStar.FindPathAStar(requestData);
        if (AStarFlowMapPath == null)
        {
            ////Debug.Log("A star pathfinding failed");
            return null;
        }
        List<FlowFieldReturn> flowFieldsPath = new List<FlowFieldReturn>();
        List<Float2> sectorVectorPath = new List<Float2>();
        List<int3> flowFieldIdxReferences = new List<int3>();
        for (int i = 0; i < AStarFlowMapPath.Count - 1; i++)
        {
            bool foundFlowField = false;
            Vector2 worldPos = AStarFlowMapPath[i].position;
            ////Debug.Log("flow map path position " + worldPos + " flow map path dir " + AStarFlowMapPath[i].dir);
            Vector2 sectorIdx = findCurrentSector(worldPos);
            int sx = (int)sectorIdx.x;
            int sy = (int)sectorIdx.y;
            sectorVectorPath.Add(new Vector2(sx, sy));
            if (sx >= 0 && sx < input.flowFieldReturns.GetLength(0) && sy >= 0 && sy < input.flowFieldReturns.GetLength(1) && input.flowFieldReturns[sx, sy].Count > 0)
            {
                List<FlowFieldReturn> selectedFlowFields = input.flowFieldReturns[sx, sy];
                for (int j = 0; j < selectedFlowFields.Count; j++)
                {
                    FlowFieldReturn selectedFlowField = selectedFlowFields[j];
                    if (selectedFlowField.dir == AStarFlowMapPath[i].dir)
                    {
                        flowFieldsPath.Add(selectedFlowField);
                        flowFieldIdxReferences.Add(new int3(sx, sy, j));
                        foundFlowField = true;
                        break;
                    }
                }

            }
            if (foundFlowField == false)
            {
                Vector2 dir = AStarFlowMapPath[i].dir;
                float halfSize = input.flowFieldSize * 0.5f;

                Vector2 sectorOrigin = findSectorOrigin(new Vector2(sx, sy));

                float nudge = input.nodeSize * 0.5f;
                Vector2 targetCenter = worldPos + (dir * (halfSize));

                Vector2 targetSize = (dir.x != 0)
                    ? new Vector2(input.nodeSize, input.flowFieldSize)
                    : new Vector2(input.flowFieldSize, input.nodeSize);

                Float2Bounds targetBounds = new Float2Bounds(new Float2(targetCenter.x, targetCenter.y), new Float2(targetSize.x, targetSize.y));

                //Debug.Log($"[AI: {input.AINumber}, Loop: {i}] Length: {input.obstacleMapReturns.GetLength(0)}, Width {input.obstacleMapReturns.GetLength(1)}, Flow Field returns: {input.flowFieldReturns.GetLength(0)}, {input.flowFieldReturns.GetLength(1)}");
                //Debug.Log($"[AI: {input.AINumber}, Loop: {i}] {sx}, {sy}");
                CompleteObstacleMapReturn ob = input.obstacleMapReturns[sx, sy];
                FlowField.FlowFieldRequestData request = new FlowField.FlowFieldRequestData
                {
                    dir = dir,
                    targetBounds = targetBounds,
                    targetPos = targetCenter,
                    mapOrigin = sectorOrigin,
                    searchWidth = input.flowFieldSize + 1,
                    searchHeight = input.flowFieldSize + 1,
                    nodeSize = input.nodeSize,
                    obstacleMap = input.obstacleMapReturns[sx, sy].obstacleMapBool,
                    nodePosInTotalMap = new Vector2(sx, sy)
                };
                FlowFieldReturn returnedFlowField = FlowField.FindFlowFieldToRect(request);
                if (returnedFlowField != null)
                {
                    returnedFlowField.worldPos = sectorIdx;
                    returnedFlowField.loopsSinceUpdated = 0;
                    returnedFlowField.pos = sectorOrigin;
                    flowFieldsPath.Add(returnedFlowField);
                    sectorVectorPath.Add(new Vector2(sx, sy));
                }
                else
                {
                    ////Debug.LogError("Flow Field pathfinding failed!");
                    return null;
                }

            }
            flowFieldsPath[i].pathRef = i;
        }
        int index = input.destinationFlowFields.FindIndex(x => x.pos == Float2.ConvertToV2(input.TargetPos));
        FlowFieldReturn destinationField = null;
        if (index != -1)
        {
            destinationField = input.destinationFlowFields[index];
        }

        if (destinationField == null)
        {
            Vector2 lastPathNodePos = AStarFlowMapPath[AStarFlowMapPath.Count - 1].position;
            Vector2 sectorIdx = findCurrentSector(lastPathNodePos);
            int sx = (int)sectorIdx.x;
            int sy = (int)sectorIdx.y;

            Vector2 sectorOrigin = findSectorOrigin(new Vector2(sx, sy));
            CompleteObstacleMapReturn ob = input.obstacleMapReturns[sx, sy];
            FlowField.FlowFieldRequestData request = new FlowField.FlowFieldRequestData
            {
                searchHeight = input.flowFieldSize + 1,
                searchWidth = input.flowFieldSize + 1,
                nodeSize = input.nodeSize,
                mapOrigin = sectorOrigin,
                obstacleMap = input.obstacleMapReturns[sx, sy].obstacleMapBool,
                targetPos = input.TargetPos,
                nodePosInTotalMap = new Vector2(sx, sy)
            };
            destinationField = FlowField.FindFlowFieldToPoint(request);
            destinationField.loopsSinceUpdated = 0;
            destinationField.pathRef = AStarFlowMapPath.Count - 1;
        }
        if (destinationField == null)
        {
            ////Debug.Log("Unable to get destination flow field");
            return null;
        }
        flowFieldsPath.Add(destinationField);
        sectorVectorPath.Add(destinationField.pos);

        path.sectorVectorPath = sectorVectorPath;
        path.sectorPath = flowFieldsPath;
        path.flowFieldIdxReferences = flowFieldIdxReferences;
        path.destinationFlowFieldIdxReference = index;
        path.cachedSectorPath = new List<FlowFieldReturn>();
        path.cachedSectorVectorPath = new List<Float2>();
        return path;


    }
    public static void DrawRectangle(Vector3 position, Vector2 extent, Color color)
    {
        // Calculate the four corner points relative to the center
        Vector3 rightOffset = Vector3.right * extent.x;
        Vector3 upOffset = Vector3.up * extent.y;

        Vector3 p1 = position + rightOffset + upOffset;  // Top-Right
        Vector3 p2 = position - rightOffset + upOffset;  // Top-Left
        Vector3 p3 = position - rightOffset - upOffset;  // Bottom-Left
        Vector3 p4 = position + rightOffset - upOffset;  // Bottom-Right

        // Draw the four lines connecting the corners
        //Debug.DrawLine(p1, p2, color); // Top
        //Debug.DrawLine(p2, p3, color); // Left
        //Debug.DrawLine(p3, p4, color); // Bottom
        //Debug.DrawLine(p4, p1, color); // Right
    }
    public bool Contains(Rect outerRect, Rect innerRect)
    {
        bool minContained = innerRect.xMin >= outerRect.xMin && innerRect.yMin >= outerRect.yMin;
        bool maxContained = innerRect.xMax <= outerRect.xMax && innerRect.yMax <= outerRect.yMax;

        return minContained && maxContained;
    }



    public static class FlowField
    {
        public class FlowFieldRequestData
        {
            public Vector2 targetPos;
            //Only used for FindFlowFieldToRect
            public Float2Bounds targetBounds;
            public int searchWidth;
            public int searchHeight;
            public Vector2 mapOrigin;
            public float nodeSize;
            public bool[,] obstacleMap;
            public Vector2 dir;
            public Vector2 nodePosInTotalMap;
            public FlowFieldReturn pathFindingTask;
        }
        public static FlowFieldReturn FindFlowFieldToPoint(FlowFieldRequestData request)
        {
            Queue<FlowNode> openNodes = new Queue<FlowNode>();
            int nodeGridWidth = Mathf.Max(1, Mathf.CeilToInt(request.searchWidth / request.nodeSize));
            int nodeGridHeight = Mathf.Max(1, Mathf.CeilToInt(request.searchHeight / request.nodeSize));
            FlowNode[,] totalNodes = new FlowNode[nodeGridWidth, nodeGridHeight];

            if (request.targetPos.x < request.mapOrigin.x - 0.1f ||
                    request.targetPos.x > request.mapOrigin.x + request.searchWidth + 0.1f ||
                    request.targetPos.y < request.mapOrigin.y - 0.1f ||
                    request.targetPos.y > request.mapOrigin.y + request.searchHeight + 0.1f)
            {
                ////Debug.LogError($"Target {request.targetPos} is outside search area. Origin: {request.mapOrigin}, Size: {request.searchWidth}");
                return null;
            }

            for (int x = 0; x < nodeGridWidth; x++)
            {
                for (int y = 0; y < nodeGridHeight; y++)
                {
                    totalNodes[x, y] = new FlowNode
                    {
                        position = new Vector2(request.mapOrigin.x + x * request.nodeSize, request.mapOrigin.y + y * request.nodeSize),
                        totalCost = float.MaxValue,
                        gridPos = new Vector2Int(x, y)
                    };
                }
            }


            int startX = Mathf.RoundToInt((request.targetPos.x - request.mapOrigin.x) / request.nodeSize);
            int startY = Mathf.RoundToInt((request.targetPos.y - request.mapOrigin.y) / request.nodeSize);
            startX = Mathf.Clamp(startX, 0, nodeGridWidth - 1);
            startY = Mathf.Clamp(startY, 0, nodeGridHeight - 1);

            FlowNode startNode = totalNodes[startX, startY];
            startNode.totalCost = 0;
            openNodes.Enqueue(startNode);

            while (openNodes.Count > 0)
            {
                FlowNode currentNode = openNodes.Dequeue();

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = (int)currentNode.gridPos.x + dx;
                        int ny = (int)currentNode.gridPos.y + dy;

                        if (nx >= 0 && nx < nodeGridWidth && ny >= 0 && ny < nodeGridHeight)
                        {
                            if (request.obstacleMap[nx, ny]) continue;

                            float moveCost = (dx != 0 && dy != 0) ? 1.414f : 1.0f;

                            Vector2 moveStep = new Vector2(dx, dy);
                            if (Vector2.Dot(moveStep.normalized, request.dir) < 0.9f)
                            {
                                moveCost += 10.0f;
                            }

                            float newCost = currentNode.totalCost + moveCost;

                            if (newCost < totalNodes[nx, ny].totalCost)
                            {
                                totalNodes[nx, ny].totalCost = newCost;
                                openNodes.Enqueue(totalNodes[nx, ny]);
                            }
                        }
                    }
                }
            }

            for (int x = 0; x < nodeGridWidth; x++)
            {
                for (int y = 0; y < nodeGridHeight; y++)
                {
                    if (totalNodes[x, y].totalCost == 0 || totalNodes[x, y].totalCost == float.MaxValue)
                    {
                        totalNodes[x, y].dir = request.dir;
                        continue;
                    }

                    float bestCost = totalNodes[x, y].totalCost;
                    Vector2 bestDir = request.dir;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < nodeGridWidth && ny >= 0 && ny < nodeGridHeight)
                            {
                                if (totalNodes[nx, ny].totalCost < bestCost - 0.01f)
                                {
                                    bestCost = totalNodes[nx, ny].totalCost;
                                    bestDir = new Vector2(dx, dy);
                                }
                            }
                        }
                    }
                    totalNodes[x, y].dir = bestDir.normalized;
                }
            }

            FlowFieldReturn fieldReturn = new FlowFieldReturn { field = totalNodes, dir = request.dir, nodeX = (int)request.nodePosInTotalMap.x, nodeY = (int)request.nodePosInTotalMap.y };
            return fieldReturn;
        }

        public static FlowFieldReturn FindFlowFieldToRect(FlowFieldRequestData request)
        {
            Queue<FlowNode> openNodes = new Queue<FlowNode>();
            int nodeGridWidth = Mathf.Max(1, Mathf.CeilToInt(request.searchWidth / request.nodeSize));
            int nodeGridHeight = Mathf.Max(1, Mathf.CeilToInt(request.searchHeight / request.nodeSize));
            FlowNode[,] totalNodes = new FlowNode[nodeGridWidth, nodeGridHeight];

            for (int x = 0; x < nodeGridWidth; x++)
            {
                for (int y = 0; y < nodeGridHeight; y++)
                {
                    totalNodes[x, y] = new FlowNode
                    {
                        position = new Vector2(request.mapOrigin.x + x * request.nodeSize, request.mapOrigin.y + y * request.nodeSize),
                        totalCost = float.MaxValue,
                        gridPos = new Vector2Int(x, y)
                    };
                }
            }

            if (request.dir == new Vector2(1, 0))
            {
                for (int y = 0; y < nodeGridHeight; y++)
                {
                    totalNodes[nodeGridWidth - 1, y].totalCost = 0;
                    openNodes.Enqueue(totalNodes[nodeGridWidth - 1, y]);
                }
            }
            else if (request.dir == new Vector2(-1, 0))
            {
                for (int y = 0; y < nodeGridHeight; y++)
                {
                    totalNodes[0, y].totalCost = 0;
                    openNodes.Enqueue(totalNodes[0, y]);
                }
            }
            else if (request.dir == new Vector2(0, 1))
            {
                for (int x = 0; x < nodeGridWidth; x++)
                {
                    totalNodes[x, nodeGridHeight - 1].totalCost = 0;
                    openNodes.Enqueue(totalNodes[x, nodeGridHeight - 1]);
                }
            }
            else if (request.dir == new Vector2(0, -1))
            {
                for (int x = 0; x < nodeGridWidth; x++)
                {
                    totalNodes[x, 0].totalCost = 0;
                    openNodes.Enqueue(totalNodes[x, 0]);
                }
            }

            while (openNodes.Count > 0)
            {
                FlowNode currentNode = openNodes.Dequeue();
                FlowNode lowestCostNeighbor = new FlowNode { totalCost = Mathf.Infinity };

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = (int)currentNode.gridPos.x + dx;
                        int ny = (int)currentNode.gridPos.y + dy;

                        if (nx >= 0 && nx < nodeGridWidth && ny >= 0 && ny < nodeGridHeight)
                        {
                            if (request.obstacleMap[nx, ny]) continue;

                            float moveCost = (dx != 0 && dy != 0) ? 1.41f : 1f; // Diagonal vs Straight
                            float newCost = currentNode.totalCost + moveCost;

                            if (newCost < totalNodes[nx, ny].totalCost)
                            {
                                totalNodes[nx, ny].totalCost = newCost;
                                openNodes.Enqueue(totalNodes[nx, ny]);
                            }
                        }
                    }
                }
            }
            for (int x = 0; x < nodeGridWidth; x++)
            {
                for (int y = 0; y < nodeGridHeight; y++)
                {
                    if (totalNodes[x, y].totalCost == 0)
                    {
                        totalNodes[x, y].dir = request.dir;
                        continue;
                    }


                    float bestCost = totalNodes[x, y].totalCost;
                    Vector2 bestDir = Vector2.zero;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < nodeGridWidth && ny >= 0 && ny < nodeGridHeight)
                            {
                                if (totalNodes[nx, ny].totalCost < bestCost)
                                {
                                    bestCost = totalNodes[nx, ny].totalCost;
                                    bestDir = new Vector2(dx, dy);
                                }
                            }
                        }
                    }
                    totalNodes[x, y].dir = bestDir.normalized;
                }
            }

            FlowFieldReturn fieldReturn = new FlowFieldReturn { field = totalNodes, dir = request.dir, nodeX = (int)request.nodePosInTotalMap.x, nodeY = (int)request.nodePosInTotalMap.y };
            return fieldReturn;
        }
    }
    public static class AStar
    {
        public class PathRequestData
        {
            public Vector2 targetPos;
            public Vector2 mapOrigin;
            public int searchWidth;
            public int searchHeight;
            public float nodeSize;
            public Vector2 AIPos;
            public bool[,] obstacleMap;
            public Task<List<Node>> pathFindingTask;
            public int AIIndex;
            public bool rightAngles;
        }
        public static List<Node> FindPathAStar(PathRequestData pathData)
        {
            // basic guards
            if (pathData.nodeSize <= 0) pathData.nodeSize = 1f;
            Node startNode;
            List<Node> OpenNodes = new List<Node>();
            Node[,] totalNodes;
            List<Node> path = new List<Node>();
            int halfWidth = pathData.searchWidth / 2;
            int halfHeight = pathData.searchHeight / 2;
            int nodeGridWidth = pathData.obstacleMap.GetLength(0);
            int nodeGridHeight = pathData.obstacleMap.GetLength(1);
            Vector2 gridWorldOrigin = pathData.mapOrigin;
            totalNodes = new Node[nodeGridWidth, nodeGridHeight];
            // ////Debug.Log($"Node width: {nodeGridWidth}, height: {halfHeight}. Pathdata width: {pathData.searchWidth}, height: {pathData.searchHeight}");
            // ensure start is in bounds (recalculate origin if needed)
            if (pathData.AIPos.x < gridWorldOrigin.x || pathData.AIPos.x > gridWorldOrigin.x + pathData.searchWidth ||
                pathData.AIPos.y < gridWorldOrigin.y || pathData.AIPos.y > gridWorldOrigin.y + pathData.searchHeight)
            {
                // ////Debug.LogError($"AI {pathData.AIIndex}'s start position {pathData.AIPos} is outside search area origin {gridWorldOrigin} size ({pathData.searchWidth},{pathData.searchHeight}). Expand search area or recenter.");
                return null;
            }
            for (int x = 0; x < nodeGridWidth; x++)
            {
                for (int y = 0; y < nodeGridHeight; y++)
                {
                    Node node = new Node
                    {
                        position = new Vector2(gridWorldOrigin.x + x * pathData.nodeSize, gridWorldOrigin.y + y * pathData.nodeSize),
                        startCost = Mathf.Infinity,
                        targetDistance = Mathf.Infinity,
                        totalCost = Mathf.Infinity,
                        parent = null,
                        evaluated = false
                    };

                    totalNodes[x, y] = node;
                }
            }
            int startX = Mathf.Clamp(Mathf.RoundToInt((pathData.AIPos.x - gridWorldOrigin.x) / pathData.nodeSize), 0, nodeGridWidth - 1);
            int startY = Mathf.Clamp(Mathf.RoundToInt((pathData.AIPos.y - gridWorldOrigin.y) / pathData.nodeSize), 0, nodeGridHeight - 1);
            startNode = totalNodes[startX, startY];
            startNode.position = pathData.AIPos;
            startNode.startCost = 0;
            startNode.targetDistance = Vector2.Distance(startNode.position, pathData.targetPos);
            startNode.totalCost = startNode.targetDistance;
            OpenNodes.Add(startNode);
            Node targetNode = null;

            // prepare obstacle mask (use "Obstacles" layer if exists, otherwise use all layers)
            //int obstacleMask = LayerMask.GetMask("Obstacles");
            //if (obstacleMask == 0) obstacleMask = ~0;

            while (OpenNodes.Count > 0)
            {
                Node currentNode = OpenNodes.Aggregate((prev, current) => current.totalCost < prev.totalCost ? current : prev);
                if (Vector2.Distance(currentNode.position, pathData.targetPos) < pathData.nodeSize * 0.9f)
                {
                    targetNode = currentNode;
                    break;
                }

                int currentNodeGridX = Mathf.Clamp(Mathf.RoundToInt((currentNode.position.x - gridWorldOrigin.x) / pathData.nodeSize), 0, nodeGridWidth - 1);
                int currentNodeGridY = Mathf.Clamp(Mathf.RoundToInt((currentNode.position.y - gridWorldOrigin.y) / pathData.nodeSize), 0, nodeGridHeight - 1);

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        int gridX = currentNodeGridX + dx;
                        int gridY = currentNodeGridY + dy;

                        if (gridX < 0 || gridX >= nodeGridWidth || gridY < 0 || gridY >= nodeGridHeight)
                        {
                            continue;
                        }
                        if (pathData.rightAngles)
                        {
                            if (dx == -1 && dy == -1)
                            {
                                continue;
                            }
                            else if (dx == -1 && dy == 1)
                            {
                                continue;
                            }
                            else if (dx == 1 && dy == 1)
                            {
                                continue;
                            }
                            else if (dx == 1 && dy == -1)
                            {
                                continue;
                            }
                        }


                        Node neighboringNode = totalNodes[gridX, gridY];
                        Vector2 neighborPos = neighboringNode.position;
                        bool hitObj = false;
                        hitObj = pathData.obstacleMap[gridX, gridY];
                        if (hitObj == true)
                        {
                            neighboringNode.startCost = Mathf.Infinity;
                            neighboringNode.targetDistance = Mathf.Infinity;
                            neighboringNode.totalCost = Mathf.Infinity;
                            totalNodes[gridX, gridY] = neighboringNode;
                            continue;
                        }


                        float newStartCostScore = currentNode.startCost + Vector2.Distance(currentNode.position, neighboringNode.position);
                        neighboringNode.targetDistance = Vector2.Distance(neighboringNode.position, pathData.targetPos);
                        if (newStartCostScore < neighboringNode.startCost)
                        {
                            currentNode.child = neighboringNode;
                            neighboringNode.parent = currentNode;
                            neighboringNode.startCost = newStartCostScore;
                            neighboringNode.totalCost = neighboringNode.startCost + neighboringNode.targetDistance;
                            if (!OpenNodes.Contains(neighboringNode))
                            {
                                OpenNodes.Add(neighboringNode);
                            }
                        }
                        totalNodes[gridX, gridY] = neighboringNode;
                    }
                }

                OpenNodes.Remove(currentNode);
                currentNode.evaluated = true;
            }

            if (targetNode == null)
            {
                ////Debug.LogWarning("No path found to the target. Possible causes: search area too small, obstacles block the route, or nodeSize is too large.");
                return null;
            }

            Node pathNode = targetNode;
            while (pathNode != null)
            {
                path.Add(pathNode);
                pathNode = pathNode.parent;
            }
            path.Reverse();

            for (int j = 0; j < path.Count - 1; j++)
            {
                path[j].dir = (path[j + 1].position - path[j].position).normalized;
            }
            path[path.Count - 1].dir = Vector2.zero;
            return path;
        }
    }

    private Vector2 WorldToSectorIndex(Vector2 worldPos)
    {
        float rx = worldPos.x - mapBounds.min.x;
        float ry = worldPos.y - mapBounds.min.y;
        int sx = Mathf.FloorToInt(rx / flowFieldSize);
        int sy = Mathf.FloorToInt(ry / flowFieldSize);
        return new Vector2(sx, sy);
    }

    private Vector2 WorldToLocalFieldIndex(Vector2 worldPos, Vector2 sectorOrigin)
    {
        float localX = worldPos.x - sectorOrigin.x;
        float localY = worldPos.y - sectorOrigin.y;
        return new Vector2(Mathf.FloorToInt(localX), Mathf.FloorToInt(localY));
    }
}
