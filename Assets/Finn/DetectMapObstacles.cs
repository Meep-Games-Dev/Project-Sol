using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    public float2 position;
    public float2 size;
    public string name;
    public int instanceID;
}
public class MapTarget
{
    public float2 position;
    public string name;
    public int instanceID;
}
public class CompleteObstacleMapReturn
{
    public List<Obstacle>[,] obstacleMap;
    public float2 startArea;
    public float2 size;
    public float2Bounds bounds;
}
public struct float2
{
    public float x;
    public float y;
    public float2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    public static float2 Lerp(float2 a, float2 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new float2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
    }
    public static float Distance(float2 a, float2 b)
    {
        float num = a.x - b.x;
        float num2 = a.y - b.y;
        return (float)Math.Sqrt(num * num + num2 * num2);
    }
    public static float2 Min(float2 lhs, float2 rhs)
    {
        return new float2(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y));
    }
    public static float2 Max(float2 lhs, float2 rhs)
    {
        return new float2(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y));
    }
    public static float2 operator +(float2 a, float2 b)
    {
        return new float2(a.x + b.x, a.y + b.y);
    }
    public static float2 operator -(float2 a, float2 b)
    {
        return new float2(a.x - b.x, a.y - b.y);
    }
    public static float2 operator -(float2 a)
    {
        return new float2(0f - a.x, 0f - a.y);
    }

    public static float2 operator *(float2 a, float d)
    {
        return new float2(a.x * d, a.y * d);
    }

    public static float2 operator *(float d, float2 a)
    {
        return new float2(a.x * d, a.y * d);
    }

    public static float2 operator /(float2 a, float d)
    {
        return new float2(a.x / d, a.y / d);
    }
}

public struct float2Bounds
{
    public float2 position;
    public float2 size;
    public float2Bounds(float2 position, float2 size)
    {
        this.position = position;
        this.size = size;
    }
    public void SetMinMax(float2 min, float2 max)
    {
        size = (max - min) * 0.5f;
        position = min + size;
    }
    public float2 min
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return position - size;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            SetMinMax(value, max);
        }
    }
    public float2 max
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return position - size;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            SetMinMax(value, max);
        }
    }
    public void Encapsulate(float2 point)
    {
        SetMinMax(float2.Min(min, point), float2.Max(max, point));
    }

    public void Encapsulate(float2Bounds bounds)
    {
        Encapsulate(bounds.position - bounds.size);
        Encapsulate(bounds.position + bounds.size);
    }
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
                obstacleMapReturn = Task.Run(() => DetectObstaclesInPosition.DetectObstacleMap(new float2(50, 50), obstacles, randomObst, 1)),
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
                    obstacleMapReturn = Task.Run(() => DetectObstaclesInPosition.DetectObstacleMap(new float2(50, 50), obstacles, randomObst, 1))
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

