using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86;
public class SelectTest : MonoBehaviour
{
    public PlayerInput PlayerInput;
    private InputAction mousePosInput;
    private InputAction mouseLeftClick;
    private InputAction mouseRightClick;
    public List<RVOAI> selectableObjs = new List<RVOAI>();
    public List<RVOAI> selectedObjs = new List<RVOAI>();
    public List<GameObject> selectableGameObjs = new List<GameObject>();

    public List<Planet> selectablePlnts = new List<Planet>();
    public List<Planet> selectedPlnts = new List<Planet>();
    public List<GameObject> selectablePlntGmObjs = new List<GameObject>();
    public List<RVOAI> enemies = new List<RVOAI>();
    Rect selectionRect = new Rect();
    private Vector2 selectionStartPos = new Vector2();
    public Texture2D selectionTexture;
    public Color selectionColor = new Color(0.8f, 0.8f, 0.9f, 0.25f);
    public Color selectedColor = new Color(1.0f, 0.0f, 0.0f, 0.6f);
    private Vector2 mouseScreenStartPos = new Vector2();
    private Rect screenSelectionRect = new Rect();
    UILineRenderer lineRenderer;
    Inspector inspector;
    public RVOManager AIManager;
    AlliedManager alliedManager;
    Inspectable currentInspectedObj;
    UIManager uiManager;
    SelectionMode selectionMode = SelectionMode.None;
    Squadron selectedSquad;
    EnemyManager enemyManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        PlayerInput = new PlayerInput();
        mousePosInput = PlayerInput.Main.MousePos;
        mouseLeftClick = PlayerInput.Main.MouseClickLeft;
        mouseRightClick = PlayerInput.Main.MouseClickRight;
    }
    private void Start()
    {
        selectionTexture = new Texture2D(1, 1);
        lineRenderer = FindFirstObjectByType(typeof(UILineRenderer)) as UILineRenderer;
        inspector = FindFirstObjectByType<Inspector>();
        alliedManager = FindFirstObjectByType<AlliedManager>();
        uiManager = FindFirstObjectByType<UIManager>();
        enemyManager = FindFirstObjectByType<EnemyManager>();
        SelectedObjectsDirty();
    }

    private void OnEnable()
    {
        mousePosInput.Enable();
        mouseLeftClick.Enable();
        mouseRightClick.Enable();
    }
    private void OnDisable()
    {
        mousePosInput.Disable();
        mouseLeftClick.Disable();
        mouseRightClick.Disable();
    }

    private void OnGUI()
    {
        GUI.depth = 100;
        if (mouseLeftClick.IsPressed() && selectionTexture != null)
        {
            GUI.color = selectionColor;
            GUI.DrawTexture(screenSelectionRect, selectionTexture);
        }
        for (int i = 0; i < selectedObjs.Count; i++)
        {
            Renderer selectedRend = selectedObjs[i].gameObjectRef.GetComponentInChildren<Renderer>();
            if (selectedRend != null)
            {
                float left = Camera.main.WorldToScreenPoint(selectedRend.bounds.center - new Vector3(selectedRend.bounds.extents.x, 0, 0)).x;
                float right = Camera.main.WorldToScreenPoint(selectedRend.bounds.center + new Vector3(selectedRend.bounds.extents.x, 0, 0)).x;
                float bottomYUp = Camera.main.WorldToScreenPoint(selectedRend.bounds.center - new Vector3(0, selectedRend.bounds.extents.y, 0)).y;
                float topYUp = Camera.main.WorldToScreenPoint(selectedRend.bounds.center + new Vector3(0, selectedRend.bounds.extents.y, 0)).y;

                float x = left;
                float y = Screen.height - topYUp;
                float width = right - left;
                float height = topYUp - bottomYUp;
                Rect selectedObjScreenSpace = new Rect(x, y, width, height);
                GUI.color = selectedColor;
                GUI.DrawTexture(selectedObjScreenSpace, selectionTexture);

            }
            else
            {
                Debug.LogWarning($"Object {selectedObjs[i].gameObjectRef.name} does not have a renderer, cannot get bounds of selected object");
                continue;
            }
        }
        GUI.color = Color.white;
    }
    public void CreateSquad(string name)
    {
        List<int> AIsToAddToSquad = new List<int>();
        for (int i = 0; i < selectedObjs.Count; i++)
        {
            AIsToAddToSquad.Add(AIManager.AIs.IndexOf(selectedObjs[i]));
        }
        alliedManager.CreateSquadron(AIsToAddToSquad, name);
        SelectedObjectsDirty();
    }
    public void DeleteSquadSingle()
    {
        int ai = AIManager.AIs.FindIndex(x => x.gameObjectRef == currentInspectedObj.gameObject);
        if (ai != -1)
        {
            alliedManager.RemoveFromSquadron(ai, AIManager.AIs[ai].squadron);
        }
    }
    public void DeleteSquad()
    {
        alliedManager.DestroySquadron(selectedSquad);

        SelectedObjectsDirty();
    }
    public void DeleteAndCreateNewSquadron()
    {

        DeleteSquad();
        CreateSquad(RandNames.RandomGreekLetter());
        SelectedObjectsDirty();
    }
    public void RemoveFromSquad()
    {
        int idx = AIManager.AIs.FindIndex(x => x.gameObjectRef == currentInspectedObj.gameObject);
        if (idx != -1)
        {


            alliedManager.RemoveFromSquadron(idx, AIManager.AIs[idx].squadron);
        }
    }
    public void ChangeSquadronFormation()
    {

        if ((int)selectedSquad.formation < System.Enum.GetNames(typeof(Formation)).Length - 1)
        {
            alliedManager.squadrons.Find(x => x == selectedSquad).formation = alliedManager.squadrons.Find(x => x == selectedSquad).formation + 1;
        }
        else
        {
            alliedManager.squadrons.Find(x => x == selectedSquad).formation = (Formation)System.Enum.GetValues(typeof(Formation)).GetValue(0);
        }
        SelectedObjectsDirty();
    }
    public void SelectedObjectsDirty()
    {
        selectedSquad = null;
        if (inspector.tracking == null || inspector.tracking.GetComponent<Inspectable>().type != InspectableTypes.SelectedGroup)
        {
            inspector.tracking = Instantiate(new GameObject("Selection Group Follower"));
        }
        Inspectable inspectable = inspector.tracking.GetComponent<Inspectable>();
        if (inspectable == null)
        {
            inspectable = inspector.tracking.AddComponent<Inspectable>();
        }
        inspectable.title = "Selected Group";
        inspectable.description = "A group of currently selected units";
        inspectable.type = InspectableTypes.SelectedGroup;
        if (selectedObjs.Count == 0)
        {
            inspector.HideInspector();
            inspector.showButton.SetActive(false);
            inspector.tracking = null;
            return;
        }
        Squadron currentSquadron = AIManager.AIs.Find(x => x == selectedObjs[0]).squadron;
        bool anySquad = false;
        for (int i = 1; i < selectedObjs.Count; i++)
        {
            if (currentSquadron != null)
            {
                anySquad = true;
                if (AIManager.AIs.Find(x => x == selectedObjs[i]).squadron != currentSquadron)
                {
                    currentSquadron = null;
                }
            }
            else
            {

                currentSquadron = AIManager.AIs.Find(x => x == selectedObjs[i]).squadron;
                if (currentSquadron != null)
                {
                    anySquad = true;
                }
            }
        }
        List<DynamicButton> buttons = new List<DynamicButton>();
        if (!anySquad)
        {
            buttons.Add(new DynamicButton
            {
                text = "Create Squadron",
                function = uiManager.RequestInput
            });
        }
        else if (currentSquadron == null)
        {
            buttons.Add(new DynamicButton
            {
                text = "Remove & Create new squadron",
                function = DeleteAndCreateNewSquadron
            });
        }
        if (currentSquadron != null)
        {
            inspectable.title = "Squadron " + currentSquadron.name;
            inspectable.description = "Allied squadron " + currentSquadron.name;
            selectedSquad = currentSquadron;
            buttons.Add(new DynamicButton
            {
                text = "Delete Squadron",
                function = DeleteSquad
            });
            buttons.Add(new DynamicButton
            {
                text = "Formation: " + currentSquadron.formation,
                function = ChangeSquadronFormation
            });
        }

        uiManager.UpdateButtonLayout(buttons);
        inspector.InspectWithoutCam(inspectable);
    }
    public void UpdateEnemies()
    {
        enemies.Clear();
        for (int i = 0; i < enemyManager.allEnemies.Count; i++)
        {
            enemies.Add(AIManager.AIs[enemyManager.allEnemies[i]]);
        }
    }
    public void UpdateAllies()
    {
        selectableObjs.Clear();
        selectableGameObjs.Clear();
        for (int i = 0; i < alliedManager.allAllied.Count; i++)
        {
            selectableObjs.Add(AIManager.AIs[alliedManager.allAllied[i]]);
            selectableGameObjs.Add(AIManager.AIs[alliedManager.allAllied[i]].gameObjectRef);
        }
    }
    // Update is called once per frame
    void Update()
    {
        bool mouseOverUI = EventSystem.current.IsPointerOverGameObject();

        UpdateAllies();
        UpdateEnemies();
        lineRenderer.ClearLines();
        Vector2 mouseScreenPos = mousePosInput.ReadValue<Vector2>();
        Vector3 screenPointWithDepth = new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(Camera.main.gameObject.transform.position.z));
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(screenPointWithDepth);
        RaycastHit hitHover;
        bool hoverTextActive = false;
        if (Physics.Raycast(new Vector3(mouseWorldPos.x, mouseWorldPos.y, -50), Vector3.forward, out hitHover) && !mouseOverUI)
        {

            Inspectable inspectableObj;
            Inspectable inspectableObjParent = hitHover.collider.gameObject.GetComponentInParent<Inspectable>();
            Inspectable inspectableObjChild = hitHover.collider.gameObject.GetComponentInChildren<Inspectable>();
            if (hitHover.collider.gameObject.TryGetComponent<Inspectable>(out inspectableObj))
            {
                inspector.ShowHoverText(inspectableObj, mouseScreenPos);
                hoverTextActive = true;
            }
            else if (inspectableObjParent)
            {
                inspector.ShowHoverText(inspectableObjParent, mouseScreenPos);
                hoverTextActive = true;
            }
            else if (inspectableObjChild)
            {
                inspector.ShowHoverText(inspectableObjChild, mouseScreenPos);
                hoverTextActive = true;
            }

        }
        if (!hoverTextActive)
        {
            inspector.HideHoverText();
        }
        if (selectionMode == SelectionMode.Multi && selectedObjs.Count > 0)
        {
            Vector2 avg = Vector2.zero;
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < selectedObjs.Count; i++)
            {
                sum += selectedObjs[i].pos;
            }
            avg = sum / selectedObjs.Count;


            if (inspector.tracking != null)
            {
                inspector.tracking.transform.position = avg;
            }
        }
        if (mouseLeftClick.WasPressedThisFrame() && !mouseOverUI)
        {

            RaycastHit hit;

            if (Physics.Raycast(new Vector3(mouseWorldPos.x, mouseWorldPos.y, -50), Vector3.forward, out hit))
            {
                uiManager.HideInspectorDropdown();
                Inspectable inspectableObj;
                Inspectable inspectableObjParent = hit.collider.gameObject.GetComponentInParent<Inspectable>();
                Inspectable inspectableObjChild = hit.collider.gameObject.GetComponentInChildren<Inspectable>();
                if (hit.collider.gameObject.TryGetComponent<Inspectable>(out inspectableObj) || inspectableObjParent || inspectableObjChild)
                {
                    if (selectionMode == SelectionMode.Multi)
                    {
                        Destroy(inspector.tracking);
                    }
                }
                currentInspectedObj = null;
                if (hit.collider.gameObject.TryGetComponent<Inspectable>(out inspectableObj))
                {
                    inspector.Inspect(inspectableObj);
                    currentInspectedObj = inspectableObj;
                    selectionMode = SelectionMode.Single;
                    inspector.ShowInspector();
                }
                else if (inspectableObjParent)
                {
                    inspector.Inspect(inspectableObjParent);
                    currentInspectedObj = inspectableObjParent;
                    selectionMode = SelectionMode.Single;
                    inspector.ShowInspector();
                }
                else if (inspectableObjChild)
                {
                    inspector.Inspect(inspectableObjChild);
                    currentInspectedObj = inspectableObjChild;
                    selectionMode = SelectionMode.Single;
                    inspector.ShowInspector();
                }
                if (currentInspectedObj != null)
                {


                    if (currentInspectedObj.type == InspectableTypes.Ally)
                    {
                        //Debug.Log("selected " + currentInspectedObj.name + " in squadron " + AIManager.AIs.Find(x => x.gameObjectRef == currentInspectedObj.gameObject).squadron.name);
                        List<DynamicButton> buttons = new List<DynamicButton>();

                        if (AIManager.AIs.Find(x => x.gameObjectRef == currentInspectedObj.gameObject).squadron != null)
                        {
                            buttons.Add(new DynamicButton
                            {
                                function = RemoveFromSquad,
                                text = "Remove from " + AIManager.AIs.Find(x => x.gameObjectRef == currentInspectedObj.gameObject).squadron.name
                            });
                            uiManager.UpdateButtonLayout(buttons);
                        }
                    }
                    else if (currentInspectedObj.type == InspectableTypes.Planet)
                    {
                        List<string> resources = new List<string>();
                        resources.Add("Resources");
                        for (int i = 0; i < currentInspectedObj.gameObject.GetComponent<Planet>().planetResources.Count; i++)
                        {
                            resources.Add(StringUtils.Nicify(currentInspectedObj.gameObject.GetComponent<Planet>().planetResources[i].type.ToString()).ToLower() + " : " + currentInspectedObj.gameObject.GetComponent<Planet>().planetResources[i].amount);
                        }
                        uiManager.UpdateInspectorDropdown(resources);
                    }
                }
            }
            else
            {
                selectionStartPos = mouseWorldPos;
                mouseScreenStartPos = mouseScreenPos;
                selectionMode = SelectionMode.Multi;
                uiManager.HideInspectorDropdown();
                selectedObjs.Clear();
                selectedSquad = null;
                inspector.tracking = null;
                inspector.HideInspector();
                inspector.showButton.SetActive(false);
                uiManager.ClearButtonLayout();
            }
        }
        else if (mouseLeftClick.WasReleasedThisFrame() && selectionMode == SelectionMode.Multi)
        {
            selectionRect = new Rect();
            selectionStartPos = new Vector2();
            screenSelectionRect = new Rect();
        }
        if (mouseRightClick.IsPressed() && selectionMode == SelectionMode.Multi && !mouseOverUI)
        {
            selectionRect = new Rect();
            selectionStartPos = new Vector2();
            screenSelectionRect = new Rect();
            GameObject parentedPlanet;
            for (int i = 0; i < selectablePlntGmObjs.Count; i++)
            {
                if (Vector2.Distance(mouseWorldPos, selectablePlntGmObjs[i].transform.position) < selectablePlntGmObjs[i].GetComponent<SphereCollider>().radius + 20)
                {
                    parentedPlanet = selectablePlntGmObjs[i];
                    break;
                }
            }
            RVOAI attacking = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (Vector2.Distance(mouseWorldPos, enemies[i].pos) < 2)
                {
                    attacking = enemies[i];
                    break;
                }
            }
            if (attacking != null)
            {
                if (selectedSquad != null)
                {
                    alliedManager.AttackSquadron(selectedSquad, attacking);
                }
                else
                {
                    for (int i = 0; i < selectedObjs.Count; i++)
                    {
                        AIManager.AttackAI(selectedObjs[i], attacking, true);
                    }
                }
            }
            else
            {
                if (selectedSquad != null)
                {
                    alliedManager.SendSquadron(selectedSquad, mouseWorldPos);
                }
                else
                {
                    for (int i = 0; i < selectedObjs.Count; i++)
                    {
                        AIManager.SendAI(selectedObjs[i], mouseWorldPos, selectedObjs.Count * 0.6f);
                    }
                }
            }


        }
        if (mouseLeftClick.IsPressed() && !mouseOverUI)
        {
            if (Vector2.Distance(selectionStartPos, mouseWorldPos) > 1 && selectionMode == SelectionMode.Multi)
            {
                Vector2 sizeWorld = mouseWorldPos - selectionStartPos;
                selectionRect = new Rect(selectionStartPos, sizeWorld);
                Vector2 sizeScreen = mouseScreenPos - new Vector2(Camera.main.WorldToScreenPoint(selectionStartPos).x, Camera.main.WorldToScreenPoint(selectionStartPos).y);
                screenSelectionRect = new Rect(
                    Camera.main.WorldToScreenPoint(selectionStartPos).x,
                    Screen.height - Camera.main.WorldToScreenPoint(selectionStartPos).y,
                    sizeScreen.x,
                    -sizeScreen.y
                );
                for (int i = 0; i < selectableObjs.Count; i++)
                {
                    if (!selectedObjs.Contains(selectableObjs[i]) && selectionRect.Contains((Vector2)selectableObjs[i].gameObjectRef.transform.position, true))
                    {
                        selectedObjs.Add(selectableObjs[i]);
                        SelectedObjectsDirty();

                    }
                    else if (selectedObjs.Contains(selectableObjs[i]) && !selectionRect.Contains((Vector2)selectableObjs[i].gameObjectRef.transform.position, true))
                    {
                        selectedObjs.Remove(selectableObjs[i]);
                        SelectedObjectsDirty();
                    }
                }
            }

        }
        for (int i = 0; i < selectedObjs.Count; i++)
        {
            if (selectedObjs[i].targetSet)
            {
                if (selectedObjs[i].followTarget != null)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(selectedObjs[i].gameObjectRef.transform.position);

                    RectTransform rectTransform = lineRenderer.GetComponent<RectTransform>();
                    Vector2 localPos;

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        rectTransform,
                        screenPos,
                        null,
                        out localPos
                    );

                    Vector3 targetScreenPos = Camera.main.WorldToScreenPoint(selectedObjs[i].followTarget.gameObjectRef.transform.position);

                    Vector2 targetLocalPos;

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        rectTransform,
                        targetScreenPos,
                        null,
                        out targetLocalPos
                    );


                    lineRenderer.DrawLine(new UILineRenderer.LineSegment
                    {
                        Color = Color.red,
                        P1 = localPos,
                        P2 = targetLocalPos,
                        Width = 1f
                    });
                }
                else
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(selectedObjs[i].gameObjectRef.transform.position);

                    RectTransform rectTransform = lineRenderer.GetComponent<RectTransform>();
                    Vector2 localPos;

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        rectTransform,
                        screenPos,
                        null,
                        out localPos
                    );

                    Vector3 targetScreenPos = Camera.main.WorldToScreenPoint(selectedObjs[i].visualTarget);

                    Vector2 targetLocalPos;

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        rectTransform,
                        targetScreenPos,
                        null,
                        out targetLocalPos
                    );
                    lineRenderer.DrawLine(new UILineRenderer.LineSegment
                    {
                        Color = Color.green,
                        P1 = localPos,
                        P2 = targetLocalPos,
                        Width = 1f
                    });
                }
            }
        }

    }
}


