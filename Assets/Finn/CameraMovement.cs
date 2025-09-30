using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public PlayerInput inputActions;
    public InputAction cameraForward;
    public InputAction cameraRight;
    public InputAction cameraLeft;
    public InputAction cameraBack;
    public float cameraMoveSpeed = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputActions = new PlayerInput();
    }

    public void OnEnable()
    {
        cameraForward = inputActions.Main.CameraForward;
        cameraRight = inputActions.Main.CameraRight;
        cameraLeft = inputActions.Main.CameraLeft;
        cameraBack = inputActions.Main.CameraBack;
        cameraForward.Enable();
        cameraRight.Enable();
        cameraLeft.Enable();
        cameraBack.Enable();
    }

    public void OnDisable()
    {
        cameraForward = inputActions.Main.CameraForward;
        cameraRight = inputActions.Main.CameraRight;
        cameraLeft = inputActions.Main.CameraLeft;
        cameraBack = inputActions.Main.CameraBack;
        cameraForward.Disable();
        cameraRight.Disable();
        cameraLeft.Disable();
        cameraBack.Disable();
    }
    // Update is called once per frame
    void Update()
    {
        if (cameraForward.IsPressed())
        {
            transform.Translate(Vector2.up * cameraMoveSpeed * Time.deltaTime);
        }
        else if(cameraBack.IsPressed())
        {
            transform.Translate(Vector2.down * cameraMoveSpeed * Time.deltaTime);
        }
        if (cameraLeft.IsPressed())
        {
            transform.Translate(Vector2.left * cameraMoveSpeed * Time.deltaTime);
        }
        else if (cameraRight.IsPressed())
        {
            transform.Translate(Vector2.right * cameraMoveSpeed * Time.deltaTime);
        }
    }
}
