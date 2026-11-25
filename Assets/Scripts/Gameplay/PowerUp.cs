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
/// Collectible 3D Power-Up item — floats above the arena, never clips below floor.
/// When the tile it sits on becomes captured, it relocates to the nearest unclaimed tile.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PowerUp : MonoBehaviour
{
    [Header("Configuration")]
    public PowerUpType type;
    public float effectDuration = 6f;
    public int bonusValue = 250;

    [Header("Visuals")]
    public Renderer powerUpRenderer;
    public Light powerUpLight;

    [Header("Animation")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.05f;
    public float spinSpeed = 80f;

    // Floor is at y = 0; powerup floats safely above it
    private const float FloorY = 0.75f;

    private bool _collected = false;
    private Vector3 _startPosition;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = true;
        _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
    }

    private void Start()
    {
        // Snap to safe height immediately
        Vector3 p = transform.position;
        p.y = FloorY;
        transform.position = p;
        _startPosition = p;

        // Auto-configure light glow if present
        if (powerUpLight != null)
        {
            Color[] colors = new Color[]
            {
                new Color(1f, 0.2f, 0.3f), // Heart — Red
                new Color(0.2f, 0.9f, 1f), // Time — Cyan
                new Color(0.3f, 0.6f, 1f), // Slow — Ice Blue
                new Color(1f, 0.6f, 0.1f), // Speed — Orange
                new Color(1f, 0.85f, 0.1f) // Coin — Gold
            };
            powerUpLight.color = colors[(int)type % colors.Length];
            powerUpLight.intensity = 1.2f;
            powerUpLight.range = 2.5f;
        }

        // Listen for territory captures
        if (TerritoryGrid.Instance != null)
            TerritoryGrid.Instance.OnCellsCaptured += OnTerritoryCaptured;

        StartCoroutine(BobAndSpinRoutine());
    }

    private void OnDestroy()
    {
        if (TerritoryGrid.Instance != null)
            TerritoryGrid.Instance.OnCellsCaptured -= OnTerritoryCaptured;
    }

    // ── Called whenever cells are captured ──────────────────────────────────
    private void OnTerritoryCaptured(System.Collections.Generic.List<Vector3> capturedPositions)
    {
        if (_collected) return;

        // Check if our current tile is now captured
        TerritoryGrid grid = TerritoryGrid.Instance;
        if (grid == null) return;

        Vector2Int myCell = grid.WorldToGrid(transform.position);
        if (grid.GetCellState(myCell.x, myCell.y) != CellState.Captured) return;

        // Find a new unclaimed position and move there
        Vector3 newPos = grid.GetRandomUnclaimedPosition();
        if (newPos == Vector3.zero) return;

        newPos.y = FloorY;
        transform.position = newPos;
        _startPosition = newPos;
    }

    private IEnumerator BobAndSpinRoutine()
    {
        float elapsed = Random.value * Mathf.PI * 2f;
        while (!_collected)
        {
            elapsed += Time.deltaTime;

            Vector3 pos = transform.position;
            // Always clamp above floor so it never dips through
            pos.y = Mathf.Max(FloorY - bobHeight, _startPosition.y + Mathf.Sin(elapsed * bobSpeed) * bobHeight);
            transform.position = pos;

            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            yield return null;
        }
    }

    // ── Collection ──────────────────────────────────────────────────────────
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
                if (GameManager.Instance != null && GameManager.Instance.TotalLives < 5)
                    GameManager.Instance.AddLife();
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

