using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MuriPNG.Audio
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;

        #region Inspector Variables

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

        #region Unity Methods

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

        private void Update()
        {
            foreach (var source in sourceList.Where(audioSource => audioSource.clip != null && !audioSource.isPlaying))
            {
                source.clip = null;
                source.outputAudioMixerGroup = null;
            }
        }

        #endregion

        #region Methods

        public void PlaySound(string clipName)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            if (sound.Source.isPlaying) return;
            sound.Play();
        }
        public void PlaySound(string clipName, float volume)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound.Volume = volume;
            sound.Play();
        }
        public void PlaySoundOnce(string clipName)
        {
            var sound = SearchSound(clipName);
            sound?.PlayOnce();
        }
        public void PlaySoundOnPosition(string clipName, Vector3 position)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound?.PlayOnPosition(position);
        }
        public void PlaySoundDelayed(string clipName, float delay)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound?.PlayDelayed(delay);
        }
        public void PlaySoundInterval(string clipName, float fromSeconds, float toSeconds)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound.Source.time = fromSeconds;
            sound.Play();
            sound.Source.SetScheduledEndTime(AudioSettings.dspTime + (toSeconds - fromSeconds));
        }
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

        public void PlaySoundWithRandomPitch(string clipName)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound.Pitch = Random.Range(0.1f, 3f);
            sound.Play();
        }
        public void PlaySoundWithRandomPitch(string clipName, float minimumPitch, float maximumPitch)
        {
            CheckAvailableSources();
            var sound = SearchSound(clipName);
            sound.Pitch = Random.Range(minimumPitch, maximumPitch);
            sound.Play();
        }

        public void StopSound(string clipName)
        {
            var sound = sounds.Find(sound => sound.Name == clipName);
            sound?.Stop();
        }

        public void StopAllSounds()
        {
            foreach (var source in sourceList.Where(audioSource => audioSource.isPlaying))
                source.Stop();
        }

        public void SetMixerVolume(string mixerName, float volume)
        {
            var mixer = mixerGroups.Find(mixer => mixer.MixerID == mixerName);
            mixer?.SetVolume(volume);
        }
        public void MuteMixer(string mixerName)
        {
            var mixer = mixerGroups.Find(mixer => mixer.MixerID == mixerName);
            mixer?.MuteMixer(true);
        }
        public void PlayMixer(string mixerName)
        {
            var mixer = mixerGroups.Find(mixer => mixer.MixerID == mixerName);
            mixer?.MuteMixer(false);
        }

        public bool IsPlaying(string clipName)
        {
            return sourceList.Any(audioSource =>
            {
                AudioClip clip;
                return (clip = audioSource.clip) != null && clip.name == clipName && audioSource.isPlaying;
            });
        }

        public void FadeOutSound(string clipName, float fadeTime)
        {
            var sound = sounds.Find(sound => sound.Name == clipName);

            StartCoroutine(FadeCoroutine(sound, fadeTime));
        }

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

        private AudioSource SearchSource()
        {
            return sourceList.FirstOrDefault(source => !source.isPlaying);
        }

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


        private static IEnumerator IntroCoroutine(Sound sound, float introTime)
        {
            yield return new WaitForSeconds(introTime);
            sound?.Play();
        }

        [Button]
        public void UpdateSoundSettings()
        {
            foreach (var sound in sounds)
            {
                if (sound.Source is null) return;
                sound.ApplySoundSettings();
            }
        }

        [Button]
        public void UpdateMixerSettings()
        {
            foreach (var mixer in mixerGroups)
            {
                mixer.ApplyMixerSettings();
            }
        }

        #endregion
    }
}