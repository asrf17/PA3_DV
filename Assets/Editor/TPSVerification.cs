using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForestJourney;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class TPSVerification
{
    static IEnumerator suite;
    static Keyboard keyboard;
    static Mouse mouse;
    static readonly List<Mouse> suspendedMice=new List<Mouse>();
    static GameObject fixture;
    static double next;
    static string report,errors;
    static GameManager game;
    static PlayerController player;
    static CameraController camera;
    static readonly Vector3 testSpawn=new Vector3(1200,101.15f,1200);

    [MenuItem("Tools/Forest Journey/Verify TPS in Play Mode")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying||suite!=null)throw new Exception("Enter Play Mode; only one verification may run.");
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        game=Object.FindAnyObjectByType<GameManager>();player=game.player;camera=game.followCamera;
        report="TPS integration verification — "+DateTime.Now.ToString("O")+"\n";errors="";
        Application.logMessageReceived+=OnLog;
        // Isolate mouse input within this Unity session, never disable an OS device.
        suspendedMice.Clear();
        foreach(var device in InputSystem.devices.OfType<Mouse>().Where(m=>m.enabled).ToArray()) {suspendedMice.Add(device);InputSystem.DisableDevice(device);}
        keyboard=InputSystem.AddDevice<Keyboard>("TPSVerificationKeyboard");mouse=InputSystem.AddDevice<Mouse>("TPSVerificationMouse");
        suite=Verify();next=0;EditorApplication.update+=Tick;
    }
    static void OnLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors+=message+"\n";}
    static void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
    static void Look(float x,float y){InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(x,y)});}
    static void Check(bool pass,string description)
    {
        if(!pass)throw new Exception(description);
        report+="PASS "+description+"\n";File.WriteAllText("Library/TPSVerification.txt",report);
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Play Mode stopped.");
            if(suite.MoveNext()){next=EditorApplication.timeSinceStartup+(suite.Current is float seconds?seconds:.05f);return;}
            Check(errors.Length==0,"No runtime exceptions or errors: "+errors);
            report+="ALL TPS CHECKS PASSED\n";Finish();
        }
        catch(Exception e){report+="FAIL "+e+"\n";Finish();Debug.LogException(e);}
    }
    static void Finish()
    {
        EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;suite=null;
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);
        foreach(var device in suspendedMice)if(device.added)InputSystem.EnableDevice(device);suspendedMice.Clear();
        if(fixture)Object.Destroy(fixture);if(game)game.ShowMainMenu();
        File.WriteAllText("Library/TPSVerification.txt",report);
    }
    static void Place(float yaw)
    {
        player.SetAiming(false);player.Body.position=testSpawn;player.Body.rotation=Quaternion.identity;
        player.Body.linearVelocity=Vector3.zero;player.Body.angularVelocity=Vector3.zero;
        Physics.SyncTransforms();camera.SetOrbit(yaw,18,true);
    }
    static IEnumerator Verify()
    {
        game.StartGame();yield return .5f;
        Check(Cursor.lockState==CursorLockMode.Locked&&!Cursor.visible,"Gameplay locks and hides cursor");
        ScreenCapture.CaptureScreenshot("Library/TPSGameplay.png");yield return .2f;
        fixture=new GameObject("Temporary TPS verification geometry");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixture.transform);floor.transform.position=new Vector3(1200,99,1200);floor.transform.localScale=new Vector3(80,2,80);
        foreach(float yaw in new[]{90f,180f})
        {
            foreach(var key in new[]{Key.W,Key.S,Key.A,Key.D})
            {
                Place(yaw);yield return .25f;
                Vector3 forward=Quaternion.Euler(0,yaw,0)*Vector3.forward,right=Quaternion.Euler(0,yaw,0)*Vector3.right;
                Vector3 expected=key==Key.W?forward:key==Key.S?-forward:key==Key.D?right:-right;
                Vector3 before=player.Body.position;
                Keys(key);yield return .55f;Keys();yield return .1f;
                Vector3 travel=Vector3.ProjectOnPlane(player.Body.position-before,Vector3.up);
                Check(travel.magnitude>1&&Vector3.Dot(travel.normalized,expected)>.96f,$"{key} follows camera-relative direction at yaw {yaw}");
                Check(Vector3.Dot(player.Body.rotation*Vector3.forward,expected)>.97f,$"Player smoothly faces {key} travel at yaw {yaw}");
                Check(Mathf.Abs(Mathf.DeltaAngle(camera.Yaw,yaw))<.1f,"Movement does not rotate orbit camera");
            }
        }
        Place(0);yield return .3f;
        Quaternion facing=player.Body.rotation;
        Look(200,100);yield return .35f;
        Check(Mathf.Abs(Mathf.DeltaAngle(camera.Yaw,28))<.5f&&Mathf.Abs(camera.Pitch-6)<.5f,"Real mouse delta applies independent horizontal and vertical sensitivities");
        Check(Quaternion.Angle(facing,player.Body.rotation)<.5f,"Orbiting at rest does not turn player");
        Look(0,-100000);yield return .3f;
        Check(Mathf.Abs(camera.Pitch-camera.maximumPitch)<.1f,"Downward view clamps at maximum pitch");
        Look(0,100000);yield return .3f;
        Check(Mathf.Abs(camera.Pitch-camera.minimumPitch)<.1f&&Vector3.Dot(camera.transform.up,Vector3.up)>0,"Upward view clamps without flipping camera");
        camera.SetOrbit(90,18,true);player.SetAiming(true);yield return .5f;
        Check(Vector3.Dot(player.Body.rotation*Vector3.forward,Vector3.right)>.98f,"Future aiming hook aligns stationary player with camera yaw");
        Vector3 aimingStart=player.Body.position;Keys(Key.S);yield return .5f;Keys();yield return .1f;
        Check(player.Body.position.x<aimingStart.x-1&&Vector3.Dot(player.Body.rotation*Vector3.forward,Vector3.right)>.98f,"Aiming supports backpedalling without turning away from camera");
        player.SetAiming(false);Quaternion aimFacing=player.Body.rotation;camera.SetOrbit(180,18,true);yield return .3f;
        Check(Quaternion.Angle(aimFacing,player.Body.rotation)<.5f,"Leaving aim restores independent orbit");
        Keys(Key.Escape);yield return .2f;Keys();yield return .1f;
        Check(game.State==GameState.Paused&&Cursor.lockState==CursorLockMode.None&&Cursor.visible,"Escape pauses and releases cursor");
        float pausedYaw=camera.Yaw;Look(500,500);yield return .2f;
        Check(Mathf.Abs(Mathf.DeltaAngle(pausedYaw,camera.Yaw))<.01f,"Mouse does not change view while paused");
        Keys(Key.Escape);yield return .2f;Keys();yield return .15f;
        Check(game.State==GameState.Playing&&Cursor.lockState==CursorLockMode.Locked&&!Cursor.visible,"Escape resume relocks cursor");
        Check(Mathf.Abs(Mathf.DeltaAngle(pausedYaw,camera.Yaw))<.1f,"Resume does not apply stale mouse delta");
        Place(0);yield return .3f;
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(fixture.transform);wall.transform.position=new Vector3(1200,103,1196);wall.transform.localScale=new Vector3(12,8,.5f);Physics.SyncTransforms();yield return .25f;
        Vector3 pivot=player.transform.position+Vector3.up*camera.height;
        Check(Vector3.Distance(camera.transform.position,pivot)<4.3f&&!Physics.CheckSphere(camera.transform.position,.25f,~4,QueryTriggerInteraction.Ignore),"Camera retracts before solid obstruction without clipping");
        wall.SetActive(false);yield return .65f;
        Check(Vector3.Distance(camera.transform.position,player.transform.position+Vector3.up*camera.height)>6.7f,"Camera returns smoothly to configured distance");
        Vector3 stable=camera.transform.position;yield return .3f;
        Check(Vector3.Distance(stable,camera.transform.position)<.01f,"Stationary follow settles without jitter");
        Check(player.Body.interpolation==RigidbodyInterpolation.Interpolate,"Physics interpolation remains enabled");
        game.StartGame();yield return .4f;
        Check(!player.IsAiming&&Mathf.Abs(Mathf.DeltaAngle(camera.Yaw,0))<.1f&&Mathf.Abs(camera.Pitch-camera.initialPitch)<.1f,"Restart resets facing, aim state and orbit");
        ScreenCapture.CaptureScreenshot("Library/TPSGameplay.png");yield return .2f;
        game.ShowMainMenu();yield return .1f;
        Check(Cursor.lockState==CursorLockMode.None&&Cursor.visible,"Main menu leaves cursor free");
    }
}
