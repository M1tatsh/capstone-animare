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
    /// Use the public API to set whether the camera follows the player or not, here are some uses
    ///     viewRotator.SetFollowPlayer(false);              //the camera stays where it is
    ///     viewRotator.MovePivotTo(position);               //the camera glides over to look at something
    ///     viewRotator.SetFollowPlayer(true);               //the camera glides back to the player and follows again
    /// While the camera isn't following, rotating turns the view around the pivot's spot, not the player.
    /// </summary>
    public class ViewRotator : MonoBehaviour
    {
        [SerializeField] Transform pivot;
        [SerializeField] PlayerMovement player;
        [SerializeField] float duration = 0.5f;
        [Tooltip("Keeps the pivot on top of the player, so the camera follows the player and turns around them. " +
            "This is only the starting setting: SetFollowPlayer changes it while the game runs.")]
        [SerializeField] bool pivotFollowsPlayer = true;
        [Tooltip("Seconds the camera takes to glide back to the player when following is turned back on " +
            "(and to glide to a spot with MovePivotTo). 0 snaps instantly.")]
        [SerializeField] float followBlendDuration = 0.5f;

        [Header("Sounds")]
        [SerializeField] SoundEffect rotateSound;
        [Tooltip("Plays when the player tries to rotate but can't (for example something would trap them).")]
        [SerializeField] SoundEffect rotateBlockedSound;

        int view;

        //gliding back to the player after following is turned back on.
        //the player can keep moving during the glide, so it is done by hand in LateUpdate instead of with a tween.
        Vector3 blendStartPosition;
        float blendTimer;
        float currentBlendDuration;

        //gliding to a spot with MovePivotTo.
        Tween pivotMoveTween;

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
            if (!pivotFollowsPlayer)
                return;

            Vector3 playerPosition = player.transform.position;

            if (blendTimer < currentBlendDuration)
            {
                //still gliding back: move from where the camera was toward wherever the player is now.
                blendTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(blendTimer / currentBlendDuration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

                pivot.position = Vector3.Lerp(blendStartPosition, playerPosition, easedProgress);
                return;
            }

            pivot.position = playerPosition;
        }

        void OnDestroy()
        {
            //stops a turn that is still running, so it does not try to use destroyed objects.
            DOTween.Kill(this);

            if (pivotMoveTween != null && pivotMoveTween.IsActive())
                pivotMoveTween.Kill();
        }

        /// <summary>
        /// True while the camera is following the player (including while it glides back to them).
        /// </summary>
        public bool IsFollowingPlayer => pivotFollowsPlayer;

        /// <summary>
        /// Turns following the player on or off. Turning it off leaves the camera where it is.
        /// Turning it on glides the camera back to the player over Follow Blend Duration, then follows again.
        /// Can be picked in a UnityEvent (it shows up with a checkbox).
        /// </summary>
        public void SetFollowPlayer(bool follow)
        {
            SetFollowPlayer(follow, followBlendDuration);
        }

        /// <summary>
        /// The same as SetFollowPlayer(follow), with a different glide time. 0 snaps straight to the player.
        /// </summary>
        public void SetFollowPlayer(bool follow, float blendDuration)
        {
            if (pivot == null)
                return;

            //a MovePivotTo glide in progress would fight with following, so it is stopped either way.
            StopPivotMove();

            if (!follow)
            {
                pivotFollowsPlayer = false;
                return;
            }

            //already following: nothing to change (and don't restart a glide that is already happening).
            if (pivotFollowsPlayer)
                return;

            pivotFollowsPlayer = true;
            blendStartPosition = pivot.position;
            blendTimer = 0f;
            currentBlendDuration = Mathf.Max(0f, blendDuration);
        }

        /// <summary>
        /// Starts following the player again. For UnityEvents that need a method with no settings.
        /// </summary>
        public void StartFollowingPlayer()
        {
            SetFollowPlayer(true);
        }

        /// <summary>
        /// Stops following the player and holds the camera where it is. For UnityEvents that need a method with no settings.
        /// </summary>
        public void StopFollowingPlayer()
        {
            SetFollowPlayer(false);
        }

        /// <summary>
        /// Stops following the player and glides the camera over to look at a spot. Call SetFollowPlayer(true) to come back.
        /// </summary>
        /// <param name="moveDuration">Seconds for the glide. Leave it out (or below 0) to use Follow Blend Duration; 0 snaps.</param>
        public void MovePivotTo(Vector3 position, float moveDuration = -1f)
        {
            if (pivot == null)
                return;

            SetFollowPlayer(false);

            if (moveDuration < 0f)
                moveDuration = followBlendDuration;

            if (moveDuration <= 0f)
            {
                pivot.position = position;
                return;
            }

            pivotMoveTween = pivot.DOMove(position, moveDuration).SetEase(Ease.InOutCubic);
        }

        /// <summary>
        /// MovePivotTo for UnityEvents: drag in any object and the camera glides over to it.
        /// </summary>
        public void MovePivotTo(Transform target)
        {
            if (target != null)
                MovePivotTo(target.position);
        }

        void StopPivotMove()
        {
            if (pivotMoveTween != null && pivotMoveTween.IsActive())
                pivotMoveTween.Kill();

            pivotMoveTween = null;
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
