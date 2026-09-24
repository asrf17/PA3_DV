using UnityEngine;

namespace ForestJourney
{
    // On the visual child; rolling stays independent of the physics root's TPS yaw.
    public sealed class BallRollingVisual : MonoBehaviour
    {
        public Transform player;
        public float radius = 1f;
        Vector3 previous;
        Quaternion visualRotation;
        void OnEnable() { ResetRolling(); }
        public void ResetRolling()
        {
            if (player) previous = player.position;
            visualRotation = Quaternion.identity;
            transform.rotation = visualRotation;
        }
        void LateUpdate()
        {
            if (!player) return;
            Vector3 delta = player.position - previous; previous = player.position;
            if (Time.timeScale == 0) return;
            Vector3 travel = Vector3.ProjectOnPlane(delta, Vector3.up);
            if (delta.magnitude <= 4 && travel.sqrMagnitude > .000001f)
                visualRotation = Quaternion.AngleAxis(travel.magnitude / Mathf.Max(.01f, radius) * Mathf.Rad2Deg, Vector3.Cross(Vector3.up, travel).normalized) * visualRotation;
            // Keep sphere rolling independent of the physics root's deliberate TPS yaw.
            transform.rotation = visualRotation;
        }
    }
}
