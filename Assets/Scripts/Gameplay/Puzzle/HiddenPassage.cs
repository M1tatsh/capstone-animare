using System.Collections.Generic;
using Data.Audio;
using DG.Tweening;
using Gameplay.Player;
using Gameplay.Rotation;
using UnityEngine;

namespace Gameplay.Puzzle
{
    public class HiddenPassage : MonoBehaviour
    {
        #region variables
        [SerializeField] private bool startOpen = false;
        [SerializeField] private float openDuration = 0.3f;
        [SerializeField] private float closeDuration = 0.3f;
        [SerializeField] private Ease openEase = Ease.InBack;
        [SerializeField] private Ease closeEase = Ease.OutBack;

        [Header("Sound")]
        [SerializeField] private SoundEffect openSound;
        [SerializeField] private SoundEffect closeSound;
        private readonly List<Transform> passageBlocks = new List<Transform>();
        private readonly List<Vector3> blockScales = new List<Vector3>();
        private BlockGrid grid;
        private PlayerMovement player;
        private HoldableBlock[] holdableBlocks;
        private bool isWaitingToClose;
        #endregion
        public bool IsOpen { get; private set; }

        void Awake()
        {
            foreach (Transform child in transform)
            {
                passageBlocks.Add(child);
                blockScales.Add(child.localScale);
            }

            if (startOpen)
            {
                IsOpen = true;

                foreach (Transform block in passageBlocks)
                    block.gameObject.SetActive(false);
            }
        }

        void Start()
        {
            grid = FindAnyObjectByType<BlockGrid>();
            holdableBlocks = FindObjectsByType<HoldableBlock>();

            if (passageBlocks.Count == 0)
                Debug.LogWarning("HiddenPassage: Put this on an empty object whose children are the wall blocks to remove.", this);

            if (startOpen)
                RebuildGrid();
        }

        void OnDestroy()
        {
            DOTween.Kill(this);
        }

        void Update()
        {
            if (isWaitingToClose && !IsSomethingInPassage())
            {
                isWaitingToClose = false;
                CloseNow();
            }
        }

        public void Open()
        {
            isWaitingToClose = false;

            if (IsOpen)
                return;

            IsOpen = true;
            DOTween.Kill(this);
            AudioManager.PlaySoundEffect(openSound);

            if (openDuration <= 0f)
            {
                FinishOpening();
                return;
            }

            foreach (Transform block in passageBlocks)
                block.DOScale(Vector3.zero, openDuration).SetEase(openEase).SetTarget(this);

            //virtual tween for the delay
            DOVirtual.DelayedCall(openDuration, FinishOpening).SetTarget(this);
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            if (IsSomethingInPassage())
            {
                isWaitingToClose = true;
                return;
            }

            CloseNow();
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        private void FinishOpening()
        {
            foreach (Transform block in passageBlocks)
                block.gameObject.SetActive(false);

            RebuildGrid();
        }

        private void CloseNow()
        {
            IsOpen = false;
            DOTween.Kill(this);
            AudioManager.PlaySoundEffect(closeSound);

            for (int blockIndex = 0; blockIndex < passageBlocks.Count; blockIndex++)
            {
                Transform block = passageBlocks[blockIndex];
                bool wasHidden = !block.gameObject.activeSelf;

                block.gameObject.SetActive(true);

                if (closeDuration <= 0f)
                {
                    block.localScale = blockScales[blockIndex];
                    continue;
                }

                if (wasHidden)
                    block.localScale = Vector3.zero;

                block.DOScale(blockScales[blockIndex], closeDuration).SetEase(closeEase).SetTarget(this);
            }

            RebuildGrid();
        }

        private void RebuildGrid()
        {
            if (grid == null)
                grid = FindAnyObjectByType<BlockGrid>();

            if (grid != null)
                grid.Rebuild();
        }

        private bool IsSomethingInPassage()
        {
            return IsPlayerInPassage() || IsHoldableBlockInPassage();
        }

        private bool IsPlayerInPassage()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerMovement>();

            if (player == null)
                return false;

            Vector3 screenRight = player.GetMoveAxis();
            Vector3 playerPosition = player.transform.position;
            float playerHorizontal = Vector3.Dot(playerPosition, screenRight);
            Vector2 playerHalfSize = player.HalfSize;
            foreach (Transform block in passageBlocks)
            {
                Vector3Int blockCell = Vector3Int.RoundToInt(block.position);
                float blockHorizontal = Vector3.Dot(blockCell, screenRight);

                bool overlapsSideways = Mathf.Abs(playerHorizontal - blockHorizontal) < 0.5f + playerHalfSize.x;
                bool overlapsUpAndDown = Mathf.Abs(playerPosition.y - blockCell.y) < 0.5f + playerHalfSize.y;

                if (overlapsSideways && overlapsUpAndDown)
                    return true;
            }

            return false;
        }

        private bool IsHoldableBlockInPassage()
        {
            foreach (HoldableBlock holdableBlock in holdableBlocks)
            {
                if (holdableBlock == null || !holdableBlock.gameObject.activeInHierarchy)
                    continue;

                Vector3Int holdableCell = Vector3Int.RoundToInt(holdableBlock.transform.position);
                foreach (Transform block in passageBlocks)
                {
                    if (Vector3Int.RoundToInt(block.position) == holdableCell)
                        return true;
                }
            }

            return false;
        }
    }
}
