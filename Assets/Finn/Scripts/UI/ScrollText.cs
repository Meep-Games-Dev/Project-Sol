using TMPro;
using UnityEngine;

public class ScrollText : MonoBehaviour
{

    private TMP_Text text;
    private string startingString;
    private float elapsedTime;
    private float startTime;
    [SerializeField]
    private float speed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void OnEnable()
    {
        text = GetComponent<TMP_Text>();
        startTime = Time.time;
        startingString = text.text;
        text.text = "";
    }
    // Update is called once per frame
    void Update()
    {
        if (startingString.Length > 0 || startingString == text.text)
        {
            elapsedTime = Time.time - startTime;
            int charCount = Mathf.FloorToInt(elapsedTime / speed);
            int totalLength = startingString.Length;

            if (charCount < totalLength)
            {
                Debug.Log(charCount + " " + startingString);
                text.text = startingString.Substring(0, charCount) + "|";
            }
            else
            {
                text.text = startingString;
            }
        }
    }
}
