using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    public float time;
    public float startTime;

    public void Start()
    {
        startTime = Time.time;
    }
    // Update is called once per frame
    void Update()
    {
        if (Time.time - startTime > time)
        {
            Destroy(gameObject);
        }
    }
}
