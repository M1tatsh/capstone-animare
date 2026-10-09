using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using Data.Scene;

[RequireComponent(typeof(UIDocument))]
public class TitleMenuBehavior : MonoBehaviour
{
    private VisualElement root;
    private Button startButton;
    private Button quitButton;
    private VisualElement titleLogo;

    [SerializeField] private string gameplaySceneName = "";
    [SerializeField] private float fadeDuration = 0.6f;


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

        GameFlow.Begin();

        CloseTitle();
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
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}
