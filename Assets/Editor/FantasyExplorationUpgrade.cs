using System;
using System.IO;
using ForestJourney;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Turns the existing forest level into an original fantasy exploration quest.
public static class FantasyExplorationUpgrade
{
    const string ScenePath = "Assets/Scenes/Gameplay.unity";
    const string PrefabPath = "Assets/Prefabs/Gameplay/AncientWaystone.prefab";

    [MenuItem("Tools/Forest Journey/Apply fantasy exploration theme")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before upgrading the scene.");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the open scene first.");
        var scene = EditorSceneManager.GetActiveScene().path == ScenePath
            ? EditorSceneManager.GetActiveScene() : EditorSceneManager.OpenScene(ScenePath);
        var game = Object.FindAnyObjectByType<GameManager>();
        var terrain = Object.FindAnyObjectByType<Terrain>();
        if (!game || !terrain || !game.resultMenu || !game.guardian)
            throw new InvalidOperationException("Apply the existing PA4 scene upgrade first.");

        ThemeMenus(game);
        ThemeRelics();
        CreateLandmarks(game, terrain);
        ThemeHud(game);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/FantasyUpgrade.txt", "Fantasy exploration scene saved " + DateTime.Now.ToString("O"));
    }

    static void ThemeMenus(GameManager game)
    {
        var panel = game.mainMenu.transform.Find("Expedition Panel");
        SetText(panel, "Eyebrow", "ECOS DEL BOSQUE  /  AVENTURA");
        SetText(panel, "Description", "Explora un bosque encantado.\nRecupera 10 reliquias de luz.\nDescubre sus tres santuarios.", 21);
        SetText(panel, "Controls", "WASD / FLECHAS   ·   Moverte\nMOUSE   ·   Mirar      ESC   ·   Pausar");
        var result = game.resultMenu.transform.Find("Result Panel");
        SetText(result, "Result Title", "¡El santuario despierta!");
        var completion = game.hud.transform.Find("Expedition Complete");
        SetText(completion, "Success Title", "¡Las reliquias vuelven a brillar!");
        SetText(completion, "Success Hint", "El santuario ha recuperado su luz.");
    }

    static void ThemeHud(GameManager game)
    {
        var hud = game.hud.transform;
        var counter = hud.Find("Coin Counter");
        SetText(counter, "Collection Label", "LUZ RECUPERADA");
        SetText(counter, "Counter", "Reliquias: 0 / 10");
        var guide = hud.Find("Direction Guide");
        SetText(guide, "Guide Label", "SIGUE LA LUZ");
        var status = hud.Find("Expedition Status") as RectTransform;
        status.sizeDelta = new Vector2(385, 181);
        status.anchoredPosition = new Vector2(-32, -30);
        var timer = status.Find("Timer") as RectTransform;
        var lives = status.Find("Lives") as RectTransform;
        var alert = status.Find("Guardian Alert") as RectTransform;
        timer.sizeDelta = lives.sizeDelta = alert.sizeDelta = new Vector2(345, 35);
        var discovery = status.Find("Discovery Status");
        if (!discovery)
        {
            var element = new GameObject("Discovery Status", typeof(RectTransform), typeof(Text));
            var rect = element.GetComponent<RectTransform>();
            rect.SetParent(status, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -132);
            rect.sizeDelta = new Vector2(345, 30);
            var label = element.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.color = new Color(.56f, .95f, .89f);
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            discovery = rect;
        }
        var presenter = game.hud.GetComponent<ExpeditionHUD>();
        presenter.discoveryStatus = discovery.GetComponent<Text>();
        presenter.discoveryStatus.text = "LUGARES  0/3";
    }

    static void SetText(Transform parent, string child, string value, int size = 0)
    {
        if (!parent) throw new InvalidOperationException("Missing UI parent for " + child);
        var element = parent.Find(child);
        if (!element) throw new InvalidOperationException("Missing UI label " + child);
        var label = element.GetComponent<Text>();
        label.text = value;
        if (size > 0) label.fontSize = size;
    }

    static void ThemeRelics()
    {
        var prefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Gameplay/GoldenCoin.prefab");
        try
        {
            var visual = prefab.transform.Find("Floating Coin Visual");
            if (!visual) throw new InvalidOperationException("Missing coin visual in reusable prefab.");
            if (visual.Find("Aether Crystal")) return;
            var cyan = Material("Relic - Aether glow", new Color(.26f, .92f, .91f), 2.2f);
            var pearl = Material("Relic - Moonstone", new Color(.87f, .94f, .76f), .6f);
            var crystal = Primitive("Aether Crystal", PrimitiveType.Cube, visual,
                new Vector3(0, 0, -.14f), new Vector3(.43f, .79f, .43f), cyan);
            crystal.transform.localRotation = Quaternion.Euler(0, 0, 45);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * .5f;
                var petal = Primitive("Moonstone Rune " + (i + 1), PrimitiveType.Cube, visual,
                    new Vector3(Mathf.Cos(angle) * .72f, Mathf.Sin(angle) * .72f, -.1f),
                    new Vector3(.19f, .19f, .17f), pearl);
                petal.transform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            PrefabUtility.SaveAsPrefabAsset(prefab, "Assets/Prefabs/Gameplay/GoldenCoin.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }

    static void CreateLandmarks(GameManager game, Terrain terrain)
    {
        var journal = game.discoveries ? game.discoveries : game.gameObject.AddComponent<DiscoveryJournal>();
        game.discoveries = journal;
        var root = GameObject.Find("Lost Sanctuaries");
        if (!root) root = new GameObject("Lost Sanctuaries");
        var names = new[] { "Santuario del Alba", "Mirador del Viento", "Ruinas del Eco" };
        var coinIndices = new[] { 0, 4, 8 };
        var landmarks = new ExplorationLandmark[names.Length];
        CreateWaystonePrefab();
        for (int i = 0; i < names.Length; i++)
        {
            var existing = root.transform.Find(names[i]);
            var landmark = existing ? existing.GetComponent<ExplorationLandmark>() : null;
            if (!landmark)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
                instance.name = names[i];
                instance.transform.SetParent(root.transform);
                instance.transform.rotation = Quaternion.Euler(0, i * 75, 0);
                landmark = instance.GetComponent<ExplorationLandmark>();
            }
            Vector3 coin = game.coins.coins[coinIndices[i]].transform.position;
            var preferred = i == 0 ? new Vector2(105, 103) : new Vector2(coin.x + 5, coin.z + 3);
            landmark.transform.position = FindGround(terrain, preferred);
            PrefabUtility.RecordPrefabInstancePropertyModifications(landmark.transform);
            landmark.displayName = names[i];
            landmark.journal = journal;
            PrefabUtility.RecordPrefabInstancePropertyModifications(landmark);
            landmarks[i] = landmark;
        }
        journal.landmarks = landmarks;
        ClearSanctuaryGlades(terrain, landmarks);
    }

    static void ClearSanctuaryGlades(Terrain terrain, ExplorationLandmark[] landmarks)
    {
        var data = terrain.terrainData;
        var trees = data.treeInstances;
        var kept = new System.Collections.Generic.List<TreeInstance>(trees.Length);
        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;
        foreach (var tree in trees)
        {
            Vector2 world = new Vector2(origin.x + tree.position.x * size.x,
                origin.z + tree.position.z * size.z);
            bool inGlade = false;
            foreach (var landmark in landmarks)
            {
                Vector3 center = landmark.transform.position;
                if ((world - new Vector2(center.x, center.z)).sqrMagnitude < 12f * 12f)
                { inGlade = true; break; }
            }
            if (!inGlade) kept.Add(tree);
        }
        if (kept.Count != trees.Length)
        {
            data.SetTreeInstances(kept.ToArray(), true);
            EditorUtility.SetDirty(data);
        }
    }

    static Vector3 FindGround(Terrain terrain, Vector2 preferred)
    {
        for (int ring = 0; ring < 10; ring++)
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2 / 16;
            var candidate = preferred + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring * 2;
            var origin = terrain.transform.position;
            var size = terrain.terrainData.size;
            float u = (candidate.x - origin.x) / size.x, v = (candidate.y - origin.z) / size.z;
            if (u < 0 || u > 1 || v < 0 || v > 1 || terrain.terrainData.GetSteepness(u, v) > 25f) continue;
            int holeX = Mathf.Clamp(Mathf.FloorToInt(u * terrain.terrainData.holesResolution), 0, terrain.terrainData.holesResolution - 1);
            int holeY = Mathf.Clamp(Mathf.FloorToInt(v * terrain.terrainData.holesResolution), 0, terrain.terrainData.holesResolution - 1);
            if (terrain.terrainData.IsHole(holeX, holeY)) continue;
            var point = new Vector3(candidate.x, 0, candidate.y);
            point.y = terrain.SampleHeight(point) + origin.y;
            bool clear = true;
            foreach (var offset in new[] { Vector2.up * 4, Vector2.down * 4, Vector2.left * 4, Vector2.right * 4 })
            {
                Vector2 edge = candidate + offset;
                float eu = (edge.x - origin.x) / size.x, ev = (edge.y - origin.z) / size.z;
                if (eu < 0 || eu > 1 || ev < 0 || ev > 1 ||
                    terrain.terrainData.IsHole(Mathf.Clamp(Mathf.FloorToInt(eu * terrain.terrainData.holesResolution), 0, terrain.terrainData.holesResolution - 1),
                        Mathf.Clamp(Mathf.FloorToInt(ev * terrain.terrainData.holesResolution), 0, terrain.terrainData.holesResolution - 1)) ||
                    Mathf.Abs(terrain.SampleHeight(new Vector3(edge.x, 0, edge.y)) + origin.y - point.y) > 2f)
                { clear = false; break; }
            }
            if (!clear) continue;
            // Decorations have no solid colliders, so they can stand among existing foliage.
            return point;
        }
        throw new InvalidOperationException("No safe location for sanctuary near " + preferred);
    }

    static void CreateWaystonePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
        var stone = Material("Sanctuary - Moonstone", new Color(.63f, .77f, .72f), .15f);
        var trim = Material("Sanctuary - Aged gold", new Color(.83f, .63f, .27f), .5f);
        var glow = Material("Sanctuary - Aether", new Color(.12f, .70f, .74f), 2.2f);
        var root = new GameObject("Ancient Waystone");
        try
        {
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 1.3f, 0);
            trigger.radius = 4.2f;
            var landmark = root.AddComponent<ExplorationLandmark>();
            Primitive("Plinth", PrimitiveType.Cylinder, root.transform, new Vector3(0, .24f, 0), new Vector3(2.5f, .24f, 2.5f), stone);
            Primitive("Altar", PrimitiveType.Cylinder, root.transform, new Vector3(0, .72f, 0), new Vector3(1.5f, .25f, 1.5f), trim);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * .5f + Mathf.PI * .25f;
                float x = Mathf.Cos(a) * 1.75f, z = Mathf.Sin(a) * 1.75f;
                Primitive("Waystone " + (i + 1), PrimitiveType.Cube, root.transform,
                    new Vector3(x, 1.75f, z), new Vector3(.55f, 3f, .55f), stone);
                Primitive("Rune " + (i + 1), PrimitiveType.Cube, root.transform,
                    new Vector3(x, 3.2f, z), new Vector3(.70f, .24f, .70f), trim);
            }
            var orb = Primitive("Aether Heart", PrimitiveType.Sphere, root.transform,
                new Vector3(0, 2.8f, 0), Vector3.one * 1.25f, glow);
            landmark.crystal = orb.GetComponent<Renderer>();
            var beam = Primitive("Guiding Spire", PrimitiveType.Cylinder, root.transform,
                new Vector3(0, 4.35f, 0), new Vector3(.14f, 1.15f, .14f), glow);
            beam.transform.localRotation = Quaternion.Euler(0, 0, 0);
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(.34f, .93f, .93f);
            light.range = 8;
            light.intensity = 1.5f;
            light.shadows = LightShadows.None;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    static Material Material(string name, Color color, float emission)
    {
        string path = "Assets/Materials/Gameplay/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) return existing;
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        material.SetColor("_BaseColor", color);
        if (emission > 0)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position,
        Vector3 scale, Material material)
    {
        var instance = GameObject.CreatePrimitive(type);
        instance.name = name;
        Object.DestroyImmediate(instance.GetComponent<Collider>());
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        instance.transform.localScale = scale;
        instance.GetComponent<Renderer>().sharedMaterial = material;
        return instance;
    }
}
