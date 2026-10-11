using UnityEngine;
using UnityEngine.UI;

namespace ForestJourney
{
    public sealed class ExpeditionHUD : MonoBehaviour
    {
        public GameManager game;
        public Text timer;
        public Text lives;
        public Image healthFill;
        public Image healthTrail;
        public Text boostStatus;
        public Text stageStatus;
        public Text guardianStatus;
        public Text discoveryStatus;

        GuardianMode lastMode;
        float targetHealth = 1f;
        float trailHoldUntil;

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
            if (!game) return;
            float dt = Time.unscaledDeltaTime;
            if (healthFill)
            {
                healthFill.fillAmount = Mathf.MoveTowards(healthFill.fillAmount, targetHealth, dt * 1.8f);
                healthFill.color = Color.Lerp(new Color(1f, .34f, .27f), new Color(.40f, .85f, .59f),
                    Mathf.Clamp01((healthFill.fillAmount - .2f) / .55f));
            }
            if (healthTrail && Time.unscaledTime >= trailHoldUntil)
                healthTrail.fillAmount = Mathf.MoveTowards(healthTrail.fillAmount, targetHealth, dt * .55f);
            if (boostStatus)
            {
                bool boosted = game.player && game.player.SpeedBoostRemaining > 0;
                boostStatus.gameObject.SetActive(boosted);
                if (boosted) boostStatus.text = $"IMPULSO  x{game.player.SpeedMultiplier:0.0}  ·  {game.player.SpeedBoostRemaining:0}s";
            }
            if (!game.guardianDirector || !guardianStatus) return;
            var mode = game.guardianDirector.AnyChasing ? GuardianMode.Chase : GuardianMode.Patrol;
            if (lastMode != mode)
            {
                lastMode = mode;
                Refresh();
            }
        }

        public void Refresh()
        {
            if (!game) return;
            int seconds = Mathf.CeilToInt(game.TimeRemaining);
            if (timer) timer.text = $"TIEMPO  {seconds / 60:00}:{seconds % 60:00}";
            if (lives) lives.text = $"VIDA  {game.Health}/{game.maxHealth}";
            float nextHealth = game.maxHealth <= 0 ? 0 : (float)game.Health / game.maxHealth;
            if (nextHealth < targetHealth) trailHoldUntil = Time.unscaledTime + .45f;
            targetHealth = nextHealth;
            if (game.State == GameState.MainMenu)
            {
                if (healthFill) healthFill.fillAmount = targetHealth;
                if (healthTrail) healthTrail.fillAmount = targetHealth;
            }
            if (stageStatus && game.coins) stageStatus.text = $"ETAPA  {game.coins.Stage}/3  ·  RELIQUIAS  {game.coins.Collected}/{game.coins.Total}";
            if (discoveryStatus && game.discoveries)
                discoveryStatus.text = game.discoveries.LastDiscovery == null
                    ? $"LUGARES  {game.discoveries.DiscoveredCount}/{game.discoveries.Total}"
                    : $"LUGARES  {game.discoveries.DiscoveredCount}/{game.discoveries.Total}  ·  {game.discoveries.LastDiscovery}";
            if (guardianStatus)
            {
                bool chasing = game.guardianDirector && game.guardianDirector.AnyChasing && game.State == GameState.Playing;
                int count = game.guardianDirector ? game.guardianDirector.ActiveCount : 1;
                guardianStatus.text = chasing ? $"¡GUARDIANES CERCA!  ·  {count}" : $"GUARDIANES  {count}  ·  PATRULLANDO";
                guardianStatus.color = chasing ? new Color(1f, .48f, .3f) : new Color(.92f, .78f, .43f);
            }
        }
    }
}
