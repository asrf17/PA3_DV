using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ForestJourney
{
    public enum GameState { MainMenu, Playing, Paused, Victory, Defeat }
    public sealed class GameManager : MonoBehaviour
    {
        public PlayerController player;
        public CameraController followCamera;
        public CoinManager coins;
        public GameObject mainMenu;
        public GameObject hud;
        public GameObject pauseMenu;
        public GameObject playButton;
        public GameObject continueButton;
        public GameObject resultMenu;
        public GameObject retryButton;
        public UnityEngine.UI.Text resultTitle;
        public UnityEngine.UI.Text resultSummary;
        public ForestGuardian guardian;
        public DiscoveryJournal discoveries;
        [Min(30)] public float roundDuration = 420f;
        [Min(1)] public int startingLives = 3;
        public GameState State { get; private set; }
        public float TimeRemaining { get; private set; }
        public int LivesRemaining { get; private set; }
        public float InvulnerabilityRemaining { get; private set; }
        public event Action Changed;

        void OnEnable() { if (coins) coins.Changed += OnCoinsChanged; }
        void OnDisable() { if (coins) coins.Changed -= OnCoinsChanged; }
        void Start() { ShowMainMenu(); }
        void Update()
        {
            if (State != GameState.Playing) return;
            if (InvulnerabilityRemaining > 0) InvulnerabilityRemaining -= Time.deltaTime;
            int previousSecond = Mathf.CeilToInt(TimeRemaining);
            TimeRemaining = Mathf.Max(0, TimeRemaining - Time.deltaTime);
            if (TimeRemaining <= 0) Finish(false);
            else if (Mathf.CeilToInt(TimeRemaining) != previousSecond) Changed?.Invoke();
        }
        public void StartGame()
        {
            TimeRemaining = roundDuration;
            LivesRemaining = startingLives;
            InvulnerabilityRemaining = 0;
            player.ResetToSpawn(); coins.ResetCoins(); followCamera.Snap();
            if (discoveries) discoveries.ResetDiscoveries();
            if (guardian) guardian.ResetGuardian();
            SetState(GameState.Playing);
        }
        public void ShowMainMenu()
        {
            TimeRemaining = roundDuration;
            LivesRemaining = startingLives;
            InvulnerabilityRemaining = 0;
            player.ResetToSpawn(); coins.ResetCoins(); followCamera.Snap();
            if (discoveries) discoveries.ResetDiscoveries();
            if (guardian) guardian.ResetGuardian();
            SetState(GameState.MainMenu);
        }
        void OnCoinsChanged()
        {
            Changed?.Invoke();
            if (State == GameState.Playing && coins.Complete) Finish(true);
        }
        public void GuardianCaughtPlayer()
        {
            if (State != GameState.Playing || InvulnerabilityRemaining > 0) return;
            LivesRemaining = Mathf.Max(0, LivesRemaining - 1);
            if (LivesRemaining == 0) { Finish(false); return; }
            InvulnerabilityRemaining = 2.5f;
            TimeRemaining = Mathf.Max(1, TimeRemaining - 10f);
            player.ResetToSpawn(); followCamera.Snap();
            if (guardian) guardian.ResetGuardian();
            Changed?.Invoke();
        }
        void Finish(bool victory)
        {
            if (State != GameState.Playing) return;
            if (resultTitle) resultTitle.text = victory ? "¡El santuario despierta!" : "El bosque espera";
            if (resultSummary) resultSummary.text = victory
                ? $"Restauraste {coins.Total} reliquias de luz. Tiempo restante: {Mathf.CeilToInt(TimeRemaining)} s."
                : LivesRemaining == 0 ? "El guardián agotó tus oportunidades." : "Se acabó el tiempo.";
            SetState(victory ? GameState.Victory : GameState.Defeat);
        }
        public void SetState(GameState state)
        {
            State = state;
            bool playing = state == GameState.Playing;
            player.ControlsEnabled = playing;
            Time.timeScale = playing ? 1 : 0;
            AudioListener.pause = !playing;
            mainMenu.SetActive(state == GameState.MainMenu);
            hud.SetActive(state == GameState.Playing || state == GameState.Paused);
            if (pauseMenu) pauseMenu.SetActive(state == GameState.Paused);
            if (resultMenu) resultMenu.SetActive(state == GameState.Victory || state == GameState.Defeat);
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !playing;
            if (!playing) player.SetAiming(false);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(state == GameState.MainMenu ? playButton : state == GameState.Paused ? continueButton : state == GameState.Victory || state == GameState.Defeat ? retryButton : null);
            Changed?.Invoke();
        }
        public void QuitGame()
        {
            Time.timeScale = 1; AudioListener.pause = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void OnDestroy() { Time.timeScale = 1; AudioListener.pause = false; Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }
    }
}
