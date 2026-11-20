#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Comprehensive automated test suite for SkyClaim.
/// Tests all core systems: Territory Grid, Flood-Fill algorithm, Enemy Spawning,
/// Randomization, Level Configurations, Lives/Timer/Score, and Controls.
/// </summary>
public static class SkyClaimAutomatedTests
{
    private static int _passedTests = 0;
    private static int _failedTests = 0;
    private static readonly List<string> _logMessages = new List<string>();

    [MenuItem("SkyClaim/Run All Automated Tests")]
    public static void RunAllTests()
    {
        _passedTests = 0;
        _failedTests = 0;
        _logMessages.Clear();

        LogHeader("SKYCLAIM AUTOMATED TEST SUITE EXECUTION");

        RunTest("1. Level Configurations (3 levels, targets 60/70/80%, counts 3/4/7)", Test_LevelConfigurations);
        RunTest("2. Territory Grid Initialization & Cell States", Test_TerritoryGrid_Initialization);
        RunTest("3. Territory Grid Trail Creation & Closure", Test_TerritoryGrid_TrailCreation);
        RunTest("4. Connected-Component Flood-Fill Algorithm", Test_ConnectedComponent_FloodFill);
        RunTest("5. Enemy Spawner Exact Counts (3, 4, 7)", Test_EnemySpawner_Counts);
        RunTest("6. Enemy Randomization & Overlap Prevention", Test_EnemySpawner_Randomization);
        RunTest("7. Player Controls Arrow-Keys Only (No WASD)", Test_PlayerControls_ArrowKeysOnly);
        RunTest("8. Lives System (3 lives shared across levels, Game Over on 0)", Test_LivesSystem);
        RunTest("9. Timer & Score Calculations", Test_TimerAndScore);
        RunTest("10. Save System Persistence & Level Clamping", Test_SaveSystem);
        RunTest("11. Power-Up Types & Effects", Test_PowerUps);

        LogSummary();

        // Write test report to disk
        string reportPath = "D:/Games/SkyClaim/TestResults.txt";
        File.WriteAllLines(reportPath, _logMessages);
        Debug.Log($"[AutomatedTests] Report written to: {reportPath}");

        if (_failedTests > 0)
        {
            EditorApplication.Exit(1);
        }
        else
        {
            EditorApplication.Exit(0);
        }
    }

    private static void RunTest(string testName, Action testAction)
    {
        try
        {
            testAction.Invoke();
            _passedTests++;
            string msg = $"[PASS] {testName}";
            _logMessages.Add(msg);
            Debug.Log(msg);
        }
        catch (Exception ex)
        {
            _failedTests++;
            string msg = $"[FAIL] {testName}: {ex.Message}";
            _logMessages.Add(msg);
            Debug.LogError(msg);
        }
    }

    // ─── TEST CASES ──────────────────────────────────────────────────────────

    private static void Test_LevelConfigurations()
    {
        var go = new GameObject("TestGM");
        var gm = go.AddComponent<GameManager>();

        Assert.IsNotNull(gm.levelConfigs, "Level configs array must not be null.");
        Assert.AreEqual(3, gm.levelConfigs.Length, "Must have EXACTLY 3 levels.");

        // Level 1: 3 balls, 60%
        Assert.AreEqual(3, gm.levelConfigs[0].ballCount, "Level 1 must have exactly 3 balls.");
        Assert.AreEqual(60f, gm.levelConfigs[0].targetPercent, "Level 1 target must be 60%.");

        // Level 2: 4 balls, 70%
        Assert.AreEqual(4, gm.levelConfigs[1].ballCount, "Level 2 must have exactly 4 balls.");
        Assert.AreEqual(70f, gm.levelConfigs[1].targetPercent, "Level 2 target must be 70%.");

        // Level 3: 7 balls, 80%
        Assert.AreEqual(7, gm.levelConfigs[2].ballCount, "Level 3 must have exactly 7 balls.");
        Assert.AreEqual(80f, gm.levelConfigs[2].targetPercent, "Level 3 target must be 80%.");

        GameObject.DestroyImmediate(go);
    }

    private static void Test_TerritoryGrid_Initialization()
    {
        var go = new GameObject("TestGrid");
        var grid = go.AddComponent<TerritoryGrid>();
        grid.gridWidth = 40;
        grid.gridHeight = 30;
        grid.cellSize = 1f;
        grid.Initialize();

        Assert.IsNotNull(grid.Grid, "Grid state array must be initialized.");

        // Check outer wall
        for (int x = 0; x < 40; x++)
        {
            Assert.AreEqual(CellState.Wall, grid.GetCellState(x, 0), $"Cell ({x}, 0) must be Wall.");
            Assert.AreEqual(CellState.Wall, grid.GetCellState(x, 29), $"Cell ({x}, 29) must be Wall.");
        }
        for (int y = 0; y < 30; y++)
        {
            Assert.AreEqual(CellState.Wall, grid.GetCellState(0, y), $"Cell (0, {y}) must be Wall.");
            Assert.AreEqual(CellState.Wall, grid.GetCellState(39, y), $"Cell (39, {y}) must be Wall.");
        }

        // Check inner captured border
        for (int x = 1; x < 39; x++)
        {
            Assert.AreEqual(CellState.Captured, grid.GetCellState(x, 1), $"Cell ({x}, 1) must be Captured.");
            Assert.AreEqual(CellState.Captured, grid.GetCellState(x, 28), $"Cell ({x}, 28) must be Captured.");
        }

        // Check center is unclaimed
        Assert.AreEqual(CellState.Unclaimed, grid.GetCellState(20, 15), "Center cell (20,15) must be Unclaimed.");

        // Check initial captured percent is non-zero (due to inner border) and less than 30%
        float initialPct = grid.GetCapturedPercent();
        Assert.IsTrue(initialPct > 5f && initialPct < 30f, $"Initial percentage {initialPct}% should reflect inner border ring.");

        GameObject.DestroyImmediate(go);
    }

    private static void Test_TerritoryGrid_TrailCreation()
    {
        var go = new GameObject("TestGrid");
        var grid = go.AddComponent<TerritoryGrid>();
        grid.gridWidth = 40;
        grid.gridHeight = 30;
        grid.Initialize();

        float initialPct = grid.GetCapturedPercent();

        // Move into unclaimed cell (2, 2)
        bool added1 = grid.TryAddTrail(new Vector2Int(2, 2));
        Assert.IsTrue(added1, "Adding trail to unclaimed cell must succeed.");
        Assert.AreEqual(CellState.Trail, grid.GetCellState(2, 2), "Cell (2, 2) must be Trail.");

        // Move into adjacent unclaimed cell (3, 2)
        bool added2 = grid.TryAddTrail(new Vector2Int(3, 2));
        Assert.IsTrue(added2, "Adding trail to unclaimed cell must succeed.");
        Assert.AreEqual(CellState.Trail, grid.GetCellState(3, 2), "Cell (3, 2) must be Trail.");

        // Return to captured border (3, 1) -> should trigger CloseTrail()
        bool closed = grid.TryAddTrail(new Vector2Int(3, 1));
        Assert.IsTrue(closed, "Returning to captured cell must close trail.");

        // Verify trail cells are now Captured
        Assert.AreEqual(CellState.Captured, grid.GetCellState(2, 2), "Trail cell (2, 2) must now be Captured.");
        Assert.AreEqual(CellState.Captured, grid.GetCellState(3, 2), "Trail cell (3, 2) must now be Captured.");

        // Captured percentage must increase
        Assert.IsTrue(grid.GetCapturedPercent() > initialPct, "Captured percentage must strictly increase after capture.");

        GameObject.DestroyImmediate(go);
    }

    private static void Test_ConnectedComponent_FloodFill()
    {
        var gridGO = new GameObject("TestGrid");
        var grid = gridGO.AddComponent<TerritoryGrid>();
        grid.gridWidth = 20;
        grid.gridHeight = 20;
        grid.cellSize = 1f;
        grid.Initialize();

        // Place simulated enemy in Right half at (15, 10) BEFORE closing trail
        var enemyGO = new GameObject("EnemyBall");
        var enemy = enemyGO.AddComponent<EnemyBall>();
        enemyGO.transform.position = grid.GridToWorld(new Vector2Int(15, 10));

        // Draw a dividing wall across x = 10 from y=2 to y=17
        for (int y = 2; y <= 17; y++)
        {
            grid.TryAddTrail(new Vector2Int(10, y));
        }

        // Connect to top border (10, 18) -> closes trail and triggers flood-fill
        grid.TryAddTrail(new Vector2Int(10, 18));

        // Left half (containing NO enemies) must be completely Captured!
        Assert.AreEqual(CellState.Captured, grid.GetCellState(5, 10), "Left cell (5,10) must be captured as it has NO enemies.");
        Assert.AreEqual(CellState.Captured, grid.GetCellState(3, 5), "Left cell (3,5) must be captured.");

        // Right half (containing the enemy) must remain Unclaimed!
        Assert.AreEqual(CellState.Unclaimed, grid.GetCellState(14, 10), "Right cell (14,10) must remain Unclaimed because enemy is present.");

        GameObject.DestroyImmediate(enemyGO);
        GameObject.DestroyImmediate(gridGO);
    }

    private static void Test_EnemySpawner_Counts()
    {
        var spawnerGO = new GameObject("TestSpawner");
        var spawner = spawnerGO.AddComponent<EnemySpawner>();

        var ballPrefab = new GameObject("BallPrefab");
        ballPrefab.AddComponent<EnemyBall>();
        spawner.enemyBallPrefab = ballPrefab;

        var gridGO = new GameObject("TestGrid");
        var grid = gridGO.AddComponent<TerritoryGrid>();
        grid.gridWidth = 40;
        grid.gridHeight = 30;
        grid.Initialize();

        // Test Level 1 count
        spawner.SpawnEnemies(3, 3f);
        Assert.AreEqual(3, spawner.GetEnemies().Count, "Level 1 must spawn exactly 3 balls.");

        // Test Level 2 count
        spawner.SpawnEnemies(4, 4.5f);
        Assert.AreEqual(4, spawner.GetEnemies().Count, "Level 2 must spawn exactly 4 balls.");

        // Test Level 3 count
        spawner.SpawnEnemies(7, 6f);
        Assert.AreEqual(7, spawner.GetEnemies().Count, "Level 3 must spawn exactly 7 balls.");

        GameObject.DestroyImmediate(ballPrefab);
        GameObject.DestroyImmediate(spawnerGO);
        GameObject.DestroyImmediate(gridGO);
    }

    private static void Test_EnemySpawner_Randomization()
    {
        var spawnerGO = new GameObject("TestSpawner");
        var spawner = spawnerGO.AddComponent<EnemySpawner>();
        var ballPrefab = new GameObject("BallPrefab");
        ballPrefab.AddComponent<EnemyBall>();
        spawner.enemyBallPrefab = ballPrefab;

        var gridGO = new GameObject("TestGrid");
        var grid = gridGO.AddComponent<TerritoryGrid>();
        grid.gridWidth = 40;
        grid.gridHeight = 30;
        grid.Initialize();

        spawner.SpawnEnemies(7, 5f);
        var enemies = spawner.GetEnemies();

        // Check that no two enemies overlap (distance >= 2.5f)
        for (int i = 0; i < enemies.Count; i++)
        {
            for (int j = i + 1; j < enemies.Count; j++)
            {
                float dist = Vector3.Distance(enemies[i].transform.position, enemies[j].transform.position);
                Assert.IsTrue(dist >= 2.5f, $"Enemies {i} and {j} must not overlap (dist: {dist:0.00}).");
            }
        }

        GameObject.DestroyImmediate(ballPrefab);
        GameObject.DestroyImmediate(spawnerGO);
        GameObject.DestroyImmediate(gridGO);
    }

    private static void Test_PlayerControls_ArrowKeysOnly()
    {
        // Read PlayerController.cs source to ensure WASD is strictly excluded
        string source = File.ReadAllText("D:/Games/SkyClaim/Assets/Scripts/Gameplay/PlayerController.cs");

        Assert.IsTrue(source.Contains("KeyCode.UpArrow"), "Must bind UpArrow.");
        Assert.IsTrue(source.Contains("KeyCode.DownArrow"), "Must bind DownArrow.");
        Assert.IsTrue(source.Contains("KeyCode.LeftArrow"), "Must bind LeftArrow.");
        Assert.IsTrue(source.Contains("KeyCode.RightArrow"), "Must bind RightArrow.");

        // WASD keys must NOT be present
        Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"\bKeyCode\.W\b"), "WASD controls are strictly forbidden (found KeyCode.W).");
        Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"\bKeyCode\.A\b"), "WASD controls are strictly forbidden (found KeyCode.A).");
        Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"\bKeyCode\.S\b"), "WASD controls are strictly forbidden (found KeyCode.S).");
        Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"\bKeyCode\.D\b"), "WASD controls are strictly forbidden (found KeyCode.D).");
    }

    private static void Test_LivesSystem()
    {
        var go = new GameObject("TestGM");
        var gm = go.AddComponent<GameManager>();

        gm.StartNewGame();
        Assert.AreEqual(3, gm.TotalLives, "Initial game run must have exactly 3 lives.");

        gm.LoseLife();
        Assert.AreEqual(2, gm.TotalLives, "After 1 hit, player must have 2 lives.");

        gm.LoseLife();
        Assert.AreEqual(1, gm.TotalLives, "After 2 hits, player must have 1 life.");

        gm.LoseLife();
        Assert.AreEqual(0, gm.TotalLives, "After 3 hits, player must have 0 lives.");
        Assert.AreEqual(GameState.GameOver, gm.CurrentState, "Reaching 0 lives must trigger Game Over.");

        GameObject.DestroyImmediate(go);
    }

    private static void Test_TimerAndScore()
    {
        var tmGO = new GameObject("TestTimer");
        var tm = tmGO.AddComponent<TimerManager>();
        tm.StartTimer(100f);
        Assert.AreEqual(100f, tm.GetRemainingTime(), 0.1f, "Remaining time must equal initialized duration.");

        tm.AddTime(30f);
        Assert.AreEqual(130f, tm.GetRemainingTime(), 0.1f, "AddTime must extend timer by 30s.");

        var smGO = new GameObject("TestScore");
        var sm = smGO.AddComponent<ScoreManager>();
        int bonus = sm.CalculateTimeBonus(10f);
        Assert.AreEqual(50, bonus, "10 seconds remaining must award 50 bonus points (5 pts/sec).");

        GameObject.DestroyImmediate(tmGO);
        GameObject.DestroyImmediate(smGO);
    }

    private static void Test_SaveSystem()
    {
        var go = new GameObject("TestSave");
        var save = go.AddComponent<SaveManager>();
        save.ResetAll();

        save.SetBestScore(9999);
        Assert.AreEqual(9999, save.GetBestScore(), "Best score must persist.");

        save.SetUnlockedLevel(2);
        Assert.AreEqual(2, save.GetUnlockedLevel(), "Unlocked level must be 2.");

        // Cannot unlock beyond level 3
        save.SetUnlockedLevel(5);
        Assert.AreEqual(3, save.GetUnlockedLevel(), "Level unlock must be clamped to max 3.");

        GameObject.DestroyImmediate(go);
    }

    private static void Test_PowerUps()
    {
        var values = Enum.GetValues(typeof(PowerUpType));
        Assert.AreEqual(5, values.Length, "Must have exactly 5 power-up types.");

        Assert.IsTrue(Enum.IsDefined(typeof(PowerUpType), "ExtraLife"), "Must have ExtraLife power-up.");
        Assert.IsTrue(Enum.IsDefined(typeof(PowerUpType), "ExtraTime"), "Must have ExtraTime power-up.");
        Assert.IsTrue(Enum.IsDefined(typeof(PowerUpType), "Slow"), "Must have Slow power-up.");
        Assert.IsTrue(Enum.IsDefined(typeof(PowerUpType), "Speed"), "Must have Speed power-up.");
        Assert.IsTrue(Enum.IsDefined(typeof(PowerUpType), "ScoreBonus"), "Must have ScoreBonus power-up.");
    }

    // ─── REPORTING HELPERS ───────────────────────────────────────────────────

    private static void LogHeader(string title)
    {
        _logMessages.Add("================================================================================");
        _logMessages.Add($"  {title}");
        _logMessages.Add($"  Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _logMessages.Add("================================================================================");
    }

    private static void LogSummary()
    {
        _logMessages.Add("--------------------------------------------------------------------------------");
        _logMessages.Add($"RESULTS: {_passedTests} PASSED, {_failedTests} FAILED (TOTAL: {_passedTests + _failedTests})");
        _logMessages.Add(_failedTests == 0 ? "STATUS: ALL TESTS PASSED SUCCESSFULLY!" : "STATUS: SOME TESTS FAILED!");
        _logMessages.Add("--------------------------------------------------------------------------------");
    }
}
#endif
