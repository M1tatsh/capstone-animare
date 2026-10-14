using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Dialogue
{
    /// <summary>
    /// Tutorial pages that appear above the player the first time a scene is loaded.
    /// Put one on any GameObject in the scene (the scene also needs a ContextDialogueManager).
    /// The player steps through the pages with Interact. Once the last page is closed, the tutorial
    /// is remembered as seen with PlayerPrefs, so it does not come back, even after quitting the game.
    /// </summary>
    public class SceneTutorial : MonoBehaviour
    {
        [SerializeField] private List<DialoguePage> pages = new List<DialoguePage>();

        [Tooltip("Where the textbox points, measured from the player. The box sits just above this point.")]
        [SerializeField] private Vector3 textboxOffset = new Vector3(0f, 1f, 0f);

        [Tooltip("Seconds to wait after the scene loads before showing the tutorial.")]
        [SerializeField] private float startDelay = 0.5f;

        [Tooltip("Name used to remember that this tutorial was seen. Leave empty to use the scene's name. " +
            "Give each tutorial its own name if one scene has more than one.")]
        [SerializeField] private string tutorialName = "";

        [Tooltip("For testing: show the tutorial every time the scene loads, even if it was seen before.")]
        [SerializeField] private bool showEveryTime = false;

        public int PageCount => pages.Count;
        public Vector3 TextboxOffset => textboxOffset;

        //the PlayerPrefs key, for example "TutorialSeen_LVL_ROTATION".
        private string SeenKey
        {
            get
            {
                string keyName = string.IsNullOrEmpty(tutorialName) ? gameObject.scene.name : tutorialName;
                return "TutorialSeen_" + keyName;
            }
        }

        public bool HasBeenSeen
        {
            get { return PlayerPrefs.GetInt(SeenKey, 0) == 1; }
        }

        public DialoguePage GetPage(int pageIndex)
        {
            return pages[pageIndex];
        }

        //Start can wait: it runs over several frames, so the delay doesn't hold anything else up.
        IEnumerator Start()
        {
            if (pages.Count == 0 || (HasBeenSeen && !showEveryTime))
                yield break;

            yield return new WaitForSeconds(startDelay);

            ContextDialogueManager dialogueManager = FindAnyObjectByType<ContextDialogueManager>();

            if (dialogueManager == null)
            {
                Debug.LogWarning("SceneTutorial: There is no ContextDialogueManager in this scene, so the tutorial can't be shown.");
                yield break;
            }

            dialogueManager.ShowTutorial(this);
        }

        /// <summary>
        /// Called by ContextDialogueManager after the last page is closed.
        /// </summary>
        public void MarkAsSeen()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// For testing: right-click the component's header in the Inspector and pick this to see the tutorial again.
        /// </summary>
        [ContextMenu("Forget That This Tutorial Was Seen")]
        public void ForgetSeen()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.Save();
        }
    }
}
