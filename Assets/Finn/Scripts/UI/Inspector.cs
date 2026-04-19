using TMPro;
using UnityEngine;

public class Inspector : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text description;
    public Camera previewCam;
    private GameObject tracking;
    private bool hiding = false;
    public float hidingSpeed;
    void Start()
    {

    }

    void Update()
    {
        if (tracking != null)
        {
            previewCam.transform.position = new Vector3(tracking.transform.position.x, tracking.transform.position.y, -50);
        }
        if (hiding && GetComponent<RectTransform>().position.x > -370)
        {
            transform.Translate(Vector2.left * hidingSpeed * Time.unscaledDeltaTime);
        }
        else if (!hiding && GetComponent<RectTransform>().position.x < 290)
        {
            transform.Translate(Vector2.right * hidingSpeed * Time.unscaledDeltaTime);
        }
    }
    public void ToggleVisability()
    {
        hiding = !hiding;

    }

    public void Inspect(Inspectable inspectable)
    {
        title.text = inspectable.title;
        description.text = inspectable.description;
        tracking = inspectable.gameObject;

    }
}
