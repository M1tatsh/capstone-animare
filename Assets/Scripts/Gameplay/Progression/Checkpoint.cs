using Data.Audio;
using Gameplay.Player;
using Gameplay.Rotation;
using Gameplay.Save;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Progression
{
    /// <summary>
    /// A respawn point for the player.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        #region variables
        [Tooltip("How close the player has to get to activate the checkpoint.")]
        [SerializeField] private float activationRadius = 1f;


        [SerializeField] private bool ignoreDepth = true;

        [Tooltip("Optional force save when activating this checkpoint.")]
        [SerializeField] private bool saveGameWhenReached = false;

        [Header("Feedback")]
        [SerializeField] private SoundEffect reachedSound;
        [Tooltip("Shown while this is not the current checkpoint.")]
        [SerializeField] private GameObject inactiveVisual;
        [Tooltip("Shown while this is the current checkpoint.")]
        [SerializeField] private GameObject activeVisual;
        #endregion

        [Tooltip("Runs each time this becomes the current checkpoint.")]
        [SerializeField] private UnityEvent onReached = new UnityEvent();

        private static Checkpoint currentCheckpoint;
        private PlayerMovement playerMovement;
        private ViewRotator viewRotator;

        public bool IsCurrent => currentCheckpoint == this;

        void Start()
        {
            playerMovement = FindAnyObjectByType<PlayerMovement>();
            viewRotator = FindAnyObjectByType<ViewRotator>();
            ShowVisual(false);
        }

        void OnDestroy()
        {
            if (currentCheckpoint == this)
                currentCheckpoint = null;
        }

        void Update()
        {
            if (IsCurrent)
                return;


            if (playerMovement == null)
            {
                playerMovement = FindAnyObjectByType<PlayerMovement>();
                return;
            }


            if (viewRotator != null && viewRotator.IsRotating)
                return;

            if (GetDistanceToPlayer() <= activationRadius)
                Reach();
        }

        private float GetDistanceToPlayer()
        {
            Vector3 checkpointToPlayer = playerMovement.transform.position - transform.position;


            if (!ignoreDepth || playerMovement.IsBehindGeometry)
                return checkpointToPlayer.magnitude;

            Vector3 intoScreen = ViewAxes.Forward(playerMovement.CurrentView);
            return Vector3.ProjectOnPlane(checkpointToPlayer, intoScreen).magnitude;
        }

        /// <summary>
        /// Makes this the checkpoint the player respawns at.
        /// </summary>
        public void Reach()
        {
            if (playerMovement == null)
                return;

            if (currentCheckpoint != null && currentCheckpoint != this)
                currentCheckpoint.ShowVisual(false);

            currentCheckpoint = this;
            ShowVisual(true);

            playerMovement.SetRespawnPoint(transform.position, playerMovement.CurrentView);

            AudioManager.PlaySoundEffect(reachedSound);
            onReached.Invoke();

            if (saveGameWhenReached)
                SaveManager.SaveGame();
        }

        private void ShowVisual(bool isActive)
        {
            if (inactiveVisual != null)
                inactiveVisual.SetActive(!isActive);

            if (activeVisual != null)
                activeVisual.SetActive(isActive);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, activationRadius);
        }
    }
}
