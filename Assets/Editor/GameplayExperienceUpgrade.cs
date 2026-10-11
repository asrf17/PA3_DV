using System;
using System.IO;
using ForestJourney;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GameplayExperienceUpgrade
{
    const string ScenePath = "Assets/Scenes/Gameplay.unity";
    const string PickupPath = "Assets/Prefabs/Gameplay/WindImpulse.prefab";
    const string UiFolder = "Assets/UI/Gameplay";

    [MenuItem("Tools/Forest Journey/Improve combat and traversal")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before changing the scene.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var game = Object.FindAnyObjectByType<GameManager>();
        var terrain = Object.FindAnyObjectByType<Terrain>();
        if (!game || !terrain || !game.hud || !game.guardianDirector)
            throw new InvalidOperationException("The current PA4 gameplay scene is required.");

        AddSpeedBoosts(game, terrain);
        ImproveHud(game);
        game.guardian.alertDelay = 2.8f;
        game.guardian.detectionRange = 30f;
        game.guardian.releaseRange = 51f;
        game.guardian.chaseSpeed = 11.1f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(game.guardian);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/ExperienceUpgrade.txt", "Saved " + DateTime.Now.ToString("O"));
    }

    static void AddSpeedBoosts(GameManager game, Terrain terrain)
    {
        CreatePickupPrefab();
        var root = GameObject.Find("Wind Impulses");
        if (!root) root = new GameObject("Wind Impulses");
        var locations = new[] { new Vector2(91, 81), new Vector2(203, 109), new Vector2(83, 162),
            new Vector2(202, 204), new Vector2(107, 244) };
        var boosts = new SpeedBoostPickup[locations.Length];
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPath);
        for (int i = 0; i < locations.Length; i++)
        {
            string name = "Impulso de viento " + (i + 1).ToString("00");
            var item = root.transform.Find(name);
            if (!item)
            {
                var created = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                created.name = name;
                created.transform.SetParent(root.transform);
                item = created.transform;
            }
            item.position = Ground(terrain, locations[i]) + Vector3.up * 1.1f;
            boosts[i] = item.GetComponent<SpeedBoostPickup>();
            boosts[i].game = game;
            PrefabUtility.RecordPrefabInstancePropertyModifications(boosts[i]);
        }
        game.speedBoosts = boosts;
    }

    static void CreatePickupPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PickupPath)) return;
        var cyan = Material("Impulse - cyan glow", new Color(.14f, .81f, 1f), 2.5f);
        var pale = Material("Impulse - pearl", new Color(.78f, .96f, 1f), .5f);
        var root = new GameObject("Wind Impulse");
        try
        {
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.5f;
            var pickup = root.AddComponent<SpeedBoostPickup>();
            var visual = new GameObject("Floating crystal").transform;
            visual.SetParent(root.transform, false);
            pickup.visualRoot = visual;
            Primitive("North crystal", PrimitiveType.Cube, visual, new Vector3(0, .24f, 0),
                new Vector3(.52f, 1.1f, .52f), cyan).transform.localRotation = Quaternion.Euler(0, 45f, 30f);
            Primitive("Pearl core", PrimitiveType.Sphere, visual, new Vector3(0, .2f, 0),
                new Vector3(.37f, .37f, .37f), pale);
            Primitive("Rune base", PrimitiveType.Cylinder, root.transform, new Vector3(0, -.72f, 0),
                new Vector3(1.45f, .1f, 1.45f), cyan);
            var light = root.AddComponent<Light>();
            light.type = LightType.Point; light.range = 4.5f; light.intensity = .8f;
            light.color = new Color(.18f, .82f, 1f); light.shadows = LightShadows.None;
            PrefabUtility.SaveAsPrefabAsset(root, PickupPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void ImproveHud(GameManager game)
    {
        var hud = game.hud.transform;
        var presenter = game.hud.GetComponent<ExpeditionHUD>();
        var status = hud.Find("Expedition Status") as RectTransform;
        var healthPanel = hud.Find("Health Display") as RectTransform;
        if (!healthPanel)
        {
            healthPanel = Box("Health Display", hud);
            var background = healthPanel.gameObject.AddComponent<Image>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiFolder + "/RoundedPanel.png");
            background.type = Image.Type.Sliced;
            background.color = new Color(.025f, .075f, .073f, .82f);
            background.raycastTarget = false;
        }
        healthPanel.anchorMin = healthPanel.anchorMax = new Vector2(.5f, 0);
        healthPanel.pivot = new Vector2(.5f, 0);
        healthPanel.anchoredPosition = new Vector2(0, 24);
        healthPanel.sizeDelta = new Vector2(500, 88);

        var lives = (healthPanel.Find("Lives") ?? status.Find("Lives")) as RectTransform;
        lives.SetParent(healthPanel, false);
        PlaceTopLeft(lives, new Vector2(24, -10), new Vector2(300, 32));
        var track = (healthPanel.Find("Health Track") ?? status.Find("Health Track")) as RectTransform;
        track.SetParent(healthPanel, false);
        PlaceTopLeft(track, new Vector2(24, -51), new Vector2(452, 17));
        var trail = track.Find("Health Trail") as RectTransform;
        if (!trail)
        {
            trail = Box("Health Trail", track);
            var image = trail.gameObject.AddComponent<Image>();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.color = new Color(1f, .38f, .24f);
            image.raycastTarget = false;
        }
        trail.anchorMin = Vector2.zero; trail.anchorMax = Vector2.one;
        trail.offsetMin = trail.offsetMax = Vector2.zero;
        trail.SetAsFirstSibling();
        presenter.healthTrail = trail.GetComponent<Image>();
        var barSprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiFolder + "/RoundedButton.png");
        presenter.healthTrail.sprite = barSprite;
        presenter.healthFill.sprite = barSprite;
        presenter.healthTrail.color = new Color(1f, .38f, .24f);
        presenter.healthTrail.fillAmount = 1f;
        var fillRect = presenter.healthFill.rectTransform;
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        presenter.healthFill.fillAmount = 1f;

        var boost = healthPanel.Find("Boost Status") as RectTransform;
        if (!boost)
        {
            boost = Box("Boost Status", healthPanel);
            var label = boost.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 17; label.alignment = TextAnchor.MiddleRight;
            label.color = new Color(.38f, .9f, 1f); label.raycastTarget = false;
        }
        PlaceTopLeft(boost, new Vector2(268, -11), new Vector2(208, 30));
        presenter.boostStatus = boost.GetComponent<Text>();
        boost.gameObject.SetActive(false);

        status.sizeDelta = new Vector2(385, 181);
        Move(status, "Guardian Alert", -57f);
        Move(status, "Discovery Status", -92f);
        Move(status, "Stage", -130f);

        var flash = hud.Find("Damage Edge") as RectTransform;
        if (!flash)
        {
            flash = Box("Damage Edge", hud);
            flash.anchorMin = Vector2.zero; flash.anchorMax = Vector2.one;
            flash.offsetMin = flash.offsetMax = Vector2.zero;
            var image = flash.gameObject.AddComponent<Image>();
            image.sprite = CreateUiSprite("DamageEdge", 128, true);
            image.color = new Color(1, .12f, .08f, 0);
            image.raycastTarget = false;
        }
        flash.SetAsLastSibling();
        var arrow = hud.Find("Damage Direction") as RectTransform;
        if (!arrow)
        {
            arrow = Box("Damage Direction", hud);
            var image = arrow.gameObject.AddComponent<Image>();
            image.sprite = CreateUiSprite("DamageArrow", 64, false);
            image.color = new Color(1, .28f, .18f, 0);
            image.raycastTarget = false;
        }
        arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(.5f, .5f);
        arrow.sizeDelta = new Vector2(42, 42);
        arrow.anchoredPosition = Vector2.zero;
        arrow.SetAsLastSibling();
        var feedback = game.hud.GetComponent<DamageFeedback>();
        if (!feedback) feedback = game.hud.AddComponent<DamageFeedback>();
        feedback.game = game;
        feedback.edgeFlash = flash.GetComponent<Image>();
        feedback.directionArrow = arrow.GetComponent<Image>();

        var pauseInstructions = game.pauseMenu.transform.Find("Pause Panel/Controls in pause").GetComponent<Text>();
        pauseInstructions.text = "WASD / FLECHAS  ·  Moverte     MOUSE  ·  Mirar\nESPACIO  ·  Saltar     ESC  ·  Pausar / continuar\nBotiquín verde  ·  Vida     Cristal celeste  ·  Velocidad";
        var description = game.mainMenu.transform.Find("Expedition Panel/Description").GetComponent<Text>();
        description.text = "Recupera reliquias por etapas.\nEsquiva guardianes, salta y busca impulsos.\nDescubre los tres santuarios.";
    }

    static Sprite CreateUiSprite(string name, int size, bool vignette)
    {
        Directory.CreateDirectory(UiFolder);
        string path = UiFolder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size * 2f - 1f;
                float v = (y + .5f) / size * 2f - 1f;
                float alpha;
                if (vignette)
                {
                    float edge = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
                    alpha = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.55f, .98f, edge));
                }
                else
                {
                    alpha = v > -.2f && v < .82f && Mathf.Abs(u) < (.82f - v) * .72f ? 1f :
                        v >= -.78f && v <= -.2f && Mathf.Abs(u) < .13f ? 1f : 0f;
                }
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Material Material(string name, Color color, float emission)
    {
        string path = "Assets/Materials/Gameplay/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        material.SetColor("_BaseColor", color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * emission);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static GameObject Primitive(string name, PrimitiveType shape, Transform parent, Vector3 position,
        Vector3 scale, Material material)
    {
        var item = GameObject.CreatePrimitive(shape);
        item.name = name; item.transform.SetParent(parent, false);
        item.transform.localPosition = position; item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(item.GetComponent<Collider>());
        return item;
    }

    static Vector3 Ground(Terrain terrain, Vector2 preferred)
    {
        var origin = terrain.transform.position;
        var size = terrain.terrainData.size;
        for (int ring = 0; ring < 12; ring++) for (int i = 0; i < 16; i++)
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
        throw new InvalidOperationException("No safe terrain near " + preferred);
    }

    static RectTransform Box(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
    static void PlaceTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    static void Move(Transform parent, string name, float y)
    {
        var rect = parent.Find(name) as RectTransform;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
    }
}
