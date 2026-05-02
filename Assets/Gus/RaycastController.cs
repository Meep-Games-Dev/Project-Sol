//using System.Numerics;
using JetBrains.Annotations;
using Station;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Savee;

public class RayController : MonoBehaviour
{
    public InputSystem_Actions input;
    public InputAction mousePosition;
    public InputAction leftMouse;
    public InputAction Rotate;
    public InputAction rightMouse;
    public InputAction kill;
    [SerializeField] private GameObject myPrefab;
    float OX = 0f;
    float OY = 0f;
    int clones = 0;
    public List<GameObject> ATpiece = new List<GameObject>();
    public List<GameObject> Connectors = new List<GameObject>();
    public Dictionary<Vector3, GameObject> pieces = new Dictionary<Vector3, GameObject>();

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
        input = new InputSystem_Actions();
        mousePosition = input.Player.MousePos;
        leftMouse = input.Player.MouseDown;
        Rotate = input.Player.Rotate;
        rightMouse = input.Player.MouseRight;
        kill = input.Player.Kill;

    }
    private GameObject attachedObject = null; // Track the currently attached object

    void Update()
    {
        Vector2 mouseScreenPos = mousePosition.ReadValue<Vector2>();
        Vector3 screenPointWithDepth = new Vector3(mouseScreenPos.x, mouseScreenPos.y,
            Mathf.Abs(Camera.main.transform.position.z));
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(screenPointWithDepth);
        // Draw ray continuously while something is attached
        if (attachedObject != null)
        {
            Vector2 start = new Vector2(attachedObject.transform.position.x, attachedObject.transform.position.y);
            Vector2 finish = Vector2.zero;
            //Vector2 direction = finish - start;
            attachedObject.SendMessage("show", SendMessageOptions.DontRequireReceiver); // make it so the sprite displays the piece name 
            // Move attached object with mouse
            //attachedObject.transform.position = (Vector2)mouseWorldPos

            if(attachedObject.tag != "clone")
            {
                GameObject clone = Instantiate(attachedObject, mouseWorldPos, Quaternion.identity); // if the piece is a parent, make a child and move that
                clone.tag = "clone";
                attachedObject = clone;
                attachedObject.transform.position = mouseWorldPos;
            }
            else
            {
                attachedObject.transform.position = (Vector2)mouseWorldPos;
            }
            

            attachedObject.transform.position = (Vector2)mouseWorldPos;

            // Rotate attached object
            if (Rotate.WasPressedThisFrame())
            {
                attachedObject.transform.Rotate(0, 0, 90);
                Debug.Log("Rotated piece: " + attachedObject.name);
            }
            if (kill.WasPressedThisFrame())
            {
                ATpiece.Remove(attachedObject);
                Destroy(attachedObject);
                attachedObject = null;
            }
        }

        // On click: attach or detach
        if (leftMouse.WasPressedThisFrame())
        {
            if (attachedObject != null)
            {
                if(attachedObject.transform.position.x <= -7) // FINN, CHANGE -6 TO FURTHEST LEFT POSITION OF DEVELOPMENT AREA, THIS IS A TEMP FIX TO PREVENT BUGS OF PIECES BEING PLACED IN THE UI AND THEN PICKED UP AND PLACED IN THE DEVELOPMENT AREA FOR FREE
                {
                    ATpiece.Remove(attachedObject);
                    Destroy(attachedObject);
                }
                // Second click — drop the object
                attachedObject.SendMessage("hide", SendMessageOptions.DontRequireReceiver); // hide the piece name when dropped
                ATpiece.Add(attachedObject);
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
        DrawConnector(ATpiece);
    }
    void DrawConnector(List<GameObject> list)
    {
        ClearPieces();
        if(list.Count >= 1)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var start = list[i].transform.position;
                Vector3 finish = Vector3.zero;
                float distance = Vector3.Distance(start, finish);
                Vector3 direction = finish - start;

                //Quaternion rotation = Quaternion.identity;
                Quaternion rotation = Quaternion.Euler(direction);

                //GameObject instantiatedPiece = Instantiate(myPrefab, position, rotation);
                /*
                GameObject instantiatedPiece = Instantiate(myPrefab, start, rotation);
                instantiatedPiece.transform.localScale = new Vector3(Xval, 1, 1);
                GameObject instantiatedPiece2 = Instantiate(prefaab, instantiatedPiece.transform.position, rotation);
                instantiatedPiece2.transform.position = new Vector3(instantiatedPiece2.transform.position.x, instantiatedPiece2.transform.position.y, instantiatedPiece2.transform.position.z); 
                //instantiatedPiece2.transform.localScale = new Vector3(1, Yval, 0);
                GameObject instantiatedPiece3 = Instantiate(myPrefab, instantiatedPiece2.transform.position, rotation);
                instantiatedPiece3.transform.position = new Vector3(instantiatedPiece3.transform.position.x, instantiatedPiece3.transform.position.y, (-Yval > 0) ? 90 : -90);
                instantiatedPiece3.transform.localScale = new Vector3(Yval, 1, 1);
                */
                rotation = Quaternion.Euler(0, 0, Vector3.SignedAngle(Vector3.right, direction, Vector3.forward));
                // + new Vector3((start.y == 0) ? start.x <= 0 ? distance / 2 : -distance / 2 : 0, (start.x == 0) ? start.y <= 0 ? distance / 2 : -distance / 2 : 0), rotation * Quaternion.Euler(0, 0, (start.x == 0) ? start.y <= 0 ? -90 : 90 : 0)
                GameObject instantiatedPiece = Instantiate(myPrefab, start, rotation);
                Connectors.Add(instantiatedPiece);
                instantiatedPiece.transform.position = (start - finish)/2;
                instantiatedPiece.transform.localScale = new Vector3(distance, 0.25f, 0.25f);
                Debug.Log("Drew connector at: " + instantiatedPiece.transform.position);
                // draw the connector pieces
            }
        }
    }
    void ClearPieces()
    {
        if (Connectors.Count == 0)
        {
            return;
        }
        else
        {
            foreach (GameObject piece in Connectors)
            {
                Destroy(piece);
            }
        }
    }
}
