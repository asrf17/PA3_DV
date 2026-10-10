using System;
using System.Collections;
using System.IO;
using System.Linq;
using ForestJourney;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Exercises the actual scene in Play Mode. Results and captures stay outside Git in Library/.
public static class PA4Verification
{
    static IEnumerator suite;
    static double resumeAt;
    static string report;
    static string errors;
    static float originalDuration;
    static int originalLives;
    static float originalDetection;
    static LayerMask originalSight;

    [MenuItem("Tools/Forest Journey/Verify PA4 in Play Mode")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (suite != null) throw new InvalidOperationException("PA4 verification is already running.");
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        report = "PA4 integration verification - " + DateTime.Now.ToString("O") + "\n";
        errors = "";
        Application.logMessageReceived += RecordError;
        suite = Verify();
        resumeAt = 0;
        EditorApplication.update += Tick;
    }

    static void RecordError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors += message + "\n";
    }

    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        report += "PASS " + name + "\n";
        File.WriteAllText("Library/PA4QA.txt", report);
    }

    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < resumeAt) return;
        try
        {
            if (!EditorApplication.isPlaying) throw new Exception("Play Mode stopped during PA4 verification.");
            if (suite.MoveNext())
            {
                resumeAt = EditorApplication.timeSinceStartup + (suite.Current is float seconds ? seconds : .05f);
                return;
            }
            Check(string.IsNullOrEmpty(errors), "No runtime errors or exceptions: " + errors);
            report += "ALL PA4 CHECKS PASSED\n";
            Finish();
        }
        catch (Exception ex)
        {
            report += "FAIL " + ex + "\n";
            Finish();
            Debug.LogException(ex);
        }
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= RecordError;
        suite = null;
        var game = Object.FindAnyObjectByType<GameManager>();
        if (game)
        {
            game.roundDuration = originalDuration;
            game.startingLives = originalLives;
            if (game.guardian)
            {
                game.guardian.detectionRange = originalDetection;
                game.guardian.sightMask = originalSight;
            }
            game.ShowMainMenu();
        }
        File.WriteAllText("Library/PA4QA.txt", report);
    }

    static IEnumerator Verify()
    {
        var game = Object.FindAnyObjectByType<GameManager>();
        var player = game.player;
        var guardian = game.guardian;
        var hud = game.hud.GetComponent<ExpeditionHUD>();
        var feedback = game.GetComponent<CollectionFeedback>();
        var ambience = game.transform.Find("Forest Ambience").GetComponent<AudioSource>();
        originalDuration = game.roundDuration;
        originalLives = game.startingLives;
        originalDetection = guardian.detectionRange;
        originalSight = guardian.sightMask;

        Check(game.State == GameState.MainMenu && !player.ControlsEnabled, "Menu freezes player before starting");
        Check(guardian.waypoints.Length >= 3 && guardian.Mode == GuardianMode.Patrol, "Guardian begins on a multi-point patrol");
        Check(ambience.clip && ambience.loop && ambience.playOnAwake, "Original ambience is assigned and loops");
        Check(feedback.PooledEffectCount >= 3, "Collection effects use a preallocated pool");
        ScreenCapture.CaptureScreenshot("Library/PA4Menu.png");
        yield return .3f;

        game.StartGame();
        Vector3 guardianStart = guardian.transform.position;
        yield return .8f;
        Check(game.State == GameState.Playing && game.LivesRemaining == 3 && game.TimeRemaining < originalDuration,
            "Session starts with three lives and a running countdown");
        Check(Vector3.Distance(guardianStart, guardian.transform.position) > .35f,
            "Guardian moves between waypoints while patrolling");
        Check(hud.timer.text.StartsWith("TIEMPO") && hud.lives.text.Contains("3") && game.resultMenu.activeSelf == false,
            "HUD displays time and lives while result screen stays hidden");
        Check(ambience.isPlaying, "Forest ambience plays during gameplay");
        ScreenCapture.CaptureScreenshot("Library/PA4Playing.png");
        yield return .3f;

        var pause = game.GetComponent<PauseMenu>();
        pause.Pause();
        Vector3 pausedGuardian = guardian.transform.position;
        float pausedTime = game.TimeRemaining;
        yield return .3f;
        Check(game.State == GameState.Paused && Vector3.Distance(pausedGuardian, guardian.transform.position) < .001f
            && Mathf.Abs(pausedTime - game.TimeRemaining) < .001f, "Pause freezes guardian and countdown");
        pause.Continue();

        guardian.sightMask = 0;
        guardian.detectionRange = 100f;
        var terrain = Object.FindAnyObjectByType<Terrain>();
        Vector3 target = guardian.transform.position + guardian.transform.forward * 6.5f;
        target.y = terrain.SampleHeight(target) + terrain.transform.position.y + 1.15f;
        player.Body.position = target;
        player.Body.linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        Vector3 towardGuardian = guardian.transform.position - target;
        game.followCamera.SetOrbit(Mathf.Atan2(towardGuardian.x, towardGuardian.z) * Mathf.Rad2Deg, 16f, true);
        yield return .5f;
        Check(guardian.Mode == GuardianMode.Chase, "Guardian detects and pursues a nearby player");
        ScreenCapture.CaptureScreenshot("Library/PA4Guardian.png");
        yield return .25f;
        yield return 1.5f;
        Check(game.LivesRemaining == 2 && game.State == GameState.Playing,
            "Guardian contact removes one life and respawns player");
        Check(game.InvulnerabilityRemaining > 0, "Respawn grants brief contact protection");
        guardian.detectionRange = originalDetection;
        guardian.sightMask = originalSight;

        game.StartGame();
        int particleCount = feedback.GetComponentsInChildren<ParticleSystem>(true).Length;
        foreach (var coin in game.coins.coins) Check(coin.TryCollect(), "Coin " + coin.name + " can be collected");
        yield return .25f;
        Check(game.State == GameState.Victory && game.resultMenu.activeSelf && !player.ControlsEnabled,
            "Tenth coin triggers a playable victory screen");
        Check(feedback.GetComponentsInChildren<ParticleSystem>(true).Length == particleCount,
            "Ten pickups reuse particle objects without allocating more");
        ScreenCapture.CaptureScreenshot("Library/PA4Victory.png");
        yield return .3f;

        game.retryButton.GetComponent<Button>().onClick.Invoke();
        yield return .15f;
        Check(game.State == GameState.Playing && game.coins.Collected == 0 && game.LivesRemaining == 3,
            "Result retry starts a clean session");

        game.startingLives = 1;
        game.StartGame();
        game.GuardianCaughtPlayer();
        yield return .1f;
        Check(game.State == GameState.Defeat && game.resultMenu.activeSelf && game.resultSummary.text.Contains("guardián"),
            "Last lost life triggers defeat with a reason");
        ScreenCapture.CaptureScreenshot("Library/PA4Defeat.png");
        yield return .3f;

        game.startingLives = originalLives;
        game.roundDuration = .5f;
        game.StartGame();
        yield return .9f;
        Check(game.State == GameState.Defeat && game.resultSummary.text.Contains("tiempo"),
            "Countdown reaching zero triggers time-out defeat");
        game.ShowMainMenu();
        yield return .1f;
        Check(game.State == GameState.MainMenu && game.coins.Collected == 0 && game.resultMenu.activeSelf == false,
            "Return to menu clears coins and result overlay");
    }
}
