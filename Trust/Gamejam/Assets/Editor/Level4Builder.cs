using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Complete deterministic builder for Assets/Scenes/Level4.unity.
/// Floating 3D Office Platformer + Combat Combo course built 100% from Assets/OFFICE/
/// (weaving stepping-stone desk pads, narrow desk bridges, multi-elevation climbs, twin flank walls,
/// slide archway, blast-jump staircases, 13 NavMesh-backed RangedCrystal + 12 CrystalTurret = 25 enemies,
/// 20 golden wall_clock TimeOrbs, automatic reload enabled, SkyBox2 void, 90 drifting Office decor).
/// Auto-builds if Assets/Scenes/Level4.unity is missing or outdated and appends cleanly to Build Settings.
/// Run via: Tools -> Level 4 -> Build Complete Level 4 (or Tools -> Build Level 4)
/// </summary>
public static partial class Level4Builder
{
    public const string ScenePath = "Assets/Scenes/Level4.unity";
    private const string NavAssetPath = "Assets/Scenes/Level4_NavMesh.asset";
    private const string MaterialFolder = "Assets/Materials/Level4";
    private const string DecorMaterialFolder = "Assets/Materials/Level4/Decor";
    private const string SketchTexPath = "Assets/ToonSketchShaderPack/Textures/PainterlyMaps/painterly1.png";
    private const string SkyBoxMatPath = "Assets/Materials/SkyBox2.mat";

    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string GameUIPrefabPath = "Assets/Prefabs/GameUI.prefab";
    private const string StartPortalPath = "Assets/Reversed Interactive/The Portal Collection/Prefabs/PortalOne.prefab";
    private const string ExitPortalPath = "Assets/Reversed Interactive/The Portal Collection/Prefabs/PortalTwo.prefab";

    private const string PistolPickupPath = "Assets/Baloglan/Weapons/Pickups/PistolPickup.prefab";
    private const string ShotgunPickupPath = "Assets/Baloglan/Weapons/Pickups/TripleShotgunPickup.prefab";
    private const string AssaultPickupPath = "Assets/Baloglan/Weapons/Pickups/AssaultRifle_Pickup.prefab";
    private const string TurretEnemyPath = "Assets/Baloglan/Enemy/Turret/CrystalTurret.prefab";
    private const string RangedEnemyPath = "Assets/Baloglan/Enemy/Ranged/RangedCrystal.prefab";
    private const string OrbParticleMatPath = "Assets/Baloglan/Weapons/Materials/MuzzleFlashAdditive.mat";

    // 100% Assets/OFFICE/ Props
    private const string ConfDesk = "Assets/OFFICE/desks/conference_desk.fbx";
    private const string ConfDeskAlt = "Assets/OFFICE/desks/conference_desk_alt.fbx";
    private const string ConfDeskSq = "Assets/OFFICE/desks/conference_desk_squared.fbx";
    private const string ConfDeskSqAlt = "Assets/OFFICE/desks/conference_desk_squared_alt.fbx";
    private const string Desk1 = "Assets/OFFICE/desks/desk1.fbx";
    private const string Desk2 = "Assets/OFFICE/desks/desk2.fbx";
    private const string DivBlackBlueWall = "Assets/OFFICE/dividers/divider_black_blue.fbx";
    private const string DivLowBlue = "Assets/OFFICE/dividers/divider_black_blue_low.fbx";
    private const string DivVeneerRed = "Assets/OFFICE/dividers/divider_veneer_red.fbx";
    private const string DivFullHBlue = "Assets/OFFICE/dividers/divider_full_h_blue.fbx";
    private const string DivFullHRed = "Assets/OFFICE/dividers/divider_full_h_red.fbx";
    private const string FileCabLargest = "Assets/OFFICE/file_cabinets/file_cabinet_largest.fbx";
    private const string FileCabLarge = "Assets/OFFICE/file_cabinets/file_cabinet_large.fbx";
    private const string BookcaseBack = "Assets/OFFICE/book_things/book_case_with_back.fbx";
    private const string BookcaseSmall = "Assets/OFFICE/book_things/book_case_with_back_Small.fbx";
    private const string BooksFbx = "Assets/OFFICE/book_things/books.fbx";
    private const string BooksAltFbx = "Assets/OFFICE/book_things/books_alt.fbx";
    private const string OfficeChairBlue = "Assets/OFFICE/chairs/office_chair_blue.fbx";
    private const string OfficeChairOrange = "Assets/OFFICE/chairs/office_chair_orange.fbx";
    private const string ConfChair = "Assets/OFFICE/chairs/conference_chair.fbx";
    private const string MonitorFbx = "Assets/OFFICE/computers/computer_monitor.fbx";
    private const string TowerFbx = "Assets/OFFICE/computers/computer_tower.fbx";
    private const string KeyboardFbx = "Assets/OFFICE/computers/computer_keyboard.fbx";
    private const string CouchLargeBlue = "Assets/OFFICE/couches/couch_large_blue.fbx";
    private const string CouchLargeRed = "Assets/OFFICE/couches/couch_large_red.fbx";
    private const string ArmchairBlue = "Assets/OFFICE/couches/armchair_blue.fbx";
    private const string DeskLamp = "Assets/OFFICE/extras/circular_desklamp.fbx";
    private const string Whiteboard = "Assets/OFFICE/extras/whiteboard.fbx";
    private const string CoffeeMachine = "Assets/OFFICE/extras/coffee_machine.fbx";
    private const string WaterCooler = "Assets/OFFICE/extras/water_cooler_bottle_attached.fbx";
    private const string WallClockFbx = "Assets/OFFICE/extras/wall_clock.fbx";
    private const string PaperStackLarge = "Assets/OFFICE/stationary/paper_stack_large.fbx";

    // Soft & Fast Forgiveness Constants
    private const float EdgeMargin = 0.35f;      // +0.35m forgiving platform edges
    private const float DeskH = 0.88f;
    private const float DeskD = 2.57f;
    private const float DeskRowStepZ = 2.62f;    // 0.05m seam (zero z-fighting)
    private const float StripWidth = 4.97f;      // 1-col ConfDesk expressway bridge
    private const float WallPanelLen = 3.06f;

    private const int GroundLayer = 3; // Layer 3: Ground (used by player & NavMeshSurface)
    private const int WallLayer = 6;   // Layer 6: Wall (wallrunLayers = 1 << 6)
    private const int DefaultLayer = 0;

    private static float DeckLen(int rows) => rows * DeskRowStepZ - 0.05f;

    private static readonly Dictionary<string, Material> toonMatCache = new Dictionary<string, Material>();
    private static readonly Dictionary<string, Material> decorMatCache = new Dictionary<string, Material>();
    private static Shader toonShader;
    private static Texture2D sketchTexture;
    private static Material orbParticleMat;
    private static Material goldenOrbMat;
    private static Health scenePlayerHealth;
    private static int propCount;
    private static int turretCount;
    private static int rangedCount;
    private static int orbCount;

    private enum WingTheme { Reception, Lounge, Executive, Archive }

    [InitializeOnLoadMethod]
    private static void AutoBuildIfMissing()
    {
        bool needsBuild = !File.Exists(ScenePath);
        if (!needsBuild)
        {
            string txt = File.ReadAllText(ScenePath);
            needsBuild = !txt.Contains("L4_Finale_Ranged1") || !txt.Contains("Level4_NavMesh");
        }

        if (!needsBuild)
            return;

        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                BuildLevel4();
        };
    }

    [MenuItem("Tools/Level 4/Build Complete Level 4")]
    [MenuItem("Tools/Build Level 4")]
    public static void BuildLevel4()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialFolder);
        EnsureFolder(DecorMaterialFolder);
        EnsureFolder("Assets/Scenes");

        toonMatCache.Clear();
        decorMatCache.Clear();
        scenePlayerHealth = null;
        propCount = 0;
        turretCount = 0;
        rangedCount = 0;
        orbCount = 0;

        toonShader = Shader.Find("Custom/URPToonSketch") ?? Shader.Find("Universal Render Pipeline/Lit");
        sketchTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SketchTexPath);
        orbParticleMat = AssetDatabase.LoadAssetAtPath<Material>(OrbParticleMatPath);
        goldenOrbMat = GetOrCreateAccentMaterial("L4_TimeOrbGold", new Color(1f, 0.84f, 0.22f), new Color(1.25f, 0.88f, 0.16f));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupLighting();

        Transform gameplayRoot = new GameObject("Gameplay").transform;
        Transform sec1 = new GameObject("S1_SteppingStoneClimb").transform;
        Transform sec2 = new GameObject("S2_Combo1_RightWallSquad").transform;
        Transform sec3 = new GameObject("S3_Combo2_ZigZagWallsAmbush").transform;
        Transform sec4 = new GameObject("S4_Combo3_SlideGantryDashSteps").transform;
        Transform sec5 = new GameObject("S5_Combo4_BlastStaircaseWall").transform;
        Transform sec6 = new GameObject("S6_Combo5_BlastTwinWallExit").transform;
        Transform sec7 = new GameObject("S7_MultiTierBoardroomFinale").transform;
        Transform orbsRoot = new GameObject("TimeOrbs").transform;
        Transform enemiesRoot = new GameObject("Enemies").transform;

        SetupGameplay(gameplayRoot);

        float z;
        float y;
        (z, y) = BuildS1_LaunchpadAndSteppingClimb(sec1, gameplayRoot, enemiesRoot, orbsRoot);
        (z, y) = BuildS2_Combo1_RightWallSquad(sec2, gameplayRoot, enemiesRoot, orbsRoot, z, y);
        (z, y) = BuildS3_Combo2_ZigZagWallsAmbush(sec3, gameplayRoot, enemiesRoot, orbsRoot, z, y);
        (z, y) = BuildS4_Combo3_SlideGantryDashSteps(sec4, gameplayRoot, enemiesRoot, orbsRoot, z, y);
        (z, y) = BuildS5_Combo4_BlastStaircaseWall(sec5, gameplayRoot, enemiesRoot, orbsRoot, z, y);
        (z, y) = BuildS6_Combo5_BlastTwinWallExit(sec6, gameplayRoot, enemiesRoot, orbsRoot, z, y);
        BuildS7_MultiTierBoardroomFinale(sec7, gameplayRoot, enemiesRoot, orbsRoot, z, y);

        BuildFloatingOfficeVoidDecor();

        // Save scene first so NavMeshData asset links properly, bake Unity 6 NavMeshSurface over Ground colliders, then re-save
        EditorSceneManager.SaveScene(scene, ScenePath);
        BakeNavMeshSurface(gameplayRoot);
        EditorSceneManager.SaveScene(scene, ScenePath);

        AppendToBuildSettingsWithoutReordering();
        AssetDatabase.SaveAssets();

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0f, 6f, 145f), new Vector3(52f, 24f, 300f)), false);

        Debug.Log($"[Level4Builder] Built Level 4 at {ScenePath}: {propCount} Office props, {rangedCount} ranged crystals, {turretCount} turrets, the Mainframe boss, {orbCount} time orbs and 8 checkpoints.");
    }

    // =========================================================================
    // S1: NARROW LAUNCHPAD + 3 WEAVING STEPPING-STONE PADS (Y = 0 -> +2.40)
    // =========================================================================
    private static (float endZ, float topY) BuildS1_LaunchpadAndSteppingClimb(
        Transform parent, Transform gameplay, Transform enemies, Transform orbs)
    {
        // Narrow 5.0m expressway bridge (Z = -5.53 .. 12.76, 7 rows)
        float startZ = -5.53f;
        float stripLen = DeckLen(7); // 18.29
        float stripEnd = startZ + stripLen; // 12.76
        BuildRunwayStrip(parent, "S1_Launchpad_Bridge", new Vector3(0f, 0f, startZ + stripLen * 0.5f), rows: 7);

        PlaceSimplePrefab(StartPortalPath, parent, "StartPortal_Visual", new Vector3(0f, 1.70f, -3.8f), 0f);
        PlaceFastPickup(PistolPickupPath,  parent, "S1_PistolPickup",       new Vector3(0f, 0.35f, 3.0f), new Color(0.25f, 0.90f, 1.0f));
        PlaceFastPickup(ShotgunPickupPath, parent, "S1_ShotgunPickup",      new Vector3(0f, 0.35f, 6.0f), new Color(1.00f, 0.72f, 0.22f));
        PlaceFastPickup(AssaultPickupPath, parent, "S1_AssaultRiflePickup", new Vector3(0f, 0.35f, 9.0f), new Color(0.32f, 1.00f, 0.55f));
        CreateCheckpoint(gameplay, "Checkpoint_1_Start", new Vector3(0f, 0.05f, -1.0f), bonusSeconds: 0f, wide: false);

        // Weaving Floating Pad 1 (Left, Y = +0.90, Z = 15.80 .. 19.91)
        BuildSquareDeskPad(parent, "S1_StepPad1_Left", new Vector3(-2.8f, 0.90f, 17.855f));
        CreateTimeOrb(orbs, "S1_PadOrb_1", new Vector3(-2.8f, 2.00f, 17.85f));

        // Weaving Floating Pad 2 (Right, Y = +1.80, Z = 22.80 .. 26.91) with 1st Ranged Enemy!
        BuildSquareDeskPad(parent, "S1_StepPad2_Right", new Vector3(2.8f, 1.80f, 24.855f));
        SpawnRangedEnemy(enemies, "L4_S1_Ranged1", new Vector3(2.8f, 1.80f, 25.0f), 190f, reward: 4.0f);

        // Floating Island 3 (Center, Y = +2.40, Z = 30.10 .. 35.07) with Turret + CP2 leading into Wall 1
        BuildDeskIsland(parent, "S1_StepIsland3_Launch", new Vector3(0f, 2.40f, 32.585f));
        SpawnFastTurret(parent, enemies, "L4_S1_Turret1", new Vector3(-2.1f, 2.40f, 32.8f), 170f, reward: 4.0f, useFileCab: false);
        CreateCheckpoint(gameplay, "Checkpoint_2_Wall1", new Vector3(0f, 2.45f, 31.8f), bonusSeconds: 8f, wide: false);

        return (35.07f, 2.40f);
    }

    // =========================================================================
    // S2: COMBO 1 — RIGHT WALL (14.0m) -> WALL-END TURRET + RANGED SQUAD -> SIDE PAD
    // =========================================================================
    private static (float endZ, float topY) BuildS2_Combo1_RightWallSquad(
        Transform parent, Transform gameplay, Transform enemies, Transform orbs, float prevEndZ, float y)
    {
        const float gap = 14.00f;
        float deckStart = prevEndZ + gap; // 49.07
        BuildWallRunSegment(parent, "S2_WallRun_Right", xPos: 3.10f, yBottom: y, prevEndZ, deckStart, segCount: 5, faceLeftTowardLane: true);
        CreateTimeOrbArc(orbs, "S2_WallOrb", new Vector3(2.05f, y + 1.45f, 0f), prevEndZ + 3.2f, deckStart - 3.2f, count: 3);

        float deckLen = DeckLen(6); // 15.67 -> ends at 64.74
        float deckEnd = deckStart + deckLen;
        BuildOfficePlatform(parent, "S2_Combat_Deck", new Vector3(0f, y, deckStart + deckLen * 0.5f), cols: 2, rows: 6, WingTheme.Executive);

        SpawnFastTurret(parent, enemies, "L4_C1_WallEndTurret", new Vector3( 2.9f, y, deckStart + 3.8f), 175f, reward: 4.0f, useFileCab: false);
        SpawnRangedEnemy(enemies, "L4_C1_Ranged1", new Vector3(-3.0f, y, deckStart + 7.5f), 170f, reward: 4.0f);
        SpawnRangedEnemy(enemies, "L4_C1_Ranged2", new Vector3( 2.6f, y, deckStart + 10.8f), 190f, reward: 4.0f);

        // Weaving Side Launch Pad aimed directly at S3 Left Wall (X = -3.4, Y = +2.80, Z = 67.60 .. 71.71)
        float padCenterZ = deckEnd + 2.86f + 2.055f; // front edge = 67.60, end = 71.71
        float padY = y + 0.40f; // 2.80
        BuildSquareDeskPad(parent, "S2_LeftLaunchPad", new Vector3(-3.4f, padY, padCenterZ));
        CreateCheckpoint(gameplay, "Checkpoint_3_ChainWalls", new Vector3(-3.4f, padY + 0.05f, padCenterZ), bonusSeconds: 8f, wide: false);

        return (deckEnd + 2.86f + 4.11f, padY);
    }

    // =========================================================================
    // S3: COMBO 2 — LEFT WALL -> BOUNCE ISLAND RANGED -> RIGHT WALL -> DOUBLE EXIT
    // =========================================================================
    private static (float endZ, float topY) BuildS3_Combo2_ZigZagWallsAmbush(
        Transform parent, Transform gameplay, Transform enemies, Transform orbs, float prevEndZ, float prevY)
    {
        const float wallGap = 11.50f;
        float bounceStart = prevEndZ + wallGap; // 83.21
        float bounceY = prevY + 0.60f;          // 3.40
        BuildWallRunSegment(parent, "S3_WallRun_Left", xPos: -3.10f, yBottom: prevY, prevEndZ, bounceStart, segCount: 4, faceLeftTowardLane: false);
        CreateTimeOrbArc(orbs, "S3_OrbL", new Vector3(-2.05f, prevY + 1.45f, 0f), prevEndZ + 3.0f, bounceStart - 3.0f, count: 2);

        float bounceLen = 4.97f;
        float bounceEnd = bounceStart + bounceLen; // 88.18
        BuildDeskIsland(parent, "S3_BounceIsland", new Vector3(0f, bounceY, bounceStart + bounceLen * 0.5f));
        SpawnRangedEnemy(enemies, "L4_C2_IslandRanged", new Vector3(1.5f, bounceY, bounceStart + 2.8f), 205f, reward: 4.0f);

        float landStart = bounceEnd + wallGap; // 99.68
        float topY = bounceY + 0.60f;          // 4.00
        BuildWallRunSegment(parent, "S3_WallRun_Right", xPos: 3.10f, yBottom: bounceY, bounceEnd, landStart, segCount: 4, faceLeftTowardLane: true);
        CreateTimeOrbArc(orbs, "S3_OrbR", new Vector3(2.05f, bounceY + 1.45f, 0f), bounceEnd + 3.0f, landStart - 3.0f, count: 2);

        float landLen = DeckLen(6); // 15.67 -> ends at 115.35
        float landEnd = landStart + landLen;
        BuildOfficePlatform(parent, "S3_DoubleExit_Deck", new Vector3(0f, topY, landStart + landLen * 0.5f), cols: 2, rows: 6, WingTheme.Archive);

        SpawnFastTurret(parent, enemies, "L4_C2_ExitTurret_R", new Vector3( 3.0f, topY, landStart + 3.8f), 175f, reward: 4.0f, useFileCab: false);
        SpawnFastTurret(parent, enemies, "L4_C2_ExitTurret_L", new Vector3(-3.0f, topY, landStart + 7.5f), 190f, reward: 4.0f, useFileCab: false);
        SpawnRangedEnemy(enemies, "L4_C2_ExitRanged", new Vector3(0f, topY, landStart + 11.2f), 180f, reward: 4.0f);

        CreateCheckpoint(gameplay, "Checkpoint_4_SlideGantry", new Vector3(0f, topY + 0.05f, landEnd - 2.6f), bonusSeconds: 8f, wide: true);
        return (landEnd, topY);
    }

    // =========================================================================
    // S4: COMBO 3 — NARROW 5m SLIDE GANTRY -> TURRET -> DOWNHILL DASH STEP STONES
    // =========================================================================
    private static (float endZ, float topY) BuildS4_Combo3_SlideGantryDashSteps(
        Transform parent, Transform gameplay, Transform enemies, Transform orbs, float prevEndZ, float y)
    {
        // 2.80m platformer hop onto narrow 5.0m Slide Expressway Bridge (7 rows = 18.29m)
        float gantryStart = prevEndZ + 2.80f; // 118.15
        float gantryLen = DeckLen(7);         // 18.29 -> ends at 136.44
        float gantryEnd = gantryStart + gantryLen;
        BuildRunwayStrip(parent, "S4_SlideGantry_Bridge", new Vector3(0f, y, gantryStart + gantryLen * 0.5f), rows: 7);

        // Slide Archway centered at gantryStart + 9.8m (~127.95)
        float tunnelCenterZ = gantryStart + 9.8f;
        Transform tunnelRoot = new GameObject("S4_SlideArchway").transform;
        tunnelRoot.SetParent(parent, false);
        tunnelRoot.position = new Vector3(0f, y, tunnelCenterZ);

        InstantiateOfficeFbx(BookcaseBack, tunnelRoot, "Arch_SideL_F", new Vector3(-2.3f, y, tunnelCenterZ - 1.0f),  90f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BookcaseBack, tunnelRoot, "Arch_SideL_B", new Vector3(-2.3f, y, tunnelCenterZ + 1.0f),  90f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BookcaseBack, tunnelRoot, "Arch_SideR_F", new Vector3( 2.3f, y, tunnelCenterZ - 1.0f), -90f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BookcaseBack, tunnelRoot, "Arch_SideR_B", new Vector3( 2.3f, y, tunnelCenterZ + 1.0f), -90f, isStatic: true, fitCollider: false, DefaultLayer);

        for (int ix = -1; ix <= 1; ix += 2)
        {
            InstantiateOfficeFbx(ConfDesk,         tunnelRoot, $"Arch_Beam_{ix}", new Vector3(ix * 1.4f, y + 1.48f, tunnelCenterZ), 0f, isStatic: true, fitCollider: false, DefaultLayer);
            InstantiateOfficeFbx(DivBlackBlueWall, tunnelRoot, $"Arch_Top_{ix}",  new Vector3(ix * 1.4f, y + 2.36f, tunnelCenterZ), 0f, isStatic: true, fitCollider: false, DefaultLayer);
        }

        AddChildBoxCollider(tunnelRoot, "Arch_CeilingCol", DefaultLayer, new Vector3(0f, 1.48f + 2.0f, 0f), new Vector3(6.0f, 4.0f, 4.0f));
        AddChildBoxCollider(tunnelRoot, "Arch_LeftCol",    DefaultLayer, new Vector3(-2.45f, 2.5f, 0f),      new Vector3(1.2f, 5.0f, 4.0f));
        AddChildBoxCollider(tunnelRoot, "Arch_RightCol",   DefaultLayer, new Vector3( 2.45f, 2.5f, 0f),      new Vector3(1.2f, 5.0f, 4.0f));

        // Enemy turret right after slide exit!
        SpawnFastTurret(parent, enemies, "L4_C3_SlideExitTurret", new Vector3(-1.7f, y, gantryEnd - 2.3f), 165f, reward: 4.0f, useFileCab: false);

        // Downhill 11.50m Dash Leap (-1.00m drop -> Y = 3.00) + 2 TimeOrbs
        const float dashGap = 11.50f;
        float islandAStart = gantryEnd + dashGap; // 147.94
        float stepAY = y - 1.00f;                 // 3.00
        CreateTimeOrbArc(orbs, "S4_DashOrb", new Vector3(-1.0f, y + 0.85f, 0f), gantryEnd + 3.4f, islandAStart - 3.4f, count: 2);

        // Weaving Stepping Island A (Left, Y = 3.00, DeskIsland) + Ranged Enemy
        float islandALen = 4.97f;
        float islandAEnd = islandAStart + islandALen; // 152.91
        BuildDeskIsland(parent, "S4_DashIslandA_Left", new Vector3(-2.2f, stepAY, islandAStart + islandALen * 0.5f));
        SpawnRangedEnemy(enemies, "L4_C3_RangedA", new Vector3(-2.2f, stepAY, islandAStart + 2.8f), 175f, reward: 4.0f);

        // Weaving Stepping Pad B (Right, Y = 2.40, SquareDeskPad) + Turret
        float padBStart = islandAEnd + 2.89f; // 155.80
        float padBY = stepAY - 0.60f;         // 2.40
        float padBLen = 4.11f;
        float padBEnd = padBStart + padBLen;  // 159.91
        BuildSquareDeskPad(parent, "S4_StepPadB_Right", new Vector3(2.4f, padBY, padBStart + padBLen * 0.5f));
        SpawnFastTurret(parent, enemies, "L4_C3_TurretB", new Vector3(3.1f, padBY, padBStart + 2.1f), 205f, reward: 4.0f, useFileCab: false);

        // Narrow Blast-Jump Launch Strip (Center, Y = 2.40, 4 rows = 10.43m) + Ranged + CP5 + Blast Pad 1
        float blastStart = padBEnd + 2.89f; // 162.80
        float blastLen = DeckLen(4);        // 10.43 -> ends at 173.23
        float blastEnd = blastStart + blastLen;
        BuildRunwayStrip(parent, "S4_BlastLaunch_Strip", new Vector3(0f, padBY, blastStart + blastLen * 0.5f), rows: 4);
        SpawnRangedEnemy(enemies, "L4_C3_RangedB", new Vector3(0f, padBY, blastStart + 4.0f), 180f, reward: 4.0f);

        CreateCheckpoint(gameplay, "Checkpoint_5_Blast1", new Vector3(0f, padBY + 0.05f, blastEnd - 3.6f), bonusSeconds: 8f, wide: false);
        CreateBlastPadMarker(parent, "S4_BlastPad1", new Vector3(0f, padBY, blastEnd - 1.4f));
        return (blastEnd, padBY);
    }

    // =========================================================================
    // S5: COMBO 4 — BLAST-JUMP -> WEAVING DOUBLE-JUMP STAIRCASE -> HIGH LEFT WALL
    // =========================================================================
    private static (float endZ, float topY) BuildS5_Combo4_BlastStaircaseWall(
        Transform parent, Transform gameplay, Transform enemies, Transform orbs, float prevEndZ, float prevY)
    {
        float rimStart = prevEndZ + 1.80f; // 175.03
        float rimY = prevY + 3.05f;        // 5.45
        float rimLen = 4.97f;
        float rimEnd = rimStart + rimLen;  // 180.00
        BuildDeskIsland(parent, "S5_SkyRim_Island", new Vector3(0f, rimY, rimStart + rimLen * 0.5f));
        CreateTimeOrb(orbs, "S5_BlastOrb", new Vector3(0f, prevY + 2.75f, prevEndZ + 0.9f));
        SpawnFastTurret(parent, enemies, "L4_C4_RimTurret", new Vector3(1.8f, rimY, rimStart + 2.6f), 185f, reward: 4.0f, useFileCab: true);

        // Weaving Double-Jump Staircase Pads (+0.90m, +0.85m)
        float stair1Start = rimEnd + 2.80f; // 182.80
        float stair1Y = rimY + 0.90f;       // 6.35
        float stair1Len = 4.11f;
        float stair1End = stair1Start + stair1Len; // 186.91
        BuildSquareDeskPad(parent, "S5_StairPad1_Left", new Vector3(-3.4f, stair1Y, stair1Start + stair1Len * 0.5f));
        SpawnRangedEnemy(enemies, "L4_C4_StairRanged", new Vector3(-3.4f, stair1Y, stair1Start + 2.1f), 175f, reward: 4.0f);

        float stair2Start = stair1End + 2.89f; // 189.80
        float stair2Y = stair1Y + 0.85f;       // 7.20
        float stair2Len = 4.97f;
        float stair2End = stair2Start + stair2Len; // 194.77
        BuildDeskIsland(parent, "S5_StairIsland2_Center", new Vector3(0f, stair2Y, stair2Start + stair2Len * 0.5f));
        SpawnFastTurret(parent, enemies, "L4_C4_StairTurret", new Vector3(1.8f, stair2Y, stair2Start + 2.6f), 190f, reward: 4.0f, useFileCab: false);

        // High 13.0m Left Wall-Run -> Wall-Exit Deck
        const float wallGap = 13.00f;
        float exitStart = stair2End + wallGap; // 207.77
        BuildWallRunSegment(parent, "S5_WallRun_Left", xPos: -3.10f, yBottom: stair2Y, stair2End, exitStart, segCount: 5, faceLeftTowardLane: false);
        CreateTimeOrbArc(orbs, "S5_WallOrb", new Vector3(-2.05f, stair2Y + 1.45f, 0f), stair2End + 3.2f, exitStart - 3.2f, count: 3);

        float exitLen = DeckLen(6); // 15.67 -> ends at 223.44
        float exitEnd = exitStart + exitLen;
        BuildOfficePlatform(parent, "S5_WallExit_Deck", new Vector3(0f, stair2Y, exitStart + exitLen * 0.5f), cols: 2, rows: 6, WingTheme.Archive);

        SpawnFastTurret(parent, enemies, "L4_C4_WallEndTurret", new Vector3(-2.9f, stair2Y, exitStart + 3.8f), 185f, reward: 4.0f, useFileCab: false);
        SpawnRangedEnemy(enemies, "L4_C4_ExitRanged", new Vector3(2.6f, stair2Y, exitStart + 7.5f), 180f, reward: 4.0f);

        CreateCheckpoint(gameplay, "Checkpoint_6_Blast2", new Vector3(0f, stair2Y + 0.05f, exitEnd - 4.8f), bonusSeconds: 8f, wide: true);
        CreateBlastPadMarker(parent, "S5_BlastPad2", new Vector3(0f, stair2Y, exitEnd - 1.4f));
        return (exitEnd, stair2Y);
    }

    // =========================================================================
    // S6: COMBO 5 — HIGH BLAST-JUMP -> TWIN-WALL PINBALL CORRIDOR -> EXIT DUO
    // =========================================================================
    private static (float endZ, float topY) BuildS6_Combo5_BlastTwinWallExit(
        Transform parent, Transform gameplay, Transform enemies, Transform orbs, float prevEndZ, float prevY)
    {
        float cloudStart = prevEndZ + 1.80f; // 225.24
        float cloudY = prevY + 3.05f;        // 10.25
        float cloudLen = 4.97f;
        float cloudEnd = cloudStart + cloudLen; // 230.21
        BuildDeskIsland(parent, "S6_Cloud_LaunchIsland", new Vector3(0f, cloudY, cloudStart + cloudLen * 0.5f));
        SpawnFastTurret(parent, enemies, "L4_C5_CloudTurret", new Vector3(-1.8f, cloudY, cloudStart + 2.6f), 175f, reward: 4.0f, useFileCab: true);

        const float twinGap = 14.50f;
        float landStart = cloudEnd + twinGap; // 244.71
        BuildWallRunSegment(parent, "S6_PinballWall_Left",  xPos: -3.10f, yBottom: cloudY, cloudEnd, landStart, segCount: 5, faceLeftTowardLane: false);
        BuildWallRunSegment(parent, "S6_PinballWall_Right", xPos:  3.10f, yBottom: cloudY, cloudEnd, landStart, segCount: 5, faceLeftTowardLane: true);
        CreateTimeOrbArc(orbs, "S6_PinballOrb", new Vector3(0f, cloudY + 1.50f, 0f), cloudEnd + 3.4f, landStart - 3.4f, count: 3);

        float landLen = DeckLen(6); // 15.67 -> ends at 260.38
        float landEnd = landStart + landLen;
        BuildOfficePlatform(parent, "S6_PinballExit_Deck", new Vector3(0f, cloudY, landStart + landLen * 0.5f), cols: 2, rows: 6, WingTheme.Lounge);

        SpawnFastTurret(parent, enemies, "L4_C5_PinballTurret_L", new Vector3(-2.9f, cloudY, landStart + 3.8f), 175f, reward: 4.0f, useFileCab: false);
        SpawnRangedEnemy(enemies, "L4_C5_PinballRanged", new Vector3(2.5f, cloudY, landStart + 6.5f), 185f, reward: 4.0f);

        CreateCheckpoint(gameplay, "Checkpoint_7_Boardroom", new Vector3(0f, cloudY + 0.05f, landEnd - 4.4f), bonusSeconds: 8f, wide: true);
        PlaceAccentStrip(parent, "S6_FinaleJumpGuide", new Vector3(0f, cloudY, landEnd - 1.5f), 2.4f, 2, new Color(0.25f, 0.95f, 0.65f));
        return (landEnd, cloudY);
    }

    // =========================================================================
    // S7: MULTI-TIER BOARDROOM FINALE + ELEVATED SNIPER PADS + EXIT ISLAND
    // =========================================================================
    private static void BuildS7_MultiTierBoardroomFinale(
        Transform parent, Transform gameplay, Transform enemies, Transform orbs, float prevEndZ, float y)
    {
        const float entryGap = 5.50f;
        float boardStart = prevEndZ + entryGap; // 265.88
        float boardLen = DeckLen(11);           // 28.77 -> ends at 294.65
        float boardEnd = boardStart + boardLen;
        float boardCenterZ = boardStart + boardLen * 0.5f;

        CreateTimeOrbArc(orbs, "S7_EntryOrb", new Vector3(0f, y + 1.15f, 0f), prevEndZ + 1.8f, boardStart - 1.8f, count: 2);
        BuildOfficePlatform(parent, "S7_Boardroom_MainDeck", new Vector3(0f, y, boardCenterZ), cols: 3, rows: 11, WingTheme.Executive);

        // Twin Flank Wall-Run Walls along Boardroom sides (open cleanly before elevated sniper islands)
        BuildWallRunSegment(parent, "S7_FlankWall_Left",  xPos: -6.90f, yBottom: y, boardStart + 3.0f, boardStart + 15.24f, segCount: 4, faceLeftTowardLane: false);
        BuildWallRunSegment(parent, "S7_FlankWall_Right", xPos:  6.90f, yBottom: y, boardStart + 3.0f, boardStart + 15.24f, segCount: 4, faceLeftTowardLane: true);

        // Executive Cover Props
        InstantiateOfficeFbx(DivLowBlue,    parent, "S7_Cover_FL",     new Vector3(-3.8f, y, boardStart +  6.2f),  20f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(DivLowBlue,    parent, "S7_Cover_FR",     new Vector3( 3.8f, y, boardStart +  6.2f), -20f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(ConfDeskSq,    parent, "S7_CenterTable",  new Vector3( 0.0f, y, boardStart + 13.0f),   0f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(CouchLargeRed, parent, "S7_LoungeCoverL", new Vector3(-4.4f, y, boardStart + 17.5f),  80f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(CouchLargeRed, parent, "S7_LoungeCoverR", new Vector3( 4.4f, y, boardStart + 17.5f), -80f, isStatic: true, fitCollider: true, DefaultLayer);

        // 2 Elevated Floating Sniper Islands (Y = +12.25, +2.0m climb, open line of sight & easy side jump past flank walls)
        float sniperY = y + 2.00f;
        float sniperZ = boardStart + 20.5f;
        BuildSquareDeskPad(parent, "S7_SniperPad_Left",  new Vector3(-11.4f, sniperY, sniperZ));
        BuildSquareDeskPad(parent, "S7_SniperPad_Right", new Vector3( 11.4f, sniperY, sniperZ));
        CreateTimeOrb(orbs, "S7_SniperOrb_L", new Vector3(-11.4f, sniperY + 1.4f, sniperZ + 1.2f));
        CreateTimeOrb(orbs, "S7_SniperOrb_R", new Vector3( 11.4f, sniperY + 1.4f, sniperZ + 1.2f));

        // 3 Patrolling NavMesh RangedCrystals + 2 Elevated CrystalTurrets = 5 Finale Enemies
        Health r1 = SpawnRangedEnemy(enemies, "L4_Finale_Ranged1", new Vector3(-3.8f, y, boardStart +  9.5f), 170f, reward: 4.5f);
        Health r2 = SpawnRangedEnemy(enemies, "L4_Finale_Ranged2", new Vector3( 3.8f, y, boardStart +  9.5f), 190f, reward: 4.5f);
        Health r3 = SpawnRangedEnemy(enemies, "L4_Finale_Ranged3", new Vector3( 0.0f, y, boardStart + 18.0f), 180f, reward: 4.5f);
        Health tL = SpawnFastTurret(parent, enemies, "L4_Finale_TurretL", new Vector3(-11.4f, sniperY, sniperZ), 130f, reward: 5.0f, useFileCab: false);
        Health tR = SpawnFastTurret(parent, enemies, "L4_Finale_TurretR", new Vector3( 11.4f, sniperY, sniperZ), 230f, reward: 5.0f, useFileCab: false);

        // Portal Exit Barrier at front edge of Boardroom (drops once all 5 Finale enemies fall)
        float barrierZ = boardEnd - 1.4f;
        GameObject barrierRoot = new GameObject("L4_ExitBarrier");
        barrierRoot.transform.SetParent(parent, false);
        barrierRoot.transform.position = new Vector3(0f, y, barrierZ);
        barrierRoot.transform.localScale = Vector3.one;
        for (int ix = -2; ix <= 2; ix++)
        {
            InstantiateOfficeFbx(DivVeneerRed, barrierRoot.transform, $"BarL_{ix + 2}", new Vector3(ix * 3.05f, y,        barrierZ), 0f, isStatic: false, fitCollider: false, DefaultLayer);
            InstantiateOfficeFbx(DivVeneerRed, barrierRoot.transform, $"BarU_{ix + 2}", new Vector3(ix * 3.05f, y + 2.5f, barrierZ), 0f, isStatic: false, fitCollider: false, DefaultLayer);
        }
        BoxCollider barCol = barrierRoot.AddComponent<BoxCollider>();
        barCol.center = new Vector3(0f, 4.5f, 0f);
        barCol.size = new Vector3(17.5f, 9.0f, 1.0f);

        TutorialTargetGate gate = parent.gameObject.AddComponent<TutorialTargetGate>();
        SerializedObject soGate = new SerializedObject(gate);
        SerializedProperty arr = soGate.FindProperty("targets");
        arr.arraySize = 5;
        arr.GetArrayElementAtIndex(0).objectReferenceValue = r1;
        arr.GetArrayElementAtIndex(1).objectReferenceValue = r2;
        arr.GetArrayElementAtIndex(2).objectReferenceValue = r3;
        arr.GetArrayElementAtIndex(3).objectReferenceValue = tL;
        arr.GetArrayElementAtIndex(4).objectReferenceValue = tR;
        soGate.FindProperty("earlyUnlockCount").intValue = -1;
        soGate.FindProperty("barrierTransform").objectReferenceValue = barrierRoot.transform;
        soGate.FindProperty("barrierCollider").objectReferenceValue = barCol;
        soGate.FindProperty("completeStepWhenCleared").boolValue = false;
        soGate.ApplyModifiedPropertiesWithoutUndo();

        // The suspended transfer island links the Boardroom and the Mainframe.
        float portalIslandStart = boardEnd + 4.80f;
        float portalIslandY = y + 0.90f; // 11.15
        float portalCenterZ = portalIslandStart + 4.97f * 0.5f;
        BuildDeskIsland(parent, "S7_ExitPortal_Island", new Vector3(0f, portalIslandY, portalCenterZ));
        BuildMainframeArena(gameplay, portalIslandY, portalIslandStart + 4.97f);
    }

    // =========================================================================
    // PLATFORMER BUILDING BLOCKS (100% Assets/OFFICE/)
    // =========================================================================
    private static void BuildRunwayStrip(Transform parent, string name, Vector3 topCenter, int rows)
    {
        Transform stripRoot = new GameObject(name).transform;
        stripRoot.SetParent(parent, false);
        stripRoot.position = topCenter;
        stripRoot.localScale = Vector3.one;
        stripRoot.gameObject.layer = GroundLayer;

        float totalLenZ = DeckLen(rows);
        float firstRowZ = topCenter.z - totalLenZ * 0.5f + DeskD * 0.5f;

        for (int iz = 0; iz < rows; iz++)
        {
            float z = firstRowZ + iz * DeskRowStepZ;
            InstantiateOfficeFbx(iz % 2 == 0 ? ConfDesk : ConfDeskAlt, stripRoot, $"Strip_{iz}",
                new Vector3(topCenter.x, topCenter.y - DeskH, z), 0f, isStatic: true, fitCollider: false, GroundLayer);

            if (iz % 2 == 0)
            {
                InstantiateOfficeFbx(FileCabLargest, stripRoot, $"StripCabL_{iz}", new Vector3(topCenter.x - 1.45f, topCenter.y - 2.29f, z),  90f, isStatic: true, fitCollider: false, DefaultLayer);
                InstantiateOfficeFbx(FileCabLargest, stripRoot, $"StripCabR_{iz}", new Vector3(topCenter.x + 1.45f, topCenter.y - 2.29f, z), -90f, isStatic: true, fitCollider: false, DefaultLayer);
            }
        }

        BoxCollider col = stripRoot.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -DeskH * 0.5f, 0f);
        col.size = new Vector3(StripWidth + EdgeMargin * 2f, DeskH, totalLenZ + EdgeMargin * 2f);

        SetFlagsRecursive(stripRoot.gameObject,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.NavigationStatic);
    }

    private static void BuildSquareDeskPad(Transform parent, string name, Vector3 topCenter)
    {
        Transform padRoot = new GameObject(name).transform;
        padRoot.SetParent(parent, false);
        padRoot.position = topCenter;
        padRoot.localScale = Vector3.one;
        padRoot.gameObject.layer = GroundLayer;

        InstantiateOfficeFbx(ConfDeskSq,    padRoot, "PadDesk_F", new Vector3(topCenter.x, topCenter.y - DeskH, topCenter.z - 1.04f), 0f, isStatic: true, fitCollider: false, GroundLayer);
        InstantiateOfficeFbx(ConfDeskSqAlt, padRoot, "PadDesk_B", new Vector3(topCenter.x, topCenter.y - DeskH, topCenter.z + 1.04f), 0f, isStatic: true, fitCollider: false, GroundLayer);
        InstantiateOfficeFbx(DivFullHBlue,  padRoot, "PadUnder",  new Vector3(topCenter.x, topCenter.y - 2.38f, topCenter.z),       0f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BooksFbx,      padRoot, "PadBooks",  new Vector3(topCenter.x - 1.65f, topCenter.y, topCenter.z - 1.5f), 25f, isStatic: true, fitCollider: false, DefaultLayer);

        BoxCollider col = padRoot.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -DeskH * 0.5f, 0f);
        col.size = new Vector3(4.19f + EdgeMargin * 2f, DeskH, 4.11f + EdgeMargin * 2f);

        SetFlagsRecursive(padRoot.gameObject,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.NavigationStatic);
    }

    private static void BuildDeskIsland(Transform parent, string name, Vector3 topCenter)
    {
        Transform islandRoot = new GameObject(name).transform;
        islandRoot.SetParent(parent, false);
        islandRoot.position = topCenter;
        islandRoot.localScale = Vector3.one;
        islandRoot.gameObject.layer = GroundLayer;

        InstantiateOfficeFbx(ConfDesk,     islandRoot, "IslandDesk_L", new Vector3(topCenter.x - 1.32f, topCenter.y - DeskH, topCenter.z),  90f, isStatic: true, fitCollider: false, GroundLayer);
        InstantiateOfficeFbx(ConfDeskAlt,  islandRoot, "IslandDesk_R", new Vector3(topCenter.x + 1.32f, topCenter.y - DeskH, topCenter.z), -90f, isStatic: true, fitCollider: false, GroundLayer);
        InstantiateOfficeFbx(DivFullHRed,  islandRoot, "IslandUnder",  new Vector3(topCenter.x,         topCenter.y - 2.38f, topCenter.z),   0f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BooksAltFbx,  islandRoot, "IslandBooks",  new Vector3(topCenter.x + 2.15f, topCenter.y, topCenter.z - 1.7f), -20f, isStatic: true, fitCollider: false, DefaultLayer);

        BoxCollider col = islandRoot.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -DeskH * 0.5f, 0f);
        col.size = new Vector3(5.21f + EdgeMargin * 2f, DeskH, 4.97f + EdgeMargin * 2f);

        SetFlagsRecursive(islandRoot.gameObject,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.NavigationStatic);
    }

    private static void BuildOfficePlatform(Transform parent, string name, Vector3 topCenter, int cols, int rows, WingTheme theme)
    {
        Transform platformRoot = new GameObject(name).transform;
        platformRoot.SetParent(parent, false);
        platformRoot.position = topCenter;
        platformRoot.localScale = Vector3.one;
        platformRoot.gameObject.layer = GroundLayer;

        float totalLenZ = DeckLen(rows);
        float firstRowZ = topCenter.z - totalLenZ * 0.5f + DeskD * 0.5f;
        const float colStepX = 5.04f;
        float firstColX = topCenter.x - (cols - 1) * colStepX * 0.5f;
        float totalW = (cols - 1) * colStepX + StripWidth;

        for (int iz = 0; iz < rows; iz++)
        {
            float z = firstRowZ + iz * DeskRowStepZ;
            for (int ix = 0; ix < cols; ix++)
            {
                float x = firstColX + ix * colStepX;
                bool alt = ((iz + ix) % 2 == 1);
                InstantiateOfficeFbx(alt ? ConfDeskAlt : ConfDesk, platformRoot, $"Deck_{ix}_{iz}",
                    new Vector3(x, topCenter.y - DeskH, z), 0f, isStatic: true, fitCollider: false, GroundLayer);
            }

            if (iz % 2 == 0)
            {
                InstantiateOfficeFbx(DivFullHBlue, platformRoot, $"UnderFrameC_{iz}", new Vector3(topCenter.x, topCenter.y - 2.38f, z), 0f, isStatic: true, fitCollider: false, DefaultLayer);
                float sideX = totalW * 0.5f - 1.05f;
                InstantiateOfficeFbx(FileCabLargest, platformRoot, $"UnderCabL_{iz}", new Vector3(topCenter.x - sideX, topCenter.y - 2.29f, z),  90f, isStatic: true, fitCollider: false, DefaultLayer);
                InstantiateOfficeFbx(FileCabLargest, platformRoot, $"UnderCabR_{iz}", new Vector3(topCenter.x + sideX, topCenter.y - 2.29f, z), -90f, isStatic: true, fitCollider: false, DefaultLayer);
            }
        }

        if (cols == 2)
        {
            for (int iz = 1; iz < rows - 1; iz++)
            {
                if (iz % 2 != 1) continue;
                float z = firstRowZ + iz * DeskRowStepZ;

                if (theme == WingTheme.Reception)
                {
                    InstantiateOfficeFbx(WaterCooler, platformRoot, $"WingL_Cooler_{iz}", new Vector3(-4.35f, topCenter.y, z),  90f, isStatic: true, fitCollider: true, DefaultLayer);
                    InstantiateOfficeFbx(BookcaseSmall, platformRoot, $"WingR_Case_{iz}", new Vector3( 4.45f, topCenter.y, z), -90f, isStatic: true, fitCollider: true, DefaultLayer);
                }
                else if (theme == WingTheme.Lounge)
                {
                    InstantiateOfficeFbx(CouchLargeBlue, platformRoot, $"LoungeL_{iz}", new Vector3(-4.15f, topCenter.y, z),  90f, isStatic: true, fitCollider: true, DefaultLayer);
                    InstantiateOfficeFbx(ArmchairBlue,   platformRoot, $"LoungeR_{iz}", new Vector3( 4.15f, topCenter.y, z), -90f, isStatic: true, fitCollider: true, DefaultLayer);
                }
                else
                {
                    PlaceWorkstationPod(platformRoot, $"PodL_{iz}", new Vector3(-4.25f, topCenter.y, z), facingInwardRight: true);
                    InstantiateOfficeFbx(CoffeeMachine, platformRoot, $"EdgeCoffeeR_{iz}", new Vector3(4.25f, topCenter.y, z), -90f, isStatic: true, fitCollider: true, DefaultLayer);
                }
            }
        }

        BoxCollider col = platformRoot.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -DeskH * 0.5f, 0f);
        col.size = new Vector3(totalW + EdgeMargin * 2f, DeskH, totalLenZ + EdgeMargin * 2f);

        SetFlagsRecursive(platformRoot.gameObject,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.NavigationStatic);
    }

    private static void BuildWallRunSegment(
        Transform parent, string name, float xPos, float yBottom, float startZ, float endZ,
        int segCount, bool faceLeftTowardLane)
    {
        float visualLen = segCount * WallPanelLen;
        float centerZ = (startZ + endZ) * 0.5f;

        Transform wallRoot = new GameObject(name).transform;
        wallRoot.SetParent(parent, false);
        wallRoot.position = new Vector3(xPos, yBottom, centerZ);
        wallRoot.gameObject.layer = WallLayer;

        float firstSegZ = centerZ - visualLen * 0.5f + WallPanelLen * 0.5f;
        float ribOffsetX = faceLeftTowardLane ? 0.52f : -0.52f;

        for (int i = 0; i < segCount; i++)
        {
            float zPos = firstSegZ + i * WallPanelLen;
            InstantiateOfficeFbx(DivBlackBlueWall, wallRoot, $"WallLower_{i}",    new Vector3(xPos, yBottom - 0.35f, zPos), 90f, isStatic: true, fitCollider: false, WallLayer);
            InstantiateOfficeFbx(DivVeneerRed,     wallRoot, $"WallUpper_{i}",    new Vector3(xPos, yBottom + 2.15f, zPos), 90f, isStatic: true, fitCollider: false, WallLayer);
            InstantiateOfficeFbx(BookcaseBack,     wallRoot, $"WallButtress_{i}", new Vector3(xPos + ribOffsetX, yBottom - 0.35f, zPos), faceLeftTowardLane ? -90f : 90f, isStatic: true, fitCollider: false, DefaultLayer);
        }

        BoxCollider col = wallRoot.gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(0.80f, 5.6f, Mathf.Abs(endZ - startZ) + 3.0f);
        col.center = new Vector3(0f, 2.2f, 0f);
    }

    // =========================================================================
    // MIXED ENEMY SPAWNERS (RangedCrystal + CrystalTurret) & UNITY 6 NAVMESH
    // =========================================================================
    private static Health SpawnRangedEnemy(Transform enemiesParent, string name, Vector3 floorPos, float yaw, float reward)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RangedEnemyPath);
        if (prefab == null) return null;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(enemiesParent, false);
        go.transform.SetPositionAndRotation(floorPos, Quaternion.Euler(0f, yaw, 0f));
        go.transform.localScale = Vector3.one;

        Health h = go.GetComponent<Health>();
        if (h != null)
        {
            SerializedObject soH = new SerializedObject(h);
            SerializedProperty p = soH.FindProperty("maxHealth");
            if (p != null) p.floatValue = 55f;
            soH.ApplyModifiedPropertiesWithoutUndo();
        }

        CrystalRangedEnemy ranged = go.GetComponent<CrystalRangedEnemy>();
        if (ranged != null)
        {
            SerializedObject soR = new SerializedObject(ranged);
            SerializedProperty targetProp = soR.FindProperty("target");
            if (targetProp != null) targetProp.objectReferenceValue = scenePlayerHealth;
            SetFloatIfFound(soR, "detectionRange", 28f);
            SetFloatIfFound(soR, "attackRange", 24f);
            SetFloatIfFound(soR, "attackCooldown", 1.5f);
            SetFloatIfFound(soR, "timePenaltySeconds", 0.5f);
            soR.ApplyModifiedPropertiesWithoutUndo();
        }

        HealthTimeReward r = go.GetComponent<HealthTimeReward>() ?? go.AddComponent<HealthTimeReward>();
        r.secondsReward = reward;

        GameObject beacon = new GameObject("RangedAccentLight");
        beacon.transform.SetParent(go.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        Light l = beacon.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.65f, 0.35f, 1f);
        l.intensity = 2.4f;
        l.range = 6.0f;
        l.shadows = LightShadows.None;

        rangedCount++;
        return h;
    }

    private static Health SpawnFastTurret(
        Transform propParent, Transform enemiesParent, string name, Vector3 floorPos, float yaw, float reward, bool useFileCab)
    {
        float pedHeight = useFileCab ? 1.41f : 0.89f;
        InstantiateOfficeFbx(useFileCab ? FileCabLargest : Desk1, propParent, $"{name}_Pedestal", floorPos, 0f, isStatic: true, fitCollider: true, DefaultLayer);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurretEnemyPath);
        if (prefab == null) return null;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(enemiesParent, false);
        go.transform.SetPositionAndRotation(floorPos + Vector3.up * pedHeight, Quaternion.Euler(0f, yaw, 0f));
        go.transform.localScale = Vector3.one;

        Health h = go.GetComponent<Health>();
        if (h != null)
        {
            SerializedObject soH = new SerializedObject(h);
            SerializedProperty p = soH.FindProperty("maxHealth");
            if (p != null) p.floatValue = 50f;
            soH.ApplyModifiedPropertiesWithoutUndo();
        }

        CrystalTurret turret = go.GetComponent<CrystalTurret>();
        if (turret != null)
        {
            SerializedObject soT = new SerializedObject(turret);
            SerializedProperty targetProp = soT.FindProperty("target");
            if (targetProp != null) targetProp.objectReferenceValue = scenePlayerHealth;
            SetFloatIfFound(soT, "detectionRange", 30f);
            SetFloatIfFound(soT, "attackRange", 28f);
            SetFloatIfFound(soT, "attackCooldown", 1.9f);
            SetFloatIfFound(soT, "damage", 5f);
            SetFloatIfFound(soT, "knockbackStrength", 2.0f);
            soT.ApplyModifiedPropertiesWithoutUndo();
        }

        HealthTimeReward r = go.GetComponent<HealthTimeReward>() ?? go.AddComponent<HealthTimeReward>();
        r.secondsReward = reward;

        GameObject beacon = new GameObject("TurretAccentLight");
        beacon.transform.SetParent(go.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        Light l = beacon.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.34f, 0.18f);
        l.intensity = 2.6f;
        l.range = 6.0f;
        l.shadows = LightShadows.None;

        turretCount++;
        return h;
    }

    private static void BakeNavMeshSurface(Transform root)
    {
        NavMeshSurface surface = root.GetComponentInChildren<NavMeshSurface>();
        if (surface == null)
        {
            GameObject navGo = new GameObject("Level4_NavMesh");
            navGo.transform.SetParent(root, false);
            surface = navGo.AddComponent<NavMeshSurface>();
        }

        NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavAssetPath);
        surface.collectObjects = CollectObjects.All;
        surface.layerMask = 1 << GroundLayer;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();

        NavMeshData baked = surface.navMeshData;
        if (baked == null) return;
        if (existing == null)
            AssetDatabase.CreateAsset(baked, NavAssetPath);
        else
        {
            surface.RemoveData();
            EditorUtility.CopySerialized(baked, existing);
            surface.navMeshData = existing;
            surface.AddData();
            EditorUtility.SetDirty(existing);
        }
        AssetDatabase.SaveAssets();
    }

    // =========================================================================
    // PICKUPS, ORBS, PODS, CHECKPOINTS & DECOR
    // =========================================================================
    private static void PlaceFastPickup(string prefabPath, Transform parent, string name, Vector3 pos, Color glow)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 35f, 0f));
        go.transform.localScale = Vector3.one;

        SphereCollider trig = go.GetComponent<SphereCollider>();
        if (trig != null) trig.radius = 1.75f;

        GameObject lg = new GameObject("PickupLight");
        lg.transform.SetParent(go.transform, false);
        lg.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        Light l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = glow;
        l.intensity = 3.0f;
        l.range = 5.5f;
        l.shadows = LightShadows.None;
    }

    private static void CreateTimeOrbArc(Transform parent, string prefix, Vector3 xyOffset, float zStart, float zEnd, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            float z = Mathf.Lerp(zStart, zEnd, t);
            CreateTimeOrb(parent, $"{prefix}_{i}", new Vector3(xyOffset.x, xyOffset.y, z));
        }
    }

    private static void CreateTimeOrb(Transform parent, string name, Vector3 pos)
    {
        GameObject orb = new GameObject(name);
        orb.transform.SetParent(parent, false);
        orb.transform.position = pos;
        orb.transform.localScale = Vector3.one;

        SphereCollider col = orb.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.45f;

        GameObject clockVisual = InstantiateOfficeFbx(WallClockFbx, orb.transform, "GoldenClock", pos, 0f, isStatic: false, fitCollider: false, DefaultLayer);
        if (clockVisual != null)
        {
            clockVisual.transform.localPosition = Vector3.zero;
            clockVisual.transform.localScale = Vector3.one * 1.45f;
            if (goldenOrbMat != null)
            {
                foreach (MeshRenderer mr in clockVisual.GetComponentsInChildren<MeshRenderer>(true))
                {
                    mr.sharedMaterial = goldenOrbMat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        GameObject idleGo = new GameObject("OrbParticles");
        idleGo.transform.SetParent(orb.transform, false);
        ParticleSystem idlePs = idleGo.AddComponent<ParticleSystem>();
        var main = idlePs.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 0.65f;
        main.startSpeed = 0.25f;
        main.startSize = 0.45f;
        main.startColor = new Color(1f, 0.85f, 0.25f, 1f);
        main.maxParticles = 14;
        var em = idlePs.emission;
        em.rateOverTime = 16f;
        var shape = idlePs.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.22f;
        ParticleSystemRenderer psr = idleGo.GetComponent<ParticleSystemRenderer>();
        psr.shadowCastingMode = ShadowCastingMode.Off;
        if (orbParticleMat != null) psr.sharedMaterial = orbParticleMat;

        GameObject lightGo = new GameObject("OrbLight");
        lightGo.transform.SetParent(orb.transform, false);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.82f, 0.25f, 1f);
        l.intensity = 2.8f;
        l.range = 5.5f;
        l.shadows = LightShadows.None;

        TimeOrb comp = orb.AddComponent<TimeOrb>();
        SerializedObject so = new SerializedObject(comp);
        so.FindProperty("secondsBonus").floatValue = 2.5f;
        so.FindProperty("idleParticles").objectReferenceValue = idlePs;
        so.FindProperty("orbLight").objectReferenceValue = l;
        if (clockVisual != null) so.FindProperty("visualSpinTarget").objectReferenceValue = clockVisual.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        orbCount++;
    }

    private static void CreateBlastPadMarker(Transform parent, string name, Vector3 floorCenter)
    {
        Transform pad = new GameObject(name).transform;
        pad.SetParent(parent, false);
        pad.position = floorCenter;

        InstantiateOfficeFbx(DeskLamp, pad, "BlastPad_LampL", floorCenter + new Vector3(-1.3f, 0f,  0.0f),  45f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(DeskLamp, pad, "BlastPad_LampR", floorCenter + new Vector3( 1.3f, 0f,  0.0f), -45f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(DeskLamp, pad, "BlastPad_LampC", floorCenter + new Vector3( 0.0f, 0f, -0.7f),   0f, isStatic: true, fitCollider: false, DefaultLayer);

        GameObject lg = new GameObject("BlastPadLight");
        lg.transform.SetParent(pad, false);
        lg.transform.position = floorCenter + new Vector3(0f, 0.8f, 0f);
        Light l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.65f, 0.15f);
        l.intensity = 3.8f;
        l.range = 7.5f;
        l.shadows = LightShadows.None;
    }

    private static void PlaceWorkstationPod(Transform parent, string name, Vector3 pos, bool facingInwardRight)
    {
        float yaw = facingInwardRight ? 90f : -90f;
        float inwardDir = facingInwardRight ? 1f : -1f;

        InstantiateOfficeFbx(facingInwardRight ? Desk1 : Desk2, parent, $"{name}_Desk", pos, yaw, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(MonitorFbx, parent, $"{name}_Monitor", pos + new Vector3(-0.15f * inwardDir, 0.89f, 0f), yaw, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(KeyboardFbx, parent, $"{name}_KB", pos + new Vector3(0.22f * inwardDir, 0.89f, 0f), yaw, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(TowerFbx, parent, $"{name}_Tower", pos + new Vector3(-0.15f * inwardDir, 0.89f, -0.72f), yaw, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(facingInwardRight ? OfficeChairBlue : OfficeChairOrange, parent, $"{name}_Chair", pos + new Vector3(0.85f * inwardDir, 0f, 0f), -yaw, isStatic: true, fitCollider: false, DefaultLayer);
    }

    private static void BuildFloatingOfficeVoidDecor()
    {
        string[] voidPoolLarge = { ConfDesk, ConfDeskAlt, ConfDeskSq, CouchLargeBlue, BookcaseBack, Whiteboard, DivFullHBlue, DivFullHRed };
        string[] voidPoolProps = { OfficeChairBlue, OfficeChairOrange, ConfChair, MonitorFbx, FileCabLargest, WaterCooler, CoffeeMachine, PaperStackLarge, BooksFbx, BooksAltFbx };

        Transform nearRoot = new GameObject("Decor_Near").transform;
        Transform midRoot = new GameObject("Decor_Mid").transform;
        Transform farRoot = new GameObject("Decor_Far").transform;

        GameObject mgrGo = new GameObject("FloatingDecorManager");
        FloatingDecorManager mgr = mgrGo.AddComponent<FloatingDecorManager>();

        System.Random rng = new System.Random(4096);
        var entries = new List<FloatingDecorManager.DecorEntry>(90);

        SpawnDecorBand(rng, nearRoot, voidPoolLarge, count: 30, minX: 16f, maxX: 34f, minY: -8f, maxY: 22f,
            minScale: 0.9f, maxScale: 1.4f, bobAmp: 0.40f, bobFreq: 0.22f, rotMax: 2.2f,
            castShadows: false, cullFar: false, brightness: 0.82f, entries);

        SpawnDecorBand(rng, midRoot, voidPoolProps, count: 35, minX: 36f, maxX: 85f, minY: -22f, maxY: 36f,
            minScale: 1.8f, maxScale: 3.6f, bobAmp: 0.65f, bobFreq: 0.14f, rotMax: 1.3f,
            castShadows: false, cullFar: false, brightness: 0.68f, entries);

        SpawnDecorBand(rng, farRoot, voidPoolLarge, count: 25, minX: 90f, maxX: 195f, minY: -45f, maxY: 55f,
            minScale: 4.5f, maxScale: 8.5f, bobAmp: 1.10f, bobFreq: 0.08f, rotMax: 0.6f,
            castShadows: false, cullFar: true, brightness: 0.50f, entries);

        mgr.SetEntries(entries.ToArray());
        EditorUtility.SetDirty(mgr);
    }

    private static void SpawnDecorBand(
        System.Random rng, Transform parent, string[] pool, int count,
        float minX, float maxX, float minY, float maxY,
        float minScale, float maxScale,
        float bobAmp, float bobFreq, float rotMax,
        bool castShadows, bool cullFar, float brightness,
        List<FloatingDecorManager.DecorEntry> entries)
    {
        for (int i = 0; i < count; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;
            float dist = Mathf.Lerp(minX, maxX, (float)rng.NextDouble());
            float z = Mathf.Lerp(-18f, 325f, (float)rng.NextDouble());
            float y = Mathf.Lerp(minY, maxY, (float)rng.NextDouble());
            Vector3 pos = new Vector3(side * dist, y, z);

            string path = pool[rng.Next(pool.Length)];
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;

            float s = Mathf.Lerp(minScale, maxScale, (float)rng.NextDouble());
            Quaternion rot = Quaternion.Euler(
                Mathf.Lerp(-18f, 18f, (float)rng.NextDouble()),
                Mathf.Lerp(0f, 360f, (float)rng.NextDouble()),
                Mathf.Lerp(-18f, 18f, (float)rng.NextDouble())
            );

            GameObject root = new GameObject($"{parent.name}_{i:00}_{asset.name}");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, rot);
            root.transform.localScale = Vector3.one;

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            visual.name = "Model";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = asset.transform.localRotation;

            Vector3 baseScale = asset.transform.localScale;
            visual.transform.localScale = baseScale;
            Renderer[] rends = visual.GetComponentsInChildren<Renderer>(true);
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);
                float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (maxDim > 0.0001f && maxDim < 0.12f)
                    baseScale *= 100f;
            }
            visual.transform.localScale = baseScale * s;

            foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);

            ApplyMutedToonMaterials(visual, brightness, castShadows);

            entries.Add(new FloatingDecorManager.DecorEntry
            {
                target = root.transform,
                basePosition = pos,
                rotSpeed = new Vector3(
                    Mathf.Lerp(-rotMax, rotMax, (float)rng.NextDouble()),
                    Mathf.Lerp(-rotMax, rotMax, (float)rng.NextDouble()),
                    Mathf.Lerp(-rotMax, rotMax, (float)rng.NextDouble())
                ),
                bobAmp = bobAmp,
                bobFreq = bobFreq,
                phase = Mathf.Lerp(0f, Mathf.PI * 2f, (float)rng.NextDouble()),
                cullByDistance = cullFar
            });
        }
    }

    // =========================================================================
    // GAMEPLAY, LIGHTING, CHECKPOINTS & BUILD SETTINGS
    // =========================================================================
    private static void SetupGameplay(Transform root)
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab != null)
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetParent(root, false);
            player.transform.SetPositionAndRotation(new Vector3(0f, 1.05f, -1.5f), Quaternion.identity);
            player.transform.localScale = Vector3.one;
            player.tag = "Player";

            scenePlayerHealth = player.GetComponent<Health>() ?? player.AddComponent<Health>();
            if (player.GetComponent<PlayerSlow>() == null) player.AddComponent<PlayerSlow>();
            if (player.GetComponent<PlayerHitReceiver>() == null) player.AddComponent<PlayerHitReceiver>();

            RigidbodyFpsController fps = player.GetComponent<RigidbodyFpsController>();
            if (fps != null)
            {
                SerializedObject soFps = new SerializedObject(fps);
                SerializedProperty maxAir = soFps.FindProperty("maxAirJumps");
                if (maxAir != null) maxAir.intValue = 1;
                soFps.ApplyModifiedPropertiesWithoutUndo();
            }

            WeaponController wc = player.GetComponent<WeaponController>();
            if (wc != null)
            {
                SerializedObject soWc = new SerializedObject(wc);
                SerializedProperty autoRel = soWc.FindProperty("autoReload");
                if (autoRel != null) autoRel.boolValue = true;
                soWc.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameUIPrefabPath);
        if (uiPrefab != null)
        {
            GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab);
            ui.transform.SetParent(root, false);
            ui.transform.localScale = Vector3.one;

            TimeManager tm = ui.GetComponent<TimeManager>() ?? ui.AddComponent<TimeManager>();
            tm.startTime = 85f;
            tm.maxTime = 120f;
            tm.drainRate = 0.80f;
            tm.isRunning = true;
        }

        GameObject cpMgr = new GameObject("CheckpointManager");
        cpMgr.transform.SetParent(root, false);
        cpMgr.AddComponent<CheckpointManager>();

        GameObject kz = new GameObject("Level4_KillZone");
        kz.transform.SetParent(root, false);
        kz.transform.position = new Vector3(0f, -18f, 150f);
        kz.transform.localScale = Vector3.one;
        BoxCollider kzCol = kz.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(220f, 8f, 400f);
        kz.AddComponent<Level3KillZone>();
    }

    private static void SetupLighting()
    {
        Transform lightRoot = new GameObject("Lighting").transform;

        Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyBoxMatPath);
        if (skyMat != null)
            RenderSettings.skybox = skyMat;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.58f, 0.65f, 0.82f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.48f, 0.56f);
        RenderSettings.ambientGroundColor = new Color(0.28f, 0.26f, 0.32f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.08f, 0.10f, 0.15f);
        RenderSettings.fogStartDistance = 95f;
        RenderSettings.fogEndDistance = 280f;

        GameObject sunGo = new GameObject("Directional Light");
        sunGo.transform.SetParent(lightRoot, false);
        sunGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.97f, 0.90f);
        sun.intensity = 1.45f;
        sun.shadows = LightShadows.Soft;

        CreateFillLight(lightRoot, "Fill_S1_Steps",     new Vector3(0f,  8f,  15f), 2.4f, 36f);
        CreateFillLight(lightRoot, "Fill_S2_Wall1",     new Vector3(0f, 10f,  54f), 2.5f, 36f);
        CreateFillLight(lightRoot, "Fill_S3_ChainWall", new Vector3(0f, 11f,  96f), 2.5f, 38f);
        CreateFillLight(lightRoot, "Fill_S4_SlideDash", new Vector3(0f, 11f, 148f), 2.5f, 40f);
        CreateFillLight(lightRoot, "Fill_S5_BlastWall", new Vector3(0f, 14f, 198f), 2.6f, 40f);
        CreateFillLight(lightRoot, "Fill_S6_Pinball",   new Vector3(0f, 17f, 245f), 2.7f, 38f);
        CreateFillLight(lightRoot, "Fill_S7_Boardroom", new Vector3(0f, 19f, 282f), 3.1f, 48f);
    }

    private static void CreateFillLight(Transform parent, string name, Vector3 pos, float intensity, float range)
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

    private static void CreateCheckpoint(Transform parent, string name, Vector3 pos, float bonusSeconds, bool wide)
    {
        GameObject cp = new GameObject(name);
        cp.transform.SetParent(parent, false);
        cp.transform.position = pos;
        cp.transform.localScale = Vector3.one;

        BoxCollider trig = cp.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.size = new Vector3(wide ? 10.0f : 5.6f, 4.5f, 3.2f);
        trig.center = new Vector3(0f, 2.0f, 0f);

        float lampOffset = wide ? 2.8f : 1.9f;
        InstantiateOfficeFbx(DeskLamp, cp.transform, $"{name}_LampL", pos + new Vector3(-lampOffset, -0.05f, 0f),  90f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(DeskLamp, cp.transform, $"{name}_LampR", pos + new Vector3( lampOffset, -0.05f, 0f), -90f, isStatic: true, fitCollider: false, DefaultLayer);

        GameObject respawn = new GameObject("RespawnPoint");
        respawn.transform.SetParent(cp.transform, false);
        respawn.transform.localPosition = new Vector3(0f, 1.0f, 0f);

        GameObject lightGo = new GameObject("CheckpointLight");
        lightGo.transform.SetParent(cp.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.78f, 0.25f);
        l.intensity = 2.8f;
        l.range = 7.0f;
        l.shadows = LightShadows.None;

        Level3CheckpointTrigger ct = cp.AddComponent<Level3CheckpointTrigger>();
        SerializedObject so = new SerializedObject(ct);
        so.FindProperty("respawnPoint").objectReferenceValue = respawn.transform;
        so.FindProperty("indicatorLight").objectReferenceValue = l;
        so.FindProperty("checkpointTimeBonus").floatValue = bonusSeconds;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PlaceAccentStrip(Transform parent, string name, Vector3 centerPos, float lengthZ, int lampCount, Color color)
    {
        Transform stripRoot = new GameObject(name).transform;
        stripRoot.SetParent(parent, false);
        stripRoot.position = centerPos;

        float startZ = -lengthZ * 0.5f;
        float step = lampCount > 1 ? lengthZ / (lampCount - 1) : 0f;

        for (int i = 0; i < lampCount; i++)
        {
            Vector3 p = centerPos + new Vector3(0f, 0f, startZ + i * step);
            InstantiateOfficeFbx(DeskLamp, stripRoot, $"GuideLamp_{i}", p, 0f, isStatic: true, fitCollider: false, DefaultLayer);

            GameObject lg = new GameObject($"GuideLight_{i}");
            lg.transform.SetParent(stripRoot, false);
            lg.transform.position = p + new Vector3(0f, 0.7f, 0f);
            Light l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = 2.6f;
            l.range = 6.0f;
            l.shadows = LightShadows.None;
        }
    }

    private static void PlaceSimplePrefab(string path, Transform parent, string name, Vector3 pos, float yaw, bool local = false)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) return;
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        inst.name = name;
        inst.transform.SetParent(parent, false);
        if (local) { inst.transform.localPosition = pos; inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f); }
        else inst.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        inst.transform.localScale = Vector3.one;
    }

    private static GameObject InstantiateOfficeFbx(
        string assetPath, Transform parent, string name, Vector3 worldPos, float yaw,
        bool isStatic, bool fitCollider, int layer)
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

        Renderer[] rends = visual.GetComponentsInChildren<Renderer>(true);
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (maxDim > 0.0001f && maxDim < 0.12f)
                visual.transform.localScale = nativeScale * 100f;
        }

        ApplyToonMaterials(visual);
        SetLayerRecursive(root, layer);

        if (fitCollider)
            OfficePropColliderAndMaterialFixer.FitFlatWalkableBoxCollider(root, 0.16f);

        if (isStatic)
        {
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.NavigationStatic;
            SetFlagsRecursive(root, flags);
        }

        propCount++;
        return root;
    }

    private static void AddChildBoxCollider(Transform parent, string name, int layer, Vector3 localCenter, Vector3 size)
    {
        GameObject go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.center = localCenter;
        col.size = size;
    }

    private static void ApplyToonMaterials(GameObject go)
    {
        if (toonShader == null) return;
        foreach (MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] shared = mr.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
            {
                Material src = shared[i];
                string key = src != null ? src.name : "DefaultOffice";
                if (!toonMatCache.TryGetValue(key, out Material m) || m == null)
                {
                    string safe = key.Replace("/", "_").Replace(":", "_").Replace(" ", "_");
                    string path = $"{MaterialFolder}/L4Toon_{safe}.mat";
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
                    if (m.HasProperty("_SketchStrength")) m.SetFloat("_SketchStrength", 0.35f);
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

    private static void ApplyMutedToonMaterials(GameObject visual, float brightness, bool castShadows)
    {
        if (toonShader == null) return;
        foreach (MeshRenderer mr in visual.GetComponentsInChildren<MeshRenderer>(true))
        {
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = castShadows;

            Material[] shared = mr.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
            {
                Material src = shared[i];
                string srcName = src != null ? src.name : "Default";
                string key = $"{srcName}_{Mathf.RoundToInt(brightness * 100)}";
                if (!decorMatCache.TryGetValue(key, out Material m) || m == null)
                {
                    string safe = key.Replace("/", "_").Replace(":", "_").Replace(" ", "_");
                    string path = $"{DecorMaterialFolder}/L4Decor_{safe}.mat";
                    m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (m == null) { m = new Material(toonShader); AssetDatabase.CreateAsset(m, path); }
                    else m.shader = toonShader;

                    Texture tex = null;
                    if (src != null)
                    {
                        if (src.HasProperty("_BaseMap")) tex = src.GetTexture("_BaseMap");
                        if (tex == null && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex");
                    }

                    m.enableInstancing = true;
                    Color muted = new Color(brightness * 0.92f, brightness * 0.94f, brightness, 1f);
                    if (m.HasProperty("_BaseMap") && tex != null) m.SetTexture("_BaseMap", tex);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", muted);
                    if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", muted * 0.55f);
                    if (m.HasProperty("_SketchMap") && sketchTexture != null) m.SetTexture("_SketchMap", sketchTexture);
                    if (m.HasProperty("_SketchStrength")) m.SetFloat("_SketchStrength", 0.30f);
                    if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0.012f);
                    EditorUtility.SetDirty(m);
                    decorMatCache[key] = m;
                }
                shared[i] = m;
            }
            mr.sharedMaterials = shared;
        }
    }

    private static Material GetOrCreateAccentMaterial(string name, Color baseCol, Color emissionCol)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(toonShader); AssetDatabase.CreateAsset(m, path); }
        else m.shader = toonShader;

        m.enableInstancing = true;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseCol);
        if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", baseCol * 0.6f);
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emissionCol);
        if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0.014f);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void SetFloatIfFound(SerializedObject so, string propName, float val)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null) p.floatValue = val;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private static void SetFlagsRecursive(GameObject root, StaticEditorFlags flags)
    {
        GameObjectUtility.SetStaticEditorFlags(root, flags);
        foreach (Transform child in root.transform)
            SetFlagsRecursive(child.gameObject, flags);
    }

    private static void AppendToBuildSettingsWithoutReordering()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == ScenePath)
            {
                if (!scenes[i].enabled)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
                return;
            }
        }
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int idx = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, idx), path.Substring(idx + 1));
    }
}
