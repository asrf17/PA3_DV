using System.Collections.Generic;
using UnityEngine;

namespace ForestJourney
{
    // One new patrol enters the forest after each relic. Each starts away from the player.
    public sealed class GuardianDirector : MonoBehaviour
    {
        public GameManager game;
        public CoinManager coins;
        public ForestGuardian firstGuardian;
        public ForestGuardian guardianPrefab;
        public Terrain terrain;
        [Min(8)] public float safeSpawnRadius = 18f;

        readonly List<ForestGuardian> reinforcements = new List<ForestGuardian>();
        public int ActiveCount => (firstGuardian ? 1 : 0) + reinforcements.Count;
        public bool AnyChasing
        {
            get
            {
                if (firstGuardian && firstGuardian.Mode == GuardianMode.Chase) return true;
                foreach (var guardian in reinforcements)
                    if (guardian && guardian.Mode == GuardianMode.Chase) return true;
                return false;
            }
        }

        void OnEnable()
        {
            if (coins && coins.coins != null)
                foreach (var coin in coins.coins) if (coin) coin.PickedUp += OnRelicCollected;
        }

        void OnDisable()
        {
            if (coins && coins.coins != null)
                foreach (var coin in coins.coins) if (coin) coin.PickedUp -= OnRelicCollected;
        }

        void OnRelicCollected(Vector3 relicPosition)
        {
            if (!game || game.State != GameState.Playing || !guardianPrefab || !terrain) return;
            var nextRelic = coins.Nearest(game.player.transform.position);
            Vector3 spawn = ChooseSpawn(nextRelic ? nextRelic.transform.position : relicPosition);
            var guardian = Instantiate(guardianPrefab, spawn, Quaternion.identity, transform);
            guardian.name = "Guardian " + (ActiveCount + 1).ToString("00");
            guardian.game = game;
            guardian.player = game.player;
            guardian.waypoints = null;
            // Later reinforcements remain threatening, while the player's top speed stays higher.
            guardian.patrolSpeed = 4.2f;
            guardian.chaseSpeed = 10f + coins.Stage * .5f;
            guardian.detectionRange = 24f + coins.Stage * 2f;
            guardian.releaseRange = guardian.detectionRange + 21f;
            guardian.alertDelay = 3f;
            guardian.ResetGuardian();
            reinforcements.Add(guardian);
            game.NotifyThreatChanged();
        }

        Vector3 ChooseSpawn(Vector3 relicPosition)
        {
            Vector3 playerPosition = game.player.transform.position;
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            for (int i = 0; i < 32; i++)
            {
                float angle = (i * 137.508f + coins.Collected * 31f) * Mathf.Deg2Rad;
                float radius = safeSpawnRadius + (i % 4) * 4f;
                Vector3 candidate = relicPosition + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                float u = (candidate.x - origin.x) / size.x;
                float v = (candidate.z - origin.z) / size.z;
                if (u < .02f || u > .98f || v < .02f || v > .98f ||
                    terrain.terrainData.GetSteepness(u, v) > 30f ||
                    Vector3.Distance(candidate, playerPosition) < safeSpawnRadius - 2f) continue;
                candidate.y = terrain.SampleHeight(candidate) + origin.y;
                if (!Physics.CheckCapsule(candidate + Vector3.up * .7f, candidate + Vector3.up * 2.2f,
                    .7f, ~4, QueryTriggerInteraction.Ignore)) return candidate;
            }
            Vector3 fallback = relicPosition + (relicPosition - playerPosition).normalized * safeSpawnRadius;
            fallback.x = Mathf.Clamp(fallback.x, origin.x + 4f, origin.x + size.x - 4f);
            fallback.z = Mathf.Clamp(fallback.z, origin.z + 4f, origin.z + size.z - 4f);
            fallback.y = terrain.SampleHeight(fallback) + origin.y;
            return fallback;
        }

        public void ResetGuardians()
        {
            foreach (var guardian in reinforcements)
            {
                if (!guardian) continue;
                guardian.gameObject.SetActive(false);
                Destroy(guardian.gameObject);
            }
            reinforcements.Clear();
            if (firstGuardian) firstGuardian.ResetGuardian();
        }
    }
}
