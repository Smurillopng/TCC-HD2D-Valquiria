using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MuriPNG.Audio
{
    /// <summary>
    /// This class represents the sound manager.
    /// </summary>
    /// <remarks>
    /// It contains the list of available mixers, the list of available audio sources, and the list of available sounds.
    /// </remarks>
    public class SoundManager : MonoBehaviour
    {
        #region === Variables ===============================================================

        public static SoundManager Instance;

        [Tooltip("List of available mixers in the game.")]
        [BoxGroup("Mixer Settings"), SerializeField] private List<Mixer> mixerGroups = new();

        [Tooltip("Maximum number of AudioSources that can be created to play sounds.")]
        [BoxGroup("Source List Settings"), SerializeField] private int sourceLimit = 10;
        [Tooltip("If the list of AudioSources can grow if the limit of sources playing sounds is reached.")]
        [BoxGroup("Source List Settings"), SerializeField] private bool canExpand;
        [Tooltip("List of AudioSources that can be used to play sounds.")]
        [BoxGroup("Source List Settings"), SerializeField] private List<AudioSource> sourceList = new();

        [Tooltip("List of sounds that can be played by the script")]
        [BoxGroup("Sound List"), SerializeField] private List<Sound> sounds;

        #endregion

        #region === Unity Methods ===========================================================

        /// <summary>Initializes the object when it is first created.</summary>
        /// <remarks>
        /// If another instance of the object already exists, this instance is destroyed.
        /// Otherwise, this instance is set as the singleton instance.
        /// The list of sound sources is created and any sounds that should play on awake are played.
        /// The object is marked as not to be destroyed on scene changes.
        /// </remarks>
        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            CreateSourceList();

            foreach (var sound in sounds.Where(sound => sound.PlayOnAwake))
            {
                PlaySound(sound.Name);
            }
            DontDestroyOnLoad(gameObject);
        }
        /// <summary>Updates the audio sources by removing the clip and output audio mixer group from any audio source that is not playing and has a clip.</summary>
        /// <remarks>This method is typically called once per frame.</remarks>
        private void Update()
        {
            foreach (var source in sourceList.Where(audioSource => audioSource.clip != null && !audioSource.isPlaying))
            {
                source.clip = null;
                source.outputAudioMixerGroup = null;
            }
        }

        #endregion

        #region === Methods =================================================================

        /// <summary>Plays a sound clip with the given name.</summary>
        /// <param name="clipName">The name of the sound clip to play.</param>
        /// <remarks>
        /// This method checks if there are any available sound sources and searches for the sound clip with the given name.
        /// If the sound clip is already playing, this method does nothing.
        /// </remarks>
        public void PlaySound(string clipName)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            if (sound.Source.isPlaying) return;
            sound?.Play();
        }
        /// <summary>Plays a sound clip at a specified volume.</summary>
        /// <param name="clipName">The name of the sound clip to play.</param>
        /// <param name="volume">The volume at which to play the sound clip.</param>
        /// <remarks>If the sound clip is not found, no sound will be played.</remarks>
        public void PlaySound(string clipName, float volume)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound.Volume = volume;
            sound?.Play();
        }
        /// <summary>Plays a sound clip once.</summary>
        /// <param name="clipName">The name of the sound clip to play.</param>
        /// <remarks>If the sound clip is not found, nothing happens.</remarks>
        public void PlaySoundOnce(string clipName)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound?.PlayOnce();
        }
        /// <summary>Plays a sound at a specified position.</summary>
        /// <param name="clipName">The name of the sound clip to play.</param>
        /// <param name="position">The position at which to play the sound.</param>
        /// <remarks>If the sound clip is not found, no sound will be played.</remarks>
        public void PlaySoundOnPosition(string clipName, Vector3 position)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound?.PlayOnPosition(position);
        }
        /// <summary>Plays a sound with a delay.</summary>
        /// <param name="clipName">The name of the sound clip to play.</param>
        /// <param name="delay">The delay in seconds before playing the sound.</param>
        /// <remarks>If the sound clip is not found, nothing happens.</remarks>
        public void PlaySoundDelayed(string clipName, float delay)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound?.PlayDelayed(delay);
        }
        /// <summary>Plays a sound clip from a specified start time to a specified end time.</summary>
        /// <param name="clipName">The name of the sound clip to play.</param>
        /// <param name="fromSeconds">The start time of the clip, in seconds.</param>
        /// <param name="toSeconds">The end time of the clip, in seconds.</param>
        /// <exception cref="NullReferenceException">Thrown when the sound clip is not found.</exception>
        public void PlaySoundInterval(string clipName, float fromSeconds, float toSeconds)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound.Source.time = fromSeconds;
            sound?.Play();
            sound.Source.SetScheduledEndTime(AudioSettings.dspTime + (toSeconds - fromSeconds));
        }
        /// <summary>Plays a sound with an intro.</summary>
        /// <param name="introName">The name of the intro sound.</param>
        /// <param name="clipName">The name of the main sound.</param>
        /// <remarks>
        /// This method first checks if there are available audio sources to play the sounds. If there are, it searches for the intro and main sounds by name.
        /// If the intro sound is found, it is played. Then, the length of the intro is calculated and used to delay the start of the main sound.
        /// </remarks>
        public void PlaySoundWithIntro(string introName, string clipName)
        {
            CheckAvailableSources();
            var intro = SearchSound(introName);
            var sound = SearchSound(clipName);
            intro?.Play();
            if (intro == null) return;
            var introLenght = (intro.Source.clip.length / intro.Source.pitch);
            StartCoroutine(IntroCoroutine(sound, introLenght));
        }
        /// <summary>Plays a sound with a random pitch.</summary>
        /// <param name="clipName">The name of the sound clip to play.</param>
        /// <remarks>
        /// This method first checks if there are any available sound sources. If there are, it searches for the sound clip with the given name and sets its pitch to a random value between 0.1 and 3.0. If the sound clip is found, it is played.
        /// </remarks>
        public void PlaySoundWithRandomPitch(string clipName)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound.Pitch = Random.Range(0.1f, 3f);
            sound?.Play();
        }
        /// <summary>Plays a sound with a random pitch.</summary>
        /// <param name="clipName">The name of the sound clip to play.</param>
        /// <param name="minimumPitch">The minimum pitch value to use.</param>
        /// <param name="maximumPitch">The maximum pitch value to use.</param>
        /// <exception cref="System.NullReferenceException">Thrown when the sound clip is not found.</exception>
        public void PlaySoundWithRandomPitch(string clipName, float minimumPitch, float maximumPitch)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound.Pitch = Random.Range(minimumPitch, maximumPitch);
            sound?.Play();
        }
        /// <summary>Stops the sound with the specified name.</summary>
        /// <param name="clipName">The name of the sound to stop.</param>
        /// <remarks>If no sound with the specified name is found, nothing happens.</remarks>
        public void StopSound(string clipName)
        {
            var sound = sounds.Find(sound => sound.Name == clipName);
            sound?.Stop();
        }
        /// <summary>Stops all currently playing sounds.</summary>
        /// <remarks>This method iterates through the list of audio sources and stops each one that is currently playing.</remarks>
        public void StopAllSounds()
        {
            foreach (var source in sourceList.Where(audioSource => audioSource.isPlaying))
                source?.Stop();
        }
        /// <summary>Sets the volume of a specified mixer.</summary>
        /// <param name="mixerName">The name of the mixer to set the volume for.</param>
        /// <param name="volume">The volume to set the mixer to.</param>
        /// <remarks>If the specified mixer is not found, the method does nothing.</remarks>
        public void SetMixerVolume(string mixerName, float volume)
        {
            var mixer = mixerGroups.Find(mixer => mixer.MixerID == mixerName);
            mixer?.SetVolume(volume);
        }
        /// <summary>Mutes the specified mixer.</summary>
        /// <param name="mixerName">The name of the mixer to mute.</param>
        /// <remarks>If the specified mixer is not found, nothing happens.</remarks>
        public void MuteMixer(string mixerName)
        {
            var mixer = mixerGroups.Find(mixer => mixer.MixerID == mixerName);
            mixer?.SetMute(true);
        }
        /// <summary>Plays the specified mixer.</summary>
        /// <param name="mixerName">The name of the mixer to play.</param>
        /// <remarks>If the specified mixer is not found, nothing happens.</remarks>
        public void PlayMixer(string mixerName)
        {
            var mixer = mixerGroups.Find(mixer => mixer.MixerID == mixerName);
            mixer?.SetMute(false);
        }
        /// <summary>Determines if a specific audio clip is currently playing.</summary>
        /// <param name="clipName">The name of the audio clip to check.</param>
        /// <returns>True if the audio clip is currently playing, false otherwise.</returns>
        public bool IsPlaying(string clipName)
        {
            return sourceList.Any(audioSource =>
            {
                AudioClip clip;
                return (clip = audioSource.clip) != null && clip.name == clipName && audioSource.isPlaying;
            });
        }
        /// <summary>Fades out a sound over a specified time period.</summary>
        /// <param name="clipName">The name of the sound clip to fade out.</param>
        /// <param name="fadeTime">The time period over which to fade out the sound.</param>
        public void FadeOutSound(string clipName, float fadeTime)
        {
            var sound = sounds.Find(sound => sound.Name == clipName);

            StartCoroutine(FadeCoroutine(sound, fadeTime));
        }
        /// <summary>Fades out a sound over a specified time period.</summary>
        /// <param name="sound">The sound to fade out.</param>
        /// <param name="fadeTime">The time period over which to fade out the sound.</param>
        /// <returns>An IEnumerator that can be used to execute the fade out effect.</returns>
        private IEnumerator FadeCoroutine(Sound sound, float fadeTime)
        {
            var initialVolume = sound.Source.volume;

            while (sound.Source.volume > 0)
            {
                sound.Source.volume -= initialVolume * Time.deltaTime / fadeTime;

                yield return null;
            }

            StopSound(sound.Name);
            sound.Source.volume = initialVolume;
        }
        /// <summary>Searches for a sound with the given name and sets its source.</summary>
        /// <param name="s">The name of the sound to search for.</param>
        /// <returns>The sound with the given name, or null if it is not found.</returns>
        /// <remarks>If the sound is not found, a warning message is logged.</remarks>
        private Sound SearchSound(string s)
        {
            var sound = sounds.Find(sound => sound.Name == s);
            if (sound == null)
            {
                Debug.LogWarning($"Sound [{s}] not found!");
                return null;
            }
            var source = SearchSource();
            sound.Source = source;
            return sound;
        }
        /// <summary>Creates a list of audio sources.</summary>
        /// <remarks>The number of sources is determined by the value of sourceLimit.</remarks>
        private void CreateSourceList()
        {
            for (var i = 0; i < sourceLimit; i++)
            {
                var newSource = new GameObject("Source " + i)
                {
                    transform =
                    {
                        parent = transform
                    }
                };
                newSource.AddComponent<AudioSource>();
                sourceList.Add(newSource.GetComponent<AudioSource>());
            }
        }
        /// <summary>Searches for an available audio source.</summary>
        /// <returns>The first available audio source found in the list of audio sources.</returns>
        private AudioSource SearchSource()
        {
            return sourceList.FirstOrDefault(source => !source.isPlaying);
        }
        /// <summary>Checks if there are available audio sources to expand and creates a new one if possible.</summary>
        /// <remarks>
        /// An audio source can be expanded if the current number of sources is less than the maximum allowed and all existing sources have a clip assigned to them.
        /// </remarks>
        private void CheckAvailableSources()
        {
            if (canExpand && sourceList.All(fonte => fonte.clip != null))
            {
                var newSource = new GameObject("Source " + sourceList.Count)
                {
                    transform =
                    {
                        parent = transform
                    }
                };
                newSource.AddComponent<AudioSource>();
                sourceList.Add(newSource.GetComponent<AudioSource>());
            }
        }
        /// <summary>Coroutine that plays a sound after a specified intro time.</summary>
        /// <param name="sound">The sound to play.</param>
        /// <param name="introTime">The time to wait before playing the sound.</param>
        /// <returns>An IEnumerator that can be used in a coroutine.</returns>
        private static IEnumerator IntroCoroutine(Sound sound, float introTime)
        {
            yield return new WaitForSeconds(introTime);
            sound?.Play();
        }
        /// <summary>Updates the sound settings for all sounds in the collection.</summary>
        /// <remarks>
        /// This method iterates through all sounds in the collection and applies the sound settings to each one.
        /// </remarks>
        [BoxGroup("Debug"), Button]
        public void UpdateSoundSettings()
        {
            foreach (var sound in sounds)
            {
                if (sound.Source is null) return;
                sound.ApplySoundSettings();
            }
        }
        /// <summary>Updates the mixer settings for all mixer groups.</summary>
        /// <remarks>This method is intended for debugging purposes.</remarks>
        [BoxGroup("Debug"), Button]
        public void UpdateMixerSettings()
        {
            foreach (var mixer in mixerGroups)
            {
                mixer.ApplyMixerSettings();
            }
        }
        /// <summary>Plays a sound sample once.</summary>
        /// <param name="sampleName">The name of the sound sample to play.</param>
        [BoxGroup("Debug"), Button("Play Sample")]
        public void PlaySample(string sampleName)
        {
            PlaySoundOnce(sampleName);
        }

        #endregion
    }
}