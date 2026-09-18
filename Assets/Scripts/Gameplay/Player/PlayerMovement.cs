using System.Collections;
using UnityEngine;
using static Gameplay.Mechanics.PlatformManager;


namespace Gameplay.Player
{
    /// <summary>
    /// Handles player movement data and provides public methods for actions like walking, dashing, and jumping.
    /// Pass any data to this class that involves manipulating an object's rigidbody.
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        #region variables
        //Stats
        [SerializeField]
        private float moveSpeed, maxMoveSpeed, jumpForce, maxJumpVelocity;
        [SerializeField] private float gravityForce = -20f;
        [SerializeField] private float cutJumpSpeed = -1f;
        [SerializeField] private float rotationSpeed = 8f;
        private float degree = 0;

        private Rigidbody rb;

        //[SerializeField] private float jumpTime = 0.4f;

        private bool isJumpPressed = false;
        private bool isJumpReleased = false;

        private CollisionCheck collisionCheck;

        private FacingDirection myFacingDirection;

        #endregion


        public FacingDirection CmdFacingDirection
        {
            set
            {
                myFacingDirection = value;
            }

        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            collisionCheck = GetComponent<CollisionCheck>();
        }

        /// <summary>
        /// Simulates 1-D movement along the x-axis by added velocity to the object's rigidbody velocity.
        /// The values are clamped by a local variable to cap the max speed of the object.
        /// Automatically handles logic for determining when the player should move on x or z axis.
        /// </summary>
        /// <param name="direction">Direction to move based on Vector3 values.</param>
        public void Walk(float direction)
        {
            if(myFacingDirection == FacingDirection.Front)
            {
                rb.linearVelocity = new Vector3(
                    Mathf.Clamp(direction * moveSpeed, -maxMoveSpeed, maxMoveSpeed),
                    rb.linearVelocity.y,
                    rb.linearVelocity.z
                );
            }
            else if (myFacingDirection == FacingDirection.Back)
            {
                rb.linearVelocity = new Vector3(
                    Mathf.Clamp(-direction * moveSpeed, -maxMoveSpeed, maxMoveSpeed),
                    rb.linearVelocity.y,
                    rb.linearVelocity.z
                );
            }
            else if (myFacingDirection == FacingDirection.Right)
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y,
                    Mathf.Clamp(direction * moveSpeed, -maxMoveSpeed, maxMoveSpeed)
                );
            }
            else if (myFacingDirection == FacingDirection.Left)
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y,
                    Mathf.Clamp(-direction * moveSpeed, -maxMoveSpeed, maxMoveSpeed)
                );
            }


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
        public void UpdateToFacingDirection(FacingDirection newDirection, float angle)
        {

            myFacingDirection = newDirection;
            degree = angle;

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

        /// <summary>
        /// Rotates the character based on amount of degrees in euler angles.
        /// </summary>
        private void HandleRotation()
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, degree, 0), rotationSpeed * Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (isJumpPressed && collisionCheck.IsGrounded)
            {
                Jump(jumpForce);
                isJumpPressed = false;
            }
                
            HandleGravity();
            HandleRotation();
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
            set { moveSpeed = value; }
        }

        public float JumpForce
        {
            get { return JumpForce; }
            set { JumpForce = value; }
        }
        public float Degree
        {
            get { return degree; }
            set { degree = value; }
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

        public FacingDirection MyFacingDirection
        {
            get { return myFacingDirection; }
            set { myFacingDirection = value; }
        }
        #endregion

    }
}
