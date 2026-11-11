using System.Collections;
using Data.Scene;
using DG.Tweening;
using Gameplay.Dialogue;
using Gameplay.Player;
using UnityEngine;

namespace UI.Behaviors
{
    /// <summary>
    /// Camera script for custom camera shots.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class DynamicCameraShot : MonoBehaviour
    {
        #region variables
        [SerializeField] private Transform startPoint;
        [SerializeField] private float initialOrthographicSize = 0f;

        [Header("When To Use")]
        [Tooltip("Use this camera shot on the title screen. Still required for the back-end logic for how the title shot works.")]
        [SerializeField] private bool useOnTitleScreen = true;
        [Tooltip("Use this camera shot when the scene starts.")]
        [SerializeField] private bool useOnSceneStart = false;

        [Header("Return To Player")]
        [SerializeField] private float returnAfterSeconds = 0f;
        [Tooltip("Return to the player after this tutorial dialogue finishes.")]
        [SerializeField] private SceneTutorial returnWithTutorial;
        [Tooltip("Return to the player after the specified tutorial page finishes.")]
        [SerializeField] private int returnOnTutorialPage = -1;
        [SerializeField] private bool skipIfTutorialAlreadySeen = true;
        [SerializeField] private bool freezePlayerWhileHolding = true;

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
        public bool IsHoldingShot => isHoldingTitleShot;
        public SceneTutorial ReturnWithTutorial => returnWithTutorial;
        #endregion

        IEnumerator Start()
        {
            if (startPoint == null)
                yield break;

            bool titleIsShowing = !GameFlow.HasStarted && FindAnyObjectByType<TitleMenuBehavior>() != null;

            if (titleIsShowing && !useOnTitleScreen)
                yield break;

            if (!titleIsShowing && !useOnSceneStart)
                yield break;

            if (!titleIsShowing && skipIfTutorialAlreadySeen && returnWithTutorial != null
                && returnWithTutorial.HasBeenSeen && !returnWithTutorial.ShowsEveryTime)
                yield break;

            yield return null;
            mainCamera = Camera.main;

            if (mainCamera == null)
                yield break;

            Transform cameraTransform = mainCamera.transform;
            gameplayLocalPosition = cameraTransform.localPosition;
            gameplayLocalRotation = cameraTransform.localRotation;
            gameplayOrthographicSize = mainCamera.orthographicSize;

            if (titleIsShowing)
            {
                if (GameFlow.HasStarted)
                    yield break;

                isHoldingTitleShot = true;
                ApplyTitleShot();

                GameFlow.Started += OnGameStarted;
                yield break;
            }

            isHoldingTitleShot = true;
            ApplyTitleShot();

            if (freezePlayerWhileHolding)
                FreezePlayer();

            StartCoroutine(WaitToReturn());
        }

        private IEnumerator WaitToReturn()
        {
            float timeHeld = 0f;
            bool tutorialWasShown = false;
            ContextDialogueManager dialogueManager = FindAnyObjectByType<ContextDialogueManager>();

            while (isHoldingTitleShot)
            {
                timeHeld += Time.deltaTime;

                if (returnAfterSeconds > 0f && timeHeld >= returnAfterSeconds)
                    break;

                if (returnWithTutorial != null && dialogueManager != null)
                {
                    bool tutorialIsShowing = dialogueManager.ActiveTutorial == returnWithTutorial;

                    if (tutorialIsShowing)
                    {
                        tutorialWasShown = true;

                        if (returnOnTutorialPage >= 0 && dialogueManager.CurrentPageIndex >= returnOnTutorialPage)
                            break;
                    }
                    else if (tutorialWasShown)
                    {
                        break;
                    }
                }

                yield return null;
            }

            ReturnToPlayer();
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

            mainCamera.transform.SetPositionAndRotation(startPoint.position, startPoint.rotation);

            if (initialOrthographicSize > 0f)
                mainCamera.orthographicSize = initialOrthographicSize;
        }

        private void OnGameStarted()
        {
            GameFlow.Started -= OnGameStarted;
            ReturnToPlayer();
        }

        public void ReturnToPlayer()
        {
            if (!isHoldingTitleShot || mainCamera == null)
                return;

            isHoldingTitleShot = false;
            ApplyTitleShot();

            if (freezePlayerWhileMoving)
                FreezePlayer();

            Transform cameraTransform = mainCamera.transform;

            moveSequence = DOTween.Sequence().SetTarget(this);
            moveSequence.AppendInterval(moveDelay);
            moveSequence.Append(cameraTransform.DOLocalMove(gameplayLocalPosition, moveDuration).SetEase(moveEase));
            moveSequence.Join(cameraTransform.DOLocalRotateQuaternion(gameplayLocalRotation, moveDuration).SetEase(moveEase));

            if (initialOrthographicSize > 0f)
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

        private void FreezePlayer()
        {
            if (frozenPlayer == null)
                frozenPlayer = FindAnyObjectByType<PlayerMovement>();

            if (frozenPlayer != null)
                frozenPlayer.SetFrozen(true);
        }

        private void UnfreezePlayer()
        {
            if (frozenPlayer != null)
                frozenPlayer.SetFrozen(false);

            frozenPlayer = null;
        }

        void OnDrawGizmosSelected()
        {
            if (startPoint == null)
                return;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(startPoint.position, 0.3f);
            Gizmos.DrawRay(startPoint.position, startPoint.forward * 2f);
        }
    }
}
