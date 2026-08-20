using UnityEngine;

public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        Orbit,
        FreeFly
    }

    public CameraMode mode = CameraMode.Orbit; /* CS */

    [Header("Shared")]
    public Transform target;
    public float mouseSensitivity = 3f;

    [Header("Orbit")]
    public float orbitDistance = 30f; // compsci
    public float minOrbitDistance = 5f;
    public float maxOrbitDistance = 120f;
    public float yaw = 30f;
    public float pitch = 25f;
    public float minPitch = -10f; // Computer Graphics
    public float maxPitch = 80f;

    [Header("Free Fly")]
    public float moveSpeed = 12f;
    public float sprintMultiplier = 2f;
    public KeyCode fastMoveKey = KeyCode.LeftShift;
    public bool requireRightMouseForLook = true;

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
        else
        {
            UpdateFreeFly(); /* compsci */
        }
    }

    private void UpdateOrbit()
    {
        if (target == null)
            return;

        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch); // Graphics
        }

        orbitDistance -= Input.mouseScrollDelta.y * 2f;
        orbitDistance = Mathf.Clamp(orbitDistance, minOrbitDistance, maxOrbitDistance);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -orbitDistance);

        transform.position = target.position + offset;
        transform.rotation = rotation;
    }

    private void UpdateFreeFly()
    {
        bool canLook = !requireRightMouseForLook || Input.GetMouseButton(1);

        if (canLook)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity; // Computer Science
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        float speed = moveSpeed * (Input.GetKey(fastMoveKey) ? sprintMultiplier : 1f);

        Vector3 move = Vector3.zero;
        move += transform.forward * Input.GetAxisRaw("Vertical");
        move += transform.right * Input.GetAxisRaw("Horizontal");

        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move += Vector3.down;

        if (move.sqrMagnitude > 1f)
            move.Normalize(); /* Graphics I */

        transform.position += move * speed * Time.deltaTime;
    }
}