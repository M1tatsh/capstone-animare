using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerMovement), typeof(CollisionCheck))]
    public class DashAbility : MonoBehaviour
    {
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 0.4f;
        [SerializeField] private int maxAirDashes = 1;

        private Rigidbody rb;
        private PlayerMovement playerMovement;
        private CollisionCheck collisionCheck;

        private float dashTimer = 0f;
        private float nextDashTime = 0f;
        private float dashSign = 1f;
        private int airDashesUsed = 0;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            playerMovement = GetComponent<PlayerMovement>();
            collisionCheck = GetComponent<CollisionCheck>();
        }

        void OnDisable()
        {
            EndDash();
        }

        public bool TryDash(float input)
        {
            if (!isActiveAndEnabled || playerMovement.IsDashing || Time.time < nextDashTime)
            {
                return false;
            }

            if (!collisionCheck.IsGrounded)
            {
                if (airDashesUsed >= maxAirDashes)
                {
                    return false;
                }

                airDashesUsed++;
            }

            dashSign = Mathf.Abs(input) > 0.1f ? Mathf.Sign(input) : playerMovement.LastMoveSign;
            dashTimer = dashDuration;
            playerMovement.IsDashing = true;
            return true;
        }

        public void ResetAirDashes()
        {
            airDashesUsed = 0;
        }

        void FixedUpdate()
        {
            if (!playerMovement.IsDashing)
            {
                return;
            }

            if (playerMovement.IsJumpPressed && collisionCheck.IsGrounded)
            {
                EndDash();
                return;
            }

            rb.linearVelocity = playerMovement.GetMoveAxis() * (dashSign * dashSpeed);
            dashTimer -= Time.fixedDeltaTime;

            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }

        private void EndDash()
        {
            if (playerMovement == null || !playerMovement.IsDashing)
            {
                return;
            }

            playerMovement.IsDashing = false;
            nextDashTime = Time.time + dashCooldown;
        }
    }
}