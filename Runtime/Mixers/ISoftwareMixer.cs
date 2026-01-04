using System;

namespace Audio
{
    /// <summary>
    /// Interface for software-based audio mixing.
    /// Manages volume and mute state for all audio layers.
    /// </summary>
    public interface ISoftwareMixer
    {
        /// <summary>
        /// Set volume for a layer (0-1).
        /// </summary>
        void SetVolume(AudioLayer layer, float volume);

        /// <summary>
        /// Get volume for a layer.
        /// </summary>
        float GetVolume(AudioLayer layer);

        /// <summary>
        /// Set mute state for a layer.
        /// </summary>
        void SetMuted(AudioLayer layer, bool muted);

        /// <summary>
        /// Get mute state for a layer.
        /// </summary>
        bool IsMuted(AudioLayer layer);

        /// <summary>
        /// Get effective volume for a layer (includes master and ducking).
        /// </summary>
        float GetEffectiveVolume(AudioLayer layer);

        /// <summary>
        /// Called when a dialogue starts playing. Triggers ducking.
        /// </summary>
        void OnDialogueStarted();

        /// <summary>
        /// Called when a dialogue stops playing.
        /// </summary>
        void OnDialogueStopped();

        /// <summary>
        /// Enable or disable ducking.
        /// </summary>
        void SetDuckingEnabled(bool enabled);

        /// <summary>
        /// Whether ducking is enabled.
        /// </summary>
        bool IsDuckingEnabled { get; }

        /// <summary>
        /// Set ducking amount (0.3 = Music at 30% when Dialogue plays).
        /// </summary>
        void SetDuckingAmount(float amount);

        /// <summary>
        /// Fired when volume changes.
        /// </summary>
        event Action<AudioLayer, float> OnVolumeChanged;

        /// <summary>
        /// Fired when mute state changes.
        /// </summary>
        event Action<AudioLayer, bool> OnMuteChanged;
    }
}
