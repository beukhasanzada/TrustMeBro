using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class Level4Builder
{
    private const string LaserPath = "Assets/Baloglan/Enemy/Ranged/CrystalLaser.prefab";
    private const string RangedSoundPath = "Assets/Baloglan/Enemy/Ranged/CrystalShoot.mp3";
    private const string DesktopCase = "Assets/OFFICE/computers/horizontal_desktop.fbx";
    private const string OpenCabinet = "Assets/OFFICE/file_cabinets/file_cabinet_largest_open.fbx";
    private const string MainframeCarpet = "Assets/OFFICE/carpet_floor/floor_3x3.fbx";

    private static void BuildMainframeArena(Transform gameplay, float floorY, float transferEnd)
    {
        Transform arena = new GameObject("S8_MainframeArena").transform;
        float bridgeLength = DeckLen(3);
        float entryZ = transferEnd + bridgeLength - 0.35f;
        float length = DeckLen(12);
        float endZ = entryZ + length;
        var killZone = gameplay.Find("Level4_KillZone").GetComponent<BoxCollider>();
        Vector3 zoneSize = killZone.size;
        zoneSize.z = Mathf.Max(zoneSize.z, 2f * (endZ + 56f - killZone.transform.position.z));
        killZone.size = zoneSize;
        BuildRunwayStrip(arena, "Mainframe transfer bridge", new Vector3(0f, floorY, transferEnd + bridgeLength * 0.5f - 0.35f), 3);
        BuildOfficePlatform(arena, "Mainframe chamber floor", new Vector3(0f, floorY, entryZ + length * 0.5f),
            cols: 5, rows: 12, theme: WingTheme.Executive);
        DressMainframeFloor(arena.Find("Mainframe chamber floor"));
        CreateCheckpoint(gameplay, "Checkpoint_8_Mainframe", new Vector3(0f, floorY + 0.05f, entryZ + 3.5f), 14f, true);
        CreateFillLight(arena, "Mainframe floor fill", new Vector3(0f, floorY + 7f, entryZ + 16f), 4f, 36f);
        CreateFillLight(arena, "Mainframe body fill", new Vector3(0f, floorY + 13f, endZ - 3f), 8f, 35f);

        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 3; i++)
                InstantiateOfficeFbx(DivFullHBlue, arena, "Chamber rail",
                    new Vector3(side * 12.15f, floorY, entryZ + 7.5f + 8.5f * i), 90f, true, true, DefaultLayer);

            InstantiateOfficeFbx(BookcaseBack, arena, "Archive cover",
                new Vector3(side * 7.7f, floorY, entryZ + 10.3f), side * 28f, true, true, GroundLayer);
            InstantiateOfficeFbx(DivLowBlue, arena, "Cubicle cover",
                new Vector3(side * 8.4f, floorY, entryZ + 20.2f), -side * 31f, true, true, GroundLayer);
            CreateTimeOrb(arena, "Mainframe time cache",
                new Vector3(side * 10.45f, floorY + 1.05f, entryZ + (side < 0 ? 16f : 23.4f)));
        }

        MainframeSign(arena, new Vector3(-5.25f, floorY + 3.25f, entryZ + 1.9f));
        GameObject exit = BuildMainframeExit(arena, new Vector3(0f, floorY, endZ - 3.1f));

        var boss = new GameObject("Mainframe • Office Giant");
        boss.transform.SetParent(arena, false);
        boss.transform.SetPositionAndRotation(new Vector3(0f, floorY, endZ + 4.9f), Quaternion.Euler(0f, 180f, 0f));
        Health health = boss.AddComponent<Health>();
        var healthConfig = new SerializedObject(health);
        healthConfig.FindProperty("maxHealth").floatValue = 1450f;
        healthConfig.ApplyModifiedPropertiesWithoutUndo();
        boss.AddComponent<HealthTimeReward>().secondsReward = 22f;
        var crystal = AssetDatabase.LoadAssetAtPath<GameObject>(RangedEnemyPath);
        if (crystal.TryGetComponent(out EnemyHitImpact crystalImpact))
        {
            var impact = new SerializedObject(boss.AddComponent<EnemyHitImpact>());
            SetMainframeRef(impact, "hitImpactVfx", new SerializedObject(crystalImpact).FindProperty("hitImpactVfx").objectReferenceValue);
            impact.ApplyModifiedPropertiesWithoutUndo();
        }

        var trigger = boss.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = boss.transform.InverseTransformPoint(new Vector3(0f, floorY + 2f, entryZ + 4.6f));
        trigger.size = new Vector3(24.7f, 5f, 6.5f);

        Transform torso = MainframeChild(boss.transform, "Upper body", Vector3.zero);
        Material red = GetOrCreateAccentMaterial("L4_MainframeSignal", new Color(1f, 0.19f, 0.08f), new Color(2.8f, 0.36f, 0.13f));
        Material cyan = GetOrCreateAccentMaterial("L4_MainframeCapacitor", new Color(0.15f, 0.8f, 1f), new Color(0.36f, 2.4f, 3.6f));
        Material glass = GetOrCreateAccentMaterial("L4_MainframeScreen", new Color(0.028f, 0.052f, 0.075f), new Color(0.027f, 0.059f, 0.082f));

        MainframePart(torso, "Waist • executive desk", ConfDeskSq, new Vector3(0f, 0.1f, -0.2f),
            new Vector3(8.5f, 3.1f, 4.7f), Quaternion.Euler(-8f, 0f, 0f));
        MainframePart(torso, "Spine • shelving", BookcaseBack, new Vector3(0f, 5.1f, -0.8f),
            new Vector3(5.0f, 8.4f, 3.0f), Quaternion.identity);
        MainframePart(torso, "Chest • boardroom table", ConfDeskAlt, new Vector3(0f, 6.45f, 1.05f),
            new Vector3(7.9f, 3.7f, 3.5f), Quaternion.Euler(-68f, 0f, 0f));
        MainframePart(torso, "Open core cabinet", OpenCabinet, new Vector3(0f, 4.7f, 2.34f),
            new Vector3(2.4f, 3.2f, 1.65f), Quaternion.Euler(0f, 180f, 0f));
        MainframePanel(torso, "Chest capacitor", new Vector3(0f, 4.9f, 3.23f), new Vector3(0.67f, 1.2f, 0.19f), cyan);
        for (int side = -1; side <= 1; side += 2)
        {
            MainframePart(torso, "Filing cabinet ribs", FileCabLargest, new Vector3(side * 2.64f, 4.0f, 1.75f),
                new Vector3(2.16f, 5.1f, 2.7f), Quaternion.Euler(0f, 176f, side * 15f));
            MainframePart(torso, "Shoulder • computer tower", TowerFbx, new Vector3(side * 4.35f, 8.0f, -.25f),
                new Vector3(2.55f, 3.55f, 2.9f), Quaternion.Euler(0f, side * 16f, side * 23f));
        }
        MainframePart(torso, "Neck • desktop case", DesktopCase, new Vector3(0f, 8.85f, 0f),
            new Vector3(2.85f, 2f, 2.7f), Quaternion.identity);

        Transform head = MainframeChild(torso, "CRT tracking pivot", new Vector3(0f, 11.65f, 0.52f));
        MainframePart(head, "Monitor head", MonitorFbx, Vector3.zero, new Vector3(6.1f, 4.8f, 4.15f), Quaternion.Euler(0f, 180f, 0f));
        MainframePanel(head, "CRT face inset", new Vector3(0f, 0.12f, 2.09f), new Vector3(4.62f, 2.88f, 0.17f), glass);
        MainframePanel(head, "Left raster eye", new Vector3(-1.13f, 0.48f, 2.2f), new Vector3(1.13f, .26f, .12f), red);
        MainframePanel(head, "Right raster eye", new Vector3(1.13f, 0.48f, 2.2f), new Vector3(1.13f, .26f, .12f), red);
        MainframePanel(head, "CRT status line", new Vector3(0f, -0.63f, 2.2f), new Vector3(1.4f, .15f, .12f), red);

        var bodyHit = MainframeChild(torso, "Cabinet hitbox", new Vector3(0f, 4.7f, 1.12f)).gameObject.AddComponent<BoxCollider>();
        bodyHit.gameObject.layer = LayerMask.NameToLayer("Enemy");
        bodyHit.size = new Vector3(7.7f, 8.2f, 4.0f);
        var headHit = MainframeChild(head, "Monitor hitbox", Vector3.zero).gameObject.AddComponent<BoxCollider>();
        headHit.gameObject.layer = bodyHit.gameObject.layer;
        headHit.size = new Vector3(6.1f, 4.8f, 4.4f);

        MinionSpawner spawner = BuildMainframeMinions(arena, floorY, entryZ);
        var slamLight = MainframeChild(arena, "Slam warning light", Vector3.zero).gameObject.AddComponent<Light>();
        slamLight.type = LightType.Point;
        slamLight.range = 10f;
        LineRenderer ring = MainframeChild(arena, "Slam warning ring", Vector3.zero).gameObject.AddComponent<LineRenderer>();
        ring.loop = true;
        ring.useWorldSpace = true;
        ring.sharedMaterial = orbParticleMat;
        ring.enabled = false;
        Transform slamCenter = MainframeChild(arena, "Slam floor center", new Vector3(0f, floorY, entryZ + 15.7f));
        var audio = boss.AddComponent<AudioSource>();
        audio.spatialBlend = 0.88f;
        audio.volume = 0.71f;
        audio.playOnAwake = false;
        audio.minDistance = 7f;
        audio.maxDistance = 65f;

        var bossConfig = new SerializedObject(boss.AddComponent<OfficeGiantBoss>());
        SetMainframeRef(bossConfig, "player", scenePlayerHealth.GetComponent<RigidbodyFpsController>());
        SetMainframeRef(bossConfig, "torso", torso);
        SetMainframeRef(bossConfig, "head", head);
        SetMainframeArray(bossConfig, "hitboxes", bodyHit, headHit);
        SetMainframeRef(bossConfig, "minions", spawner);
        SetMainframeRef(bossConfig, "exit", exit);
        SetMainframeRef(bossConfig, "slamCenter", slamCenter);
        SetMainframeRef(bossConfig, "slamRing", ring);
        SetMainframeRef(bossConfig, "slamLight", slamLight);
        SetMainframeRef(bossConfig, "audioSource", audio);
        SetMainframeRef(bossConfig, "laserPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(LaserPath).GetComponent<EnemyLaserProjectile>());
        SetMainframeRef(bossConfig, "laserSound", AssetDatabase.LoadAssetAtPath<AudioClip>(RangedSoundPath));
        BuildMainframeArm(torso, -1, bossConfig.FindProperty("left"), red, cyan);
        BuildMainframeArm(torso, 1, bossConfig.FindProperty("right"), red, cyan);
        bossConfig.ApplyModifiedPropertiesWithoutUndo();

        bodyHit.enabled = headHit.enabled = slamLight.enabled = false;
        exit.SetActive(false);
    }

    private static void DressMainframeFloor(Transform deck)
    {
        Bounds floor = deck.GetComponent<BoxCollider>().bounds;
        Transform carpet = MainframeChild(deck, "Office carpet deck", Vector3.zero);
        const int columns = 8;
        const int rows = 10;
        float width = floor.size.x / columns;
        float depth = floor.size.z / rows;
        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            Vector3 center = new Vector3(floor.min.x + (column + .5f) * width,
                floor.max.y + .026f, floor.min.z + (row + .5f) * depth);
            var tile = InstantiateOfficeFbx(MainframeCarpet, carpet, "Carpet tile", center, 0f, true, false, DefaultLayer);
            Bounds original = MainframeBounds(tile);
            tile.transform.localScale = new Vector3(width / original.size.x, 1f, depth / original.size.z);
            Bounds fitted = MainframeBounds(tile);
            tile.transform.position += center - new Vector3(fitted.center.x, fitted.max.y, fitted.center.z);
        }
    }

    private static GameObject BuildMainframeExit(Transform arena, Vector3 floor)
    {
        Transform exit = MainframeChild(arena, "Level4_ExitPortal", floor);
        PlaceSimplePrefab(ExitPortalPath, exit, "Mainframe extraction", Vector3.zero, 90f, local: true);
        Transform visual = exit.GetChild(0);
        foreach (Collider col in visual.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
        visual.position += Vector3.up * (floor.y - MainframeBounds(visual.gameObject).min.y);

        var box = exit.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.2f, 0f);
        box.size = new Vector3(3.2f, 2.6f, 2.8f);
        box.isTrigger = true;
        var config = new SerializedObject(exit.gameObject.AddComponent<PortalTrigger>());
        config.FindProperty("targetScene").stringValue = "MainMenu";
        config.ApplyModifiedPropertiesWithoutUndo();
        return exit.gameObject;
    }

    private static MinionSpawner BuildMainframeMinions(Transform arena, float floorY, float entryZ)
    {
        Transform staging = MainframeChild(arena, "Mainframe reinforcement bays", Vector3.zero);
        var points = new Transform[6];
        var warnings = new GameObject[6];
        for (int i = 0; i < points.Length; i++)
        {
            int side = i % 2 == 0 ? -1 : 1;
            float z = entryZ + 8.1f + (i / 2) * 8.1f;
            points[i] = MainframeChild(staging, "Reinforcement spawn", new Vector3(side * 9.65f, floorY, z));
            warnings[i] = MainframeChild(points[i], "Spawn warning", Vector3.zero).gameObject;
            var outline = warnings[i].AddComponent<LineRenderer>();
            outline.useWorldSpace = false;
            outline.sharedMaterial = orbParticleMat;
            outline.loop = true;
            outline.startColor = outline.endColor = new Color(0.25f, 0.94f, 1f);
            outline.widthMultiplier = .105f;
            outline.positionCount = 24;
            for (int j = 0; j < outline.positionCount; j++)
            {
                float angle = j * 2f * Mathf.PI / outline.positionCount;
                outline.SetPosition(j, new Vector3(Mathf.Cos(angle) * 1.05f, .14f, Mathf.Sin(angle) * 1.05f));
            }
            warnings[i].SetActive(false);
        }

        var spawner = staging.gameObject.AddComponent<MinionSpawner>();
        var config = new SerializedObject(spawner);
        SetMainframeRef(config, "minionPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(RangedEnemyPath));
        SetMainframeArray(config, "spawnPoints", points);
        SetMainframeArray(config, "telegraphMarkers", warnings);
        config.FindProperty("aliveCap").intValue = 6;
        config.FindProperty("poolSize").intValue = 6;
        config.FindProperty("minionHealth").floatValue = 65f;
        config.FindProperty("minionSecondsReward").floatValue = 2.5f;
        config.FindProperty("minPlayerDistance").floatValue = 8f;
        config.FindProperty("minSpawnInterval").floatValue = 4.1f;
        config.FindProperty("maxSpawnInterval").floatValue = 5.25f;
        config.FindProperty("telegraphSeconds").floatValue = 0.72f;
        config.ApplyModifiedPropertiesWithoutUndo();
        return spawner;
    }

    private static void BuildMainframeArm(Transform torso, int side, SerializedProperty slot, Material red, Material cyan)
    {
        string prefix = side < 0 ? "Left " : "Right ";
        Transform shoulder = MainframeChild(torso, prefix + "shoulder", new Vector3(side * 4.62f, 7.8f, 0.2f));
        Transform rest = MainframeChild(torso, prefix + "rest pose", new Vector3(side * 7.1f, 3.45f, 5.55f));
        Transform upper = MainframeChild(torso, prefix + "upper arm", Vector3.zero);
        Transform lower = MainframeChild(torso, prefix + "forearm", Vector3.zero);
        Transform fist = MainframeChild(torso, prefix + "fist", new Vector3(side * 7.1f, 3.45f, 5.55f));
        MainframePart(upper, "Upper arm • tipped file cabinet", FileCabLargest, Vector3.zero,
            new Vector3(2.15f, 2.22f, 1f), Quaternion.Euler(0f, 180f, 0f));
        MainframePart(lower, "Forearm • shelving", BookcaseSmall, Vector3.zero,
            new Vector3(2.5f, 2.22f, 1.08f), Quaternion.Euler(0f, 180f, 0f));
        MainframePart(fist, "Fist • desktop case", DesktopCase, Vector3.zero, new Vector3(3.1f, 1.67f, 2.7f), Quaternion.identity);
        MainframePart(fist, "Knuckles • keyboard", KeyboardFbx, new Vector3(0f, .58f, 1.02f), new Vector3(2.6f, .25f, .75f), Quaternion.Euler(14f, 0f, 0f));
        MainframePanel(fist, "Gauntlet light", new Vector3(0f, -.04f, 1.38f), new Vector3(.92f, .3f, .13f), side < 0 ? red : cyan);
        Transform muzzle = MainframeChild(fist, "Laser muzzle", new Vector3(0f, 0f, 1.46f));
        Transform lamp = MainframeChild(fist, "Gauntlet charge light", new Vector3(0f, .62f, 1.63f));
        var charge = lamp.gameObject.AddComponent<Light>();
        charge.type = LightType.Point;
        charge.range = 9.5f;
        charge.enabled = false;

        Vector3 elbow = Vector3.Lerp(shoulder.position, rest.position, .52f) + torso.right * side * 1.25f + Vector3.up * .65f;
        PositionMainframeLink(upper, shoulder.position, elbow);
        PositionMainframeLink(lower, elbow, rest.position);
        slot.FindPropertyRelative("shoulder").objectReferenceValue = shoulder;
        slot.FindPropertyRelative("upper").objectReferenceValue = upper;
        slot.FindPropertyRelative("lower").objectReferenceValue = lower;
        slot.FindPropertyRelative("fist").objectReferenceValue = fist;
        slot.FindPropertyRelative("rest").objectReferenceValue = rest;
        slot.FindPropertyRelative("muzzle").objectReferenceValue = muzzle;
        slot.FindPropertyRelative("chargeLight").objectReferenceValue = charge;
    }

    private static void PositionMainframeLink(Transform link, Vector3 from, Vector3 to)
    {
        Vector3 span = to - from;
        link.SetPositionAndRotation((from + to) * .5f, Quaternion.LookRotation(span));
        link.localScale = new Vector3(1f, 1f, span.magnitude);
    }

    private static void MainframePart(Transform parent, string name, string asset, Vector3 center, Vector3 size, Quaternion rotation)
    {
        GameObject model = InstantiateOfficeFbx(asset, parent, name, parent.TransformPoint(center), 180f, false, false, DefaultLayer);
        model.transform.localRotation = Quaternion.identity;
        Transform transform = model.transform;
        Vector3 dims = MainframeBounds(model).size;
        transform.localScale = new Vector3(size.x / Mathf.Max(dims.x, .001f), size.y / Mathf.Max(dims.y, .001f), size.z / Mathf.Max(dims.z, .001f));
        transform.position += parent.TransformPoint(center) - MainframeBounds(model).center;
        Vector3 pivotOffset = transform.localPosition - center;
        transform.localRotation = rotation;
        transform.localPosition = center + rotation * pivotOffset;
    }

    private static Bounds MainframeBounds(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void MainframePanel(Transform parent, string name, Vector3 center, Vector3 size, Material material)
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = name;
        panel.transform.SetParent(parent, false);
        panel.transform.localPosition = center;
        panel.transform.localScale = size;
        panel.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(panel.GetComponent<Collider>());
    }

    private static void MainframeSign(Transform parent, Vector3 position)
    {
        Transform sign = MainframeChild(parent, "Mainframe instruction panel", position);
        sign.localRotation = Quaternion.Euler(0f, -15f, 0f);
        Material backing = GetOrCreateAccentMaterial("L4_MainframeScreen", new Color(.028f, .052f, .075f), new Color(.027f, .059f, .082f));
        MainframePanel(sign, "Panel frame", Vector3.zero, new Vector3(7.8f, 1.45f, .12f), backing);
        var canvasObject = new GameObject("Mainframe instructions", typeof(RectTransform), typeof(Canvas));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = canvasObject.GetComponent<RectTransform>();
        rect.SetParent(sign, false);
        rect.localScale = Vector3.one * .005f;
        rect.localPosition = new Vector3(0f, 0f, -.07f);
        rect.sizeDelta = new Vector2(1500f, 260f);
        var labelObject = new GameObject("Instructions", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var text = labelObject.GetComponent<Text>();
        text.rectTransform.SetParent(rect, false);
        text.rectTransform.sizeDelta = rect.sizeDelta;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 63;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(.42f, .91f, 1f);
        text.text = "THE MAINFRAME\nDash clear of the amber slam marker";
        text.raycastTarget = false;
    }

    private static Transform MainframeChild(Transform parent, string name, Vector3 position)
    {
        Transform child = new GameObject(name).transform;
        child.SetParent(parent, false);
        child.localPosition = position;
        return child;
    }

    private static void SetMainframeRef(SerializedObject settings, string name, Object reference) =>
        settings.FindProperty(name).objectReferenceValue = reference;

    private static void SetMainframeArray(SerializedObject settings, string name, params Object[] values)
    {
        SerializedProperty array = settings.FindProperty(name);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
