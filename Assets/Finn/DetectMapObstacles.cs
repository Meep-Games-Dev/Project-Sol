using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class ObstacleMapRequest
{
    public Task<bool[,]> obstacleMapReturn;
    public float timeStarted;
    public float timeCompleted;
}
public class Obstacle
{
    public Vector2 position;
    public Vector2 size;
    public string name;
    public int instanceID;
}

public class DetectMapObstacles : MonoBehaviour
{
    public GameObject randomObj;
    private Obstacle randomObst = new Obstacle();
    public ObstacleManager obstacleManager;
    public List<ObstacleMapRequest> mapRequests = new List<ObstacleMapRequest>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        obstacleManager = FindFirstObjectByType(typeof(ObstacleManager)).GetComponent<ObstacleManager>();
        randomObst = DetectObstaclesInPosition.SetupObstacle(randomObj);
        List<Obstacle> obstacles = obstacleManager.GetObstaclesInScene();
        for (int i = 0; i < 100; i++)
        {
            ObstacleMapRequest rq = new ObstacleMapRequest
            {
                obstacleMapReturn = Task.Run(() => DetectObstaclesInPosition.DetectObstacleMap(new Vector2(50, 50), obstacles, randomObst, 1)),
                timeStarted = Time.time
            };
            mapRequests.Add(rq);
        }
    }

    // Update is called once per frame
    void Update()
    {
        List<Obstacle> obstacles = obstacleManager.GetObstaclesInScene();
        for (int i = 0; i < mapRequests.Count; i++)
        {

            if (mapRequests[i].obstacleMapReturn.IsCompletedSuccessfully)
            {
                mapRequests[i].obstacleMapReturn = null;
                mapRequests[i].timeCompleted = Time.time;
                Debug.Log("Task completed successfully in " + (mapRequests[i].timeCompleted - mapRequests[i].timeStarted) + " seconds");
                ObstacleMapRequest rq = new ObstacleMapRequest
                {
                    obstacleMapReturn = Task.Run(() => DetectObstaclesInPosition.DetectObstacleMap(new Vector2(50, 50), obstacles, randomObst, 1))
                };
                mapRequests[i] = rq;
                mapRequests[i].timeStarted = Time.time;
            }

        }
    }

    public List<Obstacle> ObstacleReturn()
    {
        List<Obstacle> obstacles = new List<Obstacle>();
        Collider2D[] obstaclesWithColliders = (Collider2D[])FindObjectsByType(typeof(Collider2D), FindObjectsSortMode.None);
        for (int i = 0; i < obstaclesWithColliders.Length; i++)
        {
            Obstacle obstacle = DetectObstaclesInPosition.SetupObstacle(obstaclesWithColliders[i].gameObject);
            obstacles.Add(obstacle);
        }
        return obstacles;
    }
}

public static class DetectObstaclesInPosition
{
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
                position = obj.transform.position,
                size = obj.GetComponent<Collider2D>().bounds.size,
                name = obj.name,
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
    public static bool[,] DetectObstacleMap(Vector2 size, List<Obstacle> obstacles, Obstacle exclude, float pathResolution)
    {
        int halfWidth = Mathf.RoundToInt(size.x / 2);
        int halfHeight = Mathf.RoundToInt(size.y / 2);
        int nodeGridWidth = Mathf.Max(1, Mathf.RoundToInt(size.x / pathResolution));
        int nodeGridHeight = Mathf.Max(1, Mathf.RoundToInt(size.y / pathResolution));
        bool[,] obstacleMapReturn = new bool[(int)size.x, (int)size.y];
        Vector2 nodePosOrigin = new Vector2(exclude.position.x - halfWidth, exclude.position.y - halfHeight);
        for (int x = 0; x < nodeGridWidth; x++)
        {
            for (int y = 0; y < nodeGridHeight; y++)
            {
                Vector2 nodePos = new Vector2(
                    nodePosOrigin.x + x * pathResolution,
                    nodePosOrigin.y + y * pathResolution
                );
                if (DetectInPositionExclusive(new Vector2(nodePos.x, nodePos.y), exclude.size, exclude.instanceID, obstacles))
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
    public static bool DetectInPositionExclusive(Vector2 position, Vector2 size, int ExcludeinstanceID, List<Obstacle> obstacles)
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
    public static bool Contains(Vector2 position1, Vector2 size1, Vector2 position2, Vector2 size2)
    {
        Vector2 hA = new Vector2(size1.x / 2, size1.y / 2);
        Vector2 hB = new Vector2(size2.x / 2, size2.y / 2);
        Vector2 s1A = new Vector2(position1.x - hA.x, position1.y + hA.y);
        Vector2 s2A = new Vector2(position1.x + hA.x, position1.y + hA.y);
        Vector2 s3A = new Vector2(position1.x + hA.x, position1.y - hA.y);
        Vector2 s4A = new Vector2(position1.x - hA.x, position1.y - hA.y);
        Vector2 s1B = new Vector2(position2.x - hB.x, position2.y + hB.y);
        Vector2 s2B = new Vector2(position2.x + hB.x, position2.y + hB.y);
        Vector2 s3B = new Vector2(position2.x + hB.x, position2.y - hB.y);
        Vector2 s4B = new Vector2(position2.x - hB.x, position2.y - hB.y);

        if (s1A.x < s2B.x && s2A.x > s1B.x && s4A.y < s2B.y && s2A.y > s4B.y)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static ContainsPointReturn ContainsPoint(Vector2 position1, Vector2 size1, Vector2 position2)
    {
        Vector2 hA = new Vector2(size1.x / 2, size1.y / 2);
        Vector2 s1A = new Vector2(position1.x - hA.x, position1.y + hA.y);
        Vector2 s2A = new Vector2(position1.x + hA.x, position1.y + hA.y);
        Vector2 s3A = new Vector2(position1.x + hA.x, position1.y - hA.y);
        Vector2 s4A = new Vector2(position1.x - hA.x, position1.y - hA.y);

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
