using UnityEngine;
using UnityEngine.Audio;

namespace Data.Audio
{
    /// <summary>
    /// Sound effect with modulation features (right-click in the Project window > Create > Audio > Sound Effect).
    /// </summary>
    [CreateAssetMenu(fileName = "NewSoundEffect", menuName = "Audio/Sound Effect")]
    public class SoundEffect : ScriptableObject
    {
        [Tooltip("One of these is picked at random each time, so repeated sounds don't all sound the same.")]
        public AudioClip[] clips;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Each play picks a random pitch between these two. Use (1, 1) for no randomness.")]
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        [Tooltip("The same sound can't play again until this many seconds have passed. Stops footsteps or pickups from stacking up.")]
        public float minimumTimeBetweenPlays = 0.05f;

        [Tooltip("Optional. Plays through this mixer group instead of the Sound Effects group from the Audio Manager Settings.")]
        public AudioMixerGroup mixerGroupOverride;

        //the last time this sound played, so minimumTimeBetweenPlays can be checked. Not saved in the asset.
        [System.NonSerialized] public float lastPlayTime = -1000f;

        /// <summary>
        /// Picks one of the clips at random, or null if there are none.
        /// </summary>
        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0)
                return null;

            return clips[Random.Range(0, clips.Length)];
        }

        /// <summary>
        /// Picks a pitch for one play: the asset's random range, then the play's own extra randomness,
        /// then the play's pitch multiplier.
        /// </summary>
        public float PickPitch(SoundPlayOptions options)
        {
            float pitch = Random.Range(pitchRange.x, pitchRange.y);

            if (options != null)
            {
                pitch += Random.Range(-options.extraRandomPitch, options.extraRandomPitch);
                pitch *= options.pitchMultiplier;
            }

            //a pitch of 0 or less would stop the sound or play it backwards.
            return Mathf.Max(0.05f, pitch);
        }
    }
}
