using UnityEngine;

namespace ForestJourney
{
    public enum GuardianMode { Patrol, Chase }

    [RequireComponent(typeof(CharacterController))]
    public sealed class ForestGuardian : MonoBehaviour
    {
        public GameManager game;
        public PlayerController player;
        public Transform[] waypoints;
        [Min(0.1f)] public float patrolSpeed = 4.2f;
        [Min(0.1f)] public float chaseSpeed = 10.6f;
        [Min(1f)] public float detectionRange = 27f;
        [Min(1f)] public float releaseRange = 48f;
        [Min(0.1f)] public float catchDistance = 2.2f;
        [Min(0.05f)] public float senseInterval = 0.2f;
        [Min(0)] public float alertDelay = 5f;
        public LayerMask sightMask = ~4;

        public GuardianMode Mode { get; private set; }
        public int CurrentWaypoint { get; private set; }
        public bool CanSeePlayer { get; private set; }

        CharacterController controller;
        Vector3 initialPosition;
        Quaternion initialRotation;
        Vector3 horizontalVelocity;
        float verticalSpeed;
        float senseCountdown;
        float lostSightTime;
        float alertCountdown;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        public void ResetGuardian()
        {
            if (!controller) controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            controller.enabled = true;
            Mode = GuardianMode.Patrol;
            CurrentWaypoint = 0;
            CanSeePlayer = false;
            horizontalVelocity = Vector3.zero;
            verticalSpeed = 0;
            senseCountdown = 0;
            lostSightTime = 0;
            alertCountdown = alertDelay;
        }

        void Update()
        {
            if (!game || game.State != GameState.Playing || !player) return;

            if (alertCountdown > 0) alertCountdown = Mathf.Max(0, alertCountdown - Time.deltaTime);

            senseCountdown -= Time.deltaTime;
            if (senseCountdown <= 0 && alertCountdown <= 0)
            {
                SensePlayer();
                senseCountdown = senseInterval;
            }

            Vector3 target = Mode == GuardianMode.Chase ? player.transform.position : PatrolTarget();
            Vector3 direction = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.01f) direction.Normalize();
            direction = SteerAroundObstacle(direction);
            float speed = Mode == GuardianMode.Chase ? chaseSpeed : patrolSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, direction * speed, 16f * Time.deltaTime);
            if (horizontalVelocity.sqrMagnitude > 0.02f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(horizontalVelocity, Vector3.up), 360f * Time.deltaTime);

            verticalSpeed = controller.isGrounded ? -2f : Mathf.Max(-25f, verticalSpeed + Physics.gravity.y * Time.deltaTime);
            controller.Move((horizontalVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);

            if (alertCountdown <= 0 && Vector3.Distance(transform.position, player.transform.position) < catchDistance)
                game.GuardianCaughtPlayer(transform.position);
        }

        Vector3 PatrolTarget()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                float angle = CurrentWaypoint * Mathf.PI * .5f;
                Vector3 roaming = initialPosition + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 8f;
                if (Vector3.ProjectOnPlane(roaming - transform.position, Vector3.up).sqrMagnitude < 9f)
                    CurrentWaypoint = (CurrentWaypoint + 1) % 4;
                return roaming;
            }
            Transform point = waypoints[CurrentWaypoint % waypoints.Length];
            if (!point) return initialPosition;
            if (Vector3.ProjectOnPlane(point.position - transform.position, Vector3.up).sqrMagnitude < 3f)
            {
                CurrentWaypoint = (CurrentWaypoint + 1) % waypoints.Length;
                point = waypoints[CurrentWaypoint];
            }
            return point ? point.position : initialPosition;
        }

        void SensePlayer()
        {
            Vector3 from = transform.position + Vector3.up * 1.2f;
            Vector3 to = player.transform.position + Vector3.up * 0.35f;
            Vector3 toward = to - from;
            float distance = toward.magnitude;
            bool inRange = distance <= detectionRange;
            CanSeePlayer = inRange && !Physics.Raycast(from, toward / Mathf.Max(distance, .001f),
                distance, sightMask, QueryTriggerInteraction.Ignore);

            if (CanSeePlayer)
            {
                Mode = GuardianMode.Chase;
                lostSightTime = 0;
            }
            else if (Mode == GuardianMode.Chase)
            {
                lostSightTime += senseInterval;
                if (distance > releaseRange || lostSightTime > 4f)
                {
                    Mode = GuardianMode.Patrol;
                    lostSightTime = 0;
                }
            }
        }

        Vector3 SteerAroundObstacle(Vector3 direction)
        {
            if (direction.sqrMagnitude < .1f) return direction;
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            if (!Physics.SphereCast(origin, .42f, direction, out var hit, 1.5f, sightMask,
                QueryTriggerInteraction.Ignore)) return direction;
            if (hit.collider == controller) return direction;
            Vector3 left = Quaternion.Euler(0, -65f, 0) * direction;
            Vector3 right = Quaternion.Euler(0, 65f, 0) * direction;
            bool leftOpen = !Physics.SphereCast(origin, .42f, left, out _, 1.4f, sightMask, QueryTriggerInteraction.Ignore);
            bool rightOpen = !Physics.SphereCast(origin, .42f, right, out _, 1.4f, sightMask, QueryTriggerInteraction.Ignore);
            if (leftOpen) return left;
            if (rightOpen) return right;
            return Vector3.zero;
        }
    }
}
