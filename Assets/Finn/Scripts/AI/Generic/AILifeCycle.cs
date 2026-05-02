using System;
using UnityEngine;

public class AILifeCycle : MonoBehaviour
{
    public RVOManager RVOManager;
    public GameObject teleportPrefab;
    public GameObject explosionPrefab;
    public Guid AI;
    private void Start()
    {
        //Instantiate(teleportPrefab, this.transform.position, this.transform.rotation);
    }
    private void OnDestroy()
    {
        if (gameObject.scene.isLoaded)
        {
            Instantiate(explosionPrefab, this.transform.position, this.transform.rotation);
            RVOManager.RemoveAI(AI);
        }
    }
}
