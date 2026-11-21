using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Phase 4 Rebalanced Editor Builder for Assets/Scenes/Level2.unity:
/// - 65% Combat / 35% Easy Parkour (all horizontal gaps <= 3.8m [<=45% of 10m max sprint-jump])
/// - 3 Big Office Combat Arenas (21x21m, 24x24m, 24x24m) + 27x27m Boss Arena
/// - 14 Regular Enemies (10 RangedCrystal @ +3.5s, 4 CrystalTurret @ +5.5s) + 3-Phase OfficeBoss (+15s)
/// - TimeManager: startTime=70, maxTime=100, drainRate=0.85
/// - 4 Checkpoints (before Arena 1, Arena 2, Arena 3, Boss Arena) + Kill Zone
/// - Calls Level2DecorBuilder for 72 instanced void decor pieces driven by 1 FloatingDecorManager
/// Run via: Tools -> Level 2 -> Build Level 2 (Phase 4 Combat + Decor)
/// </summary>
public static class Level2Builder
{
    private const string ScenePath = "Assets/Scenes/Level2.unity";
    private const string Level1Path = "Assets/Scenes/Level1.unity";
    private const string MaterialFolder = "Assets/Materials/Level2";

    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string GameUIPrefabPath = "Assets/Prefabs/GameUI.prefab";
    private const string RangedEnemyPath = "Assets/Baloglan/Enemy/Ranged/RangedCrystal.prefab";
    private const string TurretEnemyPath = "Assets/Baloglan/Enemy/Turret/CrystalTurret.prefab";
    private const string BossPrefabPath = "Assets/Prefabs/OfficeBoss.prefab";
    private const string StartPortalPath = "Assets/Reversed Interactive/The Portal Collection/Prefabs/PortalOne.prefab";
    private const string ExitPortalPath = "Assets/Reversed Interactive/The Portal Collection/Prefabs/PortalTwo.prefab";
    private const string SketchTexPath = "Assets/ToonSketchShaderPack/Textures/PainterlyMaps/painterly1.png";
    private const string VolumeProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";

    // Office FBX paths
    private const string Floor3x3 = "Assets/OFFICE/carpet_floor/floor_3x3.fbx";
    private const string FloorCornerRound = "Assets/OFFICE/carpet_floor/floor_corner_round.fbx";
    private const string ConfDesk = "Assets/OFFICE/desks/conference_desk.fbx";
    private const string ConfDeskSq = "Assets/OFFICE/desks/conference_desk_squared.fbx";
    private const string Desk1 = "Assets/OFFICE/desks/desk1.fbx";
    private const string Desk2 = "Assets/OFFICE/desks/desk2.fbx";
    private const string DivFullHBlue = "Assets/OFFICE/dividers/divider_full_h_blue.fbx";
    private const string DivFullHRed = "Assets/OFFICE/dividers/divider_full_h_red.fbx";
    private const string DivLowBlue = "Assets/OFFICE/dividers/divider_black_blue_low.fbx";
    private const string DivLowRed = "Assets/OFFICE/dividers/divider_black_red_low.fbx";
    private const string CabLargest = "Assets/OFFICE/file_cabinets/file_cabinet_largest.fbx";
    private const string BookcaseSmall = "Assets/OFFICE/book_things/book_case_with_back_Small.fbx";
    private const string Whiteboard = "Assets/OFFICE/extras/whiteboard.fbx";
    private const string CoffeeMachine = "Assets/OFFICE/extras/coffee_machine.fbx";
    private const string DeskLamp = "Assets/OFFICE/extras/circular_desklamp.fbx";
    private const string Monitor = "Assets/OFFICE/computers/computer_monitor.fbx";

    private static readonly Dictionary<string, Material> toonMatCache = new Dictionary<string, Material>();
    private static Shader toonShader;
    private static Texture2D sketchTexture;

    [MenuItem("Tools/Level 2/Build Level 2 (Phase 4 Combat + Decor)")]
    [MenuItem("Tools/Build Level 2")]
    public static void BuildLevel2()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialFolder);
        EnsureFolder("Assets/Scenes");

        toonMatCache.Clear();
        toonShader = Shader.Find("Custom/URPToonSketch") ?? Shader.Find("Universal Render Pipeline/Lit");
        sketchTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SketchTexPath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Bright, Readable Realtime Lighting
        SetupLighting();

        // 2. Organized Roots
        Transform gameplayRoot = new GameObject("Gameplay").transform;
        Transform startRoot = new GameObject("Zone0_StartArrival").transform;
        Transform arena1Root = new GameObject("Zone1_Arena1_Reception").transform;
        Transform bridge1Root = new GameObject("Zone1B_Bridge1").transform;
        Transform arena2Root = new GameObject("Zone2_Arena2_OpenFloor").transform;
        Transform bridge2Root = new GameObject("Zone2B_Bridge2").transform;
        Transform arena3Root = new GameObject("Zone3_Arena3_UpperDeck").transform;
        Transform bossApproachRoot = new GameObject("Zone3B_BossApproach").transform;
        Transform bossArenaRoot = new GameObject("Zone4_BossArena").transform;
        Transform enemiesRoot = new GameObject("Enemies").transform;

        // 3. Gameplay Systems (Player, GameUI, TimeManager start=70 max=100 drain=0.85, 4 Checkpoints, KillZone)
        SetupGameplay(gameplayRoot, arena1Root, arena2Root, arena3Root, bossArenaRoot);

        // 4. Build Walkable Floating Office Decks & Short Easy Bridges (0 primitives, all gaps <= 3.8m)
        BuildStartIsland(startRoot);
        BuildArena1(arena1Root);
        BuildBridge1(bridge1Root);
        BuildArena2(arena2Root);
        BuildBridge2(bridge2Root);
        BuildArena3(arena3Root);
        BuildBossApproach(bossApproachRoot);
        BuildBossArena(bossArenaRoot);

        // 5. Place 14 Regular Enemies + 3-Phase Boss + Locked Exit Portal
        PlaceRegularEnemies(enemiesRoot);
        SetupBossFight(bossArenaRoot);

        // 6. Build 72 Floating Void Decor Pieces (Near/Mid/Far + single FloatingDecorManager)
        Level2DecorBuilder.BuildDecorInActiveScene();

        // 7. Save Scene, Bake NavMesh, Update Build Settings
        EditorSceneManager.SaveScene(scene, ScenePath);
        try
        {
            NavMeshBuilder.BuildNavMesh();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Level2Builder] NavMesh bake warning: {ex.Message}");
        }

        AddLevel2ToBuildSettings();
        AssetDatabase.SaveAssets();

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0f, 2f, 24f), new Vector3(28f, 12f, 40f)), false);

        Debug.Log("[Level2Builder] Phase 4 Level2 built: 3 Combat Arenas + Boss Arena, 14 regular enemies + Boss, 72 void decor pieces.");
    }

    // =========================================================================
    // ARENAS & EASY BRIDGES (100% Office FBX, gaps <= 3.8m, unit-scale colliders)
    // =========================================================================

    private static void BuildStartIsland(Transform parent)
    {
        BuildCarpetDeck(parent, "Start_Carpet_9x9", new Vector3(0f, 0f, 1.5f), 3, 3);
        PlaceSimplePrefab(StartPortalPath, parent, "ArrivalPortal_Visual", new Vector3(0f, 1.7f, -2.2f), 0f);
        PlaceProp(Desk2, parent, "Start_DeskL", new Vector3(-3.0f, 0f, 1.0f), 90f, fitCol: true);
        PlaceProp(Whiteboard, parent, "Start_BoardR", new Vector3(3.4f, 0f, 1.0f), -90f, fitCol: true);

        // Easy single bridge desk hop (2.4m gaps) from Start (Z=6) to Arena 1 (Z=14)
        PlaceProp(ConfDesk, parent, "Start_BridgeDesk", new Vector3(0f, -0.88f, 10.0f), 90f, fitCol: true, guide: true);
    }

    private static void BuildArena1(Transform parent)
    {
        // 21x21m flat Office deck (Z = 14..35, Y = 0)
        BuildCarpetDeck(parent, "Arena1_Carpet_21x21", new Vector3(0f, 0f, 24.5f), 7, 7);

        // Generous cover props
        PlaceProp(DivLowBlue, parent, "A1_Cover_FL", new Vector3(-4.5f, 0f, 20.5f), 15f, fitCol: true);
        PlaceProp(DivLowBlue, parent, "A1_Cover_FR", new Vector3(4.5f, 0f, 20.5f), -15f, fitCol: true);
        PlaceProp(ConfDesk, parent, "A1_CenterDesk", new Vector3(0f, 0f, 25.0f), 0f, fitCol: true);
        PlaceProp(BookcaseSmall, parent, "A1_BookcaseL", new Vector3(-7.5f, 0f, 28.0f), 90f, fitCol: true);
        PlaceProp(BookcaseSmall, parent, "A1_BookcaseR", new Vector3(7.5f, 0f, 28.0f), -90f, fitCol: true);
        PlaceProp(CabLargest, parent, "A1_CabBack", new Vector3(0f, 0f, 32.0f), 0f, fitCol: true, guide: true);
    }

    private static void BuildBridge1(Transform parent)
    {
        // Two wide conference desks (gaps ~2.5m) from Arena 1 (Z=35) to Arena 2 (Z=52, Y=0.4)
        PlaceProp(ConfDesk, parent, "B1_Desk1", new Vector3(0f, -0.68f, 40.0f), 90f, fitCol: true, guide: true);
        PlaceProp(ConfDesk, parent, "B1_Desk2", new Vector3(0f, -0.48f, 47.0f), 90f, fitCol: true, guide: true);
    }

    private static void BuildArena2(Transform parent)
    {
        // 24x24m flat Office deck (Z = 52..76, Y = 0.4)
        BuildCarpetDeck(parent, "Arena2_Carpet_24x24", new Vector3(0f, 0.4f, 64.0f), 8, 8);

        // Cover & slight turret pedestals
        PlaceProp(DivLowRed, parent, "A2_Cover_FL", new Vector3(-5.0f, 0.4f, 58.0f), 20f, fitCol: true);
        PlaceProp(DivLowRed, parent, "A2_Cover_FR", new Vector3(5.0f, 0.4f, 58.0f), -20f, fitCol: true);
        PlaceProp(DivFullHBlue, parent, "A2_CenterCubicle", new Vector3(0f, 0.4f, 64.0f), 0f, fitCol: true);
        PlaceProp(Desk1, parent, "A2_CoverMidL", new Vector3(-8.5f, 0.4f, 65.0f), 90f, fitCol: true);
        PlaceProp(Desk1, parent, "A2_CoverMidR", new Vector3(8.5f, 0.4f, 65.0f), -90f, fitCol: true);
        PlaceProp(ConfDeskSq, parent, "A2_TurretPadL", new Vector3(-7.5f, 0.4f, 72.5f), 20f, fitCol: true);
        PlaceProp(ConfDeskSq, parent, "A2_TurretPadR", new Vector3(7.5f, 0.4f, 72.5f), -20f, fitCol: true);
        PlaceProp(Whiteboard, parent, "A2_ExitMarker", new Vector3(0f, 0.4f, 74.5f), 0f, fitCol: true, guide: true);
    }

    private static void BuildBridge2(Transform parent)
    {
        // Wide cubicle + desk hops (gaps ~2.6m, gentle +0.4m steps) from Arena 2 (Z=76, Y=0.4) to Arena 3 (Z=94, Y=1.2)
        PlaceProp(DivFullHBlue, parent, "B2_Cubicle1", new Vector3(0f, -0.7f, 81.0f), 0f, fitCol: true, guide: true);
        PlaceProp(ConfDesk, parent, "B2_Desk2", new Vector3(0f, 0.32f, 88.5f), 90f, fitCol: true, guide: true);
    }

    private static void BuildArena3(Transform parent)
    {
        // 24x24m flat Office deck (Z = 94..118, Y = 1.2)
        BuildCarpetDeck(parent, "Arena3_Carpet_24x24", new Vector3(0f, 1.2f, 106.0f), 8, 8);

        // Cover + 2 elevated enemy ledges (Y = 2.7m top)
        PlaceProp(DivLowBlue, parent, "A3_Cover_FL", new Vector3(-5.0f, 1.2f, 100.0f), 15f, fitCol: true);
        PlaceProp(DivLowBlue, parent, "A3_Cover_FR", new Vector3(5.0f, 1.2f, 100.0f), -15f, fitCol: true);
        PlaceProp(DivFullHRed, parent, "A3_HighLedgeL", new Vector3(-8.0f, 1.2f, 108.0f), 0f, fitCol: true);
        PlaceProp(DivFullHRed, parent, "A3_HighLedgeR", new Vector3(8.0f, 1.2f, 108.0f), 0f, fitCol: true);
        PlaceProp(ConfDesk, parent, "A3_CenterCover", new Vector3(0f, 1.2f, 106.0f), 0f, fitCol: true);
        PlaceProp(ConfDeskSq, parent, "A3_BackDeck", new Vector3(0f, 1.2f, 114.5f), 0f, fitCol: true, guide: true);
    }

    private static void BuildBossApproach(Transform parent)
    {
        // 2 easy conference desks from Arena 3 (Z=118, Y=1.2) to Boss Arena (Z=134, Y=2.0)
        PlaceProp(ConfDesk, parent, "BA_Desk1", new Vector3(0f, 0.72f, 122.5f), 90f, fitCol: true, guide: true);
        PlaceProp(ConfDesk, parent, "BA_Desk2", new Vector3(0f, 1.12f, 129.5f), 90f, fitCol: true, guide: true);
    }

    private static void BuildBossArena(Transform parent)
    {
        // 27x27m flat Boss Arena (Z = 134..161, Y = 2.0, open edges over void)
        BuildCarpetDeck(parent, "BossArena_Carpet_27x27", new Vector3(0f, 2.0f, 147.5f), 9, 9, roundCorners: true);

        // Cover props & 3 perimeter turret pads
        PlaceProp(DivLowBlue, parent, "Boss_Cover_FL", new Vector3(-6.0f, 2.0f, 141.0f), 25f, fitCol: true);
        PlaceProp(DivLowBlue, parent, "Boss_Cover_FR", new Vector3(6.0f, 2.0f, 141.0f), -25f, fitCol: true);
        PlaceProp(BookcaseSmall, parent, "Boss_Cover_ML", new Vector3(-8.5f, 2.0f, 147.5f), 90f, fitCol: true);
        PlaceProp(BookcaseSmall, parent, "Boss_Cover_MR", new Vector3(8.5f, 2.0f, 147.5f), -90f, fitCol: true);
        PlaceProp(DivLowRed, parent, "Boss_Cover_BL", new Vector3(-5.5f, 2.0f, 153.5f), -20f, fitCol: true);
        PlaceProp(DivLowRed, parent, "Boss_Cover_BR", new Vector3(5.5f, 2.0f, 153.5f), 20f, fitCol: true);
        PlaceProp(Whiteboard, parent, "Boss_Board", new Vector3(-10.5f, 2.0f, 143.0f), 90f, fitCol: true);
        PlaceProp(CoffeeMachine, parent, "Boss_Coffee", new Vector3(10.5f, 2.0f, 143.0f), -90f, fitCol: true);

        PlaceProp(ConfDeskSq, parent, "Boss_TurretPad_Center", new Vector3(0f, 2.0f, 158.5f), 0f, fitCol: true);
        PlaceProp(ConfDeskSq, parent, "Boss_TurretPad_Left", new Vector3(-10.0f, 2.0f, 156.0f), 35f, fitCol: true);
        PlaceProp(ConfDeskSq, parent, "Boss_TurretPad_Right", new Vector3(10.0f, 2.0f, 156.0f), -35f, fitCol: true);
    }

    private static void BuildCarpetDeck(Transform parent, string name, Vector3 center, int tilesX, int tilesZ, bool roundCorners = false)
    {
        Transform deckRoot = new GameObject(name).transform;
        deckRoot.SetParent(parent, false);
        deckRoot.position = center;
        deckRoot.localScale = Vector3.one;

        float halfX = (tilesX - 1) * 1.5f;
        float halfZ = (tilesZ - 1) * 1.5f;

        for (int ix = 0; ix < tilesX; ix++)
        {
            for (int iz = 0; iz < tilesZ; iz++)
            {
                bool isCorner = roundCorners && (ix == 0 || ix == tilesX - 1) && (iz == 0 || iz == tilesZ - 1);
                string fbx = isCorner ? FloorCornerRound : Floor3x3;
                float yaw = 0f;
                if (isCorner)
                {
                    if (ix == 0 && iz == tilesZ - 1) yaw = 0f;
                    else if (ix == tilesX - 1 && iz == tilesZ - 1) yaw = 90f;
                    else if (ix == tilesX - 1 && iz == 0) yaw = 180f;
                    else yaw = 270f;
                }
                Vector3 pos = new Vector3(center.x - halfX + ix * 3f, center.y, center.z - halfZ + iz * 3f);
                InstantiateOfficeFbx(fbx, deckRoot, $"Tile_{ix}_{iz}", pos, yaw, isStatic: true, fitCollider: false);
            }
        }

        BoxCollider col = deckRoot.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -0.12f, 0f);
        col.size = new Vector3(tilesX * 3f, 0.24f, tilesZ * 3f);
        GameObjectUtility.SetStaticEditorFlags(deckRoot.gameObject,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic);
    }

    // =========================================================================
    // ENEMIES (14 Regular Enemies + OfficeBoss + 3 Phase Turrets)
    // =========================================================================

    private static void PlaceRegularEnemies(Transform root)
    {
        // Arena 1: 3 Ranged (+3.5s each)
        SpawnEnemy(RangedEnemyPath, root, "A1_Ranged_1", new Vector3(-5.5f, 0.05f, 26.0f), 165f, 3.5f);
        SpawnEnemy(RangedEnemyPath, root, "A1_Ranged_2", new Vector3(5.5f, 0.05f, 26.0f), 195f, 3.5f);
        SpawnEnemy(RangedEnemyPath, root, "A1_Ranged_3", new Vector3(0.0f, 0.05f, 31.0f), 180f, 3.5f);

        // Bridge 1: 1 Ranged (+3.5s)
        SpawnEnemy(RangedEnemyPath, root, "B1_Ranged_1", new Vector3(0.0f, 0.45f, 47.0f), 180f, 3.5f);

        // Arena 2: 2 Ranged (+3.5s) + 2 Turrets (+5.5s)
        SpawnEnemy(RangedEnemyPath, root, "A2_Ranged_1", new Vector3(-6.0f, 0.45f, 64.0f), 160f, 3.5f);
        SpawnEnemy(RangedEnemyPath, root, "A2_Ranged_2", new Vector3(6.0f, 0.45f, 64.0f), 200f, 3.5f);
        SpawnEnemy(TurretEnemyPath, root, "A2_Turret_L", new Vector3(-7.5f, 1.30f, 72.5f), 160f, 5.5f);
        SpawnEnemy(TurretEnemyPath, root, "A2_Turret_R", new Vector3(7.5f, 1.30f, 72.5f), 200f, 5.5f);

        // Bridge 2: 1 Ranged (+3.5s)
        SpawnEnemy(RangedEnemyPath, root, "B2_Ranged_1", new Vector3(0.0f, 1.25f, 88.5f), 180f, 3.5f);

        // Arena 3: 5 Enemies mixed (3 Ranged + 2 Turrets, 2 on elevated ledges)
        SpawnEnemy(RangedEnemyPath, root, "A3_Ranged_FloorL", new Vector3(-4.5f, 1.25f, 104.0f), 170f, 3.5f);
        SpawnEnemy(RangedEnemyPath, root, "A3_Ranged_FloorR", new Vector3(4.5f, 1.25f, 104.0f), 190f, 3.5f);
        SpawnEnemy(RangedEnemyPath, root, "A3_Ranged_HighL",  new Vector3(-8.0f, 2.75f, 108.0f), 150f, 3.5f);
        SpawnEnemy(TurretEnemyPath, root, "A3_Turret_HighR",  new Vector3(8.0f, 2.75f, 108.0f), -150f, 5.5f);
        SpawnEnemy(TurretEnemyPath, root, "A3_Turret_Back",   new Vector3(0.0f, 2.10f, 114.5f), 180f, 5.5f);
    }

    private static GameObject SpawnEnemy(string prefabPath, Transform parent, string name, Vector3 pos, float yaw, float reward)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return null;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        go.transform.localScale = Vector3.one;

        if (reward > 0f)
        {
            HealthTimeReward r = go.GetComponent<HealthTimeReward>() ?? go.AddComponent<HealthTimeReward>();
            r.secondsReward = reward;
        }

        GameObject beacon = new GameObject("EnemyAccentLight");
        beacon.transform.SetParent(go.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        Light l = beacon.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.32f, 0.18f);
        l.intensity = 2.4f;
        l.range = 5.0f;
        l.shadows = LightShadows.None;
        return go;
    }

    private static void SetupBossFight(Transform arenaRoot)
    {
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        if (bossPrefab == null) bossPrefab = CreateBossPrefab();

        GameObject bossInstance = null;
        Health bossHealth = null;
        if (bossPrefab != null)
        {
            bossInstance = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);
            bossInstance.name = "OfficeBoss";
            bossInstance.transform.SetParent(arenaRoot, false);
            bossInstance.transform.SetPositionAndRotation(new Vector3(0f, 2.05f, 151.5f), Quaternion.Euler(0f, 180f, 0f));
            bossInstance.transform.localScale = Vector3.one;
            bossHealth = bossInstance.GetComponent<Health>();
        }

        GameObject t1 = SpawnEnemy(TurretEnemyPath, arenaRoot, "Arena_Phase1_Turret", new Vector3(0f, 2.90f, 158.5f), 180f, 5.5f);
        GameObject t2 = SpawnEnemy(TurretEnemyPath, arenaRoot, "Arena_Phase2_Turret", new Vector3(-10.0f, 2.90f, 156.0f), 145f, 5.5f);
        GameObject t3 = SpawnEnemy(TurretEnemyPath, arenaRoot, "Arena_Phase3_Turret", new Vector3(10.0f, 2.90f, 156.0f), -145f, 5.5f);

        GameObject exitRoot = new GameObject("ExitPortal_BossLocked");
        exitRoot.transform.SetParent(arenaRoot, false);
        exitRoot.transform.position = new Vector3(0f, 3.75f, 155.5f);
        exitRoot.transform.localScale = Vector3.one;
        PlaceSimplePrefab(ExitPortalPath, exitRoot.transform, "ExitPortal_Visual", Vector3.zero, 0f, local: true);

        BoxCollider exitTrig = exitRoot.AddComponent<BoxCollider>();
        exitTrig.isTrigger = true;
        exitTrig.size = new Vector3(3.4f, 4.0f, 2.2f);
        PortalTrigger pt = exitRoot.AddComponent<PortalTrigger>();
        SerializedObject soPt = new SerializedObject(pt);
        soPt.FindProperty("targetScene").stringValue = "MainMenu";
        soPt.FindProperty("loadingSceneName").stringValue = "Loading";
        soPt.FindProperty("playerTag").stringValue = "Player";
        soPt.ApplyModifiedPropertiesWithoutUndo();

        GameObject ctrlGo = new GameObject("BossArena_Controller");
        ctrlGo.transform.SetParent(arenaRoot, false);
        ctrlGo.transform.position = new Vector3(0f, 3.5f, 138.0f);
        ctrlGo.transform.localScale = Vector3.one;
        BoxCollider introCol = ctrlGo.AddComponent<BoxCollider>();
        introCol.isTrigger = true;
        introCol.size = new Vector3(26f, 5f, 4f);

        BossArenaController ctrl = ctrlGo.AddComponent<BossArenaController>();
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("bossRoot").objectReferenceValue = bossInstance;
        so.FindProperty("bossHealth").objectReferenceValue = bossHealth;
        so.FindProperty("bossTitle").stringValue = "BOSS";
        so.FindProperty("bossDeathTimeReward").floatValue = 15f;
        so.FindProperty("phase2Threshold").floatValue = 0.66f;
        so.FindProperty("phase3Threshold").floatValue = 0.33f;
        so.FindProperty("phase1Turret").objectReferenceValue = t1;
        so.FindProperty("phase2Turret").objectReferenceValue = t2;
        so.FindProperty("phase3Turret").objectReferenceValue = t3;
        so.FindProperty("exitPortalRoot").objectReferenceValue = exitRoot;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateBossPrefab()
    {
        EnsureFolder("Assets/Prefabs");
        GameObject baseRanged = AssetDatabase.LoadAssetAtPath<GameObject>(RangedEnemyPath);
        if (baseRanged == null) return null;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(baseRanged);
        instance.name = "OfficeBoss";
        instance.transform.localScale = Vector3.one;
        foreach (Transform child in instance.transform)
            if (child.GetComponent<Collider>() == null) child.localScale = Vector3.one * 1.75f;

        Health h = instance.GetComponent<Health>();
        if (h != null)
        {
            SerializedObject soH = new SerializedObject(h);
            SerializedProperty p = soH.FindProperty("maxHealth");
            if (p != null) p.floatValue = 800f;
            soH.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, BossPrefabPath);
        Object.DestroyImmediate(instance);
        return saved;
    }

    // =========================================================================
    // LIGHTING, GAMEPLAY, CHECKPOINTS & UNIT-SCALE OFFICE PROP HELPER
    // =========================================================================

    private static void SetupLighting()
    {
        Transform lightRoot = new GameObject("Lighting").transform;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.82f, 0.85f, 0.92f);
        RenderSettings.ambientEquatorColor = new Color(0.64f, 0.62f, 0.66f);
        RenderSettings.ambientGroundColor = new Color(0.28f, 0.26f, 0.32f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.08f, 0.10f, 0.15f);
        RenderSettings.fogStartDistance = 85f;
        RenderSettings.fogEndDistance = 260f;

        GameObject dirGo = new GameObject("Directional Light");
        dirGo.transform.SetParent(lightRoot, false);
        dirGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light dir = dirGo.AddComponent<Light>();
        dir.type = LightType.Directional;
        dir.color = new Color(1f, 0.97f, 0.92f);
        dir.intensity = 1.45f;
        dir.shadows = LightShadows.Soft;

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile != null)
        {
            GameObject volGo = new GameObject("Global Volume");
            volGo.transform.SetParent(lightRoot, false);
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.profile = profile;
        }

        CreateArenaLight(lightRoot, "Light_Arena1", new Vector3(0f, 7f, 24.5f), 2.4f, 28f);
        CreateArenaLight(lightRoot, "Light_Arena2", new Vector3(0f, 7.5f, 64f), 2.6f, 30f);
        CreateArenaLight(lightRoot, "Light_Arena3", new Vector3(0f, 8.5f, 106f), 2.6f, 30f);
        CreateArenaLight(lightRoot, "Light_BossArena", new Vector3(0f, 9.5f, 147.5f), 3.0f, 34f);
    }

    private static void CreateArenaLight(Transform parent, string name, Vector3 pos, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.92f, 0.96f, 1f);
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    private static void SetupGameplay(Transform root, Transform a1, Transform a2, Transform a3, Transform bossA)
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab != null)
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetParent(root, false);
            player.transform.SetPositionAndRotation(new Vector3(0f, 1.05f, 0.5f), Quaternion.identity);
            player.transform.localScale = Vector3.one;
            player.tag = "Player";

            if (player.GetComponent<Health>() == null) player.AddComponent<Health>();
            if (player.GetComponent<PlayerSlow>() == null) player.AddComponent<PlayerSlow>();
            if (player.GetComponent<PlayerHitReceiver>() == null) player.AddComponent<PlayerHitReceiver>();

            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.07f, 0.09f, 0.14f, 1f);
            }
        }

        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameUIPrefabPath);
        if (uiPrefab != null)
        {
            GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab);
            ui.transform.SetParent(root, false);
            ui.transform.localScale = Vector3.one;

            TimeManager tm = ui.GetComponent<TimeManager>();
            if (tm != null)
            {
                tm.startTime = 70f;
                tm.maxTime = 100f;
                tm.drainRate = 0.85f;
                tm.isRunning = true;
            }
        }

        GameObject cpMgr = new GameObject("CheckpointManager");
        cpMgr.transform.SetParent(root, false);
        cpMgr.AddComponent<CheckpointManager>();

        CreateCheckpoint(a1, "Checkpoint_1_Arena1", new Vector3(0f, 0.10f, 15.5f));
        CreateCheckpoint(a2, "Checkpoint_2_Arena2", new Vector3(0f, 0.50f, 53.5f));
        CreateCheckpoint(a3, "Checkpoint_3_Arena3", new Vector3(0f, 1.30f, 95.5f));
        CreateCheckpoint(bossA, "Checkpoint_4_BossArena", new Vector3(0f, 2.10f, 135.5f));

        GameObject killZone = new GameObject("KillZone");
        killZone.transform.SetParent(root, false);
        killZone.transform.position = new Vector3(0f, -25f, 80f);
        killZone.transform.localScale = Vector3.one;
        BoxCollider kzCol = killZone.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(240f, 6f, 320f);
        killZone.AddComponent<KillZoneRespawn>();
    }

    private static void CreateCheckpoint(Transform parent, string name, Vector3 pos)
    {
        GameObject cp = new GameObject(name);
        cp.transform.SetParent(parent, false);
        cp.transform.position = pos;
        cp.transform.localScale = Vector3.one;

        BoxCollider trig = cp.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.size = new Vector3(8f, 3.5f, 2.5f);

        InstantiateOfficeFbx(DeskLamp, cp.transform, $"{name}_Lamp", pos, 0f, isStatic: true, fitCollider: false);

        GameObject lightGo = new GameObject("CheckpointLight");
        lightGo.transform.SetParent(cp.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.78f, 0.25f);
        l.intensity = 2.5f;
        l.range = 6.5f;
        l.shadows = LightShadows.None;

        CheckpointTrigger ct = cp.AddComponent<CheckpointTrigger>();
        SerializedObject so = new SerializedObject(ct);
        so.FindProperty("indicatorLight").objectReferenceValue = l;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject PlaceProp(string path, Transform parent, string name, Vector3 pos, float yaw, bool fitCol, bool guide = false)
    {
        GameObject go = InstantiateOfficeFbx(path, parent, name, pos, yaw, isStatic: true, fitCollider: fitCol);
        if (go != null && guide)
        {
            BoxCollider c = go.GetComponent<BoxCollider>();
            float topY = c != null ? (c.center.y + c.size.y * 0.5f) : 0.9f;
            Vector3 lampPos = pos + new Vector3(0f, topY, 0f);
            InstantiateOfficeFbx(DeskLamp, go.transform, $"{name}_GuideLamp", lampPos, yaw, isStatic: true, fitCollider: false);
            GameObject lg = new GameObject($"{name}_GuideLight");
            lg.transform.SetParent(go.transform, false);
            lg.transform.position = lampPos + new Vector3(0f, 0.45f, 0f);
            Light l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.32f, 0.92f, 1f);
            l.intensity = 1.8f;
            l.range = 4.5f;
            l.shadows = LightShadows.None;
        }
        return go;
    }

    private static void PlaceSimplePrefab(string path, Transform parent, string name, Vector3 pos, float yaw, bool local = false)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) return;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.name = name;
        go.transform.SetParent(parent, false);
        if (local) { go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f); }
        else go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        go.transform.localScale = Vector3.one;
    }

    private static GameObject InstantiateOfficeFbx(string assetPath, Transform parent, string name, Vector3 worldPos, float yaw, bool isStatic, bool fitCollider)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null) return null;

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, yaw, 0f));
        root.transform.localScale = Vector3.one;

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        visual.name = "Model";
        Quaternion nativeRot = asset.transform.localRotation;
        Vector3 nativeScale = asset.transform.localScale;
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = nativeRot;
        visual.transform.localScale = nativeScale;

        Renderer[] rends = visual.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (maxDim > 0.0001f && maxDim < 0.12f)
                visual.transform.localScale = nativeScale * 100f;
        }

        ApplyToonMaterials(visual);
        if (fitCollider) OfficePropColliderAndMaterialFixer.FitFlatWalkableBoxCollider(root);

        if (isStatic)
        {
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.NavigationStatic;
            SetFlagsRecursive(root, flags);
        }
        return root;
    }

    private static void ApplyToonMaterials(GameObject go)
    {
        if (toonShader == null) return;
        foreach (MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer>())
        {
            Material[] shared = mr.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
            {
                Material src = shared[i];
                string key = src != null ? src.name : "DefaultOffice";
                if (!toonMatCache.TryGetValue(key, out Material m) || m == null)
                {
                    string safe = key.Replace("/", "_").Replace(":", "_");
                    string path = $"{MaterialFolder}/Toon_{safe}.mat";
                    m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (m == null) { m = new Material(toonShader); AssetDatabase.CreateAsset(m, path); }
                    else m.shader = toonShader;

                    Texture tex = null;
                    Color col = new Color(0.94f, 0.91f, 0.86f);
                    if (src != null)
                    {
                        if (src.HasProperty("_BaseMap")) tex = src.GetTexture("_BaseMap");
                        if (tex == null && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex");
                        if (src.HasProperty("_BaseColor")) col = src.GetColor("_BaseColor");
                        else if (src.HasProperty("_Color")) col = src.GetColor("_Color");
                    }
                    m.enableInstancing = true;
                    if (m.HasProperty("_BaseMap") && tex != null) m.SetTexture("_BaseMap", tex);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                    if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", new Color(0.56f, 0.54f, 0.60f));
                    if (m.HasProperty("_SketchMap") && sketchTexture != null) m.SetTexture("_SketchMap", sketchTexture);
                    if (m.HasProperty("_SketchScale")) m.SetFloat("_SketchScale", 0.45f);
                    if (m.HasProperty("_SketchStrength")) m.SetFloat("_SketchStrength", 0.38f);
                    if (m.HasProperty("_ToonSteps")) m.SetFloat("_ToonSteps", 3f);
                    if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", new Color(0.10f, 0.09f, 0.12f));
                    if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0.018f);
                    EditorUtility.SetDirty(m);
                    toonMatCache[key] = m;
                }
                shared[i] = m;
            }
            mr.sharedMaterials = shared;
        }
    }

    private static void SetFlagsRecursive(GameObject root, StaticEditorFlags flags)
    {
        GameObjectUtility.SetStaticEditorFlags(root, flags);
        foreach (Transform child in root.transform) SetFlagsRecursive(child.gameObject, flags);
    }

    private static void AddLevel2ToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == ScenePath);
        int l1 = scenes.FindIndex(s => s.path == Level1Path);
        if (l1 >= 0) scenes.Insert(l1 + 1, new EditorBuildSettingsScene(ScenePath, true));
        else
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Level1Path) != null)
                scenes.Add(new EditorBuildSettingsScene(Level1Path, true));
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = System.IO.Path.GetDirectoryName(folderPath).Replace("\\", "/");
        string child = System.IO.Path.GetFileName(folderPath);
        AssetDatabase.CreateFolder(parent, child);
    }
}
