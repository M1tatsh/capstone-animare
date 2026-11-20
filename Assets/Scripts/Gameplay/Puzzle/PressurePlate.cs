using Data.Audio;
using DG.Tweening;
using Gameplay.Player;
using Gameplay.Rotation;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Puzzle
{
    public class PressurePlate : MonoBehaviour
    {
        #region variables
        [Header("Pressing")]
        [SerializeField] private bool releaseWhenNothingIsOnIt = false;
        [SerializeField] private bool onlyBlocksCanPress = false;
        [SerializeField] private float feetHeightTolerance = 0.2f;
        [SerializeField] private Transform buttonTop;

        [Header("Sound")]
        [SerializeField] private SoundEffect pressSound;
        [SerializeField] private SoundEffect releaseSound;

        [Header("Events")]
        [SerializeField] private UnityEvent onPressed = new UnityEvent();
        [SerializeField] private UnityEvent onReleased = new UnityEvent();


        private float pressedDepth = 0.1f;
        private float pressDuration = 0.1f;
        private PlayerMovement player;
        private HoldableBlock[] holdableBlocks;
        private Vector3 buttonTopRestingPosition;
        #endregion

        public bool IsPressed { get; private set; }
        public Vector3Int Cell => Vector3Int.RoundToInt(transform.position);

        void Start()
        {
            holdableBlocks = FindObjectsByType<HoldableBlock>();

            if (buttonTop != null)
                buttonTopRestingPosition = buttonTop.localPosition;
        }

        void OnDestroy()
        {
            DOTween.Kill(this);
        }

        void Update()
        {
            bool somethingIsOnIt = IsBlockOnButton() 
            || (!onlyBlocksCanPress && IsPlayerOnButton());

            if (somethingIsOnIt && !IsPressed)
                Press();
            else if (!somethingIsOnIt 
                    && IsPressed 
                    && releaseWhenNothingIsOnIt)
                Release();
        }

        private bool IsBlockOnButton()
        {
            Vector3Int buttonCell = Cell;

            foreach (HoldableBlock block in holdableBlocks)
            {
                if (block == null || block.IsHeld || !block.gameObject.activeInHierarchy)
                    continue;

                if (Vector3Int.RoundToInt(block.transform.position) == buttonCell)
                    return true;
            }

            return false;
        }

        private bool IsPlayerOnButton()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerMovement>();

            if (player == null || !player.IsGrounded)
                return false;

            Vector3 playerPosition = player.transform.position;
            Vector3Int buttonCell = Cell;
            float reach = 0.5f + player.HalfSize.x;

            bool isOverButton = Mathf.Abs(playerPosition.x - buttonCell.x) < reach
                && Mathf.Abs(playerPosition.z - buttonCell.z) < reach;
            float feetHeight = playerPosition.y - player.HalfSize.y;
            float buttonBottom = buttonCell.y - 0.5f;
            bool feetAreOnButton = Mathf.Abs(feetHeight - buttonBottom) <= feetHeightTolerance;

            return isOverButton && feetAreOnButton;
        }

        private void Press()
        {
            IsPressed = true;
            MoveButtonTop(true);
            
            AudioManager.PlaySoundEffect(pressSound);
            onPressed.Invoke();
        }

        private void Release()
        {
            IsPressed = false;
            MoveButtonTop(false);

            AudioManager.PlaySoundEffect(releaseSound);
            onReleased.Invoke();
        }

        private void MoveButtonTop(bool pressed)
        {
            if (buttonTop == null)
                return;

            DOTween.Kill(this);
            Vector3 targetPosition = pressed ? buttonTopRestingPosition + Vector3.down * pressedDepth : buttonTopRestingPosition;
            buttonTop.DOLocalMove(targetPosition, pressDuration).SetTarget(this);
        }
    }
}
