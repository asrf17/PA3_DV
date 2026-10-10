using UnityEngine;

namespace ForestJourney
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ExplorationLandmark : MonoBehaviour
    {
        public string displayName;
        public DiscoveryJournal journal;
        public Renderer crystal;
        public bool Discovered { get; private set; }

        void Reset()
        {
            var trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 4f;
        }

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player && player.ControlsEnabled && journal) journal.Discover(this);
        }

        public void ResetDiscovery()
        {
            Discovered = false;
            SetGlow(new Color(.10f, .55f, .60f) * 1.5f);
        }

        public void MarkDiscovered()
        {
            Discovered = true;
            SetGlow(new Color(1f, .72f, .20f) * 2f);
        }

        void SetGlow(Color color)
        {
            if (!crystal) return;
            var properties = new MaterialPropertyBlock();
            crystal.GetPropertyBlock(properties);
            properties.SetColor("_EmissionColor", color);
            crystal.SetPropertyBlock(properties);
        }
    }
}
