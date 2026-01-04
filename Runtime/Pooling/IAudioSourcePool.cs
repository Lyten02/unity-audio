using System;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Interface for AudioSource pooling.
    /// </summary>
    public interface IAudioSourcePool : IDisposable
    {
        /// <summary>
        /// Get AudioSource for SFX playback.
        /// </summary>
        AudioSource GetSfxSource();

        /// <summary>
        /// Get AudioSource for Music playback.
        /// Returns next source in rotation for cross-fade support.
        /// </summary>
        AudioSource GetMusicSource();

        /// <summary>
        /// Get previous Music source (for cross-fade out).
        /// </summary>
        AudioSource GetPreviousMusicSource();

        /// <summary>
        /// Get AudioSource for Dialogue playback.
        /// </summary>
        AudioSource GetDialogueSource();

        /// <summary>
        /// Return SFX source to pool.
        /// </summary>
        void ReturnSfxSource(AudioSource source);

        /// <summary>
        /// Stop all sources.
        /// </summary>
        void StopAll();

        /// <summary>
        /// Pause all sources.
        /// </summary>
        void PauseAll();

        /// <summary>
        /// Resume all paused sources.
        /// </summary>
        void ResumeAll();
    }
}
