using System;
using System.Collections.Generic;
using Atomic.Elements;
using UnityEngine;

namespace Audio.Integrations.Settings
{
    /// <summary>
    /// Binds IAudioVolumeSettings reactive volumes to SoftwareMixer.
    /// Provides sync: Settings -> Mixer.
    /// </summary>
    public sealed class SettingsVolumeBinder : IDisposable
    {
        private readonly ISoftwareMixer _mixer;
        private readonly IAudioVolumeSettings _settings;
        private readonly List<IDisposable> _subscriptions = new();

        public SettingsVolumeBinder(ISoftwareMixer mixer, IAudioVolumeSettings settings)
        {
            _mixer = mixer;
            _settings = settings;
        }

        /// <summary>
        /// Bind all volume settings to mixer.
        /// </summary>
        public void Bind()
        {
            if (_settings == null)
            {
                Debug.LogWarning("[SettingsVolumeBinder] Settings is null");
                return;
            }

            // SFX Volume
            if (_settings.SfxVolume != null)
            {
                // Set initial value
                _mixer.SetVolume(AudioLayer.SFX, _settings.SfxVolume.Value);

                // Subscribe to changes
                _subscriptions.Add(_settings.SfxVolume.Subscribe(vol =>
                {
                    if (Math.Abs(_mixer.GetVolume(AudioLayer.SFX) - vol) > 0.001f)
                    {
                        _mixer.SetVolume(AudioLayer.SFX, vol);
                    }
                }));
            }

            // Music Volume
            if (_settings.MusicVolume != null)
            {
                _mixer.SetVolume(AudioLayer.Music, _settings.MusicVolume.Value);

                _subscriptions.Add(_settings.MusicVolume.Subscribe(vol =>
                {
                    if (Math.Abs(_mixer.GetVolume(AudioLayer.Music) - vol) > 0.001f)
                    {
                        _mixer.SetVolume(AudioLayer.Music, vol);
                    }
                }));
            }

            // Dialogue Volume
            if (_settings.DialogueVolume != null)
            {
                _mixer.SetVolume(AudioLayer.Dialogue, _settings.DialogueVolume.Value);

                _subscriptions.Add(_settings.DialogueVolume.Subscribe(vol =>
                {
                    if (Math.Abs(_mixer.GetVolume(AudioLayer.Dialogue) - vol) > 0.001f)
                    {
                        _mixer.SetVolume(AudioLayer.Dialogue, vol);
                    }
                }));
            }

            // Master is always 1.0 (individual layers control volume)
            _mixer.SetVolume(AudioLayer.Master, 1f);

            Debug.Log("[SettingsVolumeBinder] Bound volumes to settings");
        }

        /// <summary>
        /// Unbind and dispose subscriptions.
        /// </summary>
        public void Dispose()
        {
            foreach (var sub in _subscriptions)
            {
                sub?.Dispose();
            }
            _subscriptions.Clear();
        }
    }
}
