using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    public List<TMP_Text> text;
    public List<string> startingText = new List<string>();
    public float speed;
    public float offset;
    public float startTime;
    public float elapsedTime;
    public GameObject planet;
    public float planetRotationalSpeed;
    public Vector3 planetRotationalVector;
    public GameObject sun;
    public float sunRotationalSpeed;
    public Vector3 sunRotationalVector;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Time.timeScale = 1;
        startTime = Time.time;

        for (int i = 0; i < text.Count; i++)
        {
            startingText.Add(text[i].text);
            text[i].text = "";
        }
    }

    // Update is called once per frame
    void Update()
    {
        elapsedTime = Time.time - startTime;

        elapsedTime = Time.time - startTime;

        planet.transform.Rotate(planetRotationalVector * planetRotationalSpeed * Time.deltaTime);
        sun.transform.Rotate(sunRotationalVector * sunRotationalSpeed * Time.deltaTime);

        for (int i = 0; i < text.Count; i++)
        {

            float localTime = elapsedTime - (offset * i);


            if (localTime > 0)
            {
                int charCount = Mathf.FloorToInt(localTime / speed);
                int totalLength = startingText[i].Length;

                if (charCount < totalLength)
                {
                    text[i].text = startingText[i].Substring(0, charCount) + "|";
                }
                else
                {
                    text[i].text = startingText[i];
                }
            }
        }
    }
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
