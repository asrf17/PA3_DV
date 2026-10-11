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
        public GuardianDirector guardianDirector;
        public Medkit[] medkits;
        public DiscoveryJournal discoveries;
        [Min(30)] public float roundDuration = 420f;
        [Min(1)] public int maxHealth = 100;
        [Min(1)] public int guardianDamage = 25;
        public GameState State { get; private set; }
        public float TimeRemaining { get; private set; }
        public int Health { get; private set; }
        public float InvulnerabilityRemaining { get; private set; }
        public event Action Changed;
        public void NotifyThreatChanged() { Changed?.Invoke(); }

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
            Health = maxHealth;
            InvulnerabilityRemaining = 0;
            player.ResetToSpawn(); coins.ResetCoins(); followCamera.Snap();
            if (discoveries) discoveries.ResetDiscoveries();
            ResetThreatsAndAid();
            SetState(GameState.Playing);
        }
        public void ShowMainMenu()
        {
            TimeRemaining = roundDuration;
            Health = maxHealth;
            InvulnerabilityRemaining = 0;
            player.ResetToSpawn(); coins.ResetCoins(); followCamera.Snap();
            if (discoveries) discoveries.ResetDiscoveries();
            ResetThreatsAndAid();
            SetState(GameState.MainMenu);
        }
        void ResetThreatsAndAid()
        {
            if (guardianDirector) guardianDirector.ResetGuardians();
            else if (guardian) guardian.ResetGuardian();
            if (medkits != null) foreach (var medkit in medkits) if (medkit) medkit.ResetMedkit();
        }
        void OnCoinsChanged()
        {
            Changed?.Invoke();
            if (State == GameState.Playing && coins.Complete) Finish(true);
        }
        public void GuardianCaughtPlayer()
        {
            if (State != GameState.Playing || InvulnerabilityRemaining > 0) return;
            Health = Mathf.Max(0, Health - guardianDamage);
            if (Health == 0) { Finish(false); return; }
            InvulnerabilityRemaining = 2.2f;
            Changed?.Invoke();
        }
        public bool Heal(int amount)
        {
            if (State != GameState.Playing || Health >= maxHealth || amount <= 0) return false;
            Health = Mathf.Min(maxHealth, Health + amount);
            Changed?.Invoke();
            return true;
        }
        void Finish(bool victory)
        {
            if (State != GameState.Playing) return;
            if (resultTitle) resultTitle.text = victory ? "¡El santuario despierta!" : "El bosque espera";
            if (resultSummary) resultSummary.text = victory
                ? $"Restauraste {coins.Total} reliquias de luz. Tiempo restante: {Mathf.CeilToInt(TimeRemaining)} s."
                : Health == 0 ? "Los guardianes agotaron tu vida. Vuelve a intentarlo." : "Se acabó el tiempo.";
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
