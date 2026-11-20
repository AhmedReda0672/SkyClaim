using System;
using System.Collections.Generic;
using UnityEngine;

public enum CellState
{
    Unclaimed = 0,
    Captured = 1,
    Trail = 2,
    Wall = 3
}

public class TerritoryGrid : MonoBehaviour
{
    public static TerritoryGrid Instance { get; private set; }

    [Header("Grid Settings")]
    public int gridWidth = 40;
    public int gridHeight = 30;
    public float cellSize = 1f;

    [Header("Materials")]
    public Material capturedMaterial;
    public Material unclaimedMaterial;
    public Material trailMaterial;
    public Material wallMaterial;

    // Grid state
    public CellState[,] Grid { get; private set; }
    private int _totalPlayableCells;
    private int _capturedCells;

    // Trail tracking
    private List<Vector2Int> _currentTrail = new List<Vector2Int>();
    private bool _isDrawingTrail = false;

    // Visuals
    private GameObject[,] _cellObjects;
    private Transform _gridParent;

    // Events
    public event Action<float> OnCapturedPercentChanged;

    // Enemy reference cache
    private EnemyBall[] _cachedEnemies;
    private float _enemyCacheTime = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize()
    {
        Instance = this;

        if (FindObjectOfType<SkyEnvironment>() == null)
        {
            var skyGO = new GameObject("SkyEnvironment");
            skyGO.AddComponent<SkyEnvironment>();
        }

        // Clean up old visuals
        if (_gridParent != null)
            Destroy(_gridParent.gameObject);

        _gridParent = new GameObject("GridVisuals").transform;
        _gridParent.SetParent(transform);

        Grid = new CellState[gridWidth, gridHeight];
        _cellObjects = new GameObject[gridWidth, gridHeight];
        _currentTrail = new List<Vector2Int>();
        _isDrawingTrail = false;

        // Set cell states
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (x == 0 || y == 0 || x == gridWidth - 1 || y == gridHeight - 1)
                {
                    // Outer border = Wall
                    Grid[x, y] = CellState.Wall;
                }
                else if (x == 1 || y == 1 || x == gridWidth - 2 || y == gridHeight - 2)
                {
                    // Inner border = Captured (starting territory)
                    Grid[x, y] = CellState.Captured;
                }
                else
                {
                    Grid[x, y] = CellState.Unclaimed;
                }
            }
        }

        // Count cells
        CountCells();

        // Build visual tiles
        BuildVisuals();

        OnCapturedPercentChanged?.Invoke(GetCapturedPercent());
    }

    private void BuildVisuals()
    {
        Vector3 origin = GetOrigin();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Create a Quad for each cell
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"Cell_{x}_{y}";
                quad.transform.SetParent(_gridParent, false);

                // Position: rotate 90 degrees on X so it lies flat
                Vector3 worldPos = origin + new Vector3(x * cellSize + cellSize * 0.5f, 0.01f, y * cellSize + cellSize * 0.5f);
                quad.transform.position = worldPos;
                quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                quad.transform.localScale = new Vector3(cellSize * 0.98f, cellSize * 0.98f, 1f);

                // Remove collider from visuals
                Destroy(quad.GetComponent<MeshCollider>());

                _cellObjects[x, y] = quad;
                ApplyMaterial(x, y);
            }
        }
    }

    private void ApplyMaterial(int x, int y)
    {
        if (_cellObjects[x, y] == null) return;
        var renderer = _cellObjects[x, y].GetComponent<Renderer>();
        if (renderer == null) return;

        switch (Grid[x, y])
        {
            case CellState.Wall:
                renderer.material = wallMaterial != null ? wallMaterial : GetFallbackMaterial(Color.gray);
                break;
            case CellState.Captured:
                renderer.material = capturedMaterial != null ? capturedMaterial : GetFallbackMaterial(new Color(0.2f, 0.6f, 1f, 0.8f));
                break;
            case CellState.Trail:
                renderer.material = trailMaterial != null ? trailMaterial : GetFallbackMaterial(Color.yellow);
                break;
            case CellState.Unclaimed:
                renderer.material = unclaimedMaterial != null ? unclaimedMaterial : GetFallbackMaterial(new Color(0.1f, 0.1f, 0.15f, 1f));
                break;
        }
    }

    private Material GetFallbackMaterial(Color color)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        if (color.a < 1f)
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        return mat;
    }

    private void RefreshAllVisuals()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                ApplyMaterial(x, y);
    }

    private void CountCells()
    {
        _totalPlayableCells = 0;
        _capturedCells = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (Grid[x, y] == CellState.Unclaimed || Grid[x, y] == CellState.Captured)
                {
                    _totalPlayableCells++;
                    if (Grid[x, y] == CellState.Captured)
                        _capturedCells++;
                }
            }
        }
    }

    // ─── Trail Logic ───────────────────────────────────────────────────────────

    public bool TryAddTrail(Vector2Int gridPos)
    {
        int x = gridPos.x;
        int y = gridPos.y;

        // Bounds check
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return false;

        CellState state = Grid[x, y];

        if (state == CellState.Wall)
        {
            // Can't enter wall
            return false;
        }

        if (state == CellState.Trail)
        {
            // Self-collision — only matters if we're actually drawing a trail
            if (_isDrawingTrail && _currentTrail.Count > 1)
            {
                GameSessionManager.Instance?.OnPlayerHit();
                return false;
            }
            return false;
        }

        if (state == CellState.Unclaimed)
        {
            // Start or extend trail
            Grid[x, y] = CellState.Trail;
            _currentTrail.Add(gridPos);
            _isDrawingTrail = true;
            ApplyMaterial(x, y);
            AudioManager.Instance?.PlaySFX("trail");
            return true;
        }

        if (state == CellState.Captured)
        {
            if (_isDrawingTrail && _currentTrail.Count > 0)
            {
                // Player returned to captured territory — close the trail
                CloseTrail();
            }
            return true;
        }

        return false;
    }

    public void CloseTrail()
    {
        if (_currentTrail.Count == 0)
        {
            _isDrawingTrail = false;
            return;
        }

        // Convert all trail cells to Captured
        foreach (var pos in _currentTrail)
        {
            Grid[pos.x, pos.y] = CellState.Captured;
        }

        // Run flood fill to capture enclosed areas
        int newlyCaptured = RunFloodFillCapture();

        // Add the trail cells themselves to the count
        newlyCaptured += _currentTrail.Count;

        // Clear trail
        _currentTrail.Clear();
        _isDrawingTrail = false;

        // Refresh visuals and counts
        RefreshAllVisuals();
        CountCells();

        // Fire event
        float percent = GetCapturedPercent();
        OnCapturedPercentChanged?.Invoke(percent);

        // Award score
        if (ScoreManager.Instance != null && newlyCaptured > 0)
        {
            ScoreManager.Instance.AwardCapturedCells(newlyCaptured);
        }

        // Play sound
        AudioManager.Instance?.PlaySFX("capture");
    }

    /// <summary>
    /// Connected-component flood-fill capture algorithm (AirXonix rules):
    /// 1. Partition all Unclaimed cells into connected components.
    /// 2. For each component, determine if any active enemy ball resides inside it.
    /// 3. Any component containing NO enemy balls is an enclosed territory and becomes Captured.
    /// 4. Components containing enemy balls remain Unclaimed.
    /// 5. If no enemies exist anywhere (e.g. tests), capture the smaller component(s).
    /// </summary>
    private int RunFloodFillCapture()
    {
        // 1. Gather grid positions of all active enemy balls
        HashSet<Vector2Int> enemyCells = new HashSet<Vector2Int>();
        RefreshEnemyCache();

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        if (_cachedEnemies != null)
        {
            foreach (var enemy in _cachedEnemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                Vector2Int ec = WorldToGrid(enemy.transform.position);
                enemyCells.Add(ec);
                // Also account for ball radius by including adjacent cells
                for (int d = 0; d < 4; d++)
                {
                    enemyCells.Add(new Vector2Int(ec.x + dx[d], ec.y + dy[d]));
                }
            }
        }

        // 2. Discover all connected components of Unclaimed cells
        bool[,] visited = new bool[gridWidth, gridHeight];
        List<List<Vector2Int>> components = new List<List<Vector2Int>>();
        List<bool> componentHasEnemy = new List<bool>();

        for (int x = 1; x < gridWidth - 1; x++)
        {
            for (int y = 1; y < gridHeight - 1; y++)
            {
                if (Grid[x, y] == CellState.Unclaimed && !visited[x, y])
                {
                    List<Vector2Int> comp = new List<Vector2Int>();
                    Queue<Vector2Int> queue = new Queue<Vector2Int>();
                    bool hasEnemy = false;

                    visited[x, y] = true;
                    queue.Enqueue(new Vector2Int(x, y));

                    while (queue.Count > 0)
                    {
                        Vector2Int cur = queue.Dequeue();
                        comp.Add(cur);

                        if (enemyCells.Contains(cur))
                        {
                            hasEnemy = true;
                        }

                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cur.x + dx[d];
                            int ny = cur.y + dy[d];

                            if (nx < 1 || ny < 1 || nx >= gridWidth - 1 || ny >= gridHeight - 1) continue;
                            if (visited[nx, ny]) continue;
                            if (Grid[nx, ny] != CellState.Unclaimed) continue;

                            visited[nx, ny] = true;
                            queue.Enqueue(new Vector2Int(nx, ny));
                        }
                    }

                    components.Add(comp);
                    componentHasEnemy.Add(hasEnemy);
                }
            }
        }

        if (components.Count == 0) return 0;

        int totalNewlyCaptured = 0;
        bool anyEnemyExists = false;
        for (int i = 0; i < componentHasEnemy.Count; i++)
        {
            if (componentHasEnemy[i])
            {
                anyEnemyExists = true;
                break;
            }
        }

        if (anyEnemyExists)
        {
            // Standard rule: capture any region with NO enemies
            for (int i = 0; i < components.Count; i++)
            {
                if (!componentHasEnemy[i])
                {
                    foreach (var cell in components[i])
                    {
                        Grid[cell.x, cell.y] = CellState.Captured;
                        totalNewlyCaptured++;
                    }
                }
            }
        }
        else if (components.Count > 1)
        {
            // Edge case: multiple regions, none have enemies -> capture all except the largest
            int largestIdx = 0;
            int largestSize = 0;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].Count > largestSize)
                {
                    largestSize = components[i].Count;
                    largestIdx = i;
                }
            }

            for (int i = 0; i < components.Count; i++)
            {
                if (i != largestIdx)
                {
                    foreach (var cell in components[i])
                    {
                        Grid[cell.x, cell.y] = CellState.Captured;
                        totalNewlyCaptured++;
                    }
                }
            }
        }

        return totalNewlyCaptured;
    }

    private void RefreshEnemyCache()
    {
        if (Time.time - _enemyCacheTime > 0.1f)
        {
            _cachedEnemies = FindObjectsOfType<EnemyBall>();
            _enemyCacheTime = Time.time;
        }
    }

    // ─── Coordinate Conversion ─────────────────────────────────────────────────

    private Vector3 GetOrigin()
    {
        return new Vector3(-gridWidth * cellSize * 0.5f, 0f, -gridHeight * cellSize * 0.5f);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 origin = GetOrigin();
        int x = Mathf.FloorToInt((worldPos.x - origin.x) / cellSize);
        int z = Mathf.FloorToInt((worldPos.z - origin.z) / cellSize);
        x = Mathf.Clamp(x, 0, gridWidth - 1);
        z = Mathf.Clamp(z, 0, gridHeight - 1);
        return new Vector2Int(x, z);
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        Vector3 origin = GetOrigin();
        return new Vector3(
            origin.x + gridPos.x * cellSize + cellSize * 0.5f,
            0.1f,
            origin.z + gridPos.y * cellSize + cellSize * 0.5f
        );
    }

    // ─── Queries ───────────────────────────────────────────────────────────────

    public float GetCapturedPercent()
    {
        if (_totalPlayableCells == 0) return 0f;
        return (float)_capturedCells / _totalPlayableCells * 100f;
    }

    public Vector3 GetRandomUnclaimedPosition()
    {
        List<Vector2Int> unclaimed = new List<Vector2Int>();
        for (int x = 2; x < gridWidth - 2; x++)
            for (int y = 2; y < gridHeight - 2; y++)
                if (Grid[x, y] == CellState.Unclaimed)
                    unclaimed.Add(new Vector2Int(x, y));

        if (unclaimed.Count == 0) return Vector3.zero;
        return GridToWorld(unclaimed[UnityEngine.Random.Range(0, unclaimed.Count)]);
    }

    public Vector3 GetRandomCapturedPosition(Vector3 avoidPos, float minDist)
    {
        List<Vector2Int> captured = new List<Vector2Int>();
        for (int x = 1; x < gridWidth - 1; x++)
        {
            for (int y = 1; y < gridHeight - 1; y++)
            {
                if (Grid[x, y] == CellState.Captured)
                {
                    Vector3 worldPos = GridToWorld(new Vector2Int(x, y));
                    if (Vector3.Distance(worldPos, avoidPos) >= minDist)
                        captured.Add(new Vector2Int(x, y));
                }
            }
        }
        if (captured.Count == 0) return GridToWorld(new Vector2Int(1, 1));
        return GridToWorld(captured[UnityEngine.Random.Range(0, captured.Count)]);
    }

    public Vector3 GetSpawnPosition()
    {
        // Return a position near the inner border on the bottom-left
        return GridToWorld(new Vector2Int(2, 2));
    }

    public void ClearTrail()
    {
        foreach (var pos in _currentTrail)
        {
            if (Grid[pos.x, pos.y] == CellState.Trail)
            {
                Grid[pos.x, pos.y] = CellState.Unclaimed;
                ApplyMaterial(pos.x, pos.y);
            }
        }
        _currentTrail.Clear();
        _isDrawingTrail = false;
    }

    public bool IsTrailAt(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight) return false;
        return Grid[pos.x, pos.y] == CellState.Trail;
    }

    public CellState GetCellState(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return CellState.Wall;
        return Grid[x, y];
    }

    public bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
