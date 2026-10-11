using UnityEngine;

namespace ForestJourney
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class SpeedBoostPickup : MonoBehaviour
    {
        public GameManager game;
        public Transform visualRoot;
        [Min(.1f)] public float duration = 8f;
        [Min(1.01f)] public float multiplier = 1.45f;
        float phase;
        Vector3 visualOrigin;

        void Awake() { if (visualRoot) visualOrigin = visualRoot.localPosition; }
        void Reset() { GetComponent<SphereCollider>().isTrigger = true; }
        void Update()
        {
            if (!visualRoot || !game || game.State != GameState.Playing) return;
            phase += Time.deltaTime;
            visualRoot.localPosition = visualOrigin + Vector3.up * Mathf.Sin(phase * 3f) * .16f;
            visualRoot.Rotate(Vector3.up, 55f * Time.deltaTime, Space.Self);
        }
        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>()) TryUse();
        }
        public bool TryUse()
        {
            if (!game || !game.GrantSpeedBoost(duration, multiplier)) return false;
            gameObject.SetActive(false);
            return true;
        }
        public void ResetPickup()
        {
            phase = 0;
            if (visualRoot) visualRoot.localPosition = visualOrigin;
            gameObject.SetActive(true);
        }
    }
}
