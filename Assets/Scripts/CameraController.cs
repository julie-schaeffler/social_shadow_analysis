using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [Header("Movement & Zoom")]
    public float moveSpeed = 20f;
    public float zoomSpeed = 5f;
    public float minZoom = 10f;
    public float maxZoom = 100f;

    [Header("Rotation")]
    public float rotationSpeed = 5f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private float yaw;
    private float pitch;
    private bool isRotating = false;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            InputField inputField = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
            if (inputField != null && inputField.isFocused)
            {
                return;
            }
        }

        HandleMovement();
        HandleZoom();
        HandleRotation();
    }

    void HandleMovement()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = Vector3.zero;
        if (Input.GetKey("w") || Input.GetKey(KeyCode.UpArrow))
            moveDirection += forward;
        if (Input.GetKey("s") || Input.GetKey(KeyCode.DownArrow))
            moveDirection -= forward;
        if (Input.GetKey("d") || Input.GetKey(KeyCode.RightArrow))
            moveDirection += right;
        if (Input.GetKey("a") || Input.GetKey(KeyCode.LeftArrow))
            moveDirection -= right;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            Vector3 pos = transform.position;
            pos.y -= scroll * zoomSpeed * 100f * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minZoom, maxZoom);
            transform.position = pos;
        }
    }

    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetMouseButton(1) && isRotating)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * rotationSpeed;
            pitch -= mouseY * rotationSpeed;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }

        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
