using System.Collections;
using UnityEngine;


namespace Gameplay.Player
{
    /// <summary>
    /// Handles player movement data and provides public methods for actions like walking, dashing, and jumping.
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        //Stats
        [SerializeField]
        private float moveSpeed, maxMoveSpeed, jumpForce, maxJumpVelocity;
        [SerializeField] private float gravityForce = -20f;
        [SerializeField] private float cutJumpSpeed = -1f;
        private Rigidbody rb;

        [SerializeField] private float jumpTime = 0.4f;

        private bool isJumpPressed = false;
        private bool isJumpReleased = false;


        private CollisionCheck collisionCheck;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            collisionCheck = GetComponent<CollisionCheck>();
        }

        /// <summary>
        /// Simulates 1-D movement along the x-axis by added velocity to the object's rigidbody velocity.
        /// The values are clamped by a local variable to cap the max speed of the object.
        /// </summary>
        /// <param name="velocity">Direction to move based on Vector3 values.</param>
        public void Walk(float velocity)
        {
            rb.linearVelocity = new Vector3(
                Mathf.Clamp(velocity * moveSpeed, -maxMoveSpeed, maxMoveSpeed),
                rb.linearVelocity.y,
                rb.linearVelocity.z
            );
        }

        /// <summary>
        /// Simulates jump action by applying an instant force on the rigidbody's y-vector. 
        /// Should only be called for one frame before going on a cooldown.
        /// </summary>
        /// <param name="velocity">Amount of force to be applied to the object.</param>
        public void Jump(float velocity)
        {
            //StartCoroutine(JumpTime(jumpTime));

            if (collisionCheck.IsGrounded)
            {
                rb.AddForce(new Vector3(0, velocity, 0), ForceMode.Impulse);
            }
            
        }

        /// <summary>
        /// This method is called anytime the player releases the jump button (inputvalue = false). 
        /// Currently used to force a "drop-down" effect when the player releases the jump button mid-jump.
        /// </summary>
        public void OnJumpReleased()
        {
            //isJumpPressed = false;

            if (rb.linearVelocity.y > cutJumpSpeed)
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    cutJumpSpeed,
                    rb.linearVelocity.z
                );

                //Debug.Log("PlayerMovement: Speed cut.");
            }
            
        }

        /// <summary>
        /// Applies a stronger gravity force on the player while they are not on the ground.
        /// </summary>
        private void HandleGravity()
        {
            if (!collisionCheck.IsGrounded)
            {
                rb.AddForce(new Vector3(0, gravityForce, 0), ForceMode.Acceleration);
            }
        }

        void FixedUpdate()
        {
            if (isJumpPressed && collisionCheck.IsGrounded)
            {
                Jump(jumpForce);
                isJumpPressed = false;
            }
                
            HandleGravity();
        }

        /*
        IEnumerator JumpTime(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if(!collisionCheck.IsGrounded)
            {
                OnJumpReleased();
            }
        }
        */

        #region Properties
        public float MoveSpeed
        {
            get { return moveSpeed; }
            private set { moveSpeed = value; }
        }

        public float JumpForce
        {
            get { return JumpForce; }
            private set { JumpForce = value; }
        }

        public bool IsJumpPressed
        {
            get { return isJumpPressed; }
            set { isJumpPressed = value; }
        }
        public bool IsJumpReleased
        {
            get { return isJumpReleased; }
            set { isJumpReleased = value; }
        }
        #endregion

    }
}
