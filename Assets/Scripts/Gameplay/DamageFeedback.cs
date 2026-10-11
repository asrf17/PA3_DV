using UnityEngine;
using UnityEngine.UI;

namespace ForestJourney
{
    public sealed class DamageFeedback : MonoBehaviour
    {
        public GameManager game;
        public Image edgeFlash;
        public Image directionArrow;
        [Min(.1f)] public float flashDuration = .8f;
        float remaining;

        void OnEnable() { if (game) game.Damaged += ShowDamage; }
        void OnDisable() { if (game) game.Damaged -= ShowDamage; }
        void Update()
        {
            remaining = Mathf.Max(0, remaining - Time.unscaledDeltaTime);
            float visibility = remaining / Mathf.Max(.01f, flashDuration);
            if (edgeFlash) edgeFlash.color = new Color(1f, .12f, .08f, visibility * .65f);
            if (directionArrow) directionArrow.color = new Color(1f, .28f, .18f, visibility * .95f);
        }
        void ShowDamage(Vector3 attackerPosition)
        {
            remaining = flashDuration;
            if (!directionArrow || !game || !game.player || !game.followCamera) return;
            Vector3 fromPlayer = Vector3.ProjectOnPlane(attackerPosition - game.player.transform.position, Vector3.up);
            Vector3 cameraForward = Vector3.ProjectOnPlane(game.followCamera.transform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward);
            Vector2 direction = new Vector2(Vector3.Dot(fromPlayer, cameraRight), Vector3.Dot(fromPlayer, cameraForward));
            if (direction.sqrMagnitude < .001f) direction = Vector2.up;
            direction.Normalize();
            directionArrow.rectTransform.anchoredPosition = direction * 140f;
            directionArrow.rectTransform.localEulerAngles = new Vector3(0, 0,
                -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg);
            if (edgeFlash) edgeFlash.color = new Color(1f, .12f, .08f, .65f);
            directionArrow.color = new Color(1f, .28f, .18f, .95f);
        }
    }
}
