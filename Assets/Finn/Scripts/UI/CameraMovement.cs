using UnityEngine;
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
        position = transform.position;
        Vector2 moveDir = movement.ReadValue<Vector2>();
        float scrollDir = scroll.ReadValue<float>();
        Vector2 offset = new Vector2(transform.position.x, transform.position.z) * parallaxSpeed;
        skyboxObj.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_CameraPos", offset);
        Debug.Log(skyboxObj.GetComponent<MeshRenderer>().sharedMaterial.GetVector("_CameraPos"));
        if (scrollDir != 0)
        {
            if (scrollDir < 0 && cam.transform.position.z > -600)
            {
                cam.gameObject.transform.Translate(new Vector3(0, 0, Mathf.Abs(scrollDir)) * -cameraZoomSpeed * Time.deltaTime);
                if (parrallax != null) parrallax.Move(new Vector2(0, 0));

            }
            else if (scrollDir > 0 && cam.transform.position.z < -10)
            {
                cam.gameObject.transform.Translate(new Vector3(0, 0, Mathf.Abs(scrollDir)) * cameraZoomSpeed * Time.deltaTime);
                if (parrallax != null) parrallax.Move(new Vector2(0, 0));

            }
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
        rb.AddForce(moveDir * cameraMoveForce);
    }
}
