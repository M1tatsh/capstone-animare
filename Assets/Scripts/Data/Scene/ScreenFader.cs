using System.Collections;
using DG.Tweening;
using Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Data.Scene
{
    /// <summary>
    /// Fades the screen to a color, loads a scene, then fades back in.
    /// You don't need to attach this to a scene object, just call the public methods
    /// from elsewhere.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        //drawn above every other UI Toolkit panel (the title menu, dialogue and HUD use 0).
        private const int FadeSortingOrder = 1000;

        private static ScreenFader instance;

        private VisualElement fadeOverlay;
        private float overlayOpacity;
        private bool isTransitioning;

        /// <summary>
        /// True from the moment the fade out starts until the fade in has finished.
        /// </summary>
        public static bool IsTransitioning
        {
            get { return instance != null && instance.isTransitioning; }
        }

        /// <summary>
        /// Fades out, loads the scene, fades back in. Calls made while a transition is already running are ignored,
        /// so pressing a door's button twice can't load the scene twice.
        /// </summary>
        public static void LoadSceneWithFade(string sceneName, LoadSceneMode mode, float fadeOutDuration, float fadeInDuration, Color fadeColor)
        {
            ScreenFader fader = GetOrCreateInstance();

            if (fader.isTransitioning)
                return;

            fader.StartCoroutine(fader.Transition(sceneName, mode, fadeOutDuration, fadeInDuration, fadeColor));
        }

        private static ScreenFader GetOrCreateInstance()
        {
            if (instance != null)
                return instance;

            GameObject faderObject = new GameObject("ScreenFader");
            DontDestroyOnLoad(faderObject);
            instance = faderObject.AddComponent<ScreenFader>();
            instance.CreateOverlay();
            return instance;
        }

        /// <summary>
        /// Builds a full-screen colored box on its own UI Toolkit panel, sorted above the game's other UI.
        /// </summary>
        private void CreateOverlay()
        {
            PanelSettings panelSettings;

            UIDocument existingDocument = FindAnyObjectByType<UIDocument>();

            if (existingDocument != null && existingDocument.panelSettings != null)
                panelSettings = Instantiate(existingDocument.panelSettings);
            else
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();

            //for covering the HUD
            panelSettings.sortingOrder = FadeSortingOrder;

            UIDocument document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;

            if (document.rootVisualElement == null)
            {
                document.enabled = false;
                document.enabled = true;
            }

            fadeOverlay = new VisualElement();
            fadeOverlay.name = "ScreenFadeOverlay";
            fadeOverlay.style.position = Position.Absolute;
            fadeOverlay.style.left = 0;
            fadeOverlay.style.top = 0;
            fadeOverlay.style.right = 0;
            fadeOverlay.style.bottom = 0;
            fadeOverlay.style.backgroundColor = Color.black;

            fadeOverlay.pickingMode = PickingMode.Ignore;

            document.rootVisualElement.Add(fadeOverlay);
            SetOverlayOpacity(0f);
        }

        private IEnumerator Transition(string sceneName, LoadSceneMode mode, float fadeOutDuration, float fadeInDuration, Color fadeColor)
        {
            isTransitioning = true;
            fadeOverlay.style.backgroundColor = fadeColor;

            //ensure player can't move while fading
            PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>();
            if (playerMovement != null)
                playerMovement.SetFrozen(true);

            //lets the game fade even while paused (using time.timescale).
            yield return FadeTo(1f, fadeOutDuration).SetUpdate(true).WaitForCompletion();

            AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName, mode);

            if (loading == null)
            {
                Debug.LogError($"ScreenFader: Could not load the scene '{sceneName}'. Check the name and that it is in File > Build Profiles.");
            }
            else
            {
                yield return loading;
            }

            if (playerMovement != null)
                playerMovement.SetFrozen(false);

            yield return null;

            yield return FadeTo(0f, fadeInDuration).SetUpdate(true).WaitForCompletion();

            isTransitioning = false;
        }

        private Tween FadeTo(float targetOpacity, float duration)
        {
            DOTween.Kill(this);

            return DOTween.To(() => overlayOpacity, SetOverlayOpacity, targetOpacity, duration)
                .SetEase(Ease.InOutQuad)
                .SetTarget(this);
        }

        private void SetOverlayOpacity(float opacity)
        {
            overlayOpacity = opacity;
            fadeOverlay.style.opacity = opacity;

            //only take part in drawing while something is showing.
            fadeOverlay.style.display = opacity > 0f ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnDestroy()
        {
            DOTween.Kill(this);

            if (instance == this)
                instance = null;
        }
    }
}
