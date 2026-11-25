using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns periodic power-ups in valid unclaimed territory throughout the level.
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject[] powerUpPrefabs;

    [Header("Spawn Cadence")]
    public float minInterval = 12f;
    public float maxInterval = 22f;
    public int maxActiveOnField = 3;

    private readonly List<GameObject> _activePowerUps = new List<GameObject>();
    private Coroutine _spawnRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ScheduleSpawns(int count)
    {
        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        _spawnRoutine = StartCoroutine(SpawnLoop(count));
    }

    private IEnumerator SpawnLoop(int initialCount)
    {
        // Initial spawn of 1-2 power-ups
        for (int i = 0; i < Mathf.Min(initialCount, 2); i++)
        {
            SpawnSinglePowerUp();
            yield return new WaitForSeconds(1.5f);
        }

        while (true)
        {
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                continue;

            // Clean up nulls
            _activePowerUps.RemoveAll(go => go == null);

            if (_activePowerUps.Count < maxActiveOnField)
            {
                SpawnSinglePowerUp();
            }
        }
    }

    private void SpawnSinglePowerUp()
    {
        TerritoryGrid grid = TerritoryGrid.Instance;
        if (grid == null) return;

        Vector3 spawnPos = grid.GetRandomUnclaimedPosition();
        if (spawnPos == Vector3.zero) return;

        spawnPos.y = 0.75f;

        // Choose random type
        PowerUpType type = (PowerUpType)Random.Range(0, 5);

        GameObject prefabToUse = null;
        if (powerUpPrefabs != null && powerUpPrefabs.Length > 0)
        {
            int idx = Mathf.Clamp((int)type, 0, powerUpPrefabs.Length - 1);
            prefabToUse = powerUpPrefabs[idx];
        }

        if (prefabToUse != null)
        {
            GameObject go = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
            PowerUp pu = go.GetComponent<PowerUp>();
            if (pu != null) pu.type = type;

            _activePowerUps.Add(go);
        }
    }
}
