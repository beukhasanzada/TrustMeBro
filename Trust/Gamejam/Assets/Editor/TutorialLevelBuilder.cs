using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Complete 10-step 3D Office Platformer & Combat Tutorial Builder for Assets/Scenes/TutorialLevel.unity.
/// 100% Assets/OFFICE/ 3D Furniture Platforms, Double Jump, Easy Pistol Range, Shotgun Blast Jump,
/// Mini-Level Cut Finale with easy turrets, SkyBox2 void, slow smooth Cinemachine Intro/Outro,
/// dynamic InputAction key prompts, 0.4s fade respawn (falls + low HP protection), and Build Settings entry.
/// Run via: Tools -> Tutorial Level -> Phase 2 - Build Complete TutorialLevel
/// </summary>
public static class TutorialLevelBuilder
{
    public const string ScenePath = "Assets/Scenes/TutorialLevel.unity";
    private const string MaterialFolder = "Assets/Materials/TutorialLevel";
    private const string DecorMaterialFolder = "Assets/Materials/TutorialLevel/Decor";
    private const string SketchTexPath = "Assets/ToonSketchShaderPack/Textures/PainterlyMaps/painterly1.png";
    private const string SkyBoxMatPath = "Assets/Materials/SkyBox2.mat";

    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string InputActionsPath = "Assets/Input/PlayerControls.inputactions";
    private const string TutorialUxmlPath = "Assets/UI/TutorialLevelUI.uxml";
    private const string PanelSettingsPath = "Assets/UI/GameUIPanelSettings.asset";
    private const string StartPortalPath = "Assets/Reversed Interactive/The Portal Collection/Prefabs/PortalOne.prefab";
    private const string ExitPortalPath = "Assets/Reversed Interactive/The Portal Collection/Prefabs/PortalTwo.prefab";

    // Weapon Pickups & Stationary Tutorial Targets/Enemies
    private const string PistolPickupPath = "Assets/Baloglan/Weapons/Pickups/PistolPickup.prefab";
    private const string ShotgunPickupPath = "Assets/Baloglan/Weapons/Pickups/TripleShotgunPickup.prefab";
    private const string TurretEnemyPath = "Assets/Baloglan/Enemy/Turret/CrystalTurret.prefab";

    // Assets/OFFICE/ 3D Furniture & Architectural Props
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
    private const string PaperStackLarge = "Assets/OFFICE/stationary/paper_stack_large.fbx";

    // -------------------------------------------------------------------------
    // Kinematic & Platform Constants (derived from Player.prefab + RigidbodyFpsController)
    // -------------------------------------------------------------------------
    public const float EdgeMargin = 0.30f;
    public const float DeskW = 4.97f;
    public const float DeskH = 0.88f;
    public const float DeskD = 2.57f;
    public const float DeskRowStepZ = 2.62f;
    public const float WidePlatformWidth = 10.01f;
    public const float HopPlatformWidth = 5.21f;
    public const float HopPlatformLength = 4.97f;

    public const float JumpHopGap = 3.00f;
    public const float DoubleJumpGap = 3.50f;
    public const float DoubleJumpRise = 2.20f;      // > 1.50m single jump, < 2.85m double jump
    public const float SprintJumpGap = 8.50f;       // > 6.90+1.0m, <= 80% of 11.50m
    public const float SprintJumpDashGap = 14.50f;  // >= 115% of 11.50m, <= 80% of 19.53m
    public const float SlideTunnelLength = 4.00f;
    public const float SlideTunnelClearanceY = 1.45f;
    public const float SlideTunnelBlockerHeight = 6.00f;
    public const float WallRunGap = 18.00f;
    public const float WallRunVisualLength = 18.36f;
    public const float BlastJumpGap = 1.80f;
    public const float BlastJumpRise = 3.05f;       // > 2.85m double jump, < 3.40m blast / 4.75m blast+air

    private const int GroundLayer = 3; // Matches Player.prefab groundLayers (1 << 3)
    private const int WallLayer = 6;   // Matches Player.prefab wallrunLayers (1 << 6)
    private const int DefaultLayer = 0;

    private static float DeckLen(int rows) => rows * DeskRowStepZ - 0.05f;

    private static readonly Dictionary<string, Material> toonMatCache = new Dictionary<string, Material>();
    private static readonly Dictionary<string, Material> decorMatCache = new Dictionary<string, Material>();
    private static Shader toonShader;
    private static Texture2D sketchTexture;
    private static int placedPropCount;

    private static GameObject exitPortalObj;
    private static RigidbodyFpsController scenePlayerMotor;
    private static FpsCameraRig sceneCameraRig;
    private static WeaponController sceneWeapons;
    private static Health scenePlayerHealth;
    private static Transform sceneSpawnPoint;
    private static UIDocument sceneTutorialUiDoc;
    private static TutorialStepController sceneStepController;
    private static TutorialKillZone sceneKillZone;

    private struct BarrierRef
    {
        public Transform Transform;
        public Collider Collider;
    }

    // Combat references wired cleanly once TutorialStepController exists
    private static Health[] pistolTargets = System.Array.Empty<Health>();
    private static BarrierRef s8Barrier;
    private static Health[] finaleTargets = System.Array.Empty<Health>();
    private static BarrierRef s10EarlyBarrier;
    private static BarrierRef s10PortalBarrier;

    private enum WingTheme { Reception, Lounge, Executive, Archive }

    [InitializeOnLoadMethod]
    private static void AutoBuildIfMissing()
    {
        bool needsBuild = !File.Exists(ScenePath);
        if (!needsBuild)
        {
            string text = File.ReadAllText(ScenePath);
            needsBuild = !text.Contains("TutorialStepController") || !text.Contains("S10_MiniLevel_ExitPlatform");
        }

        if (!needsBuild)
            return;

        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                BuildTutorialLevel();
        };
    }

    [MenuItem("Tools/Tutorial Level/Phase 2 - Build Complete TutorialLevel")]
    [MenuItem("Tools/Build TutorialLevel")]
    public static void BuildTutorialLevel()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialFolder);
        EnsureFolder(DecorMaterialFolder);
        EnsureFolder("Assets/Scenes");

        toonMatCache.Clear();
        decorMatCache.Clear();
        placedPropCount = 0;
        exitPortalObj = null;
        scenePlayerMotor = null;
        sceneCameraRig = null;
        sceneWeapons = null;
        scenePlayerHealth = null;
        sceneSpawnPoint = null;
        sceneTutorialUiDoc = null;
        sceneStepController = null;
        sceneKillZone = null;
        pistolTargets = System.Array.Empty<Health>();
        finaleTargets = System.Array.Empty<Health>();
        s8Barrier = default;
        s10EarlyBarrier = default;
        s10PortalBarrier = default;

        toonShader = Shader.Find("Custom/URPToonSketch") ?? Shader.Find("Universal Render Pipeline/Lit");
        sketchTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SketchTexPath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupLighting();

        Transform gameplayRoot = new GameObject("Gameplay").transform;
        Transform sec1 = new GameObject("Section1_Walk").transform;
        Transform sec2 = new GameObject("Section2_Jump").transform;
        Transform sec3 = new GameObject("Section3_DoubleJump").transform;
        Transform sec4 = new GameObject("Section4_SprintJump").transform;
        Transform sec5 = new GameObject("Section5_SprintJumpDash").transform;
        Transform sec6 = new GameObject("Section6_Slide").transform;
        Transform sec7 = new GameObject("Section7_WallRun").transform;
        Transform sec8 = new GameObject("Section8_PistolRange").transform;
        Transform sec9 = new GameObject("Section9_ShotgunBlastJump").transform;
        Transform sec10 = new GameObject("Section10_MiniLevelFinale").transform;
        Transform tutorialFlowRoot = new GameObject("TutorialFlow").transform;

        SetupPlayerAndSafety(gameplayRoot);

        float z = BuildSection1_Walk(sec1, gameplayRoot);
        z = BuildSection2_Jump(sec2, gameplayRoot, z);
        float y = 0f;
        (z, y) = BuildSection3_DoubleJump(sec3, gameplayRoot, z, y);
        z = BuildSection4_SprintJump(sec4, gameplayRoot, z, y);
        float dashLandStart = z + SprintJumpDashGap;
        z = BuildSection5_DashAndSlide(sec5, sec6, gameplayRoot, z, y);
        z = BuildSection7_WallRun(sec7, gameplayRoot, z, y);
        z = BuildSection8_PistolRange(sec8, gameplayRoot, z, y);
        (z, y) = BuildSection9_ShotgunBlastJump(sec9, gameplayRoot, z, y);
        BuildSection10_MiniLevelFinale(sec10, gameplayRoot, z, y);

        BuildTutorialUIAndCutscenes(tutorialFlowRoot);
        WireTargetGates(tutorialFlowRoot);
        BuildFloatingOfficeVoidDecor();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AppendToBuildSettingsWithoutReordering();
        AssetDatabase.SaveAssets();

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0f, 5f, 150f), new Vector3(50f, 25f, 320f)), false);

        Debug.Log($"[TutorialLevelBuilder] Built 10-step 3D Office Platformer & Combat TutorialLevel at {ScenePath} ({placedPropCount} Office props, 2 weapon pickups, 6 turrets, Double Jump + Blast Jump + Mini-Level Finale, dashLandStart={dashLandStart:0.00}).");
    }

    // =========================================================================
    // SECTION 1: WALK (Z = -5.53 .. +18.00, Y = 0.00)
    // =========================================================================
    private static float BuildSection1_Walk(Transform parent, Transform gameplay)
    {
        float startZ = -5.53f;
        float len = DeckLen(9); // 23.53 -> ends at 18.00
        float endZ = startZ + len;
        BuildOfficePlatform(parent, "S1_OfficePlatform", new Vector3(0f, 0f, startZ + len * 0.5f), rows: 9, WingTheme.Reception);

        PlaceVisualOnlyPortal(StartPortalPath, parent, "SpawnPortal_Visual", new Vector3(0f, 1.7f, -3.8f), 0f);
        InstantiateOfficeFbx(Whiteboard, parent, "S1_FarEnd_SignboardL", new Vector3(-3.8f, 0f, 16.5f), 25f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(Whiteboard, parent, "S1_FarEnd_SignboardR", new Vector3(3.8f, 0f, 16.5f), -25f, isStatic: true, fitCollider: true, DefaultLayer);
        PlaceAccentStrip(parent, "S1_WalkGuide", new Vector3(0f, 0f, 4.5f), 11.5f, 3, new Color(0.25f, 0.88f, 1f));

        CreateCheckpoint(gameplay, "Checkpoint_1_Start", new Vector3(0f, 0.05f, -1.5f));
        CreateCheckpoint(gameplay, "Checkpoint_2_Jump", new Vector3(0f, 0.05f, 6.5f)); // 11.5m run-up before 18.00
        return endZ;
    }

    // =========================================================================
    // SECTION 2: JUMP (3 x 3.00m hops + 0.88m desk obstacle, Y = 0.00)
    // =========================================================================
    private static float BuildSection2_Jump(Transform parent, Transform gameplay, float prevEndZ)
    {
        float hop1Start = prevEndZ + JumpHopGap; // 21.00
        BuildOfficeHopIsland(parent, "S2_Hop1_OfficeIsland", new Vector3(0f, 0f, hop1Start + HopPlatformLength * 0.5f), useAlt: false);
        PlaceAccentStrip(parent, "S2_Hop1_Guide", new Vector3(0f, 0f, hop1Start + HopPlatformLength * 0.5f), 2.8f, 2, new Color(0.25f, 0.88f, 1f));

        float hop2Start = hop1Start + HopPlatformLength + JumpHopGap; // 28.97
        BuildOfficeHopIsland(parent, "S2_Hop2_OfficeIsland", new Vector3(0f, 0f, hop2Start + HopPlatformLength * 0.5f), useAlt: true);
        PlaceAccentStrip(parent, "S2_Hop2_Guide", new Vector3(0f, 0f, hop2Start + HopPlatformLength * 0.5f), 2.8f, 2, new Color(0.25f, 0.88f, 1f));

        float hop3Start = hop2Start + HopPlatformLength + JumpHopGap; // 36.94
        float len = DeckLen(9); // 23.53 -> ends at 60.47
        float endZ = hop3Start + len;
        BuildOfficePlatform(parent, "S2_ObstacleAndRunway_Platform", new Vector3(0f, 0f, hop3Start + len * 0.5f), rows: 9, WingTheme.Executive);

        // Low desk obstacle at Z = 44.00 (0.88m tall < 1.50m single jump rise)
        Transform obstacleRoot = new GameObject("S2_LowDeskObstacle").transform;
        obstacleRoot.SetParent(parent, false);
        obstacleRoot.position = new Vector3(0f, 0f, 44.0f);
        obstacleRoot.gameObject.layer = GroundLayer;
        InstantiateOfficeFbx(ConfDesk, obstacleRoot, "ObstacleDesk_L", new Vector3(-2.45f, 0f, 44.0f), 0f, isStatic: true, fitCollider: false, GroundLayer);
        InstantiateOfficeFbx(ConfDeskAlt, obstacleRoot, "ObstacleDesk_R", new Vector3(2.45f, 0f, 44.0f), 0f, isStatic: true, fitCollider: false, GroundLayer);
        InstantiateOfficeFbx(PaperStackLarge, obstacleRoot, "ObstacleProp_Papers", new Vector3(-1.4f, 0.88f, 44.0f), 15f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BooksFbx, obstacleRoot, "ObstacleProp_Books", new Vector3(1.5f, 0.88f, 44.0f), -20f, isStatic: true, fitCollider: false, DefaultLayer);
        BoxCollider obsCol = obstacleRoot.gameObject.AddComponent<BoxCollider>();
        obsCol.size = new Vector3(9.9f, 0.88f, 2.57f);
        obsCol.center = new Vector3(0f, 0.44f, 0f);

        CreateCheckpoint(gameplay, "Checkpoint_3_DoubleJump", new Vector3(0f, 0.05f, 48.0f)); // 12.47m run-up before 60.47
        PlaceAccentStrip(parent, "S2_ToDoubleJumpGuide", new Vector3(0f, 0f, 54.0f), 8.5f, 3, new Color(0.25f, 0.88f, 1f));
        return endZ;
    }

    // =========================================================================
    // SECTION 3: DOUBLE JUMP (+2.20m vertical climb, 3.50m gap)
    // =========================================================================
    private static (float endZ, float topY) BuildSection3_DoubleJump(Transform parent, Transform gameplay, float prevEndZ, float prevY)
    {
        float startZ = prevEndZ + DoubleJumpGap; // 63.97
        float topY = prevY + DoubleJumpRise;     // 2.20
        float len = DeckLen(7);                  // 18.29 -> ends at 82.26
        float endZ = startZ + len;

        BuildOfficePlatform(parent, "S3_DoubleJumpTerrace", new Vector3(0f, topY, startZ + len * 0.5f), rows: 7, WingTheme.Lounge);
        // Extra structural columns under the +2.20m raised edge so the climb reads clearly
        InstantiateOfficeFbx(BookcaseBack, parent, "S3_FrontSupportL", new Vector3(-3.0f, topY - 2.50f, startZ + 0.5f), 0f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BookcaseBack, parent, "S3_FrontSupportR", new Vector3( 3.0f, topY - 2.50f, startZ + 0.5f), 0f, isStatic: true, fitCollider: false, DefaultLayer);

        CreateCheckpoint(gameplay, "Checkpoint_4_SprintJump", new Vector3(0f, topY + 0.05f, 68.0f)); // 14.26m run-up before 82.26
        PlaceAccentStrip(parent, "S3_ToSprintGuide", new Vector3(0f, topY, 75.5f), 9.0f, 3, new Color(0.3f, 1f, 0.55f));
        return (endZ, topY);
    }

    // =========================================================================
    // SECTION 4: SPRINT + JUMP (8.50m gap, Y = 2.20)
    // =========================================================================
    private static float BuildSection4_SprintJump(Transform parent, Transform gameplay, float prevEndZ, float y)
    {
        float startZ = prevEndZ + SprintJumpGap; // 90.76
        float len = DeckLen(7);                  // 18.29 -> ends at 109.05
        float endZ = startZ + len;

        BuildOfficePlatform(parent, "S4_SprintJumpLanding", new Vector3(0f, y, startZ + len * 0.5f), rows: 7, WingTheme.Executive);

        InstantiateOfficeFbx(DivLowBlue, parent, "S4_LaunchFrame_L", new Vector3(-4.2f, y, prevEndZ - 0.8f), 90f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(DivLowBlue, parent, "S4_LaunchFrame_R", new Vector3( 4.2f, y, prevEndZ - 0.8f), 90f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(DivLowBlue, parent, "S4_LandFrame_L", new Vector3(-4.2f, y, startZ + 0.8f), 90f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(DivLowBlue, parent, "S4_LandFrame_R", new Vector3( 4.2f, y, startZ + 0.8f), 90f, isStatic: true, fitCollider: true, DefaultLayer);

        CreateCheckpoint(gameplay, "Checkpoint_5_DashJump", new Vector3(0f, y + 0.05f, 94.5f)); // 14.55m run-up before 109.05
        PlaceAccentStrip(parent, "S4_ToDashGuide", new Vector3(0f, y, 102.0f), 9.0f, 3, new Color(1f, 0.78f, 0.2f));
        return endZ;
    }

    // =========================================================================
    // SECTIONS 5 & 6: SPRINT+JUMP+DASH (14.50m gap) + SLIDE TUNNEL (Z = 141..145)
    // =========================================================================
    private static float BuildSection5_DashAndSlide(Transform dashParent, Transform slideParent, Transform gameplay, float prevEndZ, float y)
    {
        float startZ = prevEndZ + SprintJumpDashGap; // 123.55
        float len = DeckLen(14);                     // 36.63 -> ends at 160.18
        float endZ = startZ + len;

        BuildOfficePlatform(dashParent, "S5_DashAndSlidePlatform", new Vector3(0f, y, startZ + len * 0.5f), rows: 14, WingTheme.Archive);

        InstantiateOfficeFbx(DivVeneerRed, dashParent, "S5_LaunchPillar_L", new Vector3(-4.3f, y, prevEndZ - 1.0f), 90f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(DivVeneerRed, dashParent, "S5_LaunchPillar_R", new Vector3( 4.3f, y, prevEndZ - 1.0f), 90f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(DivVeneerRed, dashParent, "S5_LandPillar_L", new Vector3(-4.3f, y, startZ + 1.0f), 90f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(DivVeneerRed, dashParent, "S5_LandPillar_R", new Vector3( 4.3f, y, startZ + 1.0f), 90f, isStatic: true, fitCollider: true, DefaultLayer);

        CreateCheckpoint(gameplay, "Checkpoint_6_Slide", new Vector3(0f, y + 0.05f, 127.5f)); // 13.50m run-up before Z=141 tunnel
        PlaceAccentStrip(dashParent, "S5_ToSlideGuide", new Vector3(0f, y, 134.5f), 8.5f, 3, new Color(1f, 0.45f, 0.25f));

        // Low 3D Office Slide Tunnel at Z = 141.0 -> 145.0 (4.0m long < 23.33m slide dist)
        float tunnelCenterZ = 143.0f;
        Transform tunnelRoot = new GameObject("S6_SlideTunnel").transform;
        tunnelRoot.SetParent(slideParent, false);
        tunnelRoot.position = new Vector3(0f, y, tunnelCenterZ);

        InstantiateOfficeFbx(BookcaseBack, tunnelRoot, "Tunnel_SideL_F", new Vector3(-3.1f, y, tunnelCenterZ - 1.0f),  90f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BookcaseBack, tunnelRoot, "Tunnel_SideL_B", new Vector3(-3.1f, y, tunnelCenterZ + 1.0f),  90f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BookcaseBack, tunnelRoot, "Tunnel_SideR_F", new Vector3( 3.1f, y, tunnelCenterZ - 1.0f), -90f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BookcaseBack, tunnelRoot, "Tunnel_SideR_B", new Vector3( 3.1f, y, tunnelCenterZ + 1.0f), -90f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(FileCabLargest, tunnelRoot, "Tunnel_CabL", new Vector3(-2.3f, y, tunnelCenterZ),  90f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(FileCabLargest, tunnelRoot, "Tunnel_CabR", new Vector3( 2.3f, y, tunnelCenterZ), -90f, isStatic: true, fitCollider: false, DefaultLayer);

        for (int ix = -1; ix <= 1; ix++)
        {
            InstantiateOfficeFbx(ConfDesk, tunnelRoot, $"Tunnel_CeilingBeam_{ix + 1}", new Vector3(ix * 3.1f, y + SlideTunnelClearanceY, tunnelCenterZ), 0f, isStatic: true, fitCollider: false, DefaultLayer);
            InstantiateOfficeFbx(DivBlackBlueWall, tunnelRoot, $"Tunnel_UpperWall1_{ix + 1}", new Vector3(ix * 3.1f, y + 2.33f, tunnelCenterZ), 0f, isStatic: true, fitCollider: false, DefaultLayer);
            InstantiateOfficeFbx(DivVeneerRed, tunnelRoot, $"Tunnel_UpperWall2_{ix + 1}", new Vector3(ix * 3.1f, y + 4.83f, tunnelCenterZ), 0f, isStatic: true, fitCollider: false, DefaultLayer);
        }

        AddChildBoxCollider(tunnelRoot, "Collider_CeilingAndOverwall", DefaultLayer,
            new Vector3(0f, SlideTunnelClearanceY + SlideTunnelBlockerHeight * 0.5f, 0f),
            new Vector3(11.0f, SlideTunnelBlockerHeight, SlideTunnelLength));
        AddChildBoxCollider(tunnelRoot, "Collider_LeftSideWall", DefaultLayer,
            new Vector3(-3.65f, SlideTunnelBlockerHeight * 0.5f, 0f),
            new Vector3(3.7f, SlideTunnelBlockerHeight, SlideTunnelLength));
        AddChildBoxCollider(tunnelRoot, "Collider_RightSideWall", DefaultLayer,
            new Vector3(3.65f, SlideTunnelBlockerHeight * 0.5f, 0f),
            new Vector3(3.7f, SlideTunnelBlockerHeight, SlideTunnelLength));

        CreateCheckpoint(gameplay, "Checkpoint_7_WallRun", new Vector3(0f, y + 0.05f, 148.0f)); // 12.18m run-up before 160.18
        PlaceAccentStrip(slideParent, "S6_ToWallRunGuide", new Vector3(1.5f, y, 154.0f), 8.5f, 3, new Color(0.85f, 0.35f, 1f));
        return endZ;
    }

    // =========================================================================
    // SECTION 7: WALL-RUN (18.00m gap, Y = 2.20)
    // =========================================================================
    private static float BuildSection7_WallRun(Transform parent, Transform gameplay, float gapStartZ, float y)
    {
        float gapEndZ = gapStartZ + WallRunGap; // 178.18
        float wallCenterZ = (gapStartZ + gapEndZ) * 0.5f;

        Transform wallRoot = new GameObject("S7_WallRun_RightWall").transform;
        wallRoot.SetParent(parent, false);
        wallRoot.position = new Vector3(3.10f, y, wallCenterZ);
        wallRoot.gameObject.layer = WallLayer;

        const int segments = 6;
        float segLen = WallRunVisualLength / segments;
        float startZ = wallCenterZ - WallRunVisualLength * 0.5f + segLen * 0.5f;

        for (int i = 0; i < segments; i++)
        {
            float zPos = startZ + i * segLen;
            InstantiateOfficeFbx(DivBlackBlueWall, wallRoot, $"WallLower_{i}", new Vector3(3.10f, y - 0.35f, zPos), 90f, isStatic: true, fitCollider: false, WallLayer);
            InstantiateOfficeFbx(DivVeneerRed, wallRoot, $"WallUpper_{i}", new Vector3(3.10f, y + 2.15f, zPos), 90f, isStatic: true, fitCollider: false, WallLayer);
            InstantiateOfficeFbx(BookcaseBack, wallRoot, $"WallButtress_{i}", new Vector3(3.62f, y - 0.35f, zPos), -90f, isStatic: true, fitCollider: false, DefaultLayer);
        }

        BoxCollider wallCol = wallRoot.gameObject.AddComponent<BoxCollider>();
        wallCol.size = new Vector3(0.75f, 5.2f, WallRunGap + 2.4f);
        wallCol.center = new Vector3(0f, 2.1f, 0f);

        return gapEndZ; // S8 landing starts right at 178.18
    }

    // =========================================================================
    // SECTION 8: EASY PISTOL SHOOTING RANGE (Z = 178.18 .. 209.57, Y = 2.20)
    // =========================================================================
    private static float BuildSection8_PistolRange(Transform parent, Transform gameplay, float startZ, float y)
    {
        float len = DeckLen(12); // 31.39 -> ends at 209.57
        float endZ = startZ + len;
        BuildOfficePlatform(parent, "S8_PistolRangePlatform", new Vector3(0f, y, startZ + len * 0.5f), rows: 12, WingTheme.Executive);

        CreateCheckpoint(gameplay, "Checkpoint_8_PistolRange", new Vector3(0f, y + 0.05f, startZ + 3.5f)); // 181.68

        // Center Pistol Pickup atop an illuminated reception Desk1 pedestal
        float pickupZ = startZ + 7.2f; // 185.38
        InstantiateOfficeFbx(Desk1, parent, "S8_PistolPedestal", new Vector3(0f, y, pickupZ), 0f, isStatic: true, fitCollider: true, DefaultLayer);
        PlaceWeaponPickup(PistolPickupPath, parent, "S8_PistolPickup", new Vector3(0f, y + 1.06f, pickupZ), new Color(0.25f, 0.90f, 1f));

        // 3 Stationary Easy Crystal Targets (50 HP, zero attack damage/range so purely target practice)
        Health tL = SpawnTutorialTurret(parent, "S8_PistolTarget_Left",   new Vector3(-3.0f, y + 0.89f, startZ + 20.5f), 170f, maxHp: 50f, isHostile: false);
        Health tC = SpawnTutorialTurret(parent, "S8_PistolTarget_Center", new Vector3( 0.0f, y + 1.41f, startZ + 24.5f), 180f, maxHp: 50f, isHostile: false);
        Health tR = SpawnTutorialTurret(parent, "S8_PistolTarget_Right",  new Vector3( 3.0f, y + 0.89f, startZ + 20.5f), 190f, maxHp: 50f, isHostile: false);
        pistolTargets = new[] { tL, tC, tR };

        // Shooting pedestals underneath each stationary target
        InstantiateOfficeFbx(Desk1, parent, "S8_Pedestal_L", new Vector3(-3.0f, y, startZ + 20.5f), 0f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(FileCabLargest, parent, "S8_Pedestal_C", new Vector3(0.0f, y, startZ + 24.5f), 0f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(Desk2, parent, "S8_Pedestal_R", new Vector3( 3.0f, y, startZ + 20.5f), 0f, isStatic: true, fitCollider: true, DefaultLayer);

        // Full-width locking barrier at Z = 207.0 (drops once all 3 pistol targets shatter)
        s8Barrier = CreateLockingBarrier(parent, "S8_RangeBarrier", new Vector3(0f, y, endZ - 2.4f), isRed: true);
        return endZ;
    }

    // =========================================================================
    // SECTION 9: SHOTGUN BLAST JUMP (Y = 2.20 -> +5.25, +3.05m rise, 1.80m gap)
    // =========================================================================
    private static (float endZ, float topY) BuildSection9_ShotgunBlastJump(Transform parent, Transform gameplay, float prevEndZ, float prevY)
    {
        // Easy 2.0m hop from S8 onto Shotgun Launch Platform
        float launchStart = prevEndZ + 2.00f; // 211.57
        float launchLen = DeckLen(5);         // 13.05 -> ends at 224.62
        float launchEnd = launchStart + launchLen;
        BuildOfficePlatform(parent, "S9_ShotgunLaunchPlatform", new Vector3(0f, prevY, launchStart + launchLen * 0.5f), rows: 5, WingTheme.Archive);

        CreateCheckpoint(gameplay, "Checkpoint_9_ShotgunBlast", new Vector3(0f, prevY + 0.05f, launchStart + 2.5f)); // 214.07

        // Shotgun Pickup pedestal
        float sgZ = launchStart + 5.8f; // 217.37
        InstantiateOfficeFbx(Desk1, parent, "S9_ShotgunPedestal", new Vector3(0f, prevY, sgZ), 0f, isStatic: true, fitCollider: true, DefaultLayer);
        PlaceWeaponPickup(ShotgunPickupPath, parent, "S9_ShotgunPickup", new Vector3(0f, prevY + 1.06f, sgZ), new Color(1f, 0.72f, 0.22f));

        // Glowing Blast-Pad floor marker near the front edge (where to look down & fire!)
        CreateBlastPadMarker(parent, "S9_BlastPadMarker", new Vector3(0f, prevY, launchEnd - 1.6f));

        // High Landing Platform: +3.05m higher, 1.80m horizontal gap
        float highStart = launchEnd + BlastJumpGap; // 226.42
        float highY = prevY + BlastJumpRise;        // 5.25
        float highLen = DeckLen(6);                 // 15.67 -> ends at 242.09
        float highEnd = highStart + highLen;

        BuildOfficePlatform(parent, "S9_BlastJumpHighPlatform", new Vector3(0f, highY, highStart + highLen * 0.5f), rows: 6, WingTheme.Lounge);
        InstantiateOfficeFbx(BookcaseBack, parent, "S9_WallAccentL", new Vector3(-3.0f, highY - 2.50f, highStart + 0.5f), 0f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BookcaseBack, parent, "S9_WallAccentR", new Vector3( 3.0f, highY - 2.50f, highStart + 0.5f), 0f, isStatic: true, fitCollider: false, DefaultLayer);

        CreateCheckpoint(gameplay, "Checkpoint_10_MiniLevel", new Vector3(0f, highY + 0.05f, highStart + 4.8f)); // 231.22
        PlaceAccentStrip(parent, "S9_ToFinaleGuide", new Vector3(0f, highY, highEnd - 4.5f), 6.0f, 3, new Color(1f, 0.55f, 0.2f));
        return (highEnd, highY);
    }

    // =========================================================================
    // SECTION 10: MINI-LEVEL CUT FINALE (Sprint-Jump -> Combat -> Wall-Run -> Combat -> Blast-Jump -> Portal)
    // =========================================================================
    private static void BuildSection10_MiniLevelFinale(Transform parent, Transform gameplay, float startFromZ, float midY)
    {
        // 10A: 6.0m Sprint-Jump onto Combat Island A (Z = 248.09 .. 263.76)
        float islandAStart = startFromZ + 6.00f;
        float islandALen = DeckLen(6); // 15.67
        float islandAEnd = islandAStart + islandALen;
        BuildOfficePlatform(parent, "S10_CombatIslandA", new Vector3(0f, midY, islandAStart + islandALen * 0.5f), rows: 6, WingTheme.Executive);

        // Tactical Office cover before turrets
        InstantiateOfficeFbx(DivLowBlue, parent, "S10A_CoverL", new Vector3(-2.6f, midY, islandAStart + 4.2f),  15f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(DivLowBlue, parent, "S10A_CoverR", new Vector3( 2.6f, midY, islandAStart + 4.2f), -15f, isStatic: true, fitCollider: true, DefaultLayer);

        Health fA1 = SpawnTutorialTurret(parent, "S10_Turret_A1", new Vector3(-3.1f, midY + 0.89f, islandAStart + 10.5f), 165f, maxHp: 50f, isHostile: true);
        Health fA2 = SpawnTutorialTurret(parent, "S10_Turret_A2", new Vector3( 3.1f, midY + 0.89f, islandAStart + 10.5f), 195f, maxHp: 50f, isHostile: true);
        InstantiateOfficeFbx(Desk1, parent, "S10A_TurretPed_L", new Vector3(-3.1f, midY, islandAStart + 10.5f), 0f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(Desk2, parent, "S10A_TurretPed_R", new Vector3( 3.1f, midY, islandAStart + 10.5f), 0f, isStatic: true, fitCollider: true, DefaultLayer);

        // Early barrier drops when A1 & A2 (first 2 finale turrets) are defeated!
        s10EarlyBarrier = CreateLockingBarrier(parent, "S10_Barrier_IslandA", new Vector3(0f, midY, islandAEnd - 1.5f), isRed: true);

        // 10B: 12.0m Wall-Run Gap (263.76 .. 275.76) with 12.24m right Layer-6 Office wall
        const float miniWallGap = 12.00f;
        float islandBStart = islandAEnd + miniWallGap;
        float wallCenterZ = (islandAEnd + islandBStart) * 0.5f;

        Transform wallRoot = new GameObject("S10_WallRun_RightWall").transform;
        wallRoot.SetParent(parent, false);
        wallRoot.position = new Vector3(3.10f, midY, wallCenterZ);
        wallRoot.gameObject.layer = WallLayer;

        const int wallSegs = 4;
        float visualLen = wallSegs * 3.06f; // 12.24m
        float wStartZ = wallCenterZ - visualLen * 0.5f + 1.53f;
        for (int i = 0; i < wallSegs; i++)
        {
            float wz = wStartZ + i * 3.06f;
            InstantiateOfficeFbx(DivBlackBlueWall, wallRoot, $"FinaleWallLower_{i}", new Vector3(3.10f, midY - 0.35f, wz), 90f, isStatic: true, fitCollider: false, WallLayer);
            InstantiateOfficeFbx(DivVeneerRed, wallRoot, $"FinaleWallUpper_{i}", new Vector3(3.10f, midY + 2.15f, wz), 90f, isStatic: true, fitCollider: false, WallLayer);
            InstantiateOfficeFbx(BookcaseBack, wallRoot, $"FinaleWallRib_{i}", new Vector3(3.62f, midY - 0.35f, wz), -90f, isStatic: true, fitCollider: false, DefaultLayer);
        }
        BoxCollider fWallCol = wallRoot.gameObject.AddComponent<BoxCollider>();
        fWallCol.size = new Vector3(0.75f, 5.2f, miniWallGap + 2.2f);
        fWallCol.center = new Vector3(0f, 2.1f, 0f);

        // Combat Island B (Z = 275.76 .. 291.43, Y = 5.25)
        float islandBLen = DeckLen(6); // 15.67
        float islandBEnd = islandBStart + islandBLen;
        BuildOfficePlatform(parent, "S10_CombatIslandB", new Vector3(0f, midY, islandBStart + islandBLen * 0.5f), rows: 6, WingTheme.Archive);
        CreateCheckpoint(gameplay, "Checkpoint_11_FinaleIslandB", new Vector3(0f, midY + 0.05f, islandBStart + 2.6f));

        InstantiateOfficeFbx(DivLowBlue, parent, "S10B_CoverC", new Vector3(0f, midY, islandBStart + 6.2f), 0f, isStatic: true, fitCollider: true, DefaultLayer);
        InstantiateOfficeFbx(FileCabLargest, parent, "S10B_TurretPed_C", new Vector3(0f, midY, islandBStart + 9.4f), 0f, isStatic: true, fitCollider: true, DefaultLayer);
        Health fB1 = SpawnTutorialTurret(parent, "S10_Turret_B1", new Vector3(0f, midY + 1.41f, islandBStart + 9.4f), 180f, maxHp: 50f, isHostile: true);

        finaleTargets = new[] { fA1, fA2, fB1 };

        // Shotgun Blast Pad at end of Island B to launch +3.05m up onto the Final Exit Platform
        CreateBlastPadMarker(parent, "S10_BlastPadMarker", new Vector3(0f, midY, islandBEnd - 1.6f));

        // 10C: Final Elevated Exit Platform (Y = 8.30, Z = 293.23 .. 314.14, 8 rows)
        float exitStartZ = islandBEnd + BlastJumpGap;
        float exitY = midY + BlastJumpRise; // 8.30
        float exitLen = DeckLen(8);         // 20.91 -> ends at 314.14
        BuildOfficePlatform(parent, "S10_MiniLevel_ExitPlatform", new Vector3(0f, exitY, exitStartZ + exitLen * 0.5f), rows: 8, WingTheme.Executive);
        CreateCheckpoint(gameplay, "Checkpoint_12_FinaleExit", new Vector3(0f, exitY + 0.05f, exitStartZ + 2.8f));

        s10PortalBarrier = CreateLockingBarrier(parent, "S10_Barrier_Portal", new Vector3(0f, exitY, exitStartZ + 11.5f), isRed: false);
        PlaceAccentStrip(parent, "S10_ExitGuide", new Vector3(0f, exitY, exitStartZ + 14.5f), 4.5f, 2, new Color(0.25f, 0.95f, 0.65f));

        exitPortalObj = PlaceVisualOnlyPortal(ExitPortalPath, parent, "ExitPortal_Visual", new Vector3(0f, exitY + 1.70f, exitStartZ + 17.6f), 0f);
    }

    // =========================================================================
    // 3D OFFICE PLATFORM & COMBAT PROP BUILDERS
    // =========================================================================
    private static void BuildOfficePlatform(Transform parent, string name, Vector3 topCenter, int rows, WingTheme theme)
    {
        Transform platformRoot = new GameObject(name).transform;
        platformRoot.SetParent(parent, false);
        platformRoot.position = topCenter;
        platformRoot.localScale = Vector3.one;
        platformRoot.gameObject.layer = GroundLayer;

        float totalLenZ = DeckLen(rows);
        float firstRowZ = topCenter.z - totalLenZ * 0.5f + DeskD * 0.5f;

        for (int iz = 0; iz < rows; iz++)
        {
            float z = firstRowZ + iz * DeskRowStepZ;
            bool alt = (iz % 2 == 1);
            string deskL = alt ? ConfDeskAlt : ConfDesk;
            string deskR = alt ? ConfDesk : ConfDeskAlt;

            InstantiateOfficeFbx(deskL, platformRoot, $"DeckL_{iz}", new Vector3(topCenter.x - 2.52f, topCenter.y - DeskH, z), 0f, isStatic: true, fitCollider: false, GroundLayer);
            InstantiateOfficeFbx(deskR, platformRoot, $"DeckR_{iz}", new Vector3(topCenter.x + 2.52f, topCenter.y - DeskH, z), 0f, isStatic: true, fitCollider: false, GroundLayer);

            if (iz % 2 == 0)
            {
                InstantiateOfficeFbx(DivFullHBlue, platformRoot, $"UnderFrameC_{iz}", new Vector3(topCenter.x, topCenter.y - 2.38f, z), 0f, isStatic: true, fitCollider: false, DefaultLayer);
                InstantiateOfficeFbx(FileCabLargest, platformRoot, $"UnderCabL_{iz}", new Vector3(topCenter.x - 3.95f, topCenter.y - 2.29f, z), 90f, isStatic: true, fitCollider: false, DefaultLayer);
                InstantiateOfficeFbx(FileCabLargest, platformRoot, $"UnderCabR_{iz}", new Vector3(topCenter.x + 3.95f, topCenter.y - 2.29f, z), -90f, isStatic: true, fitCollider: false, DefaultLayer);
            }
        }

        PopulatePlatformWings(platformRoot, topCenter, rows, firstRowZ, theme);

        BoxCollider col = platformRoot.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -DeskH * 0.5f, 0f);
        col.size = new Vector3(WidePlatformWidth + EdgeMargin * 2f, DeskH, totalLenZ + EdgeMargin * 2f);

        SetFlagsRecursive(platformRoot.gameObject,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.NavigationStatic);
    }

    private static void BuildOfficeHopIsland(Transform parent, string name, Vector3 topCenter, bool useAlt)
    {
        Transform islandRoot = new GameObject(name).transform;
        islandRoot.SetParent(parent, false);
        islandRoot.position = topCenter;
        islandRoot.localScale = Vector3.one;
        islandRoot.gameObject.layer = GroundLayer;

        string deskAsset = useAlt ? ConfDeskAlt : ConfDesk;
        InstantiateOfficeFbx(deskAsset, islandRoot, "IslandDesk_L", new Vector3(topCenter.x - 1.32f, topCenter.y - DeskH, topCenter.z),  90f, isStatic: true, fitCollider: false, GroundLayer);
        InstantiateOfficeFbx(deskAsset, islandRoot, "IslandDesk_R", new Vector3(topCenter.x + 1.32f, topCenter.y - DeskH, topCenter.z), -90f, isStatic: true, fitCollider: false, GroundLayer);

        InstantiateOfficeFbx(DivFullHRed, islandRoot, "Island_UnderCubicle", new Vector3(topCenter.x, topCenter.y - 2.38f, topCenter.z), 0f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(FileCabLargest, islandRoot, "Island_CabFL", new Vector3(topCenter.x - 1.75f, topCenter.y - 2.29f, topCenter.z - 1.4f),   0f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(FileCabLargest, islandRoot, "Island_CabFR", new Vector3(topCenter.x + 1.75f, topCenter.y - 2.29f, topCenter.z + 1.4f), 180f, isStatic: true, fitCollider: false, DefaultLayer);

        InstantiateOfficeFbx(PaperStackLarge, islandRoot, "Island_Papers", new Vector3(topCenter.x - 2.15f, topCenter.y, topCenter.z - 1.8f),  22f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(BooksAltFbx, islandRoot, "Island_Books",      new Vector3(topCenter.x + 2.15f, topCenter.y, topCenter.z + 1.8f), -35f, isStatic: true, fitCollider: false, DefaultLayer);

        BoxCollider col = islandRoot.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -DeskH * 0.5f, 0f);
        col.size = new Vector3(HopPlatformWidth + EdgeMargin * 2f, DeskH, HopPlatformLength + EdgeMargin * 2f);

        SetFlagsRecursive(islandRoot.gameObject,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.NavigationStatic);
    }

    private static void PopulatePlatformWings(Transform platformRoot, Vector3 topCenter, int rows, float firstRowZ, WingTheme theme)
    {
        for (int iz = 1; iz < rows - 1; iz++)
        {
            if (iz % 2 != 1) continue;
            float z = firstRowZ + iz * DeskRowStepZ;

            // Keep Slide Tunnel, Pistol Targets (196..207), and Wall-Run transitions clean
            if (z > 138.0f && z < 147.0f) continue;
            if (z > 195.0f && z < 208.5f) continue;

            bool placeRight = !(z > 154.0f && z < 181.5f) && !(z > 259.0f && z < 277.0f);

            switch (theme)
            {
                case WingTheme.Reception:
                    if (iz == 1)
                    {
                        InstantiateOfficeFbx(WaterCooler, platformRoot, $"WingL_Cooler_{iz}", new Vector3(-4.35f, topCenter.y, z),  90f, isStatic: true, fitCollider: true, DefaultLayer);
                        InstantiateOfficeFbx(BookcaseSmall, platformRoot, $"WingR_Bookcase_{iz}", new Vector3(4.45f, topCenter.y, z), -90f, isStatic: true, fitCollider: true, DefaultLayer);
                    }
                    else
                    {
                        PlaceWorkstationPod(platformRoot, $"PodL_{iz}", new Vector3(-4.25f, topCenter.y, z), facingInwardRight: true);
                        PlaceWorkstationPod(platformRoot, $"PodR_{iz}", new Vector3( 4.25f, topCenter.y, z), facingInwardRight: false);
                    }
                    break;

                case WingTheme.Lounge:
                    InstantiateOfficeFbx(CouchLargeBlue, platformRoot, $"LoungeL_{iz}", new Vector3(-4.15f, topCenter.y, z), 90f, isStatic: true, fitCollider: true, DefaultLayer);
                    if (placeRight)
                        InstantiateOfficeFbx(ArmchairBlue, platformRoot, $"LoungeR_{iz}", new Vector3(4.10f, topCenter.y, z), -90f, isStatic: true, fitCollider: true, DefaultLayer);
                    break;

                case WingTheme.Archive:
                    InstantiateOfficeFbx(FileCabLarge, platformRoot, $"ArchL_{iz}", new Vector3(-4.35f, topCenter.y, z), 90f, isStatic: true, fitCollider: true, DefaultLayer);
                    if (placeRight)
                        PlaceWorkstationPod(platformRoot, $"ArchPodR_{iz}", new Vector3(4.25f, topCenter.y, z), facingInwardRight: false);
                    break;

                default:
                    PlaceWorkstationPod(platformRoot, $"ExecL_{iz}", new Vector3(-4.25f, topCenter.y, z), facingInwardRight: true);
                    if (placeRight)
                    {
                        if (iz == 3)
                            InstantiateOfficeFbx(CoffeeMachine, platformRoot, $"Exec_Coffee_{iz}", new Vector3(4.25f, topCenter.y, z), -90f, isStatic: true, fitCollider: true, DefaultLayer);
                        else
                            InstantiateOfficeFbx(CouchLargeRed, platformRoot, $"ExecCouchR_{iz}", new Vector3(4.15f, topCenter.y, z), -90f, isStatic: true, fitCollider: true, DefaultLayer);
                    }
                    break;
            }
        }
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

    private static void PlaceWeaponPickup(string prefabPath, Transform parent, string name, Vector3 pos, Color glowColor)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 35f, 0f));
        go.transform.localScale = Vector3.one;

        // Generous trigger radius so walking near or across the desk pedestal cleanly picks up the weapon
        SphereCollider trig = go.GetComponent<SphereCollider>();
        if (trig != null) trig.radius = 1.65f;

        GameObject lightGo = new GameObject("PickupBeaconLight");
        lightGo.transform.SetParent(go.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = glowColor;
        l.intensity = 3.2f;
        l.range = 6.5f;
        l.shadows = LightShadows.None;
    }

    private static Health SpawnTutorialTurret(Transform parent, string name, Vector3 pos, float yaw, float maxHp, bool isHostile)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurretEnemyPath);
        if (prefab == null) return null;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        go.transform.localScale = Vector3.one;

        Health h = go.GetComponent<Health>();
        if (h != null)
        {
            SerializedObject soH = new SerializedObject(h);
            SetFloatIfFound(soH, "maxHealth", maxHp);
            soH.ApplyModifiedPropertiesWithoutUndo();
        }

        CrystalTurret turret = go.GetComponent<CrystalTurret>();
        if (turret != null)
        {
            SerializedObject soT = new SerializedObject(turret);
            SerializedProperty targetProp = soT.FindProperty("target");
            if (targetProp != null) targetProp.objectReferenceValue = scenePlayerHealth;

            if (isHostile)
            {
                SetFloatIfFound(soT, "detectionRange", 28f);
                SetFloatIfFound(soT, "attackRange", 26f);
                SetFloatIfFound(soT, "attackCooldown", 2.2f);
                SetFloatIfFound(soT, "damage", 5f);
                SetFloatIfFound(soT, "knockbackStrength", 2f);
            }
            else
            {
                // Stationary harmless shooting range target for Step 8
                SetFloatIfFound(soT, "detectionRange", 0.1f);
                SetFloatIfFound(soT, "attackRange", 0.1f);
                SetFloatIfFound(soT, "damage", 0f);
                SetFloatIfFound(soT, "knockbackStrength", 0f);
            }
            soT.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject lightGo = new GameObject("TargetHighlightLight");
        lightGo.transform.SetParent(go.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = isHostile ? new Color(1f, 0.35f, 0.2f) : new Color(0.25f, 0.9f, 1f);
        l.intensity = 2.6f;
        l.range = 6.0f;
        l.shadows = LightShadows.None;
        return h;
    }

    private static BarrierRef CreateLockingBarrier(Transform parent, string name, Vector3 bottomCenter, bool isRed)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = bottomCenter;
        root.transform.localScale = Vector3.one;

        string panel = isRed ? DivVeneerRed : DivBlackBlueWall;
        for (int ix = -1; ix <= 1; ix++)
        {
            float px = ix * 3.1f;
            InstantiateOfficeFbx(panel, root.transform, $"BarLower_{ix + 1}", new Vector3(px, bottomCenter.y,        bottomCenter.z), 0f, isStatic: false, fitCollider: false, DefaultLayer);
            InstantiateOfficeFbx(panel, root.transform, $"BarUpper_{ix + 1}", new Vector3(px, bottomCenter.y + 2.5f, bottomCenter.z), 0f, isStatic: false, fitCollider: false, DefaultLayer);
        }

        BoxCollider col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 4.5f, 0f);
        col.size = new Vector3(14.5f, 9.0f, 1.0f); // Wide & tall so player must clear targets before advancing

        return new BarrierRef { Transform = root.transform, Collider = col };
    }

    private static void CreateBlastPadMarker(Transform parent, string name, Vector3 floorCenter)
    {
        Transform pad = new GameObject(name).transform;
        pad.SetParent(parent, false);
        pad.position = floorCenter;

        InstantiateOfficeFbx(DeskLamp, pad, "BlastPad_LampL", floorCenter + new Vector3(-1.4f, 0f, 0f),  45f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(DeskLamp, pad, "BlastPad_LampR", floorCenter + new Vector3( 1.4f, 0f, 0f), -45f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(DeskLamp, pad, "BlastPad_LampC", floorCenter + new Vector3( 0.0f, 0f, -0.7f), 0f, isStatic: true, fitCollider: false, DefaultLayer);

        GameObject lightGo = new GameObject("BlastPadLight");
        lightGo.transform.SetParent(pad, false);
        lightGo.transform.position = floorCenter + new Vector3(0f, 0.8f, 0f);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.65f, 0.15f);
        l.intensity = 3.8f;
        l.range = 7.5f;
        l.shadows = LightShadows.None;
    }

    // =========================================================================
    // FLOATING OFFICE VOID DECOR (Assets/OFFICE/ platforms & props)
    // =========================================================================
    private static void BuildFloatingOfficeVoidDecor()
    {
        string[] voidDeskPool = { ConfDesk, ConfDeskAlt, ConfDeskSq, ConfDeskSqAlt, CouchLargeBlue, BookcaseBack, Whiteboard, DivFullHBlue };
        string[] voidPropPool = { OfficeChairBlue, OfficeChairOrange, ConfChair, MonitorFbx, FileCabLargest, WaterCooler, CoffeeMachine, PaperStackLarge };

        Transform nearRoot = new GameObject("Decor_Near").transform;
        Transform midRoot = new GameObject("Decor_Mid").transform;

        GameObject mgrGo = new GameObject("FloatingDecorManager");
        FloatingDecorManager mgr = mgrGo.AddComponent<FloatingDecorManager>();

        System.Random rng = new System.Random(1099);
        var entries = new List<FloatingDecorManager.DecorEntry>(60);

        SpawnDecorBand(rng, nearRoot, voidDeskPool, count: 30, minX: 14f, maxX: 34f, minY: -8f, maxY: 18f,
            minScale: 0.9f, maxScale: 1.35f, bobAmp: 0.35f, bobFreq: 0.16f, rotMax: 1.6f,
            castShadows: false, cullFar: false, brightness: 0.85f, entries);

        SpawnDecorBand(rng, midRoot, voidPropPool, count: 30, minX: 36f, maxX: 90f, minY: -22f, maxY: 32f,
            minScale: 1.8f, maxScale: 3.4f, bobAmp: 0.55f, bobFreq: 0.10f, rotMax: 0.9f,
            castShadows: false, cullFar: true, brightness: 0.72f, entries);

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
            float z = Mathf.Lerp(-12f, 330f, (float)rng.NextDouble());
            float y = Mathf.Lerp(minY, maxY, (float)rng.NextDouble());
            Vector3 pos = new Vector3(side * dist, y, z);

            string path = pool[rng.Next(pool.Length)];
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;

            float s = Mathf.Lerp(minScale, maxScale, (float)rng.NextDouble());
            Quaternion rot = Quaternion.Euler(
                Mathf.Lerp(-14f, 14f, (float)rng.NextDouble()),
                Mathf.Lerp(0f, 360f, (float)rng.NextDouble()),
                Mathf.Lerp(-14f, 14f, (float)rng.NextDouble())
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
    // UI, 10 STEP TRIGGERS, TARGET GATES & SLOW CUTSCENES
    // =========================================================================
    private static void BuildTutorialUIAndCutscenes(Transform root)
    {
        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

        GameObject uiGo = new GameObject("Tutorial_UI");
        uiGo.transform.SetParent(root, false);
        sceneTutorialUiDoc = uiGo.AddComponent<UIDocument>();
        sceneTutorialUiDoc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        sceneTutorialUiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TutorialUxmlPath);
        sceneTutorialUiDoc.sortingOrder = 10;

        sceneStepController = uiGo.AddComponent<TutorialStepController>();

        if (sceneKillZone != null)
        {
            SerializedObject soKz = new SerializedObject(sceneKillZone);
            soKz.FindProperty("tutorialUiDoc").objectReferenceValue = sceneTutorialUiDoc;
            soKz.ApplyModifiedPropertiesWithoutUndo();
        }

        // Sequential End-Zone Triggers (Step 8 index 7 completes via PistolTargetGate; all others via end-zones)
        CreateStepTrigger(root, "Step1_Walk_EndZone",           0, new Vector3(0f, 1.80f,  16.5f));
        CreateStepTrigger(root, "Step2_Jump_EndZone",           1, new Vector3(0f, 1.80f,  47.0f));
        CreateStepTrigger(root, "Step3_DoubleJump_EndZone",     2, new Vector3(0f, 4.00f,  66.8f));
        CreateStepTrigger(root, "Step4_SprintJump_EndZone",     3, new Vector3(0f, 4.00f,  93.5f));
        CreateStepTrigger(root, "Step5_SprintJumpDash_EndZone", 4, new Vector3(0f, 4.00f, 126.5f));
        CreateStepTrigger(root, "Step6_Slide_EndZone",          5, new Vector3(0f, 4.00f, 147.5f));
        CreateStepTrigger(root, "Step7_WallRun_EndZone",        6, new Vector3(0f, 4.00f, 181.0f));
        CreateStepTrigger(root, "Step9_BlastJump_EndZone",      8, new Vector3(0f, 7.05f, 229.2f));
        CreateStepTrigger(root, "Step10_Finale_EndZone",        9, new Vector3(0f, 10.1f, 307.2f));

        // Slow, Smooth Cinemachine Cutscene Camera & Waypoints
        GameObject cutGo = new GameObject("Tutorial_CutsceneManager");
        cutGo.transform.SetParent(root, false);

        GameObject vcamGo = new GameObject("TutorialCutsceneVCam");
        vcamGo.transform.SetParent(cutGo.transform, false);
        CinemachineCamera vcam = vcamGo.AddComponent<CinemachineCamera>();
        vcam.Priority.Value = -10;

        Transform introRoot = new GameObject("IntroPath").transform;
        introRoot.SetParent(cutGo.transform, false);
        Transform[] introWps =
        {
            CreatePoint(introRoot, "IntroWP_0_Overlook", new Vector3(-3.8f, 9.5f, 38.0f)),
            CreatePoint(introRoot, "IntroWP_1_JumpDeck", new Vector3( 1.2f, 7.2f, 24.0f)),
            CreatePoint(introRoot, "IntroWP_2_Approach", new Vector3( 2.6f, 4.6f, 11.0f)),
            CreatePoint(introRoot, "IntroWP_3_Start",    new Vector3( 1.8f, 2.5f,  2.5f)),
        };
        Transform[] introLooks =
        {
            CreatePoint(introRoot, "IntroLook_0", new Vector3(0.0f, 2.0f, 110.0f)),
            CreatePoint(introRoot, "IntroLook_1", new Vector3(0.0f, 1.2f,  32.0f)),
            CreatePoint(introRoot, "IntroLook_2", new Vector3(0.0f, 1.3f,   3.0f)),
            CreatePoint(introRoot, "IntroLook_3", new Vector3(0.0f, 1.6f,  -3.5f)),
        };

        // Slow outro drift toward Exit Portal atop the Mini-Level Finale deck (Y = 8.30)
        Transform outroRoot = new GameObject("OutroPath").transform;
        outroRoot.SetParent(cutGo.transform, false);
        Transform[] outroWps =
        {
            CreatePoint(outroRoot, "OutroWP_0", new Vector3(2.8f, 11.5f, 302.5f)),
            CreatePoint(outroRoot, "OutroWP_1", new Vector3(0.4f, 10.6f, 305.0f)),
        };

        TutorialCutsceneController cutCtrl = cutGo.AddComponent<TutorialCutsceneController>();
        SerializedObject soCut = new SerializedObject(cutCtrl);
        soCut.FindProperty("inputActions").objectReferenceValue = inputActions;
        soCut.FindProperty("playerMotor").objectReferenceValue = scenePlayerMotor;
        soCut.FindProperty("playerCameraRig").objectReferenceValue = sceneCameraRig;
        soCut.FindProperty("playerWeapons").objectReferenceValue = sceneWeapons;
        soCut.FindProperty("playerSpawnPoint").objectReferenceValue = sceneSpawnPoint;
        soCut.FindProperty("tutorialUiDoc").objectReferenceValue = sceneTutorialUiDoc;
        soCut.FindProperty("cutsceneVCam").objectReferenceValue = vcam;
        SetTransformArray(soCut.FindProperty("introWaypoints"), introWps);
        SetTransformArray(soCut.FindProperty("introLookTargets"), introLooks);
        soCut.FindProperty("introDuration").floatValue = 10.0f;
        SetTransformArray(soCut.FindProperty("outroWaypoints"), outroWps);
        soCut.FindProperty("exitPortalVisual").objectReferenceValue = exitPortalObj != null ? exitPortalObj.transform : null;
        soCut.FindProperty("outroDuration").floatValue = 6.5f;
        soCut.FindProperty("rotationDamping").floatValue = 2.2f;
        soCut.FindProperty("targetScene").stringValue = "Level1";
        soCut.FindProperty("loadingSceneName").stringValue = "Loading";
        soCut.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soStep = new SerializedObject(sceneStepController);
        soStep.FindProperty("inputActions").objectReferenceValue = inputActions;
        soStep.FindProperty("weaponController").objectReferenceValue = sceneWeapons;
        soStep.FindProperty("cutsceneController").objectReferenceValue = cutCtrl;
        soStep.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireTargetGates(Transform root)
    {
        // Step 8 Pistol Gate (completes Step 8 & drops S8_RangeBarrier when 3 targets cleared)
        CreateGateObject(root, "Gate_S8_PistolRange", stepIndex: 7, completeWhenCleared: true,
            pistolTargets, earlyCount: -1, earlyBarrier: default, mainBarrier: s8Barrier);

        // Step 10 Finale Gate (2 Island-A turrets drop S10_Barrier_IslandA; 3rd turret drops S10_Barrier_Portal)
        CreateGateObject(root, "Gate_S10_MiniLevelFinale", stepIndex: 9, completeWhenCleared: false,
            finaleTargets, earlyCount: 2, earlyBarrier: s10EarlyBarrier, mainBarrier: s10PortalBarrier);
    }

    private static void CreateGateObject(
        Transform root, string name, int stepIndex, bool completeWhenCleared,
        Health[] targets, int earlyCount, BarrierRef earlyBarrier, BarrierRef mainBarrier)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        TutorialTargetGate gate = go.AddComponent<TutorialTargetGate>();

        SerializedObject so = new SerializedObject(gate);
        SerializedProperty arr = so.FindProperty("targets");
        arr.arraySize = targets.Length;
        for (int i = 0; i < targets.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];

        so.FindProperty("earlyBarrierTransform").objectReferenceValue = earlyBarrier.Transform;
        so.FindProperty("earlyBarrierCollider").objectReferenceValue = earlyBarrier.Collider;
        so.FindProperty("earlyUnlockCount").intValue = earlyCount;
        so.FindProperty("barrierTransform").objectReferenceValue = mainBarrier.Transform;
        so.FindProperty("barrierCollider").objectReferenceValue = mainBarrier.Collider;
        so.FindProperty("stepController").objectReferenceValue = sceneStepController;
        so.FindProperty("stepIndex").intValue = stepIndex;
        so.FindProperty("completeStepWhenCleared").boolValue = completeWhenCleared;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateStepTrigger(Transform root, string name, int stepIndex, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(10.6f, 4.8f, 3.2f);

        TutorialStepTrigger trig = go.AddComponent<TutorialStepTrigger>();
        SerializedObject so = new SerializedObject(trig);
        so.FindProperty("stepIndex").intValue = stepIndex;
        so.FindProperty("controller").objectReferenceValue = sceneStepController;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform CreatePoint(Transform parent, string name, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        return go.transform;
    }

    private static void SetTransformArray(SerializedProperty prop, Transform[] items)
    {
        prop.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
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

    // =========================================================================
    // PLAYER, CHECKPOINTS, KILL ZONE & LIGHTING
    // =========================================================================
    private static void SetupPlayerAndSafety(Transform root)
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab != null)
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetParent(root, false);
            player.transform.SetPositionAndRotation(new Vector3(0f, 1.05f, -1.5f), Quaternion.identity);
            player.transform.localScale = Vector3.one;
            player.tag = "Player";

            scenePlayerMotor = player.GetComponent<RigidbodyFpsController>();
            sceneCameraRig = player.GetComponentInChildren<FpsCameraRig>(true);
            sceneWeapons = player.GetComponent<WeaponController>();
            scenePlayerHealth = player.GetComponent<Health>();

            GameObject spawnRef = new GameObject("PlayerStartPoint");
            spawnRef.transform.SetParent(root, false);
            spawnRef.transform.SetPositionAndRotation(new Vector3(0f, 1.05f, -1.5f), Quaternion.identity);
            sceneSpawnPoint = spawnRef.transform;

            // Ensure Double Jump (1 air jump) is enabled on the scene instance
            if (scenePlayerMotor != null)
            {
                SerializedObject soFps = new SerializedObject(scenePlayerMotor);
                SerializedProperty maxAirProp = soFps.FindProperty("maxAirJumps");
                if (maxAirProp != null) maxAirProp.intValue = 1;
                soFps.ApplyModifiedPropertiesWithoutUndo();
            }

            // Calmer, less twitchy camera look/bob on scene instance
            if (sceneCameraRig != null)
            {
                SerializedObject soCam = new SerializedObject(sceneCameraRig);
                SetFloatIfFound(soCam, "mouseSensitivity", 0.055f);
                SetFloatIfFound(soCam, "gamepadDegreesPerSecond", 135f);
                SetFloatIfFound(soCam, "bobHeight", 0.018f);
                SetFloatIfFound(soCam, "bobWidth", 0.012f);
                SetFloatIfFound(soCam, "lookSway", 0.70f);
                SetFloatIfFound(soCam, "strafeRoll", 1.0f);
                SetFloatIfFound(soCam, "effectSharpness", 7.5f);
                soCam.ApplyModifiedPropertiesWithoutUndo();
            }

            // Player starts unarmed and picks up Pistol at Step 8 and Shotgun at Step 9
            if (sceneWeapons != null)
            {
                SerializedObject soWc = new SerializedObject(sceneWeapons);
                SerializedProperty loadoutProp = soWc.FindProperty("loadout");
                if (loadoutProp != null) loadoutProp.arraySize = 0;
                soWc.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        GameObject cpMgr = new GameObject("CheckpointManager");
        cpMgr.transform.SetParent(root, false);
        cpMgr.AddComponent<CheckpointManager>();

        GameObject kz = new GameObject("Tutorial_KillZone");
        kz.transform.SetParent(root, false);
        kz.transform.position = new Vector3(0f, -18f, 155f);
        kz.transform.localScale = Vector3.one;
        BoxCollider kzCol = kz.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(180f, 8f, 420f);
        sceneKillZone = kz.AddComponent<TutorialKillZone>();

        if (scenePlayerHealth != null)
        {
            SerializedObject soKz = new SerializedObject(sceneKillZone);
            soKz.FindProperty("playerHealth").objectReferenceValue = scenePlayerHealth;
            soKz.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetFloatIfFound(SerializedObject so, string propName, float val)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null) p.floatValue = val;
    }

    private static void SetupLighting()
    {
        Transform lightRoot = new GameObject("Lighting").transform;

        Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyBoxMatPath);
        if (skyMat != null)
            RenderSettings.skybox = skyMat;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.54f, 0.62f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.46f, 0.55f);
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.24f, 0.30f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.08f, 0.10f, 0.15f);
        RenderSettings.fogStartDistance = 95f;
        RenderSettings.fogEndDistance = 260f;

        GameObject sunGo = new GameObject("Directional Light");
        sunGo.transform.SetParent(lightRoot, false);
        sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.957f, 0.839f);
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;

        CreateFillLight(lightRoot, "Fill_S1_Walk",       new Vector3(0f,  7f,   6f), 2.2f, 26f);
        CreateFillLight(lightRoot, "Fill_S2_Jump",       new Vector3(0f,  7f,  35f), 2.2f, 34f);
        CreateFillLight(lightRoot, "Fill_S3_DoubleJump", new Vector3(0f,  9f,  72f), 2.2f, 28f);
        CreateFillLight(lightRoot, "Fill_S4_Sprint",     new Vector3(0f,  9f, 100f), 2.2f, 28f);
        CreateFillLight(lightRoot, "Fill_S5_Dash",       new Vector3(0f,  9f, 134f), 2.2f, 28f);
        CreateFillLight(lightRoot, "Fill_S6_Slide",      new Vector3(0f,  9f, 152f), 2.2f, 24f);
        CreateFillLight(lightRoot, "Fill_S7_WallRun",    new Vector3(0f,  9f, 175f), 2.4f, 34f);
        CreateFillLight(lightRoot, "Fill_S8_Pistol",     new Vector3(0f,  9f, 198f), 2.5f, 34f);
        CreateFillLight(lightRoot, "Fill_S9_BlastJump",  new Vector3(0f, 12f, 232f), 2.4f, 34f);
        CreateFillLight(lightRoot, "Fill_S10_FinaleA",   new Vector3(0f, 12f, 256f), 2.5f, 34f);
        CreateFillLight(lightRoot, "Fill_S10_FinaleB",   new Vector3(0f, 12f, 283f), 2.5f, 34f);
        CreateFillLight(lightRoot, "Fill_S10_Exit",      new Vector3(0f, 15f, 304f), 2.6f, 32f);
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

    // =========================================================================
    // LOW-LEVEL OFFICE PROP, LAMP, CHECKPOINT & MATERIAL HELPERS
    // =========================================================================
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

        placedPropCount++;
        return root;
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

    private static void CreateCheckpoint(Transform parent, string name, Vector3 pos)
    {
        GameObject cp = new GameObject(name);
        cp.transform.SetParent(parent, false);
        cp.transform.position = pos;
        cp.transform.localScale = Vector3.one;

        BoxCollider trig = cp.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.size = new Vector3(10.0f, 4f, 2.5f);
        trig.center = new Vector3(0f, 1.8f, 0f);

        InstantiateOfficeFbx(DeskLamp, cp.transform, $"{name}_LampL", pos + new Vector3(-2.7f, -0.05f, 0f),  90f, isStatic: true, fitCollider: false, DefaultLayer);
        InstantiateOfficeFbx(DeskLamp, cp.transform, $"{name}_LampR", pos + new Vector3( 2.7f, -0.05f, 0f), -90f, isStatic: true, fitCollider: false, DefaultLayer);

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

        CheckpointTrigger ct = cp.AddComponent<CheckpointTrigger>();
        SerializedObject so = new SerializedObject(ct);
        so.FindProperty("respawnPoint").objectReferenceValue = respawn.transform;
        so.FindProperty("indicatorLight").objectReferenceValue = l;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject PlaceVisualOnlyPortal(string prefabPath, Transform parent, string name, Vector3 pos, float yaw)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (asset == null) return null;

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        inst.name = name;
        inst.transform.SetParent(parent, false);
        inst.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        inst.transform.localScale = Vector3.one;

        foreach (PortalTrigger pt in inst.GetComponentsInChildren<PortalTrigger>(true))
            Object.DestroyImmediate(pt, true);
        foreach (Collider c in inst.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
        return inst;
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
                    string path = $"{MaterialFolder}/TutoToon_{safe}.mat";
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
                    string path = $"{DecorMaterialFolder}/TutoDecor_{safe}.mat";
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

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int idx = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, idx), path.Substring(idx + 1));
    }
}
