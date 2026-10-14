using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Progression
{
    /// <summary>
    /// Counts the collectibles the player has picked up in this stage (one per scene).
    /// When enough are collected, the stage counts as unlocked, and exit doors open.
    /// Progress is not saved: reloading the scene starts the count again.
    /// </summary>
    public class StageProgress : MonoBehaviour
    {
        [Tooltip("How many collectibles unlock the exit. 0 means all of the collectibles in this scene.")]
        [SerializeField] private int requiredCount = 0;

        [Tooltip("Runs once, the moment enough collectibles have been picked up.")]
        [SerializeField] private UnityEvent onUnlocked = new UnityEvent();

        private int collectedCount;
        private int totalCollectibles;

        /// <summary>
        /// Called every time the count changes, with (collected, required). Doors and the HUD listen to this.
        /// </summary>
        public event System.Action<int, int> CountChanged;

        public int CollectedCount => collectedCount;
        public int TotalCollectibles => totalCollectibles;

        public int RequiredCount
        {
            get { return requiredCount > 0 ? requiredCount : totalCollectibles; }
        }

        public bool IsUnlocked
        {
            get { return collectedCount >= RequiredCount; }
        }

        public int RemainingCount
        {
            get { return Mathf.Max(0, RequiredCount - collectedCount); }
        }

        void Awake()
        {
            //count the collectibles once, before any of them can be picked up and switched off.
            totalCollectibles = FindObjectsByType<StageCollectible>().Length;

            if (requiredCount > totalCollectibles)
            {
                Debug.LogWarning($"StageProgress: Required Count is {requiredCount}, but this scene only has {totalCollectibles} collectibles. The exit can never unlock.", this);
            }
        }

        void Start()
        {
            //let doors and the HUD show the starting numbers.
            CountChanged?.Invoke(collectedCount, RequiredCount);

            //a stage with nothing to collect is unlocked from the start.
            if (IsUnlocked)
                onUnlocked.Invoke();
        }

        /// <summary>
        /// Called by StageCollectible when the player picks it up.
        /// </summary>
        public void AddCollected()
        {
            bool wasUnlocked = IsUnlocked;

            collectedCount++;
            CountChanged?.Invoke(collectedCount, RequiredCount);

            if (!wasUnlocked && IsUnlocked)
                onUnlocked.Invoke();
        }
    }
}
