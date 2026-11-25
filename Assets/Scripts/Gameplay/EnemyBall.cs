using System.Collections;
using UnityEngine;

/// <summary>
/// 3D bouncing enemy ball that navigates unclaimed territory.
/// Reverses velocity when encountering Wall or Captured territory.
/// Colliding with the player or the player's active trail triggers life loss.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyBall : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 4f;
    public float bounceRandomness = 0.1f;

    [Header("Visuals")]
    public Renderer ballRenderer;
    public TrailRenderer trailRenderer;

    private Rigidbody _rb;
    private Vector3 _velocity;
    private TerritoryGrid _grid;
    private bool _isSlowed = false;
    private float _baseSpeed = 4f;
    private Coroutine _slowCoroutine;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Start()
    {
        _grid = TerritoryGrid.Instance;
        _baseSpeed = speed;

        if (_velocity == Vector3.zero)
        {
            Vector2 randDir = Random.insideUnitCircle.normalized;
            if (randDir == Vector2.zero) randDir = new Vector2(0.707f, 0.707f);
            _velocity = new Vector3(randDir.x, 0f, randDir.y).normalized * speed;
        }
    }

    public void Initialize(Color color, Vector3 startPos, Vector3 startDirection, float ballSpeed)
    {
        speed = ballSpeed;
        _baseSpeed = ballSpeed;
        transform.position = startPos;

        if (startDirection == Vector3.zero)
            startDirection = new Vector3(1f, 0f, 1f).normalized;

        _velocity = startDirection.normalized * speed;

        if (ballRenderer != null)
        {
            // Tint authentic sci-fi ball material's emission to reflect level/color while keeping detailed texture
            if (ballRenderer.material != null && ballRenderer.material.HasProperty("_EmissionColor"))
            {
                ballRenderer.material.EnableKeyword("_EMISSION");
                ballRenderer.material.SetColor("_EmissionColor", color * 0.75f);
            }
        }

        if (trailRenderer != null)
        {
            trailRenderer.startColor = new Color(color.r, color.g, color.b, 0.8f);
            trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
        {
            _rb.velocity = Vector3.zero;
            return;
        }

        float currentSpeed = _isSlowed ? _baseSpeed * 0.45f : _baseSpeed;
        Vector3 displacement = _velocity.normalized * (currentSpeed * Time.fixedDeltaTime);
        Vector3 newPos = _rb.position + displacement;

        if (_grid != null)
        {
            // Predict future cell state along X and Z axes independently using ball leading edge
            const float edgeOffset = 0.45f;
            float offsetX = Mathf.Abs(_velocity.x) > 0.001f ? Mathf.Sign(_velocity.x) * edgeOffset : 0f;
            float offsetZ = Mathf.Abs(_velocity.z) > 0.001f ? Mathf.Sign(_velocity.z) * edgeOffset : 0f;

            Vector2Int checkCellX = _grid.WorldToGrid(new Vector3(newPos.x + offsetX, newPos.y, _rb.position.z));
            Vector2Int checkCellZ = _grid.WorldToGrid(new Vector3(_rb.position.x, newPos.y, newPos.z + offsetZ));

            CellState stateX = _grid.GetCellState(checkCellX.x, checkCellX.y);
            CellState stateZ = _grid.GetCellState(checkCellZ.x, checkCellZ.y);

            bool bounced = false;

            // Bounce off Captured cells and outer Walls (keep balls strictly outside safe border)
            if (stateX == CellState.Wall || stateX == CellState.Captured)
            {
                _velocity.x = -_velocity.x + Random.Range(-bounceRandomness, bounceRandomness);
                bounced = true;
            }

            if (stateZ == CellState.Wall || stateZ == CellState.Captured)
            {
                _velocity.z = -_velocity.z + Random.Range(-bounceRandomness, bounceRandomness);
                bounced = true;
            }

            if (bounced)
            {
                if (_velocity == Vector3.zero)
                    _velocity = new Vector3(Random.value > 0.5f ? 1f : -1f, 0f, Random.value > 0.5f ? 1f : -1f);

                _velocity = _velocity.normalized * currentSpeed;
                newPos = _rb.position + _velocity * Time.fixedDeltaTime;
            }

            // Check if current or new position intersects with the player's active trail
            Vector2Int currentGridCell = _grid.WorldToGrid(newPos);
            if (_grid.IsTrailAt(currentGridCell))
            {
                GameSessionManager.Instance?.OnPlayerHit();
                return;
            }

            // Keep within playable interior bounds (strictly inside outer black border)
            float borderPad = _grid.borderThickness * _grid.cellSize + 0.45f;
            float halfW = _grid.gridWidth * _grid.cellSize * 0.5f - borderPad;
            float halfH = _grid.gridHeight * _grid.cellSize * 0.5f - borderPad;
            newPos.x = Mathf.Clamp(newPos.x, -halfW, halfW);
            newPos.z = Mathf.Clamp(newPos.z, -halfH, halfH);
        }

        newPos.y = 0.5f;
        _rb.MovePosition(newPos);

        // Visual rolling rotation of the sci-fi ball across the platform
        if (_velocity.sqrMagnitude > 0.01f)
        {
            Vector3 rollAxis = Vector3.Cross(Vector3.up, _velocity.normalized);
            float rollSpeed = (currentSpeed / 0.4f) * Mathf.Rad2Deg;
            transform.Rotate(rollAxis, rollSpeed * Time.fixedDeltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameSessionManager.Instance?.OnPlayerHit();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            GameSessionManager.Instance?.OnPlayerHit();
            return;
        }

        // Reflect off solid obstacles
        if (collision.contactCount > 0)
        {
            Vector3 normal = collision.contacts[0].normal;
            normal.y = 0f;
            if (normal != Vector3.zero)
            {
                _velocity = Vector3.Reflect(_velocity, normal.normalized);
                _velocity.y = 0f;
            }
        }
    }

    public void ApplySlow(float duration)
    {
        if (_slowCoroutine != null) StopCoroutine(_slowCoroutine);
        _slowCoroutine = StartCoroutine(SlowRoutine(duration));
    }

    private IEnumerator SlowRoutine(float duration)
    {
        _isSlowed = true;
        yield return new WaitForSeconds(duration);
        _isSlowed = false;
    }
}
