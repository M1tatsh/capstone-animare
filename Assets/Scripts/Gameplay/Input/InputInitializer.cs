using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Input
{
    /// <summary>
    /// Disables every action map on startup, then enables one based on an index assigned on construction.
    /// </summary>
    public class InputInitializer : MonoBehaviour
    {
        private PlayerInput playerInput;
        private int playerIndex;

        //[SerializeField] private string playerInputName;

        void Start()
        {
            playerInput = GetComponent<PlayerInput>();

            if (playerInput != null && playerInput.actions != null)
            {
                //disable every action map
                foreach (var map in playerInput.actions.actionMaps)
                {
                    map.Disable();
                }

                //enable default starting map based off player index
                var defaultMap = playerInput.actions.actionMaps[playerIndex];
                if (defaultMap != null)
                {
                    defaultMap.Enable();
                    playerInput.SwitchCurrentActionMap(defaultMap.name);
                }
            }
        }

        /// <summary>
        /// Custom constructor method for initialization at run-time. 
        /// </summary>
        public void Init(int index)
        {
            this.playerIndex = index;
        }
    }
}
