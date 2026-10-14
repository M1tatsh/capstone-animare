using UnityEngine;

namespace Data.Audio
{
    /// <summary>
    /// Plays a sound effect from an object in the world, with various customizable options.
    /// </summary>
    public class SoundEffectEmitter : MonoBehaviour
    {
        [SerializeField] private SoundEffect soundEffect;
        [SerializeField] private SoundPlayOptions options = new SoundPlayOptions();
        [Tooltip("Plays when the scene starts. For a looping sound it also starts again whenever this object is turned back on.")]
        [SerializeField] private bool playOnStart = false;

        [Header("Looping")]
        [Tooltip("Plays the sound over and over until Stop() is called or this object is turned off.")]
        [SerializeField] private bool loop = false;

        [Header("Hearing distance")]
        [Tooltip("On: only heard near this object, and quieter the farther away the player is. " +
            "Off: heard at the same volume anywhere in the scene.")]
        [SerializeField] private bool useHearingRange = true;
        [SerializeField] private SoundHearingRange hearingRange = new SoundHearingRange();

        //looping sounds play on their own AudioSource (on a child object), not the AudioManager's pool,
        //because they keep playing for a long time and their volume changes every frame.
        private AudioSource loopPlayer;
        private AudioLowPassFilter loopFilter;
        private bool hasStarted;

        public SoundHearingRange HearingRange => hearingRange;

        /// <summary>
        /// True while a looping sound is playing.
        /// </summary>
        public bool IsLooping => loopPlayer != null && loopPlayer.isPlaying;

        void Start()
        {
            hasStarted = true;

            if (playOnStart)
                Play();
        }

        void OnEnable()
        {
            //Start only runs once, so a looping sound that was stopped by turning the object off starts again here.
            if (hasStarted && playOnStart && loop)
                Play();
        }

        void OnDisable()
        {
            Stop();
        }

        void OnValidate()
        {
            if (hearingRange != null)
                hearingRange.KeepRadiiInOrder();
        }

        void Update()
        {
            if (!IsLooping)
                return;

            UpdateLoopVolume();
        }

        /// <summary>
        /// Plays the sound effect with this emitter's options. If Loop is ticked, starts the loop instead.
        /// </summary>
        public void Play()
        {
            if (loop)
            {
                StartLoop();
                return;
            }

            PlayOnce(soundEffect);
        }

        /// <summary>
        /// Plays a different sound effect once, with this emitter's options and hearing range. Also usable from a UnityEvent.
        /// </summary>
        public void Play(SoundEffect otherSoundEffect)
        {
            PlayOnce(otherSoundEffect);
        }

        /// <summary>
        /// Stops a looping sound. One-shot sounds are short, so they are left to finish.
        /// </summary>
        public void Stop()
        {
            if (loopPlayer != null)
                loopPlayer.Stop();
        }

        private void PlayOnce(SoundEffect soundEffectToPlay)
        {
            if (useHearingRange)
                AudioManager.PlaySoundEffectAt(soundEffectToPlay, transform.position, hearingRange, options);
            else
                AudioManager.PlaySoundEffect(soundEffectToPlay, options);
        }

        private void StartLoop()
        {
            //already playing: don't restart it from a different point.
            if (soundEffect == null || IsLooping)
                return;

            AudioClip clip = soundEffect.PickClip();
            if (clip == null)
            {
                Debug.LogWarning($"SoundEffectEmitter: The sound effect '{soundEffect.name}' has no clips.", this);
                return;
            }

            if (loopPlayer == null)
                CreateLoopPlayer();

            loopPlayer.clip = clip;
            loopPlayer.pitch = soundEffect.PickPitch(options);
            loopPlayer.outputAudioMixerGroup = AudioManager.GetMixerGroupFor(soundEffect);

            //set the right volume before it starts, so it doesn't blip at full volume for one frame.
            UpdateLoopVolume();

            loopPlayer.Play();

            //starting at a random point stops two emitters with the same loop from sounding like one loud sound.
            //timeSamples is the position in samples. Random.Range with whole numbers never returns the last one,
            //so this can't land past the end of the clip.
            loopPlayer.timeSamples = Random.Range(0, clip.samples);
        }

        private void CreateLoopPlayer()
        {
            //a child object, because a low-pass filter muffles every AudioSource on its own GameObject.
            GameObject loopObject = new GameObject("LoopPlayer");
            loopObject.transform.SetParent(transform, false);

            loopPlayer = loopObject.AddComponent<AudioSource>();
            loopPlayer.playOnAwake = false;
            loopPlayer.loop = true;
            loopPlayer.spatialBlend = 0f;   //flat 2D sound, the hearing range handles the distance

            loopFilter = loopObject.AddComponent<AudioLowPassFilter>();
            loopFilter.enabled = false;
        }

        private void UpdateLoopVolume()
        {
            float distanceVolume = 1f;
            float distanceCutoffFrequency = SoundHearingRange.UnmuffledCutoffFrequency;

            if (useHearingRange && AudioManager.Listener != null)
            {
                float distance = hearingRange.GetDistance(transform.position, AudioManager.Listener.position);
                float closeness = hearingRange.GetCloseness(distance);

                distanceVolume = hearingRange.GetVolumeMultiplier(closeness);
                distanceCutoffFrequency = hearingRange.GetMuffleCutoffFrequency(closeness);
            }

            loopPlayer.volume = soundEffect.volume * options.volumeMultiplier * distanceVolume * AudioManager.SoundEffectsVolume;

            //muffling: whichever is more muffled, the options or the distance.
            float cutoffFrequency = SoundHearingRange.UnmuffledCutoffFrequency;
            if (options.muffled)
                cutoffFrequency = options.muffleCutoffFrequency;
            cutoffFrequency = Mathf.Min(cutoffFrequency, distanceCutoffFrequency);

            loopFilter.enabled = cutoffFrequency < SoundHearingRange.UnmuffledCutoffFrequency;
            loopFilter.cutoffFrequency = cutoffFrequency;
        }

        void OnDrawGizmosSelected()
        {
            if (!useHearingRange || hearingRange == null)
                return;

            //inner sphere: full volume. Outer sphere: the edge of hearing.
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, hearingRange.fullVolumeRadius);

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, hearingRange.hearingRadius);
        }
    }
}
