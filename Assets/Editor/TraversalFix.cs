using System;
using System.Collections.Generic;
using System.IO;
using ForestJourney;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TraversalFix
{
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
        var terrain = Object.FindAnyObjectByType<Terrain>();
        var player = Object.FindAnyObjectByType<PlayerController>();
        var root = GameObject.Find("Environment Collision - Terrain Instances");
        if (!terrain || !root) throw new InvalidOperationException("The gameplay terrain and collision proxies are required.");
        var visibleTrees = new HashSet<(int, int)>();
        Vector3 origin = terrain.transform.position, size = terrain.terrainData.size;
        foreach (var tree in terrain.terrainData.treeInstances)
        {
            float x = origin.x + tree.position.x * size.x;
            float z = origin.z + tree.position.z * size.z;
            visibleTrees.Add((Mathf.RoundToInt(x * 100), Mathf.RoundToInt(z * 100)));
        }
        int removed = 0;
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            var child = root.transform.GetChild(i);
            var key = (Mathf.RoundToInt(child.position.x * 100), Mathf.RoundToInt(child.position.z * 100));
            if (visibleTrees.Contains(key)) continue;
            Object.DestroyImmediate(child.gameObject);
            removed++;
        }
        if (player)
        {
            player.jumpHeight = 4.5f;
            player.jumpBufferTime = .2f;
            player.coyoteTime = .16f;
            EditorUtility.SetDirty(player);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Library/TraversalFix.txt", $"Removed {removed} collision proxies with no visible terrain tree.");
    }
}
