using System;
using ForestJourney;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GameplayTPSSetup
{
    [MenuItem("Tools/Forest Journey/Configure TPS controller")]
    public static void Configure()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before configuring the scene.");
        var game=Object.FindAnyObjectByType<GameManager>();
        if(!game||game.gameObject.scene.path!="Assets/Scenes/Gameplay.unity")throw new Exception("Open Gameplay first.");
        var camera=game.followCamera;
        camera.distance=7;camera.height=1.6f;camera.shoulderOffset=.55f;camera.followTime=.08f;
        camera.horizontalSensitivity=.14f;camera.verticalSensitivity=.12f;
        camera.minimumPitch=-25;camera.maximumPitch=65;camera.initialPitch=18;camera.rotationSmoothTime=.035f;
        camera.GetComponent<Camera>().nearClipPlane=.1f;
        game.player.rotationSpeed=540;
        var arrow=game.player.transform.Find("Facing Indicator");
        if(!arrow)
        {
            arrow=new GameObject("Facing Indicator",typeof(MeshFilter),typeof(MeshRenderer)).transform;
            arrow.SetParent(game.player.transform,false);arrow.localPosition=new Vector3(0,1.08f,0);arrow.gameObject.layer=2;
            var mesh=new Mesh {name="TPS facing arrow"};
            mesh.vertices=new[]{new Vector3(0,0,.7f),new Vector3(-.28f,0,-.25f),new Vector3(0,0,-.04f),new Vector3(.28f,0,-.25f)};
            mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();
            AssetDatabase.CreateAsset(mesh,"Assets/Models/Gameplay/FacingArrow.asset");
            arrow.GetComponent<MeshFilter>().sharedMesh=mesh;
            arrow.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Gameplay/Sphere - Brass bands.mat");
        }
        foreach(var label in game.mainMenu.GetComponentsInChildren<Text>(true))
            if(label.name=="Controls")label.text="WASD / FLECHAS · Moverte\nMOUSE · Mirar alrededor\nESC · Pausar y liberar el cursor";
        foreach(var label in game.hud.GetComponentsInChildren<Text>(true))
            if(label.name=="Hints")label.text="WASD / FLECHAS · Moverte    |    MOUSE · Mirar    |    ESC · Pausa";
        PrefabUtility.SaveAsPrefabAsset(game.player.gameObject,"Assets/Prefabs/Gameplay/PlayerSphere.prefab");
        camera.Snap();
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
        GameplaySceneBuilder.Validate();
    }
}
