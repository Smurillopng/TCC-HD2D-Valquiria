// Created by Sérgio Murillo da Costa Faria

using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace MuriPNG.Audio
{
    [Serializable]
    public class Mixer
    {
        #region === Variables ===============================================================

        [FoldoutGroup("$mixerID")]
        [BoxGroup("$mixerID/Settings")]
        [SerializeField, Tooltip("Mixer ID")] 
        private string mixerID;
        
        [BoxGroup("$mixerID/Settings")]
        [SerializeField, Tooltip("Mixer Group")] 
        private AudioMixerGroup mixerGroup;
        
        [BoxGroup("$mixerID/Settings")]
        [SerializeField, Tooltip("Exposed Parameter")] 
        private string exposedParameterName;
        
        [BoxGroup("$mixerID/Settings")]
        [Range(-80f, 0f), SerializeField, Tooltip("Mixer's Volume")] 
        private float volume;
        
        [BoxGroup("$mixerID/Settings")]
        [SerializeField, Tooltip("Is the Mixer Muted?")] 
        private bool mute;

        #endregion ==========================================================================

        #region === Properties ==============================================================

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

        #endregion ==========================================================================

        #region === Methods =================================================================

        /// <summary>Sets the volume of an audio mixer group.</summary>
        /// <param name="volumeValue">The new volume value.</param>
        /// <remarks>
        /// This method sets the volume of an audio mixer group to the specified value. If the mixer group or the audio mixer is null, the method does nothing.
        /// </remarks>
        public void SetVolume(float volumeValue)
        {
            volume = volumeValue;
            if (mixerGroup != null && mixerGroup.audioMixer != null) mixerGroup.audioMixer.SetFloat(exposedParameterName, volume);
        }
        /// <summary>Sets the mute state of an audio mixer group.</summary>
        /// <param name="muteMixer">True to mute the mixer, false to unmute it.</param>
        /// <remarks>
        /// If the mixer group and audio mixer are not null, sets the exposed parameter of the audio mixer to -80f if muteMixer is true,
        /// or to the current volume if muteMixer is false.
        /// </remarks>
        public void SetMute(bool muteMixer)
        {
            mute = muteMixer;
            if (mixerGroup != null && mixerGroup.audioMixer != null) mixerGroup.audioMixer.SetFloat(exposedParameterName, mute ? -80f : volume);
        }
        /// <summary>Applies the current mixer settings to the audio mixer.</summary>
        /// <remarks>
        /// If the mixer group or audio mixer is null, no action is taken.
        /// The exposed parameter name is used to set the volume of the mixer.
        /// If the mute flag is set, the volume is set to -80f, otherwise it is set to the current volume.
        /// </remarks>
        public void ApplyMixerSettings()
        {
            if (mixerGroup != null && mixerGroup.audioMixer != null) mixerGroup.audioMixer.SetFloat(exposedParameterName, mute ? -80f : volume);
        }

        #endregion ==========================================================================
    }
}