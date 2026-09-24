using UnityEngine;

namespace ForestJourney
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        public PlayerController player;
        public float distance = 10f;
        public float height = 6f;
        public float followTime = .16f;
        Vector3 velocity;
        Vector3 heading = Vector3.forward;
        void Start() { Snap(); }
        public void Snap()
        {
            if (!player) return;
            heading = Vector3.forward; velocity = Vector3.zero;
            transform.position = player.transform.position - heading * distance + Vector3.up * height;
            transform.LookAt(player.transform.position + Vector3.up);
        }
        void LateUpdate()
        {
            if (!player || Time.timeScale == 0) return;
            // Do not rotate while strafing/reversing: camera-relative controls stay predictable.
            if (player.MoveInput.y > .1f)
                heading = Vector3.Slerp(heading, player.Heading, 1 - Mathf.Exp(-1.3f * Time.deltaTime));
            Vector3 focus = player.transform.position + Vector3.up;
            Vector3 offset = -heading * distance + Vector3.up * (height - 1);
            Vector3 desired = focus + offset;
            if (Physics.SphereCast(focus, .4f, offset.normalized, out var hit, offset.magnitude, ~4, QueryTriggerInteraction.Ignore))
                desired = focus + offset.normalized * Mathf.Max(.5f, hit.distance - .15f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, followTime);
            // Recheck after smoothing, so corners cannot place the camera behind an obstacle.
            Vector3 actual = transform.position - focus;
            if (Physics.SphereCast(focus, .35f, actual.normalized, out hit, actual.magnitude, ~4, QueryTriggerInteraction.Ignore))
                transform.position = focus + actual.normalized * Mathf.Max(.45f, hit.distance - .1f);
            transform.LookAt(focus);
        }
    }
}
