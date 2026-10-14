using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace Data.Audio
{
    /// <summary>
    /// Plays sound effects and music. Nothing needs to be placed in a scene: the first call creates the
    /// AudioManager, and it survives scene loads.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        //Unity's cutoff for a low-pass filter that changes nothing (the top of human hearing).
        private const float UnmuffledCutoffFrequency = SoundHearingRange.UnmuffledCutoffFrequency;
        //how many sound effect players are made at the start. More are added when needed, up to the maximum.
        private const int StartingSoundEffectPlayers = 8;

        private static AudioManager instance;

        private AudioManagerSettings settings;

        //sound effects: a pool of players, reused so new ones don't have to be created for every sound.
        private readonly List<AudioSource> soundEffectPlayers = new List<AudioSource>();
        private readonly List<AudioLowPassFilter> soundEffectFilters = new List<AudioLowPassFilter>();
        private float soundEffectsVolume = 1f;

        //music: two players, so one song can fade out while the next fades in.
        private AudioSource[] musicPlayers;
        private AudioLowPassFilter[] musicFilters;
        private int activeMusicPlayerIndex;
        private float musicVolume = 1f;
        //the volume the current song was asked to play at, before musicVolume is applied.
        private float currentSongVolume = 1f;
        private float musicCutoffFrequency = UnmuffledCutoffFrequency;

        #region Public: sound effects

        /// <summary>
        /// Plays a sound effect once. Returns false if nothing was played (no clip, or played again too soon).
        /// </summary>
        public static bool PlaySoundEffect(SoundEffect soundEffect)
        {
            return PlaySoundEffect(soundEffect, null);
        }

        /// <summary>
        /// Plays a sound effect once, changed for this play by the options (volume, pitch, muffling).
        /// </summary>
        public static bool PlaySoundEffect(SoundEffect soundEffect, SoundPlayOptions options)
        {
            if (soundEffect == null)
                return false;

            return GetOrCreateInstance().PlaySoundEffectInternal(soundEffect, options ?? SoundPlayOptions.Default,
                1f, UnmuffledCutoffFrequency);
        }

        /// <summary>
        /// Plays a sound effect that comes from a place in the world. It is quieter the farther it is from the
        /// Listener (the player), and isn't played at all if the Listener is outside the hearing range.
        /// If there is no Listener (for example on the title screen), it plays at full volume.
        /// Returns false if nothing was played.
        /// </summary>
        public static bool PlaySoundEffectAt(SoundEffect soundEffect, Vector3 soundPosition,
            SoundHearingRange hearingRange, SoundPlayOptions options = null)
        {
            if (soundEffect == null)
                return false;

            if (hearingRange == null || Listener == null)
                return PlaySoundEffect(soundEffect, options);

            float distance = hearingRange.GetDistance(soundPosition, Listener.position);
            float closeness = hearingRange.GetCloseness(distance);

            //too far away to hear, so don't use up a sound effect player on it.
            if (closeness <= 0f)
                return false;

            return GetOrCreateInstance().PlaySoundEffectInternal(soundEffect, options ?? SoundPlayOptions.Default,
                hearingRange.GetVolumeMultiplier(closeness), hearingRange.GetMuffleCutoffFrequency(closeness));
        }

        /// <summary>
        /// Where sounds in the world are heard from. The player sets this to itself when it spawns.
        /// This doesn't need the AudioManager to exist yet, so setting it never creates one.
        /// </summary>
        public static Transform Listener
        {
            get;
            set;
        }

        /// <summary>
        /// The mixer group a sound effect should play through (its own override, or the settings' Sound Effects group).
        /// Used by looping Sound Effect Emitters, which play on their own AudioSource.
        /// </summary>
        public static AudioMixerGroup GetMixerGroupFor(SoundEffect soundEffect)
        {
            if (soundEffect != null && soundEffect.mixerGroupOverride != null)
                return soundEffect.mixerGroupOverride;

            return GetOrCreateInstance().settings.soundEffectsGroup;
        }

        /// <summary>
        /// Volume for all sound effects, 0 to 1. For a settings menu.
        /// </summary>
        public static float SoundEffectsVolume
        {
            get { return GetOrCreateInstance().soundEffectsVolume; }
            set { GetOrCreateInstance().soundEffectsVolume = Mathf.Clamp01(value); }
        }

        #endregion

        #region Public: music

        /// <summary>
        /// Fades from the current song (if any) into this one, which loops. Asking for the song that is
        /// already playing does nothing, so the same song carries on across scenes without restarting.
        /// </summary>
        /// <param name="fadeDuration">Seconds for the fade. Leave it out (or below 0) to use the settings' default.</param>
        public static void PlayMusic(AudioClip song, float songVolume = 1f, float fadeDuration = -1f)
        {
            GetOrCreateInstance().PlayMusicInternal(song, songVolume, fadeDuration);
        }

        /// <summary>
        /// Fades the music out and stops it.
        /// </summary>
        public static void StopMusic(float fadeDuration = -1f)
        {
            GetOrCreateInstance().PlayMusicInternal(null, 0f, fadeDuration);
        }

        /// <summary>
        /// Muffles the music (as if heard from another room), or brings it back to normal. Good for pause menus.
        /// </summary>
        public static void SetMusicMuffled(bool muffled, float cutoffFrequency = 800f, float fadeDuration = 0.3f)
        {
            GetOrCreateInstance().SetMusicCutoff(muffled ? cutoffFrequency : UnmuffledCutoffFrequency, fadeDuration);
        }

        /// <summary>
        /// Volume for the music, 0 to 1. For a settings menu.
        /// </summary>
        public static float MusicVolume
        {
            get { return GetOrCreateInstance().musicVolume; }
            set
            {
                AudioManager manager = GetOrCreateInstance();
                manager.musicVolume = Mathf.Clamp01(value);

                //change the playing song right away (unless it is in the middle of a fade).
                AudioSource activePlayer = manager.musicPlayers[manager.activeMusicPlayerIndex];
                if (activePlayer.isPlaying && !DOTween.IsTweening(activePlayer))
                    activePlayer.volume = manager.currentSongVolume * manager.musicVolume;
            }
        }

        /// <summary>
        /// The song playing (or fading in) right now, or null.
        /// </summary>
        public static AudioClip CurrentSong
        {
            get
            {
                if (instance == null)
                    return null;

                AudioSource activePlayer = instance.musicPlayers[instance.activeMusicPlayerIndex];
                return activePlayer.isPlaying ? activePlayer.clip : null;
            }
        }

        #endregion

        #region Setup

        private static AudioManager GetOrCreateInstance()
        {
            if (instance != null)
                return instance;

            GameObject managerObject = new GameObject("AudioManager");
            DontDestroyOnLoad(managerObject);
            instance = managerObject.AddComponent<AudioManager>();
            instance.SetUp();
            return instance;
        }

        private void SetUp()
        {
            //optional settings asset at Assets/Resources/AudioManagerSettings.
            settings = Resources.Load<AudioManagerSettings>("AudioManagerSettings");

            if (settings == null)
                settings = ScriptableObject.CreateInstance<AudioManagerSettings>();

            for (int i = 0; i < StartingSoundEffectPlayers; i++)
                AddSoundEffectPlayer();

            musicPlayers = new AudioSource[2];
            musicFilters = new AudioLowPassFilter[2];

            for (int i = 0; i < 2; i++)
            {
                GameObject musicObject = new GameObject("MusicPlayer" + i);
                musicObject.transform.SetParent(transform);

                AudioSource musicPlayer = musicObject.AddComponent<AudioSource>();
                musicPlayer.playOnAwake = false;
                musicPlayer.loop = true;
                musicPlayer.spatialBlend = 0f;   //flat 2D sound
                musicPlayer.outputAudioMixerGroup = settings.musicGroup;
                //keeps playing while the game is paused with Time.timeScale = 0.
                musicPlayer.ignoreListenerPause = true;

                AudioLowPassFilter musicFilter = musicObject.AddComponent<AudioLowPassFilter>();
                musicFilter.cutoffFrequency = UnmuffledCutoffFrequency;

                musicPlayers[i] = musicPlayer;
                musicFilters[i] = musicFilter;
            }
        }

        private AudioSource AddSoundEffectPlayer()
        {
            GameObject playerObject = new GameObject("SoundEffectPlayer" + soundEffectPlayers.Count);
            playerObject.transform.SetParent(transform);

            AudioSource soundEffectPlayer = playerObject.AddComponent<AudioSource>();
            soundEffectPlayer.playOnAwake = false;
            soundEffectPlayer.spatialBlend = 0f;   //flat 2D sound

            //each player has its own filter, so one sound can be muffled without muffling the others.
            AudioLowPassFilter filter = playerObject.AddComponent<AudioLowPassFilter>();
            filter.enabled = false;

            soundEffectPlayers.Add(soundEffectPlayer);
            soundEffectFilters.Add(filter);
            return soundEffectPlayer;
        }

        void OnDestroy()
        {
            if (musicPlayers != null)
            {
                foreach (AudioSource musicPlayer in musicPlayers)
                    DOTween.Kill(musicPlayer);
            }

            DOTween.Kill(this);

            if (instance == this)
                instance = null;
        }

        #endregion

        #region Sound effects

        /// <param name="distanceVolume">Volume multiplier from the hearing range (1 for sounds that aren't in the world).</param>
        /// <param name="distanceCutoffFrequency">Muffling from the hearing range (UnmuffledCutoffFrequency for none).</param>
        private bool PlaySoundEffectInternal(SoundEffect soundEffect, SoundPlayOptions options,
            float distanceVolume, float distanceCutoffFrequency)
        {
            AudioClip clip = soundEffect.PickClip();

            if (clip == null)
            {
                Debug.LogWarning($"AudioManager: The sound effect '{soundEffect.name}' has no clips.", soundEffect);
                return false;
            }

            //unscaledTime, so the limit still works while the game is paused.
            if (Time.unscaledTime - soundEffect.lastPlayTime < soundEffect.minimumTimeBetweenPlays)
                return false;

            soundEffect.lastPlayTime = Time.unscaledTime;

            int playerIndex = FindFreeSoundEffectPlayer();
            AudioSource soundEffectPlayer = soundEffectPlayers[playerIndex];
            AudioLowPassFilter filter = soundEffectFilters[playerIndex];

            soundEffectPlayer.Stop();
            soundEffectPlayer.clip = clip;
            soundEffectPlayer.volume = soundEffect.volume * options.volumeMultiplier * distanceVolume * soundEffectsVolume;
            soundEffectPlayer.pitch = soundEffect.PickPitch(options);
            soundEffectPlayer.outputAudioMixerGroup = soundEffect.mixerGroupOverride != null
                ? soundEffect.mixerGroupOverride
                : settings.soundEffectsGroup;

            //muffling: whichever is more muffled, the options or the distance.
            float cutoffFrequency = UnmuffledCutoffFrequency;
            if (options.muffled)
                cutoffFrequency = options.muffleCutoffFrequency;
            cutoffFrequency = Mathf.Min(cutoffFrequency, distanceCutoffFrequency);

            filter.enabled = cutoffFrequency < UnmuffledCutoffFrequency;
            filter.cutoffFrequency = cutoffFrequency;

            soundEffectPlayer.Play();
            return true;
        }

        /// <summary>
        /// A player that isn't playing anything. If they are all busy, a new one is added, or once the maximum
        /// is reached, the one that has been playing the longest is reused.
        /// </summary>
        private int FindFreeSoundEffectPlayer()
        {
            for (int i = 0; i < soundEffectPlayers.Count; i++)
            {
                if (!soundEffectPlayers[i].isPlaying)
                    return i;
            }

            if (soundEffectPlayers.Count < settings.maximumSimultaneousSounds)
            {
                AddSoundEffectPlayer();
                return soundEffectPlayers.Count - 1;
            }

            //every player is busy: cut off the one furthest through its clip.
            int oldestIndex = 0;
            float furthestProgress = -1f;

            for (int i = 0; i < soundEffectPlayers.Count; i++)
            {
                AudioSource soundEffectPlayer = soundEffectPlayers[i];
                float progress = soundEffectPlayer.clip != null ? soundEffectPlayer.time / soundEffectPlayer.clip.length : 1f;

                if (progress > furthestProgress)
                {
                    furthestProgress = progress;
                    oldestIndex = i;
                }
            }

            return oldestIndex;
        }

        #endregion

        #region Music

        private void PlayMusicInternal(AudioClip song, float songVolume, float fadeDuration)
        {
            if (fadeDuration < 0f)
                fadeDuration = settings.defaultMusicFadeDuration;

            AudioSource currentPlayer = musicPlayers[activeMusicPlayerIndex];

            //the same song is already playing: just make sure it is at the asked volume.
            if (song != null && currentPlayer.isPlaying && currentPlayer.clip == song)
            {
                currentSongVolume = songVolume;
                DOTween.Kill(currentPlayer);
                currentPlayer.DOFade(songVolume * musicVolume, fadeDuration).SetUpdate(true).SetTarget(currentPlayer);
                return;
            }

            //fade out whatever is playing.
            if (currentPlayer.isPlaying)
            {
                DOTween.Kill(currentPlayer);
                currentPlayer.DOFade(0f, fadeDuration)
                    .SetUpdate(true)
                    .SetTarget(currentPlayer)
                    .OnComplete(() => currentPlayer.Stop());
            }

            if (song == null)
                return;

            //fade the new song in on the other player.
            activeMusicPlayerIndex = 1 - activeMusicPlayerIndex;
            AudioSource nextPlayer = musicPlayers[activeMusicPlayerIndex];
            musicFilters[activeMusicPlayerIndex].cutoffFrequency = musicCutoffFrequency;

            currentSongVolume = songVolume;
            DOTween.Kill(nextPlayer);
            nextPlayer.clip = song;
            nextPlayer.volume = 0f;
            nextPlayer.Play();
            nextPlayer.DOFade(songVolume * musicVolume, fadeDuration).SetUpdate(true).SetTarget(nextPlayer);
        }

        private void SetMusicCutoff(float cutoffFrequency, float fadeDuration)
        {
            DOTween.Kill(this);

            //both players, so a song that is fading in gets the same muffling.
            DOTween.To(() => musicCutoffFrequency, value =>
                {
                    musicCutoffFrequency = value;
                    musicFilters[0].cutoffFrequency = value;
                    musicFilters[1].cutoffFrequency = value;
                }, cutoffFrequency, fadeDuration)
                .SetUpdate(true)
                .SetTarget(this);
        }

        #endregion
    }
}
