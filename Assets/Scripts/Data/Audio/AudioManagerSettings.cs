using UnityEngine;
using UnityEngine.Audio;

namespace Data.Audio
{
    /// <summary>
    /// Create asset (Create > Audio > Audio Manager Settings) to for custom, name it exactly "AudioManagerSettings".
    /// defaults are below.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioManagerSettings", menuName = "Audio/Audio Manager Settings")]
    public class AudioManagerSettings : ScriptableObject
    {
        [Header("Mixer (optional)")]
        [Tooltip("Mixer group every sound effect plays through, unless the Sound Effect has its own override.")]
        public AudioMixerGroup soundEffectsGroup;
        [Tooltip("Mixer group the music plays through.")]
        public AudioMixerGroup musicGroup;

        [Header("Sound effects")]
        [Tooltip("How many sound effects can play at the same time. The oldest is cut off when they are all busy.")]
        [Range(4, 64)]
        public int maximumSimultaneousSounds = 24;

        [Header("Music")]
        [Tooltip("Default seconds for one song to fade into the next.")]
        public float defaultMusicFadeDuration = 1f;
    }
}
