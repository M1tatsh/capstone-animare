using DG.Tweening;
using Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Rotation
{
    [DefaultExecutionOrder(110)]
    public class CameraLookAround : MonoBehaviour
    {
        public enum LookControl
        {
            MouseCursor,
            DirectionalInput
        }

        [Header("Control")]
        [SerializeField] private LookControl lookControl = LookControl.MouseCursor;
        [Range(0f, 0.9f)]
        [SerializeField] private float mouseDeadZone = 0.1f;
        [SerializeField] private bool showCursorWhileLooking = true;

        [Header("Limits")]
        [SerializeField] private Vector2 maximumOffset = new Vector2(8f, 5f);

        [Header("Movement")]
        [SerializeField] private float panSpeed = 6f;
        [SerializeField] private float panSmoothTime = 0.12f;
        [SerializeField] private float returnDuration = 0.35f;
        [SerializeField] private Ease returnEase = Ease.OutCubic;

        [Header("Rules")]
        [SerializeField] private bool onlyWhenGrounded = true;

        private Camera mainCamera;
        private Transform cameraTransform;
        private Vector3 baseLocalPosition;

        private Vector2 offset;
        private Vector2 targetOffset;
        private Vector2 offsetVelocity;
        private Vector2 lookInput;

        private bool isLooking;
        private bool isReturning;
        private Tween returnTween;
        private PlayerMovement frozenPlayer;
        private ViewRotator viewRotator;

        private bool cursorWasVisible;
        private CursorLockMode cursorLockBeforeLooking;

        public bool IsLooking => isLooking;
        public bool IsActive => isLooking || isReturning;
        public Vector2 Offset => offset;

        void OnDisable()
        {
            if (isLooking)
                StopLooking();

            if (returnTween != null && returnTween.IsActive())
                returnTween.Complete();
        }

        void OnDestroy()
        {
            DOTween.Kill(this);
        }

        public bool TryStartLooking()
        {
            if (isLooking)
                return true;

            if (!isActiveAndEnabled || LevelFlyover.IsPlaying)
                return false;

            if (viewRotator == null)
                viewRotator = FindAnyObjectByType<ViewRotator>();

            if (viewRotator != null && viewRotator.IsRotating)
                return false;

            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();

            if (onlyWhenGrounded && player != null && !player.IsGrounded)
                return false;

            mainCamera = Camera.main;

            if (mainCamera == null)
                return false;

            if (isReturning)
            {
                if (returnTween != null && returnTween.IsActive())
                    returnTween.Kill();

                isReturning = false;
            }
            else
            {
                cameraTransform = mainCamera.transform;
                baseLocalPosition = cameraTransform.localPosition;
                offset = Vector2.zero;
            }

            targetOffset = offset;
            offsetVelocity = Vector2.zero;
            lookInput = Vector2.zero;
            isLooking = true;

            if (lookControl == LookControl.MouseCursor)
                UnlockCursor();

            frozenPlayer = player;

            if (frozenPlayer != null)
                frozenPlayer.SetFrozen(true);

            return true;
        }

        public void StopLooking()
        {
            if (!isLooking)
                return;

            isLooking = false;
            isReturning = true;
            lookInput = Vector2.zero;

            if (frozenPlayer != null)
                frozenPlayer.SetFrozen(false);

            frozenPlayer = null;

            if (lookControl == LookControl.MouseCursor)
                RestoreCursor();

            returnTween = DOTween.To(() => offset, value => offset = value, Vector2.zero, returnDuration)
                .SetEase(returnEase)
                .SetTarget(this)
                .OnComplete(FinishReturning);
        }

        public void SetLookInput(Vector2 input)
        {
            lookInput = Vector2.ClampMagnitude(input, 1f);
        }

        void LateUpdate()
        {
            if (!isLooking && !isReturning)
                return;

            if (cameraTransform == null)
            {
                isLooking = false;
                isReturning = false;
                return;
            }

            if (isLooking)
            {
                if (lookControl == LookControl.MouseCursor)
                {
                    Vector2 cursorDirection = GetCursorDirectionFromCenter();
                    targetOffset = new Vector2(cursorDirection.x * maximumOffset.x, cursorDirection.y * maximumOffset.y);
                }
                else
                {
                    targetOffset += lookInput * panSpeed * Time.deltaTime;
                    targetOffset.x = Mathf.Clamp(targetOffset.x, -maximumOffset.x, maximumOffset.x);
                    targetOffset.y = Mathf.Clamp(targetOffset.y, -maximumOffset.y, maximumOffset.y);
                }

                offset = Vector2.SmoothDamp(offset, targetOffset, ref offsetVelocity, panSmoothTime);
            }

            ApplyOffset();
        }

        private Vector2 GetCursorDirectionFromCenter()
        {
            if (Mouse.current == null || Screen.width <= 0 || Screen.height <= 0)
                return Vector2.zero;

            Vector2 cursorPosition = Mouse.current.position.ReadValue();
            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;

            float horizontal = Mathf.Clamp((cursorPosition.x - screenCenter.x) / screenCenter.x, -1f, 1f);
            float vertical = Mathf.Clamp((cursorPosition.y - screenCenter.y) / screenCenter.y, -1f, 1f);

            return new Vector2(ApplyDeadZone(horizontal), ApplyDeadZone(vertical));
        }

        private float ApplyDeadZone(float value)
        {
            float size = Mathf.Abs(value);

            if (size <= mouseDeadZone)
                return 0f;

            float rescaled = (size - mouseDeadZone) / (1f - mouseDeadZone);
            return Mathf.Sign(value) * rescaled;
        }

        private void UnlockCursor()
        {
            cursorWasVisible = Cursor.visible;
            cursorLockBeforeLooking = Cursor.lockState;

            Cursor.lockState = CursorLockMode.Confined;

            if (showCursorWhileLooking)
                Cursor.visible = true;
        }

        private void RestoreCursor()
        {
            Cursor.lockState = cursorLockBeforeLooking;
            Cursor.visible = cursorWasVisible;
        }

        private void ApplyOffset()
        {
            Vector3 worldOffset = cameraTransform.right * offset.x + cameraTransform.up * offset.y;
            Transform cameraParent = cameraTransform.parent;
            Vector3 localOffset = cameraParent != null ? cameraParent.InverseTransformVector(worldOffset) : worldOffset;

            cameraTransform.localPosition = baseLocalPosition + localOffset;
        }

        private void FinishReturning()
        {
            isReturning = false;
            offset = Vector2.zero;

            if (cameraTransform != null)
                cameraTransform.localPosition = baseLocalPosition;
        }

        void OnDrawGizmosSelected()
        {
            Camera sceneCamera = Camera.main;

            if (sceneCamera == null)
                return;

            Transform target = sceneCamera.transform;
            Vector3 center = target.position + target.forward * 5f;
            Vector3 right = target.right * maximumOffset.x;
            Vector3 up = target.up * maximumOffset.y;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(center - right - up, center + right - up);
            Gizmos.DrawLine(center + right - up, center + right + up);
            Gizmos.DrawLine(center + right + up, center - right + up);
            Gizmos.DrawLine(center - right + up, center - right - up);
        }
    }
}
