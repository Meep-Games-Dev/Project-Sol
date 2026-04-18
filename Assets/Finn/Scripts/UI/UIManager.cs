using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject planetDialoguePrefab;
    public Transform canvas;
    public UILineRenderer lineRenderer;
    public List<GameObject> currentDialogues = new List<GameObject>();
    public List<int> lineTokens = new List<int>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public int DisplayPlanetDialogue(Vector2 screenPos, Planet planet)
    {
        Vector2 dialoguePos = new Vector2(screenPos.x + 10, screenPos.y + 10);
        float dialoguePosX = dialoguePos.x;
        float dialoguePosY = dialoguePos.y;

        dialoguePosX = Mathf.Min(dialoguePosX, Screen.width);
        dialoguePosY = Mathf.Min(dialoguePosY, Screen.height);

        dialoguePos = new Vector2(dialoguePosX, dialoguePosY);
        GameObject instantiatedObj = Instantiate(planetDialoguePrefab, dialoguePos, Quaternion.identity);
        instantiatedObj.transform.SetParent(canvas, false);
        TMP_Text titleText = instantiatedObj.transform.Find("Planet Name").gameObject.GetComponent<TMP_Text>();
        TMP_Text typeText = instantiatedObj.transform.Find("Planet Type").gameObject.GetComponent<TMP_Text>();
        TMP_Text descriptionText = instantiatedObj.transform.Find("Planet Description").gameObject.GetComponent<TMP_Text>();

        titleText.text = planet.planetName;
        typeText.text = planet.readablePlanetType;
        descriptionText.text = planet.planetDescription;


        currentDialogues.Add(instantiatedObj);
        InfoUI UIData = instantiatedObj.GetComponent<InfoUI>();
        UIData.idx = currentDialogues.Count - 1;

        return currentDialogues.Count - 1;
    }
    //Need to add algorithim to update all other UI indexes 
    public void KillDialogue(int idx)
    {
        for (int i = idx + 1; i < currentDialogues.Count; i++)
        {
            currentDialogues[i].GetComponent<InfoUI>().idx = i - 1;
            Debug.Log("Moved UI idx down");
        }
        Debug.Log($"Removing UI {idx}/{currentDialogues.Count}");
        GameObject dialogue = currentDialogues[idx];

        currentDialogues.RemoveAt(idx);
        //lineRenderer.ClearPersistantLine(lineTokens[idx]);
        //lineTokens.RemoveAt(idx);
        Destroy(dialogue);
    }
}
