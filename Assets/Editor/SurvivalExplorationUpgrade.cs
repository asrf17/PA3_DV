using System;
using System.Collections.Generic;
using System.IO;
using ForestJourney;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SurvivalExplorationUpgrade
{
    const string ScenePath = "Assets/Scenes/Gameplay.unity";
    const string MedkitPath = "Assets/Prefabs/Gameplay/ForestMedkit.prefab";
    const string TunnelPath = "Assets/Prefabs/Gameplay/MoonstonePassage.prefab";
    static readonly Color Panel = new Color(.025f, .075f, .073f, .76f);
    static readonly Color Gold = new Color(.92f, .74f, .39f);
    static readonly Color Pale = new Color(.87f, .94f, .85f);

    [MenuItem("Tools/Forest Journey/Apply survival exploration upgrade")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing the scene.");
        var scene = EditorSceneManager.GetActiveScene().path == ScenePath
            ? EditorSceneManager.GetActiveScene() : EditorSceneManager.OpenScene(ScenePath);
        var game = Object.FindAnyObjectByType<GameManager>();
        var terrain = Object.FindAnyObjectByType<Terrain>();
        if (!game || !terrain || !game.guardian || !game.coins || !game.hud)
            throw new InvalidOperationException("The fantasy gameplay scene must exist first.");

        AddRelics(game, terrain);
        AddGuardianDirector(game, terrain);
        AddMedkits(game, terrain);
        AddPassages(terrain);
        ThemeInterface(game);
        terrain.detailObjectDensity = .55f;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/SurvivalUpgrade.txt", "Survival exploration saved " + DateTime.Now.ToString("O"));
    }

    static void AddRelics(GameManager game, Terrain terrain)
    {
        var list = new List<Coin>(game.coins.coins);
        var parent = list[0].transform.parent;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/GoldenCoin.prefab");
        var destinations = new[] { new Vector2(234, 230), new Vector2(60, 270) };
        for (int i = 0; i < destinations.Length; i++)
        {
            string name = "Coin " + (11 + i).ToString("00");
            var existing = parent.Find(name);
            var coin = existing ? existing.GetComponent<Coin>() : null;
            if (!coin)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = name;
                instance.transform.SetParent(parent);
                coin = instance.GetComponent<Coin>();
            }
            coin.transform.position = FindGround(terrain, destinations[i]) + Vector3.up * 1.7f;
            coin.manager = game.coins;
            if (!list.Contains(coin)) list.Add(coin);
        }
        game.coins.coins = list.ToArray();
        game.coins.stageSizes = new[] { 3, 4, 5 };
    }

    static void AddGuardianDirector(GameManager game, Terrain terrain)
    {
        const string guardianPath = "Assets/Prefabs/Gameplay/ForestGuardian.prefab";
        var template = PrefabUtility.LoadPrefabContents(guardianPath);
        try
        {
            var settings = template.GetComponent<ForestGuardian>();
            settings.patrolSpeed = 4.2f;
            settings.chaseSpeed = 10.6f;
            settings.detectionRange = 27f;
            settings.releaseRange = 48f;
            settings.alertDelay = 5f;
            PrefabUtility.SaveAsPrefabAsset(template, guardianPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(template); }
        var director = game.GetComponent<GuardianDirector>();
        if (!director) director = game.gameObject.AddComponent<GuardianDirector>();
        director.game = game;
        director.coins = game.coins;
        director.firstGuardian = game.guardian;
        director.guardianPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(guardianPath)
            .GetComponent<ForestGuardian>();
        director.terrain = terrain;
        director.safeSpawnRadius = 18f;
        game.guardianDirector = director;
        game.guardian.patrolSpeed = 4.2f;
        game.guardian.chaseSpeed = 10.6f;
        game.guardian.detectionRange = 27f;
        game.guardian.releaseRange = 48f;
        game.guardian.alertDelay = 5f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(game.guardian);
        game.maxHealth = 100;
        game.guardianDamage = 25;
    }

    static void AddMedkits(GameManager game, Terrain terrain)
    {
        CreateMedkitPrefab();
        var root = GameObject.Find("Forest Remedies");
        if (!root) root = new GameObject("Forest Remedies");
        var positions = new[]
        {
            new Vector2(118, 104), new Vector2(44, 94), new Vector2(172, 144),
            new Vector2(48, 204), new Vector2(238, 66), new Vector2(144, 260)
        };
        var kits = new Medkit[positions.Length];
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MedkitPath);
        for (int i = 0; i < positions.Length; i++)
        {
            string name = "Botiquín " + (i + 1).ToString("00");
            var existing = root.transform.Find(name);
            var kit = existing ? existing.GetComponent<Medkit>() : null;
            if (!kit)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = name;
                instance.transform.SetParent(root.transform);
                kit = instance.GetComponent<Medkit>();
            }
            kit.transform.position = FindGround(terrain, positions[i]) + Vector3.up * 1.15f;
            kit.game = game;
            kits[i] = kit;
        }
        game.medkits = kits;
        var clearings = new Vector3[kits.Length];
        for (int i = 0; i < kits.Length; i++) clearings[i] = kits[i].transform.position;
        ClearTrees(terrain, clearings, 6f);
    }

    static void CreateMedkitPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(MedkitPath))
        {
            var existing = PrefabUtility.LoadPrefabContents(MedkitPath);
            try
            {
                if (!existing.transform.Find("Healing plinth"))
                {
                    var aura = Material("Remedy - jade glow", new Color(.17f, .84f, .47f), 1.8f);
                    Primitive("Healing plinth", PrimitiveType.Cylinder, existing.transform,
                        new Vector3(0, -.65f, 0), new Vector3(1.8f, .12f, 1.8f), aura, false);
                    PrefabUtility.SaveAsPrefabAsset(existing, MedkitPath);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(existing); }
            return;
        }
        var stone = Material("Remedy - ceramic", new Color(.76f, .88f, .72f), 0);
        var green = Material("Remedy - jade glow", new Color(.17f, .84f, .47f), 1.8f);
        var root = new GameObject("Forest Medkit");
        try
        {
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true; trigger.radius = 1.5f;
            root.AddComponent<Medkit>();
            Primitive("Ceramic case", PrimitiveType.Cube, root.transform, Vector3.zero,
                new Vector3(1.25f, .85f, .48f), stone, false);
            Primitive("Jade vertical", PrimitiveType.Cube, root.transform, new Vector3(0, 0, -.26f),
                new Vector3(.18f, .61f, .08f), green, false);
            Primitive("Jade horizontal", PrimitiveType.Cube, root.transform, new Vector3(0, 0, -.27f),
                new Vector3(.61f, .18f, .08f), green, false);
            Primitive("Healing plinth", PrimitiveType.Cylinder, root.transform, new Vector3(0, -.65f, 0),
                new Vector3(1.8f, .12f, 1.8f), green, false);
            var light = root.AddComponent<Light>();
            light.type = LightType.Point; light.range = 4f; light.intensity = .65f;
            light.color = new Color(.2f, 1f, .55f); light.shadows = LightShadows.None;
            PrefabUtility.SaveAsPrefabAsset(root, MedkitPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void AddPassages(Terrain terrain)
    {
        CreatePassagePrefab();
        var root = GameObject.Find("Moonstone Passages");
        if (!root) root = new GameObject("Moonstone Passages");
        var points = new[] { new Vector2(118, 139), new Vector2(164, 205), new Vector2(57, 175) };
        var rotations = new[] { 35f, -42f, 73f };
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TunnelPath);
        var centers = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            string name = "Moonstone Passage " + (i + 1).ToString("00");
            var instance = root.transform.Find(name);
            if (!instance)
            {
                var created = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                created.name = name; created.transform.SetParent(root.transform);
                instance = created.transform;
            }
            centers[i] = FindGround(terrain, points[i]);
            instance.SetPositionAndRotation(centers[i], Quaternion.Euler(0, rotations[i], 0));
        }
        ClearTrees(terrain, centers, 12f);
    }

    static void CreatePassagePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(TunnelPath)) return;
        var stone = Material("Passage - slate", new Color(.34f, .48f, .45f), 0);
        var pale = Material("Passage - moonstone", new Color(.69f, .77f, .68f), .2f);
        var glow = Material("Passage - guiding runes", new Color(.22f, .78f, .75f), 1.5f);
        var root = new GameObject("Moonstone Passage");
        try
        {
            for (int i = 0; i < 3; i++)
            {
                float z = -5f + i * 5f;
                Primitive("Left pillar " + i, PrimitiveType.Cube, root.transform,
                    new Vector3(-4f, 2.15f, z), new Vector3(1.25f, 4.3f, 1.3f), stone, true);
                Primitive("Right pillar " + i, PrimitiveType.Cube, root.transform,
                    new Vector3(4f, 2.15f, z), new Vector3(1.25f, 4.3f, 1.3f), stone, true);
                Primitive("Lintel " + i, PrimitiveType.Cube, root.transform,
                    new Vector3(0, 4.4f, z), new Vector3(9.2f, .85f, 1.3f), pale, true);
                Primitive("Rune " + i, PrimitiveType.Cube, root.transform,
                    new Vector3(0, 3.9f, z - .7f), new Vector3(.34f, .34f, .1f), glow, false);
            }
            PrefabUtility.SaveAsPrefabAsset(root, TunnelPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void ThemeInterface(GameManager game)
    {
        var main = game.mainMenu.transform.Find("Expedition Panel") as RectTransform;
        main.sizeDelta = new Vector2(535, 610);
        main.GetComponent<Image>().color = Panel;
        SetText(main, "Description", "Recupera reliquias por etapas.\nEsquiva a los guardianes y busca botiquines.\nDescubre los tres santuarios.", 20);
        main.Find("Controls").gameObject.SetActive(false);
        game.mainMenu.GetComponent<Image>().color = new Color(.01f, .035f, .04f, .16f);

        var hud = game.hud.transform;
        hud.Find("Controls").gameObject.SetActive(false);
        foreach (string name in new[] { "Coin Counter", "Direction Guide", "Expedition Status" })
            hud.Find(name).GetComponent<Image>().color = Panel;
        var counter = hud.Find("Coin Counter");
        SetText(counter, "Collection Label", "RELIQUIAS DE LUZ");
        SetText(counter, "Counter", "Reliquias: 0 / 12", 24);
        var status = hud.Find("Expedition Status") as RectTransform;
        status.sizeDelta = new Vector2(385, 228);
        Move(status, "Guardian Alert", -115f);
        Move(status, "Discovery Status", -149f);
        var lives = status.Find("Lives") as RectTransform;
        lives.anchoredPosition = new Vector2(20, -52);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var barBg = status.Find("Health Track") as RectTransform;
        if (!barBg) barBg = Box("Health Track", status, new Vector2(20, -91), new Vector2(345, 11));
        var track = barBg.GetComponent<Image>();
        if (!track) track = barBg.gameObject.AddComponent<Image>();
        track.color = new Color(.22f, .30f, .28f, .9f);
        var bar = barBg.Find("Health Fill") as RectTransform;
        if (!bar) bar = Box("Health Fill", barBg, Vector2.zero, new Vector2(345, 11));
        bar.anchorMax = Vector2.one; bar.offsetMin = bar.offsetMax = Vector2.zero;
        var fill = bar.GetComponent<Image>();
        if (!fill) fill = bar.gameObject.AddComponent<Image>();
        fill.color = new Color(.40f, .85f, .59f);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
        var stage = status.Find("Stage") as RectTransform;
        if (!stage) stage = Box("Stage", status, new Vector2(20, -184), new Vector2(345, 26));
        var stageText = stage.GetComponent<Text>();
        if (!stageText) stageText = stage.gameObject.AddComponent<Text>();
        stageText.font = font; stageText.fontSize = 16; stageText.color = Gold;
        stageText.alignment = TextAnchor.MiddleLeft; stageText.raycastTarget = false;
        var presenter = game.hud.GetComponent<ExpeditionHUD>();
        presenter.healthFill = fill; presenter.stageStatus = stageText;

        var pause = game.pauseMenu.transform.Find("Pause Panel") as RectTransform;
        pause.sizeDelta = new Vector2(520, 675);
        pause.GetComponent<Image>().color = Panel;
        var hint = pause.Find("Resume Hint") as RectTransform;
        hint.anchoredPosition = new Vector2(44, -613);
        hint.GetComponent<Text>().text = "ESC también permite continuar";
        var instructions = pause.Find("Controls in pause") as RectTransform;
        if (!instructions) instructions = Box("Controls in pause", pause, new Vector2(44, -493), new Vector2(432, 102));
        var help = instructions.GetComponent<Text>();
        if (!help) help = instructions.gameObject.AddComponent<Text>();
        help.font = font; help.fontSize = 17; help.color = Pale;
        help.alignment = TextAnchor.MiddleCenter; help.raycastTarget = false;
        help.text = "WASD / FLECHAS  ·  Moverte     MOUSE  ·  Mirar\nESC  ·  Pausar / continuar\nBotiquines verdes  ·  Recuperar vida";
        game.pauseMenu.GetComponent<Image>().color = new Color(.01f, .035f, .04f, .58f);

        var result = game.resultMenu.transform.Find("Result Panel");
        result.GetComponent<Image>().color = Panel;
        game.resultMenu.GetComponent<Image>().color = new Color(.01f, .035f, .04f, .58f);
        SetText(result, "Result Title", "El bosque recuerda tu viaje", 35);
    }

    static void Move(Transform parent, string child, float y)
    {
        var rect = parent.Find(child) as RectTransform;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
    }

    static void SetText(Transform parent, string child, string value, int fontSize = 0)
    {
        var text = parent.Find(child).GetComponent<Text>();
        text.text = value;
        if (fontSize > 0) text.fontSize = fontSize;
    }

    static RectTransform Box(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    static Material Material(string name, Color color, float emission)
    {
        string path = "Assets/Materials/Gameplay/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        material.SetColor("_BaseColor", color);
        if (emission > 0)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static GameObject Primitive(string name, PrimitiveType type, Transform parent,
        Vector3 position, Vector3 scale, Material material, bool solid)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static Vector3 FindGround(Terrain terrain, Vector2 preferred)
    {
        var origin = terrain.transform.position;
        var size = terrain.terrainData.size;
        for (int ring = 0; ring < 12; ring++)
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2 / 16f;
            var p = preferred + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring * 2f;
            float u = (p.x - origin.x) / size.x, v = (p.y - origin.z) / size.z;
            if (u < .03f || u > .97f || v < .03f || v > .97f || terrain.terrainData.GetSteepness(u, v) > 25f) continue;
            int hx = Mathf.Clamp(Mathf.FloorToInt(u * terrain.terrainData.holesResolution), 0, terrain.terrainData.holesResolution - 1);
            int hy = Mathf.Clamp(Mathf.FloorToInt(v * terrain.terrainData.holesResolution), 0, terrain.terrainData.holesResolution - 1);
            if (terrain.terrainData.IsHole(hx, hy)) continue;
            var point = new Vector3(p.x, 0, p.y);
            point.y = terrain.SampleHeight(point) + origin.y;
            return point;
        }
        throw new InvalidOperationException("No suitable ground near " + preferred);
    }

    static void ClearTrees(Terrain terrain, Vector3[] centers, float radius)
    {
        var data = terrain.terrainData;
        var kept = new List<TreeInstance>();
        Vector3 origin = terrain.transform.position, size = data.size;
        foreach (var tree in data.treeInstances)
        {
            Vector2 p = new Vector2(origin.x + tree.position.x * size.x, origin.z + tree.position.z * size.z);
            bool close = false;
            foreach (var center in centers)
                if ((p - new Vector2(center.x, center.z)).sqrMagnitude < radius * radius) { close = true; break; }
            if (!close) kept.Add(tree);
        }
        data.SetTreeInstances(kept.ToArray(), true);
        EditorUtility.SetDirty(data);
    }
}
