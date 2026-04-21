using TMPro;
using UnityEngine;

public class Inspector : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text description;
    public Camera previewCam;
    private GameObject tracking;
    private bool hiding = true;
    public float hidingSpeed;
    public GameObject showButton;
    void Start()
    {

        GetComponent<RectTransform>().position = new Vector2(-350, GetComponent<RectTransform>().position.y);
    }

    void Update()
    {
        if (tracking != null)
        {
            previewCam.transform.position = new Vector3(tracking.transform.position.x, tracking.transform.position.y, -50);
            showButton.SetActive(hiding);
        }
        else
        {
            HideInspector();
            showButton.SetActive(false);
        }
        if (hiding && GetComponent<RectTransform>().position.x > -350)
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
    public void HideInspector()
    {
        hiding = true;
    }
    public void ShowInspector()
    {
        hiding = false;
    }

    public void Inspect(Inspectable inspectable)
    {
        title.text = inspectable.title;
        description.text = inspectable.description;
        tracking = inspectable.gameObject;

    }
}
