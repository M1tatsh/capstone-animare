using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Sends the player's movement to the Animator every frame, so the Animator Controller can switch
    /// between idle and walking (and later jumping, falling, and so on). Also flips the sprite to face
    /// the way the player last moved.
    ///
    /// Put this on the Player, next to PlayerMovement. The Animator and SpriteRenderer can be on the
    /// Player itself or on a child object.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerAnimation : MonoBehaviour
    {
        [Header("References (found automatically if left empty)")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Animator parameter names")]
        [Tooltip("Bool parameter that is true while the player is walking on the ground.")]
        [SerializeField] private string walkingParameterName = "isWalking";
        [Tooltip("Bool parameter that is true while the player is standing on the ground.")]
        [SerializeField] private string groundedParameterName = "Grounded";

        [Header("Walking")]
        [Tooltip("The player counts as walking when they move faster than this, in units per second. " +
            "This uses how far the player actually moved, so walking into a wall plays the idle animation.")]
        [SerializeField] private float minimumWalkSpeed = 0.1f;

        [Header("Facing")]
        [Tooltip("Tick this if the sprite art faces right. Untick it if the art faces left (the Reaper sprites face left).")]
        [SerializeField] private bool spriteFacesRight = false;

        private PlayerMovement playerMovement;
        private Vector3 positionLastFrame;

        //Animator parameters are looked up by number, which is faster than by name every frame.
        private int walkingParameterId;
        private int groundedParameterId;
        private bool hasWalkingParameter;
        private bool hasGroundedParameter;

        void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (animator == null)
            {
                Debug.LogWarning("PlayerAnimation: No Animator was found on the player or its children, so no animations will play.", this);
                enabled = false;
                return;
            }

            walkingParameterId = Animator.StringToHash(walkingParameterName);
            groundedParameterId = Animator.StringToHash(groundedParameterName);

            //checking first stops the Animator from filling the Console with warnings if a parameter is missing.
            hasWalkingParameter = HasParameter(walkingParameterName, AnimatorControllerParameterType.Bool);
            hasGroundedParameter = HasParameter(groundedParameterName, AnimatorControllerParameterType.Bool);
        }

        void Start()
        {
            positionLastFrame = transform.position;
        }

        //LateUpdate runs after PlayerMovement has moved the player this frame.
        void LateUpdate()
        {
            bool isGrounded = playerMovement.IsGrounded;
            bool isWalking = isGrounded && GetScreenSpeed() > minimumWalkSpeed;

            if (hasWalkingParameter)
                animator.SetBool(walkingParameterId, isWalking);

            if (hasGroundedParameter)
                animator.SetBool(groundedParameterId, isGrounded);

            UpdateFacing();

            positionLastFrame = transform.position;
        }

        /// <summary>
        /// How fast the player moved left or right on the screen since last frame.
        /// </summary>
        private float GetScreenSpeed()
        {
            if (Time.deltaTime <= 0f)
                return 0f;

            Vector3 movedThisFrame = transform.position - positionLastFrame;

            //only the left/right part of the movement on screen counts, not falling or depth changes.
            float sidewaysDistance = Vector3.Dot(movedThisFrame, playerMovement.GetMoveAxis());

            return Mathf.Abs(sidewaysDistance) / Time.deltaTime;
        }

        /// <summary>
        /// Flips the sprite so it faces the direction the player last pressed. Left and right are on the
        /// screen, so this stays correct in every view.
        /// </summary>
        private void UpdateFacing()
        {
            if (spriteRenderer == null)
                return;

            bool isFacingRight = playerMovement.LastMoveSign > 0f;

            if (spriteFacesRight)
                spriteRenderer.flipX = !isFacingRight;
            else
                spriteRenderer.flipX = isFacingRight;
        }

        private bool HasParameter(string parameterName, AnimatorControllerParameterType parameterType)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == parameterName && parameter.type == parameterType)
                    return true;
            }

            Debug.LogWarning($"PlayerAnimation: The Animator Controller has no {parameterType} parameter called '{parameterName}'. " +
                "Add it in the Animator window, or change the name on this component.", this);
            return false;
        }
    }
}
