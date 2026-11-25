#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// Editor utility that programmatically builds both game scenes.
/// Run from the Unity menu: SkyClaim > Build All Scenes
/// This creates fully wired scenes without manual setup.
/// </summary>
public static class SceneBuilder
{
    [MenuItem("SkyClaim/Build All Scenes")]
    public static void BuildAllScenes()
    {
        string tmpPackage = "Library/PackageCache/com.unity.textmeshpro@3.0.6/Package Resources/TMP Essential Resources.unitypackage";
        if (File.Exists(tmpPackage) && !Directory.Exists("Assets/TextMesh Pro"))
        {
            AssetDatabase.ImportPackage(tmpPackage, false);
            AssetDatabase.Refresh();
        }

        BuildMainMenuScene();
        BuildGameScene();
        Debug.Log("[SceneBuilder] All scenes built successfully.");
    }

    [MenuItem("SkyClaim/Build Main Menu Scene")]
    public static void BuildMainMenuScene()
    {
        // Ensure scenes folder exists
        Directory.CreateDirectory("Assets/Scenes");
        
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // --- Lighting ---
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        
        // --- Camera ---
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.15f);
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = new Vector3(0, 0, -10);
        
        // --- Bootstrapper ---
        var bootGO = new GameObject("Bootstrapper");
        bootGO.AddComponent<SceneBootstrapper>();
        
        // --- EventSystem ---
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        
        // --- Main Canvas ---
        var canvasGO = new GameObject("MainMenuCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // --- Background panel ---
        var bgPanel = CreateUIPanel(canvasGO.transform, "Background", new Color(0.05f, 0.05f, 0.15f));
        SetAnchors(bgPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        
        // --- Title Text ---
        var titleGO = new GameObject("TitleText", typeof(RectTransform));
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "SKYCLAIM";
        titleText.fontSize = 72;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.cyan;
        titleText.alignment = TextAlignmentOptions.Center;
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 300);
        titleRect.sizeDelta = new Vector2(800, 100);
        
        // --- Main Menu Panel ---
        var mainMenuPanel = CreateUIPanel(canvasGO.transform, "MainMenuPanel", new Color(0, 0, 0, 0));
        SetAnchors(mainMenuPanel, new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.9f), Vector2.zero, Vector2.zero);
        
        // --- Buttons ---
        CreateMenuButton(mainMenuPanel.transform, "PlayButton", "PLAY", new Vector2(0, 100));
        CreateMenuButton(mainMenuPanel.transform, "LevelSelectButton", "LEVEL SELECT", new Vector2(0, 20));
        CreateMenuButton(mainMenuPanel.transform, "SettingsButton", "SETTINGS", new Vector2(0, -60));
        CreateMenuButton(mainMenuPanel.transform, "ExitButton", "EXIT", new Vector2(0, -140));
        
        // Best Score Text
        var bestScoreGO = new GameObject("BestScoreText", typeof(RectTransform));
        bestScoreGO.transform.SetParent(canvasGO.transform, false);
        var bestScoreText = bestScoreGO.AddComponent<TextMeshProUGUI>();
        bestScoreText.text = "Best Score: 0";
        bestScoreText.fontSize = 24;
        bestScoreText.color = Color.white;
        bestScoreText.alignment = TextAlignmentOptions.Center;
        var bsRect = bestScoreGO.GetComponent<RectTransform>();
        bsRect.anchoredPosition = new Vector2(0, -400);
        bsRect.sizeDelta = new Vector2(600, 50);
        
        // --- Level Select Panel (initially hidden) ---
        var levelSelectPanel = CreateUIPanel(canvasGO.transform, "LevelSelectPanel", new Color(0, 0, 0, 0.8f));
        SetAnchors(levelSelectPanel, new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.9f), Vector2.zero, Vector2.zero);
        levelSelectPanel.SetActive(false);
        
        var levelSelectTitle = CreateLabel(levelSelectPanel.transform, "LevelSelectTitle", "SELECT LEVEL", new Vector2(0, 200));
        
        var lvl1Btn = CreateMenuButton(levelSelectPanel.transform, "Level1Button", "Level 1 - Beginner\n3 Enemies | 60% Target", new Vector2(0, 80));
        var lvl2Btn = CreateMenuButton(levelSelectPanel.transform, "Level2Button", "Level 2 - Medium\n4 Enemies | 70% Target", new Vector2(0, 0));
        var lvl3Btn = CreateMenuButton(levelSelectPanel.transform, "Level3Button", "Level 3 - Hard\n7 Enemies | 80% Target", new Vector2(0, -80));
        CreateMenuButton(levelSelectPanel.transform, "BackFromLevelSelectButton", "BACK", new Vector2(0, -200));
        
        // --- Settings Panel (initially hidden) ---
        var settingsPanel = CreateUIPanel(canvasGO.transform, "SettingsPanel", new Color(0, 0, 0, 0.8f));
        SetAnchors(settingsPanel, new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.8f), Vector2.zero, Vector2.zero);
        settingsPanel.SetActive(false);
        
        CreateLabel(settingsPanel.transform, "SettingsTitle", "SETTINGS", new Vector2(0, 150));
        CreateLabel(settingsPanel.transform, "MusicLabel", "Music Volume", new Vector2(-100, 60));
        CreateLabel(settingsPanel.transform, "SFXLabel", "SFX Volume", new Vector2(-100, 0));
        
        var musicSlider = CreateSlider(settingsPanel.transform, "MusicSlider", new Vector2(80, 60));
        var sfxSlider = CreateSlider(settingsPanel.transform, "SFXSlider", new Vector2(80, 0));
        CreateMenuButton(settingsPanel.transform, "BackFromSettingsButton", "BACK", new Vector2(0, -150));
        
        // --- MainMenuController ---
        var controllerGO = new GameObject("MainMenuController");
        var controller = controllerGO.AddComponent<MainMenuController>();
        controller.mainMenuPanel = mainMenuPanel;
        controller.levelSelectPanel = levelSelectPanel;
        controller.settingsPanel = settingsPanel;
        controller.bestScoreText = bestScoreText;
        controller.musicSlider = musicSlider;
        controller.sfxSlider = sfxSlider;
        
        // Wire level buttons
        controller.levelButtons = new Button[]
        {
            lvl1Btn.GetComponent<Button>(),
            lvl2Btn.GetComponent<Button>(),
            lvl3Btn.GetComponent<Button>()
        };
        
        // Wire button click events using AddListener
        mainMenuPanel.transform.Find("PlayButton")?.GetComponent<Button>()?.onClick.AddListener(controller.OnPlayButton);
        mainMenuPanel.transform.Find("LevelSelectButton")?.GetComponent<Button>()?.onClick.AddListener(controller.ShowLevelSelect);
        mainMenuPanel.transform.Find("SettingsButton")?.GetComponent<Button>()?.onClick.AddListener(controller.ShowSettings);
        mainMenuPanel.transform.Find("ExitButton")?.GetComponent<Button>()?.onClick.AddListener(controller.OnExitButton);
        
        int idx1 = 1; lvl1Btn.GetComponent<Button>()?.onClick.AddListener(() => controller.OnLevelButton(idx1));
        int idx2 = 2; lvl2Btn.GetComponent<Button>()?.onClick.AddListener(() => controller.OnLevelButton(idx2));
        int idx3 = 3; lvl3Btn.GetComponent<Button>()?.onClick.AddListener(() => controller.OnLevelButton(idx3));
        
        levelSelectPanel.transform.Find("BackFromLevelSelectButton")?.GetComponent<Button>()?.onClick.AddListener(controller.ShowMainMenu);
        settingsPanel.transform.Find("BackFromSettingsButton")?.GetComponent<Button>()?.onClick.AddListener(controller.ShowMainMenu);
        
        // Save scene
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        Debug.Log("[SceneBuilder] MainMenu scene built.");
    }

    [MenuItem("SkyClaim/Build Game Scene")]
    public static void BuildGameScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // --- Bootstrapper ---
        var bootGO = new GameObject("Bootstrapper");
        bootGO.AddComponent<SceneBootstrapper>();
        
        // --- GameStateListener ---
        var gslGO = new GameObject("GameStateListener");
        gslGO.AddComponent<GameStateListener>();
        
        // --- Lighting ---
        // Sky light
        var skyLight = new GameObject("SkyLight");
        var skyL = skyLight.AddComponent<Light>();
        skyL.type = LightType.Directional;
        skyL.color = new Color(0.9f, 0.9f, 1f);
        skyL.intensity = 1.2f;
        skyLight.transform.rotation = Quaternion.Euler(45, -30, 0);
        
        // Ambient and Atmospheric Fog - set via RenderSettings
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.6f);
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.52f, 0.74f, 0.98f, 1f);
        RenderSettings.fogStartDistance = 42f;
        RenderSettings.fogEndDistance = 85f;
        
        // --- Camera ---
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.52f, 0.74f, 0.98f, 1f); // Sky blue matching fog
        cam.fieldOfView = 48f;
        camGO.AddComponent<AudioListener>();
        var camCtrl = camGO.AddComponent<CameraController>();
        camCtrl.height = 34f;
        camCtrl.distanceZ = -27.5f;
        camCtrl.tiltAngle = 53f;
        camCtrl.baseFieldOfView = 48f;
        camCtrl.adaptiveAspectFraming = true;
        camGO.transform.position = new Vector3(0, 34f, -27.5f);
        camGO.transform.rotation = Quaternion.Euler(53f, 0, 0);
        
        // --- EventSystem ---
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        
        // --- Managers ---
        var managersGO = new GameObject("Managers");
        
        var sessionGO = new GameObject("GameSessionManager");
        sessionGO.transform.SetParent(managersGO.transform);
        var session = sessionGO.AddComponent<GameSessionManager>();
        
        var scoreGO = new GameObject("ScoreManager");
        scoreGO.transform.SetParent(managersGO.transform);
        scoreGO.AddComponent<ScoreManager>();
        
        var timerGO = new GameObject("TimerManager");
        timerGO.transform.SetParent(managersGO.transform);
        timerGO.AddComponent<TimerManager>();
        
        // --- Territory Grid ---
        var gridGO = new GameObject("TerritoryGrid");
        var grid = gridGO.AddComponent<TerritoryGrid>();
        grid.gridWidth = 44;
        grid.gridHeight = 34;
        grid.borderThickness = 2;
        grid.cellSize = 1f;

        // --- Arena Floor Collider (invisible, keeps everything above y=0) ---
        // Grid is 44x34 centered at origin; we add a thin box collider at y = -0.05
        var floorGO = new GameObject("ArenaFloor");
        var floorCol = floorGO.AddComponent<BoxCollider>();
        floorCol.size = new Vector3(48f, 0.1f, 38f);   // slightly wider than grid
        floorGO.transform.position = new Vector3(0f, -0.05f, 0f);
        
        // Create materials for grid
        grid.capturedMaterial = CreateColorMaterial("CapturedMat", new Color(0.2f, 0.6f, 1f, 1f));
        grid.unclaimedMaterial = CreateColorMaterial("UnclaimedMat", new Color(0.3f, 0.3f, 0.35f, 1f));
        grid.trailMaterial = CreateColorMaterial("TrailMat", new Color(1f, 1f, 0.2f, 1f));
        grid.wallMaterial = CreateColorMaterial("WallMat", new Color(0.1f, 0.1f, 0.15f, 1f));
        
        // --- Ensure Prefabs directory exists ---
        Directory.CreateDirectory("Assets/Prefabs");
        AssetDatabase.Refresh();

        // --- Enemy Spawner with Diverse Sci-Fi Ball Models ---
        var enemySpawnerGO = new GameObject("EnemySpawner");
        var enemySpawner = enemySpawnerGO.AddComponent<EnemySpawner>();
        
        string[] sciFiBallModels = new string[]
        {
            "RollingBalls_Sci-fi_3_1",
            "RollingBalls_Sci-fi_3_2",
            "RollingBalls_Sci-fi_3_3",
            "RollingBalls_Sci-fi_3_4",
            "RollingBalls_Sci-fi_3_5",
            "RollingBalls_Sci-fi_4_1",
            "RollingBalls_Sci-fi_4_2",
            "RollingBalls_Sci-fi_4_3",
            "RollingBalls_Sci-fi_4_4",
            "RollingBalls_Sci-fi_4_5"
        };
        var ballPrefabsList = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < sciFiBallModels.Length; i++)
        {
            var enemyBallTemplate = CreateEnemyBallGameObject(sciFiBallModels[i]);
            var saved = PrefabUtility.SaveAsPrefabAsset(enemyBallTemplate, $"Assets/Prefabs/EnemyBall_{i + 1}.prefab");
            GameObject.DestroyImmediate(enemyBallTemplate);
            if (saved != null) ballPrefabsList.Add(saved);
        }
        enemySpawner.enemyBallPrefabs = ballPrefabsList.ToArray();
        if (ballPrefabsList.Count > 0)
            enemySpawner.enemyBallPrefab = ballPrefabsList[0];
        
        // --- Mine Spawner ---
        var mineSpawnerGO = new GameObject("MineSpawner");
        var mineSpawner = mineSpawnerGO.AddComponent<MineSpawner>();
        
        var mineTemplate = CreateMineGameObject();
        mineSpawner.minePrefab = PrefabUtility.SaveAsPrefabAsset(mineTemplate, "Assets/Prefabs/Mine.prefab");
        GameObject.DestroyImmediate(mineTemplate);
        
        // --- PowerUp Manager ---
        var puManagerGO = new GameObject("PowerUpManager");
        var puManager = puManagerGO.AddComponent<PowerUpManager>();
        
        // Create powerup prefabs
        var puPrefabs = new GameObject[5];
        for (int i = 0; i < 5; i++)
        {
            var puTemplate = CreatePowerUpGameObject((PowerUpType)i);
            Directory.CreateDirectory("Assets/Prefabs");
            puPrefabs[i] = PrefabUtility.SaveAsPrefabAsset(puTemplate, $"Assets/Prefabs/PowerUp_{(PowerUpType)i}.prefab");
            GameObject.DestroyImmediate(puTemplate);
        }
        puManager.powerUpPrefabs = puPrefabs;
        
        // --- Player Aircraft ---
        var playerGO = CreatePlayerAircraftGameObject();
        playerGO.tag = "Player";
        var playerCtrl = playerGO.GetComponent<PlayerController>();
        
        // --- Wire GameSessionManager ---
        session.enemySpawner = enemySpawner;  // Note: private fields need SerializedObject
        session.mineSpawner = mineSpawner;
        session.powerUpManager = puManager;
        session.territoryGrid = grid;
        session.playerController = playerCtrl;
        session.timerManager = timerGO.GetComponent<TimerManager>();
        
        // Use SerializedObject to set private serialized fields
        var sessionSO = new SerializedObject(session);
        sessionSO.FindProperty("enemySpawner").objectReferenceValue = enemySpawner;
        sessionSO.FindProperty("mineSpawner").objectReferenceValue = mineSpawner;
        sessionSO.FindProperty("powerUpManager").objectReferenceValue = puManager;
        sessionSO.FindProperty("territoryGrid").objectReferenceValue = grid;
        sessionSO.FindProperty("playerController").objectReferenceValue = playerCtrl;
        sessionSO.FindProperty("timerManager").objectReferenceValue = timerGO.GetComponent<TimerManager>();
        sessionSO.ApplyModifiedProperties();
        
        // --- UI ---
        var uiSetup = BuildGameSceneUI(camCtrl);
        
        // Wire UIManager to other components
        var uiManagerSO = new SerializedObject(uiSetup.uiManager);
        // (fields are public in UIManager, so direct assignment works)
        
        // --- Save scene ---
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
        Debug.Log("[SceneBuilder] GameScene built.");
    }
    
    // ===================== HELPER METHODS =====================
    
    private static Material CreateColorMaterial(string name, Color color)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        if (color.a < 1f)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }
        Directory.CreateDirectory("Assets/Materials");
        string assetPath = $"Assets/Materials/{name}.mat";
        if (File.Exists(assetPath))
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null) return existing;
        }
        AssetDatabase.CreateAsset(mat, assetPath);
        return mat;
    }
    
    private static GameObject CreateEnemyBallGameObject(string modelName = "RollingBalls_Sci-fi_1_1")
    {
        var go = new GameObject($"EnemyBall_{modelName}");

        // Load authentic diverse sci-fi ball model from Rolling_Balls-Sci-fi_Pack
        string prefabPath = $"Assets/Rolling_Balls-Sci-fi_Pack/Rolling_Balls-Sci-fi_Pack_SRP_Build/Prefab/{modelName}.prefab";
        var ballAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Renderer mainRenderer = null;

        if (ballAsset != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(ballAsset);
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.8f;
            foreach (var col in visual.GetComponentsInChildren<Collider>()) Destroy(col);
            mainRenderer = visual.GetComponentInChildren<Renderer>();
        }
        else
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(go.transform, false);
            sphere.transform.localScale = Vector3.one * 0.8f;
            mainRenderer = sphere.GetComponent<Renderer>();
            Destroy(sphere.GetComponent<Collider>());
        }

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Trigger collider for territory & player detection
        var sc = go.AddComponent<SphereCollider>();
        sc.radius = 0.55f;
        sc.isTrigger = true;

        var ball = go.AddComponent<EnemyBall>();
        ball.ballRenderer = mainRenderer;

        // Glowing Sci-Fi Trail
        var trail = go.AddComponent<TrailRenderer>();
        trail.startWidth = 0.35f;
        trail.endWidth = 0.05f;
        trail.time = 0.45f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        ball.trailRenderer = trail;

        return go;
    }
    
    private static GameObject CreateMineGameObject()
    {
        var go = new GameObject("Mine");
        
        // Visual: cube with spikes (simplified)
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(go.transform);
        body.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.2f, 0.2f, 0.2f);
        body.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(body.GetComponent<Collider>());
        
        // Trigger collider on parent
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.8f, 0.8f, 0.8f);
        col.isTrigger = true;
        
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        var mine = go.AddComponent<MineController>();
        mine.mineRenderer = body.GetComponent<Renderer>();
        
        return go;
    }
    
    private static GameObject CreatePowerUpGameObject(PowerUpType type)
    {
        var go = new GameObject($"PowerUp_{type}");

        // Map type to authentic BTM 3D asset prefab
        string prefabName = type switch
        {
            PowerUpType.ExtraLife => "Heart",
            PowerUpType.ExtraTime => "Clock",
            PowerUpType.Slow => "Spiral",
            PowerUpType.Speed => "Thunder",
            PowerUpType.ScoreBonus => "Coin",
            _ => "Coin"
        };

        string prefabPath = $"Assets/BTM_Assets/BTM_Items_Gems/Prefabs/{prefabName}.prefab";
        var puAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Renderer mainRenderer = null;

        if (puAsset != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(puAsset);
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            visual.transform.localScale = Vector3.one * 0.85f;
            foreach (var col in visual.GetComponentsInChildren<Collider>()) Destroy(col);
            foreach (var anim in visual.GetComponentsInChildren<Benjathemaker.SimpleGemsAnim>()) Destroy(anim);
            
            // Destroy the grey PowerUp_Sphere outer shell embedded in TeKniKo prefabs
            var toRemove = new System.Collections.Generic.List<GameObject>();
            foreach (var t in visual.GetComponentsInChildren<Transform>(true))
            {
                if (t != visual.transform && (t.name.Contains("Sphere") || t.name.Contains("PowerUp_Sphere")))
                {
                    toRemove.Add(t.gameObject);
                }
            }
            foreach (var obj in toRemove)
            {
                Destroy(obj);
            }
            
            // Get renderer from the main 3D icon model
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                mainRenderer = r;
                break;
            }
        }
        else
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.5f;
            mainRenderer = visual.GetComponent<Renderer>();
            Destroy(visual.GetComponent<Collider>());
        }

        var sc = go.AddComponent<SphereCollider>();
        sc.center = new Vector3(0f, 0.1f, 0f);
        sc.radius = 0.95f;
        sc.isTrigger = true;

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        var puComp = go.AddComponent<PowerUp>();
        puComp.type = type;
        puComp.powerUpRenderer = mainRenderer;

        // Glowing Point Light
        var lightGO = new GameObject("Light");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0, 0.65f, 0);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 3f;
        light.intensity = 1.5f;
        puComp.powerUpLight = light;

        return go;
    }
    
    private static GameObject CreatePlayerAircraftGameObject()
    {
        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.layer = LayerMask.NameToLayer("Default");

        // Load 3D Cartoon Airplane from AwesomeCartoonPlanes
        string planePath = "Assets/AwesomeCartoonPlanes/Prefabs/Plane1.prefab";
        var planeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(planePath);
        Transform propTransform = null;

        if (planeAsset != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(planeAsset);
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            visual.name = "PlaneVisual";
            visual.transform.SetParent(playerGO.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity; // In Plane1.prefab, fuselage points forward (+Z)
            visual.transform.localScale = Vector3.one * 0.45f;

            // Remove demo scripts
            foreach (var s in visual.GetComponentsInChildren<MonoBehaviour>())
            {
                if (s is not PlayerController) Destroy(s);
            }
            foreach (var col in visual.GetComponentsInChildren<Collider>()) Destroy(col);

            // Configure propeller: hide static prop, show blurred spinning prop
            var propStatic = visual.transform.Find("Prop1");
            if (propStatic != null) propStatic.gameObject.SetActive(false);

            var propBlur = visual.transform.Find("Prop1Blur");
            if (propBlur != null)
            {
                propBlur.gameObject.SetActive(true);
                propTransform = propBlur;
            }
            else if (propStatic != null)
            {
                propStatic.gameObject.SetActive(true);
                propTransform = propStatic;
            }
        }
        else
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(playerGO.transform, false);
            body.transform.localScale = new Vector3(0.5f, 0.5f, 0.8f);
            Destroy(body.GetComponent<Collider>());
        }

        // Add PlayerController
        var ctrl = playerGO.AddComponent<PlayerController>();
        ctrl.propeller = propTransform;

        // Player Rigidbody
        var rb = playerGO.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Player Collider (trigger aligned along Z fuselage axis)
        var playerCol = playerGO.AddComponent<CapsuleCollider>();
        playerCol.direction = 2;
        playerCol.height = 1.8f;
        playerCol.radius = 0.5f;
        playerCol.isTrigger = true;

        playerGO.transform.position = new Vector3(0, 0.5f, -16f);

        return playerGO;
    }
    
    private static (UIManager uiManager, GameObject hudPanel, GameObject pausePanel, GameObject lcPanel, GameObject goPanel, GameObject victoryPanel) BuildGameSceneUI(CameraController camCtrl)
    {
        // EventSystem already added
        
        // Main Canvas
        var canvasGO = new GameObject("GameCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // --- HUD Panel ---
        var hudPanel = CreateUIPanel(canvasGO.transform, "HUDPanel", new Color(0,0,0,0));
        SetAnchors(hudPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        
        // Top bar
        var topBar = CreateUIPanel(hudPanel.transform, "TopBar", new Color(0,0,0,0.5f));
        SetRectAnchors(topBar.GetComponent<RectTransform>(), new Vector2(0,0.9f), new Vector2(1,1), Vector2.zero, Vector2.zero);
        
        var lvlText = CreateLabel(topBar.transform, "LevelText", "Level 1", new Vector2(-700, 0));
        var capturedText = CreateLabel(topBar.transform, "CapturedText", "Captured: 0%", new Vector2(-300, 0));
        var targetText = CreateLabel(topBar.transform, "TargetText", "Target: 60%", new Vector2(0, 0));
        var timerText = CreateLabel(topBar.transform, "TimerText", "03:00", new Vector2(300, 0));
        var livesText = CreateLabel(topBar.transform, "LivesText", "Lives: 3", new Vector2(600, 0));
        var scoreText = CreateLabel(topBar.transform, "ScoreText", "Score: 0", new Vector2(800, 0));
        
        // Progress bar
        var sliderGO = new GameObject("CaptureSlider", typeof(RectTransform));
        sliderGO.transform.SetParent(topBar.transform, false);
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 0;
        var sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(0, -25);
        sliderRect.sizeDelta = new Vector2(600, 15);
        
        // PowerUp indicator
        var puIndicator = CreateLabel(hudPanel.transform, "PowerUpText", "", new Vector2(0, -30));
        puIndicator.GetComponent<TextMeshProUGUI>().fontSize = 28;
        puIndicator.GetComponent<TextMeshProUGUI>().color = Color.yellow;
        puIndicator.SetActive(false);
        
        // --- Pause Panel ---
        var pausePanel = CreateUIPanel(canvasGO.transform, "PausePanel", new Color(0, 0, 0, 0.8f));
        SetAnchors(pausePanel, new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f), Vector2.zero, Vector2.zero);
        pausePanel.SetActive(false);
        
        CreateLabel(pausePanel.transform, "PausedTitle", "PAUSED", new Vector2(0, 150));
        CreateMenuButton(pausePanel.transform, "ResumeButton", "RESUME", new Vector2(0, 60));
        CreateMenuButton(pausePanel.transform, "RestartButton", "RESTART", new Vector2(0, 0));
        CreateMenuButton(pausePanel.transform, "MenuFromPauseButton", "MAIN MENU", new Vector2(0, -60));
        
        // --- Level Complete Panel ---
        var lcPanel = CreateUIPanel(canvasGO.transform, "LevelCompletePanel", new Color(0, 0.1f, 0, 0.9f));
        SetAnchors(lcPanel, new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.8f), Vector2.zero, Vector2.zero);
        lcPanel.SetActive(false);
        
        var lcLvlText = CreateLabel(lcPanel.transform, "LCLevelText", "Level 1 Complete!", new Vector2(0, 180));
        var lcCapturedLabel = CreateLabel(lcPanel.transform, "LCCapturedText", "Captured: 0%", new Vector2(0, 100));
        var lcScoreLabel = CreateLabel(lcPanel.transform, "LCScoreText", "Score: 0", new Vector2(0, 40));
        var lcTimeBonusLabel = CreateLabel(lcPanel.transform, "LCTimeBonusText", "Time Bonus: +0", new Vector2(0, -20));
        CreateMenuButton(lcPanel.transform, "NextLevelButton", "NEXT LEVEL", new Vector2(0, -100));
        
        // --- Game Over Panel ---
        var goPanel = CreateUIPanel(canvasGO.transform, "GameOverPanel", new Color(0.15f, 0, 0, 0.95f));
        SetAnchors(goPanel, new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.8f), Vector2.zero, Vector2.zero);
        goPanel.SetActive(false);
        
        CreateLabel(goPanel.transform, "GameOverTitle", "GAME OVER", new Vector2(0, 180));
        var goFinalScore = CreateLabel(goPanel.transform, "GOFinalScoreText", "Final Score: 0", new Vector2(0, 80));
        var goBestScore = CreateLabel(goPanel.transform, "GOBestScoreText", "Best Score: 0", new Vector2(0, 20));
        CreateMenuButton(goPanel.transform, "NewGameButton", "NEW GAME", new Vector2(0, -60));
        CreateMenuButton(goPanel.transform, "MenuFromGOButton", "MAIN MENU", new Vector2(0, -130));
        
        // --- Victory Panel ---
        var victoryPanel = CreateUIPanel(canvasGO.transform, "VictoryPanel", new Color(0, 0.1f, 0.2f, 0.95f));
        SetAnchors(victoryPanel, new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.85f), Vector2.zero, Vector2.zero);
        victoryPanel.SetActive(false);
        
        CreateLabel(victoryPanel.transform, "VictoryTitle", "YOU WIN!", new Vector2(0, 220));
        CreateLabel(victoryPanel.transform, "VictorySub", "Congratulations! All levels complete!", new Vector2(0, 160));
        var victScoreText = CreateLabel(victoryPanel.transform, "VictoryScoreText", "Final Score: 0", new Vector2(0, 80));
        var victPctText = CreateLabel(victoryPanel.transform, "VictoryPercentText", "Territory: 0%", new Vector2(0, 20));
        CreateMenuButton(victoryPanel.transform, "PlayAgainButton", "PLAY AGAIN", new Vector2(0, -60));
        CreateMenuButton(victoryPanel.transform, "MenuFromVictoryButton", "MAIN MENU", new Vector2(0, -130));
        
        // --- Wire UIManager ---
        var uiManagerGO = new GameObject("UIManager");
        var uiManager = uiManagerGO.AddComponent<UIManager>();
        
        uiManager.levelText = lvlText.GetComponent<TextMeshProUGUI>();
        uiManager.capturedPercentText = capturedText.GetComponent<TextMeshProUGUI>();
        uiManager.targetPercentText = targetText.GetComponent<TextMeshProUGUI>();
        uiManager.timerText = timerText.GetComponent<TextMeshProUGUI>();
        uiManager.livesText = livesText.GetComponent<TextMeshProUGUI>();
        uiManager.scoreText = scoreText.GetComponent<TextMeshProUGUI>();
        uiManager.captureProgressSlider = slider;
        uiManager.hudPanel = hudPanel;
        uiManager.pausePanel = pausePanel;
        uiManager.levelCompletePanel = lcPanel;
        uiManager.gameOverPanel = goPanel;
        uiManager.victoryPanel = victoryPanel;
        uiManager.lcLevelText = lcLvlText.GetComponent<TextMeshProUGUI>();
        uiManager.lcCapturedText = lcCapturedLabel.GetComponent<TextMeshProUGUI>();
        uiManager.lcScoreText = lcScoreLabel.GetComponent<TextMeshProUGUI>();
        uiManager.lcTimeBonusText = lcTimeBonusLabel.GetComponent<TextMeshProUGUI>();
        uiManager.goFinalScoreText = goFinalScore.GetComponent<TextMeshProUGUI>();
        uiManager.goBestScoreText = goBestScore.GetComponent<TextMeshProUGUI>();
        uiManager.victoryScoreText = victScoreText.GetComponent<TextMeshProUGUI>();
        uiManager.victoryPercentText = victPctText.GetComponent<TextMeshProUGUI>();
        uiManager.powerUpText = puIndicator.GetComponent<TextMeshProUGUI>();
        
        // Wire pause buttons
        pausePanel.transform.Find("ResumeButton")?.GetComponent<Button>()?.onClick.AddListener(uiManager.OnResumeButton);
        pausePanel.transform.Find("RestartButton")?.GetComponent<Button>()?.onClick.AddListener(uiManager.OnRestartButton);
        pausePanel.transform.Find("MenuFromPauseButton")?.GetComponent<Button>()?.onClick.AddListener(uiManager.OnMainMenuButton);
        lcPanel.transform.Find("NextLevelButton")?.GetComponent<Button>()?.onClick.AddListener(uiManager.OnNextLevelButton);
        goPanel.transform.Find("NewGameButton")?.GetComponent<Button>()?.onClick.AddListener(uiManager.OnNewGameButton);
        goPanel.transform.Find("MenuFromGOButton")?.GetComponent<Button>()?.onClick.AddListener(uiManager.OnMainMenuButton);
        victoryPanel.transform.Find("PlayAgainButton")?.GetComponent<Button>()?.onClick.AddListener(uiManager.OnNewGameButton);
        victoryPanel.transform.Find("MenuFromVictoryButton")?.GetComponent<Button>()?.onClick.AddListener(uiManager.OnMainMenuButton);
        
        return (uiManager, hudPanel, pausePanel, lcPanel, goPanel, victoryPanel);
    }
    
    // ===================== UI HELPERS =====================
    
    private static GameObject CreateUIPanel(Transform parent, string name, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        return go;
    }
    
    private static void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
    
    private static void SetRectAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
    
    private static GameObject CreateLabel(Transform parent, string name, string text, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset");
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = 32;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(600, 60);
        return go;
    }
    
    private static GameObject CreateMenuButton(Transform parent, string name, string text, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.35f, 0.6f, 0.9f);
        
        var btn = go.AddComponent<Button>();
        
        // Set hover colors
        var colors = btn.colors;
        colors.normalColor = new Color(0.15f, 0.35f, 0.6f, 0.9f);
        colors.highlightedColor = new Color(0.25f, 0.5f, 0.8f, 1f);
        colors.pressedColor = new Color(0.1f, 0.25f, 0.45f, 1f);
        btn.colors = colors;
        
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(400, 60);
        
        // Button label
        var labelGO = new GameObject("Text", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset");
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        
        return go;
    }
    
    private static Slider CreateSlider(Transform parent, string name, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.75f;
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(260, 24);

        // Background
        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(go.transform, false);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        bgGO.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.25f, 0.95f);

        // Fill Area
        var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(go.transform, false);
        var fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.sizeDelta = new Vector2(-12f, 0f);

        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;
        fillGO.GetComponent<Image>().color = new Color(0.1f, 0.8f, 1f, 1f);
        slider.fillRect = fillRect;

        // Handle Slide Area
        var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGO.transform.SetParent(go.transform, false);
        var handleAreaRect = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-20f, 0f);

        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        var handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(26f, 26f);
        handleGO.GetComponent<Image>().color = Color.white;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleGO.GetComponent<Image>();

        return slider;
    }
    
    private static void Destroy(UnityEngine.Object obj)
    {
        GameObject.DestroyImmediate(obj);
    }
}
#endif
