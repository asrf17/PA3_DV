using UnityEngine;
using UnityEngine.InputSystem;

namespace ForestJourney
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class PlayerController : MonoBehaviour
    {
        public Transform cameraTransform;
        public float speed = 14f;
        public float acceleration = 32f;
        [Min(.1f)] public float jumpHeight = 2.4f;
        [Min(0f)] public float jumpBufferTime = .14f;
        [Min(0f)] public float coyoteTime = .12f;
        [Tooltip("Maximum yaw rotation in degrees per second.")]
        [Min(0)] public float rotationSpeed = 540f;
        public bool ControlsEnabled { get; set; }
        public Vector2 MoveInput { get; private set; }
        public Vector3 Heading { get; private set; } = Vector3.forward;
        public Rigidbody Body { get; private set; }
        Vector3 spawn;
        Quaternion spawnRotation;
        public bool IsAiming { get; private set; }
        public bool Grounded { get; private set; }
        public float SpeedBoostRemaining { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public float CurrentSpeed => speed * SpeedMultiplier;
        float jumpBufferRemaining;
        float coyoteRemaining;
        float jumpLockRemaining;
        void Awake()
        {
            Body = GetComponent<Rigidbody>(); spawn = transform.position; spawnRotation = transform.rotation;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.constraints = RigidbodyConstraints.FreezeRotation;
        }
        void Update()
        {
            var k = Keyboard.current;
            MoveInput = !ControlsEnabled || k == null ? Vector2.zero : Vector2.ClampMagnitude(new Vector2(
                ((k.dKey.isPressed || k.rightArrowKey.isPressed) ? 1 : 0) - ((k.aKey.isPressed || k.leftArrowKey.isPressed) ? 1 : 0),
                ((k.wKey.isPressed || k.upArrowKey.isPressed) ? 1 : 0) - ((k.sKey.isPressed || k.downArrowKey.isPressed) ? 1 : 0)), 1);
            if (!ControlsEnabled) return;
            if (k != null && k.spaceKey.wasPressedThisFrame) RequestJump();
            if (SpeedBoostRemaining > 0)
            {
                SpeedBoostRemaining = Mathf.Max(0, SpeedBoostRemaining - Time.deltaTime);
                if (SpeedBoostRemaining == 0) SpeedMultiplier = 1f;
            }
        }
        void FixedUpdate()
        {
            if (!ControlsEnabled) return;
            jumpBufferRemaining = Mathf.Max(0, jumpBufferRemaining - Time.fixedDeltaTime);
            jumpLockRemaining = Mathf.Max(0, jumpLockRemaining - Time.fixedDeltaTime);
            Vector3 forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = forward * MoveInput.y + right * MoveInput.x;
            if (direction.sqrMagnitude > .01f) Heading = direction.normalized;
            Vector3 facing = IsAiming ? forward : direction;
            if (facing.sqrMagnitude > .001f)
                Body.MoveRotation(Quaternion.RotateTowards(Body.rotation, Quaternion.LookRotation(facing, Vector3.up), rotationSpeed * Time.fixedDeltaTime));
            RaycastHit ground = default;
            Grounded = jumpLockRemaining <= 0 && Body.linearVelocity.y <= .2f &&
                Physics.SphereCast(Body.position, .85f, Vector3.down, out ground, .5f, ~4, QueryTriggerInteraction.Ignore)
                && Vector3.Dot(ground.normal, Vector3.up) > .65f;
            if (Grounded) coyoteRemaining = coyoteTime;
            else coyoteRemaining = Mathf.Max(0, coyoteRemaining - Time.fixedDeltaTime);
            Vector3 normal = Grounded ? ground.normal : Vector3.up;
            Vector3 target = Vector3.ProjectOnPlane(direction, normal).normalized * direction.magnitude * CurrentSpeed;
            Vector3 horizontal = Vector3.ProjectOnPlane(Body.linearVelocity, normal);
            Vector3 change = Vector3.ClampMagnitude(target - horizontal, acceleration * (Grounded ? 1f : .2f) * Time.fixedDeltaTime);
            Body.AddForce(change, ForceMode.VelocityChange);
            if (jumpBufferRemaining > 0 && coyoteRemaining > 0)
            {
                float upward = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
                Vector3 velocity = Body.linearVelocity;
                Body.linearVelocity = new Vector3(velocity.x, upward, velocity.z);
                jumpBufferRemaining = coyoteRemaining = 0;
                jumpLockRemaining = .18f;
                Grounded = false;
            }
            else if (Grounded) Body.AddForce(-Vector3.ProjectOnPlane(Physics.gravity, normal) - normal * 8f, ForceMode.Acceleration);
            if (Body.position.y < -30) ResetToSpawn();
        }
        public void ResetToSpawn()
        {
            Body.position = spawn; Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            Body.rotation = spawnRotation;
            MoveInput = Vector2.zero; Heading = spawnRotation * Vector3.forward; IsAiming = false;
            Grounded = false; jumpBufferRemaining = coyoteRemaining = jumpLockRemaining = 0;
            SpeedBoostRemaining = 0; SpeedMultiplier = 1f;
            var rolling = GetComponentInChildren<BallRollingVisual>();
            if (rolling) rolling.ResetRolling();
        }
        // Future aiming input can call this without replacing movement or physics.
        public void SetAiming(bool aiming) { IsAiming = aiming; }
        public void RequestJump() { if (ControlsEnabled) jumpBufferRemaining = jumpBufferTime; }
        public void ApplySpeedBoost(float duration, float multiplier)
        {
            SpeedBoostRemaining = Mathf.Max(SpeedBoostRemaining, duration);
            SpeedMultiplier = Mathf.Max(SpeedMultiplier, multiplier);
        }
    }
}
