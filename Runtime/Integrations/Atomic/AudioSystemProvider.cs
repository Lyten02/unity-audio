using System;
using UnityEngine;

namespace Audio.Integrations.Atomic
{
    /// <summary>
    /// Global static access to audio service.
    /// Used by extension methods and scene components.
    /// </summary>
    public static class AudioSystemProvider
    {
        private static IAudioService _service;
        private static AudioConfig _config;

        /// <summary>
        /// Fired when audio service is registered.
        /// </summary>
        public static event Action OnInitialized;

        /// <summary>
        /// Global audio service instance.
        /// </summary>
        public static IAudioService Service => _service;

        /// <summary>
        /// Global audio config instance.
        /// </summary>
        public static AudioConfig Config => _config;

        /// <summary>
        /// Whether audio service is initialized.
        /// </summary>
        public static bool IsInitialized => _service != null;

        /// <summary>
        /// Register audio service globally.
        /// </summary>
        public static void Register(IAudioService service, AudioConfig config)
        {
            _service = service;
            _config = config;

            Debug.Log($"[AudioSystemProvider] Registered: {service?.GetType().Name}");
            OnInitialized?.Invoke();
        }

        /// <summary>
        /// Clear registration.
        /// </summary>
        public static void Clear()
        {
            _service = null;
            _config = null;
        }

        // === Convenience methods ===

        /// <summary>
        /// Play sound using global service.
        /// </summary>
        public static AudioHandle Play(int audioKey)
        {
            if (_service == null)
            {
                Debug.LogWarning("[AudioSystemProvider] Not initialized");
                return AudioHandle.Invalid;
            }
            return _service.Play(audioKey);
        }

        /// <summary>
        /// Play sound with settings.
        /// </summary>
        public static AudioHandle Play(int audioKey, AudioPlaySettings settings)
        {
            if (_service == null)
            {
                Debug.LogWarning("[AudioSystemProvider] Not initialized");
                return AudioHandle.Invalid;
            }
            return _service.Play(audioKey, settings);
        }

        /// <summary>
        /// Play music with cross-fade.
        /// </summary>
        public static void PlayMusic(int audioKey, float fadeDuration = 1f)
        {
            _service?.PlayMusic(audioKey, fadeDuration);
        }

        /// <summary>
        /// Stop all sounds.
        /// </summary>
        public static void StopAll()
        {
            _service?.StopAll();
        }

        // === Audio Groups ===

        /// <summary>
        /// Play random sound from group.
        /// </summary>
        public static AudioHandle PlayGroup(int groupKey)
        {
            if (_service == null)
            {
                Debug.LogWarning("[AudioSystemProvider] Not initialized");
                return AudioHandle.Invalid;
            }
            return _service.PlayGroup(groupKey);
        }

        /// <summary>
        /// Play random sound from group with settings.
        /// </summary>
        public static AudioHandle PlayGroup(int groupKey, AudioPlaySettings settings)
        {
            if (_service == null)
            {
                Debug.LogWarning("[AudioSystemProvider] Not initialized");
                return AudioHandle.Invalid;
            }
            return _service.PlayGroup(groupKey, settings);
        }

        // === Playlists ===

        /// <summary>
        /// Start music playlist.
        /// </summary>
        public static PlaylistHandle PlayPlaylist(int groupKey, float fadeDuration = 1f)
        {
            return _service?.PlayPlaylist(groupKey, fadeDuration) ?? PlaylistHandle.Invalid;
        }

        /// <summary>
        /// Stop current playlist.
        /// </summary>
        public static void StopPlaylist(float fadeDuration = 1f)
        {
            _service?.StopPlaylist(fadeDuration);
        }

        /// <summary>
        /// Pause current playlist.
        /// </summary>
        public static void PausePlaylist()
        {
            _service?.PausePlaylist();
        }

        /// <summary>
        /// Resume paused playlist.
        /// </summary>
        public static void ResumePlaylist()
        {
            _service?.ResumePlaylist();
        }

        /// <summary>
        /// Skip to next track in playlist.
        /// </summary>
        public static void SkipTrack()
        {
            _service?.SkipTrack();
        }

        /// <summary>
        /// Get current playlist handle.
        /// </summary>
        public static PlaylistHandle CurrentPlaylist => _service?.CurrentPlaylist ?? PlaylistHandle.Invalid;

        /// <summary>
        /// Mark audio service as ready (for WebGL after user interaction).
        /// </summary>
        public static void MarkReady()
        {
            if (_service is AudioService audioService)
            {
                audioService.SetReady();
                Debug.Log("[AudioSystemProvider] Marked as ready (user interaction detected)");
            }
        }
    }
}
