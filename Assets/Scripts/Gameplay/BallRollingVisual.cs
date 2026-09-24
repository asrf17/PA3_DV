using UnityEngine;

namespace ForestJourney
{
    // On the visual child, independent of the non-rotating physics root.
    public sealed class BallRollingVisual : MonoBehaviour
    {
        public Transform player;
        public float radius = 1f;
        Vector3 previous;
        void OnEnable() { if (player) previous = player.position; }
        void LateUpdate()
        {
            if (!player) return;
            Vector3 delta = player.position - previous; previous = player.position;
            if (delta.magnitude > 4 || Time.timeScale == 0) return;
            Vector3 travel = Vector3.ProjectOnPlane(delta, Vector3.up);
            transform.Rotate(Vector3.Cross(Vector3.up, travel).normalized, travel.magnitude / radius * Mathf.Rad2Deg, Space.World);
        }
    }
}
