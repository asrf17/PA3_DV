using UnityEngine;
using UnityEngine.UI;

namespace ForestJourney
{
    public sealed class CoinCounterUI : MonoBehaviour
    {
        public CoinManager manager;
        public Text counter;
        public GameObject completion;
        public Image progress;
        void OnEnable() { manager.Changed += Refresh; Refresh(); }
        void OnDisable() { if (manager) manager.Changed -= Refresh; }
        void Refresh()
        {
            counter.text = $"Monedas: {manager.Collected} / {manager.Total}";
            if (progress) progress.fillAmount = manager.Total == 0 ? 0 : (float)manager.Collected / manager.Total;
            if (completion) completion.SetActive(manager.Complete);
        }
    }
}
