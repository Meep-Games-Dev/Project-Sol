using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject planetDialoguePrefab;
    public Transform canvas;
    public UILineRenderer lineRenderer;
    public List<GameObject> currentDialogues = new List<GameObject>();
    public List<int> lineTokens = new List<int>();
    public AlliedManager alliedManager;
    public GameObject buttonPrefab;
    public GameObject buttonGroup;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        alliedManager = FindFirstObjectByType<AlliedManager>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void UpdateButton(int id, DynamicButton newButton)
    {
        GameObject button = buttonGroup.transform.GetChild(id).gameObject;
        Button buttonComp = button.GetComponent<Button>();
        buttonComp.onClick.RemoveAllListeners();
        buttonComp.onClick.AddListener(() => newButton.function());
        TMP_Text text = button.GetComponent<TMP_Text>();
        text.text = newButton.text;
    }
    public void UpdateButtonLayout(List<DynamicButton> buttons)
    {
        ClearButtonLayout();
        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i;
            GameObject instantiatedButton = Instantiate(buttonPrefab, buttonGroup.transform);
            Button button = instantiatedButton.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => buttons[index].function());
            TMP_Text text = instantiatedButton.GetComponentInChildren<TMP_Text>();
            text.text = buttons[i].text;
        }
    }
    public void ClearButtonLayout()
    {
        int children = buttonGroup.transform.childCount;
        for (int i = children - 1; i > 0; i--)
        {
            Debug.Log("Destroyed Gameobject" + buttonGroup.transform.GetChild(i).gameObject.name);
            Destroy(buttonGroup.transform.GetChild(i).gameObject);
        }
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
