using UnityEngine;
using UnityEngine.UI;

namespace ForestJourney
{
    public sealed class ExpeditionHUD : MonoBehaviour
    {
        public GameManager game;
        public Text timer;
        public Text lives;
        public Text guardianStatus;
        public Text discoveryStatus;

        GuardianMode lastMode;

        void OnEnable()
        {
            if (game) game.Changed += Refresh;
            if (game && game.discoveries) game.discoveries.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (game) game.Changed -= Refresh;
            if (game && game.discoveries) game.discoveries.Changed -= Refresh;
        }

        void Update()
        {
            if (!game || !game.guardian || !guardianStatus) return;
            if (lastMode != game.guardian.Mode)
            {
                lastMode = game.guardian.Mode;
                Refresh();
            }
        }

        public void Refresh()
        {
            if (!game) return;
            int seconds = Mathf.CeilToInt(game.TimeRemaining);
            if (timer) timer.text = $"TIEMPO  {seconds / 60:00}:{seconds % 60:00}";
            if (lives) lives.text = $"OPORTUNIDADES  {game.LivesRemaining}";
            if (discoveryStatus && game.discoveries)
                discoveryStatus.text = game.discoveries.LastDiscovery == null
                    ? $"LUGARES  {game.discoveries.DiscoveredCount}/{game.discoveries.Total}"
                    : $"LUGARES  {game.discoveries.DiscoveredCount}/{game.discoveries.Total}  ·  {game.discoveries.LastDiscovery}";
            if (guardianStatus)
            {
                bool chasing = game.guardian && game.guardian.Mode == GuardianMode.Chase && game.State == GameState.Playing;
                guardianStatus.text = chasing ? "¡EL GUARDIÁN TE SIGUE!" : "EL GUARDIÁN PATRULLA";
                guardianStatus.color = chasing ? new Color(1f, .48f, .3f) : new Color(.92f, .78f, .43f);
            }
        }
    }
}
