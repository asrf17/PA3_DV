using UnityEngine;

namespace ForestJourney
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Medkit : MonoBehaviour
    {
        public GameManager game;
        [Min(1)] public int healing = 35;

        void Reset() { GetComponent<SphereCollider>().isTrigger = true; }
        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>()) TryUse();
        }
        public bool TryUse()
        {
            if (!game || !game.Heal(healing)) return false;
            gameObject.SetActive(false);
            return true;
        }
        public void ResetMedkit() { gameObject.SetActive(true); }
    }
}
