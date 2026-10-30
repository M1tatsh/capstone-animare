using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// MonoBehavior that unlocks a player form when Unlock() is called. Attach to trigger object on screen
    /// </summary>
    public class FormUnlock : MonoBehaviour
    {
        [SerializeField] private PlayerForm formToUnlock = PlayerForm.Fox;

        [Tooltip("Also switches the player into the form right away.")]
        [SerializeField] private bool switchToFormNow = false;

        public void Unlock()
        {
            PlayerFormManager formManager = FindAnyObjectByType<PlayerFormManager>();

            if (formManager == null)
            {
                Debug.LogWarning("FormUnlock: There is no player with a PlayerFormManager in the scene.", this);
                return;
            }

            formManager.SetFormUnlocked(formToUnlock, true);

            if (switchToFormNow)
                formManager.SetForm(formToUnlock);
        }
    }
}
