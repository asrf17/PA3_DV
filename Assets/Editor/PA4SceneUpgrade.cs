using System;
using System.IO;
using ForestJourney;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PA4SceneUpgrade
{
    const string ScenePath = "Assets/Scenes/Gameplay.unity";
    const string PrefabPath = "Assets/Prefabs/Gameplay/ForestGuardian.prefab";
    static readonly Color Ink = new Color(.035f, .075f, .085f, .94f);
    static readonly Color Gold = new Color(1f, .76f, .3f);
    static readonly Color White = new Color(.92f, .96f, .92f);
    static Font font;

    [MenuItem("Tools/Forest Journey/Apply PA4 game upgrade")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before upgrading the scene.");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the scene before upgrading it.");
        var scene = EditorSceneManager.GetActiveScene().path == ScenePath
            ? EditorSceneManager.GetActiveScene() : EditorSceneManager.OpenScene(ScenePath);
        var game = Object.FindAnyObjectByType<GameManager>();
        var terrain = Object.FindAnyObjectByType<Terrain>();
        if (!game || !terrain || !game.hud) throw new InvalidOperationException("Gameplay scene is incomplete.");
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (!game.guardian) CreateGuardian(game, terrain);
        if (!game.hud.GetComponent<ExpeditionHUD>()) CreateStatusPanel(game);
        if (!game.resultMenu) CreateResultPanel(game);
        CreateAmbientSound(game);

        var menuDescription = game.mainMenu.transform.Find("Expedition Panel/Description");
        if (menuDescription)
        {
            var description = menuDescription.GetComponent<Text>();
            description.text = "Explora un bosque encantado.\nRecupera 10 reliquias de luz.\nDescubre sus tres santuarios.";
            description.fontSize = 21;
        }
        var hints = game.hud.transform.Find("Controls/Hints");
        if (hints) hints.GetComponent<Text>().text = "WASD / FLECHAS  ·  Moverte     MOUSE  ·  Mirar     ESC  ·  Pausa";
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/PA4Upgrade.txt", "PA4 scene upgrade saved " + DateTime.Now.ToString("O"));
    }

    static void CreateGuardian(GameManager game, Terrain terrain)
    {
        Directory.CreateDirectory("Assets/Materials/Gameplay");
        Directory.CreateDirectory("Assets/Prefabs/Gameplay");
        Material body = Material("Guardian - Deep teal", new Color(.06f, .22f, .23f), false);
        Material trim = Material("Guardian - Brass", new Color(.96f, .60f, .16f), true);
        Material eyes = Material("Guardian - Warning eyes", new Color(1f, .18f, .08f), true);

        var template = new GameObject("Forest Guardian");
        var capsule = template.AddComponent<CharacterController>();
        capsule.radius = .65f; capsule.height = 2.8f; capsule.center = new Vector3(0, 1.4f, 0);
        capsule.stepOffset = .35f; capsule.slopeLimit = 42f;
        template.AddComponent<ForestGuardian>();
        Primitive("Mantle", PrimitiveType.Capsule, template.transform, new Vector3(0, 1.4f, 0), new Vector3(1.35f, 1.15f, 1.35f), body);
        Primitive("Crown", PrimitiveType.Cylinder, template.transform, new Vector3(0, 2.65f, 0), new Vector3(1.45f, .16f, 1.45f), trim);
        Primitive("Core", PrimitiveType.Sphere, template.transform, new Vector3(0, 1.45f, .66f), new Vector3(.45f, .45f, .2f), trim);
        Primitive("Left Eye", PrimitiveType.Sphere, template.transform, new Vector3(-.29f, 2.12f, .58f), Vector3.one * .21f, eyes);
        Primitive("Right Eye", PrimitiveType.Sphere, template.transform, new Vector3(.29f, 2.12f, .58f), Vector3.one * .21f, eyes);
        PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
        Object.DestroyImmediate(template);

        Vector3 start = FindFree(terrain, new Vector2(118, 116));
        var guardianObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        guardianObject.transform.position = start;
        var guardian = guardianObject.GetComponent<ForestGuardian>();
        var route = new GameObject("Guardian Patrol Route");
        var candidates = new[] { new Vector2(118, 116), new Vector2(132, 126), new Vector2(124, 143), new Vector2(105, 126) };
        var points = new Transform[candidates.Length];
        for (int i = 0; i < points.Length; i++)
        {
            var marker = new GameObject("Waypoint " + (i + 1));
            marker.transform.SetParent(route.transform);
            marker.transform.position = FindFree(terrain, candidates[i]);
            points[i] = marker.transform;
        }
        guardian.game = game; guardian.player = game.player; guardian.waypoints = points;
        game.guardian = guardian;
    }

    static Vector3 FindFree(Terrain terrain, Vector2 preferred)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        for (int ring = 0; ring < 8; ring++)
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16f;
            Vector2 xy = preferred + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring * 2f;
            float u = (xy.x - origin.x) / size.x, v = (xy.y - origin.z) / size.z;
            if (u < 0 || u > 1 || v < 0 || v > 1 || terrain.terrainData.GetSteepness(u, v) > 28f) continue;
            Vector3 point = new Vector3(xy.x, terrain.SampleHeight(new Vector3(xy.x, 0, xy.y)) + origin.y, xy.y);
            if (!Physics.CheckCapsule(point + Vector3.up, point + Vector3.up * 2.1f,
                    .65f, ~4, QueryTriggerInteraction.Ignore)) return point;
        }
        throw new InvalidOperationException("Could not find a clear guardian waypoint near " + preferred);
    }

    static Material Material(string name, Color color, bool glow)
    {
        string path = "Assets/Materials/Gameplay/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) return existing;
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        material.SetColor("_BaseColor", color);
        if (glow)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.4f);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var visual = GameObject.CreatePrimitive(type);
        visual.name = name;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = position;
        visual.transform.localScale = scale;
        visual.GetComponent<Renderer>().sharedMaterial = material;
    }

    static RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var element = new GameObject(name, typeof(RectTransform));
        var rect = element.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    static Text Label(string name, Transform parent, string value, int size, Vector2 position, Vector2 dimensions, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var rect = Box(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, dimensions);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = font; label.fontSize = size; label.text = value; label.color = color;
        label.alignment = alignment; label.raycastTarget = false;
        return label;
    }

    static Button Button(string name, Transform parent, string text, Vector2 position, UnityAction action, bool primary = false)
    {
        var rect = Box(name, parent, new Vector2(.5f, 1), new Vector2(.5f, 1), position, new Vector2(430, 58));
        rect.gameObject.AddComponent<Image>().color = primary ? Gold : new Color(.13f, .23f, .24f, 1f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        Label("Label", rect, text, 22, Vector2.zero, new Vector2(430, 58), primary ? Ink : White, TextAnchor.MiddleCenter);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }

    static void CreateStatusPanel(GameManager game)
    {
        var status = Box("Expedition Status", game.hud.transform, Vector2.one, Vector2.one,
            new Vector2(-32, -30), new Vector2(346, 148));
        status.gameObject.AddComponent<Image>().color = Ink;
        var timer = Label("Timer", status, "TIEMPO  07:00", 26, new Vector2(20, -10), new Vector2(310, 40), White);
        var lives = Label("Lives", status, "OPORTUNIDADES  3", 21, new Vector2(20, -53), new Vector2(310, 35), White);
        var alert = Label("Guardian Alert", status, "EL GUARDIÁN PATRULLA", 17, new Vector2(20, -100), new Vector2(310, 30), Gold);
        var presenter = game.hud.AddComponent<ExpeditionHUD>();
        presenter.game = game; presenter.timer = timer; presenter.lives = lives; presenter.guardianStatus = alert;
    }

    static void CreateResultPanel(GameManager game)
    {
        var canvas = game.hud.transform.parent;
        var overlay = Box("Expedition Result", canvas, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        overlay.anchorMax = Vector2.one;
        overlay.offsetMax = Vector2.zero;
        overlay.gameObject.AddComponent<Image>().color = new Color(.015f, .025f, .03f, .75f);
        var panel = Box("Result Panel", overlay, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(650, 445));
        panel.gameObject.AddComponent<Image>().color = Ink;
        game.resultTitle = Label("Result Title", panel, "¡Expedición completada!", 40,
            new Vector2(34, -35), new Vector2(582, 65), Gold, TextAnchor.MiddleCenter);
        game.resultSummary = Label("Result Summary", panel, "", 22,
            new Vector2(45, -107), new Vector2(560, 65), White, TextAnchor.MiddleCenter);
        game.retryButton = Button("Retry", panel, "Jugar otra vez", new Vector2(0, -191), game.StartGame, true).gameObject;
        Button("Return to Menu", panel, "Volver al menú", new Vector2(0, -267), game.ShowMainMenu);
        Button("Exit", panel, "Salir", new Vector2(0, -343), game.QuitGame);
        game.resultMenu = overlay.gameObject;
        overlay.gameObject.SetActive(false);
    }

    static void CreateAmbientSound(GameManager game)
    {
        const string path = "Assets/Audio/Gameplay/ForestAmbience.wav";
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (!clip) throw new InvalidOperationException("Missing original ambient clip: " + path);
        var existing = game.transform.Find("Forest Ambience");
        var sound = existing ? existing.gameObject : new GameObject("Forest Ambience", typeof(AudioSource));
        if (!existing) sound.transform.SetParent(game.transform, false);
        var source = sound.GetComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = true;
        source.loop = true;
        source.spatialBlend = 0;
        source.volume = .45f;
    }
}
