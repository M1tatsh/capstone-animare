using Data.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextSceneObject : MonoBehaviour
{
    [SerializeField] string nextScene = "";

    [Header("Screen Fade")]
    [Tooltip("Fade the screen out and back in around the scene load.")]
    [SerializeField] bool useFade = true;
    [Tooltip("Seconds to fade to the fade color before loading.")]
    [SerializeField] float fadeOutDuration = 0.5f;
    [Tooltip("Seconds to fade back in once the new scene has loaded.")]
    [SerializeField] float fadeInDuration = 0.5f;
    [SerializeField] Color fadeColor = Color.black;

    public void LoadScene()
    {
        Load(LoadSceneMode.Single);
    }

    public void LoadSceneAdditive()
    {
        Load(LoadSceneMode.Additive);
    }

    void Load(LoadSceneMode mode)
    {
        if (useFade)
            ScreenFader.LoadSceneWithFade(nextScene, mode, fadeOutDuration, fadeInDuration, fadeColor);
        else
            SceneManager.LoadScene(nextScene, mode);
    }
}
