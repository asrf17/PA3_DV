using UnityEngine;

namespace ForestJourney
{
    public sealed class CoinDirectionIndicator : MonoBehaviour
    {
        public CoinManager manager;
        public Transform player;
        public Transform cameraTransform;
        public GameObject indicator;
        public RectTransform arrow;
        public Coin Target { get; private set; }
        public float Bearing { get; private set; }
        void LateUpdate()
        {
            Target = manager.Nearest(player.position);
            indicator.SetActive(Target != null);
            if (!Target) return;
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            Vector3 direction = Vector3.ProjectOnPlane(Target.transform.position - player.position, Vector3.up);
            // Twelve broad sectors deliberately avoid pinpointing a world position or distance.
            Bearing = Mathf.Round(Vector3.SignedAngle(forward, direction, Vector3.up) / 30f) * 30f;
            Quaternion desired = Quaternion.Euler(0, 0, -Bearing);
            arrow.localRotation = Quaternion.Slerp(arrow.localRotation, desired, 1 - Mathf.Exp(-8 * Time.deltaTime));
        }
    }
}
