using Data.Audio;
using DG.Tweening;
using Gameplay.Player;
using UnityEngine;

namespace Gameplay.Rotation
{
    /// <summary>
    /// Turns the camera 90 degrees at a time around the player using tweens (thanks demigant!).
    /// The camera has to be a child of the pivot to work, since it moves on a pivot. The distance
    /// between the camera and the pivot act as a spring arm, basically.
    /// 
    /// While the camera is turning, the player is frozen, and when the turn ends the player is
    /// placed into the new flattened view.
    /// </summary>
    public class ViewRotator : MonoBehaviour
    {
        [SerializeField] Transform pivot;
        [SerializeField] PlayerMovement player;
        [SerializeField] float duration = 0.5f;
        [Tooltip("Keeps the pivot on top of the player, so the camera follows the player and turns around them.")]
        [SerializeField] bool pivotFollowsPlayer = true;

        [Header("Sounds")]
        [SerializeField] SoundEffect rotateSound;
        [Tooltip("Plays when the player tries to rotate but can't (for example something would trap them).")]
        [SerializeField] SoundEffect rotateBlockedSound;

        int view;

        public bool IsRotating
        {
            get;
            private set;
        }

        /// <summary>
        /// Set this to -1 or 1 to ask for one turn. It goes back to 0 once the request has been used.
        /// </summary>
        public int RotateDir
        {
            get;
            set;
        }

        void Start()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerMovement>();

            if (pivot == null || player == null)
            {
                Debug.LogError("ViewRotator: Needs a pivot Transform and a PlayerMovement in the scene. Rotation is turned off.");
                enabled = false;
                return;
            }

            if (pivotFollowsPlayer)
                pivot.position = player.transform.position;

            AttachCameraToPivot();
        }

        void Update()
        {
            if (IsRotating || RotateDir == 0)
                return;

            //consumes the request so one key press gives exactly one attempt.
            int requestedDirection = RotateDir;
            RotateDir = 0;

            Rotate(requestedDirection);
        }

        //LateUpdate runs after the player has moved this frame, so the camera never lags behind.
        void LateUpdate()
        {
            if (pivotFollowsPlayer)
                pivot.position = player.transform.position;
        }

        void OnDestroy()
        {
            //stops a turn that is still running, so it does not try to use destroyed objects.
            DOTween.Kill(this);
        }

        void Rotate(int direction)
        {
            //wrap to 0 once view reaches 4
            int nextView = (view + direction + 4) % 4;
            if (!player.CanRotateTo(nextView))
            {
                AudioManager.PlaySoundEffect(rotateBlockedSound);
                return;
            }

            AudioManager.PlaySoundEffect(rotateSound);
            IsRotating = true;
            player.SetFrozen(true);

            pivot.DORotateQuaternion(Quaternion.Euler(0, 90f * nextView, 0), duration)
                 .SetEase(Ease.InOutCubic)
                 .SetTarget(this)
                 .OnUpdate(() =>
                 {
                     //turn the player together with the camera so they keep facing it during the turn.
                     player.transform.rotation = pivot.rotation;
                 })
                 .OnComplete(() =>
                 {
                     view = nextView;
                     player.CommitRotation(nextView);
                     player.SetFrozen(false);
                     IsRotating = false;
                     RotateDir = 0;
                 });
        }

        /// <summary>
        /// Makes sure the main camera is a child of the pivot.
        /// </summary>
        void AttachCameraToPivot()
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                Debug.LogWarning("ViewRotator: No camera tagged MainCamera was found, so nothing will turn with the pivot.");
                return;
            }

            if (mainCamera.transform.IsChildOf(pivot))
                return;

            mainCamera.transform.SetParent(pivot, true);

            Debug.LogWarning("ViewRotator: Main Camera was not a child of the pivot, so it was parented to it for this play session. " +
                "Drag Main Camera onto the pivot object in the Hierarchy to make this permanent.");
        }
    }
}
