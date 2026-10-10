using System;
using UnityEngine;

namespace ForestJourney
{
    // Optional discoveries reward looking around without blocking the main relic quest.
    public sealed class DiscoveryJournal : MonoBehaviour
    {
        public ExplorationLandmark[] landmarks;
        public int DiscoveredCount { get; private set; }
        public int Total => landmarks == null ? 0 : landmarks.Length;
        public string LastDiscovery { get; private set; }
        public event Action Changed;

        public void ResetDiscoveries()
        {
            DiscoveredCount = 0;
            LastDiscovery = null;
            if (landmarks != null)
                foreach (var landmark in landmarks)
                    if (landmark) landmark.ResetDiscovery();
            Changed?.Invoke();
        }

        public bool Discover(ExplorationLandmark landmark)
        {
            if (!landmark || landmark.Discovered || landmarks == null ||
                Array.IndexOf(landmarks, landmark) < 0) return false;
            landmark.MarkDiscovered();
            DiscoveredCount++;
            LastDiscovery = landmark.displayName;
            Changed?.Invoke();
            return true;
        }
    }
}
