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
    /// FacingDirection for determining when the prompt can show based on camera orientation.
    /// </summary>
    public enum FacingDirection
    {
        Forward,
        Back,
        Right,
        Left
    }

    /// <summary>
    /// Put this on anything to show a dialogue box above it.
    /// When the player is close, a textbox appears next to this object with the prompt.
    /// </summary>
    public class ContextDialogue : MonoBehaviour
    {
        #region variables
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
        [Header("Viewing Angle")]
        [Tooltip("Only show the prompt when the camera is looking at this object's front, for example a door " +
            "that can only be seen (and used) from one side. Select the object to see its front as a yellow arrow.")]
        [SerializeField] private bool onlyWhenFacingCamera = false;
        [Tooltip("Which way this object's front points, measured from the object's own rotation.")]
        [SerializeField] private FacingDirection facing = FacingDirection.Back;
        [Tooltip("How far (in degrees) the camera can be turned away from looking straight at the front. " +
            "45 means only the one view that looks at the front. Above 90 also includes the two side views.")]
        [Range(1f, 180f)]
        [SerializeField] private float facingTolerance = 45f;

        [Tooltip("Where the textbox points, measured from this object. The box sits just above this point.")]
        [SerializeField] private Vector3 textboxOffset = new Vector3(0f, 1f, 0f);
        #endregion

        [Header("Events")]
        [Tooltip("Runs when the player presses Interact while the prompt is showing (not while paging through dialogue).")]
        [SerializeField] private UnityEvent onInteract = new UnityEvent();

        public string PromptText => promptText;
        public Sprite PromptImage => promptImage;
        public int PageCount => pages.Count;
        public float ShowDistance => showDistance;
        public bool IgnoreDepth => ignoreDepth;
        public Vector3 TextboxAnchor => transform.position + textboxOffset;

        /// <summary>
        /// The world direction this object's front points in (flat, ignoring up and down).
        /// </summary>
        public Vector3 FrontDirection
        {
            get
            {
                Vector3 direction;

                if (facing == FacingDirection.Forward)
                    direction = transform.forward;
                else if (facing == FacingDirection.Back)
                    direction = -transform.forward;
                else if (facing == FacingDirection.Right)
                    direction = transform.right;
                else
                    direction = -transform.right;

                direction.y = 0f;
                return direction.normalized;
            }
        }

        /// <summary>
        /// Can the camera see this object's front right now? Always true when Only When Facing Camera is off.
        /// </summary>
        public bool IsFacingCamera(Camera viewCamera)
        {
            if (!onlyWhenFacingCamera || viewCamera == null)
                return true;

            
            Vector3 towardCamera = -viewCamera.transform.forward;
            towardCamera.y = 0f;

            if (towardCamera.sqrMagnitude < 0.0001f)
                return true;

            float angle = Vector3.Angle(FrontDirection, towardCamera.normalized);
            return angle <= facingTolerance;
        }
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

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, showDistance);
            Gizmos.DrawLine(transform.position, TextboxAnchor);
            Gizmos.DrawWireCube(TextboxAnchor, Vector3.one * 0.15f);

            if (onlyWhenFacingCamera)
            {
                Gizmos.color = Color.yellow;
                Vector3 arrowEnd = transform.position + FrontDirection * 1.5f;
                Gizmos.DrawLine(transform.position, arrowEnd);
                Gizmos.DrawWireSphere(arrowEnd, 0.1f);
            }
        }
    }
}
