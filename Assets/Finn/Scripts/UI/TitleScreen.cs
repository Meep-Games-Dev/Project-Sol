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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
}
