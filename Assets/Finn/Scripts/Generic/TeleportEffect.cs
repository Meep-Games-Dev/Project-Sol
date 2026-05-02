
using UnityEngine;

public class TeleportEffect : MonoBehaviour
{
    public float timeToMax;
    public float startTime;
    public float startSize;
    private void Start()
    {
        startTime = Time.time;
        startSize = Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }
    // Update is called once per frame
    void Update()
    {
        float scale = Time.time / timeToMax;
        if (Time.time - startTime > timeToMax * 2)
        {
            Destroy(gameObject);
        }
        if (Time.time - startTime > timeToMax)
        {
            scale = (startTime - (Time.time - startTime)) / timeToMax;
        }
        transform.localScale = new Vector3(scale * startSize, scale * startSize, scale * startSize);
    }
}
