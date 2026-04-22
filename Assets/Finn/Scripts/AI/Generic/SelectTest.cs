using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
    // Update is called once per frame
    void Update()
    {
        selectableObjs = AIManager.AIs;
        lineRenderer.ClearLines();
        Vector2 mouseScreenPos = mousePosInput.ReadValue<Vector2>();
        Vector3 screenPointWithDepth = new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(Camera.main.gameObject.transform.position.z));
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(screenPointWithDepth);
        if (mouseLeftClick.WasPressedThisFrame())
        {

            RaycastHit hit;
            if (Physics.Raycast(new Vector3(mouseWorldPos.x, mouseWorldPos.y, -50), Vector3.forward, out hit))
            {
                Inspectable inspectableObj;
                if (hit.collider.gameObject.TryGetComponent<Inspectable>(out inspectableObj))
                {
                    inspector.Inspect(inspectableObj);
                    inspector.ShowInspector();
                }

            }
            else
            {
                selectionStartPos = mouseWorldPos;
                mouseScreenStartPos = mouseScreenPos;
                selectedObjs.Clear();
            }




        }
        else if (mouseLeftClick.WasReleasedThisFrame())
        {
            selectionRect = new Rect();
            selectionStartPos = new Vector2();
            screenSelectionRect = new Rect();
        }
        if (mouseRightClick.IsPressed())
        {
            selectionRect = new Rect();
            selectionStartPos = new Vector2();
            screenSelectionRect = new Rect();
            //if (enemies.FindIndex(x => Vector2.Distance(x.obj.transform.position, mouseWorldPos) < 2) != -1)
            //{
            //    AIManager.SendMultipleAI(selectedObjs, mouseWorldPos, true);
            //}
            //else
            //{
            //    AIManager.SendMultipleAI(selectedObjs, mouseWorldPos, false);
            //}
            //for (int i = 0; i < selectedObjs.Count; i++)
            //{
            //    AIManager.SendAI(selectedObjs[i], mouseWorldPos);
            //}
            for (int i = 0; i < selectedObjs.Count; i++)
            {
                AIManager.SendAI(selectedObjs[i], mouseWorldPos, selectedObjs.Count * 0.6f);
            }
        }
        if (mouseLeftClick.IsPressed())
        {
            if (Vector2.Distance(selectionStartPos, mouseWorldPos) > 1)
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
                    }
                    else if (selectedObjs.Contains(selectableObjs[i]) && !selectionRect.Contains((Vector2)selectableObjs[i].gameObjectRef.transform.position, true))
                    {
                        selectedObjs.Remove(selectableObjs[i]);
                    }
                }
            }

        }
        for (int i = 0; i < selectedObjs.Count; i++)
        {
            if (selectedObjs[i].targetSet)
            {
                if (selectedObjs[i].enemyTarget != null)
                {
                    lineRenderer.DrawLine(new UILineRenderer.LineSegment
                    {
                        Color = Color.red,
                        P1 = Camera.main.WorldToScreenPoint(selectedObjs[i].gameObjectRef.transform.position),
                        P2 = Camera.main.WorldToScreenPoint(selectedObjs[i].target),
                        Width = 5f
                    });
                }
                else
                {
                    lineRenderer.DrawLine(new UILineRenderer.LineSegment
                    {
                        Color = Color.green,
                        P1 = Camera.main.WorldToScreenPoint(selectedObjs[i].gameObjectRef.transform.position),
                        P2 = Camera.main.WorldToScreenPoint(selectedObjs[i].target),
                        Width = 5f
                    });
                }
            }
        }

    }
}
