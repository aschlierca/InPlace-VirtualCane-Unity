using UnityEngine;

public class HeadMovement : MonoBehaviour
{
    public Transform playerBody; // Parent capsule for horizontal rotation
    public float mouseSensitivity = 2f;
    public float touchSensitivity = 0.2f;

    private float xRotation = 0f; // Vertical rotation
    private float yRotation = 0f; // Horizontal rotation

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }

    void Update()
    {
#if UNITY_EDITOR
        HandleMouseLook();
#elif UNITY_IOS
        HandleTouchLook();
#endif
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Vertical rotation
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal rotation
        playerBody.Rotate(Vector3.up * mouseX, Space.World);
    }

    void HandleTouchLook()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float deltaX = touch.deltaPosition.x * touchSensitivity;
                float deltaY = touch.deltaPosition.y * touchSensitivity;

                // Vertical rotation
                xRotation -= deltaY;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);
                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

                // Horizontal rotation
                playerBody.Rotate(Vector3.up * deltaX);
            }
        }
    }
}
