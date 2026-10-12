using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerMovement), typeof(CollisionCheck))]
    public class DoubleJumpAbility : MonoBehaviour
    {
        [SerializeField] private int extraJumps = 1;
        [SerializeField] private float jumpForceMultiplier = 1f;

        private PlayerMovement playerMovement;
        private CollisionCheck collisionCheck;
        private int jumpsUsed = 0;

        void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            collisionCheck = GetComponent<CollisionCheck>();
        }

        public bool TryAirJump()
        {
            if (!isActiveAndEnabled || collisionCheck.IsGrounded || jumpsUsed >= extraJumps)
            {
                return false;
            }

            jumpsUsed++;
            //Josh: playerMovement moves the player on the grid now, so the air jump sets the upward speed there.
            //replacing the speed (instead of adding to it) cancels any falling speed first, the same as before.
            playerMovement.VerticalVelocity = playerMovement.JumpForce * jumpForceMultiplier;
            return true;
        }

        public void ResetAirJumps()
        {
            jumpsUsed = 0;
        }
    }
}