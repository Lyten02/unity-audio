using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Represents an active audio playback channel.
    /// Manages a single AudioSource during playback.
    /// </summary>
    public sealed class AudioChannel
    {
        private static int _nextId = 1;

        public int Id { get; }
        public int AudioKey { get; }
        public AudioLayer Layer { get; }
        public AudioSource Source { get; }
        public bool IsPlaying => Source != null && Source.isPlaying;

        private readonly ISoftwareMixer _mixer;
        private readonly float _baseVolume;
        private float _settingsVolume = 1f;

        public AudioChannel(int audioKey, AudioLayer layer, AudioSource source, ISoftwareMixer mixer, float baseVolume)
        {
            Id = _nextId++;
            AudioKey = audioKey;
            Layer = layer;
            Source = source;
            _mixer = mixer;
            _baseVolume = baseVolume;

            UpdateVolume();
        }

        /// <summary>
        /// Update volume based on mixer state.
        /// </summary>
        public void UpdateVolume()
        {
            if (Source == null) return;

            var effectiveVolume = _mixer.GetEffectiveVolume(Layer);
            Source.volume = VolumeCalculator.Calculate(
                _baseVolume * _settingsVolume,
                effectiveVolume,
                1f, // Master is already included in effective
                false);
        }

        /// <summary>
        /// Set playback settings volume multiplier.
        /// </summary>
        public void SetSettingsVolume(float volume)
        {
            _settingsVolume = Mathf.Clamp01(volume);
            UpdateVolume();
        }

        /// <summary>
        /// Stop playback.
        /// </summary>
        public void Stop()
        {
            if (Source != null)
            {
                Source.Stop();
                Source.clip = null;
            }
        }

        /// <summary>
        /// Fade out and stop.
        /// </summary>
        public async UniTask FadeOutAsync(float duration)
        {
            if (Source == null || !Source.isPlaying)
            {
                return;
            }

            float startVolume = Source.volume;
            float elapsed = 0f;

            while (elapsed < duration && Source != null && Source.isPlaying)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Source.volume = Mathf.Lerp(startVolume, 0f, t);
                await UniTask.Yield();
            }

            Stop();
        }

        /// <summary>
        /// Create AudioHandle for this channel.
        /// </summary>
        public AudioHandle CreateHandle(IAudioService service)
        {
            return new AudioHandle(Id, AudioKey, Layer, Source, service);
        }
    }
}
