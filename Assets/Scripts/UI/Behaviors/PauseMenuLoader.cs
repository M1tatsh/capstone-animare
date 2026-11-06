using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Behaviors
{
    /// <summary>
    /// Keeps the pause menu's own scene loaded next to whatever scene is loaded
    /// </summary>
    public static class PauseMenuLoader
    {
        
        public const string PauseMenuSceneName = "PauseMenu";


        private static bool isLoading;
        
        private static bool hasWarnedAboutMissingScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            isLoading = false;
            hasWarnedAboutMissingScene = false;
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWatchingScenes()
        {
            
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            LoadPauseMenuIfMissing();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == PauseMenuSceneName)
            {
                isLoading = false;
                return;
            }

            LoadPauseMenuIfMissing();
        }

        private static void LoadPauseMenuIfMissing()
        {
            if (isLoading || SceneManager.GetSceneByName(PauseMenuSceneName).isLoaded)
                return;

            if (!Application.CanStreamedLevelBeLoaded(PauseMenuSceneName))
            {
                if (!hasWarnedAboutMissingScene)
                {
                    Debug.LogWarning($"PauseMenuLoader: There is no scene named '{PauseMenuSceneName}' in File > Build Profiles, " +
                        "so the pause menu isn't loaded.");
                    hasWarnedAboutMissingScene = true;
                }

                return;
            }

            isLoading = true;


            SceneManager.LoadSceneAsync(PauseMenuSceneName, LoadSceneMode.Additive);
        }
    }
}
