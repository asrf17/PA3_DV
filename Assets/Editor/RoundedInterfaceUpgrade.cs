using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ForestJourney;

public static class RoundedInterfaceUpgrade
{
    const string Folder = "Assets/UI/Gameplay";

    [MenuItem("Tools/Forest Journey/Round interface corners")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode before changing the interface.");

        var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        if (!game || !game.mainMenu || !game.pauseMenu || !game.hud || !game.resultMenu)
            throw new InvalidOperationException("The gameplay interface is not loaded in the current scene.");

        var panelSprite = CreateSprite("RoundedPanel", 64, 16, 20);
        var buttonSprite = CreateSprite("RoundedButton", 48, 12, 16);

        RoundPanel(game.mainMenu.transform, "Expedition Panel", panelSprite);
        RoundPanel(game.hud.transform, "Coin Counter", panelSprite);
        RoundPanel(game.hud.transform, "Direction Guide", panelSprite);
        RoundPanel(game.hud.transform, "Expedition Status", panelSprite);
        RoundPanel(game.hud.transform, "Expedition Complete", panelSprite);
        RoundPanel(game.pauseMenu.transform, "Pause Panel", panelSprite);
        RoundPanel(game.resultMenu.transform, "Result Panel", panelSprite);

        int buttons = 0;
        foreach (var root in new[] { game.mainMenu, game.pauseMenu, game.resultMenu })
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var graphic = button.GetComponent<Image>();
                if (!graphic) continue;
                ApplySprite(graphic, buttonSprite);
                buttons++;
            }
        }

        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/RoundedInterfaceUpgrade.txt",
            "Rounded 7 panels and " + buttons + " buttons on " + DateTime.Now.ToString("O"));
    }

    static void RoundPanel(Transform root, string name, Sprite sprite)
    {
        var panel = root.Find(name);
        if (!panel || !panel.TryGetComponent<Image>(out var graphic))
            throw new InvalidOperationException("Missing interface panel: " + name);
        ApplySprite(graphic, sprite);
    }

    static void ApplySprite(Image graphic, Sprite sprite)
    {
        // Image.color, dimensions and Button.targetGraphic stay unchanged.
        graphic.sprite = sprite;
        graphic.type = Image.Type.Sliced;
        graphic.fillCenter = true;
        graphic.preserveAspect = false;
        EditorUtility.SetDirty(graphic);
    }

    static Sprite CreateSprite(string name, int size, float radius, int border)
    {
        Directory.CreateDirectory(Folder);
        string path = Folder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var half = size * .5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x + .5f - half) - (half - radius);
                float dy = Mathf.Abs(y + .5f - half) - (half - radius);
                float outside = Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) +
                                           Mathf.Max(dy, 0) * Mathf.Max(dy, 0));
                float signedDistance = outside + Mathf.Min(Mathf.Max(dx, dy), 0) - radius;
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(.5f - signedDistance));
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(border, border, border, border);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (!sprite) throw new InvalidOperationException("Could not import UI sprite: " + path);
        return sprite;
    }
}
