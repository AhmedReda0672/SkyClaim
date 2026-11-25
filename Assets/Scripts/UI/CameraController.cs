using UnityEngine;

/// <summary>
/// Authentic AirXonix-style elevated isometric camera controller.
/// Frames the entire 40x30 arena grid with calibrated perspective,
/// preventing the player aircraft and enemy balls from being too close or too far.
/// Features automatic aspect-ratio compensation, optional gentle player tracking,
/// and screen shake feedback.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("AirXonix Authentic Framing")]
    [Tooltip("Camera elevation above the arena floor (44x34 optimal = 34)")]
    public float height = 34f;

    [Tooltip("Camera horizontal distance offset along Z axis (44x34 optimal = -27.5)")]
    public float distanceZ = -27.5f;

    [Tooltip("Camera downward tilt pitch angle in degrees (44x34 optimal = 53)")]
    [Range(40f, 75f)]
    public float tiltAngle = 53f;

    [Tooltip("Base vertical field of view for 16:9 displays (AirXonix optimal = 48)")]
    [Range(30f, 75f)]
    public float baseFieldOfView = 48f;

    [Header("Aspect Ratio Adaptation")]
    [Tooltip("Automatically adapt FOV so the entire arena is visible across 16:9, 16:10, 4:3, and Ultrawide")]
    public bool adaptiveAspectFraming = true;
    public float targetAspect = 16f / 9f;

    [Header("Player Tracking (Optional)")]
    [Tooltip("Keep false for authentic classic AirXonix static arena framing")]
    public bool followPlayer = false;
    public Transform target;
    [Range(1f, 10f)]
    public float followSpeed = 4f;
    [Range(0.05f, 0.5f)]
    public float followIntensity = 0.2f;

    [Header("Screen Shake Feedback")]
    private float _shakeTimeRemaining;
    private float _shakeMagnitude;

    private Camera _cam;
    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private float _lastAspect;

    public static CameraController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
        ApplyFraming();
    }

    private void Start()
    {
        ApplyFraming();
    }

    /// <summary>
    /// Applies calibrated AirXonix framing calculations to position, rotation, and FOV.
    /// </summary>
    public void ApplyFraming()
    {
        if (_cam == null) _cam = GetComponent<Camera>();

        // Set exact AirXonix look angle and position
        _basePosition = new Vector3(0f, height, distanceZ);
        _baseRotation = Quaternion.Euler(tiltAngle, 0f, 0f);

        transform.position = _basePosition;
        transform.rotation = _baseRotation;

        UpdateFieldOfView();
    }

    /// <summary>
    /// Computes dynamic vertical FOV to guarantee full horizontal arena bounds on all screens.
    /// </summary>
    private void UpdateFieldOfView()
    {
        if (_cam == null) return;

        float currentAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        _lastAspect = currentAspect;

        if (adaptiveAspectFraming && currentAspect < targetAspect)
        {
            // Expand vertical FOV for narrower aspect ratios (16:10, 4:3, 5:4)
            float baseRad = baseFieldOfView * Mathf.Deg2Rad;
            float reqTanH = targetAspect * Mathf.Tan(baseRad * 0.5f);
            float neededTanV = reqTanH / currentAspect;
            _cam.fieldOfView = 2f * Mathf.Atan(neededTanV) * Mathf.Rad2Deg;
        }
        else
        {
            _cam.fieldOfView = baseFieldOfView;
        }
    }

    private void LateUpdate()
    {
        // Check for resolution / aspect changes dynamically
        float currentAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        if (Mathf.Abs(currentAspect - _lastAspect) > 0.01f)
        {
            UpdateFieldOfView();
        }

        Vector3 targetPos = _basePosition;

        // Optional gentle player following (subtle pan)
        if (followPlayer && target != null)
        {
            Vector3 offset = new Vector3(target.position.x * followIntensity, 0f, target.position.z * followIntensity);
            targetPos += offset;
        }

        // Apply screen shake if active
        if (_shakeTimeRemaining > 0f)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * _shakeMagnitude;
            shakeOffset.y *= 0.5f; // keep vertical shake restrained
            targetPos += shakeOffset;
            _shakeTimeRemaining -= Time.deltaTime;
        }

        if (followPlayer && target != null)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPos;
        }

        transform.rotation = _baseRotation;
    }

    /// <summary>
    /// Trigger an arcade screen shake on impact or life loss.
    /// </summary>
    public void TriggerShake(float intensity = 0.35f, float duration = 0.22f)
    {
        _shakeMagnitude = intensity;
        _shakeTimeRemaining = duration;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyFraming();
    }
#endif
}
