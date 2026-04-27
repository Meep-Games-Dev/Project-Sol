//using System.Numerics;
using JetBrains.Annotations;
using Station;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
//using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;

/*
public class RayController : MonoBehaviour
{
    public InputSystem_Actions input;
    public InputAction mousePosition;
    public InputAction leftMouse;
    public InputAction Rotate;
    public InputAction rightMouse;
    public InputAction kill;
    public Draw drawer;
    [SerializeField] private GameObject myPrefab;
    [SerializeField] private GameObject prefaab;
    public Dictionary<GameObject, (Vector3, Vector3)> RaysToDraw = new Dictionary<GameObject, (Vector3, Vector3)>();
    float OX = 0f;
    float OY = 0f;
    int clones = 0;
    public List<GameObject> ATpiece = new List<GameObject>();

    public void OnEnable()
    {
        mousePosition.Enable();
        leftMouse.Enable();
        Rotate.Enable();
        rightMouse.Enable();
        kill.Enable();
    }
    public void OnDisable()
    {
        mousePosition.Disable();
        leftMouse.Disable();
        Rotate.Disable();  
        rightMouse.Disable();
        kill.Disable();
    }
        void Awake()
    {
        drawer = FindFirstObjectByType<Draw>();
        input = new InputSystem_Actions();
        mousePosition = input.Player.MousePos;
        leftMouse = input.Player.MouseDown;
        Rotate = input.Player.Rotate;
        rightMouse = input.Player.MouseRight;
        kill = input.Player.Kill;

    }


    private bool isShowingLabel = false;

        private GameObject attachedObject = null; // Track the currently attached object

    void Update()
    {
        Vector2 mouseScreenPos = mousePosition.ReadValue<Vector2>();
        Vector3 screenPointWithDepth = new Vector3(mouseScreenPos.x, mouseScreenPos.y,
            Mathf.Abs(Camera.main.transform.position.z));
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(screenPointWithDepth);
        Debug.Log(mouseWorldPos);

        // Draw ray continuously while something is attached
        if (attachedObject != null)
        {
            Vector2 start = new Vector2(attachedObject.transform.position.x, attachedObject.transform.position.y);
            Vector2 finish = Vector2.zero;
            //Vector2 direction = finish - start;
            RaysToDraw[attachedObject] = (start, finish);
            attachedObject.SendMessage("show", SendMessageOptions.DontRequireReceiver); // make it so the sprite displays the piece name 
            isShowingLabel = true;
            // Move attached object with mouse
            //attachedObject.transform.position = (Vector2)mouseWorldPos;
            /*
            if(attachedObject.transform.root == null)
            {
                Instantiate(attachedObject, mouseWorldPos, Quaternion.identity); // if the piece is a parent, make a child and move that
                attachedObject.GetComponentInChildren<Transform>().position = mouseWorldPos;
            }
            else
            {
                attachedObject.transform.position = (Vector3)mouseWorldPos;
            }
            

            attachedObject.transform.position = (Vector2)mouseWorldPos;

            // Rotate attached object
            if (Rotate.WasPressedThisFrame())
            {
                attachedObject.transform.Rotate(0, 0, 90);
                Debug.Log("Rotated piece: " + attachedObject.name);
            }
            if(kill.WasPressedThisFrame())
            {
                Destroy(attachedObject);
                attachedObject = null;
                isShowingLabel = false;
            }
        }

        // On click: attach or detach
        if (leftMouse.WasPressedThisFrame())
        {
            if (attachedObject != null)
            {
                // Second click — drop the object
                attachedObject.SendMessage("hide", SendMessageOptions.DontRequireReceiver); // hide the piece name when dropped
                isShowingLabel = false;
                SpaceStation.ATpiece.Add(attachedObject); 
                attachedObject = null;

            }
            else
            {
                // First click — try to pick up an object
                RaycastHit hit;
                if (Physics.Raycast(new Vector3(mouseWorldPos.x, mouseWorldPos.y, -50), Vector3.forward, out hit))
                {
                    
                    attachedObject = hit.collider.gameObject;
                    Debug.Log("Attached: " + attachedObject.name);
                }
            }
        }
    }
    public void DrawConnectors()
    {
        foreach (var kvp in RaysToDraw)
        {
            Vector3 start = kvp.Value.Item1; // start is object
            Vector3 finish = kvp.Value.Item2; // end is 0,0,0
            //DrawConnector(start, finish);
            DrawConnector(start, finish);
        }
    }
    void DrawConnector(Vector3 start, Vector3 finish)
    {
        //ClearPieces();
        Vector3 direction = finish - start;
        float Xval = -start.x;
        float Yval = -start.y;

        Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 0, 0);
        //GameObject instantiatedPiece = Instantiate(myPrefab, position, rotation);
        GameObject instantiatedPiece = Instantiate(myPrefab, start, rotation);
        instantiatedPiece.transform.localScale = new Vector3(Xval, 1, 1);
        GameObject instantiatedPiece2 = Instantiate(prefaab, instantiatedPiece.transform.position, rotation);
        //instantiatedPiece2.transform.localScale = new Vector3(1, Yval, 0);
        GameObject instantiatedPiece3 = Instantiate(myPrefab, instantiatedPiece2.transform.position, rotation);
        instantiatedPiece3.transform.position = new Vector3(instantiatedPiece3.transform.position.x, instantiatedPiece3.transform.position.y, (-Yval > 0) ? 90 : -90);
        instantiatedPiece3.transform.localScale = new Vector3(Yval, 1, 1);

        


        // draw the connector pieces
    }
    void ClearPieces()
    {
        foreach (GameObject piece in GameObject.FindGameObjectsWithTag("ConnectorPiece"))
        {
            Destroy(piece);
        }
    }
}*/