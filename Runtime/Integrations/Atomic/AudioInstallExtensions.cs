using System;
using Atomic.Entities;
using Audio.Integrations.Settings;
using Localization;
using UnityEngine;

namespace Audio.Integrations.Atomic
{
    /// <summary>
    /// Extension methods for installing Audio module into entities.
    /// </summary>
    public static class AudioInstallExtensions
    {
        private static readonly int AudioServiceId = EntityNames.NameToId("AudioService");
        private static readonly int AudioConfigId = EntityNames.NameToId("AudioConfig");

        /// <summary>
        /// Install Audio module into entity (typically ProjectContext).
        /// </summary>
        /// <param name="entity">Entity to install into.</param>
        /// <param name="config">Audio configuration.</param>
        /// <param name="volumeSettings">Volume settings for binding (optional).</param>
        /// <param name="getCurrentLanguage">Function to get current language (for localized audio).</param>
        /// <returns>True if installation succeeded.</returns>
        public static bool InstallAudio(
            this IEntity entity,
            AudioConfig config,
            IAudioVolumeSettings volumeSettings = null,
            Func<LanguageCode> getCurrentLanguage = null)
        {
            var result = new AudioInstaller(config, volumeSettings, getCurrentLanguage).Install();
            if (!result.IsValid)
            {
                return false;
            }

            entity.AddValue(AudioServiceId, result.Service);
            entity.AddValue(AudioConfigId, result.Config);
            return true;
        }
    }

    /// <summary>
    /// Result of audio installation.
    /// </summary>
    public readonly struct AudioInstallResult
    {
        public readonly IAudioService Service;
        public readonly AudioConfig Config;

        public bool IsValid => Service != null;

        public AudioInstallResult(IAudioService service, AudioConfig config)
        {
            Service = service;
            Config = config;
        }
    }

    /// <summary>
    /// Installer for Audio module.
    /// Creates all services and registers them globally.
    /// </summary>
    public sealed class AudioInstaller
    {
        private readonly AudioConfig _config;
        private readonly IAudioVolumeSettings _volumeSettings;
        private readonly Func<LanguageCode> _getCurrentLanguage;

        public AudioInstaller(
            AudioConfig config,
            IAudioVolumeSettings volumeSettings,
            Func<LanguageCode> getCurrentLanguage = null)
        {
            _config = config;
            _volumeSettings = volumeSettings;
            _getCurrentLanguage = getCurrentLanguage;
        }

        /// <summary>
        /// Install audio module and return created service.
        /// </summary>
        public AudioInstallResult Install()
        {
            if (_config == null)
            {
                Debug.LogWarning("[Audio] Config is null, cannot install");
                return default;
            }

            if (_config.Database == null)
            {
                Debug.LogWarning("[Audio] Database is null, cannot install");
                return default;
            }

            // Create mixer
            var mixer = new SoftwareMixer();
            mixer.SetDuckingEnabled(_config.DuckingEnabled);
            mixer.SetDuckingAmount(_config.DuckingAmount);

            // Create pool
            var pool = new AudioSourcePool(
                _config.SfxPoolSize,
                _config.MusicSources,
                _config.DialogueSources
            );

            // Create providers
            var directProvider = new DirectClipProvider(_config.Database, _getCurrentLanguage);
            var addressableProvider = new AddressableClipProvider(_config.Database);

            // Create service
            var service = new AudioService(
                _config,
                mixer,
                pool,
                directProvider,
                addressableProvider
            );

            // Bind to volume settings
            if (_volumeSettings != null)
            {
                var binder = new Settings.SettingsVolumeBinder(mixer, _volumeSettings);
                binder.Bind();
            }

#if GAME_PUSH_ENABLED
            // Bind to GamePush
            var gpBridge = new GamePush.GamePushAudioBridge(mixer);
            gpBridge.Initialize();
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
            // Create WebGL audio activator to handle browser autoplay policy
            var activatorGo = new GameObject("[WebGLAudioActivator]");
            activatorGo.AddComponent<WebGL.WebGLAudioActivator>();
            UnityEngine.Object.DontDestroyOnLoad(activatorGo);
            Debug.Log("[Audio] Created WebGL audio activator");
#endif

            // Register globally
            AudioSystemProvider.Register(service, _config);

            Debug.Log("[Audio] Installed successfully");
            return new AudioInstallResult(service, _config);
        }
    }
}
