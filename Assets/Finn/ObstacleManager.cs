using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public List<ObstacleObj> obstaclesInScene = new List<ObstacleObj>();

    public List<Obstacle> GetObstaclesInScene()
    {
        List<Obstacle> obstacles = new List<Obstacle>();
        for (int i = 0; i < obstaclesInScene.Count; ++i)
        {
            obstacles.Add(obstaclesInScene[i].objObstacle);
        }
        return obstacles;
    }
    public Obstacle FindObstacleByInstanceID(int instanceID)
    {
        for (int i = 0; i < obstaclesInScene.Count; i++)
        {
            if (obstaclesInScene[i].objObstacle.instanceID == instanceID)
            {
                return obstaclesInScene[i].objObstacle;
            }
        }
        return null;
    }
}
