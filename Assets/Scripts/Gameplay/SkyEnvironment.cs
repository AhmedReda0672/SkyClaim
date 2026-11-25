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
    public float fogStartDistance = 42f;
    public float fogEndDistance = 85f;

    [Header("Cloud Deck")]
    public int cloudCount = 36;
    public float deckHeight = -3.5f;   // below arena floor (y=0) so clouds never poke through
    public float driftSpeed = 0.55f;
    public Vector2 deckBounds = new Vector2(92f, 80f);

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
            float y = Random.Range(-1.5f, -0.2f); // keep clouds below arena floor

            puff.transform.localPosition = new Vector3(x, y, z);

            // Flattened puffy ellipsoid scale
            float scaleX = Random.Range(6f, 13f);
            float scaleY = Random.Range(1.2f, 2.5f); // flatter = stays below y=0
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

        // Positions along Left, Right, Bottom, Top of 44x34 arena ([-22..22] x [-17..17])
        Vector3[] perimeterSpots = new Vector3[]
        {
            // Left edge (x = -26 to -27)
            new Vector3(-26f, -2.5f, -11f),
            new Vector3(-27f, -2.2f, 0f),
            new Vector3(-26f, -2.6f, 11f),
            // Right edge (x = 26 to 27)
            new Vector3(26f, -2.4f, -11f),
            new Vector3(27f, -2.3f, 0f),
            new Vector3(26f, -2.5f, 11f),
            // Bottom edge (z = -20 to -21)
            new Vector3(-13f, -2.8f, -20f),
            new Vector3(0f, -2.5f, -21f),
            new Vector3(13f, -2.9f, -20f),
            // Top edge (z = 20 to 21)
            new Vector3(-13f, -2.3f, 20f),
            new Vector3(0f, -2.1f, 21f),
            new Vector3(13f, -2.4f, 20f)
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
            puff.transform.localScale = new Vector3(Random.Range(5f, 9f), Random.Range(1f, 2f), Random.Range(5f, 8f));

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
