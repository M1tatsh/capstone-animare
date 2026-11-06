using System.Collections;
using Data.Scene;
using DG.Tweening;
using Gameplay.Player;
using UnityEngine;

namespace UI.Behaviors
{
    /// <summary>
    /// Custom camera script for the Title Screen. It holds the camera at a specific point until the game starts, then smoothly transitions to the gameplay 
    /// camera position and rotation.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class TitleCameraShot : MonoBehaviour
    {
        [SerializeField] private Transform titleCameraPoint;
        [SerializeField] private float titleOrthographicSize = 0f;

        [Header("Move To Gameplay")]
        [SerializeField] private float moveDuration = 2f;
        [SerializeField] private float moveDelay = 0f;
        [SerializeField] private Ease moveEase = Ease.InOutCubic;
        [SerializeField] private bool freezePlayerWhileMoving = true;

        private Camera mainCamera;
        private Vector3 gameplayLocalPosition;
        private Quaternion gameplayLocalRotation;
        private float gameplayOrthographicSize;

        private bool isHoldingTitleShot;
        private Sequence moveSequence;
        private PlayerMovement frozenPlayer;

        public bool IsMoving => moveSequence != null && moveSequence.IsActive();

        IEnumerator Start()
        {
            if (titleCameraPoint == null || GameFlow.HasStarted || FindAnyObjectByType<TitleMenuBehavior>() == null)
                yield break;

            yield return null;
            mainCamera = Camera.main;

            if (mainCamera == null)
                yield break;

            Transform cameraTransform = mainCamera.transform;
            gameplayLocalPosition = cameraTransform.localPosition;
            gameplayLocalRotation = cameraTransform.localRotation;
            gameplayOrthographicSize = mainCamera.orthographicSize;

            if (GameFlow.HasStarted)
                yield break;

            isHoldingTitleShot = true;
            ApplyTitleShot();

            GameFlow.Started += OnGameStarted;
        }

        void LateUpdate()
        {
            if (isHoldingTitleShot)
                ApplyTitleShot();
        }

        void OnDestroy()
        {
            GameFlow.Started -= OnGameStarted;

            if (moveSequence != null && moveSequence.IsActive())
                moveSequence.Kill();

            UnfreezePlayer();
        }

        private void ApplyTitleShot()
        {
            if (mainCamera == null)
                return;

            mainCamera.transform.SetPositionAndRotation(titleCameraPoint.position, titleCameraPoint.rotation);

            if (titleOrthographicSize > 0f)
                mainCamera.orthographicSize = titleOrthographicSize;
        }

        private void OnGameStarted()
        {
            GameFlow.Started -= OnGameStarted;

            if (!isHoldingTitleShot || mainCamera == null)
                return;

            isHoldingTitleShot = false;
            ApplyTitleShot();

            if (freezePlayerWhileMoving)
            {
                frozenPlayer = FindAnyObjectByType<PlayerMovement>();

                if (frozenPlayer != null)
                    frozenPlayer.SetFrozen(true);
            }

            Transform cameraTransform = mainCamera.transform;

            moveSequence = DOTween.Sequence().SetTarget(this);
            moveSequence.AppendInterval(moveDelay);
            moveSequence.Append(cameraTransform.DOLocalMove(gameplayLocalPosition, moveDuration).SetEase(moveEase));
            moveSequence.Join(cameraTransform.DOLocalRotateQuaternion(gameplayLocalRotation, moveDuration).SetEase(moveEase));

            if (titleOrthographicSize > 0f)
                moveSequence.Join(mainCamera.DOOrthoSize(gameplayOrthographicSize, moveDuration).SetEase(moveEase));

            moveSequence.OnComplete(OnMoveFinished);
        }

        private void OnMoveFinished()
        {
            Transform cameraTransform = mainCamera.transform;
            cameraTransform.localPosition = gameplayLocalPosition;
            cameraTransform.localRotation = gameplayLocalRotation;
            mainCamera.orthographicSize = gameplayOrthographicSize;

            UnfreezePlayer();
        }

        private void UnfreezePlayer()
        {
            if (frozenPlayer != null)
                frozenPlayer.SetFrozen(false);

            frozenPlayer = null;
        }

        void OnDrawGizmosSelected()
        {
            if (titleCameraPoint == null)
                return;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(titleCameraPoint.position, 0.3f);
            Gizmos.DrawRay(titleCameraPoint.position, titleCameraPoint.forward * 2f);
        }
    }
}
