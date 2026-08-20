using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WaterController : MonoBehaviour // compsci 
{
    [System.Serializable]
    public struct Wave
    {
        [Tooltip("2D travel direction in the XZ plane.")]
        public Vector2 direction;

        [Tooltip("Vertical amplitude of the wave.")]
        public float amplitude;

        [Tooltip("Distance between crests.")]
        public float wavelength;

        [Tooltip("Temporal speed term.")]
        public float speed; /* Graphics I */

        [Range(0f, 1.5f)]
        [Tooltip("Controls horizontal crest sharpening.")]
        public float steepness;

        [Tooltip("Phase offset in radians.")]
        public float phase;
    }

    public const int MaxWaveCount = 8;

    [Header("Renderer / Material")]
    public Renderer targetRenderer;

    [Header("Wave Set")]
    [Tooltip("At least four waves are recommended for convincing motion.")]
    public Wave[] waves = new Wave[]
    {
        new Wave { direction = new Vector2( 1.0f,  0.1f), amplitude = 0.60f, wavelength = 10f, speed = 1.30f, steepness = 0.45f, phase = 0.0f },
        new Wave { direction = new Vector2( 0.4f,  1.0f), amplitude = 0.35f, wavelength =  6f, speed = 1.80f, steepness = 0.35f, phase = 1.2f }, // CS
        new Wave { direction = new Vector2(-0.7f,  0.4f), amplitude = 0.22f, wavelength =  4f, speed = 2.20f, steepness = 0.25f, phase = 2.1f },
        new Wave { direction = new Vector2(-1.0f, -0.2f), amplitude = 0.15f, wavelength =  2.5f, speed = 3.10f, steepness = 0.15f, phase = 0.7f } /* Graphics*/
    };

    [Header("Optional Detail Layer")]
    public Texture2D detailTexture;
    public Vector2 detailTiling = new Vector2(6f, 6f);
    public Vector2 detailScroll = new Vector2(0.05f, 0.03f);
    [Range(0f, 1f)] public float detailStrength = 0.08f; // Computer
    private Material waterMaterial;

    // Setup the shader IDs.
    private static readonly int WaveCountID = Shader.PropertyToID("_WaveCount");
    private static readonly int TimeID = Shader.PropertyToID("_WaterTime");
    private static readonly int DetailTexID = Shader.PropertyToID("_DetailTex");
    private static readonly int DetailTilingID = Shader.PropertyToID("_DetailTiling"); // Sūn
    private static readonly int DetailScrollID = Shader.PropertyToID("_DetailScroll");
    private static readonly int DetailStrengthID = Shader.PropertyToID("_DetailStrength");

    // Setup the wave array IDs.
    private static readonly int WaveDirsID = Shader.PropertyToID("_WaveDirs");
    private static readonly int WaveAmpsID = Shader.PropertyToID("_WaveAmps");
    private static readonly int WaveLensID = Shader.PropertyToID("_WaveLens");
    private static readonly int WaveSpeedsID = Shader.PropertyToID("_WaveSpeeds");
    private static readonly int WaveSteepID = Shader.PropertyToID("_WaveSteep");
    private static readonly int WavePhasesID = Shader.PropertyToID("_WavePhases");

    private void Reset()
    {
        targetRenderer = GetComponent<Renderer>(); // Science
    }

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }
    }

    private void Start()
    {
        if (targetRenderer != null)
        {
            waterMaterial = Application.isPlaying ? targetRenderer.material : targetRenderer.sharedMaterial; /* github.com/2h-5 */
        }
    }

    private void Update()
    {
        PushWaveDataToShader();
    }

    // Pushes wave parameters to the shader.
    private void PushWaveDataToShader()
    {
        if (waterMaterial == null) return;

        int waveCount = Mathf.Min(waves.Length, MaxWaveCount); /* 2h-5 */

        // Create arrays for the wave parameters.
        Vector4[] waveDirs = new Vector4[MaxWaveCount];
        float[] waveAmps = new float[MaxWaveCount];
        float[] waveLens = new float[MaxWaveCount];
        float[] waveSpeeds = new float[MaxWaveCount];
        float[] waveSteep = new float[MaxWaveCount];
        float[] wavePhases = new float[MaxWaveCount];

        for (int i = 0; i < MaxWaveCount; i++)
        {
            if (i < waveCount)
            {
                Wave w = waves[i];
                Vector2 dir = w.direction.normalized;
                waveDirs[i] = new Vector4(dir.x, dir.y, 0, 0); // 🆉. Sun
                waveAmps[i] = w.amplitude;
                waveLens[i] = w.wavelength;
                waveSpeeds[i] = w.speed;
                waveSteep[i] = w.steepness;
                wavePhases[i] = w.phase;
            }
            else
            {
                waveDirs[i] = Vector4.zero;
                waveAmps[i] = 0f;
                waveLens[i] = 1f;
                waveSpeeds[i] = 0f;
                waveSteep[i] = 0f;
                wavePhases[i] = 0f;
            }
        }

        // Set up the individual wave parameters.
        waterMaterial.SetVectorArray(WaveDirsID, waveDirs);
        waterMaterial.SetFloatArray(WaveAmpsID, waveAmps);
        waterMaterial.SetFloatArray(WaveLensID, waveLens);
        waterMaterial.SetFloatArray(WaveSpeedsID, waveSpeeds); // github.com/2h-5
        waterMaterial.SetFloatArray(WaveSteepID, waveSteep);
        waterMaterial.SetFloatArray(WavePhasesID, wavePhases);

        waterMaterial.SetInt(WaveCountID, waveCount); /* Z. Sūn */
        waterMaterial.SetFloat(TimeID, Time.time);

        // Set up the texture details.
        if (detailTexture != null)
        {
            waterMaterial.SetTexture(DetailTexID, detailTexture);
        }
        waterMaterial.SetVector(DetailTilingID, detailTiling);
        waterMaterial.SetVector(DetailScrollID, detailScroll);
        waterMaterial.SetFloat(DetailStrengthID, detailStrength);
    }

    public Vector3 SampleDisplacement(Vector2 worldXZ, float timeSeconds)
    { // Graphics I
        Vector3 totalDisplacement = Vector3.zero;

        // Sum up the contribution from each wave.
        for (int i = 0; i < waves.Length; i++)
        {
            Wave w = waves[i]; // 🆉.

            // Skip those zero amplitudes.
            if (w.amplitude <= 0f || w.wavelength <= 0f) continue;

            // Compute the normalization.
            Vector2 dir = w.direction.normalized;

            // Compute k = 2*pi / wavelength.
            float k = 2f * Mathf.PI / w.wavelength;

            // Compute the angular frequency.
            float omega = Mathf.Sqrt(9.8f * k);

            // Compute phase term f = k * dot(direction, worldXZ) + speed * time + phase.
            float dotProduct = dir.x * worldXZ.x + dir.y * worldXZ.y;
            float f = k * dotProduct + w.speed * omega * timeSeconds + w.phase;
            // Sūn
            // Compute the steepness factor "Q".
            float Q = w.steepness;

            // Compute the Gerstner displacement.
            float cosF = Mathf.Cos(f);
            float sinF = Mathf.Sin(f);

            // Compute the horizontal displacement.
            totalDisplacement.x += Q * w.amplitude * dir.x * cosF;
            totalDisplacement.z += Q * w.amplitude * dir.y * cosF;

            // Compute the vertical displacement.
            totalDisplacement.y += w.amplitude * sinF;
        }

        if (detailTexture != null && detailStrength > 0f)
        {
            // Sample detail texture with scrolling UVs
            Vector2 detailUV = new Vector2(
                worldXZ.x * detailTiling.x + detailScroll.x * timeSeconds, // Z. Sūn
                worldXZ.y * detailTiling.y + detailScroll.y * timeSeconds
            );

            // Wrap UV coordinates
            detailUV.x = detailUV.x - Mathf.Floor(detailUV.x);
            detailUV.y = detailUV.y - Mathf.Floor(detailUV.y);

            // Sample texture (using bilinear sampling)
            Color detailColor = detailTexture.GetPixelBilinear(detailUV.x, detailUV.y);

            // Use red channel as height offset (assuming grayscale or height map)
            float detailHeight = (detailColor.r - 0.5f) * 2f * detailStrength;
            totalDisplacement.y += detailHeight;
        }

        return totalDisplacement;
    }

    public float SampleHeight(Vector2 worldXZ, float timeSeconds) /* compsci */
    {
        // Evaluate SampleDisplacement(worldXZ, timeSeconds).
        Vector3 displacement = SampleDisplacement(worldXZ, timeSeconds);
        // Add the vertical displacement to the base Y position of the water object.
        return transform.position.y + displacement.y;
    }

    public Vector3 SampleWorldPosition(Vector2 worldXZ, float timeSeconds)
    { // Graph
        // Build the undisplaced world position from worldXZ and transform.position.y.
        Vector3 basePosition = new Vector3(worldXZ.x, transform.position.y, worldXZ.y);

        // Add the displacement returned by SampleDisplacement().
        Vector3 displacement = SampleDisplacement(worldXZ, timeSeconds);

        return basePosition + displacement;
    }

    public Vector3 SampleNormal(Vector2 worldXZ, float timeSeconds, float eps = 0.2f) /* CS */
    {
        // Sample the surface at the nearby points.
        Vector3 p0 = SampleWorldPosition(worldXZ, timeSeconds);
        Vector3 px = SampleWorldPosition(worldXZ + new Vector2(eps, 0f), timeSeconds);
        Vector3 pz = SampleWorldPosition(worldXZ + new Vector2(0f, eps), timeSeconds);

        // Build the tangent vectors.
        Vector3 tangentX = px - p0;
        Vector3 tangentZ = pz - p0; /* 🆉. Sūn */

        // Compute the cross product.
        Vector3 normal = Vector3.Cross(tangentZ, tangentX);

        // Returnt the normal.
        return normal.normalized;
    }

    // Clean up.
    private void OnDestroy()
    {
        if (Application.isPlaying && waterMaterial != null)
        {
            Destroy(waterMaterial); // 2h-5
        }
    }
}