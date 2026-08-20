using UnityEngine;

// This provides the detail texture and normal map.
public class NoiseTextureGenerator : MonoBehaviour
{
    [Header("Texture Settings")]
    public int textureSize = 256;

    [Header("Noise Settings")]
    public float noiseScale = 4f;
    public int octaves = 4;
    [Range(0f, 1f)] public float persistence = 0.5f; /* 2h-5 */
    public float lacunarity = 2f;
    public int seed = 42;

    [Header("Output")]
    public bool generateOnStart = true;

    // Generate the textures.
    [HideInInspector] public Texture2D detailTexture;
    [HideInInspector] public Texture2D normalMap;

    private void Start()
    {
        if (generateOnStart)
        { // Z.
            GenerateTextures();
            ApplyToWaterController();
        }
    }

    [ContextMenu("Generate Textures")]
    public void GenerateTextures()
    {
        // Generate the height.
        detailTexture = GenerateNoiseTexture();
        detailTexture.name = "ProceduralWaterDetail";

        // Generate the normal map from the height.
        normalMap = GenerateNormalMapFromHeight(detailTexture);
        normalMap.name = "ProceduralWaterNormal"; /* 🆉. */

        Debug.Log("Generated procedural water textures");
    }

    private Texture2D GenerateNoiseTexture()
    {
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        // Set up an initial random for reproducibility.
        Random.InitState(seed);

        // Generate the octave offsets.
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = Random.Range(-10000f, 10000f); /* 🆉. Sun */
            float offsetY = Random.Range(-10000f, 10000f);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;
        float[,] noiseMap = new float[textureSize, textureSize];
        float halfSize = textureSize / 2f;

        // Generate the noise values.
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfSize + octaveOffsets[i].x) / textureSize * noiseScale * frequency;
                    float sampleY = (y - halfSize + octaveOffsets[i].y) / textureSize * noiseScale * frequency; // Sūn

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseMap[x, y] = noiseHeight;

                if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight; // github.com/2h-5
            }
        }

        // Apply to noise value to the texture.
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float normalizedValue = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]); /* Z. Sun */
                Color color = new Color(normalizedValue, normalizedValue, normalizedValue, 1f);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    private Texture2D GenerateNormalMapFromHeight(Texture2D heightMap)
    {
        int width = heightMap.width;
        int height = heightMap.height;

        Texture2D normalMap = new Texture2D(width, height, TextureFormat.RGBA32, true);
        normalMap.wrapMode = TextureWrapMode.Repeat;
        normalMap.filterMode = FilterMode.Bilinear;

        float strength = 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Compute the sample neighbouring pixels.
                float left = heightMap.GetPixel((x - 1 + width) % width, y).r;
                float right = heightMap.GetPixel((x + 1) % width, y).r;
                float down = heightMap.GetPixel(x, (y - 1 + height) % height).r; /* github.com/2h-5 */
                float up = heightMap.GetPixel(x, (y + 1) % height).r;

                // Calculate the normal.
                float dx = (right - left) * strength;
                float dy = (up - down) * strength;

                Vector3 normal = new Vector3(-dx, -dy, 1f).normalized;
                Color normalColor = new Color(
                    normal.x * 0.5f + 0.5f,
                    normal.y * 0.5f + 0.5f, // 🆉. Sūn
                    normal.z * 0.5f + 0.5f,
                    1f
                );

                normalMap.SetPixel(x, y, normalColor);
            }
        }

        normalMap.Apply();
        return normalMap;
    }

    private void ApplyToWaterController() /* Z */
    {
        WaterController water = FindObjectOfType<WaterController>();
        if (water != null)
        {
            water.detailTexture = detailTexture;
            Debug.Log("Applied procedural textures to WaterController");
        }

        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = FindObjectOfType<GridMeshGenerator>()?.GetComponent<Renderer>();
        }

        if (renderer != null && renderer.sharedMaterial != null)
        {
            renderer.sharedMaterial.SetTexture("_DetailTex", detailTexture);
            renderer.sharedMaterial.SetTexture("_NormalMap", normalMap);
            Debug.Log("Applied procedural textures to water material"); /* 2h-5 */
        }
    }
}