using System;
using UnityEngine;

namespace ForestJourney
{
    public sealed class CoinManager : MonoBehaviour
    {
        public Coin[] coins;
        [Tooltip("Relics unlocked in successive exploration stages.")]
        public int[] stageSizes = { 3, 4, 5 };
        public int Collected { get; private set; }
        public int Stage { get; private set; }
        public int Total => coins == null ? 0 : coins.Length;
        public bool Complete => Total > 0 && Collected == Total;
        public event Action Changed;
        void Awake() { ResetCoins(); }
        public void ResetCoins()
        {
            Collected = 0;
            Stage = 1;
            if (coins != null) for (int i = 0; i < coins.Length; i++)
            {
                var coin = coins[i];
                if (!coin) continue;
                coin.manager = this;
                coin.ResetCoin();
                coin.gameObject.SetActive(i < StageEnd(1));
            }
            Changed?.Invoke();
        }
        public void RegisterCollection()
        {
            Collected++;
            if (Collected >= StageEnd(Stage) && Collected < Total && Stage < stageSizes.Length)
            {
                Stage++;
                for (int i = 0; i < Total; i++)
                    if (coins[i] && !coins[i].Collected) coins[i].gameObject.SetActive(i < StageEnd(Stage));
            }
            Changed?.Invoke();
        }
        int StageEnd(int stage)
        {
            if (stageSizes == null || stageSizes.Length == 0) return Total;
            int end = 0;
            for (int i = 0; i < Mathf.Min(stage, stageSizes.Length); i++) end += Mathf.Max(0, stageSizes[i]);
            return Mathf.Min(Total, end);
        }
        public Coin Nearest(Vector3 position)
        {
            Coin nearest = null; float distance = float.PositiveInfinity;
            if (coins == null) return null;
            foreach (var coin in coins)
            {
                if (!coin || !coin.gameObject.activeInHierarchy || coin.Collected) continue;
                float d = (coin.transform.position - position).sqrMagnitude;
                if (d < distance) { distance = d; nearest = coin; }
            }
            return nearest;
        }
    }
}
