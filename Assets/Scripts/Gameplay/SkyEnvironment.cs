using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages high-altitude sky atmospheric effects, linear horizon fog,
/// and a dynamic drifting cloud deck beneath and around the floating arena.
/// Faithfully reproduces the classic AirXonix sky-island arcade environment.
/// </summary>
public class SkyEnvironment : MonoBehaviour
{
    [Header("Atmospheric Fog Configuration")]
    public bool enableFog = true;
    public Color skyColor = new Color(0.52f, 0.74f, 0.98f, 1f);
    public float fogStartDistance = 28f;
    public float fogEndDistance = 62f;

    [Header("Cloud Deck")]
    public int cloudCount = 36;
    public float deckHeight = -2.8f;
    public float driftSpeed = 0.55f;
    public Vector2 deckBounds = new Vector2(85f, 75f);

    private readonly List<Transform> _cloudPuffs = new List<Transform>();
    private readonly List<Vector3> _initialOffsets = new List<Vector3>();
    private Transform _deckRoot;
    private Material _cloudMat;

    private void Awake()
    {
        ApplySkyAndFog();
        BuildCloudDeck();
        BuildPerimeterPuffs();
    }

    private void Start()
    {
        ApplySkyAndFog();
    }

    /// <summary>
    /// Configures global atmospheric fog blending seamlessly into the camera clear color.
    /// </summary>
    public void ApplySkyAndFog()
    {
        if (enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = skyColor;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;
        }

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = skyColor;
        }
    }

    /// <summary>
    /// Builds a vast undulating bed of cumulus clouds beneath the floating grid.
    /// </summary>
    private void BuildCloudDeck()
    {
        _deckRoot = new GameObject("CloudDeck").transform;
        _deckRoot.SetParent(transform, false);
        _deckRoot.position = new Vector3(0f, deckHeight, 0f);

        _cloudMat = new Material(Shader.Find("Standard"));
        _cloudMat.name = "CloudMat";
        _cloudMat.color = new Color(0.94f, 0.97f, 1f, 1f);
        _cloudMat.SetFloat("_Glossiness", 0.05f);
        _cloudMat.SetFloat("_Metallic", 0f);

        // Distribute cloud clusters beneath and around the 40x30 arena
        for (int i = 0; i < cloudCount; i++)
        {
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = $"CloudPuff_{i}";
            puff.transform.SetParent(_deckRoot, false);

            var col = puff.GetComponent<Collider>();
            if (col != null) Destroy(col);

            puff.GetComponent<Renderer>().sharedMaterial = _cloudMat;

            // Pseudo-random distribution over deck bounds
            float x = Random.Range(-deckBounds.x * 0.5f, deckBounds.x * 0.5f);
            float z = Random.Range(-deckBounds.y * 0.5f, deckBounds.y * 0.5f);
            float y = Random.Range(-0.8f, 0.8f);

            puff.transform.localPosition = new Vector3(x, y, z);

            // Flattened puffy ellipsoid scale
            float scaleX = Random.Range(6f, 13f);
            float scaleY = Random.Range(2f, 4f);
            float scaleZ = Random.Range(6f, 13f);
            puff.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

            _cloudPuffs.Add(puff.transform);
            _initialOffsets.Add(puff.transform.localPosition);
        }
    }

    /// <summary>
    /// Places soft cloud banks closely floating along the perimeter of the arena.
    /// </summary>
    private void BuildPerimeterPuffs()
    {
        var perimeterRoot = new GameObject("PerimeterClouds").transform;
        perimeterRoot.SetParent(transform, false);

        // Positions along Left, Right, Bottom, Top of 40x30 arena
        Vector3[] perimeterSpots = new Vector3[]
        {
            // Left edge (x = -22 to -25)
            new Vector3(-24f, -1.2f, -10f),
            new Vector3(-25f, -0.8f, 0f),
            new Vector3(-24f, -1.4f, 10f),
            // Right edge (x = 22 to 25)
            new Vector3(24f, -1.1f, -10f),
            new Vector3(25f, -0.9f, 0f),
            new Vector3(24f, -1.3f, 10f),
            // Bottom edge (z = -17 to -20)
            new Vector3(-12f, -1.5f, -18f),
            new Vector3(0f, -1.2f, -19f),
            new Vector3(12f, -1.6f, -18f),
            // Top edge (z = 17 to 20)
            new Vector3(-12f, -1.0f, 18f),
            new Vector3(0f, -0.7f, 19f),
            new Vector3(12f, -1.1f, 18f)
        };

        for (int i = 0; i < perimeterSpots.Length; i++)
        {
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = $"PerimeterPuff_{i}";
            puff.transform.SetParent(perimeterRoot, false);

            var col = puff.GetComponent<Collider>();
            if (col != null) Destroy(col);

            puff.GetComponent<Renderer>().sharedMaterial = _cloudMat;
            puff.transform.localPosition = perimeterSpots[i];
            puff.transform.localScale = new Vector3(Random.Range(5f, 9f), Random.Range(2f, 3.5f), Random.Range(5f, 8f));

            _cloudPuffs.Add(puff.transform);
            _initialOffsets.Add(puff.transform.localPosition);
        }
    }

    private void Update()
    {
        // Gentle wind drift across the sky
        if (_deckRoot != null)
        {
            _deckRoot.Translate(Vector3.right * (driftSpeed * Time.deltaTime), Space.World);

            // Wrap deck position seamlessly when drifted too far
            if (_deckRoot.position.x > 30f)
            {
                Vector3 p = _deckRoot.position;
                p.x -= 60f;
                _deckRoot.position = p;
            }
        }

        // Subtle undulating float on each cloud puff
        float t = Time.time;
        for (int i = 0; i < _cloudPuffs.Count; i++)
        {
            if (_cloudPuffs[i] == null) continue;
            Vector3 baseOffset = _initialOffsets[i];
            float bob = Mathf.Sin(t * 0.8f + i * 0.5f) * 0.25f;
            Vector3 pos = _cloudPuffs[i].localPosition;
            pos.y = baseOffset.y + bob;
            _cloudPuffs[i].localPosition = pos;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplySkyAndFog();
    }
#endif
}
