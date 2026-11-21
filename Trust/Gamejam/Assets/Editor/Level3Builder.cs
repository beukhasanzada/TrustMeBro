using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Complete, deterministic, rerunnable builder for Assets/Scenes/Level3.unity.
/// Built on the proven Level2Builder conventions (native Blender FBX rotation/scale preserved,
/// single TimeManager on GameUI, Player tag/Health/PlayerSlow/PlayerHitReceiver, SolidColor void camera,
/// URPToonSketch _SketchMap + Trash URP/Lit materials, recursive NavigationStatic/BatchingStatic flags,
/// bright overhead arena fill lights + Linear fog, 90 drifting void decor pieces).
/// Run via: Tools -> Level 3 -> Build Complete Level 3 (or Tools -> Build Level 3)
/// </summary>
public static class Level3Builder
{
    public const string ScenePath = "Assets/Scenes/Level3.unity";
    private const string MaterialFolder = "Assets/Materials/Level3";
    private const string SketchTexPath = "Assets/ToonSketchShaderPack/Textures/PainterlyMaps/painterly1.png";
    private const string VolumeProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";

    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string GameUIPrefabPath = "Assets/Prefabs/GameUI.prefab";
    private const string BossUxmlPath = "Assets/UI/Level3BossUI.uxml";
    private const string PanelSettingsPath = "Assets/UI/GameUIPanelSettings.asset";
    private const string StartPortalPath = "Assets/Reversed Interactive/The Portal Collection/Prefabs/PortalOne.prefab";
    private const string ExitPortalPath = "Assets/Reversed Interactive/The Portal Collection/Prefabs/PortalTwo.prefab";

    private const string PistolPickupPath = "Assets/Baloglan/Weapons/Pickups/PistolPickup.prefab";
    private const string ShotgunPickupPath = "Assets/Baloglan/Weapons/Pickups/TripleShotgunPickup.prefab";
    private const string AssaultPickupPath = "Assets/Baloglan/Weapons/Pickups/AssaultRifle_Pickup.prefab";

    private const string RangedEnemyPath = "Assets/Baloglan/Enemy/Ranged/RangedCrystal.prefab";
    private const string TurretEnemyPath = "Assets/Baloglan/Enemy/Turret/CrystalTurret.prefab";
    private const string EnemyLaserPath = "Assets/Baloglan/Enemy/Ranged/CrystalLaser.prefab";
    private const string TelegraphVfxPath = "Assets/Baloglan/Plugins/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR2 Ground Hit.prefab";
    private const string VanishVfxPath = "Assets/Baloglan/Plugins/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Electric/CFXR3 Hit Electric C (Air).prefab";

    // Office props
    private const string Floor3x3 = "Assets/OFFICE/carpet_floor/floor_3x3.fbx";
    private const string FloorCornerRound = "Assets/OFFICE/carpet_floor/floor_corner_round.fbx";
    private const string ConfDesk = "Assets/OFFICE/desks/conference_desk.fbx";
    private const string ConfDeskSq = "Assets/OFFICE/desks/conference_desk_squared.fbx";
    private const string Desk1 = "Assets/OFFICE/desks/desk1.fbx";
    private const string DivBlueWall = "Assets/OFFICE/dividers/divider_black_blue.fbx";
    private const string DivRedWall = "Assets/OFFICE/dividers/divider_veneer_red.fbx";
    private const string DivLowBlue = "Assets/OFFICE/dividers/divider_black_blue_low.fbx";
    private const string DivLowRed = "Assets/OFFICE/dividers/divider_black_red_low.fbx";
    private const string BookcaseBack = "Assets/OFFICE/book_things/book_case_with_back.fbx";
    private const string FileCabLargest = "Assets/OFFICE/file_cabinets/file_cabinet_largest.fbx";
    private const string PaperStackLarge = "Assets/OFFICE/stationary/paper_stack_large.fbx";
    private const string MonitorFbx = "Assets/OFFICE/computers/computer_monitor.fbx";
    private const string WallClockFbx = "Assets/OFFICE/extras/wall_clock.fbx";
    private const string DeskLamp = "Assets/OFFICE/extras/circular_desklamp.fbx";

    // Trash & Debris FBX
    private const string TrashFbxPath = "Assets/Trash and Debris/Trash and Debris/TrashAndDebris.fbx";

    // Forgiveness constants
    private const float EdgeMargin = 0.30f;
    private const float WallLipThickness = 0.25f;

    private static readonly Dictionary<string, Material> toonMatCache = new Dictionary<string, Material>();
    private static readonly Dictionary<string, GameObject> trashCache = new Dictionary<string, GameObject>();
    private static Shader toonShader;
    private static Texture2D sketchTexture;
    private static Material orbParticleMat;
    private static Material goldenOrbMat;
    private static Material bossGlowMat;
    private static int builtPropsCount;
    private static int timeOrbCount;

    [MenuItem("Tools/Level 3/Build Complete Level 3")]
    [MenuItem("Tools/Build Level 3")]
    public static void BuildLevel3()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialFolder);
        EnsureFolder("Assets/Scenes");

        if (!AssetDatabase.IsValidFolder("Assets/Materials/Level3/Trash"))
            Level3PropCatalogBuilder.BuildCatalog();

        toonMatCache.Clear();
        trashCache.Clear();
        builtPropsCount = 0;
        timeOrbCount = 0;

        toonShader = Shader.Find("Custom/URPToonSketch") ?? Shader.Find("Universal Render Pipeline/Lit");
        sketchTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SketchTexPath);
        orbParticleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Baloglan/Weapons/Materials/MuzzleFlashAdditive.mat");
        goldenOrbMat = GetOrCreateAccentMaterial("L3_TimeOrbGold", new Color(1f, 0.84f, 0.22f), new Color(1.2f, 0.85f, 0.15f));
        bossGlowMat = GetOrCreateAccentMaterial("L3_OverseerCrimson", new Color(0.85f, 0.16f, 0.16f), new Color(1.4f, 0.18f, 0.12f));
        LoadTrashLookup();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupLighting();

        Transform gameplayRoot = new GameObject("Gameplay").transform;
        Transform sec1 = new GameObject("Section1_StartRunway").transform;
        Transform sec2 = new GameObject("Section2_ArenaA").transform;
        Transform sec3 = new GameObject("Section3_ComboCourse1").transform;
        Transform sec4 = new GameObject("Section4_VerticalShaft").transform;
        Transform sec5 = new GameObject("Section5_ArenaB").transform;
        Transform sec6 = new GameObject("Section6_ComboCourse2").transform;
        Transform sec7 = new GameObject("Section7_ArenaC_BossApproach").transform;
        Transform sec8 = new GameObject("Section8_BossArena_Overseer").transform;
        Transform orbsRoot = new GameObject("TimeOrbs").transform;
        Transform enemiesRoot = new GameObject("Enemies").transform;

        SetupGameplay(gameplayRoot);

        BuildSection1_Start(sec1, gameplayRoot);
        BuildSection2_ArenaA(sec2, gameplayRoot);
        BuildSection3_ComboCourse1(sec3, gameplayRoot, orbsRoot);
        BuildSection4_VerticalShaft(sec4, gameplayRoot, orbsRoot);
        BuildSection5_ArenaB(sec5, gameplayRoot);
        BuildSection6_ComboCourse2(sec6, gameplayRoot, orbsRoot);
        BuildSection7_ArenaCAndApproach(sec7, gameplayRoot);
        BuildSection8_BossArena(sec8, gameplayRoot, enemiesRoot);

        PlaceEnemiesAndDirectors(enemiesRoot, orbsRoot);

        // 90 drifting void decor pieces (Office + Trash & Debris) under 1 FloatingDecorManager
        Level3DecorBuilder.BuildDecorInActiveScene();

        EditorSceneManager.SaveScene(scene, ScenePath);
        try
        {
            NavMeshBuilder.BuildNavMesh();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Level3Builder] NavMesh bake warning: {ex.Message}");
        }

        UpdateBuildSettingsOrder();
        AssetDatabase.SaveAssets();

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0f, 3f, 25f), new Vector3(36f, 16f, 55f)), false);

        Debug.Log($"[Level3Builder] Complete Level 3 built & NavMesh baked: 8 sections, {builtPropsCount} props, 21 enemies + OVERSEER Boss + RetroWindowsGUI UI, {timeOrbCount} TimeOrbs, 90 void decor pieces.");
    }

    // =========================================================================
    // SECTION 1: START PORTAL & WARM-UP RUNWAY (Z = -6 to 18.5, Y = 0)
    // =========================================================================
    private static void BuildSection1_Start(Transform parent, Transform gameplay)
    {
        BuildCarpetDeck(parent, "Start_Runway_9x15", new Vector3(0f, 0f, 1.5f), 3, 5, roundCorners: true);
        PlaceSimplePrefab(StartPortalPath, parent, "StartPortal_Visual", new Vector3(0f, 1.7f, -4.2f), 0f);

        // Place all 3 weapon pickups right ahead of spawn so player picks them up immediately
        PlaceSimplePrefab(PistolPickupPath, parent, "Start_PistolPickup", new Vector3(-1.4f, 0.35f, 2.2f), 0f);
        PlaceSimplePrefab(ShotgunPickupPath, parent, "Start_ShotgunPickup", new Vector3(0.0f, 0.35f, 2.5f), 0f);
        PlaceSimplePrefab(AssaultPickupPath, parent, "Start_AssaultRiflePickup", new Vector3(1.4f, 0.35f, 2.2f), 0f);

        InstantiateOfficeFbx(Desk1, parent, "Start_DeskL", new Vector3(-3.6f, 0f, 1.0f), 90f, isStatic: true, fitCollider: true);
        InstantiateOfficeFbx(Desk1, parent, "Start_DeskR", new Vector3(3.6f, 0f, 1.0f), -90f, isStatic: true, fitCollider: true);
        PlaceTrashCluster(parent, "Start_DebrisL", new Vector3(-3.2f, 0f, 6.2f), 0);
        PlaceTrashCluster(parent, "Start_DebrisR", new Vector3(3.2f, 0f, 6.2f), 1);

        CreateCheckpoint(gameplay, "Checkpoint_1_Start", new Vector3(0f, 0.05f, 0.5f), bonusSeconds: 0f);
        PlaceDirectionBeacon(parent, "Beacon_StartToWarmup", new Vector3(0f, 0f, 8.2f), new Color(0.25f, 0.88f, 1f));

        // 3.5m gentle warm-up gap -> Warmup Slab 9 x 6m (Z = 12.5..18.5)
        BuildCarpetDeck(parent, "Warmup_Slab_9x6", new Vector3(0f, 0f, 15.5f), 3, 2);
        PlaceDirectionBeacon(parent, "Beacon_WarmupToArenaA", new Vector3(0f, 0f, 17.8f), new Color(0.25f, 0.88f, 1f));
    }

    // =========================================================================
    // SECTION 2: COMBAT ARENA A (27 x 27m, Z = 23 to 50, Y = 0)
    // =========================================================================
    private static void BuildSection2_ArenaA(Transform parent, Transform gameplay)
    {
        BuildCarpetDeck(parent, "ArenaA_Deck_27x27", new Vector3(0f, 0f, 36.5f), 9, 9, roundCorners: true);
        CreateCheckpoint(gameplay, "Checkpoint_2_ArenaA", new Vector3(0f, 0.05f, 25.0f), bonusSeconds: 8f);

        InstantiateOfficeFbx(ConfDesk, parent, "ArenaA_CenterDesk", new Vector3(0f, 0f, 36.5f), 0f, isStatic: true, fitCollider: true);
        InstantiateOfficeFbx(DivLowBlue, parent, "ArenaA_Cover_FL", new Vector3(-6.5f, 0f, 31.0f), 20f, isStatic: true, fitCollider: true);
        InstantiateOfficeFbx(DivLowBlue, parent, "ArenaA_Cover_FR", new Vector3(6.5f, 0f, 31.0f), -20f, isStatic: true, fitCollider: true);
        InstantiateOfficeFbx(DivLowRed, parent, "ArenaA_Cover_BL", new Vector3(-7.0f, 0f, 42.5f), -15f, isStatic: true, fitCollider: true);
        InstantiateOfficeFbx(DivLowRed, parent, "ArenaA_Cover_BR", new Vector3(7.0f, 0f, 42.5f), 15f, isStatic: true, fitCollider: true);

        PlaceTrashProp("Barriar", parent, "ArenaA_BarrierL", new Vector3(-9.5f, 0f, 36.5f), 85f, fitCol: true);
        PlaceTrashProp("FracturedBarriar", parent, "ArenaA_BarrierR", new Vector3(9.5f, 0f, 36.5f), -85f, fitCol: true);
        PlaceTrashCluster(parent, "ArenaA_Debris_NW", new Vector3(-10.5f, 0f, 45.0f), 2);
        PlaceTrashCluster(parent, "ArenaA_Debris_NE", new Vector3(10.5f, 0f, 45.0f), 3);

        PlaceDirectionBeacon(parent, "Beacon_ArenaA_Safe", new Vector3(-4.5f, 0f, 49.0f), new Color(0.25f, 0.88f, 1f));
        PlaceDirectionBeacon(parent, "Beacon_ArenaA_Style", new Vector3(7.5f, 0f, 49.0f), new Color(1f, 0.78f, 0.2f));
    }

    // =========================================================================
    // SECTION 3: COMBO COURSE 1 (Z = 50 to 99.5, Y = 0 -> 1.0)
    // Safe Line: 4.5-5.0m gaps | Style Line (Set Piece #1): 18m Wall-Run + Wall-Jump
    // =========================================================================
    private static void BuildSection3_ComboCourse1(Transform parent, Transform gameplay, Transform orbsRoot)
    {
        CreateCheckpoint(gameplay, "Checkpoint_3_Combo1", new Vector3(-2.0f, 0.05f, 48.5f), bonusSeconds: 8f);

        // SAFE LINE (X = -4.5, gaps 4.5m / 5.0m / 5.0m / 5.0m)
        BuildCarpetDeck(parent, "C1_Safe_Island1_9x6", new Vector3(-4.5f, 0.0f, 57.5f), 3, 2);
        PlaceTrashCluster(parent, "C1_Safe_Debris1", new Vector3(-7.5f, 0.0f, 57.5f), 0);

        BuildCarpetDeck(parent, "C1_Safe_Island2_9x9", new Vector3(-4.5f, 0.5f, 70.0f), 3, 3);
        PlaceTrashProp("Barriar", parent, "C1_Safe_Cover2", new Vector3(-7.0f, 0.5f, 70.0f), 90f, fitCol: true);

        BuildCarpetDeck(parent, "C1_Safe_Island3_9x6", new Vector3(-4.5f, 1.0f, 82.5f), 3, 2);
        PlaceTrashCluster(parent, "C1_Safe_Debris3", new Vector3(-7.5f, 1.0f, 82.5f), 1);

        BuildCarpetDeck(parent, "C1_JoinLanding_12x9", new Vector3(0f, 1.0f, 95.0f), 4, 3, roundCorners: true);

        // STYLE LINE — SET PIECE #1: 18m continuous WALL_RUN_WALL + Wall-Jump onto Join Landing
        BuildCarpetDeck(parent, "C1_Style_Entry_6x6", new Vector3(7.5f, 0f, 57.5f), 2, 2);
        PlaceDirectionBeacon(parent, "Beacon_C1_WallRun", new Vector3(9.5f, 0f, 60.0f), new Color(1f, 0.78f, 0.2f));

        BuildWallRunSegment(parent, "C1_Style_WallRun_18m", DivBlueWall,
            new Vector3(10.5f, 0.0f, 71.0f), lengthZ: 18.0f, heightY: 2.6f, facingSignX: -1f, segments: 6);

        CreateTimeOrb(orbsRoot, "Orb_C1_1", new Vector3(9.1f, 1.7f, 65.0f));
        CreateTimeOrb(orbsRoot, "Orb_C1_2", new Vector3(9.1f, 1.9f, 70.0f));
        CreateTimeOrb(orbsRoot, "Orb_C1_3", new Vector3(9.1f, 2.0f, 75.0f));
        CreateTimeOrb(orbsRoot, "Orb_C1_4", new Vector3(8.9f, 2.1f, 80.0f));
        CreateTimeOrb(orbsRoot, "Orb_C1_5", new Vector3(6.8f, 2.6f, 86.0f));
    }

    // =========================================================================
    // SECTION 4: VERTICAL SHAFT (Z = 99.5 to 148, Y = 1.0 -> 8.2)
    // Safe Line: Spiral steps (+1.2m) | Set Piece #3: Zig-Zag Shaft | Set Piece #4: Blast-Jump
    // =========================================================================
    private static void BuildSection4_VerticalShaft(Transform parent, Transform gameplay, Transform orbsRoot)
    {
        CreateCheckpoint(gameplay, "Checkpoint_4_Shaft", new Vector3(0f, 1.05f, 97.5f), bonusSeconds: 8f);
        PlaceDirectionBeacon(parent, "Beacon_Shaft_Safe", new Vector3(-4.5f, 1.0f, 98.8f), new Color(0.25f, 0.88f, 1f));
        PlaceDirectionBeacon(parent, "Beacon_Shaft_Style", new Vector3(4.5f, 1.0f, 98.8f), new Color(1f, 0.78f, 0.2f));

        // SAFE LINE: 5 wide stepped islands (+1.2m rise each, 3.0-4.0m gaps)
        BuildCarpetDeck(parent, "Shaft_Safe_Step1", new Vector3(-8.5f, 2.2f, 106.5f), 2, 2);
        BuildCarpetDeck(parent, "Shaft_Safe_Step2", new Vector3(-9.5f, 3.4f, 116.5f), 2, 2);
        BuildCarpetDeck(parent, "Shaft_Safe_Step3", new Vector3(-8.5f, 4.6f, 126.5f), 2, 2);
        BuildCarpetDeck(parent, "Shaft_Safe_Step4", new Vector3(-6.5f, 5.8f, 136.5f), 2, 2);
        BuildCarpetDeck(parent, "Shaft_Safe_Step5", new Vector3(-4.0f, 7.0f, 144.0f), 2, 2);

        PlaceTrashCluster(parent, "Shaft_Safe_Debris1", new Vector3(-10.8f, 2.2f, 106.5f), 2);
        PlaceTrashCluster(parent, "Shaft_Safe_Debris3", new Vector3(-10.8f, 4.6f, 126.5f), 3);

        // STYLE LINE — SET PIECE #3: Zig-Zag Wall-Jump Shaft (two tall facing walls 6m apart)
        BuildCarpetDeck(parent, "Shaft_Style_Entry", new Vector3(4.5f, 1.0f, 105.5f), 2, 2);
        for (int tier = 0; tier < 3; tier++)
        {
            float ty = 1.0f + tier * 2.5f;
            BuildWallRunSegment(parent, $"Shaft_WallL_T{tier}", DivRedWall,
                new Vector3(1.5f, ty, 118.0f), lengthZ: 15.0f, heightY: 2.5f, facingSignX: 1f, segments: 5);
            BuildWallRunSegment(parent, $"Shaft_WallR_T{tier}", DivRedWall,
                new Vector3(7.5f, ty, 118.0f), lengthZ: 15.0f, heightY: 2.5f, facingSignX: -1f, segments: 5);
        }
        InstantiateOfficeFbx(PaperStackLarge, parent, "Shaft_PillarL", new Vector3(1.0f, 1.0f, 126.5f), 0f, isStatic: true, fitCollider: false);
        InstantiateOfficeFbx(PaperStackLarge, parent, "Shaft_PillarR", new Vector3(8.0f, 1.0f, 126.5f), 90f, isStatic: true, fitCollider: false);

        BuildCarpetDeck(parent, "Shaft_Style_TopLanding", new Vector3(4.5f, 8.2f, 138.0f), 3, 3);
        CreateTimeOrb(orbsRoot, "Orb_Shaft_1", new Vector3(2.8f, 3.0f, 113.0f));
        CreateTimeOrb(orbsRoot, "Orb_Shaft_2", new Vector3(6.2f, 4.8f, 117.5f));
        CreateTimeOrb(orbsRoot, "Orb_Shaft_3", new Vector3(2.8f, 6.6f, 122.0f));
        CreateTimeOrb(orbsRoot, "Orb_Shaft_4", new Vector3(5.5f, 8.5f, 128.0f));

        // STYLE LINE — SET PIECE #4: Shotgun Blast-Jump Shortcut (+4.8m straight up to Arena B!)
        BuildCarpetDeck(parent, "Shaft_BlastJump_Pad", new Vector3(-1.5f, 3.4f, 142.0f), 2, 2);
        PlaceDirectionBeacon(parent, "Beacon_BlastJump", new Vector3(-1.5f, 3.4f, 144.2f), new Color(1f, 0.5f, 0.15f));
        CreateTimeOrb(orbsRoot, "Orb_Blast_1", new Vector3(-1.5f, 5.2f, 144.5f));
        CreateTimeOrb(orbsRoot, "Orb_Blast_2", new Vector3(-1.5f, 7.0f, 146.0f));
        CreateTimeOrb(orbsRoot, "Orb_Blast_3", new Vector3(-1.5f, 9.0f, 147.8f));
    }

    // =========================================================================
    // SECTION 5: COMBAT ARENA B (27 x 27m + 4 Elevated Ledges, Z = 148 to 175, Y = 8.2)
    // =========================================================================
    private static void BuildSection5_ArenaB(Transform parent, Transform gameplay)
    {
        BuildCarpetDeck(parent, "ArenaB_Deck_27x27", new Vector3(0f, 8.2f, 161.5f), 9, 9, roundCorners: true);
        CreateCheckpoint(gameplay, "Checkpoint_5_ArenaB", new Vector3(0f, 8.25f, 150.0f), bonusSeconds: 8f);

        // 4 Elevated Perimeter Ledges (+3.0m high, Y = 11.2)
        BuildCarpetDeck(parent, "ArenaB_Ledge_FL", new Vector3(-12.5f, 11.2f, 155.5f), 2, 2);
        BuildCarpetDeck(parent, "ArenaB_Ledge_FR", new Vector3(12.5f, 11.2f, 155.5f), 2, 2);
        BuildCarpetDeck(parent, "ArenaB_Ledge_BL", new Vector3(-12.5f, 11.2f, 168.5f), 2, 2);
        BuildCarpetDeck(parent, "ArenaB_Ledge_BR", new Vector3(12.5f, 11.2f, 168.5f), 2, 2);

        InstantiateOfficeFbx(ConfDeskSq, parent, "ArenaB_StepL", new Vector3(-10.5f, 8.2f, 162.0f), 90f, isStatic: true, fitCollider: true);
        InstantiateOfficeFbx(ConfDeskSq, parent, "ArenaB_StepR", new Vector3(10.5f, 8.2f, 162.0f), 90f, isStatic: true, fitCollider: true);

        InstantiateOfficeFbx(ConfDesk, parent, "ArenaB_CenterDesk", new Vector3(0f, 8.2f, 161.5f), 90f, isStatic: true, fitCollider: true);
        PlaceTrashProp("Barriar", parent, "ArenaB_Barriar_F", new Vector3(0f, 8.2f, 155.0f), 0f, fitCol: true);
        PlaceTrashProp("FracturedBarriar", parent, "ArenaB_Barriar_B", new Vector3(0f, 8.2f, 168.0f), 180f, fitCol: true);
        PlaceTrashCluster(parent, "ArenaB_Debris_L", new Vector3(-6.0f, 8.2f, 161.5f), 0);
        PlaceTrashCluster(parent, "ArenaB_Debris_R", new Vector3(6.0f, 8.2f, 161.5f), 2);

        PlaceDirectionBeacon(parent, "Beacon_ArenaB_Safe", new Vector3(-7.5f, 8.2f, 174.0f), new Color(0.25f, 0.88f, 1f));
        PlaceDirectionBeacon(parent, "Beacon_ArenaB_Style", new Vector3(5.0f, 8.2f, 174.0f), new Color(1f, 0.78f, 0.2f));
    }

    // =========================================================================
    // SECTION 6: COMBO COURSE 2 (Z = 175 to 249, Y = 8.2)
    // Set Piece #2: 13.5m Mega-Gap (+ Catch Slab #1) | Set Piece #5: 9.5m Dash Gap (+ Catch Slab #2)
    // =========================================================================
    private static void BuildSection6_ComboCourse2(Transform parent, Transform gameplay, Transform orbsRoot)
    {
        CreateCheckpoint(gameplay, "Checkpoint_6_Combo2", new Vector3(0f, 8.25f, 173.5f), bonusSeconds: 8f);

        // PART A: Safe Line (X = -8.0, 5.0m gaps)
        BuildCarpetDeck(parent, "C2A_Safe_Island1", new Vector3(-8.0f, 8.2f, 183.0f), 2, 2);
        BuildCarpetDeck(parent, "C2A_Safe_Island2", new Vector3(-8.0f, 8.2f, 194.0f), 2, 2);
        BuildCarpetDeck(parent, "C2A_Safe_Island3", new Vector3(-8.0f, 8.2f, 205.0f), 2, 2);
        PlaceTrashCluster(parent, "C2A_Safe_Debris", new Vector3(-10.0f, 8.2f, 194.0f), 1);

        // Set Piece #2: 13.5m Mega-Gap (Z = 186.0 -> 199.5)
        BuildCarpetDeck(parent, "C2A_Style_Launch", new Vector3(5.0f, 8.2f, 183.0f), 2, 2);
        PlaceDirectionBeacon(parent, "Beacon_MegaGap", new Vector3(5.0f, 8.2f, 185.5f), new Color(1f, 0.78f, 0.2f));
        BuildCarpetDeck(parent, "C2A_Style_Landing", new Vector3(5.0f, 8.2f, 203.0f), 2, 2);

        CreateTimeOrb(orbsRoot, "Orb_Mega_1", new Vector3(5.0f, 9.5f, 189.0f));
        CreateTimeOrb(orbsRoot, "Orb_Mega_2", new Vector3(5.0f, 10.1f, 192.5f));
        CreateTimeOrb(orbsRoot, "Orb_Mega_3", new Vector3(5.0f, 10.2f, 196.0f));
        CreateTimeOrb(orbsRoot, "Orb_Mega_4", new Vector3(5.0f, 9.6f, 199.2f));
        CreateTimeOrb(orbsRoot, "Orb_Mega_5", new Vector3(3.0f, 9.5f, 209.5f));

        // CATCH SLAB #1
        BuildCarpetDeck(parent, "CatchSlab1_DebrisFloor", new Vector3(4.0f, 3.8f, 193.0f), 4, 4);
        PlaceTrashCluster(parent, "CatchSlab1_DebrisA", new Vector3(7.5f, 3.8f, 191.0f), 0);
        PlaceTrashCluster(parent, "CatchSlab1_DebrisB", new Vector3(7.5f, 3.8f, 195.5f), 2);
        PlaceDirectionBeacon(parent, "Beacon_Catch1_WayUp", new Vector3(-1.5f, 3.8f, 195.0f), new Color(0.25f, 0.95f, 0.55f));
        BuildCarpetDeck(parent, "CatchSlab1_StepUp1", new Vector3(-3.0f, 4.9f, 198.5f), 2, 2);
        BuildCarpetDeck(parent, "CatchSlab1_StepUp2", new Vector3(-5.0f, 6.0f, 203.0f), 2, 2);
        BuildCarpetDeck(parent, "CatchSlab1_StepUp3", new Vector3(-5.5f, 7.1f, 208.5f), 2, 2);

        // Mid Hub
        BuildCarpetDeck(parent, "C2_MidHub_12x9", new Vector3(0f, 8.2f, 217.5f), 4, 3, roundCorners: true);
        PlaceTrashProp("Barriar", parent, "C2_MidHub_Cover", new Vector3(0f, 8.2f, 217.5f), 0f, fitCol: true);

        // PART B: Safe Line (X = -7.5, 5.0m gaps)
        BuildCarpetDeck(parent, "C2B_Safe_Island1", new Vector3(-7.5f, 8.2f, 230.0f), 2, 2);
        BuildCarpetDeck(parent, "C2B_Safe_Island2", new Vector3(-7.5f, 8.2f, 241.0f), 2, 2);

        // Set Piece #5: 9.5m Dash Gap over floating Trash wreck
        PlaceDirectionBeacon(parent, "Beacon_DashGap", new Vector3(4.5f, 8.2f, 221.2f), new Color(1f, 0.78f, 0.2f));
        PlaceTrashProp("FracturedBarriar", parent, "DashWreck_1", new Vector3(4.5f, 6.2f, 225.5f), 25f, fitCol: false);
        PlaceTrashProp("Sheet_Metal_1", parent, "DashWreck_2", new Vector3(6.2f, 6.8f, 227.5f), -35f, fitCol: false);
        PlaceTrashProp("Wooden_Plank_1", parent, "DashWreck_3", new Vector3(2.8f, 6.5f, 228.8f), 50f, fitCol: false);

        BuildCarpetDeck(parent, "C2B_Style_Island", new Vector3(4.5f, 8.2f, 236.0f), 2, 3);
        CreateTimeOrb(orbsRoot, "Orb_Dash_1", new Vector3(4.5f, 9.4f, 225.0f));
        CreateTimeOrb(orbsRoot, "Orb_Dash_2", new Vector3(4.5f, 9.5f, 228.5f));
        CreateTimeOrb(orbsRoot, "Orb_Dash_3", new Vector3(4.5f, 9.5f, 243.0f));
        CreateTimeOrb(orbsRoot, "Orb_Dash_4", new Vector3(3.0f, 9.5f, 246.5f));

        // CATCH SLAB #2
        BuildCarpetDeck(parent, "CatchSlab2_DebrisFloor", new Vector3(3.5f, 4.2f, 227.0f), 4, 4);
        PlaceTrashCluster(parent, "CatchSlab2_DebrisA", new Vector3(7.0f, 4.2f, 226.0f), 3);
        PlaceDirectionBeacon(parent, "Beacon_Catch2_WayUp", new Vector3(-2.0f, 4.2f, 229.0f), new Color(0.25f, 0.95f, 0.55f));
        BuildCarpetDeck(parent, "CatchSlab2_StepUp1", new Vector3(-3.0f, 5.5f, 233.0f), 2, 2);
        BuildCarpetDeck(parent, "CatchSlab2_StepUp2", new Vector3(-4.5f, 6.8f, 238.5f), 2, 2);
    }

    // =========================================================================
    // SECTION 7: COMBAT ARENA C (27 x 27m) + BOSS APPROACH BRIDGE (Z = 249 to 307.5)
    // =========================================================================
    private static void BuildSection7_ArenaCAndApproach(Transform parent, Transform gameplay)
    {
        BuildCarpetDeck(parent, "ArenaC_Deck_27x27", new Vector3(0f, 8.2f, 262.5f), 9, 9, roundCorners: true);
        CreateCheckpoint(gameplay, "Checkpoint_7_ArenaC", new Vector3(0f, 8.25f, 251.0f), bonusSeconds: 8f);

        InstantiateOfficeFbx(ConfDesk, parent, "ArenaC_CenterDesk", new Vector3(0f, 8.2f, 262.5f), 0f, isStatic: true, fitCollider: true);
        InstantiateOfficeFbx(BookcaseBack, parent, "ArenaC_ShelfL", new Vector3(-7.5f, 8.2f, 258.0f), 75f, isStatic: true, fitCollider: true);
        InstantiateOfficeFbx(BookcaseBack, parent, "ArenaC_ShelfR", new Vector3(7.5f, 8.2f, 258.0f), -75f, isStatic: true, fitCollider: true);
        PlaceTrashProp("Barriar", parent, "ArenaC_BarriarL", new Vector3(-6.5f, 8.2f, 268.0f), 30f, fitCol: true);
        PlaceTrashProp("FracturedBarriar", parent, "ArenaC_BarriarR", new Vector3(6.5f, 8.2f, 268.0f), -30f, fitCol: true);
        PlaceTrashCluster(parent, "ArenaC_DebrisL", new Vector3(-10.5f, 8.2f, 262.5f), 0);
        PlaceTrashCluster(parent, "ArenaC_DebrisR", new Vector3(10.5f, 8.2f, 262.5f), 1);

        PlaceDirectionBeacon(parent, "Beacon_BossApproach", new Vector3(0f, 8.2f, 275.0f), new Color(1f, 0.35f, 0.25f));
        BuildCarpetDeck(parent, "BossApproach_Slab1_9x9", new Vector3(0f, 8.2f, 285.0f), 3, 3);
        PlaceTrashCluster(parent, "Approach_DebrisL", new Vector3(-3.4f, 8.2f, 285.0f), 2);
        PlaceTrashCluster(parent, "Approach_DebrisR", new Vector3(3.4f, 8.2f, 285.0f), 3);

        BuildCarpetDeck(parent, "BossApproach_Slab2_9x9", new Vector3(0f, 8.2f, 298.5f), 3, 3);
        CreateCheckpoint(gameplay, "Checkpoint_8_BossApproach", new Vector3(0f, 8.25f, 299.0f), bonusSeconds: 8f);
    }

    // =========================================================================
    // SECTION 8: 33 x 33m BOSS ARENA + OVERSEER BOSS + RETROWINDOWSGUI UI
    // =========================================================================
    private static void BuildSection8_BossArena(Transform parent, Transform gameplay, Transform enemiesRoot)
    {
        Vector3 arenaCenter = new Vector3(0f, 8.2f, 324.0f);
        BuildCarpetDeck(parent, "BossArena_Deck_33x33", arenaCenter, 11, 11, roundCorners: true);
        CreateCheckpoint(gameplay, "Checkpoint_9_BossArena", new Vector3(0f, 8.25f, 309.5f), bonusSeconds: 8f);

        PlaceTrashProp("Barriar", parent, "Boss_Cover_FL", new Vector3(-7.5f, 8.2f, 316.0f), 25f, fitCol: true);
        PlaceTrashProp("FracturedBarriar", parent, "Boss_Cover_FR", new Vector3(7.5f, 8.2f, 316.0f), -25f, fitCol: true);
        InstantiateOfficeFbx(ConfDesk, parent, "Boss_Cover_ML", new Vector3(-9.5f, 8.2f, 324.5f), 80f, isStatic: true, fitCollider: true);
        InstantiateOfficeFbx(ConfDesk, parent, "Boss_Cover_MR", new Vector3(9.5f, 8.2f, 324.5f), -80f, isStatic: true, fitCollider: true);
        PlaceTrashCluster(parent, "Boss_Debris_BL", new Vector3(-8.0f, 8.2f, 333.0f), 0);
        PlaceTrashCluster(parent, "Boss_Debris_BR", new Vector3(8.0f, 8.2f, 333.0f), 2);

        InstantiateOfficeFbx(ConfDeskSq, parent, "Boss_TurretPad_L", new Vector3(-13.5f, 8.2f, 335.0f), 0f, isStatic: true, fitCollider: true);
        InstantiateOfficeFbx(ConfDeskSq, parent, "Boss_TurretPad_R", new Vector3(13.5f, 8.2f, 335.0f), 0f, isStatic: true, fitCollider: true);

        GameObject turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurretEnemyPath);
        GameObject rangedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RangedEnemyPath);
        GameObject telegraphPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TelegraphVfxPath);
        GameObject vanishPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VanishVfxPath);

        Health tL = SpawnEnemy(turretPrefab, enemiesRoot, "Overseer_P3_TurretL", new Vector3(-13.5f, 9.1f, 335.0f), 145f, 5.0f);
        Health tR = SpawnEnemy(turretPrefab, enemiesRoot, "Overseer_P3_TurretR", new Vector3(13.5f, 9.1f, 335.0f), -145f, 5.0f);

        // 8 Rim Minion Spawn Points (r = 13.5m)
        GameObject spawnerGo = new GameObject("Overseer_MinionSpawner");
        spawnerGo.transform.SetParent(parent, false);
        spawnerGo.transform.position = arenaCenter;

        Transform[] spawnPts = new Transform[8];
        GameObject[] markers = new GameObject[8];
        for (int i = 0; i < 8; i++)
        {
            float ang = i * (Mathf.PI * 2f / 8f);
            Vector3 p = arenaCenter + new Vector3(Mathf.Cos(ang) * 13.5f, 0.05f, Mathf.Sin(ang) * 13.5f);

            GameObject pt = new GameObject($"MinionSpawn_{i + 1}");
            pt.transform.SetParent(spawnerGo.transform, false);
            pt.transform.position = p;
            pt.transform.LookAt(arenaCenter);
            spawnPts[i] = pt.transform;

            GameObject marker = new GameObject($"MinionTelegraph_{i + 1}");
            marker.transform.SetParent(spawnerGo.transform, false);
            marker.transform.position = p + Vector3.up * 0.15f;
            Light glow = marker.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.30f, 0.20f, 1f);
            glow.intensity = 4.5f;
            glow.range = 5.5f;
            glow.shadows = LightShadows.None;
            if (telegraphPrefab != null)
            {
                GameObject vfx = (GameObject)PrefabUtility.InstantiatePrefab(telegraphPrefab);
                vfx.transform.SetParent(marker.transform, false);
            }
            markers[i] = marker;
        }

        ParticleSystem vanishPs = null;
        if (vanishPrefab != null)
        {
            GameObject vfxGo = (GameObject)PrefabUtility.InstantiatePrefab(vanishPrefab);
            vfxGo.name = "MinionVanishVfx";
            vfxGo.transform.SetParent(spawnerGo.transform, false);
            vanishPs = vfxGo.GetComponent<ParticleSystem>();
        }

        MinionSpawner spawner = spawnerGo.AddComponent<MinionSpawner>();
        SerializedObject soSp = new SerializedObject(spawner);
        soSp.FindProperty("minionPrefab").objectReferenceValue = rangedPrefab;
        soSp.FindProperty("poolSize").intValue = 12;
        soSp.FindProperty("minionHealth").floatValue = 40f;
        soSp.FindProperty("minionSecondsReward").floatValue = 1.5f;
        soSp.FindProperty("aliveCap").intValue = 10;
        soSp.FindProperty("minSpawnInterval").floatValue = 4.0f;
        soSp.FindProperty("maxSpawnInterval").floatValue = 5.0f;
        soSp.FindProperty("minPlayerDistance").floatValue = 12.0f;
        soSp.FindProperty("telegraphSeconds").floatValue = 0.8f;
        SerializedProperty spArr = soSp.FindProperty("spawnPoints");
        spArr.arraySize = spawnPts.Length;
        for (int i = 0; i < spawnPts.Length; i++) spArr.GetArrayElementAtIndex(i).objectReferenceValue = spawnPts[i];
        SetGameObjectArray(soSp.FindProperty("telegraphMarkers"), markers);
        soSp.FindProperty("vanishVfxTemplate").objectReferenceValue = vanishPs;
        soSp.ApplyModifiedPropertiesWithoutUndo();

        // Boss Shockwave
        GameObject shockGo = new GameObject("Overseer_Shockwave");
        shockGo.transform.SetParent(parent, false);
        shockGo.transform.position = arenaCenter;
        Light pulseLight = new GameObject("ShockwaveLight").AddComponent<Light>();
        pulseLight.transform.SetParent(shockGo.transform, false);
        pulseLight.type = LightType.Point;
        pulseLight.range = 16f;
        pulseLight.shadows = LightShadows.None;
        BossShockwave shockwave = shockGo.AddComponent<BossShockwave>();
        SerializedObject soSw = new SerializedObject(shockwave);
        soSw.FindProperty("pulseLight").objectReferenceValue = pulseLight;
        soSw.ApplyModifiedPropertiesWithoutUndo();

        // Exit Portal -> SampleScene via Loading
        GameObject exitRoot = new GameObject("Level3_ExitPortal");
        exitRoot.transform.SetParent(parent, false);
        exitRoot.transform.position = new Vector3(0f, 9.9f, 338.0f);
        exitRoot.transform.localScale = Vector3.one;
        PlaceSimplePrefab(ExitPortalPath, exitRoot.transform, "ExitPortal_Visual", Vector3.zero, 0f, local: true);
        BoxCollider pCol = exitRoot.AddComponent<BoxCollider>();
        pCol.isTrigger = true;
        pCol.size = new Vector3(3.4f, 4.0f, 2.2f);
        PortalTrigger portalTrigger = exitRoot.AddComponent<PortalTrigger>();
        SerializedObject soPt = new SerializedObject(portalTrigger);
        soPt.FindProperty("targetScene").stringValue = "SampleScene";
        soPt.FindProperty("loadingSceneName").stringValue = "Loading";
        soPt.FindProperty("playerTag").stringValue = "Player";
        soPt.ApplyModifiedPropertiesWithoutUndo();

        // OVERSEER BOSS (unit-scale root + BoxCollider; visual child built properly with native FBX rot/scale)
        GameObject bossGo = new GameObject("Boss_OVERSEER");
        bossGo.transform.SetParent(enemiesRoot, false);
        bossGo.transform.position = new Vector3(0f, 11.2f, 331.0f);
        bossGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        bossGo.transform.localScale = Vector3.one;
        bossGo.tag = "Enemy";

        BoxCollider bossCol = bossGo.AddComponent<BoxCollider>();
        bossCol.size = new Vector3(3.2f, 4.2f, 3.2f);

        GameObject visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(bossGo.transform, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one;

        // Build towering multi-prop Office Monolith at unit scale first, then scale Visual child cleanly
        GameObject core = InstantiateOfficeFbx(FileCabLargest, visualRoot.transform, "Overseer_Core", bossGo.transform.position + new Vector3(0f, -0.75f, 0f), 180f, isStatic: false, fitCollider: false);
        GameObject eye = InstantiateOfficeFbx(MonitorFbx, visualRoot.transform, "Overseer_Eye", bossGo.transform.position + new Vector3(0f, 0.35f, -0.55f), 180f, isStatic: false, fitCollider: false);
        if (eye != null && bossGlowMat != null)
            foreach (MeshRenderer mr in eye.GetComponentsInChildren<MeshRenderer>())
                mr.sharedMaterial = bossGlowMat;

        visualRoot.transform.localScale = Vector3.one * 2.1f;

        GameObject bossLightGo = new GameObject("OverseerGlow");
        bossLightGo.transform.SetParent(bossGo.transform, false);
        bossLightGo.transform.localPosition = new Vector3(0f, 0.4f, 1.4f);
        Light bossLight = bossLightGo.AddComponent<Light>();
        bossLight.type = LightType.Point;
        bossLight.color = new Color(1f, 0.25f, 0.18f);
        bossLight.intensity = 4.5f;
        bossLight.range = 10f;
        bossLight.shadows = LightShadows.None;

        GameObject muzzle = new GameObject("FireMuzzle");
        muzzle.transform.SetParent(bossGo.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.2f, 1.7f);

        // Enrage Shield Visual
        GameObject shieldGo = new GameObject("EnrageShield");
        shieldGo.transform.SetParent(bossGo.transform, false);
        ParticleSystem shieldPs = shieldGo.AddComponent<ParticleSystem>();
        var sm = shieldPs.main;
        sm.loop = true;
        sm.startLifetime = 0.6f;
        sm.startSpeed = 0.5f;
        sm.startSize = 0.55f;
        sm.startColor = new Color(1f, 0.22f, 0.18f, 0.9f);
        var sem = shieldPs.emission;
        sem.rateOverTime = 26f;
        var ssh = shieldPs.shape;
        ssh.shapeType = ParticleSystemShapeType.Sphere;
        ssh.radius = 2.2f;
        ParticleSystemRenderer spsr = shieldGo.GetComponent<ParticleSystemRenderer>();
        spsr.shadowCastingMode = ShadowCastingMode.Off;
        if (orbParticleMat != null) spsr.sharedMaterial = orbParticleMat;
        Light shieldLight = shieldGo.AddComponent<Light>();
        shieldLight.type = LightType.Point;
        shieldLight.color = new Color(1f, 0.2f, 0.15f, 1f);
        shieldLight.intensity = 5.5f;
        shieldLight.range = 9f;
        shieldLight.shadows = LightShadows.None;

        GameObject floorOrigin = new GameObject("ShockwaveFloorOrigin");
        floorOrigin.transform.SetParent(parent, false);
        floorOrigin.transform.position = new Vector3(0f, 8.2f, 331.0f);

        BossOverseer boss = bossGo.AddComponent<BossOverseer>();
        EnemyLaserProjectile laserPrefab = AssetDatabase.LoadAssetAtPath<EnemyLaserProjectile>(EnemyLaserPath);
        SerializedObject soBoss = new SerializedObject(boss);
        soBoss.FindProperty("health").intValue = 1232;
        soBoss.FindProperty("secondsReward").floatValue = 15f;
        soBoss.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
        soBoss.FindProperty("fireMuzzle").objectReferenceValue = muzzle.transform;
        soBoss.FindProperty("projectilePrefab").objectReferenceValue = laserPrefab;
        soBoss.FindProperty("minionSpawner").objectReferenceValue = spawner;
        soBoss.FindProperty("shockwave").objectReferenceValue = shockwave;
        soBoss.FindProperty("shieldVisual").objectReferenceValue = shieldGo;
        soBoss.FindProperty("exitPortalRoot").objectReferenceValue = exitRoot;
        soBoss.FindProperty("shockwaveFloorOrigin").objectReferenceValue = floorOrigin.transform;
        SetGameObjectArray(soBoss.FindProperty("phase3Turrets"), new[]
        {
            tL != null ? tL.gameObject : null,
            tR != null ? tR.gameObject : null
        });
        soBoss.ApplyModifiedPropertiesWithoutUndo();

        // Boss Arena Entry Trigger
        GameObject trigGo = new GameObject("OverseerArenaTrigger");
        trigGo.transform.SetParent(parent, false);
        trigGo.transform.position = arenaCenter + Vector3.up * 2.0f;
        BoxCollider trigCol = trigGo.AddComponent<BoxCollider>();
        trigCol.isTrigger = true;
        trigCol.size = new Vector3(32f, 6f, 31f);
        OverseerArenaTrigger arenaTrig = trigGo.AddComponent<OverseerArenaTrigger>();
        SerializedObject soTrig = new SerializedObject(arenaTrig);
        soTrig.FindProperty("boss").objectReferenceValue = boss;
        soTrig.ApplyModifiedPropertiesWithoutUndo();

        // RetroWindowsGUI Boss UI (UI Toolkit UIDocument, non-blocking, sortingOrder 5)
        GameObject bossUiGo = new GameObject("Level3_BossUI");
        bossUiGo.transform.SetParent(parent, false);
        UIDocument bossUiDoc = bossUiGo.AddComponent<UIDocument>();
        bossUiDoc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        bossUiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BossUxmlPath);
        bossUiDoc.sortingOrder = 5;

        Level3BossUIController bossUiCtrl = bossUiGo.AddComponent<Level3BossUIController>();
        SerializedObject soUi = new SerializedObject(bossUiCtrl);
        soUi.FindProperty("boss").objectReferenceValue = boss;
        soUi.FindProperty("minionSpawner").objectReferenceValue = spawner;
        soUi.ApplyModifiedPropertiesWithoutUndo();
    }

    // =========================================================================
    // ENEMIES & ARENA DIRECTORS (21 REGULAR ENEMIES)
    // =========================================================================
    private static void PlaceEnemiesAndDirectors(Transform enemiesRoot, Transform orbsRoot)
    {
        GameObject rangedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RangedEnemyPath);
        GameObject turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurretEnemyPath);
        GameObject telegraphPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TelegraphVfxPath);

        // ARENA A (4 enemies)
        Transform arenaAGroup = new GameObject("ArenaA_Enemies").transform;
        arenaAGroup.SetParent(enemiesRoot, false);
        Health[] aW1 =
        {
            SpawnEnemy(rangedPrefab, arenaAGroup, "ArenaA_W1_Ranged1", new Vector3(-6.5f, 0.05f, 40.5f), 180f, 3.0f),
            SpawnEnemy(rangedPrefab, arenaAGroup, "ArenaA_W1_Ranged2", new Vector3(6.5f, 0.05f, 40.5f), 180f, 3.0f)
        };
        Vector3[] aW2Pos = { new Vector3(-8.5f, 0.05f, 46.5f), new Vector3(8.5f, 0.05f, 46.5f) };
        Health[] aW2 =
        {
            SpawnEnemy(rangedPrefab, arenaAGroup, "ArenaA_W2_Ranged1", aW2Pos[0], 180f, 3.0f),
            SpawnEnemy(rangedPrefab, arenaAGroup, "ArenaA_W2_Ranged2", aW2Pos[1], 180f, 3.0f)
        };
        CreateArenaDirector(arenaAGroup, "ArenaA_Director", new Vector3(0f, 1.5f, 36.5f), new Vector3(26f, 6f, 26f),
            aW1, aW2, aW2Pos, telegraphPrefab, orbsRoot, new Vector3(0f, 1.2f, 39.5f), "ArenaA");

        // ARENA B (5 enemies: 3 elevated turrets + 2 Wave 2 ranged)
        Transform arenaBGroup = new GameObject("ArenaB_Enemies").transform;
        arenaBGroup.SetParent(enemiesRoot, false);
        Health[] bW1 =
        {
            SpawnEnemy(turretPrefab, arenaBGroup, "ArenaB_W1_Turret_FL", new Vector3(-12.5f, 11.25f, 155.5f), 145f, 5.0f),
            SpawnEnemy(turretPrefab, arenaBGroup, "ArenaB_W1_Turret_FR", new Vector3(12.5f, 11.25f, 155.5f), -145f, 5.0f),
            SpawnEnemy(turretPrefab, arenaBGroup, "ArenaB_W1_Turret_BR", new Vector3(12.5f, 11.25f, 168.5f), -160f, 5.0f)
        };
        Vector3[] bW2Pos = { new Vector3(-5.5f, 8.25f, 167.5f), new Vector3(5.5f, 8.25f, 167.5f) };
        Health[] bW2 =
        {
            SpawnEnemy(rangedPrefab, arenaBGroup, "ArenaB_W2_Ranged1", bW2Pos[0], 180f, 3.0f),
            SpawnEnemy(rangedPrefab, arenaBGroup, "ArenaB_W2_Ranged2", bW2Pos[1], 180f, 3.0f)
        };
        CreateArenaDirector(arenaBGroup, "ArenaB_Director", new Vector3(0f, 10.2f, 161.5f), new Vector3(28f, 8f, 26f),
            bW1, bW2, bW2Pos, telegraphPrefab, orbsRoot, new Vector3(0f, 9.4f, 164.5f), "ArenaB");

        // ARENA C (6 enemies)
        Transform arenaCGroup = new GameObject("ArenaC_Enemies").transform;
        arenaCGroup.SetParent(enemiesRoot, false);
        Health[] cW1 =
        {
            SpawnEnemy(rangedPrefab, arenaCGroup, "ArenaC_W1_Ranged1", new Vector3(-7.0f, 8.25f, 265.0f), 180f, 3.0f),
            SpawnEnemy(rangedPrefab, arenaCGroup, "ArenaC_W1_Ranged2", new Vector3(7.0f, 8.25f, 265.0f), 180f, 3.0f),
            SpawnEnemy(rangedPrefab, arenaCGroup, "ArenaC_W1_Ranged3", new Vector3(0f, 8.25f, 271.0f), 180f, 3.0f),
            SpawnEnemy(turretPrefab, arenaCGroup, "ArenaC_W1_Turret", new Vector3(0f, 9.15f, 262.5f), 180f, 5.0f)
        };
        Vector3[] cW2Pos = { new Vector3(-9.0f, 8.25f, 272.0f), new Vector3(9.0f, 8.25f, 272.0f) };
        Health[] cW2 =
        {
            SpawnEnemy(rangedPrefab, arenaCGroup, "ArenaC_W2_Ranged1", cW2Pos[0], 180f, 3.0f),
            SpawnEnemy(rangedPrefab, arenaCGroup, "ArenaC_W2_Ranged2", cW2Pos[1], 180f, 3.0f)
        };
        CreateArenaDirector(arenaCGroup, "ArenaC_Director", new Vector3(0f, 9.8f, 262.5f), new Vector3(26f, 6f, 26f),
            cW1, cW2, cW2Pos, telegraphPrefab, orbsRoot, new Vector3(0f, 9.4f, 266.0f), "ArenaC");

        // COMBO COURSE AMBUSH ENEMIES (6 total)
        Transform ambushGroup = new GameObject("Ambush_Enemies").transform;
        ambushGroup.SetParent(enemiesRoot, false);
        SpawnEnemy(rangedPrefab, ambushGroup, "Ambush_1_Combo1", new Vector3(-7.5f, 0.55f, 72.5f), 180f, 3.0f);
        SpawnEnemy(rangedPrefab, ambushGroup, "Ambush_2_C1Join", new Vector3(3.8f, 1.05f, 96.5f), 180f, 3.0f);
        SpawnEnemy(rangedPrefab, ambushGroup, "Ambush_3_ShaftMid", new Vector3(-8.5f, 4.65f, 128.0f), 180f, 3.0f);
        SpawnEnemy(rangedPrefab, ambushGroup, "Ambush_4_ShaftTop", new Vector3(5.5f, 8.25f, 139.5f), 180f, 3.0f);
        SpawnEnemy(rangedPrefab, ambushGroup, "Ambush_5_Combo2Hub", new Vector3(3.5f, 8.25f, 219.0f), 180f, 3.0f);
        SpawnEnemy(rangedPrefab, ambushGroup, "Ambush_6_BossApproach", new Vector3(0f, 8.25f, 286.5f), 180f, 3.0f);
    }

    private static Health SpawnEnemy(GameObject prefab, Transform parent, string name, Vector3 pos, float yaw, float rewardSeconds)
    {
        if (prefab == null) return null;
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.name = name;
        inst.transform.SetParent(parent, false);
        inst.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        inst.transform.localScale = Vector3.one;

        HealthTimeReward reward = inst.GetComponent<HealthTimeReward>() ?? inst.AddComponent<HealthTimeReward>();
        reward.secondsReward = rewardSeconds;

        GameObject accentGo = new GameObject("EnemyAccentLight");
        accentGo.transform.SetParent(inst.transform, false);
        accentGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        Light accent = accentGo.AddComponent<Light>();
        accent.type = LightType.Point;
        accent.color = new Color(1f, 0.34f, 0.20f, 1f);
        accent.intensity = 2.6f;
        accent.range = 5.0f;
        accent.shadows = LightShadows.None;

        return inst.GetComponent<Health>();
    }

    private static void CreateArenaDirector(
        Transform parent, string name, Vector3 triggerCenter, Vector3 triggerSize,
        Health[] wave1, Health[] wave2, Vector3[] wave2Pos, GameObject telegraphPrefab,
        Transform orbsRoot, Vector3 rewardCenter, string arenaTag)
    {
        GameObject dirGo = new GameObject(name);
        dirGo.transform.SetParent(parent, false);
        dirGo.transform.position = triggerCenter;

        BoxCollider trig = dirGo.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.size = triggerSize;

        GameObject[] markers = new GameObject[wave2Pos.Length];
        for (int i = 0; i < wave2Pos.Length; i++)
        {
            GameObject marker = new GameObject($"{arenaTag}_Wave2_Telegraph_{i}");
            marker.transform.SetParent(dirGo.transform, false);
            marker.transform.position = wave2Pos[i] + new Vector3(0f, 0.15f, 0f);

            Light glow = marker.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.28f, 0.18f, 1f);
            glow.intensity = 4.5f;
            glow.range = 5.5f;
            glow.shadows = LightShadows.None;

            if (telegraphPrefab != null)
            {
                GameObject vfx = (GameObject)PrefabUtility.InstantiatePrefab(telegraphPrefab);
                vfx.transform.SetParent(marker.transform, false);
            }
            markers[i] = marker;
        }

        GameObject[] rewardOrbs = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            Vector3 orbPos = rewardCenter + new Vector3((i - 1) * 2.2f, 0f, 0f);
            CreateTimeOrb(orbsRoot, $"Orb_{arenaTag}_Clear_{i + 1}", orbPos);
            rewardOrbs[i] = orbsRoot.GetChild(orbsRoot.childCount - 1).gameObject;
        }

        ArenaDirector director = dirGo.AddComponent<ArenaDirector>();
        SerializedObject so = new SerializedObject(director);
        SetHealthArray(so.FindProperty("wave1Enemies"), wave1);
        SetHealthArray(so.FindProperty("wave2Enemies"), wave2);
        SetGameObjectArray(so.FindProperty("wave2TelegraphMarkers"), markers);
        SetGameObjectArray(so.FindProperty("completionTimeOrbs"), rewardOrbs);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // =========================================================================
    // GAMEPLAY, LIGHTING & PROVEN PROP HELPERS (MATCHING LEVEL 2)
    // =========================================================================
    private static void SetupGameplay(Transform root)
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab != null)
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetParent(root, false);
            player.transform.SetPositionAndRotation(new Vector3(0f, 1.05f, -1.2f), Quaternion.identity);
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

        // Configure the single TimeManager built into GameUI.prefab (no duplicate!)
        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameUIPrefabPath);
        if (uiPrefab != null)
        {
            GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab);
            ui.transform.SetParent(root, false);
            ui.transform.localScale = Vector3.one;

            TimeManager tm = ui.GetComponent<TimeManager>() ?? ui.AddComponent<TimeManager>();
            tm.startTime = 100f;
            tm.maxTime = 140f;
            tm.drainRate = 0.85f;
            tm.isRunning = true;
        }

        GameObject cpMgr = new GameObject("CheckpointManager");
        cpMgr.transform.SetParent(root, false);
        cpMgr.AddComponent<CheckpointManager>();

        GameObject kz = new GameObject("Level3_KillZone");
        kz.transform.SetParent(root, false);
        kz.transform.position = new Vector3(0f, -18f, 170f);
        kz.transform.localScale = Vector3.one;
        BoxCollider kzCol = kz.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(220f, 8f, 460f);
        kz.AddComponent<Level3KillZone>();
    }

    private static void SetupLighting()
    {
        Transform lightRoot = new GameObject("Lighting").transform;

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
        sun.color = new Color(1f, 0.97f, 0.92f);
        sun.intensity = 1.50f;
        sun.shadows = LightShadows.Soft;

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile != null)
        {
            GameObject volGo = new GameObject("Global Volume");
            volGo.transform.SetParent(lightRoot, false);
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.profile = profile;
        }

        // Bright shadowless overhead fill lights across every section (zero dark spots)
        CreateAreaLight(lightRoot, "Light_Start",      new Vector3(0f, 7.5f,   4.0f), 2.4f, 26f);
        CreateAreaLight(lightRoot, "Light_ArenaA",     new Vector3(0f, 8.5f,  36.5f), 2.8f, 34f);
        CreateAreaLight(lightRoot, "Light_Combo1",     new Vector3(2f, 8.0f,  75.0f), 2.6f, 32f);
        CreateAreaLight(lightRoot, "Light_Shaft",      new Vector3(0f,13.0f, 122.0f), 2.8f, 34f);
        CreateAreaLight(lightRoot, "Light_ArenaB",     new Vector3(0f,17.0f, 161.5f), 2.8f, 36f);
        CreateAreaLight(lightRoot, "Light_Combo2A",    new Vector3(0f,16.0f, 198.0f), 2.6f, 34f);
        CreateAreaLight(lightRoot, "Light_Combo2B",    new Vector3(0f,16.0f, 232.0f), 2.6f, 32f);
        CreateAreaLight(lightRoot, "Light_ArenaC",     new Vector3(0f,17.0f, 262.5f), 2.8f, 36f);
        CreateAreaLight(lightRoot, "Light_BossArena",  new Vector3(0f,18.5f, 324.0f), 3.2f, 44f);
    }

    private static void CreateAreaLight(Transform parent, string name, Vector3 pos, float intensity, float range)
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

        // Forgiveness: +0.3m invisible BoxCollider margin past visible edges at exact same top height
        BoxCollider col = deckRoot.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -0.14f, 0f);
        col.size = new Vector3(tilesX * 3f + EdgeMargin * 2f, 0.28f, tilesZ * 3f + EdgeMargin * 2f);

        SetFlagsRecursive(deckRoot.gameObject,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.NavigationStatic);
    }

    private static void BuildWallRunSegment(Transform parent, string name, string wallFbxPath, Vector3 center, float lengthZ, float heightY, float facingSignX, int segments)
    {
        GameObject wallRoot = new GameObject(name);
        wallRoot.transform.SetParent(parent, false);
        wallRoot.transform.position = center;
        wallRoot.transform.localScale = Vector3.one;

        float segLen = lengthZ / Mathf.Max(1, segments);
        float startZ = -lengthZ * 0.5f + segLen * 0.5f;

        for (int i = 0; i < segments; i++)
        {
            Vector3 pos = center + new Vector3(0f, 0f, startZ + i * segLen);
            InstantiateOfficeFbx(wallFbxPath, wallRoot.transform, $"Seg_{i}", pos, 90f, isStatic: true, fitCollider: false);
        }

        // Forgiveness: continuous invisible lip BoxCollider proud on run face & +0.5m past ends
        BoxCollider lip = wallRoot.AddComponent<BoxCollider>();
        lip.size = new Vector3(0.55f + WallLipThickness, heightY, lengthZ + 1.0f);
        lip.center = new Vector3(facingSignX * (WallLipThickness * 0.5f), heightY * 0.5f, 0f);

        SetFlagsRecursive(wallRoot, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
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

        if (fitCollider)
        {
            BoxCollider bc = OfficePropColliderAndMaterialFixer.FitFlatWalkableBoxCollider(root, 0.16f);
            if (bc != null)
            {
                Vector3 sz = bc.size;
                bc.size = new Vector3(sz.x + EdgeMargin, sz.y, sz.z + EdgeMargin);
            }
        }

        if (isStatic)
        {
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.NavigationStatic;
            SetFlagsRecursive(root, flags);
        }

        builtPropsCount++;
        return root;
    }

    private static GameObject PlaceTrashProp(string subName, Transform parent, string name, Vector3 pos, float yaw, bool fitCol)
    {
        if (!trashCache.TryGetValue(subName, out GameObject src) || src == null) return null;

        GameObject holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        holder.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        holder.transform.localScale = Vector3.one;

        GameObject visual = Object.Instantiate(src);
        visual.name = "Model";
        visual.transform.SetParent(holder.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = src.transform.localRotation;
        visual.transform.localScale = src.transform.lossyScale;

        // Normalize cm export if needed, then center X/Z and flush bottom to pos.y
        Bounds b = CalcBounds(visual);
        float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (maxDim > 0.0001f && maxDim < 0.12f)
        {
            visual.transform.localScale *= 100f;
            b = CalcBounds(visual);
        }
        if (b.size.sqrMagnitude > 0.0001f)
            visual.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);

        if (fitCol)
            OfficePropColliderAndMaterialFixer.FitFlatWalkableBoxCollider(holder, 0.16f);

        SetFlagsRecursive(holder,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.NavigationStatic);
        builtPropsCount++;
        return holder;
    }

    private static void PlaceTrashCluster(Transform parent, string name, Vector3 pos, int variant)
    {
        GameObject cluster = new GameObject(name);
        cluster.transform.SetParent(parent, false);
        cluster.transform.position = pos;

        string[] a = { "CardboardBox_1", "CardboardBox_2", "Concrete_Debris_1", "TrashBag_1" };
        string[] b = { "TrashBag_2", "Cinder_Block_1", "Wheel_1", "CardboardBox_3" };
        string[] c = { "News_Stack", "Wooden_Plank_1", "GasCan", "Sheet_Metal_2" };

        PlaceTrashProp(a[variant % a.Length], cluster.transform, "P0", pos, variant * 40f, fitCol: true);
        PlaceTrashProp(b[variant % b.Length], cluster.transform, "P1", pos + new Vector3(0.85f, 0f, 0.45f), variant * 75f, fitCol: false);
        PlaceTrashProp(c[variant % c.Length], cluster.transform, "P2", pos + new Vector3(-0.75f, 0f, 0.55f), variant * 110f, fitCol: false);
    }

    private static void PlaceDirectionBeacon(Transform parent, string name, Vector3 pos, Color color)
    {
        GameObject lamp = InstantiateOfficeFbx(DeskLamp, parent, name, pos, 0f, isStatic: true, fitCollider: false);
        Transform host = lamp != null ? lamp.transform : parent;

        GameObject lightGo = new GameObject($"{name}_Light");
        lightGo.transform.SetParent(host, false);
        lightGo.transform.position = pos + new Vector3(0f, 0.9f, 0f);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = 3.8f;
        l.range = 7.5f;
        l.shadows = LightShadows.None;
    }

    private static void CreateCheckpoint(Transform parent, string name, Vector3 pos, float bonusSeconds)
    {
        GameObject cp = new GameObject(name);
        cp.transform.SetParent(parent, false);
        cp.transform.position = pos;
        cp.transform.localScale = Vector3.one;

        BoxCollider trig = cp.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.size = new Vector3(12f, 5f, 4f);
        trig.center = new Vector3(0f, 2.2f, 0f);

        InstantiateOfficeFbx(DeskLamp, cp.transform, $"{name}_Lamp", pos, 0f, isStatic: true, fitCollider: false);

        GameObject respawn = new GameObject("RespawnPoint");
        respawn.transform.SetParent(cp.transform, false);
        respawn.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        GameObject lightGo = new GameObject("CheckpointLight");
        lightGo.transform.SetParent(cp.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        Light ind = lightGo.AddComponent<Light>();
        ind.type = LightType.Point;
        ind.color = new Color(1f, 0.78f, 0.25f, 1f);
        ind.intensity = 3.0f;
        ind.range = 7.5f;
        ind.shadows = LightShadows.None;

        Level3CheckpointTrigger trigger = cp.AddComponent<Level3CheckpointTrigger>();
        SerializedObject so = new SerializedObject(trigger);
        so.FindProperty("respawnPoint").objectReferenceValue = respawn.transform;
        so.FindProperty("indicatorLight").objectReferenceValue = ind;
        so.FindProperty("checkpointTimeBonus").floatValue = bonusSeconds;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateTimeOrb(Transform parent, string name, Vector3 pos)
    {
        GameObject orb = new GameObject(name);
        orb.transform.SetParent(parent, false);
        orb.transform.position = pos;
        orb.transform.localScale = Vector3.one;

        SphereCollider col = orb.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.25f;

        // Real Office Wall Clock tinted emissive gold (ZERO primitives, thematic "Time Orb"!)
        GameObject clockVisual = InstantiateOfficeFbx(WallClockFbx, orb.transform, "GoldenClock", pos, 0f, isStatic: false, fitCollider: false);
        if (clockVisual != null)
        {
            clockVisual.transform.localPosition = Vector3.zero;
            clockVisual.transform.localScale = Vector3.one * 1.4f;
            if (goldenOrbMat != null)
            {
                foreach (MeshRenderer mr in clockVisual.GetComponentsInChildren<MeshRenderer>())
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
        l.range = 5.0f;
        l.shadows = LightShadows.None;

        TimeOrb comp = orb.AddComponent<TimeOrb>();
        SerializedObject so = new SerializedObject(comp);
        so.FindProperty("secondsBonus").floatValue = 2f;
        so.FindProperty("idleParticles").objectReferenceValue = idlePs;
        so.FindProperty("orbLight").objectReferenceValue = l;
        if (clockVisual != null) so.FindProperty("visualSpinTarget").objectReferenceValue = clockVisual.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        timeOrbCount++;
    }

    private static void PlaceSimplePrefab(string path, Transform parent, string name, Vector3 pos, float yaw, bool local = false)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.name = name;
        inst.transform.SetParent(parent, false);
        if (local) { inst.transform.localPosition = pos; inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f); }
        else inst.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        inst.transform.localScale = Vector3.one;
    }

    private static void LoadTrashLookup()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(TrashFbxPath);
        if (fbx == null) return;
        foreach (Transform child in fbx.transform)
            if (child.GetComponentInChildren<Renderer>(true) != null)
                trashCache[child.name] = child.gameObject;
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

    private static void SetFlagsRecursive(GameObject root, StaticEditorFlags flags)
    {
        GameObjectUtility.SetStaticEditorFlags(root, flags);
        foreach (Transform child in root.transform) SetFlagsRecursive(child.gameObject, flags);
    }

    private static void SetHealthArray(SerializedProperty prop, Health[] items)
    {
        prop.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    private static void SetGameObjectArray(SerializedProperty prop, GameObject[] items)
    {
        prop.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    private static Bounds CalcBounds(GameObject go)
    {
        Bounds b = default;
        bool has = false;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

    private static void UpdateBuildSettingsOrder()
    {
        string[] orderedPaths =
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/Loading.unity",
            "Assets/Scenes/Level1.unity",
            "Assets/Scenes/Level2.unity",
            ScenePath
        };
        var scenes = new List<EditorBuildSettingsScene>(orderedPaths.Length);
        foreach (string p in orderedPaths)
        {
            if (System.IO.File.Exists(p))
                scenes.Add(new EditorBuildSettingsScene(p, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int idx = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, idx), path.Substring(idx + 1));
    }
}
