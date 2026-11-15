using Data.Audio;
using Data.Playtest;
using Data.Scene;
using DG.Tweening;
using Gameplay.Save;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TitleMenuBehavior : MonoBehaviour
{
    private VisualElement root;
    private Button startButton;
    private Button quitButton;
    
    private Button continueButton;
    private Button openLogsButton;
    private VisualElement titleLogo;

    [FormerlySerializedAs("gameplaySceneName")]
    [SerializeField] private string newGameSceneName = "";
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float sceneFadeInDuration = 0.5f;
    [SerializeField] private float noSaveContinueOpacity = 0.4f;

    [Header("Title Music")]
    [SerializeField] private AudioClip titleMusic;
    [Range(0f, 1f)]
    [SerializeField] private float titleMusicVolume = 0.8f;
    [SerializeField] private bool muffleSceneMusicUnderTitle = true;
    [SerializeField] private float muffledCutoffFrequency = 800f;

    private bool hasSceneUnderneath;
    private bool isPlayingTitleMusic;
    private bool isMufflingSceneMusic;
    private readonly List<EventSystem> switchedOffEventSystems = new List<EventSystem>();


    void Awake()
    {
        GameFlow.SetTitleShowing(true);
    }

    IEnumerator Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        startButton = root.Q<Button>("StartButton");
        quitButton = root.Q<Button>("QuitButton");
        titleLogo = root.Q<VisualElement>("TitleLogo");

        startButton.RegisterCallback<ClickEvent>(OnStartButtonClicked);
        quitButton.RegisterCallback<ClickEvent>(OnQuitButtonClicked);

        continueButton = root.Q<Button>("ContinueButton");
        if (continueButton != null)
        {
            continueButton.RegisterCallback<ClickEvent>(OnContinueButtonClicked);
            continueButton.style.display = DisplayStyle.Flex;
        }

        openLogsButton = root.Q<Button>("OpenLogsButton");
        if (openLogsButton != null)
            openLogsButton.RegisterCallback<ClickEvent>(OnOpenLogsButtonClicked);

        SetButtonsEnabled(false);

        if (SaveManager.LoadSaveUnderTitle(out string savedSceneName))
        {
            if (muffleSceneMusicUnderTitle)
            {
                AudioManager.SetMusicMuffled(true, muffledCutoffFrequency, 0f);
                isMufflingSceneMusic = true;
            }

            AsyncOperation loading = SceneManager.LoadSceneAsync(savedSceneName, LoadSceneMode.Additive);

            if (loading != null)
            {
                yield return loading;

                Scene savedScene = SceneManager.GetSceneByName(savedSceneName);
                SceneManager.SetActiveScene(savedScene);
                SwitchOffEventSystemsIn(savedScene);
                hasSceneUnderneath = true;

                yield return null;
                yield return null;
                yield return null;

                Gameplay.GameplayPause.Pause(this);
            }
            else
            {
                Debug.LogWarning($"TitleMenuBehavior: The saved scene '{savedSceneName}' could not be loaded, so the title screen starts without it.");
                SaveManager.CancelSaveUnderTitle();
                StopMufflingSceneMusic(0f);
            }
        }

        if (!hasSceneUnderneath)
        {
            CreateBackgroundCamera();

            if (titleMusic != null)
            {
                AudioManager.PlayMusic(titleMusic, titleMusicVolume);
                isPlayingTitleMusic = true;
            }
        }

        startButton.SetEnabled(true);

        if (continueButton != null)
        {
            continueButton.SetEnabled(hasSceneUnderneath);
            continueButton.style.opacity = hasSceneUnderneath ? 1f : noSaveContinueOpacity;
        }
    }

    void OnDestroy()
    {
        DOTween.Kill(this);
        StopMufflingSceneMusic(0f);
        Gameplay.GameplayPause.Resume(this);
        GameFlow.SetTitleShowing(false);
    }

    private void StopMufflingSceneMusic(float fadeOutDuration)
    {
        if (!isMufflingSceneMusic)
            return;

        isMufflingSceneMusic = false;
        AudioManager.SetMusicMuffled(false, muffledCutoffFrequency, fadeOutDuration);
    }

    private void StopTitleMusic(float fadeOutDuration)
    {
        if (!isPlayingTitleMusic)
            return;

        isPlayingTitleMusic = false;
        AudioManager.StopMusic(fadeOutDuration);
    }

    private void CreateBackgroundCamera()
    {
        GameObject cameraObject = new GameObject("TitleBackgroundCamera");
        SceneManager.MoveGameObjectToScene(cameraObject, gameObject.scene);

        Camera backgroundCamera = cameraObject.AddComponent<Camera>();
        backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        backgroundCamera.backgroundColor = Color.black;
        backgroundCamera.cullingMask = 0;

        cameraObject.AddComponent<AudioListener>();
    }

    private void SwitchOffEventSystemsIn(Scene scene)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            foreach (EventSystem eventSystem in rootObject.GetComponentsInChildren<EventSystem>())
            {
                if (!eventSystem.enabled)
                    continue;

                eventSystem.enabled = false;
                switchedOffEventSystems.Add(eventSystem);
            }
        }
    }

    private static void SwitchOnEventSystems(List<EventSystem> eventSystems)
    {
        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem != null)
                eventSystem.enabled = true;
        }

        eventSystems.Clear();
    }

    private void SetButtonsEnabled(bool buttonsEnabled)
    {
        startButton.SetEnabled(buttonsEnabled);

        if (continueButton != null)
            continueButton.SetEnabled(buttonsEnabled);
    }

    private void OnStartButtonClicked(ClickEvent clickEvent)
    {
        SetButtonsEnabled(false);
        SaveManager.CancelSaveUnderTitle();
        GameFlow.PressStart(fadeDuration);
        StopTitleMusic(fadeDuration);
        StopMufflingSceneMusic(fadeDuration);

        FadeOutTitle(null);
        ScreenFader.FadeOutAndIn(StartNewGame, fadeDuration, sceneFadeInDuration, Color.black);
    }

    private void StartNewGame()
    {
        SaveManager.ClearProgress();
        GameFlow.Begin();
        SceneManager.LoadScene(newGameSceneName, LoadSceneMode.Single);
    }

    private void OnContinueButtonClicked(ClickEvent clickEvent)
    {
        if (!hasSceneUnderneath)
            return;

        SetButtonsEnabled(false);
        SaveManager.ContinueSaveUnderTitle();
        GameFlow.PressStart(fadeDuration);
        StopMufflingSceneMusic(fadeDuration);

        FadeOutTitle(CloseTitle);
    }

    private void FadeOutTitle(TweenCallback onFadeFinished)
    {
        Tween fadeTween = DOTween.To(() => root.resolvedStyle.opacity,
                   v => root.style.opacity = v,
                   0f, fadeDuration)
               .SetTarget(this);

        if (onFadeFinished != null)
            fadeTween.OnComplete(onFadeFinished);
    }

    private void OnOpenLogsButtonClicked(ClickEvent clickEvent)
    {
        string folderPath = CSVDataLogger.FolderPath;
        Directory.CreateDirectory(folderPath);
        Application.OpenURL(new System.Uri(folderPath).AbsoluteUri);
    }

    private void OnQuitButtonClicked(ClickEvent clickEvent)
    {
        Debug.Log("TitleMenuBehavior.OnQuitButtonClicked | Game is exiting...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    void CloseTitle()
    {
        Gameplay.GameplayPause.Resume(this);
        GameFlow.Begin();

        List<EventSystem> eventSystemsToSwitchOn = new List<EventSystem>(switchedOffEventSystems);
        switchedOffEventSystems.Clear();

        AsyncOperation unloading = SceneManager.UnloadSceneAsync(gameObject.scene);

        if (unloading != null)
            unloading.completed += operation => SwitchOnEventSystems(eventSystemsToSwitchOn);
        else
            SwitchOnEventSystems(eventSystemsToSwitchOn);
    }
}
