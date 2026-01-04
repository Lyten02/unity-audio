using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Interface for audio clip loading providers.
    /// Implementations handle different loading strategies (direct, addressables).
    /// </summary>
    public interface IAudioClipProvider
    {
        /// <summary>
        /// Provider name for debugging.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Check if this provider can handle the given audio key.
        /// </summary>
        bool CanHandle(int audioKey);

        /// <summary>
        /// Load audio clip asynchronously.
        /// </summary>
        UniTask<AudioClip> GetClipAsync(int audioKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get clip synchronously if available (for UI sounds).
        /// Returns null if clip needs async loading.
        /// </summary>
        AudioClip GetClipSync(int audioKey);

        /// <summary>
        /// Preload clips into memory.
        /// </summary>
        UniTask PreloadAsync(int[] audioKeys, IProgress<float> progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Release clip from memory.
        /// </summary>
        void Release(int audioKey);

        /// <summary>
        /// Release all loaded clips.
        /// </summary>
        void ReleaseAll();

        /// <summary>
        /// Check if clip is currently loaded.
        /// </summary>
        bool IsLoaded(int audioKey);
    }
}
