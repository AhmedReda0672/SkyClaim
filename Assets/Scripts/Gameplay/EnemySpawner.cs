using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemy balls with validated randomized positions, colors, and directions.
/// Ensures balls do not overlap and do not spawn on the player or invalid territory.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Enemy Prefab")]
    public GameObject enemyBallPrefab;
    public GameObject[] enemyBallPrefabs;

    [Header("Validation Rules")]
    public float minDistanceBetweenEnemies = 3.5f;
    public float minDistanceFromPlayer = 6f;

    // High-contrast, easily distinguishable color palette
    private static readonly Color[] Palette = new Color[]
    {
        new Color(1f, 0.2f, 0.2f),     // Bright Red
        new Color(0.2f, 0.5f, 1f),     // Bright Blue
        new Color(0.1f, 1f, 0.3f),     // Bright Green
        new Color(1f, 0.85f, 0.1f),    // Bright Yellow
        new Color(0.9f, 0.2f, 1f),     // Magenta / Purple
        new Color(1f, 0.5f, 0.1f),     // Orange
        new Color(0.1f, 1f, 1f)        // Cyan
    };

    private readonly List<EnemyBall> _spawnedEnemies = new List<EnemyBall>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SpawnEnemies(int count, float speed)
    {
        // Destroy existing balls
        foreach (var ball in _spawnedEnemies)
        {
            if (ball != null)
                Destroy(ball.gameObject);
        }
        _spawnedEnemies.Clear();

        if (enemyBallPrefab == null && (enemyBallPrefabs == null || enemyBallPrefabs.Length == 0))
        {
            Debug.LogError("[EnemySpawner] No enemyBallPrefab assigned!");
            return;
        }

        TerritoryGrid grid = TerritoryGrid.Instance;
        Vector3 playerPos = grid != null
            ? grid.GetSpawnPosition()
            : (PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero);

        List<Vector3> placedPositions = new List<Vector3>();
        Color[] shuffledColors = GetShuffledPalette();

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = FindValidSpawnPosition(placedPositions, playerPos);
            placedPositions.Add(spawnPos);

            Vector3 direction = GetRandomDirection();
            Color color = shuffledColors[i % shuffledColors.Length];

            // Select diverse sci-fi ball model
            GameObject prefabToUse = enemyBallPrefab;
            if (enemyBallPrefabs != null && enemyBallPrefabs.Length > 0)
            {
                prefabToUse = enemyBallPrefabs[i % enemyBallPrefabs.Length];
            }

            GameObject go = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
            go.name = $"EnemyBall_{i + 1}";

            EnemyBall ball = go.GetComponent<EnemyBall>();
            if (ball == null) ball = go.AddComponent<EnemyBall>();

            ball.Initialize(color, spawnPos, direction, speed);
            _spawnedEnemies.Add(ball);
        }

        Debug.Log($"[EnemySpawner] Successfully spawned {_spawnedEnemies.Count} enemy balls.");
    }

    private Color[] GetShuffledPalette()
    {
        Color[] shuffled = (Color[])Palette.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Color temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }
        return shuffled;
    }

    private Vector3 FindValidSpawnPosition(List<Vector3> existingPositions, Vector3 playerPos)
    {
        TerritoryGrid grid = TerritoryGrid.Instance;
        if (grid == null) grid = FindObjectOfType<TerritoryGrid>();

        // Up to 200 attempts to find a spot that satisfies all constraints
        for (int attempt = 0; attempt < 200; attempt++)
        {
            Vector3 candidate;
            if (grid != null)
                candidate = grid.GetRandomUnclaimedPosition();
            else
                candidate = new Vector3(Random.Range(-12f, 12f), 0.5f, Random.Range(-10f, 10f));

            candidate.y = 0.5f;

            if (playerPos != Vector3.zero && Vector3.Distance(candidate, playerPos) < minDistanceFromPlayer)
                continue;

            bool tooClose = false;
            foreach (var pos in existingPositions)
            {
                if (Vector3.Distance(candidate, pos) < minDistanceBetweenEnemies)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                return candidate;
        }

        // Deterministic fallback: place in guaranteed separated ring
        float angle = existingPositions.Count * 0.9f;
        float radius = 6f + (existingPositions.Count * 2.5f);
        return new Vector3(Mathf.Cos(angle) * radius, 0.5f, Mathf.Sin(angle) * radius);
    }

    private Vector3 GetRandomDirection()
    {
        // 8 symmetrical 45-degree angles to ensure dynamic bounces
        float[] angles = { 35f, 55f, 125f, 145f, 215f, 235f, 305f, 325f };
        float rad = angles[Random.Range(0, angles.Length)] * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
    }

    public void ApplySlowToAll(float duration)
    {
        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy != null)
                enemy.ApplySlow(duration);
        }
    }

    public List<EnemyBall> GetEnemies() => _spawnedEnemies;
}
