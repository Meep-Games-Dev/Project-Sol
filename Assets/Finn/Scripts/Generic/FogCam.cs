using UnityEngine;

public class FogCam : MonoBehaviour
{
    void Start()
    {
        Camera cam = GetComponent<Camera>();
        Debug.Log($"Fog cam aspect: {cam.aspect}, orthographic size: {cam.orthographicSize}");
        Debug.Log($"World width: {cam.orthographicSize * cam.aspect * 2}, World height: {cam.orthographicSize * 2}");
    }
}
