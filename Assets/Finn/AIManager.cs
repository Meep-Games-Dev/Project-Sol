using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using static UnityEngine.InputManagerEntry;


public class AIManager : MonoBehaviour
{
    [HideInInspector]
    public List<PathFinderAI> AIs = new List<PathFinderAI>();
    public int AINumber;
    public GameObject AIPrefab;
    public List<GameObject> targets;
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


    private CompleteObstacleMapReturn highQualityMapReturn;

    //this is actually just a blank list for AStar flow field pathfinding to use because I didn't feel like making another pathfinding function
    private CompleteObstacleMapReturn lowQualityMapReturn;

    private int loops;

    public int loopsPerObstacleMap = 1000;


    private void Start()
    {
        selector = gameObject.GetComponent<SelectTest>();
        UnityEngine.Random.InitState(1);
        for (int i = 0; i < AINumber; i++)
        {
            PathFinderAI newAI = new PathFinderAI();

            newAI.obj = Instantiate(AIPrefab, new Vector2(UnityEngine.Random.Range(0, 40), UnityEngine.Random.Range(0, 40)), Quaternion.identity);
            //No need for obstacle on AI anymore, we can just use vector pushing
            //newAI.obj.gameObject.AddComponent<ObstacleObj>();
            newAI.obj.AddComponent<ObjectMono>();
            newAI.obj.name = "AI " + i;
            newAI.instanceID = newAI.obj.gameObject.GetInstanceID();
            selector.selectableGameObjs.Add(newAI.obj);
            selector.selectableObjs.Add(newAI);
            System.Random rnd = new System.Random();
            newAI.targetPos = targets[rnd.Next(0, targets.Count)].gameObject.transform.position;
            newAI.pathStatus = PathfindingStatus.Requested;
            newAI.targetSet = true;
            //newAI.rb = newAI.obj.GetComponent<Rigidbody2D>();
            AIs.Add(newAI);
        }
        obstacleManager = FindFirstObjectByType(typeof(ObstacleManager)).GetComponent<ObstacleManager>();
        mapBounds = DetectObstaclesInPosition.CompleteObstacleMap(obstacleManager.GetObstaclesInScene(), 1, DetectObstaclesInPosition.SetupMapTargets(targets), obstacleManager.GetObjectsInScene()).bounds;
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
        //Debug.Log($"Low quality Map Return size is currently: x: {lowQualityMapReturn.obstacleMap.GetLength(0)}, y: {lowQualityMapReturn.obstacleMap.GetLength(1)}");
        //Debug.Log($"Mapbounds is at position: x: {mapBounds.position.x}, y: {mapBounds.position.y}, size (extents): x: {mapBounds.size.x}, y: {mapBounds.size.y}");



    }
    public static Vector2 StaticFindCurrentSector(Vector2 position, Float2Bounds mapBounds, int flowFieldSize)
    {
        Vector2 currentSector = new Vector2();
        float rX = position.x - mapBounds.min.x;
        float rY = position.y - mapBounds.min.y;

        currentSector = new Vector2(Mathf.FloorToInt(rX / flowFieldSize), Mathf.FloorToInt(rY / flowFieldSize));

        return currentSector;
    }
    public Vector2 findCurrentSector(Vector2 position)
    {
        Vector2 currentSector = new Vector2();
        float rX = position.x - mapBounds.min.x;
        float rY = position.y - mapBounds.min.y;

        currentSector = new Vector2(Mathf.FloorToInt(rX / flowFieldSize), Mathf.FloorToInt(rY / flowFieldSize));

        return currentSector;
    }
    public static Vector2 StaticFindSectorOrigin(Vector2 sector, Float2Bounds mapBounds, int flowFieldSize)
    {
        return new Vector2(
            mapBounds.min.x + sector.x * flowFieldSize,
            mapBounds.min.y + sector.y * flowFieldSize
        );
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
        DrawRectangle(mapBounds.position, mapBounds.size, Color.green);
        // //Debug.Log($"mapbounds size {mapBounds.size.x}, {mapBounds.size.y}");
        if (loops > loopsPerObstacleMap)
        {
            loops = 0;
            List<Obstacle> returnedObstacles = obstacleManager.GetObstaclesInScene();
            List<MapTarget> returnedTargets = DetectObstaclesInPosition.SetupMapTargets(targets);
            List<CustomObject> returnedObjects = DetectObstaclesInPosition.SetupObjects(AIs.Select(a => a.obj).ToList());
            currentObstacleMapTask = Task.Run(() => DetectObstaclesInPosition.CompleteObstacleMap(returnedObstacles, 1, returnedTargets, returnedObjects));
        }

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
                // //Debug.Log($"Mapbounds is at position: x: {mapBounds.position.x}, y: {mapBounds.position.y}, size: x: {mapBounds.size.x}, y: {mapBounds.size.y}");
            }
            else if (currentObstacleMapTask.IsFaulted)
            {
                //Debug.LogError(currentObstacleMapTask.Exception.InnerException);
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
        NativeList<PathFindTSInput> inputs = new NativeList<PathFindTSInput>(Allocator.Temp);
        NativeArray<float3> AIPositions = new NativeArray<float3>(AIs.Count, Allocator.TempJob);
        List<int> AIsQueuing = new List<int>();
        var results = new NativeArray<float2>(AIs.Count, Allocator.TempJob);
        for (int i = 0; i < AIs.Count; i++)
        {
            PathFinderAI AI = AIs[i];
            AI.currentSector = findCurrentSector(AI.obj.transform.position);
            AI.targetSector = findCurrentSector(AI.targetPos);
            if (AI.targetSet)
            {
                if (AI.pathStatus == PathfindingStatus.Faulted)
                {
                    //Debug.LogError($"Alert! AI {i}'s pathfinding task has been marked as 'faulted'! find more info above this in the  //Debug log. Retrying pathfinding task");
                    AI.pathStatus = PathfindingStatus.Requested;
                }
                if (AI.pathStatus == PathfindingStatus.Requested)
                {
                    //Debug.Log($"AI {i}: path has been requested");
                    AI.pathfindingTask = null;
                    AI.pathStatus = PathfindingStatus.CalculatingFlowField;
                    int count = 0;
                    NativeArray<CustomObject> nativeListObj = new NativeArray<CustomObject>(DetectObstaclesInPosition.SetupObjects(AIs.Select(a => a.obj).ToList()).Count, Allocator.TempJob);


                    foreach (var item in DetectObstaclesInPosition.SetupObjects(AIs.Select(a => a.obj).ToList()))
                    {
                        nativeListObj[count] = item;
                    }
                    NativeArray<MapTarget> nativeListMap = new NativeArray<MapTarget>(DetectObstaclesInPosition.SetupMapTargets(targets).Count, Allocator.TempJob);

                    count = 0;
                    foreach (var item in DetectObstaclesInPosition.SetupMapTargets(targets).ToList())
                    {
                        nativeListMap[count] = item;
                    }
                    List<Obstacle> obstacles = obstacleManager.GetObstaclesInScene();
                    NativeArray<Obstacle> nativeListObstacles = new NativeArray<Obstacle>(obstacles.Count, Allocator.TempJob);

                    count = 0;
                    foreach (var item in obstacles)
                    {
                        nativeListObstacles[count] = item;
                    }
                    NativeArray<FlowFieldReturnStruct> nativeListDestinations = new NativeArray<FlowFieldReturnStruct>(destinationFlowFields.Count, Allocator.TempJob);

                    count = 0;
                    foreach (var item in destinationFlowFields)
                    {

                        nativeListDestinations[count] = FlowFieldUtils.FlowFieldReturnToStruct(item);
                        count++;
                    }

                    PathFindTSInput data = new PathFindTSInput
                    {
                        AIPos = AI.obj.transform.position,
                        TargetPos = AI.targetPos,
                        obstaclesInScene = nativeListObstacles,
                        mapTargetsInScene = nativeListMap,
                        objectsInScene = nativeListObj,
                        flowFieldReturns = FlowFieldUtils.Multiple2DFlowFieldReturnToStruct(flowFields),
                        flowFieldSize = flowFieldSize,
                        mapbounds = mapBounds,
                        lowQualityMapReturn = ObstacleMapUtils.ObstacleMapReturnToStruct(lowQualityMapReturn),
                        obstacleMapReturns = ObstacleMapUtils.MultipleObstaclesToStruct(flowFieldObstacleMapReturns),
                        nodeSize = nodeSize,
                        destinationFlowFields = FlowFieldUtils.FlattenMultipleFlowFieldReturns(nativeListDestinations),
                    };
                    inputs.Add(data);
                    AIsQueuing.Add(i);
                    //AI.pathfindingTask = Task.Run(() => Pathfind(data));
                    //Debug.Log($"AI {i}: flow field has begun calculating");
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
                            //Debug.Log($"AI {i}: flow field has been calculated");
                            AI.pathStatus = PathfindingStatus.FinishedFlowField;
                            AI.finishedPathfindingTask = AI.pathfindingTask.Result;
                            AI.finishedPathfindingTask.cachedSectorPath = AI.pathfindingTask.Result.sectorPath;
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
                        }

                    }
                    else if (AI.pathfindingTask.IsFaulted)
                    {
                        AI.pathStatus = PathfindingStatus.Faulted;
                        //Debug.LogError(AI.pathfindingTask.Exception.InnerException);
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
                        Debug.Log($"AI position index {localIdx}, sector origin {sectorOrigin}");



                        Vector2 moveDir = new Vector2();
                        try
                        {
                            moveDir = currentActiveField.field[lx, ly].dir;
                            if (moveDir == Vector2.zero)
                            {
                                Debug.Log($"AI {i} is currently in position {lx}, {ly} on its flow field with no move dir");

                            }
                        }
                        catch
                        {
                            moveDir = Vector2.zero;
                            Debug.Log($"AI {i} is outside of current flow field");
                        }
                        if (moveDir != Vector2.zero)
                        {
                            AI.obj.transform.Translate(AISpeed * Time.deltaTime * moveDir);
                            //Debug.Log("Moving in Dir " + moveDir);
                        }
                        else
                        {

                            if (findCurrentSector(AI.obj.transform.position) != AI.finishedPathfindingTask.sectorPath[0].pos)
                            {

                                if (AI.finishedPathfindingTask.sectorPath.Count > 1)
                                {
                                    Debug.Log($"AI {i} Initiating Nudge");
                                    Nudge(AI, i);
                                }
                                else
                                {
                                    Debug.Log($"AI {i} Repathing");
                                    AI.pathStatus = PathfindingStatus.Requested;
                                }
                            }
                            else
                            {
                                Debug.Log($"AI {i} Currently in sector");
                                Nudge(AI, i);
                            }
                        }
                        //if (AI.finishedPathfindingTask.sectorPath.Count > 1)
                        //{
                        //    Vector2 nextSectorPos = findSectorOrigin(new Vector2(AI.finishedPathfindingTask.sectorPath[1].nodeX, AI.finishedPathfindingTask.sectorPath[1].nodeY));
                        //    Vector2 handoffDir = (nextSectorPos - (Vector2)AI.obj.transform.position).normalized;
                        //    AI.obj.transform.Translate(handoffDir * Time.deltaTime * AISpeed);
                        //}

                        if (Vector2.Distance(AI.obj.transform.position, AI.targetPos) < 1f)
                        {
                            AI.targetSet = false;
                            AI.pathStatus = PathfindingStatus.NotRequested;
                            //Debug.Log($"AI {i} has arrived!");
                        }
                        //for (int j = 0; j < AI.finishedPathfindingTask.sectorPath.Count; j++)
                        //{

                        //    //Debug.DrawRay(AI.finishedPathfindingTask.sectorPath[j].pos, new Vector2(1, 0), Color.white);


                        //    for (int x = 0; x < AI.finishedPathfindingTask.sectorPath[j].field.GetLength(0); x++)
                        //    {
                        //        for (int y = 0; y < AI.finishedPathfindingTask.sectorPath[j].field.GetLength(1); y++)
                        //        {
                        //            Debug.DrawRay(AI.finishedPathfindingTask.sectorPath[j].field[x, y].position, AI.finishedPathfindingTask.sectorPath[j].field[x, y].dir, Color.green);
                        //        }
                        //    }
                        //    //DebugNodePos = sectorOrigin + new Vector2(lx * nodeSize + nodeSize * 0.5f, ly * nodeSize + nodeSize * 0.5f);
                        //    //Debug.DrawLine(AI.obj.transform.position,  //DebugNodePos, Color.blue);
                        //}
                        // //Debug.Log(currentActiveField.field[lx, ly].dir + " lx: " + lx + " ly:" + ly + " sector origin: " + sectorOrigin);

                    }
                }

            }
            AIPositions[i] = AIs[i].obj.transform.position;
            //Debug.Log($"AI {i} pos is {AIs[i].obj.transform.position}");
            //string debugString = "";
            //for (int j = 0; j < AI.finishedPathfindingTask.sectorVectorPath.Count; j++)
            //{
            //    debugString = debugString + " " + Float2.ConvertToV2(AI.finishedPathfindingTask.sectorVectorPath[j]);
            //}
            //Debug.Log(debugString);
            //debugString = "";
            //for (int j = 0; j < AI.finishedPathfindingTask.sectorPath.Count; j++)
            //{
            //    debugString = debugString + " " + AI.finishedPathfindingTask.sectorPath[j].nodeX + ", " + AI.finishedPathfindingTask.sectorPath[j].nodeY;
            //}
            //Debug.Log(debugString);
            //Debug.Log(AI.finishedPathfindingTask.sectorVectorPath.Count + " " + AI.finishedPathfindingTask.sectorPath.Count);
            //Debug.Log($"Frame Info: AI pos: {AI.obj.transform.position}, Flow Field IDX {AI.finishedPathfindingTask.sectorPath[0].pathRef}, Flow Field Pos: {AI.finishedPathfindingTask.sectorPath[0].pos}, Current Sector {findCurrentSector(AI.obj.transform.position)}");
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
                //Debug.Log($"AI {i} is applying force {safeMove}");
            }
        }
        AIPositions.Dispose();
        results.Dispose();

        NativeArray<NativePath> pathreturns = new NativeArray<NativePath>(inputs.Length, Allocator.Temp);
        CompletePathfind completePathfindingJob = new CompletePathfind()
        {
            inputs = inputs,
            pathReturns = pathreturns,
        };
        JobHandle handle2 = completePathfindingJob.Schedule(inputs.Length, 64);
        handle2.Complete();
        for (int i = 0; i < pathreturns.Length; i++)
        {
            AIs[AIsQueuing[i]].pathStatus = PathfindingStatus.FinishedFlowField;
            AIs[AIsQueuing[i]].finishedPathfindingTask = pathreturns[i];
        }
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
        var currentActiveField = AI.finishedPathfindingTask.sectorPath[0];
        Vector2 sectorOrigin = findSectorOrigin(new Vector2(currentActiveField.nodeX, currentActiveField.nodeY));
        Vector2 localIdx = WorldToLocalFieldIndex(AI.obj.transform.position, sectorOrigin);
        Vector2 AIPos = AI.obj.transform.position;
        Float2 target = Float2.ConvertToF2(findCurrentSector(AI.obj.transform.position));
        //Debug.Log($"X: {mapBounds.min.x}, Y: {mapBounds.min.y}");
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
            }

            currentActiveField = AI.finishedPathfindingTask.sectorPath[0];

            //Debug.Log(currentActiveField.pos.x + " " + currentActiveField.pos.y);

            Vector2 newSectorCenter = new Vector2((currentActiveField.pos.x * currentActiveField.dir.x) + (flowFieldSize / 2), (currentActiveField.pos.y * currentActiveField.dir.y) + (flowFieldSize / 2));

            sectorOrigin = findSectorOrigin(new Vector2(currentActiveField.nodeX, currentActiveField.nodeY));
            localIdx = WorldToLocalFieldIndex(AI.obj.transform.position, sectorOrigin);
            if (localIdx.x > flowFieldSize || localIdx.x < 0 || localIdx.y > flowFieldSize || localIdx.y < 0)
            {

                Vector2 nudgeDir = newSectorCenter - AIPos;
                AI.obj.transform.Translate(nudgeDir.normalized * nudgeForce * Time.deltaTime);
            }
        }
        else
        {
            matchedFlowFieldIdx = AI.finishedPathfindingTask.cachedSectorPath.FindIndex(x => x.nodeX == target.x && x.nodeY == target.y);
            if (matchedFlowFieldIdx != -1)
            {
                //string cachedSectorsDebug = AI.finishedPathfindingTask.cachedSectorPath[0].pathRef.ToString();
                //for (int j = 1; j < AI.finishedPathfindingTask.cachedSectorPath.Count; j++)
                //{
                //    cachedSectorsDebug = cachedSectorsDebug + ", " + AI.finishedPathfindingTask.cachedSectorPath[j].pathRef.ToString();
                //}
                //Debug.Log($"AI {i} Cached sectors: {cachedSectorsDebug}");
                //Debug.Log($"Matched flow field idx : {matchedFlowFieldIdx}/{AI.finishedPathfindingTask.sectorVectorPath.Count}/{AI.finishedPathfindingTask.sectorPath.Count}");
                //Debug.Log($"Sector cache count: {AI.finishedPathfindingTask.cachedSectorPath.Count}");
                //Debug.Log($"Pathref new {AI.finishedPathfindingTask.cachedSectorPath[matchedFlowFieldIdx].pathRef}, pathref old {currentActiveField.pathRef}");
                for (int j = currentActiveField.pathRef; j > AI.finishedPathfindingTask.cachedSectorPath[matchedFlowFieldIdx].pathRef + 1; j--)
                {
                    AI.finishedPathfindingTask.sectorPath.Insert(0, AI.finishedPathfindingTask.cachedSectorPath[j - 1]);

                }


                currentActiveField = AI.finishedPathfindingTask.sectorPath[0];


                Vector2 newSectorCenter = new Vector2(currentActiveField.pos.x + (flowFieldSize / 2), currentActiveField.pos.y + (flowFieldSize / 2));

                sectorOrigin = findSectorOrigin(new Vector2(currentActiveField.nodeX, currentActiveField.nodeY));
                localIdx = WorldToLocalFieldIndex(AI.obj.transform.position, sectorOrigin);
                if (localIdx.x > flowFieldSize || localIdx.x < 0 || localIdx.y > flowFieldSize || localIdx.y < 0)
                {

                    Vector2 nudgeDir = newSectorCenter - AIPos;
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
                    //Debug.Log($"AI {i} found AI {j} nearby at position {myPos.x}, {myPos.y}. Other AI at position {allPositions[j].x}, {allPositions[j].y}");
                    float2 diff = myPos - allPositions[j].xy;

                    separationVec += diff / dSq;
                }
            }
            nearbyMap[i] = separationVec;
        }
    }
    [BurstCompile]
    public struct CompletePathfind : IJobParallelFor
    {
        [ReadOnly] public NativeArray<PathFindTSInput> inputs;
        public NativeArray<NativePath> pathReturns;

        public void Execute(int a)
        {
            PathFindTSInput input = inputs[a];

            int2 startSector = StaticFindCurrentSector(input.AIPos, input.mapbounds, input.flowFieldSize);
            int2 targetSector = StaticFindCurrentSector(input.TargetPos, input.mapbounds, input.flowFieldSize);

            AStar.PathRequestData requestData = new AStar.PathRequestData
            {
                AIPos = StaticFindSectorOrigin(startSector, input.mapbounds, input.flowFieldSize),
                nodeSize = input.flowFieldSize,
                searchWidth = Mathf.CeilToInt(input.mapbounds.size.x * 2f),
                searchHeight = Mathf.CeilToInt(input.mapbounds.size.y * 2f),
                mapOrigin = input.mapbounds.min,
                rightAngles = true,
                obstacleMap = input.lowQualityMapReturn.obstacleMapBool,
                targetPos = StaticFindSectorOrigin(targetSector, input.mapbounds, input.flowFieldSize)
            };

            NativeArray<Node> AStarFlowMapPath = AStar.FindPathAStar(requestData);
            if (!AStarFlowMapPath.IsCreated || AStarFlowMapPath.Length == 0) return;

            NativeList<FlowFieldReturnStruct> flowFieldsPath = new NativeList<FlowFieldReturnStruct>(Allocator.Persistent);
            NativeList<Float2> sectorVectorPath = new NativeList<Float2>(Allocator.Persistent);
            NativeList<int3> flowFieldIdxReferences = new NativeList<int3>(Allocator.Persistent);

            for (int i = 0; i < AStarFlowMapPath.Length - 1; i++)
            {
                bool foundFlowField = false;
                Vector2 worldPos = AStarFlowMapPath[i].position;
                int2 sIdx = StaticFindCurrentSector(worldPos, input.mapbounds, input.flowFieldSize);

                sectorVectorPath.Add(new Float2(sIdx.x, sIdx.y));

                if (sIdx.x >= 0 && sIdx.x < input.flowFieldReturns.gridSize.x &&
                    sIdx.y >= 0 && sIdx.y < input.flowFieldReturns.gridSize.y)
                {
                    int flatTileIdx = sIdx.y * input.flowFieldReturns.gridSize.x + sIdx.x;
                    int2 tileRange = input.flowFieldReturns.tileOffsets[flatTileIdx];

                    for (int j = 0; j < tileRange.y; j++)
                    {
                        int globalIdx = tileRange.x + j;
                        float2 fieldDir = input.flowFieldReturns.dir[globalIdx];

                        if (math.all(fieldDir == (float2)AStarFlowMapPath[i].dir))
                        {
                            flowFieldsPath.Add(FlowFieldUtils.GetFlowFieldMetadataOnly(input.flowFieldReturns, globalIdx));
                            flowFieldIdxReferences.Add(new int3(sIdx.x, sIdx.y, j));
                            foundFlowField = true;
                            break;
                        }
                    }
                }

                if (!foundFlowField)
                {
                    Vector2 dir = AStarFlowMapPath[i].dir;
                    Vector2 sectorOrigin = StaticFindSectorOrigin(sIdx, input.mapbounds, input.flowFieldSize);

                    float halfSize = input.flowFieldSize * 0.5f;
                    Vector2 targetCenter = worldPos + (dir * halfSize);
                    Vector2 targetSize = (dir.x != 0) ? new Vector2(input.nodeSize, input.flowFieldSize) : new Vector2(input.flowFieldSize, input.nodeSize);

                    FlowField.FlowFieldRequestData request = new FlowField.FlowFieldRequestData
                    {
                        dir = dir,
                        targetBounds = new Float2Bounds(targetCenter, targetSize),
                        targetPos = targetCenter,
                        mapOrigin = sectorOrigin,
                        searchWidth = input.flowFieldSize,
                        searchHeight = input.flowFieldSize,
                        nodeSize = input.nodeSize,
                        obstacleMap = ObstacleMapUtils.RetrieveObstacleMap(input.obstacleMapReturns, sIdx).obstacleMapBool,
                        nodePosInTotalMap = new float2(sIdx.x, sIdx.y)
                    };

                    FlowFieldReturnStruct generated = FlowField.FindFlowFieldToRect(request);
                    if (!generated.failed)
                    {
                        generated.pathRef = i;
                        flowFieldsPath.Add(generated);
                        flowFieldIdxReferences.Add(new int3(sIdx.x, sIdx.y, -1));
                    }
                }
            }
            int destIndex = FlowFieldUtils.FindIndexPositionNative(input.destinationFlowFields.worldPos, input.TargetPos);
            FlowFieldReturnStruct destinationField;

            if (destIndex != -1)
            {
                destinationField = FlowFieldUtils.GetFlowFieldMetadataOnly(input.destinationFlowFields, destIndex);
            }
            else
            {
                int2 dIdx = StaticFindCurrentSector(input.TargetPos, input.mapbounds, input.flowFieldSize);
                FlowField.FlowFieldRequestData destRequest = new FlowField.FlowFieldRequestData
                {
                    mapOrigin = StaticFindSectorOrigin(dIdx, input.mapbounds, input.flowFieldSize),
                    searchWidth = input.flowFieldSize,
                    searchHeight = input.flowFieldSize,
                    nodeSize = input.nodeSize,
                    obstacleMap = ObstacleMapUtils.RetrieveObstacleMap(input.obstacleMapReturns, dIdx).obstacleMapBool,
                    targetPos = input.TargetPos
                };
                destinationField = FlowField.FindFlowFieldToPoint(destRequest);
            }

            destinationField.pathRef = AStarFlowMapPath.Length - 1;
            flowFieldsPath.Add(destinationField);
            pathReturns[a] = new NativePath
            {
                sectorPath = flowFieldsPath,
                flowFieldIdxReferences = flowFieldIdxReferences,
                sectorVectorPath = sectorVectorPath,
                destinationFlowFieldIdxReference = destIndex
            };
            if (AStarFlowMapPath.IsCreated) AStarFlowMapPath.Dispose();
        }
        public static int2 StaticFindCurrentSector(Vector2 pos, Float2Bounds bounds, int sectorSize)
        {
            float2 offset = (float2)pos - (float2)bounds.min;
            return new int2(math.floor(offset / sectorSize));
        }

        public static float2 StaticFindSectorOrigin(int2 sectorIdx, Float2Bounds bounds, int sectorSize)
        {
            return (float2)bounds.min + (new float2(sectorIdx.x, sectorIdx.y) * sectorSize);
        }
    }
    //public Path Pathfind(PathFindTSInput input)
    //{
    //    Path path = new Path();
    //    AStar.PathRequestData requestData = new AStar.PathRequestData();
    //    Float2 AIPos = input.AIPos;
    //    requestData.AIPos = findSectorOrigin(findCurrentSector(AIPos));
    //    requestData.nodeSize = input.flowFieldSize;
    //    requestData.searchWidth = Mathf.CeilToInt(input.mapbounds.size.x * 2f);
    //    requestData.searchHeight = Mathf.CeilToInt(input.mapbounds.size.y * 2f);
    //    requestData.mapOrigin = input.mapbounds.min;
    //    requestData.rightAngles = true;
    //    requestData.obstacleMap = input.lowQualityMapReturn.obstacleMapBool;
    //    requestData.targetPos = findSectorOrigin(findCurrentSector(input.TargetPos));
    //    List<Node> AStarFlowMapPath = AStar.FindPathAStar(requestData);
    //    if (AStarFlowMapPath == null)
    //    {
    //        //Debug.Log("A star pathfinding failed");
    //        return null;
    //    }
    //    List<FlowFieldReturn> flowFieldsPath = new List<FlowFieldReturn>();
    //    List<Float2> sectorVectorPath = new List<Float2>();
    //    List<int3> flowFieldIdxReferences = new List<int3>();
    //    for (int i = 0; i < AStarFlowMapPath.Count - 1; i++)
    //    {
    //        bool foundFlowField = false;
    //        Vector2 worldPos = AStarFlowMapPath[i].position;
    //        //Debug.Log("flow map path position " + worldPos + " flow map path dir " + AStarFlowMapPath[i].dir);
    //        Vector2 sectorIdx = findCurrentSector(worldPos);
    //        int sx = (int)sectorIdx.x;
    //        int sy = (int)sectorIdx.y;
    //        sectorVectorPath.Add(new Vector2(sx, sy));
    //        if (sx >= 0 && sx < input.flowFieldReturns.GetLength(0) && sy >= 0 && sy < input.flowFieldReturns.GetLength(1) && input.flowFieldReturns[sx, sy].Count > 0)
    //        {
    //            List<FlowFieldReturn> selectedFlowFields = input.flowFieldReturns[sx, sy];
    //            for (int j = 0; j < selectedFlowFields.Count; j++)
    //            {
    //                FlowFieldReturn selectedFlowField = selectedFlowFields[j];
    //                if (selectedFlowField.dir == AStarFlowMapPath[i].dir)
    //                {
    //                    flowFieldsPath.Add(selectedFlowField);
    //                    flowFieldIdxReferences.Add(new int3(sx, sy, j));
    //                    foundFlowField = true;
    //                    // //Debug.Log("Found existing flow field!");
    //                    break;
    //                }
    //            }

    //        }
    //        if (foundFlowField == false)
    //        {
    //            Vector2 dir = AStarFlowMapPath[i].dir;
    //            float halfSize = input.flowFieldSize * 0.5f;

    //            Vector2 sectorOrigin = findSectorOrigin(new Vector2(sx, sy));

    //            float nudge = input.nodeSize * 0.5f;
    //            Vector2 targetCenter = worldPos + (dir * (halfSize));

    //            Vector2 targetSize = (dir.x != 0)
    //                ? new Vector2(input.nodeSize, input.flowFieldSize)
    //                : new Vector2(input.flowFieldSize, input.nodeSize);

    //            Float2Bounds targetBounds = new Float2Bounds(new Float2(targetCenter.x, targetCenter.y), new Float2(targetSize.x, targetSize.y));
    //            //Debug.Log($"Target bounds flow field {i}: Size: X: {targetSize.x}, Y: {targetSize.y}. Position: X: {targetCenter.x} Y: {targetCenter.y}. Dir: X: {dir.x} Y: {dir.y}");
    //            FlowField.FlowFieldRequestData request = new FlowField.FlowFieldRequestData
    //            {
    //                dir = dir,
    //                targetBounds = targetBounds,
    //                targetPos = targetCenter,
    //                mapOrigin = sectorOrigin,
    //                searchWidth = input.flowFieldSize + 1,
    //                searchHeight = input.flowFieldSize + 1,
    //                nodeSize = input.nodeSize,
    //                obstacleMap = input.obstacleMapReturns[sx, sy].obstacleMapBool,
    //                nodePosInTotalMap = new Vector2(sx, sy)
    //            };
    //            //Debug.Log(input.obstacleMapReturns[sx, sy].obstacleMapBool.GetLength(0));
    //            FlowFieldReturn returnedFlowField = FlowField.FindFlowFieldToRect(request);
    //            if (returnedFlowField != null)
    //            {
    //                returnedFlowField.worldPos = sectorIdx;
    //                returnedFlowField.loopsSinceUpdated = 0;
    //                returnedFlowField.pos = sectorOrigin;
    //                flowFieldsPath.Add(returnedFlowField);
    //                sectorVectorPath.Add(new Vector2(sx, sy));
    //            }
    //            else
    //            {
    //                //Debug.LogError("Flow Field pathfinding failed!");
    //                return null;
    //            }

    //        }
    //        flowFieldsPath[i].pathRef = i;
    //    }
    //    int index = input.destinationFlowFields.FindIndex(x => x.pos == Float2.ConvertToV2(input.TargetPos));
    //    FlowFieldReturn destinationField = null;
    //    if (index != -1)
    //    {
    //        destinationField = input.destinationFlowFields[index];
    //    }

    //    if (destinationField == null)
    //    {
    //        Vector2 lastPathNodePos = AStarFlowMapPath[AStarFlowMapPath.Count - 1].position;
    //        Vector2 sectorIdx = findCurrentSector(lastPathNodePos);
    //        int sx = (int)sectorIdx.x;
    //        int sy = (int)sectorIdx.y;

    //        Vector2 sectorOrigin = findSectorOrigin(new Vector2(sx, sy));
    //        FlowField.FlowFieldRequestData request = new FlowField.FlowFieldRequestData
    //        {
    //            searchHeight = input.flowFieldSize + 1,
    //            searchWidth = input.flowFieldSize + 1,
    //            nodeSize = input.nodeSize,
    //            mapOrigin = sectorOrigin,
    //            obstacleMap = input.obstacleMapReturns[sx, sy].obstacleMapBool,
    //            targetPos = input.TargetPos,
    //            nodePosInTotalMap = new Vector2(sx, sy)
    //        };
    //        destinationField = FlowField.FindFlowFieldToPoint(request);
    //        destinationField.loopsSinceUpdated = 0;
    //        destinationField.pathRef = AStarFlowMapPath.Count - 1;
    //    }
    //    if (destinationField == null)
    //    {
    //        //Debug.Log("Unable to get destination flow field");
    //        return null;
    //    }
    //    flowFieldsPath.Add(destinationField);
    //    sectorVectorPath.Add(destinationField.pos);

    //    path.sectorVectorPath = sectorVectorPath;
    //    path.sectorPath = flowFieldsPath;
    //    path.flowFieldIdxReferences = flowFieldIdxReferences;
    //    path.destinationFlowFieldIdxReference = index;
    //    path.cachedSectorPath = new List<FlowFieldReturn>();
    //    path.cachedSectorVectorPath = new List<Float2>();
    //    return path;


    //}
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
        public struct FlowFieldRequestData
        {
            public Vector2 targetPos;
            //Only used for FindFlowFieldToRect
            public Float2Bounds targetBounds;
            public int searchWidth;
            public int searchHeight;
            public Vector2 mapOrigin;
            public float nodeSize;
            public NativeSlice<bool> obstacleMap;
            public int2 obstacleMapSize;
            public Vector2 dir;
            public Vector2 nodePosInTotalMap;
            public FlowFieldReturn pathFindingTask;
        }
        [BurstCompile]
        public static FlowFieldReturnStruct FindFlowFieldToPoint(FlowFieldRequestData request)
        {
            NativeQueue<int> openNodes = new NativeQueue<int>(Allocator.Temp);
            int nodeGridWidth = Mathf.Max(1, Mathf.CeilToInt(request.searchWidth / request.nodeSize));
            int nodeGridHeight = Mathf.Max(1, Mathf.CeilToInt(request.searchHeight / request.nodeSize));
            NativeArray<FlowNode> totalNodes = new NativeArray<FlowNode>(nodeGridWidth * nodeGridHeight, Allocator.Persistent);
            if (request.targetPos.x < request.mapOrigin.x - 0.1f ||
                    request.targetPos.x > request.mapOrigin.x + request.searchWidth + 0.1f ||
                    request.targetPos.y < request.mapOrigin.y - 0.1f ||
                    request.targetPos.y > request.mapOrigin.y + request.searchHeight + 0.1f)
            {
                return new FlowFieldReturnStruct { failed = true };
            }

            for (int i = 0; i < totalNodes.Length; i++)
            {
                int x = i % nodeGridWidth;
                int y = i / nodeGridWidth;
                totalNodes[i] = new FlowNode
                {
                    position = new float2(request.mapOrigin.x + x * request.nodeSize, request.mapOrigin.y + y * request.nodeSize),
                    totalCost = float.MaxValue,
                    gridPos = new int2(x, y),
                    idx = i
                };
            }


            int startX = Mathf.RoundToInt((request.targetPos.x - request.mapOrigin.x) / request.nodeSize);
            int startY = Mathf.RoundToInt((request.targetPos.y - request.mapOrigin.y) / request.nodeSize);
            startX = Mathf.Clamp(startX, 0, nodeGridWidth - 1);
            startY = Mathf.Clamp(startY, 0, nodeGridHeight - 1);

            FlowNode startNode = totalNodes[startY * nodeGridWidth + startX];
            startNode.totalCost = 0;
            startNode.idx = startY * nodeGridWidth + startX;
            openNodes.Enqueue(startNode.idx);


            while (openNodes.Count > 0)
            {
                FlowNode currentNode = totalNodes[openNodes.Dequeue()];

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = currentNode.gridPos.x + dx;
                        int ny = currentNode.gridPos.y + dy;

                        if (nx >= 0 && nx < nodeGridWidth && ny >= 0 && ny < nodeGridHeight)
                        {
                            if (request.obstacleMap[ny * request.obstacleMapSize.x * nx]) continue;

                            float moveCost = (dx != 0 && dy != 0) ? 1.414f : 1.0f;

                            float2 moveStep = math.normalize(new float2(dx, dy));
                            if (math.dot(moveStep, request.dir) < 0.5f)
                            {
                                moveCost += 10.0f;
                            }

                            float newCost = currentNode.totalCost + moveCost;

                            if (newCost < totalNodes[ny * nodeGridWidth + nx].totalCost)
                            {
                                FlowNode neighbor = totalNodes[ny * nodeGridWidth + nx];
                                neighbor.totalCost = newCost;
                                totalNodes[ny * nodeGridWidth + nx] = neighbor;
                                openNodes.Enqueue(totalNodes[ny * nodeGridWidth + nx].idx);
                            }
                        }
                    }
                }
            }

            for (int x = 0; x < nodeGridWidth; x++)
            {
                for (int y = 0; y < nodeGridHeight; y++)
                {
                    FlowNode current = totalNodes[y * nodeGridWidth + x];
                    if (totalNodes[y * nodeGridWidth + x].totalCost == 0 || totalNodes[y * nodeGridWidth + x].totalCost == float.MaxValue)
                    {

                        current.dir = request.dir;
                        totalNodes[y * nodeGridWidth + x] = current;
                        continue;
                    }

                    float bestCost = totalNodes[y * nodeGridWidth + x].totalCost;
                    float2 bestDir = request.dir;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < nodeGridWidth && ny >= 0 && ny < nodeGridHeight)
                            {
                                if (totalNodes[ny * nodeGridWidth + nx].totalCost < bestCost - 0.01f)
                                {
                                    bestCost = totalNodes[ny * nodeGridWidth + nx].totalCost;
                                    bestDir = new float2(dx, dy);
                                }
                            }
                        }
                    }
                    if (math.lengthsq(bestDir) > 0.00001f)
                    {
                        current.dir = math.normalize(bestDir);
                    }
                    else
                    {
                        current.dir = float2.zero;
                    }
                    totalNodes[y * nodeGridWidth + x] = current;
                }
            }

            FlowFieldReturnStruct fieldReturn = new FlowFieldReturnStruct { flattenedField = totalNodes, dir = request.dir, nodeX = (int)request.nodePosInTotalMap.x, nodeY = (int)request.nodePosInTotalMap.y, failed = false };
            openNodes.Dispose();
            return fieldReturn;
        }

        [BurstCompile]
        public static FlowFieldReturnStruct FindFlowFieldToRect(FlowFieldRequestData request)
        {
            NativeQueue<int> openNodes = new NativeQueue<int>(Allocator.Temp);
            int nodeGridWidth = Mathf.Max(1, Mathf.CeilToInt(request.searchWidth / request.nodeSize));
            int nodeGridHeight = Mathf.Max(1, Mathf.CeilToInt(request.searchHeight / request.nodeSize));
            NativeArray<FlowNode> totalNodes = new NativeArray<FlowNode>(nodeGridWidth * nodeGridHeight, Allocator.Persistent);

            for (int i = 0; i < totalNodes.Length; i++)
            {
                int x = i % nodeGridWidth;
                int y = i / nodeGridWidth;
                totalNodes[i] = new FlowNode
                {
                    position = new float2(request.mapOrigin.x + x * request.nodeSize, request.mapOrigin.y + y * request.nodeSize),
                    totalCost = float.MaxValue,
                    gridPos = new int2(x, y),
                    idx = i
                };
            }

            float2 reqDir = (float2)request.dir;
            if (math.all(reqDir == new float2(1, 0)))
            {
                for (int y = 0; y < nodeGridHeight; y++) Seed(nodeGridWidth - 1, y);
            }
            else if (math.all(reqDir == new float2(-1, 0)))
            {
                for (int y = 0; y < nodeGridHeight; y++) Seed(0, y);
            }
            else if (math.all(reqDir == new float2(0, 1)))
            {
                for (int x = 0; x < nodeGridWidth; x++) Seed(x, nodeGridHeight - 1);
            }
            else if (math.all(reqDir == new float2(0, -1)))
            {
                for (int x = 0; x < nodeGridWidth; x++) Seed(x, 0);
            }

            void Seed(int x, int y)
            {
                int idx = y * nodeGridWidth + x;
                FlowNode n = totalNodes[idx];
                n.totalCost = 0;
                totalNodes[idx] = n;
                openNodes.Enqueue(idx);
            }
            while (openNodes.Count > 0)
            {
                int currIdx = openNodes.Dequeue();
                FlowNode currentNode = totalNodes[currIdx];

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = currentNode.gridPos.x + dx;
                        int ny = currentNode.gridPos.y + dy;

                        if (nx >= 0 && nx < nodeGridWidth && ny >= 0 && ny < nodeGridHeight)
                        {
                            if (request.obstacleMap[ny * request.obstacleMapSize.x + nx]) continue;

                            float moveCost = (dx != 0 && dy != 0) ? 1.4142f : 1.0f;
                            float newCost = currentNode.totalCost + moveCost;

                            int neighborIdx = ny * nodeGridWidth + nx;
                            if (newCost < totalNodes[neighborIdx].totalCost)
                            {
                                FlowNode neighbor = totalNodes[neighborIdx];
                                neighbor.totalCost = newCost;
                                totalNodes[neighborIdx] = neighbor;
                                openNodes.Enqueue(neighborIdx);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < totalNodes.Length; i++)
            {
                FlowNode node = totalNodes[i];
                if (node.totalCost == 0)
                {
                    node.dir = request.dir;
                    totalNodes[i] = node;
                    continue;
                }

                float bestCost = node.totalCost;
                float2 bestDir = float2.zero;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = node.gridPos.x + dx;
                        int ny = node.gridPos.y + dy;

                        if (nx >= 0 && nx < nodeGridWidth && ny >= 0 && ny < nodeGridHeight)
                        {
                            float nCost = totalNodes[ny * nodeGridWidth + nx].totalCost;
                            if (nCost < bestCost)
                            {
                                bestCost = nCost;
                                bestDir = new float2(dx, dy);
                            }
                        }
                    }
                }

                if (math.lengthsq(bestDir) > 0.001f)
                    node.dir = math.normalize(bestDir);
                else
                    node.dir = float2.zero;

                totalNodes[i] = node;
            }

            openNodes.Dispose();
            return new FlowFieldReturnStruct
            {
                flattenedField = totalNodes,
                dir = request.dir,
                nodeX = (int)request.nodePosInTotalMap.x,
                nodeY = (int)request.nodePosInTotalMap.y
            };
        }
    }
    public static class AStar
    {
        public struct PathRequestData
        {
            public Vector2 targetPos;
            public Vector2 mapOrigin;
            public int searchWidth;
            public int searchHeight;
            public float nodeSize;
            public Vector2 AIPos;
            public NativeSlice<bool> obstacleMap;
            public int2 obstacleMapSize;
            public bool rightAngles;
        }
        [BurstCompile]
        public static NativeArray<Node> FindPathAStar(PathRequestData pathData)
        {

            if (pathData.nodeSize <= 0) pathData.nodeSize = 1f;

            int halfWidth = pathData.searchWidth / 2;
            int halfHeight = pathData.searchHeight / 2;
            int nodeGridWidth = pathData.obstacleMapSize.x;
            int nodeGridHeight = pathData.obstacleMapSize.y;
            Vector2 gridWorldOrigin = pathData.mapOrigin;
            NativeArray<Node> totalNodes = new NativeArray<Node>(nodeGridWidth * nodeGridHeight, Allocator.Temp);
            // //Debug.Log($"Node width: {nodeGridWidth}, height: {halfHeight}. Pathdata width: {pathData.searchWidth}, height: {pathData.searchHeight}");

            if (pathData.AIPos.x < gridWorldOrigin.x || pathData.AIPos.x > gridWorldOrigin.x + pathData.searchWidth ||
                pathData.AIPos.y < gridWorldOrigin.y || pathData.AIPos.y > gridWorldOrigin.y + pathData.searchHeight)
            {
                // //Debug.LogError($"AI {pathData.AIIndex}'s start position {pathData.AIPos} is outside search area origin {gridWorldOrigin} size ({pathData.searchWidth},{pathData.searchHeight}). Expand search area or recenter.");
                return new NativeArray<Node>();
            }
            int index = 0;
            for (int i = 0; i < totalNodes.Length; i++)
            {
                int x = i % nodeGridWidth;
                int y = i / nodeGridWidth;
                totalNodes[i] = new Node
                {
                    position = new float2(pathData.mapOrigin.x + x * pathData.nodeSize, pathData.mapOrigin.y + y * pathData.nodeSize),
                    startCost = float.MaxValue,
                    targetDistance = float.MaxValue,
                    totalCost = float.MaxValue,
                    parentIdx = new int2(-1, -1),
                    idx = i,
                };
            }

            NativeList<int> OpenNodes = new NativeList<int>();
            int startX = Mathf.Clamp(Mathf.RoundToInt((pathData.AIPos.x - gridWorldOrigin.x) / pathData.nodeSize), 0, nodeGridWidth - 1);
            int startY = Mathf.Clamp(Mathf.RoundToInt((pathData.AIPos.y - gridWorldOrigin.y) / pathData.nodeSize), 0, nodeGridHeight - 1);
            index = startY * nodeGridWidth + startX;
            Node startNode = totalNodes[index];
            startNode.startCost = 0;
            startNode.totalCost = math.distance(startNode.position, (float2)pathData.targetPos);
            totalNodes[index] = startNode;
            OpenNodes.Add(index);
            bool targetFound = false;
            int endNodeIdx = -1;

            while (OpenNodes.Length > 0)
            {
                int bestNodeIndex = -1;
                float lowestCost = float.MaxValue;
                for (int i = 0; i < OpenNodes.Length; i++)
                {
                    if (totalNodes[OpenNodes[i]].totalCost < lowestCost)
                    {
                        lowestCost = totalNodes[OpenNodes[i]].totalCost;
                        bestNodeIndex = i;
                    }
                }
                int currentIdx = OpenNodes[bestNodeIndex];
                Node currentNode = totalNodes[currentIdx];
                OpenNodes.RemoveAtSwapBack(bestNodeIndex);
                if (Vector2.Distance(currentNode.position, pathData.targetPos) < pathData.nodeSize * 0.9f)
                {
                    targetFound = true;
                    endNodeIdx = currentIdx;
                    break;
                }

                int currentNodeGridX = Mathf.Clamp(Mathf.RoundToInt((currentNode.position.x - gridWorldOrigin.x) / pathData.nodeSize), 0, nodeGridWidth - 1);
                int currentNodeGridY = Mathf.Clamp(Mathf.RoundToInt((currentNode.position.y - gridWorldOrigin.y) / pathData.nodeSize), 0, nodeGridHeight - 1);
                int currX = currentIdx % nodeGridWidth;
                int currY = currentIdx / nodeGridWidth;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (pathData.rightAngles && math.abs(dx) + math.abs(dy) > 1) continue;

                        int nx = currX + dx;
                        int ny = currY + dy;

                        if (nx >= 0 && nx < nodeGridWidth && ny >= 0 && ny < nodeGridHeight)
                        {
                            if (pathData.obstacleMap[ny * pathData.obstacleMapSize.x + nx]) continue;

                            int neighborIdx = ny * nodeGridWidth + nx;
                            Node neighbor = totalNodes[neighborIdx];

                            float newStartCost = currentNode.startCost + math.distance(currentNode.position, neighbor.position);

                            if (newStartCost < neighbor.startCost)
                            {
                                neighbor.parentIdx = new int2(currX, currY);
                                neighbor.startCost = newStartCost;
                                neighbor.targetDistance = math.distance(neighbor.position, (float2)pathData.targetPos);
                                neighbor.totalCost = newStartCost + neighbor.targetDistance;

                                totalNodes[neighborIdx] = neighbor;

                                bool alreadyIn = false;
                                for (int j = 0; j < OpenNodes.Length; j++) if (OpenNodes[j] == neighborIdx) alreadyIn = true;
                                if (!alreadyIn) OpenNodes.Add(neighborIdx);
                            }
                        }
                    }
                }

                currentNode.evaluated = true;
            }

            NativeArray<Node> finalPath;
            if (targetFound)
            {
                NativeList<Node> tempPath = new NativeList<Node>(Allocator.Temp);
                int traceIdx = endNodeIdx;
                while (traceIdx != -1)
                {
                    Node pNode = totalNodes[traceIdx];
                    tempPath.Add(pNode);
                    if (pNode.parentIdx.x == -1) break;
                    traceIdx = pNode.parentIdx.y * nodeGridWidth + pNode.parentIdx.x;
                }

                finalPath = new NativeArray<Node>(tempPath.Length, Allocator.TempJob);
                for (int i = 0; i < tempPath.Length; i++)
                {
                    int revIdx = tempPath.Length - 1 - i;
                    finalPath[i] = tempPath[revIdx];
                }

                for (int j = 0; j < finalPath.Length - 1; j++)
                {
                    Node n = finalPath[j];
                    n.dir = math.normalize(finalPath[j + 1].position - n.position);
                    finalPath[j] = n;
                }
                tempPath.Dispose();
            }
            else
            {
                finalPath = new NativeArray<Node>(0, Allocator.TempJob);
            }

            totalNodes.Dispose();
            OpenNodes.Dispose();

            return finalPath;
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
