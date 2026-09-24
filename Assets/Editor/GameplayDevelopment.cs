using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only command runner: reports stay in ignored Library/, never in a build.
[InitializeOnLoad]
public static class GameplayDevelopment
{
    static double next;
    static GameplayDevelopment() { EditorApplication.update += Tick; EditorApplication.delayCall += Inspect; }
    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        next = EditorApplication.timeSinceStartup + 1;
        const string path = "Library/GameplayCommand.txt";
        if (!File.Exists(path)) return;
        string command = File.ReadAllText(path).Trim(); File.Delete(path);
        try
        {
            if (command == "refresh") AssetDatabase.Refresh();
            else if (command == "inspect") Inspect();
            else if (command == "play") EditorApplication.isPlaying = true;
            else if (command == "stop") EditorApplication.isPlaying = false;
            else if (command == "build" || command == "validate")
            {
                var type = typeof(GameplayDevelopment).Assembly.GetType("GameplaySceneBuilder");
                type.GetMethod(command == "build" ? "Build" : "Validate").Invoke(null, null);
            }
            else if (command == "qa") typeof(GameplayDevelopment).Assembly.GetType("GameplayVerification").GetMethod("Run").Invoke(null, null);
        }
        catch (Exception e) { File.WriteAllText("Library/GameplayFailure.txt", e.ToString()); Debug.LogException(e); }
    }
    public static void Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        string report = "COMPILED " + DateTime.Now.ToString("O") + "\nScene: " + scene.path + " dirty=" + scene.isDirty;
        foreach (var root in scene.GetRootGameObjects()) report += "\nRoot: " + root.name + " " + root.transform.position;
        foreach (var t in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            report += "\nTerrain: " + t.name + " size=" + t.terrainData.size + " trees=" + t.terrainData.treeInstanceCount + " centerHeight=" + t.terrainData.GetInterpolatedHeight(.5f,.5f);
            foreach(var p in t.terrainData.treePrototypes) report += "\nPrototype: "+p.prefab.name+" colliders="+p.prefab.GetComponentsInChildren<Collider>().Length;
        }
        File.WriteAllText("Library/GameplayInspection.txt", report);
    }
}
