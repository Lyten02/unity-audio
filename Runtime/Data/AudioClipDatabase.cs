using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Database of all audio clips in the project.
    /// Single source of truth for audio entries.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioClipDatabase",
        menuName = "Audio/Clip Database"
    )]
    public sealed class AudioClipDatabase : ScriptableObject
    {
        [Title("Regular Clips")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "_name")]
        [SerializeField]
        private List<AudioClipEntry> _entries = new();

        [Title("Localized Clips")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "_name")]
        [SerializeField]
        private List<LocalizedAudioClipEntry> _localizedEntries = new();

        // Runtime lookup caches
        private Dictionary<int, AudioClipEntry> _entryLookup;
        private Dictionary<int, LocalizedAudioClipEntry> _localizedLookup;
        private bool _isInitialized;

        public IReadOnlyList<AudioClipEntry> Entries => _entries;
        public IReadOnlyList<LocalizedAudioClipEntry> LocalizedEntries => _localizedEntries;

        /// <summary>
        /// Initialize lookup dictionaries for fast access.
        /// Called automatically on first access.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _entryLookup = new Dictionary<int, AudioClipEntry>();
            _localizedLookup = new Dictionary<int, LocalizedAudioClipEntry>();

            foreach (var entry in _entries)
            {
                if (entry != null && !_entryLookup.ContainsKey(entry.Id))
                {
                    _entryLookup[entry.Id] = entry;
                }
            }

            foreach (var entry in _localizedEntries)
            {
                if (entry != null && !_localizedLookup.ContainsKey(entry.Id))
                {
                    _localizedLookup[entry.Id] = entry;
                }
            }

            _isInitialized = true;
        }

        private void EnsureInitialized()
        {
            // Check both flag and actual dictionary - dictionaries can become null after domain reload
            if (!_isInitialized || _entryLookup == null || _localizedLookup == null)
            {
                _isInitialized = false; // Reset to ensure full reinitialization
                Initialize();
            }
        }

        /// <summary>
        /// Try to get regular entry by ID.
        /// </summary>
        public bool TryGetEntry(int id, out AudioClipEntry entry)
        {
            EnsureInitialized();
            return _entryLookup.TryGetValue(id, out entry);
        }

        /// <summary>
        /// Try to get localized entry by ID.
        /// </summary>
        public bool TryGetLocalizedEntry(int id, out LocalizedAudioClipEntry entry)
        {
            EnsureInitialized();
            return _localizedLookup.TryGetValue(id, out entry);
        }

        /// <summary>
        /// Get entry by ID (returns null if not found).
        /// </summary>
        public AudioClipEntry GetEntry(int id)
        {
            TryGetEntry(id, out var entry);
            return entry;
        }

        /// <summary>
        /// Get localized entry by ID (returns null if not found).
        /// </summary>
        public LocalizedAudioClipEntry GetLocalizedEntry(int id)
        {
            TryGetLocalizedEntry(id, out var entry);
            return entry;
        }

        /// <summary>
        /// Check if ID exists (regular or localized).
        /// </summary>
        public bool HasEntry(int id)
        {
            EnsureInitialized();
            return _entryLookup.ContainsKey(id) || _localizedLookup.ContainsKey(id);
        }

        /// <summary>
        /// Check if ID is localized entry.
        /// </summary>
        public bool IsLocalized(int id)
        {
            EnsureInitialized();
            return _localizedLookup.ContainsKey(id);
        }

        /// <summary>
        /// Get all entries by category.
        /// </summary>
        public IEnumerable<AudioClipEntry> GetEntriesByCategory(AudioCategory category)
        {
            foreach (var entry in _entries)
            {
                if (entry != null && entry.Category == category)
                {
                    yield return entry;
                }
            }
        }

        /// <summary>
        /// Get all entries by layer.
        /// </summary>
        public IEnumerable<AudioClipEntry> GetEntriesByLayer(AudioLayer layer)
        {
            foreach (var entry in _entries)
            {
                if (entry != null && entry.Layer == layer)
                {
                    yield return entry;
                }
            }
        }

        /// <summary>
        /// Get all audio keys in category (for preloading).
        /// </summary>
        public int[] GetKeysByCategory(AudioCategory category)
        {
            var keys = new List<int>();

            foreach (var entry in _entries)
            {
                if (entry != null && entry.Category == category)
                {
                    keys.Add(entry.Id);
                }
            }

            foreach (var entry in _localizedEntries)
            {
                if (entry != null && entry.Category == category)
                {
                    keys.Add(entry.Id);
                }
            }

            return keys.ToArray();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Clear initialization state (Editor only).
        /// </summary>
        public void ClearCache()
        {
            _isInitialized = false;
            _entryLookup = null;
            _localizedLookup = null;
        }

        /// <summary>
        /// Validate database for duplicates and missing clips.
        /// </summary>
        [Button("Validate Database")]
        public void Validate()
        {
            var seenIds = new HashSet<int>();
            var errors = new List<string>();

            foreach (var entry in _entries)
            {
                if (entry == null) continue;

                if (!seenIds.Add(entry.Id))
                {
                    errors.Add($"Duplicate ID: {entry.Id} ({entry.Name})");
                }

                if (entry.DirectClip == null && string.IsNullOrEmpty(entry.AddressableKey))
                {
                    errors.Add($"No audio source: {entry.Name} (ID: {entry.Id})");
                }
            }

            foreach (var entry in _localizedEntries)
            {
                if (entry == null) continue;

                if (!seenIds.Add(entry.Id))
                {
                    errors.Add($"Duplicate ID: {entry.Id} ({entry.Name})");
                }

                if (entry.Variants.Count == 0)
                {
                    errors.Add($"No variants: {entry.Name} (ID: {entry.Id})");
                }
            }

            if (errors.Count > 0)
            {
                Debug.LogError($"[AudioClipDatabase] Validation found {errors.Count} errors:\n" + string.Join("\n", errors));
            }
            else
            {
                Debug.Log($"[AudioClipDatabase] Validation passed. {_entries.Count} regular + {_localizedEntries.Count} localized entries.");
            }
        }
#endif
    }
}
