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
        public bool ControlsEnabled { get; set; }
        public Vector2 MoveInput { get; private set; }
        public Vector3 Heading { get; private set; } = Vector3.forward;
        public Rigidbody Body { get; private set; }
        Vector3 spawn;
        void Awake()
        {
            Body = GetComponent<Rigidbody>(); spawn = transform.position;
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
            Vector3 horizontal = Vector3.ProjectOnPlane(Body.linearVelocity, Vector3.up);
            Vector3 change = Vector3.ClampMagnitude(direction * speed - horizontal, acceleration * Time.fixedDeltaTime);
            Body.AddForce(change, ForceMode.VelocityChange);
            if (Body.position.y < -30) ResetToSpawn();
        }
        public void ResetToSpawn()
        {
            Body.position = spawn; Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            MoveInput = Vector2.zero; Heading = Vector3.forward;
        }
    }
}
