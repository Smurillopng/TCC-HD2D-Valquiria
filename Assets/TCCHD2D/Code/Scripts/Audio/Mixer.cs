using System;
using UnityEngine;
using UnityEngine.Audio;

namespace MuriPNG.Audio
{
    [Serializable]
    public class Mixer
    {
        #region Inspector Variables

        [Tooltip("Mixer ID")]
        [SerializeField] private string mixerID;
        [Tooltip("Mixer Group")]
        [SerializeField] private AudioMixerGroup mixerGroup;
        [Tooltip("Exposed Parameter")]
        [SerializeField] private string exposedParameterName;
        [Tooltip("Mixer's Volume")]
        [Range(-80f, 0f)][SerializeField] private float volume;
        [Tooltip("Is the Mixer Muted?")]
        [SerializeField] private bool mute;

        #endregion

        #region Properties

        public string MixerID => mixerID;
        public AudioMixerGroup MixerGroup => mixerGroup;
        public string ExposedParameterName => exposedParameterName;
        public float Volume
        {
            get => volume;
            set => volume = value;
        }
        public bool Mute
        {
            get => mute;
            set => mute = value;
        }

        #endregion

        #region Methods

        public void SetVolume(float volumeValue) // Change the volume of the mixer
        {
            volume = volumeValue;
            if (mixerGroup?.audioMixer != null) mixerGroup.audioMixer.SetFloat(exposedParameterName, volume);
        }

        public void MuteMixer(bool muteMixer) // Mute the mixer
        {
            mute = muteMixer;
            if (mixerGroup?.audioMixer != null) mixerGroup.audioMixer.SetFloat(exposedParameterName, mute ? -80f : volume);
        }

        public void ApplyMixerSettings() // Update mixer settings
        {
            if (mixerGroup?.audioMixer != null) mixerGroup.audioMixer.SetFloat(exposedParameterName, volume);
            if (mixerGroup?.audioMixer != null) mixerGroup.audioMixer.SetFloat(exposedParameterName, mute ? -80f : volume);
        }

        #endregion
    }
}