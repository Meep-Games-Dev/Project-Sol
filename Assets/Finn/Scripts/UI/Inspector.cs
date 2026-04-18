using TMPro;
using UnityEngine;

public class Inspector : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text description;
    public Camera previewCam;
    private GameObject tracking;
    void Start()
    {

    }

    void Update()
    {
        if (tracking != null)
        {
            previewCam.transform.position = new Vector3(tracking.transform.position.x, tracking.transform.position.y, -50);
        }
    }

    public void Inspect(Inspectable inspectable)
    {
        title.text = inspectable.title;
        description.text = inspectable.description;
        tracking = inspectable.gameObject;

    }
}
