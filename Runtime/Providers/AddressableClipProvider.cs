using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Audio
{
    /// <summary>
    /// Provider for Addressable AudioClips.
    /// Loads clips asynchronously with reference counting.
    /// </summary>
    public sealed class AddressableClipProvider : IAudioClipProvider
    {
        public string Name => "Addressables";

        private readonly AudioClipDatabase _database;
        private readonly Dictionary<int, LoadedClip> _loadedClips = new();
        private readonly Dictionary<int, UniTask<AudioClip>> _pendingLoads = new();

        public AddressableClipProvider(AudioClipDatabase database)
        {
            _database = database;
        }

        public bool CanHandle(int audioKey)
        {
            if (_database.TryGetEntry(audioKey, out var entry))
            {
                return entry.UsesAddressables;
            }

            if (_database.TryGetLocalizedEntry(audioKey, out var localized))
            {
                foreach (var variant in localized.Variants)
                {
                    if (variant.UsesAddressables)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async UniTask<AudioClip> GetClipAsync(int audioKey, CancellationToken cancellationToken = default)
        {
#if UNITY_ADDRESSABLES
            // Check cache
            if (_loadedClips.TryGetValue(audioKey, out var loaded))
            {
                loaded.RefCount++;
                return loaded.Clip;
            }

            // Check pending loads
            if (_pendingLoads.TryGetValue(audioKey, out var pending))
            {
                return await pending;
            }

            // Get addressable key from database
            string addressableKey = GetAddressableKey(audioKey);
            if (string.IsNullOrEmpty(addressableKey))
            {
                Debug.LogWarning($"[Audio] No addressable key for audio: {audioKey}");
                return null;
            }

            // Start load
            var loadTask = LoadClipInternalAsync(audioKey, addressableKey, cancellationToken);
            _pendingLoads[audioKey] = loadTask;

            try
            {
                return await loadTask;
            }
            finally
            {
                _pendingLoads.Remove(audioKey);
            }
#else
            Debug.LogWarning("[Audio] Addressables not enabled. Install com.unity.addressables package.");
            return null;
#endif
        }

        public AudioClip GetClipSync(int audioKey)
        {
            // Addressables are async-only
            if (_loadedClips.TryGetValue(audioKey, out var loaded))
            {
                return loaded.Clip;
            }
            return null;
        }

#if UNITY_ADDRESSABLES
        private async UniTask<AudioClip> LoadClipInternalAsync(int audioKey, string addressableKey, CancellationToken cancellationToken)
        {
            var handle = Addressables.LoadAssetAsync<AudioClip>(addressableKey);

            try
            {
                // Await with cancellation support
                var clip = await handle.WithCancellation(cancellationToken);

                if (clip != null)
                {
                    var entry = _database.GetEntry(audioKey);
                    var unloadPolicy = entry?.UnloadPolicy ?? UnloadPolicy.UnloadOnSceneChange;

                    _loadedClips[audioKey] = new LoadedClip
                    {
                        Clip = clip,
                        Handle = handle,
                        RefCount = 1,
                        UnloadPolicy = unloadPolicy
                    };
                }

                return clip;
            }
            catch (OperationCanceledException)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Audio] Failed to load addressable '{addressableKey}': {e.Message}");
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                return null;
            }
        }
#endif

        public async UniTask PreloadAsync(int[] audioKeys, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            var tasks = new List<UniTask>();
            int completed = 0;

            foreach (var key in audioKeys)
            {
                if (!_loadedClips.ContainsKey(key) && CanHandle(key))
                {
                    var task = GetClipAsync(key, cancellationToken).ContinueWith(_ =>
                    {
                        completed++;
                        progress?.Report((float)completed / audioKeys.Length);
                    });
                    tasks.Add(task);
                }
                else
                {
                    completed++;
                    progress?.Report((float)completed / audioKeys.Length);
                }
            }

            await UniTask.WhenAll(tasks);
        }

        public void Release(int audioKey)
        {
#if UNITY_ADDRESSABLES
            if (!_loadedClips.TryGetValue(audioKey, out var loaded))
            {
                return;
            }

            loaded.RefCount--;

            if (loaded.RefCount <= 0)
            {
                if (loaded.Handle.IsValid())
                {
                    Addressables.Release(loaded.Handle);
                }
                _loadedClips.Remove(audioKey);
            }
#endif
        }

        public void ReleaseAll()
        {
#if UNITY_ADDRESSABLES
            foreach (var kvp in _loadedClips)
            {
                if (kvp.Value.Handle.IsValid())
                {
                    Addressables.Release(kvp.Value.Handle);
                }
            }
            _loadedClips.Clear();
#endif
        }

        public bool IsLoaded(int audioKey)
        {
            return _loadedClips.ContainsKey(audioKey);
        }

        /// <summary>
        /// Release clips by unload policy.
        /// </summary>
        public void ReleaseByPolicy(UnloadPolicy policy)
        {
#if UNITY_ADDRESSABLES
            var toRemove = new List<int>();

            foreach (var kvp in _loadedClips)
            {
                if (kvp.Value.UnloadPolicy == policy)
                {
                    if (kvp.Value.Handle.IsValid())
                    {
                        Addressables.Release(kvp.Value.Handle);
                    }
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                _loadedClips.Remove(key);
            }
#endif
        }

        private string GetAddressableKey(int audioKey)
        {
            if (_database.TryGetEntry(audioKey, out var entry))
            {
                return entry.AddressableKey;
            }

            if (_database.TryGetLocalizedEntry(audioKey, out var localized))
            {
                // Return first available addressable key
                foreach (var variant in localized.Variants)
                {
                    if (variant.UsesAddressables)
                    {
                        return variant.AddressableKey;
                    }
                }
            }

            return null;
        }

        private class LoadedClip
        {
            public AudioClip Clip;
#if UNITY_ADDRESSABLES
            public AsyncOperationHandle<AudioClip> Handle;
#endif
            public int RefCount;
            public UnloadPolicy UnloadPolicy;
        }
    }
}
