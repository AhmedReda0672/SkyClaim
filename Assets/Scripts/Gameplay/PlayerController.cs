using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float boostedSpeed = 9f;

    [Header("References")]
    public Transform propeller;
    public ParticleSystem deathParticles;

    public static PlayerController Instance { get; private set; }

    private Rigidbody _rb;
    private float _currentSpeed;
    private bool _isBoosted = false;
    private bool _isAlive = false;
    private Vector2Int _lastGridPos;
    private TerritoryGrid _grid;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezePositionY |
                          RigidbodyConstraints.FreezeRotationX |
                          RigidbodyConstraints.FreezeRotationY |
                          RigidbodyConstraints.FreezeRotationZ;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _currentSpeed = moveSpeed;
    }

    private void Start()
    {
        _grid = TerritoryGrid.Instance;
    }

    private void Update()
    {
        if (!_isAlive) return;

        // Propeller rotation
        if (propeller != null)
        {
            propeller.Rotate(Vector3.right, 1200f * Time.deltaTime, Space.Self);
        }

        // Pause input
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState == GameState.Playing)
                    GameManager.Instance.PauseGame();
                else if (GameManager.Instance.CurrentState == GameState.Paused)
                    GameManager.Instance.ResumeGame();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!_isAlive) return;
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameState.Playing &&
            GameManager.Instance.CurrentState != GameState.LifeLost) return;

        // Movement — ARROW KEYS ONLY (no WASD)
        Vector3 moveDir = Vector3.zero;

        if (Input.GetKey(KeyCode.UpArrow))    moveDir = Vector3.forward;
        else if (Input.GetKey(KeyCode.DownArrow))  moveDir = Vector3.back;
        else if (Input.GetKey(KeyCode.LeftArrow))  moveDir = Vector3.left;
        else if (Input.GetKey(KeyCode.RightArrow)) moveDir = Vector3.right;

        if (moveDir != Vector3.zero)
        {
            _rb.velocity = moveDir * _currentSpeed;

            // Smoothly orient aircraft towards movement direction with flight banking tilt
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            if (moveDir == Vector3.left)
                targetRot *= Quaternion.Euler(0, 0, 18f);
            else if (moveDir == Vector3.right)
                targetRot *= Quaternion.Euler(0, 0, -18f);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 720f * Time.fixedDeltaTime);
        }
        else
        {
            _rb.velocity = Vector3.zero;
        }

        // Clamp within grid bounds
        ClampPosition();

        // Check if grid cell changed
        if (_grid != null)
        {
            Vector2Int currentGridPos = _grid.WorldToGrid(transform.position);
            if (currentGridPos != _lastGridPos)
            {
                _lastGridPos = currentGridPos;
                _grid.TryAddTrail(currentGridPos);
            }
        }
    }

    private void ClampPosition()
    {
        if (_grid == null) return;

        Vector3 pos = transform.position;
        float halfW = _grid.gridWidth * _grid.cellSize * 0.5f;
        float halfH = _grid.gridHeight * _grid.cellSize * 0.5f;
        float margin = _grid.cellSize * 0.5f;

        pos.x = Mathf.Clamp(pos.x, -halfW + margin, halfW - margin);
        pos.z = Mathf.Clamp(pos.z, -halfH + margin, halfH - margin);
        pos.y = 0.5f; // Keep at fixed height

        transform.position = pos;
    }

    public void Respawn(Vector3 position)
    {
        position.y = 0.5f;
        gameObject.SetActive(true);
        transform.position = position;
        transform.rotation = Quaternion.identity;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _isAlive = true;

        if (_grid != null)
            _lastGridPos = _grid.WorldToGrid(position);
    }

    public void PlayDeathAnimation()
    {
        _isAlive = false;
        _rb.velocity = Vector3.zero;

        if (deathParticles != null)
        {
            deathParticles.transform.position = transform.position;
            deathParticles.Play();
        }

        gameObject.SetActive(false);
        AudioManager.Instance?.PlaySFX("death");
    }

    public void ApplySpeedBoost(float duration)
    {
        StartCoroutine(SpeedBoostCoroutine(duration));
    }

    private IEnumerator SpeedBoostCoroutine(float duration)
    {
        _isBoosted = true;
        _currentSpeed = boostedSpeed;
        yield return new WaitForSeconds(duration);
        _isBoosted = false;
        _currentSpeed = moveSpeed;
    }

    public bool IsAlive => _isAlive;
}
