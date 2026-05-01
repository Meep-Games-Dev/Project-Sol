using UnityEngine;

public class LargeBounds : MonoBehaviour
{
    public Vector3 size;
    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            meshFilter.mesh.bounds = new Bounds(Vector3.zero, size);
        }
    }
}
