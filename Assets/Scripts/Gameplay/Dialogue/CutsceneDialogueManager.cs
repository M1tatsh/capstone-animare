using Data.Audio;
using DG.Tweening;
using Gameplay.Rotation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Gameplay.Dialogue
{
    /// <summary>
    /// Manages the dialogue UI and progression for cutscenes, similar to the ContextDialogueManager.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CutsceneDialogueManager : MonoBehaviour
    {
        #region serialized variables
        [Header("Look")]
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;
        [SerializeField] private int sortingOrder = 400;

        [Header("Advance Icon")]
        [SerializeField] private Sprite advanceIcon;
        [SerializeField] private Sprite lastPageIcon;
        [SerializeField] private float iconBounceHeight = 6f;
        [SerializeField] private float iconBounceDuration = 0.4f;
        [SerializeField] private string nextPageHint = "[F] Next";
        [SerializeField] private string lastPageHint = "[F] Close";

        [Header("Input")]
        [SerializeField] private InputAction interactAction = new InputAction("Interact", InputActionType.Button);

        [Header("Typewriter")]
        [Min(1f)]
        [SerializeField] private float charactersPerSecond = 40f;
        [SerializeField] private SoundEffect defaultTypingSound;
        [Min(1)]
        [SerializeField] private int lettersPerSound = 2;
        [Min(0f)]
        [SerializeField] private float punctuationPause = 0.15f;
        #endregion

        #region private variables
        public static CutsceneDialogueManager Current { get; private set; }
        public static bool IsPlaying => Current != null;

        private VisualElement layer;
        private VisualElement stillFrameElement;
        private VisualElement portraitElement;
        private Label speakerLabel;
        private Label textLabel;
        private VisualElement advanceElement;
        private VisualElement advanceIconElement;
        private Label hintLabel;
        private TypewriterText typewriter;

        private Cutscene activeCutscene;
        private int pageIndex;
        private int pageShownFrame = -1;
        private bool isClosing;
        private float layerOpacity;
        private Tween fadeTween;
        private Tween bounceTween;
        private bool isAdvanceIconShown;
        #endregion

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Current = null;
        }

        void Awake()
        {
            UIDocument document = GetComponent<UIDocument>();

            if (document.panelSettings != null)
            {
                PanelSettings sortedSettings = Instantiate(document.panelSettings);
                sortedSettings.sortingOrder = sortingOrder;
                document.panelSettings = sortedSettings;
            }
        }

        void Start()
        {
            TryFindElements();
        }

        void OnEnable()
        {
            if (interactAction.bindings.Count == 0)
            {
                interactAction.AddBinding("<Keyboard>/f");
                interactAction.AddBinding("<Gamepad>/buttonWest");
            }

            interactAction.Enable();
        }

        private bool HasElements
        {
            get { return layer != null && layer.panel != null; }
        }

        private bool TryFindElements()
        {
            UIDocument document = GetComponent<UIDocument>();

            if (!document.isActiveAndEnabled || document.rootVisualElement == null)
                return false;

            VisualElement root = document.rootVisualElement;
            layer = root.Q<VisualElement>("CutsceneLayer");
            stillFrameElement = root.Q<VisualElement>("CutsceneFrame");
            portraitElement = root.Q<VisualElement>("CutscenePortrait");
            speakerLabel = root.Q<Label>("CutsceneSpeaker");
            textLabel = root.Q<Label>("CutsceneText");
            hintLabel = root.Q<Label>("CutsceneHint");
            advanceElement = root.Q<VisualElement>("CutsceneAdvance");
            advanceIconElement = root.Q<VisualElement>("CutsceneAdvanceIcon");

            if (layer == null || stillFrameElement == null || portraitElement == null || speakerLabel == null
                || textLabel == null || hintLabel == null || advanceElement == null || advanceIconElement == null)
            {
                Debug.LogError("CutsceneDialogueManager: The UIDocument needs CutsceneTextbox.uxml as its Source Asset. Cutscenes are turned off.", this);
                layer = null;
                enabled = false;
                return false;
            }

            typewriter = new TypewriterText(textLabel, lettersPerSound, punctuationPause);

            isAdvanceIconShown = false;
            advanceElement.style.visibility = Visibility.Hidden;

            if (activeCutscene == null)
            {
                SetLayerOpacity(0f);
                layer.style.display = DisplayStyle.None;
            }
            else
            {
                SetLayerOpacity(1f);
                layer.style.display = DisplayStyle.Flex;
                ShowPage();
            }

            return true;
        }

        void Update()
        {
            if (activeCutscene == null || typewriter == null)
                return;

            if (UI.Behaviors.PauseMenu.IsPaused)
                return;

            if (interactAction.WasPressedThisFrame())
                Interact();

            if (activeCutscene == null)
                return;

            typewriter.UpdateTyping(Time.unscaledDeltaTime);
            ShowAdvanceIcon(!typewriter.IsTyping);
        }

        void OnDisable()
        {
            interactAction.Disable();

            if (activeCutscene != null)
                EndCutscene(false);
        }

        void OnDestroy()
        {
            DOTween.Kill(this);
        }

        public void Play(Cutscene cutscene)
        {
            if (cutscene == null || cutscene.PageCount == 0)
                return;

            if (activeCutscene != null)
            {
                Debug.LogWarning("CutsceneDialogueManager: A cutscene is already playing, so '" + cutscene.name + "' was skipped.", cutscene);
                return;
            }

            if (!HasElements && !TryFindElements())
                return;

            activeCutscene = cutscene;
            Current = this;
            pageIndex = 0;
            isClosing = false;

            GameplayPause.Pause(this);

            CameraLookAround cameraLookAround = FindAnyObjectByType<CameraLookAround>();

            if (cameraLookAround != null && cameraLookAround.IsLooking)
                cameraLookAround.StopLooking();

            stillFrameElement.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            stillFrameElement.style.display = DisplayStyle.None;

            ShowPage();
            FadeLayer(true);
        }

        public void Interact()
        {
            if (activeCutscene == null || isClosing)
                return;

            if (Time.frameCount == pageShownFrame)
                return;

            if (typewriter.IsTyping)
            {
                typewriter.FinishTyping();
                ShowAdvanceIcon(true);
                return;
            }

            pageIndex++;

            if (pageIndex >= activeCutscene.PageCount)
                EndCutscene(true);
            else
                ShowPage();
        }

        private void ShowPage()
        {
            CutscenePage page = activeCutscene.GetPage(pageIndex);
            pageShownFrame = Time.frameCount;

            if (page.stillFrame != null)
            {
                stillFrameElement.style.backgroundImage = new StyleBackground(page.stillFrame);
                stillFrameElement.style.display = DisplayStyle.Flex;
            }

            bool hasSpeaker = !string.IsNullOrEmpty(page.speakerName);
            speakerLabel.text = hasSpeaker ? page.speakerName : "";
            speakerLabel.style.display = hasSpeaker ? DisplayStyle.Flex : DisplayStyle.None;

            bool hasPortrait = page.portrait != null;
            portraitElement.style.backgroundImage = hasPortrait ? new StyleBackground(page.portrait) : new StyleBackground(StyleKeyword.None);
            portraitElement.style.display = hasPortrait ? DisplayStyle.Flex : DisplayStyle.None;

            bool isLastPage = pageIndex == activeCutscene.PageCount - 1;
            Sprite pageIcon = isLastPage && lastPageIcon != null ? lastPageIcon : advanceIcon;
            bool hasIcon = pageIcon != null;

            advanceIconElement.style.backgroundImage = hasIcon ? new StyleBackground(pageIcon) : new StyleBackground(StyleKeyword.None);
            advanceIconElement.style.display = hasIcon ? DisplayStyle.Flex : DisplayStyle.None;
            hintLabel.text = isLastPage ? lastPageHint : nextPageHint;
            hintLabel.style.display = hasIcon ? DisplayStyle.None : DisplayStyle.Flex;

            SoundEffect pageSound = page.voiceSound != null ? page.voiceSound : defaultTypingSound;
            typewriter.StartTyping(page.text, pageSound, charactersPerSecond);
            ShowAdvanceIcon(!typewriter.IsTyping);
        }

        private void ShowAdvanceIcon(bool show)
        {
            if (show == isAdvanceIconShown)
                return;

            isAdvanceIconShown = show;
            advanceElement.style.visibility = show ? Visibility.Visible : Visibility.Hidden;

            if (bounceTween != null && bounceTween.IsActive())
                bounceTween.Kill();

            SetIconBounce(0f);

            if (!show || iconBounceDuration <= 0f)
                return;

            bounceTween = DOTween.To(() => 0f, SetIconBounce, 1f, iconBounceDuration)
                .SetEase(Ease.OutQuad)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private void SetIconBounce(float bounceAmount)
        {
            advanceIconElement.style.translate = new Translate(0f, -bounceAmount * iconBounceHeight);
        }

        private void EndCutscene(bool fadeOut)
        {
            Cutscene finishedCutscene = activeCutscene;

            if (typewriter != null)
                typewriter.StopTyping();

            if (bounceTween != null && bounceTween.IsActive())
                bounceTween.Kill();

            isAdvanceIconShown = false;

            if (!fadeOut || !HasElements)
            {
                if (fadeTween != null && fadeTween.IsActive())
                    fadeTween.Kill();

                if (HasElements)
                {
                    SetLayerOpacity(0f);
                    layer.style.display = DisplayStyle.None;
                }

                ReleaseCutscene(finishedCutscene, false);
                return;
            }

            isClosing = true;
            FadeLayer(false);
            fadeTween.OnComplete(() =>
            {
                layer.style.display = DisplayStyle.None;
                ReleaseCutscene(finishedCutscene, true);
            });
        }

        private void ReleaseCutscene(Cutscene finishedCutscene, bool finishedNormally)
        {
            isClosing = false;
            activeCutscene = null;

            if (Current == this)
                Current = null;

            GameplayPause.Resume(this);

            if (finishedNormally && finishedCutscene != null)
                finishedCutscene.MarkAsFinished();
        }

        private void FadeLayer(bool show)
        {
            if (fadeTween != null && fadeTween.IsActive())
                fadeTween.Kill();

            if (show)
                layer.style.display = DisplayStyle.Flex;

            fadeTween = DOTween.To(() => layerOpacity, SetLayerOpacity, show ? 1f : 0f, fadeDuration)
                .SetEase(fadeEase)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private void SetLayerOpacity(float opacity)
        {
            layerOpacity = opacity;

            if (layer != null)
                layer.style.opacity = opacity;
        }
    }
}
