using UnityEngine;

public class FloatingObjectController : MonoBehaviour
{
    [Header("Water Reference")]
    public WaterController water;

    [Header("Sampling")]
    public Vector3 localSampleOffset = Vector3.zero;
    public float verticalOffset = 0.25f;

    [Header("Smoothing")]
    public float positionLerp = 6f;
    public float tiltLerp = 4f;

    [Header("Orientation")]
    public bool useSurfaceNormalForTilt = true;

    [Header("Debug")]
    public bool showDebug = false;

    private float currentHeight;
    private Quaternion currentRotation;
    private bool initialized = false;

    private void Start()
    {
        // Set the initial position.
        currentHeight = transform.position.y;
        currentRotation = transform.rotation;
        initialized = true;
    }

    private void LateUpdate()
    {
        // Set exception stage.
        if (water == null)
        {
            Debug.LogWarning("FloatingObjectController: No WaterController assigned!");
            return;
        }

        float currentTime = Time.time;

        // Compute the sampling point.
        Vector3 worldSamplePoint = transform.TransformPoint(localSampleOffset);

        // Convert to x-z coordinates.
        Vector2 sampleXZ = new Vector2(worldSamplePoint.x, worldSamplePoint.z);

        // Query the water height.
        float waterHeight = water.SampleHeight(sampleXZ, currentTime);

        // Add the vertical offset.
        float targetHeight = waterHeight + verticalOffset;

        // Smooth the height transition
        if (!initialized)
        {
            currentHeight = targetHeight;
            initialized = true;
        }
        else
        {
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, positionLerp * Time.deltaTime);
        }

        // Update the position.
        Vector3 newPosition = transform.position;
        newPosition.y = currentHeight;
        transform.position = newPosition;

        if (useSurfaceNormalForTilt)
        {
            // Find the water normal at the sampling point.
            Vector3 waterNormal = water.SampleNormal(sampleXZ, currentTime);

            // Build the rotation from the water normal.
            Vector3 currentForward = transform.forward;
            Vector3 projectedForward = Vector3.ProjectOnPlane(currentForward, waterNormal);

            // Handle the edge case.
            if (projectedForward.sqrMagnitude < 0.001f)
            {
                projectedForward = Vector3.ProjectOnPlane(Vector3.forward, waterNormal);
            }
            projectedForward.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, waterNormal);

            // Smooth the rotation.
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, tiltLerp * Time.deltaTime);
            transform.rotation = currentRotation;
        }

        // Debug.
        if (showDebug)
        {
            Debug.DrawLine(worldSamplePoint, worldSamplePoint + Vector3.down * 2f, Color.yellow);
            if (useSurfaceNormalForTilt)
            {
                Vector3 waterPos = water.SampleWorldPosition(sampleXZ, currentTime);
                Vector3 waterNormal = water.SampleNormal(sampleXZ, currentTime);
                Debug.DrawLine(waterPos, waterPos + waterNormal * 2f, Color.green);
            }
        }
    }

    // Call for validation.
    private void OnValidate()
    {
        if (positionLerp < 0f) positionLerp = 0f;
        if (tiltLerp < 0f) tiltLerp = 0f;
    }

    // Set up by drawing gizmos.
    private void OnDrawGizmosSelected()
    {
        Vector3 samplePoint = transform.TransformPoint(localSampleOffset);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(samplePoint, 0.3f);
        Gizmos.DrawLine(transform.position, samplePoint);
    }
}