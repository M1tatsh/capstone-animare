using System.Collections.Generic;
using System.Text.RegularExpressions;
using Data.Audio;
using DG.Tweening;
using Gameplay.Player;
using Gameplay.Rotation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Gameplay.Dialogue
{
    /// <summary>
    /// Shows one textbox on screen next to the ContextDialogue closest to the player.
    /// Put this on the GameObject that has the UIDocument using ContextTextbox.uxml (one per scene).
    ///
    /// While the player is close to an object, its prompt is shown. Pressing Interact runs the object's
    /// On Interact event and starts its dialogue pages; each press after that shows the next page,
    /// and after the last page the prompt comes back. Walking away closes the textbox.
    ///
    /// A SceneTutorial can also show pages above the player. While a tutorial is showing,
    /// objects' prompts wait until it is closed.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ContextDialogueManager : MonoBehaviour
    {
        [Tooltip("How long the textbox takes to fade in or out, in seconds.")]
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;
        [Tooltip("Shown under a dialogue page when there are more pages after it.")]
        [SerializeField] private string nextPageHint = "[F] Next";
        [Tooltip("Shown under the last dialogue page.")]
        [SerializeField] private string lastPageHint = "[F] Close";

        [Header("Typewriter")]
        [Tooltip("Letters per second when a page is typed out. Each ContextDialogue can use its own speed instead.")]
        [Min(1f)]
        [SerializeField] private float charactersPerSecond = 40f;
        [Tooltip("Blip played while letters appear. ContextDialogues and pages can use their own voice sound instead.")]
        [SerializeField] private SoundEffect defaultTypingSound;
        [Tooltip("Plays the blip once every this many letters. 1 = every letter, 2 or 3 sounds less busy.")]
        [Min(1)]
        [SerializeField] private int lettersPerSound = 2;
        [Tooltip("Extra pause, in seconds, after . ! ? (and half of it after , ; :), so sentences read naturally.")]
        [Min(0f)]
        [SerializeField] private float punctuationPause = 0.15f;
        [Tooltip("Tutorial pages are typed out too.")]
        [SerializeField] private bool typeOutTutorials = true;

        //every ContextDialogue in the scene that is switched on. They add and remove themselves.
        private static readonly List<ContextDialogue> allDialogues = new List<ContextDialogue>();

        private Transform player;
        private PlayerInput playerInput;
        private Camera mainCamera;
        private ViewRotator viewRotator;

        //the parts of the textbox, found by name in ContextTextbox.uxml.
        private VisualElement textbox;
        private VisualElement imageElement;
        private Label textLabel;
        private Label hintLabel;

        private ContextDialogue currentDialogue;
        //the tutorial being shown above the player, or null when there is none.
        private SceneTutorial activeTutorial;
        //-1 means the prompt is showing. 0 and up is the dialogue (or tutorial) page being shown.
        private int pageIndex = -1;
        //the frame the current page appeared on, so the button press that opened it can't also skip it.
        private int pageShownFrame = -1;

        private bool isVisible;
        private float textboxOpacity;
        private Tween fadeTween;

        //typewriter: the page being typed out right now.
        private bool isTyping;
        private string fullText = "";
        //for each letter that shows on screen, where it ends in fullText (rich text tags like <b> are skipped over).
        private readonly List<int> letterEndIndexes = new List<int>();
        private readonly List<char> letters = new List<char>();
        private int lettersShown;
        //counts up by letters-per-second; one letter appears each time it passes 1.
        private float typingProgress;
        private float typingSpeed;
        private SoundEffect typingSound;
        private int lettersSinceSound;

        //Unity's rich text tag that makes the letters after it see-through, so the hidden part of the page
        //still takes up its space and the box doesn't grow and jump around while typing.
        private const string HiddenTextTag = "<alpha=#00>";
        //color tags inside the hidden part would make it visible again, so they are taken out of that part.
        private static readonly Regex ColorTagPattern = new Regex(@"</?(color|alpha)[^>]*>", RegexOptions.IgnoreCase);

        /// <summary>
        /// True while the player is reading dialogue pages (not just seeing a prompt).
        /// </summary>
        public bool IsInConversation
        {
            get { return currentDialogue != null && pageIndex >= 0; }
        }

        /// <summary>
        /// True while a page is still appearing letter by letter.
        /// </summary>
        public bool IsTyping
        {
            get { return isTyping; }
        }

        /// <summary>
        /// True while a tutorial is showing above the player.
        /// </summary>
        public bool IsShowingTutorial
        {
            get { return activeTutorial != null; }
        }

        public static void Register(ContextDialogue dialogue)
        {
            if (!allDialogues.Contains(dialogue))
                allDialogues.Add(dialogue);
        }

        public static void Unregister(ContextDialogue dialogue)
        {
            allDialogues.Remove(dialogue);
        }

        void Start()
        {
            PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                player = playerMovement.transform;
                playerInput = playerMovement.GetComponent<PlayerInput>();
            }

            mainCamera = Camera.main;
            viewRotator = FindAnyObjectByType<ViewRotator>();

            //the UI Document may still be switched off here (for example by a SceneLoadGate behind the title screen).
            //In that case Update keeps trying until it is switched on.
            TryFindTextbox();
        }

        /// <summary>
        /// True when the textbox exists and is on screen-ready. It is gone while the UI Document is switched off,
        /// and Unity builds a brand new one each time the UI Document is switched back on.
        /// </summary>
        private bool HasTextbox
        {
            get { return textbox != null && textbox.panel != null; }
        }

        /// <summary>
        /// Looks up the parts of the textbox in the UI Document. Returns false if the UI Document is switched off.
        /// </summary>
        private bool TryFindTextbox()
        {
            UIDocument document = GetComponent<UIDocument>();

            if (!document.isActiveAndEnabled || document.rootVisualElement == null)
                return false;

            VisualElement root = document.rootVisualElement;
            textbox = root.Q<VisualElement>("ContextTextbox");
            imageElement = root.Q<VisualElement>("ContextImage");
            textLabel = root.Q<Label>("ContextText");
            hintLabel = root.Q<Label>("ContextHint");

            if (textbox == null || imageElement == null || textLabel == null || hintLabel == null)
            {
                Debug.LogError("ContextDialogueManager: The UIDocument needs ContextTextbox.uxml as its Source Asset. Dialogue is turned off.");
                enabled = false;
                return false;
            }

            //the typewriter hides letters with a rich text tag.
            textLabel.enableRichText = true;

            //a fresh textbox starts hidden. Anything that was showing is shown again on it.
            if (fadeTween != null && fadeTween.IsActive())
                fadeTween.Kill();

            isVisible = false;
            SetTextboxOpacity(0f);
            textbox.style.display = DisplayStyle.None;

            if (activeTutorial != null || currentDialogue != null)
                ShowCurrentContent();

            return true;
        }

        void Update()
        {
            if (player == null || mainCamera == null)
                return;

            //wait for the UI Document to be switched on (and find the new textbox each time it is).
            if (!HasTextbox && !TryFindTextbox())
                return;

            //real time, so typing speed doesn't change if the game is slowed down (for example by the form menu).
            UpdateTyping(Time.unscaledDeltaTime);

            //pages that wait for a specific action (like RotateCamera) move on when the player uses it.
            InputAction pageAction = FindActionForCurrentPage();

            if (pageAction != null && Time.frameCount != pageShownFrame && pageAction.WasPerformedThisFrame())
            {
                AdvancePage();
            }

            //a tutorial has the textbox to itself until it is closed.
            if (activeTutorial != null)
                return;

            //the camera is halfway between two views while rotating, so leave the textbox as it is until the turn ends.
            if (viewRotator != null && viewRotator.IsRotating)
                return;

            ContextDialogue closestDialogue = FindDialogueToShow();

            if (closestDialogue != currentDialogue)
            {
                //a different object (or none): start again from its prompt.
                currentDialogue = closestDialogue;
                pageIndex = -1;
                ShowCurrentContent();
            }
            else if (currentDialogue != null && pageIndex < 0 && textLabel.text != currentDialogue.PromptText)
            {
                //the object changed its prompt while it was showing (for example a door that just unlocked).
                ShowCurrentContent();
            }
        }

        //LateUpdate runs after the player and the camera have moved, so the textbox never lags behind.
        void LateUpdate()
        {
            if (textbox == null || textbox.panel == null)
                return;

            //the point the textbox sits above: the player during a tutorial, otherwise the object.
            Vector3 anchor;

            if (activeTutorial != null && player != null)
                anchor = player.position + activeTutorial.TextboxOffset;
            else if (currentDialogue != null)
                anchor = currentDialogue.TextboxAnchor;
            else
                return;

            //turn the point in the world into a position on the UI panel.
            Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(textbox.panel, anchor, mainCamera);
            textbox.style.left = panelPosition.x;
            textbox.style.top = panelPosition.y;
        }

        void OnDestroy()
        {
            DOTween.Kill(this);
        }

        /// <summary>
        /// Called by PlayerController when the Interact button is pressed.
        /// </summary>
        public void Interact()
        {
            if (!HasTextbox)
                return;

            DialoguePage currentPage = GetCurrentPage();

            //a page that waits for another action ignores Interact.
            if (currentPage != null && currentPage.advanceWithAction != null)
                return;

            //a page that is still typing: the first press shows the rest of it, the next press moves on.
            if (isTyping)
            {
                FinishTyping();
                return;
            }

            if (activeTutorial != null || pageIndex >= 0)
            {
                AdvancePage();
                return;
            }

            if (currentDialogue == null)
                return;

            currentDialogue.Interact();

            //the event might have switched the object off (for example, picking it up).
            if (!currentDialogue.isActiveAndEnabled || currentDialogue.PageCount == 0)
                return;

            pageIndex = 0;
            ShowCurrentContent();
        }

        /// <summary>
        /// Moves to the next page of the tutorial or dialogue.
        /// After the last page a tutorial closes, and a dialogue goes back to its prompt.
        /// </summary>
        private void AdvancePage()
        {
            if (activeTutorial != null)
            {
                pageIndex++;

                if (pageIndex >= activeTutorial.PageCount)
                    FinishTutorial();
                else
                    ShowCurrentContent();

                return;
            }

            if (currentDialogue == null || pageIndex < 0)
                return;

            pageIndex++;

            //after the last page, go back to the prompt.
            if (pageIndex >= currentDialogue.PageCount)
                pageIndex = -1;

            ShowCurrentContent();
        }

        /// <summary>
        /// The page being shown right now, or null when a prompt (or nothing) is showing.
        /// </summary>
        private DialoguePage GetCurrentPage()
        {
            if (activeTutorial != null)
                return activeTutorial.GetPage(pageIndex);

            if (currentDialogue != null && pageIndex >= 0)
                return currentDialogue.GetPage(pageIndex);

            return null;
        }

        /// <summary>
        /// Finds the player's own copy of the action the current page is waiting for, or null if it waits for Interact.
        /// The PlayerInput component can use its own copy of the actions, so the action is looked up there by its id.
        /// </summary>
        private InputAction FindActionForCurrentPage()
        {
            DialoguePage currentPage = GetCurrentPage();

            if (currentPage == null || currentPage.advanceWithAction == null || currentPage.advanceWithAction.action == null || playerInput == null)
                return null;

            return playerInput.actions.FindAction(currentPage.advanceWithAction.action.id);
        }

        /// <summary>
        /// Called by SceneTutorial. Shows the tutorial's first page above the player,
        /// in place of any prompt that was showing.
        /// </summary>
        public void ShowTutorial(SceneTutorial tutorial)
        {
            if (!isActiveAndEnabled || tutorial == null || tutorial.PageCount == 0)
                return;

            if (!HasTextbox && !TryFindTextbox())
                return;

            activeTutorial = tutorial;
            currentDialogue = null;
            pageIndex = 0;
            ShowCurrentContent();
        }

        /// <summary>
        /// Closes the tutorial and remembers that it was seen. Prompts come back on the next frame.
        /// </summary>
        private void FinishTutorial()
        {
            activeTutorial.MarkAsSeen();
            StopTyping();
            activeTutorial = null;
            currentDialogue = null;
            pageIndex = -1;
            FadeTextbox(false);
        }

        /// <summary>
        /// Picks the object whose textbox should show: the closest one in range.
        /// During a conversation the same object is kept for as long as the player stays in its range.
        /// </summary>
        private ContextDialogue FindDialogueToShow()
        {
            if (IsInConversation && currentDialogue.isActiveAndEnabled && GetScreenDistance(currentDialogue) <= currentDialogue.ShowDistance)
                return currentDialogue;

            ContextDialogue closestDialogue = null;
            float closestDistance = float.MaxValue;

            foreach (ContextDialogue dialogue in allDialogues)
            {
                float distance = GetScreenDistance(dialogue);

                if (distance <= dialogue.ShowDistance && distance < closestDistance)
                {
                    closestDialogue = dialogue;
                    closestDistance = distance;
                }
            }

            return closestDialogue;
        }

        /// <summary>
        /// Distance between the player and the object. Normally the real 3D distance.
        /// If the object has Ignore Depth ticked, the direction the camera looks in is left out,
        /// so only the distance as seen on screen counts.
        /// </summary>
        private float GetScreenDistance(ContextDialogue dialogue)
        {
            Vector3 playerToObject = dialogue.transform.position - player.position;

            if (!dialogue.IgnoreDepth)
                return playerToObject.magnitude;

            return Vector3.ProjectOnPlane(playerToObject, mainCamera.transform.forward).magnitude;
        }

        /// <summary>
        /// Fills the textbox with the current prompt or page, then fades it in (or out if there is nothing to show).
        /// </summary>
        private void ShowCurrentContent()
        {
            string text;
            Sprite image;
            string hint;
            //typewriter settings for this page.
            bool typeThisPage = false;
            SoundEffect pageTypingSound = defaultTypingSound;
            float pageTypingSpeed = charactersPerSecond;

            //whatever was typing before is replaced.
            StopTyping();

            if (activeTutorial != null)
            {
                DialoguePage tutorialPage = activeTutorial.GetPage(pageIndex);
                text = tutorialPage.text;
                image = tutorialPage.image;
                hint = GetPageHint(tutorialPage, activeTutorial.PageCount);

                typeThisPage = typeOutTutorials;
                if (tutorialPage.voiceSound != null)
                    pageTypingSound = tutorialPage.voiceSound;
            }
            else if (currentDialogue == null)
            {
                FadeTextbox(false);
                return;
            }
            else if (pageIndex < 0)
            {
                text = currentDialogue.PromptText;
                image = currentDialogue.PromptImage;
                hint = "";
            }
            else
            {
                DialoguePage page = currentDialogue.GetPage(pageIndex);
                text = page.text;
                image = page.image;
                hint = GetPageHint(page, currentDialogue.PageCount);

                //page voice first, then the object's voice, then the manager's default.
                typeThisPage = currentDialogue.TypeOutPages;
                if (page.voiceSound != null)
                    pageTypingSound = page.voiceSound;
                else if (currentDialogue.VoiceSound != null)
                    pageTypingSound = currentDialogue.VoiceSound;

                if (currentDialogue.CharactersPerSecond > 0f)
                    pageTypingSpeed = currentDialogue.CharactersPerSecond;
            }

            pageShownFrame = Time.frameCount;

            bool hasText = !string.IsNullOrEmpty(text);
            bool hasImage = image != null;
            bool hasHint = !string.IsNullOrEmpty(hint);

            if (!hasText && !hasImage)
            {
                FadeTextbox(false);
                return;
            }

            textLabel.text = hasText ? text : "";
            textLabel.style.display = hasText ? DisplayStyle.Flex : DisplayStyle.None;

            imageElement.style.backgroundImage = hasImage ? new StyleBackground(image) : new StyleBackground(StyleKeyword.None);
            imageElement.style.display = hasImage ? DisplayStyle.Flex : DisplayStyle.None;

            hintLabel.text = hasHint ? hint : "";
            hintLabel.style.display = hasHint ? DisplayStyle.Flex : DisplayStyle.None;
            hintLabel.style.visibility = Visibility.Visible;

            if (typeThisPage && hasText)
                StartTyping(text, pageTypingSound, pageTypingSpeed);

            FadeTextbox(true);
        }

        #region Typewriter

        /// <summary>
        /// Starts showing the text one letter at a time. The hint ([F] Next) waits until the page is done.
        /// </summary>
        private void StartTyping(string text, SoundEffect sound, float speed)
        {
            fullText = text;
            FindLetters(text);

            lettersShown = 0;
            typingProgress = 0f;
            typingSpeed = Mathf.Max(1f, speed);
            typingSound = sound;
            //so the very first letter makes a sound.
            lettersSinceSound = lettersPerSound;
            isTyping = letters.Count > 0;

            //hidden, but still taking up its space, so the box doesn't change size when it appears.
            hintLabel.style.visibility = isTyping ? Visibility.Hidden : Visibility.Visible;

            ShowTypedLetters();
        }

        /// <summary>
        /// Shows more letters as time passes, with pauses after punctuation, and plays the typing blip.
        /// </summary>
        private void UpdateTyping(float deltaTime)
        {
            if (!isTyping)
                return;

            typingProgress += deltaTime * typingSpeed;
            bool lettersAdded = false;
            bool soundPlayedThisFrame = false;

            while (typingProgress >= 1f && lettersShown < letters.Count)
            {
                typingProgress -= 1f;
                char letter = letters[lettersShown];
                lettersShown++;
                lettersAdded = true;

                //a pause after punctuation: take the pause's worth of letters off the progress.
                if (letter == '.' || letter == '!' || letter == '?')
                    typingProgress -= punctuationPause * typingSpeed;
                else if (letter == ',' || letter == ';' || letter == ':')
                    typingProgress -= punctuationPause * 0.5f * typingSpeed;

                //no blip for spaces, and at most one per frame so fast text doesn't stack sounds.
                if (char.IsWhiteSpace(letter))
                    continue;

                lettersSinceSound++;

                if (lettersSinceSound >= lettersPerSound && !soundPlayedThisFrame)
                {
                    AudioManager.PlaySoundEffect(typingSound);
                    lettersSinceSound = 0;
                    soundPlayedThisFrame = true;
                }
            }

            if (lettersAdded)
                ShowTypedLetters();

            if (lettersShown >= letters.Count)
                FinishTyping();
        }

        /// <summary>
        /// Shows the whole page right away (for example when the player presses Interact while it is typing).
        /// </summary>
        private void FinishTyping()
        {
            if (!isTyping)
                return;

            isTyping = false;
            lettersShown = letters.Count;
            textLabel.text = fullText;
            hintLabel.style.visibility = Visibility.Visible;
        }

        /// <summary>
        /// Stops typing without changing what is on screen. Used when the page is replaced or the box closes.
        /// </summary>
        private void StopTyping()
        {
            isTyping = false;

            if (hintLabel != null)
                hintLabel.style.visibility = Visibility.Visible;
        }

        /// <summary>
        /// Puts the full page in the label, with the letters that haven't appeared yet made see-through.
        /// </summary>
        private void ShowTypedLetters()
        {
            if (lettersShown >= letters.Count)
            {
                textLabel.text = fullText;
                return;
            }

            int splitIndex = lettersShown == 0 ? 0 : letterEndIndexes[lettersShown - 1];

            string shownPart = fullText.Substring(0, splitIndex);
            string hiddenPart = ColorTagPattern.Replace(fullText.Substring(splitIndex), "");

            textLabel.text = shownPart + HiddenTextTag + hiddenPart;
        }

        /// <summary>
        /// Lists the letters that show on screen, skipping rich text tags (bold, color and so on),
        /// which appear instantly and don't count as letters.
        /// </summary>
        private void FindLetters(string text)
        {
            letters.Clear();
            letterEndIndexes.Clear();

            int index = 0;

            while (index < text.Length)
            {
                if (text[index] == '<')
                {
                    int tagEnd = text.IndexOf('>', index);

                    if (tagEnd > index)
                    {
                        index = tagEnd + 1;
                        continue;
                    }
                }

                letters.Add(text[index]);
                letterEndIndexes.Add(index + 1);
                index++;
            }
        }

        #endregion

        /// <summary>
        /// [F] Next or [F] Close, or nothing for a page that waits for another action (its text says what to press).
        /// </summary>
        private string GetPageHint(DialoguePage page, int pageCount)
        {
            if (page.advanceWithAction != null)
                return "";

            return pageIndex == pageCount - 1 ? lastPageHint : nextPageHint;
        }

        private void FadeTextbox(bool show)
        {
            if (!show)
                StopTyping();

            if (show == isVisible)
                return;

            isVisible = show;

            if (fadeTween != null && fadeTween.IsActive())
                fadeTween.Kill();

            if (show)
                textbox.style.display = DisplayStyle.Flex;

            fadeTween = DOTween.To(() => textboxOpacity, SetTextboxOpacity, show ? 1f : 0f, fadeDuration)
                .SetEase(fadeEase)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    //fully faded out: take the textbox out of the layout until it is needed again.
                    if (!isVisible)
                        textbox.style.display = DisplayStyle.None;
                });
        }

        private void SetTextboxOpacity(float opacity)
        {
            textboxOpacity = opacity;
            textbox.style.opacity = opacity;
        }
    }
}
