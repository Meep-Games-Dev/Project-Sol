using Unity.VisualScripting;
using UnityEngine;

public class ObstacleObj : MonoBehaviour
{

    public Obstacle objObstacle = new Obstacle();
    ObstacleManager obstacleManager;
    public void Start()
    {
        objObstacle = DetectObstaclesInPosition.SetupObstacle(gameObject);
        if (objObstacle == null)
        {
            Debug.LogWarning($"GameObject {gameObject.name} does not have a collider, so there is no reason for it to be an obstacle. Deleting ObstacleObj script from GameObject {gameObject.name}");
            Destroy(this);
        }

        obstacleManager = FindFirstObjectByType(typeof(ObstacleManager)).GetComponent<ObstacleManager>();
        if (obstacleManager == null)
        {
            Debug.LogWarning("No obstacleManager detected in scene, please add one!");
        }
        obstacleManager.obstaclesInScene.Add(this);
    }
    public void LateUpdate()
    {
        if (Vector2.Distance(objObstacle.position, transform.position) < 0.001f)
        {
            objObstacle.position = transform.position;
        }
        if (objObstacle.size != new Vector2(GetComponent<Collider2D>().bounds.size.x, GetComponent<Collider2D>().bounds.size.y))
        {
            objObstacle.size = GetComponent<Collider2D>().bounds.size;
        }
    }
    public void OnDestroy()
    {
        obstacleManager.obstaclesInScene.Remove(this);
    }
}
