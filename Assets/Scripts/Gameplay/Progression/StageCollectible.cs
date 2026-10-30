using System.Globalization;
using DG.Tweening;
using Gameplay.Player;
using Gameplay.Rotation;
using Gameplay.Save;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Progression
{
    /// <summary>
    /// Something the player picks up by touching it. Each one adds to the scene's StageProgress.
    ///
    /// How "touching" works with the flattened world:
    ///   - Normally the player collects it when they overlap it on screen, whatever its depth,
    ///     the same way the flattened world lets them stand on blocks at any depth.
    ///     A collectible hidden behind a building can't be picked up from in front of the building.
    ///   - While the player is behind geometry (background mode), the real 3D distance is used instead.
    /// </summary>
    public class StageCollectible : MonoBehaviour
    {
        [Tooltip("How close the player's center has to get, in world units.")]
        [SerializeField] private float pickupRadius = 0.75f;
        [Tooltip("Degrees per second the collectible spins while waiting to be picked up. 0 for no spin.")]
        [SerializeField] private float spinSpeed = 90f;
        [Tooltip("How long the pick-up shrink takes, in seconds.")]
        [SerializeField] private float pickupDuration = 0.25f;
        [Tooltip("Runs when the player picks this up (for a sound or particle effect).")]
        [SerializeField] private UnityEvent onCollected = new UnityEvent();
        [Tooltip("Optional. The name this item is saved under. Leave empty to make one from the scene, " +
            "its place in the Hierarchy and its position (only set this if two items would get the same name).")]
        [SerializeField] private string saveIdOverride = "";

        //half the size of the box used to check whether a building hides this collectible.
        private const float HiddenCheckHalfSize = 0.25f;

        private StageProgress stageProgress;
        private PlayerMovement playerMovement;
        private BlockGrid grid;
        private ViewRotator viewRotator;
        private bool isCollected;
        private bool wasAlreadyCollected;
        private string saveId;

        /// <summary>
        /// Save File ID String
        /// </summary>
        public string SaveId => saveId;

        void Awake()
        {
            saveId = string.IsNullOrEmpty(saveIdOverride) ? MakeSaveId() : saveIdOverride;

            if (SaveManager.IsCollected(saveId))
            {
                
                isCollected = true;
                wasAlreadyCollected = true;


                StageProgress progress = FindAnyObjectByType<StageProgress>();

                if (progress != null)
                    progress.AddAlreadyCollected();
            }
        }

        /// <summary>
        /// Naming convention: Scene Name + Hierarchy Path + @position <br/>
        /// "LVL1_WIP/Collectibles/MemoryShard@(3.0, 2.0, 1.0)" like so
        /// </summary>
        private string MakeSaveId()
        {
            string hierarchyPath = transform.name;
            Transform parent = transform.parent;

            while (parent != null)
            {
                hierarchyPath = parent.name + "/" + hierarchyPath;
                parent = parent.parent;
            }

            Vector3 position = transform.position;
            string positionText = string.Format(CultureInfo.InvariantCulture, "({0:0.0}, {1:0.0}, {2:0.0})",
                position.x, position.y, position.z);

            return gameObject.scene.name + "/" + hierarchyPath + "@" + positionText;
        }

        void Start()
        {
            if (wasAlreadyCollected)
            {
                gameObject.SetActive(false);
                return;
            }

            stageProgress = FindAnyObjectByType<StageProgress>();
            playerMovement = FindAnyObjectByType<PlayerMovement>();
            grid = FindAnyObjectByType<BlockGrid>();
            viewRotator = FindAnyObjectByType<ViewRotator>();

            if (stageProgress == null)
                Debug.LogWarning("StageCollectible: There is no StageProgress in this scene, so picking this up won't count toward anything.", this);
        }

        void Update()
        {
            if (isCollected)
                return;

            if (spinSpeed != 0f)
                transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

            if (playerMovement == null)
                return;

            //the camera is between two views while rotating, so wait for the turn to finish.
            if (viewRotator != null && viewRotator.IsRotating)
                return;

            if (GetDistanceToPlayer() <= pickupRadius)
                Collect();
        }

        private float GetDistanceToPlayer()
        {
            Vector3 collectibleToPlayer = playerMovement.transform.position - transform.position;

            //behind geometry the player is really at their depth, so use the real distance.
            if (playerMovement.IsBehindGeometry)
                return collectibleToPlayer.magnitude;

            //a collectible hidden behind a building can't be reached from in front of it.
            if (IsHiddenBehindBuilding())
                return float.MaxValue;

            //otherwise only the distance on screen counts: depth is left out.
            Vector3 intoScreen = ViewAxes.Forward(playerMovement.CurrentView);
            return Vector3.ProjectOnPlane(collectibleToPlayer, intoScreen).magnitude;
        }

        private bool IsHiddenBehindBuilding()
        {
            if (grid == null)
                return false;

            int view = playerMovement.CurrentView;
            float horizontal = Vector3.Dot(transform.position, (Vector3)ViewAxes.Right(view));
            float vertical = transform.position.y;
            int depth = Mathf.RoundToInt(Vector3.Dot(transform.position, (Vector3)ViewAxes.Forward(view)));

            return grid.BehindBuilding(view,
                horizontal - HiddenCheckHalfSize, horizontal + HiddenCheckHalfSize,
                vertical - HiddenCheckHalfSize, vertical + HiddenCheckHalfSize,
                depth);
        }

        private void Collect()
        {
            isCollected = true;
            SaveManager.MarkCollected(saveId);

            if (stageProgress != null)
                stageProgress.AddCollected();

            onCollected.Invoke();

            //shrink and float up a little, then switch off.
            Sequence pickupSequence = DOTween.Sequence().SetTarget(this);
            pickupSequence.Join(transform.DOScale(Vector3.zero, pickupDuration).SetEase(Ease.InBack));
            pickupSequence.Join(transform.DOMoveY(transform.position.y + 0.5f, pickupDuration));
            pickupSequence.OnComplete(() => gameObject.SetActive(false));
        }

        void OnDestroy()
        {
            DOTween.Kill(this);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
