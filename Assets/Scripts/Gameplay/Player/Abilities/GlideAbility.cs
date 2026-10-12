using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerMovement), typeof(CollisionCheck))]
    public class GlideAbility : MonoBehaviour
    {
        [SerializeField] private float glideFallSpeed = 1.5f;
        [SerializeField] private float glideAcceleration = 40f;

        private PlayerMovement playerMovement;
        private CollisionCheck collisionCheck;
        private bool isHeld = false;

        void Awake()
        {
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
            if (!isHeld || collisionCheck.IsGrounded || playerMovement.VerticalVelocity > 0f)
            {
                EndGlide();
                return;
            }

            playerMovement.IsGliding = true;

            //Josh: playermovement moves the player on the grid now, so the glide changes the falling speed there.
            //gravity does not need cancelling here any more: pm skips gravity while IsGliding is true.
            playerMovement.VerticalVelocity = Mathf.MoveTowards(playerMovement.VerticalVelocity, -glideFallSpeed, glideAcceleration * Time.fixedDeltaTime);
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