using Data.Scene;
using DG.Tweening;
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

        var op = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Additive);
        yield return op;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(gameplaySceneName));

        startButton.SetEnabled(true);
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
