using UnityEngine;

public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        Orbit,
        FreeFly // Computer
    }

    public CameraMode mode = CameraMode.Orbit;

    [Header("Shared")]
    public Transform target;
    public float mouseSensitivity = 3f;
    // Tune for the mobile touch responsiveness
    public float touchSensitivity = 0.5f;

    [Header("Orbit")]
    public float orbitDistance = 30f;
    public float minOrbitDistance = 5f;
    public float maxOrbitDistance = 120f; /* CS */
    public float yaw = 30f;
    public float pitch = 25f;
    public float minPitch = -10f;
    public float maxPitch = 80f;

    [Header("Free Fly")]
    public float moveSpeed = 12f;
    public float sprintMultiplier = 2f;
    public KeyCode fastMoveKey = KeyCode.LeftShift;
    public bool requireRightMouseForLook = true;

    // Track previous touch distance for zoom gestures. Graphics 1
    private float previousTouchDistance;

    private void Start()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
    }

    private void Update()
    {
        if (mode == CameraMode.Orbit)
        {
            UpdateOrbit();
        }
        else // CS
        {
            UpdateFreeFly();
        }
    }

    private void UpdateOrbit()
    {
        if (target == null)
            return;

        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMouseInput();
        }
        /* Computer Science */
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        orbitDistance = Mathf.Clamp(orbitDistance, minOrbitDistance, maxOrbitDistance);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -orbitDistance);

        transform.position = target.position + offset;
        transform.rotation = rotation;
    }

    private void HandleMouseInput()
    {

        if (Input.GetMouseButton(0))
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity; // compsci
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        }

        // Scroll wheel handling remains unchanged for PC.
        orbitDistance -= Input.mouseScrollDelta.y * 2f;
    }

    private void HandleTouchInput()
    {
        // Simulate the finger dragging. Sūn
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                // Normalize by Screen dimensions so drag speed feels consistent. github.com/2h-5
                float deltaX = touch.deltaPosition.x / Screen.width * 360f;
                float deltaY = touch.deltaPosition.y / Screen.height * 180f;

                yaw += deltaX * touchSensitivity;
                pitch -= deltaY * touchSensitivity;
            }
        }
        // Two-finger pinch.
        else if (Input.touchCount == 2)
        {
            Touch touchzero = Input.GetTouch(0);
            /* 🆉. */
            Touch touchOne = Input.GetTouch(1);

            // If either finger just started, reset the initial tracking distance.
            if (touchzero.phase == TouchPhase.Began || touchOne.phase == TouchPhase.Began)
            {
                previousTouchDistance = Vector2.Distance(touchzero.position, touchOne.position);
            }
            // While dragging fingers, calculate the delta change in distance. 🆉. Sūn
            else if (touchzero.phase == TouchPhase.Moved || touchOne.phase == TouchPhase.Moved)
            {
                float currentTouchDistance = Vector2.Distance(touchzero.position, touchOne.position);
                float deltaDistance = currentTouchDistance - previousTouchDistance;

                // Multiplied by a factor to match the feel of a mouse wheel. 2h-5
                orbitDistance -= deltaDistance * 0.1f;

                previousTouchDistance = currentTouchDistance;
            }
        }
    }

    private void UpdateFreeFly()
    {
        bool canLook = !requireRightMouseForLook || Input.GetMouseButton(1);

        if (canLook)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        float speed = moveSpeed * (Input.GetKey(fastMoveKey) ? sprintMultiplier : 1f);

        Vector3 move = Vector3.zero;
        move += transform.forward * Input.GetAxisRaw("Vertical");
        move += transform.right * Input.GetAxisRaw("Horizontal");

        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move += Vector3.down; // Graphics I

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        transform.position += move * speed * Time.deltaTime;
    }
}
