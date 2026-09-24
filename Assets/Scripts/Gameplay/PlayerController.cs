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
        }
        void FixedUpdate()
        {
            if (!ControlsEnabled) return;
            Vector3 forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = forward * MoveInput.y + right * MoveInput.x;
            if (direction.sqrMagnitude > .01f) Heading = direction.normalized;
            Vector3 facing = IsAiming ? forward : direction;
            if (facing.sqrMagnitude > .001f)
                Body.MoveRotation(Quaternion.RotateTowards(Body.rotation, Quaternion.LookRotation(facing, Vector3.up), rotationSpeed * Time.fixedDeltaTime));
            Grounded = Physics.SphereCast(Body.position, .85f, Vector3.down, out var ground, .5f, ~4, QueryTriggerInteraction.Ignore)
                && Vector3.Dot(ground.normal, Vector3.up) > .65f;
            Vector3 normal = Grounded ? ground.normal : Vector3.up;
            Vector3 target = Vector3.ProjectOnPlane(direction, normal).normalized * direction.magnitude * speed;
            Vector3 horizontal = Vector3.ProjectOnPlane(Body.linearVelocity, normal);
            Vector3 change = Vector3.ClampMagnitude(target - horizontal, acceleration * (Grounded ? 1f : .2f) * Time.fixedDeltaTime);
            Body.AddForce(change, ForceMode.VelocityChange);
            if (Grounded) Body.AddForce(-Vector3.ProjectOnPlane(Physics.gravity, normal) - normal * 8f, ForceMode.Acceleration);
            if (Body.position.y < -30) ResetToSpawn();
        }
        public void ResetToSpawn()
        {
            Body.position = spawn; Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            Body.rotation = spawnRotation;
            MoveInput = Vector2.zero; Heading = spawnRotation * Vector3.forward; IsAiming = false;
            var rolling = GetComponentInChildren<BallRollingVisual>();
            if (rolling) rolling.ResetRolling();
        }
        // Future aiming input can call this without replacing movement or physics.
        public void SetAiming(bool aiming) { IsAiming = aiming; }
    }
}
