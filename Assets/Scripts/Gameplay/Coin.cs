using System;
using UnityEngine;

namespace ForestJourney
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Coin : MonoBehaviour
    {
        public CoinManager manager;
        public bool Collected { get; private set; }
        public event Action<Vector3> PickedUp;
        void Reset() { GetComponent<SphereCollider>().isTrigger = true; }
        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player && player.ControlsEnabled) TryCollect();
        }
        public bool TryCollect()
        {
            if (Collected || !manager) return false;
            Collected = true; manager.RegisterCollection(); PickedUp?.Invoke(transform.position);
            gameObject.SetActive(false); return true;
        }
        public void ResetCoin() { Collected = false; gameObject.SetActive(true); }
    }
}
