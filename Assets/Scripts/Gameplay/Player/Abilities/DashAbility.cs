using Data.Audio;
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
        //Josh: sound for the dash. Leave empty for no sound.
        [SerializeField] private SoundEffect dashSound;

        private PlayerMovement playerMovement;
        private CollisionCheck collisionCheck;

        private float dashTimer = 0f;
        private float nextDashTime = 0f;
        private float dashSign = 1f;
        private int airDashesUsed = 0;

        void Awake()
        {
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
            //Josh: played through playermovement so it gets muffled behind buildings like the other player sounds.
            playerMovement.PlayPlayerSound(dashSound);
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

            //Josh: playermovement moves the player on the grid now, so the dash sets its speed there instead of on the rb.
            //dashSign is -1 for the left side of the screen and 1 for the right side, in whichever view the camera is in.
            playerMovement.HorizontalVelocity = dashSign * dashSpeed;
            playerMovement.VerticalVelocity = 0f;
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