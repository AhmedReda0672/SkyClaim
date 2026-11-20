using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns patrol mines on captured territory.
/// </summary>
public class MineSpawner : MonoBehaviour
{
    public static MineSpawner Instance { get; private set; }

    [Header("Mine Prefab")]
    public GameObject minePrefab;

    private readonly List<GameObject> _spawnedMines = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SpawnMines(int count, float speed)
    {
        foreach (var m in _spawnedMines)
        {
            if (m != null) Destroy(m);
        }
        _spawnedMines.Clear();

        if (minePrefab == null)
        {
            Debug.LogWarning("[MineSpawner] minePrefab is not assigned.");
            return;
        }

        TerritoryGrid grid = TerritoryGrid.Instance;
        Vector3 playerPos = PlayerController.Instance != null ?
            PlayerController.Instance.transform.position : Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = grid != null ? grid.GetRandomCapturedPosition(playerPos, 5f) : Vector3.zero;
            pos.y = 0.5f;

            GameObject go = Instantiate(minePrefab, pos, Quaternion.identity);
            go.name = $"Mine_{i + 1}";

            MineController mc = go.GetComponent<MineController>();
            if (mc != null) mc.speed = speed;

            _spawnedMines.Add(go);
        }
    }
}
