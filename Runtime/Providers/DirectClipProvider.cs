using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Localization;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Provider for direct AudioClip references.
    /// Used for UI sounds that need instant playback.
    /// Clips are always in memory.
    /// </summary>
    public sealed class DirectClipProvider : IAudioClipProvider
    {
        public string Name => "Direct";

        private readonly AudioClipDatabase _database;
        private readonly Dictionary<int, AudioClip> _clips = new();
        private readonly Func<LanguageCode> _getCurrentLanguage;

        public DirectClipProvider(AudioClipDatabase database, Func<LanguageCode> getCurrentLanguage = null)
        {
            _database = database;
            _getCurrentLanguage = getCurrentLanguage ?? (() => LanguageCode.En);

            // Pre-cache all non-localized direct clips from database
            foreach (var entry in database.Entries)
            {
                if (entry != null && entry.DirectClip != null && !entry.UsesAddressables)
                {
                    _clips[entry.Id] = entry.DirectClip;
                }
            }

            // Note: Localized clips are NOT pre-cached here.
            // They are fetched dynamically based on current language in GetClipSync.
        }

        public bool CanHandle(int audioKey)
        {
            // Check non-localized cache
            if (_clips.ContainsKey(audioKey))
            {
                return true;
            }

            // Check if database entry has direct clip
            if (_database.TryGetEntry(audioKey, out var entry))
            {
                return entry.DirectClip != null;
            }

            // Check localized entries
            if (_database.TryGetLocalizedEntry(audioKey, out var localizedEntry))
            {
                var language = _getCurrentLanguage();
                if (localizedEntry.TryGetVariant(language, out var variant))
                {
                    return variant.DirectClip != null;
                }
            }

            return false;
        }

        public UniTask<AudioClip> GetClipAsync(int audioKey, CancellationToken cancellationToken = default)
        {
            var clip = GetClipSync(audioKey);
            return UniTask.FromResult(clip);
        }

        public AudioClip GetClipSync(int audioKey)
        {
            // First check non-localized cache
            if (_clips.TryGetValue(audioKey, out var clip))
            {
                return clip;
            }

            // Try regular (non-localized) entry
            if (_database.TryGetEntry(audioKey, out var entry) && entry.DirectClip != null)
            {
                _clips[audioKey] = entry.DirectClip;
                return entry.DirectClip;
            }

            // Try localized entry with current language
            if (_database.TryGetLocalizedEntry(audioKey, out var localizedEntry))
            {
                var language = _getCurrentLanguage();
                if (localizedEntry.TryGetVariant(language, out var variant) && variant.DirectClip != null)
                {
                    return variant.DirectClip;
                }
            }

            return null;
        }

        public UniTask PreloadAsync(int[] audioKeys, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            // Direct clips are already in memory
            progress?.Report(1f);
            return UniTask.CompletedTask;
        }

        public void Release(int audioKey)
        {
            // Direct clips stay in memory - they're referenced by ScriptableObject
        }

        public void ReleaseAll()
        {
            // Direct clips stay in memory
        }

        public bool IsLoaded(int audioKey)
        {
            return _clips.ContainsKey(audioKey) || CanHandle(audioKey);
        }
    }
}
