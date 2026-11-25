using System.Collections;
using UnityEngine;

/// <summary>
/// Mine hazard that patrols across captured territory.
/// Reverses direction when encountering Unclaimed cells or arena walls.
/// Damages the player on contact.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MineController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2.5f;

    [Header("Visuals")]
    public Renderer mineRenderer;
    public ParticleSystem explosionParticles;

    private Rigidbody _rb;
    private Vector3 _velocity;
    private TerritoryGrid _grid;
    private bool _hasDetonated = false;

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
        // Pick a lively diagonal angle so mines weave across the black border and enter blue captured zones
        float[] angles = { 35f, 55f, 125f, 145f, 215f, 235f, 305f, 325f };
        float rad = angles[Random.Range(0, angles.Length)] * Mathf.Deg2Rad;
        _velocity = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized * speed;

        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        while (!_hasDetonated)
        {
            transform.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);
            yield return null;
        }
    }

    private void FixedUpdate()
    {
        if (_hasDetonated) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            return;

        Vector3 newPos = _rb.position + _velocity * Time.fixedDeltaTime;

        if (_grid != null)
        {
            const float edgeOffset = 0.25f;
            float offsetX = Mathf.Abs(_velocity.x) > 0.001f ? Mathf.Sign(_velocity.x) * edgeOffset : 0f;
            float offsetZ = Mathf.Abs(_velocity.z) > 0.001f ? Mathf.Sign(_velocity.z) * edgeOffset : 0f;

            // Check unclamped world coordinates so outer grid edges properly bounce inward
            bool safeX = _grid.IsSafeWorldPos(newPos.x + offsetX, _rb.position.z);
            bool safeZ = _grid.IsSafeWorldPos(_rb.position.x, newPos.z + offsetZ);

            // Mines patrol safe border (Wall) + Captured cells — bounce off Unclaimed, Trail, and outer void
            if (!safeX)
                _velocity.x = -_velocity.x;

            if (!safeZ)
                _velocity.z = -_velocity.z;

            if (safeX && safeZ && !_grid.IsSafeWorldPos(newPos.x + offsetX, newPos.z + offsetZ))
            {
                _velocity.x = -_velocity.x;
                _velocity.z = -_velocity.z;
            }

            if (_velocity != Vector3.zero)
                _velocity = _velocity.normalized * speed;

            float stepX = safeX ? _velocity.x * Time.fixedDeltaTime : 0f;
            float stepZ = safeZ ? _velocity.z * Time.fixedDeltaTime : 0f;
            newPos = _rb.position + new Vector3(stepX, 0f, stepZ);

            if (!_grid.IsSafeWorldPos(newPos.x, newPos.z))
                newPos = _rb.position;

            float halfW = _grid.gridWidth * _grid.cellSize * 0.5f - edgeOffset;
            float halfH = _grid.gridHeight * _grid.cellSize * 0.5f - edgeOffset;
            newPos.x = Mathf.Clamp(newPos.x, -halfW, halfW);
            newPos.z = Mathf.Clamp(newPos.z, -halfH, halfH);
        }

        newPos.y = 0.5f;
        _rb.MovePosition(newPos);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasDetonated) return;

        if (other.CompareTag("Player"))
        {
            Detonate();
        }
    }

    private void Detonate()
    {
        _hasDetonated = true;

        if (explosionParticles != null)
        {
            explosionParticles.transform.position = transform.position;
            explosionParticles.Play();
        }

        AudioManager.Instance?.PlaySFX("mine");
        GameSessionManager.Instance?.OnPlayerHit();

        gameObject.SetActive(false);
    }
}
