using System;
using Localization;
using UnityEngine;

namespace Audio.Integrations.Localization
{
    /// <summary>
    /// Bridge for language-aware audio playback.
    /// Integrates with LocalizationService to get current language.
    /// </summary>
    public sealed class LocalizationAudioBridge : IDisposable
    {
        private readonly AudioClipDatabase _database;
        private LanguageCode _currentLanguage = LanguageCode.En;

        /// <summary>
        /// Fired when language changes.
        /// </summary>
        public event Action<LanguageCode> OnLanguageChanged;

        /// <summary>
        /// Current language.
        /// </summary>
        public LanguageCode CurrentLanguage => _currentLanguage;

#if LOCALIZATION_MODULE
        private readonly ILocalizationService _localizationService;

        public LocalizationAudioBridge(AudioClipDatabase database, ILocalizationService localizationService)
        {
            _database = database;
            _localizationService = localizationService;
            _currentLanguage = LanguageCodeExtensions.FromCode(localizationService?.CurrentLanguage ?? "en");

            if (localizationService != null)
            {
                localizationService.OnLanguageChanged += HandleLanguageChanged;
            }
        }

        private void HandleLanguageChanged(string newLanguage)
        {
            _currentLanguage = LanguageCodeExtensions.FromCode(newLanguage);
            OnLanguageChanged?.Invoke(_currentLanguage);
        }

        public void Dispose()
        {
            if (_localizationService != null)
            {
                _localizationService.OnLanguageChanged -= HandleLanguageChanged;
            }
        }
#else
        public LocalizationAudioBridge(AudioClipDatabase database, LanguageCode defaultLanguage = LanguageCode.En)
        {
            _database = database;
            _currentLanguage = defaultLanguage;
        }

        /// <summary>
        /// Set language manually when Localization module is not available.
        /// </summary>
        public void SetLanguage(LanguageCode language)
        {
            if (_currentLanguage != language)
            {
                _currentLanguage = language;
                OnLanguageChanged?.Invoke(language);
            }
        }

        public void Dispose() { }
#endif

        /// <summary>
        /// Get localized audio clip for the given key.
        /// Falls back to default language if current language variant not found.
        /// </summary>
        public AudioClip GetLocalizedClip(int audioKey)
        {
            if (_database.TryGetLocalizedEntry(audioKey, out var entry))
            {
                if (entry.TryGetVariant(_currentLanguage, out var variant))
                {
                    return variant.DirectClip;
                }
            }

            // Fallback to regular entry
            if (_database.TryGetEntry(audioKey, out var regularEntry))
            {
                return regularEntry.DirectClip;
            }

            Debug.LogWarning($"[LocalizationAudioBridge] No clip found for key: {audioKey}");
            return null;
        }

        /// <summary>
        /// Get addressable key for localized audio.
        /// </summary>
        public string GetLocalizedAddressableKey(int audioKey)
        {
            if (_database.TryGetLocalizedEntry(audioKey, out var entry))
            {
                if (entry.TryGetVariant(_currentLanguage, out var variant))
                {
                    return variant.AddressableKey;
                }
            }

            // Fallback to regular entry
            if (_database.TryGetEntry(audioKey, out var regularEntry))
            {
                return regularEntry.AddressableKey;
            }

            return null;
        }

        /// <summary>
        /// Check if audio key has localization for specified language.
        /// </summary>
        public bool HasLocalization(int audioKey, LanguageCode language)
        {
            if (_database.TryGetLocalizedEntry(audioKey, out var entry))
            {
                return entry.HasLanguage(language);
            }
            return false;
        }

        /// <summary>
        /// Check if audio key is localized.
        /// </summary>
        public bool IsLocalized(int audioKey)
        {
            return _database.IsLocalized(audioKey);
        }

        /// <summary>
        /// Get all languages available for audio key.
        /// </summary>
        public LanguageCode[] GetAvailableLanguages(int audioKey)
        {
            if (_database.TryGetLocalizedEntry(audioKey, out var entry))
            {
                return entry.GetSupportedLanguages();
            }
            return Array.Empty<LanguageCode>();
        }
    }
}
