using UnityEngine;

namespace Gameplay.Player
{
    public class SceneFormLock : MonoBehaviour
    {
        [SerializeField] private bool foxAvailable = true;
        [SerializeField] private bool rabbitAvailable = true;
        [SerializeField] private bool crowAvailable = true;
        [SerializeField] private bool antAvailable = true;

        void Start()
        {
            PlayerFormManager formManager = FindAnyObjectByType<PlayerFormManager>();

            if (formManager == null)
                return;

            formManager.SetFormUnlocked(PlayerForm.Fox, foxAvailable);
            formManager.SetFormUnlocked(PlayerForm.Rabbit, rabbitAvailable);
            formManager.SetFormUnlocked(PlayerForm.Crow, crowAvailable);
            formManager.SetFormUnlocked(PlayerForm.Ant, antAvailable);
        }
    }
}
