using UnityEngine;

namespace Gameplay.Player
{
    public class CollisionCheck : MonoBehaviour
    {
        [Header("Ground Check Settings")]
        [SerializeField] private Transform groundCheckTransform; // Position an empty GameObject at the player's feet
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, 0, 0);
        [SerializeField] private float sphereRadius = 0.3f;
        [SerializeField] private LayerMask groundLayer; // Look for ground objects

        private bool isGrounded;

        // PlayerMovement turns this off when the level uses a BlockGrid. It then sets IsGrounded itself,
        // because the grid knows about blocks that only line up from the current view and physics does not.
        private bool usePhysicsGroundCheck = true;

        void Update()
        {
            if (usePhysicsGroundCheck)
            {
                isGrounded = Physics.CheckSphere(groundCheckTransform.position + groundCheckOffset, sphereRadius, groundLayer);
            }
        }
        private void OnDrawGizmos()
        {
            if (groundCheckTransform != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawSphere(groundCheckTransform.position + groundCheckOffset, sphereRadius);
            }
        }

        public bool IsGrounded
        {
            get { return isGrounded; }
            set { isGrounded = value; }
        }

        public bool UsePhysicsGroundCheck
        {
            get { return usePhysicsGroundCheck; }
            set { usePhysicsGroundCheck = value; }
        }

        public Vector3 GroundCheckOffset
        {
            get { return groundCheckOffset; }
            private set { groundCheckOffset = value; }
        }
    }
}
