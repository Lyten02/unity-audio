using System;
using System.Collections.Generic;
using Localization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Audio clip entry with language variants.
    /// Used for dialogue and localized voice content.
    /// </summary>
    [Serializable]
    public sealed class LocalizedAudioClipEntry
    {
        [HorizontalGroup("Main", Width = 60)]
        [VerticalGroup("Main/Left")]
        [LabelWidth(20)]
        [SerializeField]
        private int _id;

        [VerticalGroup("Main/Left")]
        [LabelWidth(20)]
        [SerializeField]
        private string _name;

        [FoldoutGroup("Settings")]
        [EnumToggleButtons]
        [SerializeField]
        private AudioLayer _layer = AudioLayer.Dialogue;

        [FoldoutGroup("Settings")]
        [EnumToggleButtons]
        [SerializeField]
        private AudioCategory _category = AudioCategory.Voice;

        [FoldoutGroup("Settings")]
        [LabelText("Fallback Language")]
        [SerializeField]
        private LanguageCode _fallbackLanguage = LanguageCode.En;

        [FoldoutGroup("Settings")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _volume = 1f;

        [FoldoutGroup("Settings")]
        [EnumToggleButtons]
        [SerializeField]
        private UnloadPolicy _unloadPolicy = UnloadPolicy.UnloadAfterPlay;

        [TableList(ShowIndexLabels = true)]
        [SerializeField]
        private List<LocalizedAudioVariant> _variants = new();

        // Properties
        public int Id => _id;
        public string Name => _name;
        public AudioLayer Layer => _layer;
        public AudioCategory Category => _category;
        public LanguageCode FallbackLanguage => _fallbackLanguage;
        public float Volume => _volume;
        public UnloadPolicy UnloadPolicy => _unloadPolicy;
        public IReadOnlyList<LocalizedAudioVariant> Variants => _variants;

        /// <summary>
        /// Try to get variant for specific language.
        /// </summary>
        public bool TryGetVariant(LanguageCode language, out LocalizedAudioVariant variant)
        {
            variant = null;

            foreach (var v in _variants)
            {
                if (v.Language == language)
                {
                    variant = v;
                    return true;
                }
            }

            // Try fallback
            if (language != _fallbackLanguage)
            {
                foreach (var v in _variants)
                {
                    if (v.Language == _fallbackLanguage)
                    {
                        variant = v;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Try to get variant for specific language (string overload for compatibility).
        /// </summary>
        public bool TryGetVariant(string languageCode, out LocalizedAudioVariant variant)
        {
            return TryGetVariant(LanguageCodeExtensions.FromCode(languageCode), out variant);
        }

        /// <summary>
        /// Check if variant exists for language.
        /// </summary>
        public bool HasLanguage(LanguageCode language)
        {
            foreach (var v in _variants)
            {
                if (v.Language == language)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if variant exists for language (string overload).
        /// </summary>
        public bool HasLanguage(string languageCode)
        {
            return HasLanguage(LanguageCodeExtensions.FromCode(languageCode));
        }

        /// <summary>
        /// Get all supported language codes.
        /// </summary>
        public LanguageCode[] GetSupportedLanguages()
        {
            var result = new LanguageCode[_variants.Count];
            for (int i = 0; i < _variants.Count; i++)
            {
                result[i] = _variants[i].Language;
            }
            return result;
        }

#if UNITY_EDITOR
        public void SetId(int id) => _id = id;
        public void SetName(string name) => _name = name;
#endif
    }

    /// <summary>
    /// Language-specific variant of an audio clip.
    /// </summary>
    [Serializable]
    public sealed class LocalizedAudioVariant
    {
        [TableColumnWidth(60)]
        [SerializeField]
        private LanguageCode _language;

        [TableColumnWidth(200)]
        [SerializeField]
        private AudioClip _directClip;

        [SerializeField]
        private string _addressableKey;

        public LanguageCode Language => _language;
        public AudioClip DirectClip => _directClip;
        public string AddressableKey => _addressableKey;

        public bool UsesAddressables => !string.IsNullOrEmpty(_addressableKey);
    }
}
