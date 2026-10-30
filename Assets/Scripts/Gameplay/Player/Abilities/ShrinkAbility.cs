using Data.Audio;
using UnityEngine;

namespace Gameplay.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public class ShrinkAbility : MonoBehaviour
    {
        [SerializeField] private float shrinkScale = 0.2f;
        [SerializeField] private float shrunkJumpScale = 0.5f;
        [SerializeField] private SoundEffect shrinkSound;
        [SerializeField] private SoundEffect growSound;

        private PlayerMovement playerMovement;
        private float normalJumpForce;
        private bool isShrunk = false;

        void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        public bool TryToggle()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            return isShrunk ? TryGrow() : TryShrink();
        }

        public bool TryShrink()
        {
            if (isShrunk)
            {
                return true;
            }

            if (!playerMovement.TrySetSizeScale(shrinkScale))
            {
                return false;
            }

            normalJumpForce = playerMovement.JumpForce;
            playerMovement.JumpForce = normalJumpForce * shrunkJumpScale;
            isShrunk = true;
            playerMovement.PlayPlayerSound(shrinkSound);
            return true;
        }

        public bool TryGrow()
        {
            if (!isShrunk)
            {
                return true;
            }

            if (!playerMovement.TrySetSizeScale(1f))
            {
                return false;
            }

            playerMovement.JumpForce = normalJumpForce;
            isShrunk = false;
            playerMovement.PlayPlayerSound(growSound);
            return true;
        }

        public bool IsShrunk
        {
            get { return isShrunk; }
        }
    }
}
