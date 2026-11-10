using System.Collections;
using System.Collections.Generic;
using Data.Audio;
using DG.Tweening;
using Gameplay.Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Gameplay.Rotation
{
    [DefaultExecutionOrder(100)]
    public class LevelFlyover : MonoBehaviour
    {
        #region inspector variables
        [Header("Level")]
        [Tooltip("Approximate center of the level to fly over.")]
        [SerializeField] private Transform levelCenter;
        [SerializeField] private Vector3 lookOffset = Vector3.zero;

        [Header("Starting View")]
        [SerializeField] private float startHeight = 25f;
        [SerializeField] private float startDistance = 20f;
        [SerializeField] private float startOrthographicSize = 0f;

        [Header("Flight")]
        [SerializeField] private float duration = 6f;
        [SerializeField] private float orbitDegrees = 360f;
        [SerializeField] private Ease flightEase = Ease.InOutSine;
        [Range(0.05f, 1f)]
        [SerializeField] private float settleFraction = 0.25f;

        [Header("Playing")]
        [SerializeField] private bool playOnlyFirstTime = true;
        [SerializeField] private string flyoverName = "";
        [SerializeField] private bool showEveryTime = false;
        [SerializeField] private bool allowSkipping = true;

        [Header("Player")]
        [SerializeField] private bool hidePlayerDuringFlyover = true;
        [SerializeField] private float playerAppearDelay = 0.2f;
        [SerializeField] private float playerAppearDuration = 0.35f;
        [SerializeField] private Ease playerAppearEase = Ease.OutBack;
        [SerializeField] private UnityEvent onPlayerAppeared = new UnityEvent();

        [Header("Sound")]
        [SerializeField] private bool muteWorldSounds = true;
        [SerializeField] private float worldSoundsFadeInDuration = 1f;
        #endregion

        #region private variables
        private static int playingCount;

        private Camera mainCamera;
        private Transform cameraTransform;
        private Transform cameraParent;
        private Vector3 gameplayLocalPosition;
        private Quaternion gameplayLocalRotation;
        private float gameplayOrthographicSize;

        private float startAngle;
        private float endAngle;
        private float endDistance;
        private float endHeight;

        private float timePlayed;
        private bool isPlaying;
        private PlayerMovement frozenPlayer;
        private PlayerInput pausedPlayerInput;

        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private Transform hiddenPlayerTransform;
        private Vector3 playerOriginalScale;
        private bool isRevealingPlayer;
        private bool willPlay;
        private bool hasMutedWorldSounds;
        private Tween appearTween;
        #endregion

        public static bool IsPlaying => playingCount > 0;

        private string SeenKey
        {
            get
            {
                string keyName = string.IsNullOrEmpty(flyoverName) ? gameObject.scene.name : flyoverName;
                return "FlyoverSeen_" + keyName;
            }
        }

        public bool HasBeenSeen => PlayerPrefs.GetInt(SeenKey, 0) == 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            playingCount = 0;
        }

        void Awake()
        {
            willPlay = !(playOnlyFirstTime && HasBeenSeen && !showEveryTime)
                && FindAnyObjectByType<TitleMenuBehavior>() == null;

            if (willPlay && muteWorldSounds)
            {
                AudioManager.SetWorldSoundsMuted(true);
                hasMutedWorldSounds = true;
            }
        }

        void Start()
        {
            if (!willPlay)
                return;

            mainCamera = Camera.main;

            if (mainCamera == null)
            {
                UnmuteWorldSounds(0f);
                return;
            }

            cameraTransform = mainCamera.transform;
            cameraParent = cameraTransform.parent;
            gameplayLocalPosition = cameraTransform.localPosition;
            gameplayLocalRotation = cameraTransform.localRotation;
            gameplayOrthographicSize = mainCamera.orthographicSize;

            Begin();
        }

        void OnDisable()
        {
            if (isPlaying)
                Finish();

            if (isRevealingPlayer)
                CompleteReveal();

            UnmuteWorldSounds(0f);
        }

        void OnDestroy()
        {
            DOTween.Kill(this);
        }

        private void Begin()
        {
            isPlaying = true;
            playingCount++;
            timePlayed = 0f;

            frozenPlayer = FindAnyObjectByType<PlayerMovement>();

            if (frozenPlayer != null)
            {
                frozenPlayer.SetFrozen(true);
                pausedPlayerInput = frozenPlayer.GetComponent<PlayerInput>();

                if (pausedPlayerInput != null)
                    pausedPlayerInput.DeactivateInput();

                if (hidePlayerDuringFlyover)
                    HidePlayer(frozenPlayer);
            }

            UpdateCamera();
        }

        private void HidePlayer(PlayerMovement player)
        {
            hiddenRenderers.Clear();
            hiddenPlayerTransform = player.transform;
            playerOriginalScale = hiddenPlayerTransform.localScale;

            foreach (Renderer playerRenderer in player.GetComponentsInChildren<Renderer>(true))
            {
                if (!playerRenderer.enabled)
                    continue;

                playerRenderer.enabled = false;
                hiddenRenderers.Add(playerRenderer);
            }
        }

        private IEnumerator RevealPlayer()
        {
            isRevealingPlayer = true;

            if (playerAppearDelay > 0f)
                yield return new WaitForSeconds(playerAppearDelay);

            ShowHiddenRenderers();

            if (hiddenPlayerTransform != null && playerAppearDuration > 0f)
            {
                hiddenPlayerTransform.localScale = Vector3.zero;
                appearTween = hiddenPlayerTransform.DOScale(playerOriginalScale, playerAppearDuration)
                    .SetEase(playerAppearEase)
                    .SetTarget(this);

                yield return appearTween.WaitForCompletion();
            }

            CompleteReveal();
        }

        private void ShowHiddenRenderers()
        {
            foreach (Renderer playerRenderer in hiddenRenderers)
            {
                if (playerRenderer != null)
                    playerRenderer.enabled = true;
            }

            hiddenRenderers.Clear();
        }

        private void CompleteReveal()
        {
            if (appearTween != null && appearTween.IsActive())
                appearTween.Kill();

            ShowHiddenRenderers();

            if (hiddenPlayerTransform != null)
                hiddenPlayerTransform.localScale = playerOriginalScale;

            hiddenPlayerTransform = null;
            isRevealingPlayer = false;

            ReleasePlayer();
            onPlayerAppeared.Invoke();
        }

        private void ReleasePlayer()
        {
            if (frozenPlayer != null)
                frozenPlayer.SetFrozen(false);

            if (pausedPlayerInput != null)
                pausedPlayerInput.ActivateInput();

            frozenPlayer = null;
            pausedPlayerInput = null;
            playingCount = Mathf.Max(0, playingCount - 1);

            UnmuteWorldSounds(isActiveAndEnabled ? worldSoundsFadeInDuration : 0f);
        }

        private void UnmuteWorldSounds(float fadeDuration)
        {
            if (!hasMutedWorldSounds)
                return;

            hasMutedWorldSounds = false;
            AudioManager.SetWorldSoundsMuted(false, fadeDuration);
        }

        void LateUpdate()
        {
            if (!isPlaying)
                return;

            if (allowSkipping && Time.timeScale > 0f && timePlayed > 0.25f && WasSkipPressed())
            {
                Finish();
                return;
            }

            timePlayed += Time.deltaTime;

            if (timePlayed >= duration)
            {
                Finish();
                return;
            }

            UpdateCamera();
        }

        private void UpdateCamera()
        {
            Vector3 center = LevelCenterPosition;
            Vector3 gameplayPosition = GameplayWorldPosition;
            Quaternion gameplayRotation = GameplayWorldRotation;
            Vector3 centerToGameplay = gameplayPosition - center;
            endDistance = new Vector2(centerToGameplay.x, centerToGameplay.z).magnitude;
            endHeight = centerToGameplay.y;
            endAngle = Mathf.Atan2(centerToGameplay.z, centerToGameplay.x) * Mathf.Rad2Deg;
            startAngle = endAngle - orbitDegrees;

            float progress = duration > 0f ? Mathf.Clamp01(timePlayed / duration) : 1f;
            float easedProgress = DOVirtual.EasedValue(0f, 1f, progress, flightEase);

            float angle = Mathf.Lerp(startAngle, endAngle, easedProgress) * Mathf.Deg2Rad;
            float distance = Mathf.Lerp(startDistance, endDistance, easedProgress);
            float height = Mathf.Lerp(startHeight, endHeight, easedProgress);

            Vector3 position = center + new Vector3(Mathf.Cos(angle) * distance, height, Mathf.Sin(angle) * distance);

            Vector3 toLookPoint = center + lookOffset - position;
            Quaternion lookRotation = toLookPoint.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(toLookPoint, Vector3.up)
                : gameplayRotation;

            float settleStart = 1f - settleFraction;
            float settleProgress = Mathf.Clamp01((progress - settleStart) / settleFraction);
            settleProgress = Mathf.SmoothStep(0f, 1f, settleProgress);

            cameraTransform.SetPositionAndRotation(position, Quaternion.Slerp(lookRotation, gameplayRotation, settleProgress));

            if (startOrthographicSize > 0f)
                mainCamera.orthographicSize = Mathf.Lerp(startOrthographicSize, gameplayOrthographicSize, easedProgress);
        }

        private bool WasSkipPressed()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                return true;

            if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame))
                return true;

            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        public void Skip()
        {
            if (isPlaying)
                Finish();
        }

        private void Finish()
        {
            isPlaying = false;

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = gameplayLocalPosition;
                cameraTransform.localRotation = gameplayLocalRotation;
                mainCamera.orthographicSize = gameplayOrthographicSize;
            }

            if (playOnlyFirstTime)
            {
                PlayerPrefs.SetInt(SeenKey, 1);
                PlayerPrefs.Save();
            }

            if (hiddenPlayerTransform != null && isActiveAndEnabled)
                StartCoroutine(RevealPlayer());
            else if (hiddenPlayerTransform != null)
                CompleteReveal();
            else
                ReleasePlayer();
        }

        private Vector3 LevelCenterPosition => levelCenter != null ? levelCenter.position : transform.position;

        private Vector3 GameplayWorldPosition => cameraParent != null ? cameraParent.TransformPoint(gameplayLocalPosition) : gameplayLocalPosition;

        private Quaternion GameplayWorldRotation => cameraParent != null ? cameraParent.rotation * gameplayLocalRotation : gameplayLocalRotation;

        [ContextMenu("Forget That This Flyover Was Seen")]
        private void ForgetSeen()
        {
            PlayerPrefs.DeleteKey(SeenKey);
        }

        void OnDrawGizmosSelected()
        {
            Vector3 center = LevelCenterPosition;
            Vector3 startPoint = center + new Vector3(startDistance, startHeight, 0f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center + lookOffset, 0.5f);
            Gizmos.DrawLine(center, center + Vector3.up * startHeight);

            Vector3 previousPoint = startPoint;

            for (int step = 1; step <= 32; step++)
            {
                float angle = step / 32f * Mathf.PI * 2f;
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * startDistance, startHeight, Mathf.Sin(angle) * startDistance);
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }
    }
}
