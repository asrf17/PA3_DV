using UnityEngine;
using UnityEngine.InputSystem;

namespace ForestJourney
{
    public sealed class PauseMenu : MonoBehaviour
    {
        public GameManager game;
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (game.State == GameState.Playing) Pause();
                else if (game.State == GameState.Paused) Continue();
            }
        }
        public void Pause() { if (game.State == GameState.Playing) game.SetState(GameState.Paused); }
        public void Continue() { if (game.State == GameState.Paused) game.SetState(GameState.Playing); }
        public void Restart() { game.StartGame(); }
        public void MainMenu() { game.ShowMainMenu(); }
        void OnApplicationFocus(bool focus) { if (!focus && game && game.State == GameState.Playing) Pause(); }
    }
}
