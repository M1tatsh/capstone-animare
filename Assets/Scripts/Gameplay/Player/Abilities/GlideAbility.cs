using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerMovement), typeof(CollisionCheck))]
    public class GlideAbility : MonoBehaviour
    {
        [SerializeField] private float glideFallSpeed = 1.5f;
        [SerializeField] private float glideAcceleration = 40f;

        private Rigidbody rb;
        private PlayerMovement playerMovement;
        private CollisionCheck collisionCheck;
        private bool isHeld = false;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            playerMovement = GetComponent<PlayerMovement>();
            collisionCheck = GetComponent<CollisionCheck>();
        }

        void OnDisable()
        {
            isHeld = false;
            EndGlide();
        }

        public void SetHeld(bool held)
        {
            isHeld = held && isActiveAndEnabled;

            if (!isHeld)
            {
                EndGlide();
            }
        }

        void FixedUpdate()
        {
            if (!isHeld || collisionCheck.IsGrounded || rb.linearVelocity.y > 0f)
            {
                EndGlide();
                return;
            }

            playerMovement.IsGliding = true;

            Vector3 velocity = rb.linearVelocity;
            velocity.y = Mathf.MoveTowards(velocity.y, -glideFallSpeed, glideAcceleration * Time.fixedDeltaTime);
            rb.linearVelocity = velocity;

            if (rb.useGravity)
            {
                rb.AddForce(-Physics.gravity, ForceMode.Acceleration);
            }
        }

        private void EndGlide()
        {
            if (playerMovement != null)
            {
                playerMovement.IsGliding = false;
            }
        }

        public bool CanGlide
        {
            get { return isActiveAndEnabled && !collisionCheck.IsGrounded; }
        }

        public bool IsGliding
        {
            get { return playerMovement != null && playerMovement.IsGliding; }
        }
    }
}