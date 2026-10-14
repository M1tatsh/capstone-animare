using UnityEngine;

namespace Data.Audio
{
    /// <summary>
    /// Sound modulator on a per-sound-play basis.
    /// </summary>
    [System.Serializable]
    public class SoundPlayOptions
    {
        [Tooltip("Multiplies the sound's volume for this play.")]
        [Range(0f, 2f)]
        public float volumeMultiplier = 1f;

        [Tooltip("Multiplies the pitch for this play (2 = an octave up, 0.5 = an octave down).")]
        [Range(0.1f, 3f)]
        public float pitchMultiplier = 1f;

        [Tooltip("Extra random pitch for this play, added on top of the asset's own pitch range. " +
            "For example 0.1 means anywhere from 0.1 lower to 0.1 higher.")]
        [Range(0f, 1f)]
        public float extraRandomPitch = 0f;

        [Tooltip("Plays the sound as if heard through a wall (a low-pass filter cuts the high frequencies).")]
        public bool muffled = false;

        [Tooltip("How muffled: lower is more muffled. Around 500 to 1500 sounds like 'behind a wall'.")]
        [Range(100f, 22000f)]
        public float muffleCutoffFrequency = 800f;

        /// <summary>
        /// No changes: plays the sound exactly as its asset describes.
        /// </summary>
        public static SoundPlayOptions Default
        {
            get { return new SoundPlayOptions(); }
        }

        /// <summary>
        /// A quick way to get muffled options in code, for example AudioManager.PlaySoundEffect(knock, SoundPlayOptions.Muffled()).
        /// </summary>
        public static SoundPlayOptions Muffled(float cutoffFrequency = 800f)
        {
            return new SoundPlayOptions { muffled = true, muffleCutoffFrequency = cutoffFrequency };
        }
    }
}
