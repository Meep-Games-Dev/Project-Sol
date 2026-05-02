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
    public TMP_Dropdown squadronDropdown;
    public TMP_Dropdown resourceDropDown;
    public SelectTest selector;
    public RVOManager AIManager;
    public TMP_Dropdown inspectorDropdown;
    public GameObject nameField;
    public TMP_InputField nameFieldInput;
    System.Random rnd = new System.Random();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        alliedManager = FindFirstObjectByType<AlliedManager>();
        selector = FindFirstObjectByType<SelectTest>();
        AIManager = FindFirstObjectByType<RVOManager>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void RequestInput()
    {
        nameField.SetActive(true);
        nameFieldInput.text = RandUtils.RandomGreekLetter();
    }
    public void FinishInput()
    {
        nameField.SetActive(false);
        selector.CreateSquad(nameFieldInput.text);
        nameFieldInput.text = "";
    }
    public void UpdateResourceDropdown(List<Resource> resources)
    {
        resourceDropDown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData
        {
            text = "Resources"
        });
        for (int i = 0; i < resources.Count; i++)
        {
            options.Add(new TMP_Dropdown.OptionData
            {
                text = StringUtils.Nicify(resources[i].type.ToString()).ToLower() + " : " + resources[i].amount,
            });
        }
        resourceDropDown.AddOptions(options);
    }
    public void OnResourceDropdownChange()
    {
        resourceDropDown.value = 0;
    }
    public void UpdateSquadronDropdown(List<Squadron> squadrons)
    {
        squadronDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData
        {
            text = "Squadrons"
        });
        options.Add(new TMP_Dropdown.OptionData
        {
            text = "Select All"
        });
        for (int i = 0; i < squadrons.Count; i++)
        {
            options.Add(new TMP_Dropdown.OptionData
            {
                text = squadrons[i].name + " : " + (squadrons[i].AIidx.Count + 1) + " units",
            });
        }
        squadronDropdown.AddOptions(options);
    }
    public void OnSquadronDropdownChange()
    {
        int selectedVal = squadronDropdown.value;
        if (selectedVal == 0) return;
        if (selectedVal == 1)
        {
            selector.selectedObjs.Clear();
            for (int i = 0; i < selector.selectableObjs.Count; i++)
            {
                selector.selectedObjs.Add(selector.selectableObjs[i]);
            }
            selector.SelectedObjectsDirty();
            squadronDropdown.value = 0;
            return;
        }

        Squadron selectedSquadron = alliedManager.squadrons[selectedVal - 1];
        selector.selectedObjs.Clear();
        selector.selectedObjs.Add(selectedSquadron.leadAI);
        for (int i = 0; i < selectedSquadron.AIidx.Count; i++)
        {
            selector.selectedObjs.Add(selectedSquadron.AIidx[i]);
        }
        selector.SelectedObjectsDirty();
        squadronDropdown.value = 0;
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
    public void HideInspectorDropdown()
    {
        inspectorDropdown.gameObject.SetActive(false);
        inspectorDropdown.ClearOptions();
    }
    public void UpdateInspectorDropdown(List<string> items)
    {
        inspectorDropdown.gameObject.SetActive(true);
        inspectorDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < items.Count; i++)
        {
            options.Add(new TMP_Dropdown.OptionData
            {
                text = items[i],
            });
        }
        inspectorDropdown.AddOptions(options);
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
        for (int i = children - 1; i >= 0; i--)
        {
            //Debug.Log("Destroyed Gameobject" + buttonGroup.transform.GetChild(i).gameObject.name);
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
