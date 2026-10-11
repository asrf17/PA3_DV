using System.IO;
using System.Linq;
using System.Text;
using ForestJourney;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TraversalDiagnostics
{
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
        var game = Object.FindAnyObjectByType<GameManager>();
        var terrain = Object.FindAnyObjectByType<Terrain>();
        Physics.SyncTransforms();
        var output = new StringBuilder();
        Vector3 start = game.player.transform.position;
        Vector3 goal = game.coins.coins[0].transform.position;
        output.AppendLine($"Player {start} coin {goal}");
        for (int i = 0; i <= 24; i++)
        {
            var p = Vector3.Lerp(start, goal, i / 24f);
            p.y = terrain.SampleHeight(p) + terrain.transform.position.y + 1.15f;
            var colliders = Physics.OverlapSphere(p, 1f, ~4, QueryTriggerInteraction.Ignore)
                .Where(c => c && c.gameObject != game.player.gameObject && !(c is TerrainCollider))
                .Select(c => $"{c.name} [{c.GetType().Name}] at {c.transform.position}").Distinct().ToArray();
            output.AppendLine($"{i,2}: {p} slope={terrain.terrainData.GetSteepness((p.x-terrain.transform.position.x)/terrain.terrainData.size.x, (p.z-terrain.transform.position.z)/terrain.terrainData.size.z):0.0} hit={string.Join(" | ", colliders)}");
        }
        var root = GameObject.Find("Environment Collision - Terrain Instances");
        if (root)
        {
            output.AppendLine($"Proxy count {root.transform.childCount} terrain tree count {terrain.terrainData.treeInstanceCount}");
            foreach (Transform child in root.transform)
            {
                if (Vector2.Distance(new Vector2(child.position.x, child.position.z), new Vector2(start.x, start.z)) < 18)
                    output.AppendLine($"Near start: {child.name} at {child.position}");
            }
        }
        File.WriteAllText("Library/TraversalDiagnostics.txt", output.ToString());
    }
}
