using UnityEngine;
using UnityEngine.EventSystems;

namespace ForestJourney
{
    public enum GameState { MainMenu, Playing, Paused }
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
        public GameState State { get; private set; }
        void Start() { ShowMainMenu(); }
        public void StartGame()
        {
            player.ResetToSpawn(); coins.ResetCoins(); followCamera.Snap(); SetState(GameState.Playing);
        }
        public void ShowMainMenu()
        {
            player.ResetToSpawn(); coins.ResetCoins(); followCamera.Snap(); SetState(GameState.MainMenu);
        }
        public void SetState(GameState state)
        {
            State = state;
            bool playing = state == GameState.Playing;
            player.ControlsEnabled = playing;
            Time.timeScale = playing ? 1 : 0;
            AudioListener.pause = !playing;
            mainMenu.SetActive(state == GameState.MainMenu);
            hud.SetActive(state != GameState.MainMenu);
            if (pauseMenu) pauseMenu.SetActive(state == GameState.Paused);
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !playing;
            if (!playing) player.SetAiming(false);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(state == GameState.MainMenu ? playButton : state == GameState.Paused ? continueButton : null);
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
