using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debugger class for checking input action maps and such. 
/// </summary>
public class InputDebugger : MonoBehaviour
{
    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        // space bar to check the active map
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (playerInput.currentActionMap != null)
            {
                Debug.Log($"[Input Debug] Active Action Map: {playerInput.currentActionMap.name} for user {playerInput.playerIndex}");
            }
            else
            {
                Debug.Log("[Input Debug] No Action Map is currently active.");
            }
        }
    }
}