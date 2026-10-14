using UnityEngine;
using UnityEngine.InputSystem;
using Gameplay.Rotation;
using Gameplay.Dialogue;

namespace Gameplay.Player
{
    /// <summary>
    /// Handles player actions through input actions assigned to the player upon initialization.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(DashAbility))]
    [RequireComponent(typeof(DoubleJumpAbility))]
    [RequireComponent(typeof(GlideAbility))]
    public class PlayerController : MonoBehaviour
    {
        #region variables
        private Vector2 move;
        private PlayerMovement playerMovement;
        private RadialSelection radialSelection;
        private DashAbility dashAbility;
        private DoubleJumpAbility doubleJumpAbility;
        private GlideAbility glideAbility;
        private ViewRotator viewRotator;
        private ContextDialogueManager dialogueManager;
        #endregion

        void Start()
        {
            playerMovement = GetComponent<PlayerMovement>();
            dashAbility = GetComponent<DashAbility>();
            doubleJumpAbility = GetComponent<DoubleJumpAbility>();
            glideAbility = GetComponent<GlideAbility>();

            radialSelection = FindAnyObjectByType<RadialSelection>();
            viewRotator = FindAnyObjectByType<ViewRotator>();
            //optional: scenes without contextual dialogue simply have no manager.
            dialogueManager = FindAnyObjectByType<ContextDialogueManager>();

            #region ThrowExceptions
            if (radialSelection == null)
            {
                Debug.LogError("PlayerController: RadialSelection is missing in the scene!");
            }

            if (viewRotator == null)
            {
                // viewRotator is null here, so calling a method on it would crash. nameof gives the class name safely.
                Debug.LogError($"PlayerController: {nameof(ViewRotator)} is missing in the scene!");
            }
            #endregion
        }

        /// <summary>
        /// Called when the input assigned to the action "Move" is performed by the user during run-time.
        /// Used to pass movement values to the PlayerMovement class.
        /// </summary>
        /// <param name="value">Vector2 that is assigned values through four-directional input by the user.</param>
        public void OnMove(InputValue value)
        {
            move = value.Get<Vector2>();
            playerMovement.SetMoveInput(move.x);
            playerMovement.SetVerticalMoveInput(move.y);
            //Debug.Log($"PlayerController | X:{move.x} Y:{move.y}");
        }

        /// <summary>
        /// Called when the input assigned to the action "Jump" is performed by the user during run-time.
        /// It's called when the input is performed (button pressed) or canceled (button released).
        /// Used to pass bool checks to the PlayerMovement class.
        /// </summary>
        /// <param name="button">bool that is assigned true/false when user is pressing/releasing the input.</param>
        public void OnJump(InputValue button)
        {
            bool wasGliding = glideAbility != null && glideAbility.IsGliding;

            if (glideAbility != null)
            {
                glideAbility.SetHeld(button.isPressed);
            }

            if (button.isPressed && doubleJumpAbility != null && doubleJumpAbility.TryAirJump())
            {
                return;
            }

            if (button.isPressed && glideAbility != null && glideAbility.CanGlide)
            {
                return;
            }

            playerMovement.IsJumpPressed = button.isPressed;

            if (button.isPressed)
            {
                //queues the jump request
                playerMovement.RequestJump();
            }

            if (!button.isPressed && !wasGliding)
            {
                playerMovement.OnJumpReleased();
            }
        }

        public void OnDash(InputValue button)
        {
            if (button.isPressed && dashAbility != null)
            {
                dashAbility.TryDash(move.x);
            }
        }

        /// <summary>
        /// Handles menu transformation based on the specified input value.
        /// </summary>
        /// <param name="value">The input value that triggers the menu transformation.</param>
        public void OnTransformMenu(InputValue value)
        {
            //Debug.Log($"OnTransformMenu called! isPressed: {value.isPressed}");

            if (radialSelection != null)
            {
                radialSelection.SetMenuState(value.isPressed);
            }
        }
        /// <summary>
        /// Called when the input assigned to the action "RotateCamera" changes (Q = -1, E = 1, released = 0).
        /// </summary>
        public void OnRotateCamera(InputValue value)
        {
            if (viewRotator == null)
            {
                return;
            }

            int rotateDirection = Mathf.RoundToInt(value.Get<float>());

            if(!viewRotator.IsRotating)
                viewRotator.RotateDir = rotateDirection;
        }

        /// <summary>
        /// Called when the input assigned to the action "Interact" is pressed (F / gamepad left face button).
        /// Shows the next dialogue page or lets the object next to the player react.
        /// </summary>
        public void OnInteract(InputValue button)
        {
            if (button.isPressed && dialogueManager != null)
            {
                dialogueManager.Interact();
            }
        }

        private void FixedUpdate()
        {
            //Vector3 movement2D = new Vector3(move.x, move.y, 0f);

            //playerMovement.Walk(move.x);
        }
    }
}
