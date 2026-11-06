using Data.Audio;
using Data.Scene;
using Gameplay.Player;
using Gameplay.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UI.Behaviors
{
    /// <summary>
    /// MonoBehavior for pause menu. Attach to GameSystem Prefab to enable pausing.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenu : MonoBehaviour
    {
        #region variables
        private const int PauseSortingOrder = 500;

        [SerializeField] private InputAction pauseAction = new InputAction("Pause", InputActionType.Button);
        [SerializeField] private string titleSceneName = "TitleMenu";
        [SerializeField] private float fadeDuration = 0.5f;

        private VisualElement pauseLayer;
        private Button resumeButton;
        private Button checkpointButton;
        private Button restartButton;
        private Button titleButton;
        private Button quitButton;
        private Slider musicSlider;
        private Slider soundSlider;

        private bool isPaused;
        
        private float timeScaleBeforePause = 1f;
        private bool cursorWasVisible;
        private UnityEngine.CursorLockMode cursorLockBeforePause;
        private PlayerInput pausedPlayerInput;
        #endregion

        public static bool IsPaused { get; private set; }


        private static PauseMenu activeMenu;

        void Awake()
        {
            if (activeMenu != null && activeMenu != this)
            {
                // don't forget to add a debug warning
                gameObject.SetActive(false);
                return;
            }

            activeMenu = this;


            if (pauseAction.bindings.Count == 0)
            {

                pauseAction.AddBinding("<Keyboard>/escape");
                pauseAction.AddBinding("<Gamepad>/start");
            }


            UIDocument document = GetComponent<UIDocument>();
            if (document.panelSettings != null)
            {
                PanelSettings sortedSettings = Instantiate(document.panelSettings);
                sortedSettings.sortingOrder = PauseSortingOrder;
                document.panelSettings = sortedSettings;
            }
        }

        void OnEnable()
        {
            pauseAction.Enable();
            pauseAction.performed += OnPausePressed;

            FindMenuElements();
            HideMenu();
        }

        void OnDisable()
        {
            pauseAction.performed -= OnPausePressed;
            pauseAction.Disable();


            if (isPaused)
                Resume();
        }

        /// <summary>
        /// Finds the buttons and sliders in PauseMenu.uxml and hooks them up.
        /// </summary>
        private void FindMenuElements()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            if (root == null)
                return;

            pauseLayer = root.Q<VisualElement>("PauseLayer");
            resumeButton = root.Q<Button>("ResumeButton");
            checkpointButton = root.Q<Button>("CheckpointButton");
            restartButton = root.Q<Button>("RestartButton");
            titleButton = root.Q<Button>("TitleButton");
            quitButton = root.Q<Button>("QuitButton");
            musicSlider = root.Q<Slider>("MusicSlider");
            soundSlider = root.Q<Slider>("SoundSlider");

            if (pauseLayer == null || resumeButton == null)
            {
                Debug.LogError("PauseMenu | The UI Document needs PauseMenu.uxml as its Source Asset.", this);
                enabled = false;
                return;
            }

            resumeButton.clicked += Resume;

            if (checkpointButton != null)
                checkpointButton.clicked += BackToCheckpoint;
            if (restartButton != null)
                restartButton.clicked += RestartLevel;
            if (titleButton != null)
                titleButton.clicked += QuitToTitle;
            if (quitButton != null)
                quitButton.clicked += QuitGame;
            if (musicSlider != null)
                musicSlider.RegisterValueChangedCallback(changeEvent => AudioManager.MusicVolume = changeEvent.newValue);
            if (soundSlider != null)
                soundSlider.RegisterValueChangedCallback(changeEvent => AudioManager.SoundEffectsVolume = changeEvent.newValue);
        }

        private void OnPausePressed(InputAction.CallbackContext context)
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        /// <summary>
        /// Opens the menu and stops the game.
        /// </summary>
        public void Pause()
        {
            
            if (pauseLayer == null || pauseLayer.panel == null)
                FindMenuElements();


            bool titleIsShowing = FindAnyObjectByType<TitleMenuBehavior>() != null;

            if (isPaused || titleIsShowing || ScreenFader.IsTransitioning || pauseLayer == null)
                return;

            isPaused = true;
            IsPaused = true;

            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;


            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
            pausedPlayerInput = player != null ? player.GetComponent<PlayerInput>() : null;

            if (pausedPlayerInput != null)
                pausedPlayerInput.DeactivateInput();


            AudioListener.pause = true;
            AudioManager.SetMusicMuffled(true);

            cursorWasVisible = UnityEngine.Cursor.visible;
            cursorLockBeforePause = UnityEngine.Cursor.lockState;
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;


            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(AudioManager.MusicVolume);

            if (soundSlider != null)
                soundSlider.SetValueWithoutNotify(AudioManager.SoundEffectsVolume);

            pauseLayer.style.display = DisplayStyle.Flex;


            resumeButton.Focus();
        }

        /// <summary>
        /// Closes the menu and starts the game again.
        /// </summary>
        public void Resume()
        {
            if (!isPaused)
                return;

            isPaused = false;
            IsPaused = false;

            Time.timeScale = timeScaleBeforePause;

            if (pausedPlayerInput != null)
                pausedPlayerInput.ActivateInput();
            pausedPlayerInput = null;

            AudioListener.pause = false;
            AudioManager.SetMusicMuffled(false);

            UnityEngine.Cursor.visible = cursorWasVisible;
            UnityEngine.Cursor.lockState = cursorLockBeforePause;

            HideMenu();
        }

        private void HideMenu()
        {
            if (pauseLayer != null)
                pauseLayer.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Manual respawn at last checkpoint.
        /// </summary>
        private void BackToCheckpoint()
        {
            Resume();
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
            if (player != null)
                player.Respawn();

        }

        /// <summary>
        /// Reloads the level from the beginning
        /// </summary>
        private void RestartLevel()
        {
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();


            string levelName = player != null ? player.gameObject.scene.name : SceneManager.GetActiveScene().name;


            Resume();
            SaveManager.RestoreLevelStartProgress();
            ScreenFader.LoadSceneWithFade(levelName, LoadSceneMode.Single, fadeDuration, fadeDuration, Color.black);
        }

        private void QuitToTitle()
        {
            Resume();


            GameFlow.ReturnToTitle();

            Debug.Log($"PauseMenu | Quitting to the {titleSceneName}.");
            SaveManager.ClearProgress();
            ScreenFader.LoadSceneWithFade(titleSceneName, LoadSceneMode.Single, fadeDuration, fadeDuration, Color.black);
        }

        private void QuitGame()
        {
            Debug.Log("PauseMenu | Quitting the game.");
            Application.Quit();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        void OnDestroy()
        {
            if (activeMenu != this)
                return;

            activeMenu = null;
            IsPaused = false;
        }
    }
}
