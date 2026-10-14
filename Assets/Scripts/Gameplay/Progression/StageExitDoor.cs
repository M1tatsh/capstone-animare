using Gameplay.Dialogue;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Progression
{
    /// <summary>
    /// The door to the next stage. It stays locked until the scene's StageProgress has enough collectibles.
    /// Uses the Context Dialogue on the same object for its prompt, and NextSceneObject to load the next scene.
    /// Walk up to it and press Interact: locked, it says how many are left; unlocked, it loads the next stage.
    /// A scene without a StageProgress has the door unlocked from the start.
    /// </summary>
    [RequireComponent(typeof(ContextDialogue))]
    [RequireComponent(typeof(NextSceneObject))]
    public class StageExitDoor : MonoBehaviour
    {
        [Tooltip("Shown while locked. {0} is replaced with how many collectibles are still needed.")]
        [SerializeField] private string lockedPrompt = "Locked: find {0} more";
        [Tooltip("Shown once the door is unlocked.")]
        [SerializeField] private string unlockedPrompt = "[F] Enter";

        [Header("Visuals (optional)")]
        [Tooltip("Switched on while locked (for example a closed door or a padlock).")]
        [SerializeField] private GameObject lockedVisual;
        [Tooltip("Switched on once unlocked (for example an open door or a glow).")]
        [SerializeField] private GameObject unlockedVisual;

        [Tooltip("Runs once when the door unlocks (for a sound or an animation).")]
        [SerializeField] private UnityEvent onUnlocked = new UnityEvent();

        private ContextDialogue contextDialogue;
        private NextSceneObject nextSceneObject;
        private StageProgress stageProgress;
        private bool isUnlocked;

        void Awake()
        {
            contextDialogue = GetComponent<ContextDialogue>();
            nextSceneObject = GetComponent<NextSceneObject>();
            stageProgress = FindAnyObjectByType<StageProgress>();

            //pressing Interact at the door runs TryEnter, no Inspector wiring needed.
            contextDialogue.OnInteract.AddListener(TryEnter);

            if (stageProgress != null)
                stageProgress.CountChanged += OnCountChanged;
        }

        void Start()
        {
            if (stageProgress == null)
                SetUnlocked(true);
            else
                OnCountChanged(stageProgress.CollectedCount, stageProgress.RequiredCount);
        }

        void OnDestroy()
        {
            if (contextDialogue != null)
                contextDialogue.OnInteract.RemoveListener(TryEnter);

            if (stageProgress != null)
                stageProgress.CountChanged -= OnCountChanged;
        }

        private void OnCountChanged(int collected, int required)
        {
            bool nowUnlocked = collected >= required;

            if (!nowUnlocked)
            {
                contextDialogue.SetPromptText(string.Format(lockedPrompt, required - collected));
            }

            SetUnlocked(nowUnlocked);
        }

        private void SetUnlocked(bool unlocked)
        {
            bool justUnlocked = unlocked && !isUnlocked;
            isUnlocked = unlocked;

            if (unlocked)
                contextDialogue.SetPromptText(unlockedPrompt);

            if (lockedVisual != null)
                lockedVisual.SetActive(!unlocked);

            if (unlockedVisual != null)
                unlockedVisual.SetActive(unlocked);

            if (justUnlocked)
                onUnlocked.Invoke();
        }

        /// <summary>
        /// Called when the player presses Interact at the door.
        /// </summary>
        public void TryEnter()
        {
            if (!isUnlocked)
                return;

            nextSceneObject.LoadScene();
        }
    }
}
