using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Deterministic builder for Assets/Scenes/EndingLevel.unity.
/// Builds two contrasting sets strictly from Assets/OFFICE/:
/// 1) ChaoticDreamOffice (floating/overturned office echoing the surreal combat dream)
/// 2) NeatRealOffice (bright, warm, spotless office where our character wakes up at their desk chair).
/// Auto-builds if Assets/Scenes/EndingLevel.unity is missing and appends cleanly to Build Settings.
/// Run manually via: Tools -> Ending Level -> Build Ending Level.
/// </summary>
public static class EndingLevelBuilder
{
    public const string ScenePath = "Assets/Scenes/EndingLevel.unity";
    private const string MaterialFolder = "Assets/Materials/Ending";
    private const string SketchTexPath = "Assets/ToonSketchShaderPack/Textures/PainterlyMaps/painterly1.png";
    private const string ChaosSkyMatPath = "Assets/Materials/SkyBox2.mat";
    private const string NeatSkyMatPath = "Assets/Materials/Skybox1.mat";
    private const string PanelSettingsPath = "Assets/UI/GameUIPanelSettings.asset";
    private const string EndingUxmlPath = "Assets/UI/EndingUI.uxml";

    // Assets/OFFICE/ FBX library
    private const string Floor3x3 = "Assets/OFFICE/carpet_floor/floor_3x3.fbx";
    private const string Desk1 = "Assets/OFFICE/desks/desk1.fbx";
    private const string Desk2 = "Assets/OFFICE/desks/desk2.fbx";
    private const string Desk2Alt = "Assets/OFFICE/desks/desk2_alt.fbx";
    private const string Desk1Weathered = "Assets/OFFICE/desks/desk1_weathered.fbx";
    private const string Desk2Weathered = "Assets/OFFICE/desks/desk2_weathered.fbx";
    private const string ConfDesk = "Assets/OFFICE/desks/conference_desk.fbx";

    private const string ChairFancy = "Assets/OFFICE/chairs/office_chair_fancy.fbx";
    private const string ChairBlue = "Assets/OFFICE/chairs/office_chair_blue.fbx";
    private const string ChairGrey = "Assets/OFFICE/chairs/office_chair_grey.fbx";
    private const string ChairOrange = "Assets/OFFICE/chairs/office_chair_orange.fbx";
    private const string ChairBlack = "Assets/OFFICE/chairs/office_chair_black.fbx";

    private const string MonitorFbx = "Assets/OFFICE/computers/computer_monitor.fbx";
    private const string KeyboardFbx = "Assets/OFFICE/computers/computer_keyboard.fbx";
    private const string MouseFbx = "Assets/OFFICE/computers/computer_mouse.fbx";
    private const string TowerFbx = "Assets/OFFICE/computers/computer_tower.fbx";
    private const string TowerAltFbx = "Assets/OFFICE/computers/computer_tower_alt.fbx";

    private const string DivFullHBlue = "Assets/OFFICE/dividers/divider_full_h_blue.fbx";
    private const string DivVeneerBlue = "Assets/OFFICE/dividers/divider_veneer_blue.fbx";
    private const string DivBlackBlue = "Assets/OFFICE/dividers/divider_black_blue.fbx";
    private const string DivLowBlue = "Assets/OFFICE/dividers/divider_black_blue_low.fbx";

    private const string BookcaseBack = "Assets/OFFICE/book_things/book_case_with_back.fbx";
    private const string BooksFbx = "Assets/OFFICE/book_things/books.fbx";
    private const string BooksAltFbx = "Assets/OFFICE/book_things/books_alt.fbx";
    private const string BinderFbx = "Assets/OFFICE/book_things/file_binder.fbx";
    private const string BinderAltFbx = "Assets/OFFICE/book_things/file_binder_alt.fbx";

    private const string CabLargest = "Assets/OFFICE/file_cabinets/file_cabinet_largest.fbx";
    private const string CabLarge = "Assets/OFFICE/file_cabinets/file_cabinet_large.fbx";
    private const string CabSmallest = "Assets/OFFICE/file_cabinets/file_cabinet_smallest.fbx";
    private const string CabSmallShelves = "Assets/OFFICE/file_cabinets/file_cabinet_small_with_shelfs.fbx";
    private const string CabLargestOpen = "Assets/OFFICE/file_cabinets/file_cabinet_largest_open.fbx";
    private const string CabLargeOpen = "Assets/OFFICE/file_cabinets/file_cabinet_large_open.fbx";

    private const string HorizDesktopFbx = "Assets/OFFICE/computers/horizontal_desktop.fbx";

    private const string CouchBlue = "Assets/OFFICE/couches/couch_blue.fbx";
    private const string ArmchairBlue = "Assets/OFFICE/couches/armchair_blue.fbx";

    private const string DeskLamp = "Assets/OFFICE/extras/circular_desklamp.fbx";
    private const string SquareLamp = "Assets/OFFICE/extras/square_desklamp.fbx";
    private const string CoffeeMachine = "Assets/OFFICE/extras/coffee_machine.fbx";
    private const string MugFbx = "Assets/OFFICE/extras/mug.fbx";
    private const string MugAltFbx = "Assets/OFFICE/extras/mug_alt.fbx";
    private const string WaterCooler = "Assets/OFFICE/extras/water_cooler_bottle_attached.fbx";
    private const string Whiteboard = "Assets/OFFICE/extras/whiteboard.fbx";
    private const string WallClock = "Assets/OFFICE/extras/wall_clock.fbx";
    private const string DeskPhone = "Assets/OFFICE/extras/desk_phone_with_cable.fbx";

    private const string PaperStack = "Assets/OFFICE/stationary/paper_stack.fbx";
    private const string PaperStackLarge = "Assets/OFFICE/stationary/paper_stack_large.fbx";
    private const string PaperSingle = "Assets/OFFICE/stationary/paper_single_sheet.fbx";
    private const string PenFbx = "Assets/OFFICE/stationary/pen.fbx";

    private static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
    private static Shader toonShader;
    private static Texture2D sketchTexture;

    private const string VersionMarker = "HeroWorkstation_V3_Ceiling";

    [InitializeOnLoadMethod]
    private static void AutoBuildIfMissing()
    {
        if (!NeedsRebuild())
            return;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !NeedsRebuild())
                return;

            Scene active = SceneManager.GetActiveScene();
            if (active.isDirty && !string.IsNullOrEmpty(active.path) && active.path != ScenePath)
                return;

            string prevScenePath = active.path;
            BuildEndingLevel();
            if (!string.IsNullOrEmpty(prevScenePath) && prevScenePath != ScenePath && File.Exists(prevScenePath))
                EditorSceneManager.OpenScene(prevScenePath, OpenSceneMode.Single);
        };
    }

    private static bool NeedsRebuild()
    {
        if (!File.Exists(ScenePath))
            return true;
        string sceneText = File.ReadAllText(ScenePath);
        return !sceneText.Contains(VersionMarker);
    }

    [MenuItem("Tools/Ending Level/Build Ending Level")]
    public static void BuildEndingLevel()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialFolder);
        EnsureFolder("Assets/Scenes");

        matCache.Clear();
        toonShader = Shader.Find("Custom/URPToonSketch") ?? Shader.Find("Universal Render Pipeline/Lit");
        sketchTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SketchTexPath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material chaosSky = AssetDatabase.LoadAssetAtPath<Material>(ChaosSkyMatPath);
        Material neatSky = AssetDatabase.LoadAssetAtPath<Material>(NeatSkyMatPath) ?? chaosSky;
        if (chaosSky != null)
            RenderSettings.skybox = chaosSky;

        // Lighting root
        Transform lightingRoot = new GameObject("Lighting").transform;
        GameObject sunGo = new GameObject("Directional Light");
        sunGo.transform.SetParent(lightingRoot, false);
        sunGo.transform.rotation = Quaternion.Euler(36f, -35f, 0f);
        Light sunLight = sunGo.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(0.86f, 0.90f, 1.0f);
        sunLight.intensity = 1.2f;
        sunLight.shadows = LightShadows.Soft;

        // Camera & UI
        GameObject camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        Camera mainCamera = camGo.GetComponent<Camera>();
        mainCamera.clearFlags = CameraClearFlags.Skybox;
        mainCamera.nearClipPlane = 0.05f;
        mainCamera.farClipPlane = 300f;
        mainCamera.fieldOfView = 64f;
        Type uacd = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (uacd != null) camGo.AddComponent(uacd);

        GameObject uiGo = new GameObject("EndingUI");
        UIDocument uiDoc = uiGo.AddComponent<UIDocument>();
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        VisualTreeAsset endingUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EndingUxmlPath);
        if (panelSettings != null) uiDoc.panelSettings = panelSettings;
        if (endingUxml != null) uiDoc.visualTreeAsset = endingUxml;

        // Build Sets
        GameObject chaosRoot = new GameObject("ChaoticDreamOffice");
        (Transform[] chaosWaypoints, Transform[] chaosLookTargets) = BuildChaoticDreamSet(chaosRoot.transform);

        GameObject neatRoot = new GameObject("NeatRealOffice");
        var neatAnchors = BuildNeatRealOfficeSet(neatRoot.transform);

        // Cutscene controller
        GameObject controllerGo = new GameObject("EndingCutsceneController");
        EndingCutsceneController ctrl = controllerGo.AddComponent<EndingCutsceneController>();
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("mainCamera").objectReferenceValue = mainCamera;
        so.FindProperty("endingUiDoc").objectReferenceValue = uiDoc;
        so.FindProperty("sunLight").objectReferenceValue = sunLight;
        so.FindProperty("chaosSetRoot").objectReferenceValue = chaosRoot;
        so.FindProperty("neatOfficeSetRoot").objectReferenceValue = neatRoot;
        AssignTransformArray(so.FindProperty("chaosWaypoints"), chaosWaypoints);
        AssignTransformArray(so.FindProperty("chaosLookTargets"), chaosLookTargets);
        so.FindProperty("sleepPose").objectReferenceValue = neatAnchors.sleepPose;
        so.FindProperty("awakePose").objectReferenceValue = neatAnchors.awakePose;
        so.FindProperty("lookLeftTarget").objectReferenceValue = neatAnchors.lookLeft;
        so.FindProperty("lookRightTarget").objectReferenceValue = neatAnchors.lookRight;
        so.FindProperty("finalDeskTarget").objectReferenceValue = neatAnchors.finalDeskTarget;
        so.FindProperty("heroPullbackPose").objectReferenceValue = neatAnchors.heroPullback;
        so.FindProperty("chaosSkybox").objectReferenceValue = chaosSky;
        so.FindProperty("neatSkybox").objectReferenceValue = neatSky;
        so.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
        so.FindProperty("loadingSceneName").stringValue = "Loading";
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AppendToBuildSettingsWithoutReordering();
        AssetDatabase.SaveAssets();

        Debug.Log($"[EndingLevelBuilder] Built {ScenePath} and ensured Build Settings entry.");
    }

    // =========================================================================
    // ACT 1 SET: SHATTERED CHAOTIC DREAM OFFICE (around origin)
    // =========================================================================
    private static (Transform[] waypoints, Transform[] lookTargets) BuildChaoticDreamSet(Transform root)
    {
        Transform floorGroup = new GameObject("ShatteredFloor").transform;
        floorGroup.SetParent(root, false);

        Vector3[] slabPos =
        {
            new Vector3(-1.4f, 0.0f,  0.0f),
            new Vector3( 2.2f, 0.6f,  3.8f),
            new Vector3(-2.6f, 0.3f,  7.4f),
            new Vector3( 1.2f, 1.1f, 11.8f),
            new Vector3(-1.5f, 0.7f, 16.2f),
            new Vector3( 2.1f, 0.2f, 20.4f),
        };
        Vector3[] slabRot =
        {
            new Vector3( 5f,   8f, -7f),
            new Vector3(-8f,  22f, 11f),
            new Vector3( 9f, -15f, -9f),
            new Vector3(-6f,  35f,  8f),
            new Vector3(11f, -24f, 12f),
            new Vector3(-9f,  14f, -6f),
        };
        for (int i = 0; i < slabPos.Length; i++)
            InstantiateOffice(Floor3x3, floorGroup, $"ChaosFloor_{i}", slabPos[i], slabRot[i], isStatic: true);

        Transform propsGroup = new GameObject("OverturnedProps").transform;
        propsGroup.SetParent(root, false);

        var decorList = new List<FloatingDecorManager.DecorEntry>();
        System.Random rng = new System.Random(20261122);

        (string fbx, Vector3 pos, Vector3 rot)[] chaosItems =
        {
            (Desk1Weathered,   new Vector3(-2.4f, 0.9f,  1.2f), new Vector3( 18f,  32f, -24f)),
            (Desk2Weathered,   new Vector3( 2.7f, 1.6f,  5.0f), new Vector3(-22f, -45f,  19f)),
            (ConfDesk,         new Vector3( 0.4f, 2.2f, 11.2f), new Vector3( 14f,  68f, -31f)),
            (Desk1Weathered,   new Vector3(-2.9f, 1.8f, 14.5f), new Vector3(-28f,  20f,  40f)),
            (Desk2Weathered,   new Vector3( 2.5f, 1.1f, 18.8f), new Vector3( 25f, -35f, -18f)),

            (ChairBlue,        new Vector3( 1.4f, 2.3f,  2.1f), new Vector3( 65f,  40f,  25f)),
            (ChairOrange,      new Vector3(-1.8f, 2.9f,  6.8f), new Vector3(-55f, 110f, -35f)),
            (ChairBlack,       new Vector3( 2.9f, 3.4f, 10.1f), new Vector3( 45f, -60f,  75f)),
            (ChairGrey,        new Vector3(-1.1f, 2.6f, 15.8f), new Vector3(-70f,  25f,  40f)),
            (ChairOrange,      new Vector3( 1.9f, 2.1f, 21.0f), new Vector3( 35f, 140f, -50f)),

            (CabLargestOpen,   new Vector3(-4.2f, 1.4f,  4.4f), new Vector3( 16f,  45f, -28f)),
            (CabLargeOpen,     new Vector3( 4.1f, 2.7f,  8.6f), new Vector3(-24f, -30f,  34f)),
            (BookcaseBack,     new Vector3(-4.6f, 2.2f, 12.6f), new Vector3( 32f,  70f, -19f)),
            (Whiteboard,       new Vector3( 3.9f, 3.8f, 14.8f), new Vector3(-18f, -110f, 25f)),
            (WaterCooler,      new Vector3(-3.2f, 3.5f, 18.2f), new Vector3( 42f,  15f, -48f)),
            (WallClock,        new Vector3( 0.8f, 4.9f,  8.2f), new Vector3( 20f, 165f, -18f)),
            (CouchBlue,        new Vector3( 4.6f, 1.9f, 16.5f), new Vector3(-30f, -85f,  22f)),

            (MonitorFbx,       new Vector3(-0.7f, 3.1f,  3.2f), new Vector3( 25f, 150f, -20f)),
            (MonitorFbx,       new Vector3( 1.5f, 3.9f, 12.9f), new Vector3(-32f,  45f,  28f)),
            (KeyboardFbx,      new Vector3( 0.6f, 2.8f,  4.1f), new Vector3( 45f, -20f,  60f)),
            (DeskLamp,         new Vector3(-2.1f, 3.4f,  8.9f), new Vector3( 50f,  90f, -35f)),
            (MugFbx,           new Vector3( 0.2f, 3.2f,  6.2f), new Vector3( 65f,  12f, -40f)),
            (PaperStackLarge,  new Vector3(-1.0f, 2.4f,  9.5f), new Vector3( 18f,  55f, -35f)),
            (PaperSingle,      new Vector3( 0.8f, 4.1f,  1.8f), new Vector3( 35f,  75f,  55f)),
            (PaperSingle,      new Vector3(-1.2f, 3.6f,  5.6f), new Vector3(-40f, 120f,  30f)),
            (PaperSingle,      new Vector3( 1.3f, 3.2f,  8.4f), new Vector3( 25f, -40f, -65f)),
            (PaperSingle,      new Vector3(-0.6f, 4.2f, 13.4f), new Vector3(-55f,  35f,  45f)),
            (PaperSingle,      new Vector3( 1.9f, 3.7f, 17.0f), new Vector3( 42f, 160f, -25f)),
            (BinderFbx,        new Vector3(-2.5f, 4.0f, 11.4f), new Vector3( 28f,  60f,  50f)),
            (BooksFbx,         new Vector3( 2.4f, 4.3f, 13.8f), new Vector3(-35f, -50f,  42f)),
        };

        for (int i = 0; i < chaosItems.Length; i++)
        {
            var item = chaosItems[i];
            GameObject go = InstantiateOffice(item.fbx, propsGroup, $"ChaosProp_{i}", item.pos, item.rot, isStatic: false);
            if (go == null) continue;

            bool isLightDebris = item.fbx == PaperSingle || item.fbx == MonitorFbx || item.fbx == KeyboardFbx || item.fbx == MugFbx;
            float rotSpeed = isLightDebris ? 16f : 7f;
            decorList.Add(new FloatingDecorManager.DecorEntry
            {
                target = go.transform,
                basePosition = item.pos,
                rotSpeed = new Vector3(
                    Mathf.Lerp(-rotSpeed, rotSpeed, (float)rng.NextDouble()),
                    Mathf.Lerp(-rotSpeed, rotSpeed, (float)rng.NextDouble()),
                    Mathf.Lerp(-rotSpeed, rotSpeed, (float)rng.NextDouble())),
                bobAmp = isLightDebris ? 0.35f : 0.22f,
                bobFreq = Mathf.Lerp(0.55f, 1.05f, (float)rng.NextDouble()),
                phase = Mathf.Lerp(0f, Mathf.PI * 2f, (float)rng.NextDouble()),
                cullByDistance = false
            });
        }

        GameObject decorMgrGo = new GameObject("ChaosFloatManager");
        decorMgrGo.transform.SetParent(root, false);
        FloatingDecorManager fdm = decorMgrGo.AddComponent<FloatingDecorManager>();
        fdm.SetEntries(decorList.ToArray());

        // Cool eerie lights across chaos corridor
        CreatePointLight(root, "ChaosLight_1", new Vector3(-1f, 5.5f,  3f), new Color(0.55f, 0.72f, 1.0f), 3.4f, 22f);
        CreatePointLight(root, "ChaosLight_2", new Vector3( 1f, 6.2f, 11f), new Color(0.85f, 0.55f, 1.0f), 3.2f, 24f);
        CreatePointLight(root, "ChaosLight_3", new Vector3( 0f, 5.5f, 19f), new Color(0.45f, 0.85f, 1.0f), 3.2f, 22f);

        // Smooth camera flight corridor through the floating mess
        Transform camMarkers = new GameObject("ChaosCameraPath").transform;
        camMarkers.SetParent(root, false);
        Vector3[] wp =
        {
            new Vector3( 0.0f, 3.6f, -6.5f),
            new Vector3(-1.4f, 4.1f,  0.5f),
            new Vector3( 1.2f, 4.4f,  7.5f),
            new Vector3(-1.0f, 3.9f, 14.5f),
            new Vector3( 0.4f, 3.4f, 21.5f),
        };
        Vector3[] lt =
        {
            new Vector3( 0.0f, 2.3f,  4.0f),
            new Vector3( 0.5f, 2.3f,  8.5f),
            new Vector3(-0.4f, 2.5f, 13.5f),
            new Vector3( 0.3f, 2.2f, 19.5f),
            new Vector3( 0.0f, 2.0f, 26.0f),
        };

        return (CreateMarkers(camMarkers, "ChaosWP", wp), CreateMarkers(camMarkers, "ChaosLook", lt));
    }

    // =========================================================================
    // ACT 2 SET: SPOTLESS MORNING OFFICE & OUR WORKSTATION (centered at Z = 122)
    // =========================================================================
    private struct NeatAnchors
    {
        public Transform sleepPose;
        public Transform awakePose;
        public Transform lookLeft;
        public Transform lookRight;
        public Transform finalDeskTarget;
        public Transform heroPullback;
    }

    private static NeatAnchors BuildNeatRealOfficeSet(Transform root)
    {
        const float centerZ = 122f;

        // Measure carpet floor tile footprint dynamically so corner-pivoted 3x3 tiles seam flush
        Bounds tileSample = MeasureAssetBounds(Floor3x3);
        float tileW = Mathf.Max(2.5f, tileSample.size.x);
        float tileD = Mathf.Max(2.5f, tileSample.size.z);

        Transform floorGroup = new GameObject("NeatCarpetFloor").transform;
        floorGroup.SetParent(root, false);
        for (int ix = -2; ix <= 2; ix++)
        {
            for (int iz = -2; iz <= 2; iz++)
            {
                Vector3 pos = new Vector3(ix * tileW, 0f, centerZ + iz * tileD);
                PlaceCenteredOnFloor(Floor3x3, floorGroup, $"Carpet_{ix}_{iz}", pos, 0f, 0f, isStatic: true);
            }
        }

        float roomHalfX = tileW * 2.5f;
        float roomMinZ = centerZ - tileD * 2.5f;
        float roomMaxZ = centerZ + tileD * 2.5f;
        const float ceilingY = 3.60f;

        // Enclosed two-tier perimeter walls (0..3.65m) + ceiling panel grid + indoor ceiling lighting
        Transform wallsGroup = new GameObject("OfficeWalls").transform;
        wallsGroup.SetParent(root, false);
        const float wallMargin = 0.14f;

        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * tileW;
            string panelFbx = i % 2 == 0 ? DivVeneerBlue : DivBlackBlue;
            PlaceAligned(panelFbx,      wallsGroup, $"WallNorth_{i}",      new Vector3(x, 0f, roomMaxZ - wallMargin),          0f,    false, new Vector3(0f, 0f, 0f),   isStatic: true, castShadows: false);
            PlaceAligned(panelFbx,      wallsGroup, $"WallNorthUpper_{i}", new Vector3(x, 0f, roomMaxZ - wallMargin + 0.01f),  1.15f, false, new Vector3(0f, 0f, 0f),   isStatic: true, castShadows: false);
            PlaceAligned(DivVeneerBlue, wallsGroup, $"WallSouth_{i}",      new Vector3(x, 0f, roomMinZ + wallMargin),          0f,    false, new Vector3(0f, 180f, 0f), isStatic: true, castShadows: false);
            PlaceAligned(DivVeneerBlue, wallsGroup, $"WallSouthUpper_{i}", new Vector3(x, 0f, roomMinZ + wallMargin - 0.01f),  1.15f, false, new Vector3(0f, 180f, 0f), isStatic: true, castShadows: false);
        }

        for (int i = 0; i < 5; i++)
        {
            float z = centerZ + (i - 2) * tileD;
            string panelFbx = i % 2 == 0 ? DivVeneerBlue : DivBlackBlue;
            PlaceAligned(panelFbx, wallsGroup, $"WallWest_{i}",      new Vector3(-roomHalfX + wallMargin,         0f, z), 0f,    false, new Vector3(0f, 90f, 0f),  isStatic: true, castShadows: false);
            PlaceAligned(panelFbx, wallsGroup, $"WallWestUpper_{i}", new Vector3(-roomHalfX + wallMargin - 0.01f, 0f, z), 1.15f, false, new Vector3(0f, 90f, 0f),  isStatic: true, castShadows: false);
            PlaceAligned(panelFbx, wallsGroup, $"WallEast_{i}",      new Vector3( roomHalfX - wallMargin,         0f, z), 0f,    false, new Vector3(0f, -90f, 0f), isStatic: true, castShadows: false);
            PlaceAligned(panelFbx, wallsGroup, $"WallEastUpper_{i}", new Vector3( roomHalfX - wallMargin + 0.01f, 0f, z), 1.15f, false, new Vector3(0f, -90f, 0f), isStatic: true, castShadows: false);
        }

        // Full 15m x 15m office ceiling (Y = 3.60m) with architectural cross-beams & zero exterior shadow casting
        Transform ceilingGroup = new GameObject("NeatOfficeCeiling").transform;
        ceilingGroup.SetParent(root, false);
        for (int ix = -2; ix <= 2; ix++)
        {
            for (int iz = -2; iz <= 2; iz++)
            {
                Vector3 cPos = new Vector3(ix * tileW, 0f, centerZ + iz * tileD);
                GameObject tile = PlaceCenteredOnCeiling(Floor3x3, ceilingGroup, $"Ceiling_{ix}_{iz}", cPos, ceilingY, 0f, isStatic: true);
                ApplyCeilingPanelMaterial(tile);
            }
        }

        float[] beamXs = { -3.0f, 0.0f, 3.0f };
        foreach (float bx in beamXs)
        {
            for (int iz = -2; iz <= 2; iz++)
            {
                Vector3 bPos = new Vector3(bx, 0f, centerZ + iz * tileD);
                PlaceAligned(DivLowBlue, ceilingGroup, $"CeilingBeam_{bx}_{iz}", bPos, ceilingY - 0.08f, alignTop: false,
                    new Vector3(90f, 90f, 0f), isStatic: true, castShadows: false);
            }
        }

        // Whiteboards & wall clock mounted cleanly in the room perimeter
        PlaceOnFloor(Whiteboard, wallsGroup, "OfficeWhiteboard_North", new Vector3(-0.55f, 0f, roomMaxZ - 0.58f), 0f, 180f, isStatic: true);
        InstantiateOffice(WallClock, wallsGroup, "OfficeWallClock", new Vector3(1.10f, 2.08f, roomMaxZ - 0.36f), new Vector3(0f, 180f, 0f), isStatic: true);
        PlaceOnFloor(Whiteboard, wallsGroup, "OfficeWhiteboard_L", new Vector3(-roomHalfX + 0.62f, 0f, centerZ + 1.8f), 0f,  90f, isStatic: true);
        PlaceOnFloor(Whiteboard, wallsGroup, "OfficeWhiteboard_R", new Vector3( roomHalfX - 0.62f, 0f, centerZ + 1.8f), 0f, -90f, isStatic: true);

        // 4 Tidy coworker workstations (2 left, 2 right)
        Transform workstations = new GameObject("TidyWorkstations").transform;
        workstations.SetParent(root, false);
        BuildTidyWorkstation(workstations, "Desk_Left_Front",  new Vector3(-4.4f, 0f, centerZ + 0.2f), facePositiveZ: true,  isBlueChair: true);
        BuildTidyWorkstation(workstations, "Desk_Left_Back",   new Vector3(-4.4f, 0f, centerZ + 3.8f), facePositiveZ: true,  isBlueChair: false);
        BuildTidyWorkstation(workstations, "Desk_Right_Front", new Vector3( 4.4f, 0f, centerZ + 0.2f), facePositiveZ: true,  isBlueChair: false);
        BuildTidyWorkstation(workstations, "Desk_Right_Back",  new Vector3( 4.4f, 0f, centerZ + 3.8f), facePositiveZ: true,  isBlueChair: true);

        // Low blue divider screens separating center aisle cleanly
        PlaceOnFloor(DivLowBlue, workstations, "AisleDivider_L", new Vector3(-2.55f, 0f, centerZ + 2.0f), 0f,  90f, isStatic: true);
        PlaceOnFloor(DivLowBlue, workstations, "AisleDivider_R", new Vector3( 2.55f, 0f, centerZ + 2.0f), 0f, -90f, isStatic: true);

        // Neat archive wall & coffee lounge corner
        Transform lounge = new GameObject("ArchiveAndLounge").transform;
        lounge.SetParent(root, false);

        Bounds bc1 = PlaceOnFloor(BookcaseBack, lounge, "Bookcase_NorthL", new Vector3(-3.0f, 0f, roomMaxZ - 0.65f), 0f, 180f, isStatic: true);
        PlaceOnShelfBooks(lounge, "ShelfBooks_NL", bc1, yaw: 180f);

        Bounds bc2 = PlaceOnFloor(BookcaseBack, lounge, "Bookcase_NorthR", new Vector3( 3.0f, 0f, roomMaxZ - 0.65f), 0f, 180f, isStatic: true);
        PlaceOnShelfBooks(lounge, "ShelfBooks_NR", bc2, yaw: 180f);

        PlaceOnFloor(CabLargest, lounge, "FileCabinet_NW1", new Vector3(-6.0f, 0f, roomMaxZ - 0.72f), 0f, 0f, isStatic: true);
        PlaceOnFloor(CabLarge,   lounge, "FileCabinet_NW2", new Vector3(-4.6f, 0f, roomMaxZ - 0.72f), 0f, 180f, isStatic: true);

        PlaceOnFloor(CouchBlue,     lounge, "BreakCouch",          new Vector3(5.4f, 0f, roomMaxZ - 1.35f), 0f, 215f, isStatic: true);
        PlaceOnFloor(ArmchairBlue,  lounge, "BreakArmchair",       new Vector3(6.4f, 0f, roomMaxZ - 2.95f), 0f, 250f, isStatic: true);
        PlaceOnFloor(CoffeeMachine, lounge, "OfficeCoffeeMachine", new Vector3(6.3f, 0f, centerZ + 3.2f),   0f, -90f, isStatic: true);
        PlaceOnFloor(WaterCooler,   lounge, "OfficeWaterCooler",   new Vector3(6.5f, 0f, centerZ + 1.6f),   0f, -90f, isStatic: true);

        // =====================================================================
        // OUR HERO WORKSTATION ("OUR DESK, COMPUTER & CHAIR")
        // =====================================================================
        Transform heroGroup = new GameObject(VersionMarker).transform;
        heroGroup.SetParent(root, false);

        const float heroDeskZ = centerZ - 1.75f; // 120.25
        Bounds deskBounds = PlaceOnFloor(Desk1, heroGroup, "OurDesk", new Vector3(0f, 0f, heroDeskZ), 0f, 180f, isStatic: true);
        float deskTopY = deskBounds.max.y;

        PlaceOnFloor(CabSmallShelves, heroGroup, "OurSideReturn", new Vector3(-1.42f, 0f, heroDeskZ), 0f, 180f, isStatic: true);
        PlaceOnFloor(ChairFancy,      heroGroup, "OurChair",      new Vector3(-0.06f, 0f, heroDeskZ - 0.96f), 0f, 168f, isStatic: true);

        // Monitor screen, keyboard, 1:1 mouse, tower, lamp, mug, phone, papers, pen & binder
        PlaceOnFloor(MonitorFbx,   heroGroup, "OurMonitor",   new Vector3( 0.00f, 0f, heroDeskZ + 0.18f), deskTopY, 180f, isStatic: true);
        PlaceOnFloor(TowerAltFbx,  heroGroup, "OurPcTower",   new Vector3( 0.76f, 0f, heroDeskZ + 0.12f), deskTopY, 180f, isStatic: true);
        PlaceOnFloor(KeyboardFbx,  heroGroup, "OurKeyboard",  new Vector3(-0.04f, 0f, heroDeskZ - 0.22f), deskTopY, 180f, isStatic: true);
        PlaceOnFloor(MouseFbx,     heroGroup, "OurMouse",     new Vector3( 0.38f, 0f, heroDeskZ - 0.22f), deskTopY, 180f, isStatic: true);
        Bounds lampB = PlaceOnFloor(DeskLamp, heroGroup, "OurDeskLamp", new Vector3(-0.78f, 0f, heroDeskZ + 0.20f), deskTopY, 140f, isStatic: true);
        PlaceOnFloor(MugFbx,       heroGroup, "OurCoffeeMug", new Vector3( 0.52f, 0f, heroDeskZ - 0.06f), deskTopY, 210f, isStatic: true);
        PlaceOnFloor(DeskPhone,    heroGroup, "OurDeskPhone", new Vector3(-0.82f, 0f, heroDeskZ - 0.18f), deskTopY, 165f, isStatic: true);
        PlaceOnFloor(PaperStack,   heroGroup, "OurPapers",    new Vector3(-0.50f, 0f, heroDeskZ + 0.08f), deskTopY,  10f, isStatic: true);
        PlaceOnFloor(PenFbx,       heroGroup, "OurPen",       new Vector3(-0.42f, 0f, heroDeskZ - 0.08f), deskTopY,  35f, isStatic: true);
        PlaceOnFloor(BinderAltFbx, heroGroup, "OurBinder",    new Vector3(-0.98f, 0f, heroDeskZ + 0.20f), deskTopY,  85f, isStatic: true);

        // Warm indoor ceiling light fixtures (3x3 grid) + hero overhead & desk lamp lighting
        Transform ceilingLightsGroup = new GameObject("OfficeCeilingLights").transform;
        ceilingLightsGroup.SetParent(root, false);
        float[] lightXs = { -4.2f, 0.0f, 4.2f };
        float[] lightZs = { centerZ - 3.6f, centerZ + 0.2f, centerZ + 4.0f };
        for (int ix = 0; ix < lightXs.Length; ix++)
        {
            for (int iz = 0; iz < lightZs.Length; iz++)
            {
                float lx = lightXs[ix];
                float lz = lightZs[iz];
                float mountY = Mathf.Abs(lx) < 0.1f ? ceilingY - 0.08f : ceilingY;
                GameObject fixture = PlaceCenteredOnCeiling(SquareLamp, ceilingLightsGroup, $"CeilingFixture_{ix}_{iz}",
                    new Vector3(lx, 0f, lz), mountY, 0f, isStatic: true);
                ApplyCeilingFixtureMaterial(fixture);
                CreatePointLight(ceilingLightsGroup, $"CeilingLight_{ix}_{iz}",
                    new Vector3(lx, ceilingY - 0.22f, lz), new Color(1.0f, 0.96f, 0.88f), 2.8f, 14f);
            }
        }
        CreatePointLight(ceilingLightsGroup, "HeroDeskOverheadLight",
            new Vector3(0.0f, 3.35f, heroDeskZ), new Color(1.0f, 0.95f, 0.86f), 3.2f, 12f);

        CreatePointLight(heroGroup, "OurDeskLampLight", new Vector3(-0.62f, lampB.max.y + 0.18f, heroDeskZ + 0.08f), new Color(1.0f, 0.90f, 0.72f), 2.4f, 4.8f);
        CreatePointLight(root, "MorningOfficeFill_Center", new Vector3( 0.0f, 3.15f, centerZ + 1.5f), new Color(1.0f, 0.97f, 0.90f), 1.8f, 18f);
        CreatePointLight(root, "MorningOfficeFill_Left",   new Vector3(-4.5f, 3.10f, centerZ + 2.2f), new Color(0.95f, 0.97f, 1.0f), 1.4f, 14f);
        CreatePointLight(root, "MorningOfficeFill_Right",  new Vector3( 4.5f, 3.10f, centerZ + 2.2f), new Color(1.0f, 0.96f, 0.88f), 1.4f, 14f);

        // Camera wake-up & inspect poses anchored cleanly to our desk, computer & chair
        Transform anchorsRoot = new GameObject("WakeupCameraPoses").transform;
        anchorsRoot.SetParent(root, false);

        Vector3 sleepPos = new Vector3(-0.16f, deskTopY + 0.17f, heroDeskZ - 0.34f);
        Vector3 sleepLook = new Vector3(0.08f, deskTopY + 0.18f, heroDeskZ + 0.16f);
        Quaternion sleepRot = Quaternion.LookRotation((sleepLook - sleepPos).normalized, Vector3.up) * Quaternion.Euler(0f, 0f, 16f);

        Vector3 awakePos = new Vector3(0.0f, deskTopY + 0.50f, heroDeskZ - 0.76f);
        Vector3 finalLookPos = new Vector3(0.0f, deskTopY + 0.24f, heroDeskZ + 0.20f);
        Vector3 pullbackPos = new Vector3(1.55f, 1.95f, heroDeskZ - 2.35f);

        return new NeatAnchors
        {
            sleepPose       = CreateMarker(anchorsRoot, "SleepPose",        sleepPos, sleepRot),
            awakePose       = CreateMarker(anchorsRoot, "AwakePose",        awakePos, Quaternion.LookRotation((finalLookPos - awakePos).normalized, Vector3.up)),
            lookLeft        = CreateMarker(anchorsRoot, "LookLeftTarget",   new Vector3(-4.2f, 1.30f, centerZ + 1.2f), Quaternion.identity),
            lookRight       = CreateMarker(anchorsRoot, "LookRightTarget",  new Vector3( 4.2f, 1.30f, centerZ + 1.2f), Quaternion.identity),
            finalDeskTarget = CreateMarker(anchorsRoot, "FinalDeskTarget",  finalLookPos, Quaternion.identity),
            heroPullback    = CreateMarker(anchorsRoot, "HeroPullbackPose", pullbackPos, Quaternion.LookRotation((finalLookPos - pullbackPos).normalized, Vector3.up))
        };
    }

    private static void BuildTidyWorkstation(Transform parent, string name, Vector3 pos, bool facePositiveZ, bool isBlueChair)
    {
        Transform ws = new GameObject(name).transform;
        ws.SetParent(parent, false);
        float yaw = facePositiveZ ? 180f : 0f;
        float facingSign = facePositiveZ ? 1f : -1f;

        Bounds db = PlaceOnFloor(isBlueChair ? Desk1 : Desk2, ws, "Desk", pos, 0f, yaw, isStatic: true);
        float topY = db.max.y;

        PlaceOnFloor(isBlueChair ? ChairBlue : ChairGrey, ws, "Chair",
            new Vector3(pos.x, 0f, pos.z - facingSign * (db.extents.z + 0.42f)), 0f, yaw, isStatic: true);
        PlaceOnFloor(TowerFbx, ws, "Tower",
            new Vector3(pos.x + 0.78f, 0f, pos.z + facingSign * 0.14f), topY, yaw, isStatic: true);
        PlaceOnFloor(MonitorFbx, ws, "Monitor",
            new Vector3(pos.x, 0f, pos.z + facingSign * 0.18f), topY, yaw, isStatic: true);
        PlaceOnFloor(KeyboardFbx, ws, "Keyboard",
            new Vector3(pos.x - 0.04f, 0f, pos.z - facingSign * 0.22f), topY, yaw, isStatic: true);
        PlaceOnFloor(MouseFbx, ws, "Mouse",
            new Vector3(pos.x + 0.36f, 0f, pos.z - facingSign * 0.22f), topY, yaw, isStatic: true);
        PlaceOnFloor(SquareLamp, ws, "Lamp",
            new Vector3(pos.x - 0.68f, 0f, pos.z + facingSign * 0.18f), topY, yaw - 35f, isStatic: true);
        PlaceOnFloor(BinderFbx, ws, "Binder",
            new Vector3(pos.x + 0.54f, 0f, pos.z + facingSign * 0.14f), topY, yaw, isStatic: true);
        PlaceOnFloor(PaperStack, ws, "Papers",
            new Vector3(pos.x - 0.54f, 0f, pos.z - facingSign * 0.12f), topY, yaw + 12f, isStatic: true);
    }

    private static void PlaceOnShelfBooks(Transform parent, string namePrefix, Bounds bookcaseBounds, float yaw)
    {
        float shelf1Y = bookcaseBounds.min.y + bookcaseBounds.size.y * 0.28f;
        float shelf2Y = bookcaseBounds.min.y + bookcaseBounds.size.y * 0.56f;
        float x = bookcaseBounds.center.x;
        float z = bookcaseBounds.center.z;

        PlaceOnFloor(BooksFbx,    parent, $"{namePrefix}_Row1L", new Vector3(x - 0.25f, 0f, z), shelf1Y, yaw, isStatic: true);
        PlaceOnFloor(BooksAltFbx, parent, $"{namePrefix}_Row1R", new Vector3(x + 0.22f, 0f, z), shelf1Y, yaw, isStatic: true);
        PlaceOnFloor(BooksAltFbx, parent, $"{namePrefix}_Row2L", new Vector3(x - 0.22f, 0f, z), shelf2Y, yaw, isStatic: true);
        PlaceOnFloor(BooksFbx,    parent, $"{namePrefix}_Row2R", new Vector3(x + 0.25f, 0f, z), shelf2Y, yaw, isStatic: true);
    }

    // =========================================================================
    // PROP INSTANTIATION & URP TOON MATERIAL HELPERS
    // =========================================================================
    private static Bounds PlaceOnFloor(string assetPath, Transform parent, string name, Vector3 xzPos, float floorY, float yaw, bool isStatic)
    {
        GameObject go = InstantiateOffice(assetPath, parent, name, new Vector3(xzPos.x, floorY, xzPos.z), new Vector3(0f, yaw, 0f), isStatic);
        if (go == null) return new Bounds(xzPos, Vector3.one);

        Bounds b = MeasureWorldBounds(go);
        go.transform.position += new Vector3(0f, floorY - b.min.y, 0f);
        return MeasureWorldBounds(go);
    }

    private static Bounds PlaceCenteredOnFloor(string assetPath, Transform parent, string name, Vector3 xzCenter, float floorY, float yaw, bool isStatic)
    {
        GameObject go = PlaceAligned(assetPath, parent, name, xzCenter, floorY, alignTop: false,
            new Vector3(0f, yaw, 0f), isStatic, castShadows: true);
        return go != null ? MeasureWorldBounds(go) : new Bounds(xzCenter, Vector3.one);
    }

    private static GameObject PlaceCenteredOnCeiling(string assetPath, Transform parent, string name, Vector3 xzCenter, float ceilingY, float yaw, bool isStatic)
    {
        return PlaceAligned(assetPath, parent, name, xzCenter, ceilingY, alignTop: true,
            new Vector3(180f, yaw, 0f), isStatic, castShadows: false);
    }

    private static GameObject PlaceAligned(
        string assetPath, Transform parent, string name,
        Vector3 xzCenter, float targetY, bool alignTop,
        Vector3 euler, bool isStatic, bool castShadows)
    {
        GameObject go = InstantiateOffice(assetPath, parent, name, new Vector3(xzCenter.x, targetY, xzCenter.z), euler, isStatic);
        if (go == null) return null;

        Bounds b = MeasureWorldBounds(go);
        float yShift = alignTop ? (targetY - b.max.y) : (targetY - b.min.y);
        go.transform.position += new Vector3(xzCenter.x - b.center.x, yShift, xzCenter.z - b.center.z);

        if (!castShadows)
            SetShadowCasting(go, false);
        return go;
    }

    private static void SetShadowCasting(GameObject go, bool enabled)
    {
        if (go == null) return;
        ShadowCastingMode mode = enabled ? ShadowCastingMode.On : ShadowCastingMode.Off;
        foreach (MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer>(true))
            mr.shadowCastingMode = mode;
    }

    private static void ApplyCeilingPanelMaterial(GameObject go)
    {
        ApplyCustomToonMaterial(
            go,
            "EndToon_CeilingPanel",
            new Color(0.95f, 0.94f, 0.91f),
            new Color(0.78f, 0.79f, 0.82f),
            new Color(0.28f, 0.27f, 0.25f, 1f),
            sketchStrength: 0.14f,
            outlineWidth: 0.008f,
            keepSourceMap: false);
    }

    private static void ApplyCeilingFixtureMaterial(GameObject go)
    {
        ApplyCustomToonMaterial(
            go,
            "EndToon_CeilingLight",
            new Color(1.0f, 0.98f, 0.92f),
            new Color(0.86f, 0.85f, 0.78f),
            new Color(0.95f, 0.88f, 0.65f, 1f),
            sketchStrength: 0.05f,
            outlineWidth: 0.012f,
            keepSourceMap: true);
    }

    private static void ApplyCustomToonMaterial(
        GameObject go, string matName,
        Color baseColor, Color shadowColor, Color emissionColor,
        float sketchStrength, float outlineWidth, bool keepSourceMap)
    {
        if (go == null || toonShader == null) return;
        if (!matCache.TryGetValue(matName, out Material mat) || mat == null)
        {
            string path = $"{MaterialFolder}/{matName}.mat";
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(toonShader); AssetDatabase.CreateAsset(mat, path); }
            else mat.shader = toonShader;

            if (keepSourceMap)
            {
                MeshRenderer sampleMr = go.GetComponentInChildren<MeshRenderer>(true);
                Material src = sampleMr != null && sampleMr.sharedMaterials.Length > 0 ? sampleMr.sharedMaterials[0] : null;
                Texture tex = src != null && src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : null;
                if (tex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }
            else if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", null);
            }

            mat.enableInstancing = true;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_ShadowColor")) mat.SetColor("_ShadowColor", shadowColor);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
            if (mat.HasProperty("_SketchMap") && sketchTexture != null) mat.SetTexture("_SketchMap", sketchTexture);
            if (mat.HasProperty("_SketchScale")) mat.SetFloat("_SketchScale", 0.45f);
            if (mat.HasProperty("_SketchStrength")) mat.SetFloat("_SketchStrength", sketchStrength);
            if (mat.HasProperty("_ToonSteps")) mat.SetFloat("_ToonSteps", 3f);
            if (mat.HasProperty("_OutlineColor")) mat.SetColor("_OutlineColor", new Color(0.16f, 0.16f, 0.20f));
            if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", outlineWidth);
            EditorUtility.SetDirty(mat);
            matCache[matName] = mat;
        }

        foreach (MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] shared = mr.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
                shared[i] = mat;
            mr.sharedMaterials = shared;
        }
    }

    private static GameObject InstantiateOffice(string assetPath, Transform parent, string name, Vector3 worldPos, Vector3 euler, bool isStatic)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning($"[EndingLevelBuilder] Missing Office FBX: {assetPath}");
            return null;
        }

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(euler));
        root.transform.localScale = Vector3.one;

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        visual.name = "Model";
        Quaternion nativeRot = asset.transform.localRotation;
        Vector3 nativeScale = asset.transform.localScale;
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = nativeRot;
        visual.transform.localScale = nativeScale;

        foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(c);

        ApplyToonMaterials(visual);

        if (isStatic)
        {
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic;
            SetFlagsRecursive(root, flags);
        }

        return root;
    }

    private static Bounds MeasureAssetBounds(string assetPath)
    {
        GameObject temp = InstantiateOffice(assetPath, null, "__TempMeasure__", Vector3.zero, Vector3.zero, isStatic: false);
        if (temp == null) return new Bounds(Vector3.zero, new Vector3(3f, 0.2f, 3f));
        Bounds b = MeasureWorldBounds(temp);
        UnityEngine.Object.DestroyImmediate(temp);
        return b;
    }

    private static Bounds MeasureWorldBounds(GameObject root)
    {
        Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return new Bounds(root.transform.position, Vector3.one);
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
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
                if (!matCache.TryGetValue(key, out Material m) || m == null)
                {
                    string safe = key.Replace("/", "_").Replace(":", "_").Replace(" ", "_");
                    string path = $"{MaterialFolder}/EndToon_{safe}.mat";
                    m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (m == null) { m = new Material(toonShader); AssetDatabase.CreateAsset(m, path); }
                    else m.shader = toonShader;

                    Texture tex = null;
                    Color col = new Color(0.95f, 0.93f, 0.89f);
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
                    if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", new Color(0.58f, 0.56f, 0.62f));
                    if (m.HasProperty("_SketchMap") && sketchTexture != null) m.SetTexture("_SketchMap", sketchTexture);
                    if (m.HasProperty("_SketchScale")) m.SetFloat("_SketchScale", 0.45f);
                    if (m.HasProperty("_SketchStrength")) m.SetFloat("_SketchStrength", 0.25f);
                    if (m.HasProperty("_ToonSteps")) m.SetFloat("_ToonSteps", 3f);
                    if (m.HasProperty("_OutlineColor")) m.SetColor("_OutlineColor", new Color(0.10f, 0.09f, 0.12f));
                    if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 0.015f);
                    EditorUtility.SetDirty(m);
                    matCache[key] = m;
                }
                shared[i] = m;
            }
            mr.sharedMaterials = shared;
        }
    }

    private static void CreatePointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    private static Transform CreateMarker(Transform parent, string name, Vector3 pos, Quaternion rot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, rot);
        return go.transform;
    }

    private static Transform[] CreateMarkers(Transform parent, string prefix, Vector3[] points)
    {
        Transform[] arr = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
            arr[i] = CreateMarker(parent, $"{prefix}_{i}", points[i], Quaternion.identity);
        return arr;
    }

    private static void AssignTransformArray(SerializedProperty prop, Transform[] arr)
    {
        if (prop == null) return;
        prop.arraySize = arr.Length;
        for (int i = 0; i < arr.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
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
