using UnityEngine;

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

        void Start()
        {
            if (song != null)
                AudioManager.PlayMusic(song, songVolume, fadeDuration);
            else
                AudioManager.StopMusic(fadeDuration);
        }
    }
}
