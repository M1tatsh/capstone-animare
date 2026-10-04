using UnityEngine;
using UnityEngine.UIElements;

public class TitleMenuBehavior : MonoBehaviour
{
    private Button playButton;
    private Button quitButton;
    private VisualElement titleLogo;

    void Awake()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        playButton = root.Q<Button>("StartButton");
        quitButton = root.Q<Button>("QuitButton");
        titleLogo = root.Q<VisualElement>("TitleLogo");

        playButton.RegisterCallback<ClickEvent>(OnStartButtonClicked);
        quitButton.RegisterCallback<ClickEvent>(OnQuitButtonClicked);
    }
    void Start()
    {
    }

    private void OnStartButtonClicked(ClickEvent clickEvent)
    {
    }

    private void OnQuitButtonClicked(ClickEvent clickEvent)
    {
        Debug.Log("TitleMenuBehavior.OnQuitButtonClicked | Game is exiting...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    void Update()
    {

    }
}
