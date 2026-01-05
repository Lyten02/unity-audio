using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Audio
{
    /// <summary>
    /// Main interface for the audio service.
    /// Provides all audio playback and control functionality.
    /// </summary>
    public interface IAudioService : IDisposable
    {
        // === State ===

        /// <summary>
        /// Whether the audio system is ready (AudioContext activated on WebGL).
        /// </summary>
        bool IsReady { get; }

        // === Play ===

        /// <summary>
        /// Play sound by audio key.
        /// </summary>
        AudioHandle Play(int audioKey);

        /// <summary>
        /// Play sound with custom settings.
        /// </summary>
        AudioHandle Play(int audioKey, AudioPlaySettings settings);

        /// <summary>
        /// Play sound asynchronously (waits for clip to load if needed).
        /// </summary>
        UniTask<AudioHandle> PlayAsync(int audioKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Play sound asynchronously with custom settings.
        /// </summary>
        UniTask<AudioHandle> PlayAsync(int audioKey, AudioPlaySettings settings, CancellationToken cancellationToken = default);

        // === Music ===

        /// <summary>
        /// Play music with cross-fade.
        /// </summary>
        void PlayMusic(int audioKey, float fadeDuration = 1f);

        /// <summary>
        /// Play music asynchronously with cross-fade.
        /// </summary>
        UniTask PlayMusicAsync(int audioKey, float fadeDuration = 1f, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop current music with fade out.
        /// </summary>
        void StopMusic(float fadeDuration = 1f);

        // === Audio Groups ===

        /// <summary>
        /// Play random sound from group (one-shot, for SFX groups).
        /// </summary>
        AudioHandle PlayGroup(int groupKey);

        /// <summary>
        /// Play random sound from group with settings.
        /// </summary>
        AudioHandle PlayGroup(int groupKey, AudioPlaySettings settings);

        // === Music Playlists ===

        /// <summary>
        /// Start music playlist with crossfade.
        /// </summary>
        PlaylistHandle PlayPlaylist(int groupKey, float fadeDuration = 1f);

        /// <summary>
        /// Start music playlist asynchronously.
        /// </summary>
        UniTask<PlaylistHandle> PlayPlaylistAsync(int groupKey, float fadeDuration = 1f, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop current playlist with fade out.
        /// </summary>
        void StopPlaylist(float fadeDuration = 1f);

        /// <summary>
        /// Pause current playlist.
        /// </summary>
        void PausePlaylist();

        /// <summary>
        /// Resume paused playlist.
        /// </summary>
        void ResumePlaylist();

        /// <summary>
        /// Skip to next track in playlist.
        /// </summary>
        void SkipTrack();

        /// <summary>
        /// Get current playlist handle.
        /// </summary>
        PlaylistHandle CurrentPlaylist { get; }

        // === Stop ===

        /// <summary>
        /// Stop specific sound.
        /// </summary>
        void Stop(AudioHandle handle);

        /// <summary>
        /// Stop all sounds in layer.
        /// </summary>
        void StopAll(AudioLayer layer);

        /// <summary>
        /// Stop all sounds in layer with fade out.
        /// </summary>
        void StopAllWithFade(AudioLayer layer, float duration);

        /// <summary>
        /// Stop all sounds.
        /// </summary>
        void StopAll();

        // === Volume ===

        /// <summary>
        /// Set volume for layer (0-1).
        /// </summary>
        void SetVolume(AudioLayer layer, float volume);

        /// <summary>
        /// Get volume for layer.
        /// </summary>
        float GetVolume(AudioLayer layer);

        // === Mute ===

        /// <summary>
        /// Set mute state for layer.
        /// </summary>
        void SetMuted(AudioLayer layer, bool muted);

        /// <summary>
        /// Get mute state for layer.
        /// </summary>
        bool IsMuted(AudioLayer layer);

        // === Ducking ===

        /// <summary>
        /// Enable or disable ducking.
        /// </summary>
        void SetDuckingEnabled(bool enabled);

        /// <summary>
        /// Set ducking amount (0.3 = Music at 30% when Dialogue plays).
        /// </summary>
        void SetDuckingAmount(float amount);

        // === Preload ===

        /// <summary>
        /// Preload clips by category.
        /// </summary>
        UniTask PreloadCategoryAsync(AudioCategory category, IProgress<float> progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Preload specific clips.
        /// </summary>
        UniTask PreloadAsync(int[] audioKeys, IProgress<float> progress = null, CancellationToken cancellationToken = default);

        // === Focus ===

        /// <summary>
        /// Handle application focus change (for WebGL).
        /// </summary>
        void OnApplicationFocus(bool hasFocus);

        // === Events ===

        /// <summary>
        /// Fired when a sound starts playing.
        /// </summary>
        event Action<AudioHandle> OnSoundStarted;

        /// <summary>
        /// Fired when a sound stops playing.
        /// </summary>
        event Action<AudioHandle> OnSoundStopped;

        /// <summary>
        /// Fired when playlist track changes.
        /// </summary>
        event Action<int> OnPlaylistTrackChanged;

        /// <summary>
        /// Fired when playlist ends (non-looping).
        /// </summary>
        event Action OnPlaylistEnded;
    }
}
