using UnityEngine;
using UnityEngine.InputSystem;

namespace ForestJourney
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        public PlayerController player;
        [Header("Third-person framing")]
        [Min(.5f)] public float distance = 7f;
        [Tooltip("Orbit pivot height above the player's centre.")]
        [Min(0)] public float height = 1.6f;
        public float shoulderOffset = .55f;
        [Min(.001f)] public float followTime = .08f;
        [Header("Mouse look - degrees per pixel")]
        [Min(0)] public float horizontalSensitivity = .14f;
        [Min(0)] public float verticalSensitivity = .12f;
        public float minimumPitch = -25f;
        public float maximumPitch = 65f;
        public float initialPitch = 18f;
        [Min(.001f)] public float rotationSmoothTime = .035f;
        [Header("Camera collision")]
        [Min(.05f)] public float collisionRadius = .28f;
        [Min(0)] public float collisionPadding = .08f;
        public LayerMask obstructionMask = ~4;

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public Ray AimRay => new Ray(transform.position, transform.forward);
        Vector3 smoothedPivot, pivotVelocity;
        float targetYaw, targetPitch, yawVelocity, pitchVelocity, boomLength, boomVelocity;
        bool lookWasActive;

        void Start() { Snap(); }
        void OnValidate()
        {
            minimumPitch = Mathf.Clamp(minimumPitch, -80f, 79f);
            maximumPitch = Mathf.Clamp(maximumPitch, minimumPitch + 1f, 80f);
            initialPitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
        }
        void Update()
        {
            bool active = player && player.ControlsEnabled && Time.timeScale > 0 &&
                Cursor.lockState == CursorLockMode.Locked && Application.isFocused;
            if (!active) { lookWasActive = false; return; }
            // Ignore the cursor-warp delta on the first frame after Play/resume/focus.
            if (!lookWasActive) { lookWasActive = true; return; }
            if (Mouse.current == null) return;
            Vector2 delta = Mouse.current.delta.ReadValue();
            // Mouse delta is already a per-frame pixel displacement; no deltaTime here.
            targetYaw = Mathf.Repeat(targetYaw + delta.x * horizontalSensitivity, 360f);
            targetPitch = Mathf.Clamp(targetPitch - delta.y * verticalSensitivity, minimumPitch, maximumPitch);
        }
        public void Snap()
        {
            if (!player) return;
            Vector3 heading = player.Heading;
            SetOrbit(Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg, initialPitch, true);
            lookWasActive = false;
        }
        // Entry point for future aiming, scripted transitions and reproducible verification.
        public void SetOrbit(float yaw, float pitch, bool immediate = false)
        {
            targetYaw = Mathf.Repeat(yaw, 360f);
            targetPitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
            if (!immediate || !player) return;
            Yaw = targetYaw; Pitch = targetPitch;
            yawVelocity = pitchVelocity = boomVelocity = 0;
            pivotVelocity = Vector3.zero;
            smoothedPivot = player.transform.position + Vector3.up * height;
            boomLength = Mathf.Sqrt(distance * distance + shoulderOffset * shoulderOffset);
            PlaceCamera(0, true);
        }
        void LateUpdate()
        {
            if (!player || !player.ControlsEnabled || Time.timeScale == 0) return;
            float dt = Time.deltaTime;
            Yaw = Mathf.SmoothDampAngle(Yaw, targetYaw, ref yawVelocity, rotationSmoothTime, Mathf.Infinity, dt);
            Pitch = Mathf.Clamp(Mathf.SmoothDamp(Pitch, targetPitch, ref pitchVelocity, rotationSmoothTime, Mathf.Infinity, dt), minimumPitch, maximumPitch);
            // Follow the interpolated render transform after physics, never the raw body position.
            smoothedPivot = Vector3.SmoothDamp(smoothedPivot, player.transform.position + Vector3.up * height, ref pivotVelocity, followTime, Mathf.Infinity, dt);
            PlaceCamera(dt, false);
        }
        void PlaceCamera(float dt, bool immediate)
        {
            Vector3 focus = player.transform.position + Vector3.up * height;
            Quaternion orbit = Quaternion.Euler(Pitch, Yaw, 0);
            Vector3 desired = smoothedPivot + orbit * new Vector3(shoulderOffset, 0, -distance);
            Vector3 offset = desired - focus;
            float allowed = offset.magnitude;
            Vector3 direction = allowed > .001f ? offset / allowed : -(orbit * Vector3.forward);
            if (Physics.SphereCast(focus, collisionRadius, direction, out var hit, allowed, obstructionMask, QueryTriggerInteraction.Ignore))
                allowed = Mathf.Max(0, hit.distance - collisionPadding);
            // Retract immediately at an obstacle; extend smoothly when it clears.
            if (immediate || allowed < boomLength) { boomLength = allowed; boomVelocity = 0; }
            else boomLength = Mathf.SmoothDamp(boomLength, allowed, ref boomVelocity, followTime, Mathf.Infinity, dt);
            transform.SetPositionAndRotation(focus + direction * boomLength, orbit);
        }
    }
}
