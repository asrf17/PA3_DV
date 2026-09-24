using System;
using UnityEngine;

namespace ForestJourney
{
    public sealed class CoinManager : MonoBehaviour
    {
        public Coin[] coins;
        public int Collected { get; private set; }
        public int Total => coins == null ? 0 : coins.Length;
        public bool Complete => Total > 0 && Collected == Total;
        public event Action Changed;
        void Awake() { ResetCoins(); }
        public void ResetCoins()
        {
            Collected = 0;
            if (coins != null) foreach (var coin in coins) { coin.manager = this; coin.ResetCoin(); }
            Changed?.Invoke();
        }
        public void RegisterCollection() { Collected++; Changed?.Invoke(); }
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
