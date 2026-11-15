using Data.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Data.Audio
{
    /// <summary>
    /// Starts a scene's music when the scene loads. Put one anywhere in the scene.
    /// If the same song is already playing from the last scene, it keeps playing without restarting.
    /// Leave the song empty to fade the music out in this scene.
    /// </summary>
    public class SceneMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip song;
        [Range(0f, 1f)]
        [SerializeField] private float songVolume = 0.8f;
        [Tooltip("Seconds to fade from the previous song. Below 0 uses the Audio Manager Settings default.")]
        [SerializeField] private float fadeDuration = -1f;

        [Header("Title Screen")]
        [SerializeField] private bool onlyPlayOnTitleScreen = false;
        [SerializeField] private float titleFadeOutDuration = -1f;

        [Header("Leaving This Scene")]
        [SerializeField] private bool fadeOutIfNextSceneHasNoMusic = false;
        [SerializeField] private float leavingFadeDuration = -1f;

        private static bool stopMusicIfNextSceneHasNone;
        private static float stopMusicFadeDuration = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            stopMusicIfNextSceneHasNone = false;
            stopMusicFadeDuration = -1f;

            SceneManager.sceneLoaded -= OnAnySceneLoaded;
            SceneManager.sceneLoaded += OnAnySceneLoaded;
        }

        private static void OnAnySceneLoaded(UnityEngine.SceneManagement.Scene loadedScene, LoadSceneMode loadMode)
        {
            if (!stopMusicIfNextSceneHasNone || loadMode != LoadSceneMode.Single)
                return;

            if (SceneHasMusic(loadedScene))
                return;

            stopMusicIfNextSceneHasNone = false;
            AudioManager.StopMusic(stopMusicFadeDuration);
        }

        private static bool SceneHasMusic(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.GetComponentInChildren<SceneMusic>(true) != null)
                    return true;
            }

            return false;
        }

        void Start()
        {
            if (onlyPlayOnTitleScreen)
            {
                if (GameFlow.HasStarted)
                    return;

                GameFlow.StartPressed += OnStartPressed;
            }

            stopMusicIfNextSceneHasNone = fadeOutIfNextSceneHasNoMusic;
            stopMusicFadeDuration = leavingFadeDuration;

            if (song != null)
                AudioManager.PlayMusic(song, songVolume, fadeDuration);
            else
                AudioManager.StopMusic(fadeDuration);
        }

        void OnDestroy()
        {
            GameFlow.StartPressed -= OnStartPressed;
        }

        private void OnStartPressed(float titleFadeDuration)
        {
            GameFlow.StartPressed -= OnStartPressed;

            float musicFadeDuration = titleFadeOutDuration >= 0f ? titleFadeOutDuration : titleFadeDuration;
            AudioManager.StopMusic(musicFadeDuration);
        }
    }
}
