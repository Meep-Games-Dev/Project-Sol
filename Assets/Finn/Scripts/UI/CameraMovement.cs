using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public PlayerInput PlayerInput;
    private InputAction movement;
    private InputAction scroll;
    public Rigidbody2D rb;
    public float cameraMoveForce = 1f;
    public float cameraRBMaxSpeed = 5f;
    public float stoppingForce = 2.0f;
    public Camera cam;
    public ParrallaxTest parrallax;
    public Vector2 position;
    public GameObject skyboxObj;
    private Material skybox;
    public float parallaxSpeed = 0.01f;
    public float cameraZoomSpeed = 1;
    public float cameraDistanceMult = 0.5f;
    public List<Planet> planets;
    public float cameraPickupRad;
    public Transform follow;
    bool following = false;
    private Vector3 lastFollowPos;
    private List<SphereCollider> planetColliders = new List<SphereCollider>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created\
    private void Awake()
    {
        skybox = skyboxObj.GetComponent<MeshRenderer>().material;
        PlayerInput = new PlayerInput();
        movement = PlayerInput.Main.Movement;
        scroll = PlayerInput.Main.Scroll;
        rb = GetComponent<Rigidbody2D>();

    }
    void Start()
    {


    }

    public async void WaitForPlanetColliders()
    {
        while (planetColliders.Count != planets.Count)
        {
            for (int i = 0; i < planets.Count; i++)
            {
                planetColliders.Add(planets[i].gameObject.GetComponentInChildren<SphereCollider>());
            }
            await Task.Yield();
        }
    }
    public void OnEnable()
    {
        movement.Enable();
        scroll.Enable();
    }

    public void OnDisable()
    {
        movement.Disable();
        scroll.Disable();
    }
    // Update is called once per frame
    void Update()
    {
        bool mouseOverUI = EventSystem.current.IsPointerOverGameObject();
        position = transform.position;
        Vector2 moveDir = movement.ReadValue<Vector2>();
        float scrollDir = scroll.ReadValue<float>();
        Vector2 offset = new Vector2(transform.position.x, transform.position.y) * parallaxSpeed;
        skyboxObj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_CameraPos", offset);
        //Debug.Log(skyboxObj.GetComponent<MeshRenderer>().sharedMaterial.GetVector("_CameraPos"));
        bool hasParent = false;
        if (planetColliders.Count == planets.Count)
        {
            for (int j = 0; j < planets.Count; j++)
            {
                SphereCollider collider = planetColliders[j];
                if (Vector2.Distance((Vector2)planets[j].gameObject.transform.position, (Vector2)transform.position) < (collider.radius * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y)) + cameraPickupRad)
                {
                    follow = planets[j].gameObject.transform;
                    following = true;
                    hasParent = true;
                    lastFollowPos = follow.position;
                }
            }
        }
        else
        {
            WaitForPlanetColliders();
        }
        if (!hasParent)
        {
            if (following)
            {
                following = false;
            }
        }

        if (scrollDir != 0 && !mouseOverUI)
        {
            if (scrollDir < 0 && cam.transform.position.z > -6000)
            {
                cam.gameObject.transform.Translate(new Vector3(0, 0, Mathf.Abs(scrollDir)) * (-cameraZoomSpeed * Mathf.Abs(cam.transform.position.z)) * Time.deltaTime);
                if (parrallax != null) parrallax.Move(new Vector2(0, 0));

            }
            else if (scrollDir > 0 && cam.transform.position.z < -10)
            {
                cam.gameObject.transform.Translate(new Vector3(0, 0, Mathf.Abs(scrollDir)) * (cameraZoomSpeed * Mathf.Abs(cam.transform.position.z)) * Time.deltaTime);
                if (parrallax != null) parrallax.Move(new Vector2(0, 0));

            }
        }
        if (cam.transform.position.z < -7000)
        {
            cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, -7000);
        }
        if (cam.transform.position.z > -15)
        {
            cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, -15);
        }
        if (rb.linearVelocity != Vector2.zero && parrallax != null)
        {
            parrallax.Move(rb.linearVelocity);
        }
        if (moveDir == Vector2.zero)
        {
            rb.linearDamping = stoppingForce;
        }
        else
        {
            rb.linearDamping = 0;
        }
        if (Vector2.Distance(new Vector2(0, 0), transform.position) < 6500)
        {
            rb.AddForce(moveDir * (cameraMoveForce * cameraDistanceMult) * Mathf.Abs(cam.transform.position.z));
        }
        else
        {
            rb.AddForce(-transform.position * 500);
        }
    }
    private void LateUpdate()
    {
        if (following && follow != null)
        {
            Vector3 delta = follow.position - lastFollowPos;
            transform.position += delta;
            lastFollowPos = follow.position;
        }
    }
}
