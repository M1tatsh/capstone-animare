using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerMovement), typeof(CollisionCheck))]
    public class DoubleJumpAbility : MonoBehaviour
    {
        [SerializeField] private int extraJumps = 1;
        [SerializeField] private float jumpForceMultiplier = 1f;

        private Rigidbody rb;
        private PlayerMovement playerMovement;
        private CollisionCheck collisionCheck;
        private int jumpsUsed = 0;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
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
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * playerMovement.JumpForce * jumpForceMultiplier, ForceMode.Impulse);
            return true;
        }

        public void ResetAirJumps()
        {
            jumpsUsed = 0;
        }
    }
}