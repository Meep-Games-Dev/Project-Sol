using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PathRequest
{
    public PathFinderAI AI;
    public Task<List<Node>> PathfindingTask;
}
public class BoundsObj
{
    public Rect bounds;
    public int boundsGameObjID;
}
public class PathFinderAI
{
    public GameObject obj;
    public float velocity;
    public float maxVelocity = 5f;
    public List<Node> path = new List<Node>();
    public bool targetSet = false;
    public Vector2 targetPos;
    public int loops;
    public bool currentlyInAITask;
    public AIPathFindingData pathData = new AIPathFindingData();
    public int instanceID;
    public Task<bool[,]> obstacleReturn;
}
public class AIPathFindingData
{
    public Vector2 mapOrigin = new Vector2();
    public int mapWidth = new int();
    public int mapHeight = new int();
    public float pathResolution = new float();
}
public class ObstacleMapReturn
{
    public bool[,] obstacleMap;
    public PathFinderAI pathfinderAI;
}
public static class DetectObstaclesInPosition
{
    public static void DrawRectangle(Vector3 position, Vector3 extent, Color color)
    {
        // Calculate the four corner points relative to the center
        Vector3 rightOffset = Vector3.right * extent.x;
        Vector3 upOffset = Vector3.up * extent.y;

        Vector3 p1 = position + rightOffset + upOffset;  // Top-Right
        Vector3 p2 = position - rightOffset + upOffset;  // Top-Left
        Vector3 p3 = position - rightOffset - upOffset;  // Bottom-Left
        Vector3 p4 = position + rightOffset - upOffset;  // Bottom-Right

        // Draw the four lines connecting the corners
        Debug.DrawLine(p1, p2, color); // Top
        Debug.DrawLine(p2, p3, color); // Left
        Debug.DrawLine(p3, p4, color); // Bottom
        Debug.DrawLine(p4, p1, color); // Right
    }
    public static Obstacle SetupObstacle(GameObject obj)
    {
        if (obj.GetComponent<Collider2D>() != null)
        {
            Obstacle returnObstacle = new Obstacle
            {
                position = new float2(obj.transform.position.x, obj.transform.position.y),
                size = new float2(obj.GetComponent<Collider2D>().bounds.size.x, obj.GetComponent<Collider2D>().bounds.size.y),
                name = obj.name.ToString(),
                instanceID = obj.GetInstanceID()
            };
            return returnObstacle;
        }
        else
        {
            Debug.LogError("ERR: OBJ " + obj.name + " DOES NOT INCLUDE A COLLIDER, PLEASE ADD ONE!");
            return null;
        }
    }
    public static MapTarget SetupMapTarget(GameObject obj)
    {
        MapTarget returnTarget = new MapTarget
        {
            position = new float2(obj.transform.position.x, obj.transform.position.y),
            name = obj.name.ToString(),
            instanceID = obj.GetInstanceID()
        };
        return returnTarget;
    }
    public static async Task<CompleteObstacleMapReturn> CompleteObstacleMap(List<Obstacle> obstacles, float resolution, List<MapTarget> mapTargets)
    {

        float2Bounds boundingBox = new float2Bounds(obstacles[0].position, obstacles[0].size);
        int tilesPerBatch = 400;
        for (int i = 1; i < obstacles.Count; i++)
        {
            boundingBox.Encapsulate(new float2Bounds(obstacles[i].position, obstacles[i].size));
            /*
            if (Contains(obstacles[0].position, obstacles[0].size, obstacles[i].position, obstacles[i].size))
            {
                boundingBox.Encapsulate(new float2Bounds(obstacles[0].position, obstacles[0].size));
                
                if (obstacles[i].position.x < leftBCorner.x)
                {
                    leftBCorner.x -= (obstacles[i].position.x - (obstacles[i].size.x / 2));`
                    leftTCorner.x -= (obstacles[i].position.x - (obstacles[i].size.x / 2));
                }
                if (obstacles[i].position.x > rightBCorner.x)
                {
                    rightBCorner.x += (obstacles[i].position.x + (obstacles[i].size.x / 2));
                    rightTCorner.x += (obstacles[i].position.x + (obstacles[i].size.x / 2));
                }
                if (obstacles[i].position.y < leftBCorner.y)
                {
                    leftBCorner.y -= (obstacles[i].position.y - (obstacles[i].size.y / 2));
                    rightBCorner.y -= (obstacles[i].position.y - (obstacles[i].size.y / 2));
                }
                if (obstacles[i].position.y > leftTCorner.y)
                {
                    leftTCorner.y += (obstacles[i].position.y + (obstacles[i].size.y / 2));
                    rightTCorner.y += (obstacles[i].position.y + (obstacles[i].size.y / 2));
                }
                
            }
        */
        }
        for (int i = 0; i < mapTargets.Count; i++)
        {
            boundingBox.Encapsulate(new float2Bounds(mapTargets[i].position, new float2(5, 5)));
        }
        float2 leftBCorner = new float2(boundingBox.position.x - (boundingBox.size.x / 2), boundingBox.position.y - (boundingBox.size.y / 2));
        float2 rightBCorner = new float2(boundingBox.position.x + (boundingBox.size.x / 2), boundingBox.position.y - (boundingBox.size.y / 2));
        float2 leftTCorner = new float2(boundingBox.position.x - (boundingBox.size.x / 2), boundingBox.position.y + (boundingBox.size.y / 2));
        float2 rightTCorner = new float2(boundingBox.position.x + (boundingBox.size.x / 2), boundingBox.position.y + (boundingBox.size.y / 2));
        float2 center = boundingBox.position;
        float2 size = boundingBox.size;

        int nodeGridWidth = Mathf.Max(1, Mathf.CeilToInt(size.x / resolution));
        int nodeGridHeight = Mathf.Max(1, Mathf.CeilToInt(size.y / resolution));
        CompleteObstacleMapReturn objMap = new CompleteObstacleMapReturn
        {
            obstacleMap = new List<Obstacle>[nodeGridWidth, nodeGridHeight],
            size = new float2(nodeGridWidth, nodeGridHeight),
            startArea = leftBCorner,
            bounds = boundingBox
        };
        int tilesDone = 0;
        for (int x = 0; x < nodeGridWidth; x++)
        {
            for (int y = 0; y < nodeGridHeight; y++)
            {
                //Start at the bottom left corner
                float2 nodePos = new float2(
                    leftBCorner.x + x * resolution,
                    leftBCorner.y + y * resolution
                );
                //Set objMap to null if no obstacles found
                objMap.obstacleMap[x, y] = new List<Obstacle>();
                //Add all obstacles in position to a list
                for (int j = 0; j < obstacles.Count; j++)
                {
                    if (ContainsPoint(obstacles[j].position, obstacles[j].size, nodePos).any)
                    {
                        objMap.obstacleMap[x, y].Add(obstacles[j]);
                    }
                }
                tilesDone++;
                if (tilesDone > tilesPerBatch)
                {
                    tilesDone = 0;
                    await Awaitable.MainThreadAsync();
                    await Awaitable.NextFrameAsync();
                    await Awaitable.BackgroundThreadAsync();
                }
            }
        }
        return objMap;
    }
    public static bool[,] DetectObstacleMap(float2 size, List<Obstacle> obstacles, Obstacle exclude, float pathResolution)
    {
        int halfWidth = Mathf.RoundToInt(size.x / 2);
        int halfHeight = Mathf.RoundToInt(size.y / 2);
        int nodeGridWidth = Mathf.Max(1, Mathf.RoundToInt(size.x / pathResolution));
        int nodeGridHeight = Mathf.Max(1, Mathf.RoundToInt(size.y / pathResolution));
        bool[,] obstacleMapReturn = new bool[(int)size.x, (int)size.y];
        float2 nodePosOrigin = new float2(exclude.position.x - halfWidth, exclude.position.y - halfHeight);
        for (int x = 0; x < nodeGridWidth; x++)
        {
            for (int y = 0; y < nodeGridHeight; y++)
            {
                float2 nodePos = new float2(
                    nodePosOrigin.x + x * pathResolution,
                    nodePosOrigin.y + y * pathResolution
                );
                if (DetectInPositionExclusive(new float2(nodePos.x, nodePos.y), exclude.size, exclude.instanceID, obstacles))
                {
                    obstacleMapReturn[x, y] = true;
                }
                else
                {
                    obstacleMapReturn[x, y] = false;
                }
            }
        }
        return obstacleMapReturn;
    }
    public class ContainsPointReturn
    {
        //Left
        public bool s1 = true;
        //Top
        public bool s2 = true;
        //Right
        public bool s3 = true;
        //Bottom
        public bool s4 = true;

        public bool any = true;
    }
    public static bool DetectInPositionExclusive(float2 position, float2 size, int ExcludeinstanceID, List<Obstacle> obstacles)
    {
        for (int i = 0; i < obstacles.Count; i++)
        {
            if (obstacles[i].instanceID == ExcludeinstanceID)
            {
                continue;
            }
            else
            {
                if (Contains(position, size, obstacles[i].position, obstacles[i].size))
                {
                    return true;
                }
            }
        }
        return false;
    }
    public static bool Contains(float2 position1, float2 size1, float2 position2, float2 size2)
    {
        float2 hA = new float2(size1.x / 2, size1.y / 2);
        float2 hB = new float2(size2.x / 2, size2.y / 2);
        float2 s1A = new float2(position1.x - hA.x, position1.y + hA.y);
        float2 s2A = new float2(position1.x + hA.x, position1.y + hA.y);
        float2 s3A = new float2(position1.x + hA.x, position1.y - hA.y);
        float2 s4A = new float2(position1.x - hA.x, position1.y - hA.y);
        float2 s1B = new float2(position2.x - hB.x, position2.y + hB.y);
        float2 s2B = new float2(position2.x + hB.x, position2.y + hB.y);
        float2 s3B = new float2(position2.x + hB.x, position2.y - hB.y);
        float2 s4B = new float2(position2.x - hB.x, position2.y - hB.y);

        if (s1A.x < s2B.x && s2A.x > s1B.x && s4A.y < s2B.y && s2A.y > s4B.y)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static ContainsPointReturn ContainsPoint(float2 position1, float2 size1, float2 position2)
    {
        float2 hA = new float2(size1.x / 2, size1.y / 2);
        float2 s1A = new float2(position1.x - hA.x, position1.y + hA.y);
        float2 s2A = new float2(position1.x + hA.x, position1.y + hA.y);
        float2 s3A = new float2(position1.x + hA.x, position1.y - hA.y);
        float2 s4A = new float2(position1.x - hA.x, position1.y - hA.y);

        if (s1A.x < position2.x && s2A.x > position2.x && s4A.y < position2.y && s2A.y > position2.y)
        {
            return new ContainsPointReturn();
        }
        else
        {
            ContainsPointReturn returnVal = new ContainsPointReturn();
            if (s1A.x > position2.x)
            {
                returnVal.s1 = true;
                returnVal.s3 = false;
            }
            else if (s2A.x < position2.x)
            {
                returnVal.s3 = true;
                returnVal.s1 = false;
            }
            else
            {
                returnVal.s3 = false;
                returnVal.s1 = false;
            }

            if (s4A.y > position2.y)
            {
                returnVal.s4 = true;
                returnVal.s2 = false;
            }
            else if (s2A.y < position2.y)
            {
                returnVal.s2 = true;
                returnVal.s4 = false;
            }
            else
            {
                returnVal.s2 = false;
                returnVal.s4 = false;
            }
            returnVal.any = false;
            return returnVal;
        }

    }
}
