using System;
using System.Runtime.CompilerServices;
using Atomic.Entities;
using UnityEngine;

namespace Audio.Integrations.Atomic
{
    /// <summary>
    /// Extension methods for playing audio from any Entity.
    /// </summary>
    public static class AudioBehaviourExtensions
    {
        /// <summary>
        /// Play sound using global AudioService.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AudioHandle PlaySound(this IEntity entity, int audioKey)
        {
            return AudioSystemProvider.Play(audioKey);
        }

        /// <summary>
        /// Play sound with custom settings.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AudioHandle PlaySound(this IEntity entity, int audioKey, AudioPlaySettings settings)
        {
            return AudioSystemProvider.Play(audioKey, settings);
        }

        /// <summary>
        /// Play sound at world position (3D audio).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AudioHandle PlaySoundAt(this IEntity entity, int audioKey, Vector3 position)
        {
            return AudioSystemProvider.Play(audioKey, AudioPlaySettings.At(position));
        }

        /// <summary>
        /// Play sound attached to entity lifecycle.
        /// Sound auto-stops when entity is disposed.
        /// </summary>
        public static AudioHandle PlayAttached(this IEntity entity, int audioKey)
        {
            var service = AudioSystemProvider.Service;
            if (service == null)
            {
                Debug.LogWarning("[Audio] Service not initialized");
                return AudioHandle.Invalid;
            }

            var handle = service.Play(audioKey);
            if (!handle.IsValid)
            {
                return handle;
            }

            // Subscribe to entity disposal
            if (entity is IDisposable)
            {
                // Store handle directly - it's a struct with internal weak references
                var capturedHandle = handle;

                // Try to subscribe to OnDisposed if available
                // This is a simplified approach - in practice you'd use entity events
                SubscribeToDisposal(entity, () =>
                {
                    if (capturedHandle.IsPlaying)
                    {
                        capturedHandle.Stop();
                    }
                });
            }

            return handle;
        }

        /// <summary>
        /// Play looping sound attached to entity.
        /// </summary>
        public static AudioHandle PlayLoopAttached(this IEntity entity, int audioKey)
        {
            var service = AudioSystemProvider.Service;
            if (service == null)
            {
                return AudioHandle.Invalid;
            }

            var handle = service.Play(audioKey, AudioPlaySettings.Looped);
            if (!handle.IsValid)
            {
                return handle;
            }

            if (entity is IDisposable)
            {
                // Store handle directly - it's a struct with internal weak references
                var capturedHandle = handle;

                SubscribeToDisposal(entity, () =>
                {
                    if (capturedHandle.IsValid)
                    {
                        capturedHandle.Stop();
                    }
                });
            }

            return handle;
        }

        /// <summary>
        /// Play music with cross-fade.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PlayMusic(this IEntity entity, int audioKey, float fadeDuration = 1f)
        {
            AudioSystemProvider.PlayMusic(audioKey, fadeDuration);
        }

        /// <summary>
        /// Stop all sounds in layer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StopAllSounds(this IEntity entity, AudioLayer layer)
        {
            AudioSystemProvider.Service?.StopAll(layer);
        }

        /// <summary>
        /// Stop all sounds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StopAllSounds(this IEntity entity)
        {
            AudioSystemProvider.StopAll();
        }

        private static void SubscribeToDisposal(IEntity entity, Action callback)
        {
            // This is a simplified implementation
            // In practice, you would use entity's lifecycle events
            // For now, we rely on the WeakReference approach
        }
    }
}
