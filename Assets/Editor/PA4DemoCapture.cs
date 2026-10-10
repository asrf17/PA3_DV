using System;
using System.IO;
using ForestJourney;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Captures an editor-only demonstration. It never changes the saved scene or the player build.
public static class PA4DemoCapture
{
    const string FullFolder = "Library/PA4FantasyVideoFrames";
    const string ContinuationFolder = "Library/PA4FantasyVideoContinuation";
    const float Duration = 205f;
    const float FrameInterval = .125f;
    static double started;
    static double nextFrame;
    static int lastStage;
    static int frame;
    static Keyboard keyboard;
    static GameManager game;
    static PA4FrameGrabber grabber;
    static bool guardianEnabled;
    static bool runInBackground;
    static float lastProgressTime;
    static Vector3 lastProgressPosition;
    static float detourUntil;
    static string folder;
    static float startOffset;

    [MenuItem("Tools/Forest Journey/Capture PA4 demonstration")]
    public static void Run()
    {
        Begin(FullFolder, 0);
    }

    public static void RunContinuation()
    {
        Begin(ContinuationFolder, 90f);
    }

    static void Begin(string targetFolder, float offset)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (keyboard != null) throw new InvalidOperationException("Capture already running.");
        game = Object.FindAnyObjectByType<GameManager>();
        if (!game || !game.guardian) throw new InvalidOperationException("PA4 scene must be open.");
        folder = targetFolder;
        startOffset = offset;
        Directory.CreateDirectory(folder);
        if (Directory.GetFiles(folder, "frame-*.jpg").Length > 0)
            throw new InvalidOperationException("Move the previous video frames before recording again.");
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        runInBackground = Application.runInBackground;
        Application.runInBackground = true;
        keyboard = InputSystem.AddDevice<Keyboard>("PA4VideoKeyboard");
        guardianEnabled = game.guardian.enabled;
        game.ShowMainMenu();
        started = EditorApplication.timeSinceStartup;
        nextFrame = started;
        lastStage = -1;
        frame = 0;
        var captureObject = new GameObject("PA4 temporary frame grabber");
        grabber = captureObject.AddComponent<PA4FrameGrabber>();
        grabber.StartCoroutine(grabber.Capture());
        EditorApplication.update += Tick;
        File.WriteAllText("Library/PA4VideoCapture.txt", "Recording started " + DateTime.Now.ToString("O"));
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || !game) { Finish("Capture interrupted"); return; }
        float elapsed = (float)(EditorApplication.timeSinceStartup - started) + startOffset;
        if (elapsed >= Duration) { Finish("Capture completed"); return; }
        int stage = elapsed < 12 ? 0 : elapsed < 90 ? 1 : elapsed < 140 ? 2
            : elapsed < 155 ? 3 : elapsed < 177 ? 4 : elapsed < 190 ? 5 : 6;
        if (stage != lastStage) { EnterStage(stage); lastStage = stage; }
        if (stage == 1) DriveToCoin(elapsed);
        if (stage == 2 && (game.State != GameState.Playing || game.LivesRemaining <= 1))
        {
            game.StartGame();
            ShowGuardianAgain();
        }
        if (stage == 2 && elapsed > 95 && Mathf.FloorToInt(elapsed - 95) % 12 == 0 &&
            Mathf.FloorToInt(elapsed - 95) != Mathf.FloorToInt(elapsed - 95 - Time.unscaledDeltaTime))
            ShowGuardianAgain();
        if (stage == 4 && game.State == GameState.Playing && elapsed > 157)
        {
            int desired = Mathf.Clamp(Mathf.FloorToInt((elapsed - 157) / 1.6f), 0, game.coins.Total);
            while (game.coins.Collected < desired)
            {
                var relic = game.coins.coins[game.coins.Collected];
                game.player.Body.position = relic.transform.position;
                game.player.Body.linearVelocity = Vector3.zero;
                Physics.SyncTransforms();
                game.followCamera.Snap();
                relic.TryCollect();
            }
        }

    }

    internal static void CaptureFrame()
    {
        if (keyboard == null || EditorApplication.timeSinceStartup < nextFrame) return;
        try
        {
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            string path = Path.Combine(folder, "frame-" + frame.ToString("D5") + ".jpg");
            if (image)
            {
                File.WriteAllBytes(path, image.EncodeToJPG(72));
                Object.DestroyImmediate(image);
            }
            else if (frame > 0)
                File.Copy(Path.Combine(folder, "frame-" + (frame - 1).ToString("D5") + ".jpg"), path);
            else return;
            frame++;
            nextFrame += FrameInterval;
            if (nextFrame < EditorApplication.timeSinceStartup - FrameInterval) nextFrame = EditorApplication.timeSinceStartup;
            float elapsed = (float)(EditorApplication.timeSinceStartup - started) + startOffset;
            if (frame % 80 == 0)
                File.WriteAllText("Library/PA4VideoCapture.txt", "Recording: " + elapsed.ToString("F1") + " s, " + frame + " frames");
        }
        catch (Exception ex)
        {
            Finish("Capture failed: " + ex);
            Debug.LogException(ex);
        }
    }

    static void EnterStage(int stage)
    {
        Keys();
        if (stage == 0) game.ShowMainMenu();
        else if (stage == 1)
        {
            game.guardian.enabled = false;
            game.StartGame();
            lastProgressPosition = game.player.transform.position;
            lastProgressTime = 12;
        }
        else if (stage == 2)
        {
            game.guardian.enabled = true;
            game.StartGame();
            ShowGuardianAgain();
        }
        else if (stage == 3) game.SetState(GameState.Paused);
        else if (stage == 4)
        {
            game.guardian.enabled = false;
            game.StartGame();
        }
        else if (stage == 5)
        {
            if (game.State != GameState.Victory)
                foreach (var coin in game.coins.coins) if (!coin.Collected) coin.TryCollect();
        }
        else if (stage == 6)
        {
            game.startingLives = 1;
            game.StartGame();
            game.GuardianCaughtPlayer();
        }
    }

    static void DriveToCoin(float elapsed)
    {
        if (game.State != GameState.Playing) { Keys(); return; }
        bool visitingSanctuary = elapsed < 24 && game.discoveries && game.discoveries.Total > 0;
        Transform destination = visitingSanctuary
            ? game.discoveries.landmarks[0].transform
            : game.coins.Nearest(game.player.transform.position)?.transform;
        if (!destination) { Keys(); return; }
        Vector3 direction = Vector3.ProjectOnPlane(destination.position - game.player.transform.position, Vector3.up);
        if (visitingSanctuary && direction.magnitude < 5f)
        {
            game.followCamera.SetOrbit(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 13f);
            Keys();
            return;
        }
        if (direction.sqrMagnitude < 2f) { Keys(); return; }
        float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        if (elapsed - lastProgressTime > 3)
        {
            if (Vector3.Distance(game.player.transform.position, lastProgressPosition) < 2f)
                detourUntil = elapsed + 2.2f;
            lastProgressPosition = game.player.transform.position;
            lastProgressTime = elapsed;
        }
        if (elapsed < detourUntil) yaw += 55f;
        game.followCamera.SetOrbit(yaw, 17f);
        Keys(Key.W);
    }

    static void ShowGuardianAgain()
    {
        var terrain = Object.FindAnyObjectByType<Terrain>();
        Vector3 target = game.guardian.transform.position + game.guardian.transform.forward * 9f;
        target.y = terrain.SampleHeight(target) + terrain.transform.position.y + 1.15f;
        game.player.Body.position = target;
        game.player.Body.linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        Vector3 toward = game.guardian.transform.position - target;
        game.followCamera.SetOrbit(Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg, 16f, true);
    }

    static void Keys(params Key[] keys)
    {
        if (keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    }

    static void Finish(string result)
    {
        EditorApplication.update -= Tick;
        if (grabber) { Object.Destroy(grabber.gameObject); grabber = null; }
        if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
        if (game)
        {
            game.startingLives = 3;
            game.guardian.enabled = guardianEnabled;
            game.ShowMainMenu();
        }
        Application.runInBackground = runInBackground;
        File.WriteAllText("Library/PA4VideoCapture.txt", result + " - " + frame + " frames - " + DateTime.Now.ToString("O"));
    }
}

internal sealed class PA4FrameGrabber : MonoBehaviour
{
    internal System.Collections.IEnumerator Capture()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            PA4DemoCapture.CaptureFrame();
        }
    }
}
