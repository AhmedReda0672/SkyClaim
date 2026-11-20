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
    }

    private void Start()
    {
        _grid = TerritoryGrid.Instance;
        Vector2 dir = Random.insideUnitCircle.normalized;
        if (dir == Vector2.zero) dir = new Vector2(1f, 1f).normalized;
        _velocity = new Vector3(dir.x, 0f, dir.y).normalized * speed;

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
            Vector2Int checkCellX = _grid.WorldToGrid(new Vector3(newPos.x, 0f, _rb.position.z));
            Vector2Int checkCellZ = _grid.WorldToGrid(new Vector3(_rb.position.x, 0f, newPos.z));

            CellState stateX = _grid.GetCellState(checkCellX.x, checkCellX.y);
            CellState stateZ = _grid.GetCellState(checkCellZ.x, checkCellZ.y);

            // Mines only navigate CAPTURED cells — bounce off Unclaimed, Trail, and Wall
            if (stateX != CellState.Captured)
            {
                _velocity.x = -_velocity.x;
            }

            if (stateZ != CellState.Captured)
            {
                _velocity.z = -_velocity.z;
            }

            if (_velocity != Vector3.zero)
                _velocity = _velocity.normalized * speed;

            newPos = _rb.position + _velocity * Time.fixedDeltaTime;
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
