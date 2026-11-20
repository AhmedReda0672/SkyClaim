using System.Collections;
using UnityEngine;

public enum PowerUpType
{
    ExtraLife = 0,
    ExtraTime = 1,
    Slow = 2,
    Speed = 3,
    ScoreBonus = 4
}

/// <summary>
/// Collectible power-up item with authentic AirXonix-style custom 3D representations:
/// - ExtraLife: 3D Heart (قلب)
/// - ExtraTime: 3D Twin-Bell Alarm Clock (منبه)
/// - Speed: 3D Lightning Bolt (صاعقة / برق)
/// - Slow: 3D Hourglass (ساعة رملية)
/// - ScoreBonus: 3D 5-Point Star (نجمة ذهبية)
/// </summary>
public class PowerUp : MonoBehaviour
{
    [Header("Configuration")]
    public PowerUpType type;
    public float effectDuration = 6f;
    public int bonusValue = 250;

    [Header("Visuals")]
    public Renderer powerUpRenderer;
    public Light powerUpLight;

    private bool _collected = false;
    private GameObject _modelRoot;

    private static readonly Color[] TypeColors = new Color[]
    {
        new Color(1f, 0.15f, 0.25f), // ExtraLife: Vibrant Heart Red
        new Color(0.1f, 0.85f, 1f),  // ExtraTime: Sky Cyan Clock
        new Color(0.4f, 0.8f, 1f),   // Slow: Ice Blue Hourglass
        new Color(1f, 0.95f, 0.1f),  // Speed: Electric Lightning Yellow
        new Color(1f, 0.8f, 0.1f)    // ScoreBonus: Radiant Star Gold
    };

    private void Start()
    {
        // Hide or disable any old generic primitive renderer
        if (powerUpRenderer != null)
        {
            powerUpRenderer.enabled = false;
        }

        // Build authentic 3D visual model
        Build3DVisualModel();

        // Configure light
        Color col = TypeColors[(int)type % TypeColors.Length];
        if (powerUpLight != null)
        {
            powerUpLight.color = col;
            powerUpLight.intensity = 1.6f;
            powerUpLight.range = 3.5f;
        }
        else
        {
            var lightGO = new GameObject("Light");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = col;
            light.intensity = 1.6f;
            light.range = 3.5f;
            powerUpLight = light;
        }

        StartCoroutine(BobAndSpinRoutine());
    }

    private void Build3DVisualModel()
    {
        Color mainColor = TypeColors[(int)type % TypeColors.Length];
        Material mainMat = CreateEmissiveMaterial($"PowerUpMat_{type}", mainColor, 0.75f);
        Material whiteMat = CreateEmissiveMaterial("PowerUpWhite", Color.white, 0.3f);
        Material darkMat = CreateEmissiveMaterial("PowerUpDark", new Color(0.12f, 0.12f, 0.15f), 0.1f);
        Material goldMat = CreateEmissiveMaterial("PowerUpGold", new Color(1f, 0.85f, 0.2f), 0.8f);

        switch (type)
        {
            case PowerUpType.ExtraLife:
                _modelRoot = CreateHeartModel(mainMat);
                break;

            case PowerUpType.ExtraTime:
                _modelRoot = CreateAlarmClockModel(mainMat, whiteMat, darkMat, goldMat);
                break;

            case PowerUpType.Speed:
                _modelRoot = CreateLightningBoltModel(mainMat);
                break;

            case PowerUpType.Slow:
                _modelRoot = CreateHourglassModel(mainMat, darkMat, goldMat);
                break;

            case PowerUpType.ScoreBonus:
                _modelRoot = CreateStarModel(mainMat, goldMat);
                break;
        }

        // Add a floating billboard emoji badge above the 3D model for instant recognition
        CreateBillboardBadge();
    }

    #region 3D Model Constructors

    /// <summary>
    /// Builds a 3D Heart (قلب) using two spherical lobes and a tapered bottom body.
    /// </summary>
    private GameObject CreateHeartModel(Material mat)
    {
        var root = new GameObject("Heart3D");
        root.transform.SetParent(transform, false);

        // Left lobe
        var leftLobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftLobe.name = "LeftLobe";
        leftLobe.transform.SetParent(root.transform, false);
        leftLobe.transform.localPosition = new Vector3(-0.16f, 0.14f, 0f);
        leftLobe.transform.localScale = new Vector3(0.42f, 0.42f, 0.35f);
        StripCollider(leftLobe);
        leftLobe.GetComponent<Renderer>().sharedMaterial = mat;

        // Right lobe
        var rightLobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightLobe.name = "RightLobe";
        rightLobe.transform.SetParent(root.transform, false);
        rightLobe.transform.localPosition = new Vector3(0.16f, 0.14f, 0f);
        rightLobe.transform.localScale = new Vector3(0.42f, 0.42f, 0.35f);
        StripCollider(rightLobe);
        rightLobe.GetComponent<Renderer>().sharedMaterial = mat;

        // Center body rotated 45 deg
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        body.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        body.transform.localScale = new Vector3(0.42f, 0.42f, 0.32f);
        StripCollider(body);
        body.GetComponent<Renderer>().sharedMaterial = mat;

        // Bottom point tip
        var tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tip.name = "Tip";
        tip.transform.SetParent(root.transform, false);
        tip.transform.localPosition = new Vector3(0f, -0.2f, 0f);
        tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        tip.transform.localScale = new Vector3(0.26f, 0.26f, 0.3f);
        StripCollider(tip);
        tip.GetComponent<Renderer>().sharedMaterial = mat;

        root.transform.localScale = Vector3.one * 1.35f;
        return root;
    }

    /// <summary>
    /// Builds a 3D Twin-Bell Alarm Clock (منبه ⏰) with body, clock face, hands, twin bells, handle, and feet.
    /// </summary>
    private GameObject CreateAlarmClockModel(Material bodyMat, Material faceMat, Material handMat, Material bellMat)
    {
        var root = new GameObject("AlarmClock3D");
        root.transform.SetParent(transform, false);

        // Main cylindrical clock body
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "ClockBody";
        body.transform.SetParent(root.transform, false);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.65f, 0.14f, 0.65f);
        StripCollider(body);
        body.GetComponent<Renderer>().sharedMaterial = bodyMat;

        // Front clock face disc
        var face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        face.name = "ClockFace";
        face.transform.SetParent(root.transform, false);
        face.transform.localPosition = new Vector3(0f, 0f, 0.09f);
        face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        face.transform.localScale = new Vector3(0.54f, 0.06f, 0.54f);
        StripCollider(face);
        face.GetComponent<Renderer>().sharedMaterial = faceMat;

        // Hour hand (pointing to 10)
        var hourHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hourHand.name = "HourHand";
        hourHand.transform.SetParent(root.transform, false);
        hourHand.transform.localPosition = new Vector3(-0.06f, 0.07f, 0.14f);
        hourHand.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        hourHand.transform.localScale = new Vector3(0.04f, 0.15f, 0.03f);
        StripCollider(hourHand);
        hourHand.GetComponent<Renderer>().sharedMaterial = handMat;

        // Minute hand (pointing to 2)
        var minHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        minHand.name = "MinHand";
        minHand.transform.SetParent(root.transform, false);
        minHand.transform.localPosition = new Vector3(0.06f, 0.09f, 0.14f);
        minHand.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
        minHand.transform.localScale = new Vector3(0.035f, 0.2f, 0.03f);
        StripCollider(minHand);
        minHand.GetComponent<Renderer>().sharedMaterial = handMat;

        // Left Bell
        var leftBell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftBell.name = "LeftBell";
        leftBell.transform.SetParent(root.transform, false);
        leftBell.transform.localPosition = new Vector3(-0.26f, 0.35f, 0f);
        leftBell.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        StripCollider(leftBell);
        leftBell.GetComponent<Renderer>().sharedMaterial = bellMat;

        // Right Bell
        var rightBell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightBell.name = "RightBell";
        rightBell.transform.SetParent(root.transform, false);
        rightBell.transform.localPosition = new Vector3(0.26f, 0.35f, 0f);
        rightBell.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        StripCollider(rightBell);
        rightBell.GetComponent<Renderer>().sharedMaterial = bellMat;

        // Top center handle
        var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Handle";
        handle.transform.SetParent(root.transform, false);
        handle.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        handle.transform.localScale = new Vector3(0.18f, 0.05f, 0.08f);
        StripCollider(handle);
        handle.GetComponent<Renderer>().sharedMaterial = bellMat;

        // Left foot
        var leftFoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftFoot.name = "LeftFoot";
        leftFoot.transform.SetParent(root.transform, false);
        leftFoot.transform.localPosition = new Vector3(-0.22f, -0.35f, 0f);
        leftFoot.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
        leftFoot.transform.localScale = new Vector3(0.08f, 0.12f, 0.08f);
        StripCollider(leftFoot);
        leftFoot.GetComponent<Renderer>().sharedMaterial = bellMat;

        // Right foot
        var rightFoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightFoot.name = "RightFoot";
        rightFoot.transform.SetParent(root.transform, false);
        rightFoot.transform.localPosition = new Vector3(0.22f, -0.35f, 0f);
        rightFoot.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
        rightFoot.transform.localScale = new Vector3(0.08f, 0.12f, 0.08f);
        StripCollider(rightFoot);
        rightFoot.GetComponent<Renderer>().sharedMaterial = bellMat;

        root.transform.localScale = Vector3.one * 1.3f;
        return root;
    }

    /// <summary>
    /// Builds a 3D Lightning Bolt (صاعقة / برق ⚡) with sharp angled segments and glowing tip.
    /// </summary>
    private GameObject CreateLightningBoltModel(Material boltMat)
    {
        var root = new GameObject("Lightning3D");
        root.transform.SetParent(transform, false);

        // Top diagonal segment
        var topSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topSegment.name = "TopSegment";
        topSegment.transform.SetParent(root.transform, false);
        topSegment.transform.localPosition = new Vector3(0.08f, 0.25f, 0f);
        topSegment.transform.localRotation = Quaternion.Euler(0f, 0f, 26f);
        topSegment.transform.localScale = new Vector3(0.13f, 0.38f, 0.18f);
        StripCollider(topSegment);
        topSegment.GetComponent<Renderer>().sharedMaterial = boltMat;

        // Middle horizontal jog
        var midSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        midSegment.name = "MidSegment";
        midSegment.transform.SetParent(root.transform, false);
        midSegment.transform.localPosition = new Vector3(0.02f, 0.03f, 0f);
        midSegment.transform.localRotation = Quaternion.Euler(0f, 0f, -15f);
        midSegment.transform.localScale = new Vector3(0.28f, 0.1f, 0.18f);
        StripCollider(midSegment);
        midSegment.GetComponent<Renderer>().sharedMaterial = boltMat;

        // Lower diagonal segment
        var lowerSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lowerSegment.name = "LowerSegment";
        lowerSegment.transform.SetParent(root.transform, false);
        lowerSegment.transform.localPosition = new Vector3(-0.08f, -0.18f, 0f);
        lowerSegment.transform.localRotation = Quaternion.Euler(0f, 0f, 26f);
        lowerSegment.transform.localScale = new Vector3(0.13f, 0.38f, 0.18f);
        StripCollider(lowerSegment);
        lowerSegment.GetComponent<Renderer>().sharedMaterial = boltMat;

        // Sharp strike tip
        var tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tip.name = "Tip";
        tip.transform.SetParent(root.transform, false);
        tip.transform.localPosition = new Vector3(-0.16f, -0.34f, 0f);
        tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        tip.transform.localScale = new Vector3(0.1f, 0.1f, 0.16f);
        StripCollider(tip);
        tip.GetComponent<Renderer>().sharedMaterial = boltMat;

        root.transform.localScale = Vector3.one * 1.45f;
        return root;
    }

    /// <summary>
    /// Builds a 3D Hourglass (ساعة رملية ⏳) representing the Slow power-up.
    /// </summary>
    private GameObject CreateHourglassModel(Material glassMat, Material frameMat, Material sandMat)
    {
        var root = new GameObject("Hourglass3D");
        root.transform.SetParent(transform, false);

        // Top cap plate
        var topPlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        topPlate.name = "TopPlate";
        topPlate.transform.SetParent(root.transform, false);
        topPlate.transform.localPosition = new Vector3(0f, 0.32f, 0f);
        topPlate.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
        StripCollider(topPlate);
        topPlate.GetComponent<Renderer>().sharedMaterial = frameMat;

        // Bottom cap plate
        var botPlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        botPlate.name = "BotPlate";
        botPlate.transform.SetParent(root.transform, false);
        botPlate.transform.localPosition = new Vector3(0f, -0.32f, 0f);
        botPlate.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
        StripCollider(botPlate);
        botPlate.GetComponent<Renderer>().sharedMaterial = frameMat;

        // Top glass bulb
        var topBulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        topBulb.name = "TopBulb";
        topBulb.transform.SetParent(root.transform, false);
        topBulb.transform.localPosition = new Vector3(0f, 0.14f, 0f);
        topBulb.transform.localScale = new Vector3(0.38f, 0.32f, 0.38f);
        StripCollider(topBulb);
        topBulb.GetComponent<Renderer>().sharedMaterial = glassMat;

        // Bottom glass bulb
        var botBulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        botBulb.name = "BotBulb";
        botBulb.transform.SetParent(root.transform, false);
        botBulb.transform.localPosition = new Vector3(0f, -0.14f, 0f);
        botBulb.transform.localScale = new Vector3(0.38f, 0.32f, 0.38f);
        StripCollider(botBulb);
        botBulb.GetComponent<Renderer>().sharedMaterial = glassMat;

        // Central narrow waist
        var waist = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        waist.name = "Waist";
        waist.transform.SetParent(root.transform, false);
        waist.transform.localPosition = Vector3.zero;
        waist.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
        StripCollider(waist);
        waist.GetComponent<Renderer>().sharedMaterial = frameMat;

        // Golden sand sphere inside
        var sand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sand.name = "Sand";
        sand.transform.SetParent(root.transform, false);
        sand.transform.localPosition = new Vector3(0f, -0.16f, 0f);
        sand.transform.localScale = new Vector3(0.24f, 0.2f, 0.24f);
        StripCollider(sand);
        sand.GetComponent<Renderer>().sharedMaterial = sandMat;

        // Left pillar
        var p1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        p1.transform.SetParent(root.transform, false);
        p1.transform.localPosition = new Vector3(-0.21f, 0f, 0f);
        p1.transform.localScale = new Vector3(0.04f, 0.32f, 0.04f);
        StripCollider(p1);
        p1.GetComponent<Renderer>().sharedMaterial = frameMat;

        // Right pillar
        var p2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        p2.transform.SetParent(root.transform, false);
        p2.transform.localPosition = new Vector3(0.21f, 0f, 0f);
        p2.transform.localScale = new Vector3(0.04f, 0.32f, 0.04f);
        StripCollider(p2);
        p2.GetComponent<Renderer>().sharedMaterial = frameMat;

        root.transform.localScale = Vector3.one * 1.3f;
        return root;
    }

    /// <summary>
    /// Builds a 3D 5-Pointed Star (نجمة ذهبية ⭐) representing Score Bonus.
    /// </summary>
    private GameObject CreateStarModel(Material starMat, Material coreMat)
    {
        var root = new GameObject("Star3D");
        root.transform.SetParent(transform, false);

        // Central faceted diamond core
        var core = GameObject.CreatePrimitive(PrimitiveType.Cube);
        core.name = "Core";
        core.transform.SetParent(root.transform, false);
        core.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        core.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
        StripCollider(core);
        core.GetComponent<Renderer>().sharedMaterial = coreMat;

        // 5 Star rays pointing outward at 72 degree intervals
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 72f * Mathf.Deg2Rad + Mathf.PI * 0.5f;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            var ray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ray.name = $"StarRay_{i}";
            ray.transform.SetParent(root.transform, false);
            ray.transform.localPosition = dir * 0.22f;
            ray.transform.localRotation = Quaternion.Euler(0f, 0f, i * 72f) * Quaternion.Euler(0f, 0f, 45f);
            ray.transform.localScale = new Vector3(0.2f, 0.2f, 0.18f);
            StripCollider(ray);
            ray.GetComponent<Renderer>().sharedMaterial = starMat;
        }

        root.transform.localScale = Vector3.one * 1.4f;
        return root;
    }

    #endregion

    /// <summary>
    /// Floating billboard badge above the power-up showing the exact symbol.
    /// </summary>
    private void CreateBillboardBadge()
    {
        string[] symbols = { "❤", "⏱", "❄", "⚡", "★" };
        string symbol = symbols[(int)type % symbols.Length];

        var badgeGO = new GameObject("BillboardBadge");
        badgeGO.transform.SetParent(transform, false);
        badgeGO.transform.localPosition = new Vector3(0f, 0.85f, 0f);

        var tm = badgeGO.AddComponent<TextMesh>();
        tm.text = symbol;
        tm.fontSize = 48;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = TypeColors[(int)type % TypeColors.Length];

        // Billboard script to face camera
        badgeGO.AddComponent<BillboardFaceCamera>();
    }

    private static Material CreateEmissiveMaterial(string name, Color color, float emissionIntensity)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * emissionIntensity);
        return mat;
    }

    private static void StripCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    private IEnumerator BobAndSpinRoutine()
    {
        Vector3 basePos = transform.position;
        float elapsed = 0f;

        while (!_collected)
        {
            elapsed += Time.deltaTime;
            // Smooth vertical floating bob
            transform.position = basePos + Vector3.up * (Mathf.Sin(elapsed * 3f) * 0.15f);

            // Rotate the 3D model around its vertical axis
            if (_modelRoot != null)
            {
                _modelRoot.transform.Rotate(Vector3.up, 100f * Time.deltaTime, Space.Self);
            }
            else
            {
                transform.Rotate(Vector3.up, 100f * Time.deltaTime, Space.World);
            }

            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (other.CompareTag("Player"))
        {
            _collected = true;
            Collect();
        }
    }

    private void Collect()
    {
        AudioManager.Instance?.PlaySFX("powerup");
        ScoreManager.Instance?.AwardPowerUp();

        switch (type)
        {
            case PowerUpType.ExtraLife:
                if (GameManager.Instance != null)
                {
                    int current = GameManager.Instance.TotalLives;
                    if (current < 5)
                    {
                        GameManager.Instance.AddLife();
                    }
                }
                break;

            case PowerUpType.ExtraTime:
                TimerManager.Instance?.AddTime(30f);
                break;

            case PowerUpType.Slow:
                EnemySpawner.Instance?.ApplySlowToAll(effectDuration);
                break;

            case PowerUpType.Speed:
                PlayerController.Instance?.ApplySpeedBoost(effectDuration);
                break;

            case PowerUpType.ScoreBonus:
                GameManager.Instance?.AddScore(bonusValue);
                break;
        }

        UIManager.Instance?.ShowPowerUpIndicator(type, effectDuration);
        Destroy(gameObject);
    }
}

/// <summary>
/// Simple billboard component keeping floating badges facing the camera.
/// </summary>
public class BillboardFaceCamera : MonoBehaviour
{
    private Transform _camTransform;

    private void Start()
    {
        if (Camera.main != null)
            _camTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (_camTransform == null && Camera.main != null)
            _camTransform = Camera.main.transform;

        if (_camTransform != null)
        {
            transform.rotation = _camTransform.rotation;
        }
    }
}
