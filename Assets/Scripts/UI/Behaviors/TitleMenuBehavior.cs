using Data.Scene;
using DG.Tweening;
using Gameplay.Save;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TitleMenuBehavior : MonoBehaviour
{
    private VisualElement root;
    private Button startButton;
    private Button quitButton;
    
    private Button continueButton;
    private VisualElement titleLogo;

    [SerializeField] private string gameplaySceneName = "";
    [SerializeField] private float fadeDuration = 1.5f;


    IEnumerator Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        startButton = root.Q<Button>("StartButton");
        quitButton = root.Q<Button>("QuitButton");
        titleLogo = root.Q<VisualElement>("TitleLogo");

        startButton.RegisterCallback<ClickEvent>(OnStartButtonClicked);
        quitButton.RegisterCallback<ClickEvent>(OnQuitButtonClicked);

        //Continue loads the save, otherwise hidden when there is no save yet.
        continueButton = root.Q<Button>("ContinueButton");
        if (continueButton != null)
        {
            continueButton.RegisterCallback<ClickEvent>(OnContinueButtonClicked);
            continueButton.SetEnabled(false);
            continueButton.style.display = SaveManager.HasSave ? DisplayStyle.Flex : DisplayStyle.None;
        }

        var op = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Additive);
        yield return op;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(gameplaySceneName));

        startButton.SetEnabled(true);


        if (continueButton != null)
            continueButton.SetEnabled(true);
    }

    private void OnStartButtonClicked(ClickEvent clickEvent)
    {
        startButton.SetEnabled(false);

        DOTween.To(() => root.resolvedStyle.opacity,
                   v => root.style.opacity = v,
                   0f, fadeDuration)
               .SetTarget(this)
               .OnComplete(CloseTitle);

        //CloseTitle();
    }

    private void OnContinueButtonClicked(ClickEvent clickEvent)
    {
        startButton.SetEnabled(false);
        continueButton.SetEnabled(false);


        //loads the saved level
        if (!SaveManager.LoadGame())
        {
            startButton.SetEnabled(true);
            continueButton.SetEnabled(true);
        }
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
        GameFlow.Begin();
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}
