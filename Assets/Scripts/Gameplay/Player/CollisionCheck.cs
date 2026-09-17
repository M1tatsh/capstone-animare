using UnityEngine;

namespace Gameplay.Player
{
    public class CollisionCheck : MonoBehaviour
    {
        [Header("Ground Check Settings")]
        [SerializeField] private Transform groundCheckTransform; // Position an empty GameObject at the player's feet
        [SerializeField] private Vector3 groundCheckOffset;
        [SerializeField] private float sphereRadius = 0.3f;
        [SerializeField] private LayerMask groundLayer; // Look for ground objects

        private bool isGrounded;

        void Update()
        {
            isGrounded = Physics.CheckSphere(groundCheckTransform.position + groundCheckOffset, sphereRadius, groundLayer);
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
    }
}
