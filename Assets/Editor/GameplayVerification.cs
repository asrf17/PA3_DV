using System;
using System.Collections;
using System.IO;
using System.Linq;
using ForestJourney;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object=UnityEngine.Object;

// Runs against the actual scene and physics in Play Mode; never included in player builds.
public static class GameplayVerification
{
    static IEnumerator suite;
    static double resumeAt;
    static Keyboard keyboard;
    static string report;
    static GameObject fixture;
    static string runtimeErrors;
    [MenuItem("Tools/Forest Journey/Verify gameplay in Play Mode")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play Mode before verification.");
        if(suite!=null)throw new Exception("Verification is already running.");
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        report="Gameplay integration verification — "+DateTime.Now.ToString("O")+"\n";
        runtimeErrors="";Application.logMessageReceived+=Log;
        keyboard=InputSystem.AddDevice<Keyboard>("GameplayTestKeyboard");
        suite=Verify();resumeAt=0;EditorApplication.update+=Tick;
    }
    static void Check(bool pass,string description)
    {
        if(!pass)throw new Exception(description);
        report+="PASS "+description+"\n";File.WriteAllText("Library/GameplayQA.txt",report);
    }
    static void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); }
    static void Log(string message,string stack,LogType type) {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)runtimeErrors+=message+"\n";}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<resumeAt)return;
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Play Mode stopped during verification.");
            if(suite.MoveNext()) {resumeAt=EditorApplication.timeSinceStartup+(suite.Current is float f?f:.05f);return;}
            Check(string.IsNullOrEmpty(runtimeErrors),"No runtime errors or exceptions: "+runtimeErrors);
            report+="ALL CHECKS PASSED\n";Finish();
        }
        catch(Exception e){report+="FAIL "+e+"\n";Finish();Debug.LogException(e);}
    }
    static void Finish()
    {
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        Application.logMessageReceived-=Log;
        if(fixture)Object.Destroy(fixture);
        suite=null;EditorApplication.update-=Tick;
        File.WriteAllText("Library/GameplayQA.txt",report);
        var game=Object.FindAnyObjectByType<GameManager>();if(game)game.ShowMainMenu();
    }
    static IEnumerator Verify()
    {
        var game=Object.FindAnyObjectByType<GameManager>();var player=game.player;
        var pause=game.GetComponent<PauseMenu>();
        var hud=game.hud.GetComponent<CoinCounterUI>();var guide=game.hud.GetComponent<CoinDirectionIndicator>();
        Check(game.State==GameState.MainMenu && Time.timeScale==0 && !player.ControlsEnabled,"Main menu freezes world and controls");
        ScreenCapture.CaptureScreenshot("Library/GameplayMenu.png");yield return .3f;
        Keys(Key.DownArrow);yield return .2f;
        var inputModule=EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        string navigationDetails=" selected="+EventSystem.current.currentSelectedGameObject.name+" move="+inputModule.move.action.ReadValue<Vector2>()+" controls="+inputModule.move.action.controls.Count+" keyboard="+keyboard.downArrowKey.isPressed;
        Keys();yield return .1f;
        Check(EventSystem.current.currentSelectedGameObject.name=="Quit","Keyboard navigation selects Exit in main menu"+navigationDetails);
        Keys(Key.UpArrow);yield return .2f;Keys();yield return .1f;
        Check(EventSystem.current.currentSelectedGameObject==game.playButton,"Keyboard navigation returns to Play");
        Keys(Key.Enter);yield return .2f;Keys();yield return .8f;
        Check(game.State==GameState.Playing && Time.timeScale==1 && player.Grounded,"Play button starts player on terrain");
        Check(game.coins.Collected==0 && hud.counter.text=="Monedas: 0 / 10","Counter starts at zero");
        Check(guide.Target==game.coins.Nearest(player.transform.position) && guide.indicator.activeSelf,"Compass targets nearest available coin");
        ScreenCapture.CaptureScreenshot("Library/GameplayPlaying.png");yield return .3f;
        var start=player.Body.position;var rotation=player.GetComponentInChildren<BallRollingVisual>().transform.rotation;
        Keys(Key.W);yield return .65f;Keys();yield return .15f;
        Check(Vector3.Distance(start,player.Body.position)>1,"W input drives sphere through actual terrain physics");
        Check(Quaternion.Angle(rotation,player.GetComponentInChildren<BallRollingVisual>().transform.rotation)>5,"Ball visibly rolls with travel");
        Keys(Key.Escape);yield return .2f;Keys();yield return .1f;
        var frozen=player.Body.position;yield return .3f;
        Check(game.State==GameState.Paused && Time.timeScale==0 && !player.ControlsEnabled && Vector3.Distance(frozen,player.Body.position)<.001f,"Escape pauses physics and movement");
        ScreenCapture.CaptureScreenshot("Library/GameplayPause.png");yield return .2f;
        Keys(Key.Enter);yield return .2f;Keys();yield return .2f;
        Check(game.State==GameState.Playing && Time.timeScale==1 && player.ControlsEnabled,"Continue button restores time and controls");
        fixture=new GameObject("Temporary Verification Physics");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixture.transform);floor.transform.position=new Vector3(1200,99,1200);floor.transform.localScale=new Vector3(40,2,40);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(fixture.transform);wall.transform.position=new Vector3(1200,103,1208);wall.transform.localScale=new Vector3(15,6,1);
        player.Body.position=new Vector3(1200,101.2f,1200);player.Body.linearVelocity=Vector3.zero;game.followCamera.Snap();Physics.SyncTransforms();yield return .3f;
        Keys(Key.UpArrow);yield return 1.4f;Keys();yield return .2f;
        Check(player.Body.position.z>1203 && player.Body.position.z<1206.7f,"Arrow input moves but sphere cannot pass through a solid wall");
        wall.SetActive(false);
        var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.transform.SetParent(fixture.transform);ramp.transform.position=new Vector3(1200,102,1204);ramp.transform.localScale=new Vector3(12,1,24);ramp.transform.rotation=Quaternion.Euler(-12,0,0);
        player.Body.position=new Vector3(1200,102.2f,1196);player.Body.linearVelocity=Vector3.zero;game.followCamera.Snap();Physics.SyncTransforms();yield return .5f;
        float initialY=player.Body.position.y;Keys(Key.W);yield return .7f;Keys();yield return .15f;
        Check(player.Body.position.y>initialY+.6f,"Sphere climbs a twelve-degree ramp");
        Object.Destroy(fixture);fixture=null;pause.Restart();yield return .4f;
        Check(game.coins.Collected==0 && game.coins.coins.All(c=>c.gameObject.activeSelf),"Restart restores every coin");
        var first=game.coins.Nearest(player.transform.position);
        foreach(var coin in game.coins.coins)
        {
            player.Body.position=coin.transform.position;player.Body.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return .18f;
            Check(coin.Collected && !coin.gameObject.activeSelf,"Trigger collects "+coin.name);
            int count=game.coins.Collected;Check(!coin.TryCollect() && game.coins.Collected==count,"Duplicate trigger does not double-count "+coin.name);
            if(coin==first && !game.coins.Complete)Check(guide.Target!=first,"Compass retargets after pickup");
        }
        yield return .2f;
        Check(game.coins.Complete && hud.counter.text=="Monedas: 10 / 10" && hud.completion.activeSelf,"All coins produce completion message and final counter");
        Check(!guide.indicator.activeSelf && guide.Target==null,"Compass hides when no coins remain");
        ScreenCapture.CaptureScreenshot("Library/GameplayComplete.png");yield return .2f;
        pause.MainMenu();yield return .1f;
        Check(game.State==GameState.MainMenu && Time.timeScale==0 && game.coins.Collected==0,"Return to main menu resets session");
        game.StartGame();yield return .2f;
        player.Body.position=new Vector3(0,-50,0);yield return .2f;
        Check(player.Body.position.y>0,"Falling outside terrain respawns safely");
        pause.Pause();pause.Restart();yield return .2f;
        Check(game.State==GameState.Playing && Time.timeScale==1 && game.coins.Collected==0,"Restart from pause restores playable state");
        game.ShowMainMenu();yield return .1f;
    }
}
