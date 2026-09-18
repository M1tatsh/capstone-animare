using UnityEngine;
using UnityEngine.InputSystem;
using Gameplay.Mechanics;

namespace Gameplay.Player
{
    /// <summary>
    /// Handles player actions through input actions assigned to the player upon initialization.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        #region variables
        private Vector2 move;
        private PlayerMovement playerMovement;
        private RadialSelection radialSelection;
        private PlatformManager platformManager;
        #endregion

        void Start()
        {
            playerMovement = GetComponent<PlayerMovement>();
            radialSelection = FindAnyObjectByType<RadialSelection>();
            platformManager = FindAnyObjectByType<PlatformManager>();

            if (playerMovement == null)
            {
                Debug.LogError("PlayerController: Game Object is missing Player Movement component!");
            }

            if (radialSelection == null)
            {
                Debug.LogError("PlayerController: RadialSelection is missing in the scene!");
            }

            if (platformManager == null)
            {
                Debug.LogError("PlayerController: Platform Manager is missing in the scene!");
            }
        }

        /// <summary>
        /// Called when the input assigned to the action "Move" is performed by the user during run-time.
        /// Used to pass movement values to the PlayerMovement class.
        /// </summary>
        /// <param name="value">Vector2 that is assigned values through four-directional input by the user.</param>
        public void OnMove(InputValue value)
        {
            move = value.Get<Vector2>();
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
            playerMovement.IsJumpPressed = button.isPressed;

            if (!button.isPressed)
            {
                playerMovement.OnJumpReleased();
            }
            //Debug.Log($"PlayerController | X:{button.isPressed}");
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
        public void OnRotateWorldLeft(InputValue button)
        {
            platformManager.rotateLeft = button.isPressed;
        }

        public void OnRotateWorldRight(InputValue button)
        {
            platformManager.rotateRight = button.isPressed;
        }

        private void FixedUpdate()
        {
            //Vector3 movement2D = new Vector3(move.x, move.y, 0f);

            playerMovement.Walk(move.x);
        }
    }
}