using UnityEngine;

namespace ForestJourney
{
    public sealed class CoinMotion : MonoBehaviour
    {
        public float bobHeight = .25f;
        public float rotationSpeed = 75;
        Vector3 origin;
        float phase;
        void Awake() { origin = transform.localPosition; phase = transform.position.x * .7f + transform.position.z; }
        void Update()
        {
            transform.localPosition = origin + Vector3.up * (Mathf.Sin(Time.time * 2.2f + phase) * bobHeight);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}
