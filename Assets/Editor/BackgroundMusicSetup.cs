using System;
using System.IO;
using ForestJourney;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BackgroundMusicSetup
{
    const string ClipPath = "Assets/Audio/Gameplay/SheGuardsTheKingdom.ogg";

    [MenuItem("Tools/Forest Journey/Install background music")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play mode before installing music.");
        if (!File.Exists(ClipPath))
            throw new FileNotFoundException("Converted music is missing.", ClipPath);

        AssetDatabase.ImportAsset(ClipPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(ClipPath);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = .6f;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        if (!clip || !game)
            throw new InvalidOperationException("Open the gameplay scene before installing music.");

        var existing = game.transform.Find("Background Music");
        var music = existing ? existing.gameObject : new GameObject("Background Music", typeof(AudioSource));
        if (!existing) music.transform.SetParent(game.transform, false);
        var source = music.GetComponent<AudioSource>();
        if (!music.GetComponent<BackgroundMusic>()) music.AddComponent<BackgroundMusic>();
        source.clip = clip;
        source.playOnAwake = true;
        source.loop = true;
        source.ignoreListenerPause = true;
        source.spatialBlend = 0;
        source.volume = .27f;

        var ambience = game.transform.Find("Forest Ambience");
        if (ambience && ambience.TryGetComponent<AudioSource>(out var forest))
            forest.volume = .25f;

        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/BackgroundMusicSetup.txt",
            "Streaming music installed: " + clip.length.ToString("F1") + " seconds; " + DateTime.Now.ToString("O"));
    }
}
