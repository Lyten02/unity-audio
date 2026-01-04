using Atomic.Elements;

namespace Audio.Integrations.Settings
{
    /// <summary>
    /// Interface for audio volume settings.
    /// Allows Audio module to work with any settings provider without direct dependency.
    /// </summary>
    public interface IAudioVolumeSettings
    {
        /// <summary>
        /// SFX volume (0-1).
        /// </summary>
        IReactiveVariable<float> SfxVolume { get; }

        /// <summary>
        /// Music volume (0-1).
        /// </summary>
        IReactiveVariable<float> MusicVolume { get; }

        /// <summary>
        /// Dialogue volume (0-1).
        /// </summary>
        IReactiveVariable<float> DialogueVolume { get; }
    }
}
