using System.Collections.Generic;
using Data.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Gameplay.Dialogue
{
    /// <summary>
    /// One page of a conversation: some text, an image, or both.
    /// </summary>
    [System.Serializable]
    public class DialoguePage
    {
        [TextArea(2, 5)] public string text;
        public Sprite image;

        [Tooltip("Optional. If set, this page moves on when the player uses this action " +
            "(for example RotateCamera or Jump) instead of Interact. The [F] hint is hidden, so say what to press in the text.")]
        public InputActionReference advanceWithAction;

        [Tooltip("Optional. The typing blip for this page only, for example when a different character speaks. " +
            "Empty uses the object's Voice Sound (or the manager's Default Typing Sound).")]
        public SoundEffect voiceSound;
    }

    /// <summary>
    /// Put this on anything the player can walk up to: a sign, a character, a lever.
    /// When the player is close, a textbox appears next to this object with the prompt.
    /// "Close" is the real 3D distance by default. Tick Ignore Depth to measure it on the flattened screen instead.
    /// Pressing Interact runs On Interact and, if there are pages, shows them one at a time.
    /// </summary>
    public class ContextDialogue : MonoBehaviour
    {
        [Header("Prompt")]
        [Tooltip("Shown while the player is close. Leave empty (and no image) to show nothing until they interact.")]
        [TextArea(1, 3)]
        [SerializeField] private string promptText = "[F] Interact";
        [SerializeField] private Sprite promptImage;

        [Header("Dialogue")]
        [Tooltip("Shown one at a time, each time the player presses Interact. Leave empty for objects that only react.")]
        [SerializeField] private List<DialoguePage> pages = new List<DialoguePage>();

        [Header("Typewriter")]
        [Tooltip("Pages appear one letter at a time, like a character speaking. Prompts always appear all at once.")]
        [SerializeField] private bool typeOutPages = true;
        [Tooltip("Optional. The blip played while this object's pages are typed out, like a character's voice. " +
            "Empty uses the Context Dialogue Manager's Default Typing Sound.")]
        [SerializeField] private SoundEffect voiceSound;
        [Tooltip("Letters per second for this object's pages. 0 uses the Context Dialogue Manager's speed.")]
        [Min(0f)]
        [SerializeField] private float charactersPerSecond = 0f;

        [Header("Placement")]
        [Tooltip("How close the player has to be, in world units.")]
        [SerializeField] private float showDistance = 1.5f;
        [Tooltip("Off: the real 3D distance is used. On: depth is ignored, so the prompt shows whenever the object " +
            "looks close on screen, even if it is far behind or in front of the player.")]
        [SerializeField] private bool ignoreDepth = false;
        [Tooltip("Where the textbox points, measured from this object. The box sits just above this point.")]
        [SerializeField] private Vector3 textboxOffset = new Vector3(0f, 1f, 0f);

        [Header("Events")]
        [Tooltip("Runs when the player presses Interact while the prompt is showing (not while paging through dialogue).")]
        [SerializeField] private UnityEvent onInteract = new UnityEvent();

        public string PromptText => promptText;
        public Sprite PromptImage => promptImage;
        public int PageCount => pages.Count;
        public float ShowDistance => showDistance;
        public bool IgnoreDepth => ignoreDepth;
        public Vector3 TextboxAnchor => transform.position + textboxOffset;
        public bool TypeOutPages => typeOutPages;
        public SoundEffect VoiceSound => voiceSound;
        public float CharactersPerSecond => charactersPerSecond;

        /// <summary>
        /// Runs when the player presses Interact next to this object. Other scripts can add listeners in code.
        /// </summary>
        public UnityEvent OnInteract => onInteract;

        /// <summary>
        /// Changes the prompt while the game runs (for example a door switching from locked to unlocked).
        /// A prompt that is showing right now updates on the next frame.
        /// </summary>
        public void SetPromptText(string newPromptText)
        {
            promptText = newPromptText;
        }

        public DialoguePage GetPage(int pageIndex)
        {
            return pages[pageIndex];
        }

        void OnEnable()
        {
            ContextDialogueManager.Register(this);
        }

        void OnDisable()
        {
            ContextDialogueManager.Unregister(this);
        }

        /// <summary>
        /// Called by ContextDialogueManager when the player presses Interact next to this object.
        /// </summary>
        public void Interact()
        {
            onInteract.Invoke();
        }

        //shows the range and the textbox point in the Scene view when this object is selected.
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, showDistance);
            Gizmos.DrawLine(transform.position, TextboxAnchor);
            Gizmos.DrawWireCube(TextboxAnchor, Vector3.one * 0.15f);
        }
    }
}
