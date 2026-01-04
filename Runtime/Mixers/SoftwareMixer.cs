using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Software-based audio mixer with ducking support.
    /// Manages volume and mute state for all audio layers.
    /// </summary>
    public sealed class SoftwareMixer : ISoftwareMixer
    {
        private readonly Dictionary<AudioLayer, MixerLayerState> _layers;

        // Ducking state
        private bool _duckingEnabled = true;
        private float _duckingAmount = 0.3f;
        private int _activeDialogueCount;

        public bool IsDuckingEnabled => _duckingEnabled;

        public event Action<AudioLayer, float> OnVolumeChanged;
        public event Action<AudioLayer, bool> OnMuteChanged;

        public SoftwareMixer()
        {
            _layers = new Dictionary<AudioLayer, MixerLayerState>
            {
                { AudioLayer.Master, new MixerLayerState(1f, false) },
                { AudioLayer.SFX, new MixerLayerState(1f, false) },
                { AudioLayer.Music, new MixerLayerState(1f, false) },
                { AudioLayer.Dialogue, new MixerLayerState(1f, false) }
            };
        }

        public void SetVolume(AudioLayer layer, float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (!_layers.TryGetValue(layer, out var state))
            {
                return;
            }

            if (Mathf.Approximately(state.Volume, volume))
            {
                return;
            }

            state.Volume = volume;
            OnVolumeChanged?.Invoke(layer, volume);
        }

        public float GetVolume(AudioLayer layer)
        {
            return _layers.TryGetValue(layer, out var state) ? state.Volume : 0f;
        }

        public void SetMuted(AudioLayer layer, bool muted)
        {
            if (!_layers.TryGetValue(layer, out var state))
            {
                return;
            }

            if (state.IsMuted == muted)
            {
                return;
            }

            state.IsMuted = muted;
            OnMuteChanged?.Invoke(layer, muted);
        }

        public bool IsMuted(AudioLayer layer)
        {
            return _layers.TryGetValue(layer, out var state) && state.IsMuted;
        }

        public float GetEffectiveVolume(AudioLayer layer)
        {
            if (!_layers.TryGetValue(layer, out var state))
            {
                return 0f;
            }

            var masterState = _layers[AudioLayer.Master];

            // Check mute states
            if (state.IsMuted || masterState.IsMuted)
            {
                return 0f;
            }

            var volume = state.Volume * masterState.Volume;

            // Apply ducking to Music when Dialogue is active
            if (layer == AudioLayer.Music && _duckingEnabled && _activeDialogueCount > 0)
            {
                volume *= _duckingAmount;
            }

            return volume;
        }

        public void OnDialogueStarted()
        {
            _activeDialogueCount++;
        }

        public void OnDialogueStopped()
        {
            _activeDialogueCount = Math.Max(0, _activeDialogueCount - 1);
        }

        public void SetDuckingEnabled(bool enabled)
        {
            _duckingEnabled = enabled;
        }

        public void SetDuckingAmount(float amount)
        {
            _duckingAmount = Mathf.Clamp01(amount);
        }

        /// <summary>
        /// Get ducking multiplier for a layer.
        /// </summary>
        public float GetDuckingMultiplier(AudioLayer layer)
        {
            if (layer == AudioLayer.Music && _duckingEnabled && _activeDialogueCount > 0)
            {
                return _duckingAmount;
            }

            return 1f;
        }
    }
}
