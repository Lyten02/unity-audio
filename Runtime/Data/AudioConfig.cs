using Sirenix.OdinInspector;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Main configuration for the Audio module.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioConfig",
        menuName = "Audio/Config"
    )]
    public sealed class AudioConfig : ScriptableObject
    {
        [Title("Database")]
        [Required]
        [SerializeField]
        private AudioClipDatabase _database;

        [Title("Pool Settings")]
        [Tooltip("Number of AudioSources for SFX")]
        [Range(4, 32)]
        [SerializeField]
        private int _sfxPoolSize = 16;

        [Tooltip("Reserved AudioSources for Music (for cross-fade)")]
        [Range(2, 4)]
        [SerializeField]
        private int _musicSources = 2;

        [Tooltip("Reserved AudioSources for Dialogue")]
        [Range(1, 4)]
        [SerializeField]
        private int _dialogueSources = 2;

        [Title("WebGL Optimization")]
        [Tooltip("Smaller pool size for WebGL due to memory constraints")]
        [Range(4, 16)]
        [SerializeField]
#pragma warning disable CS0414 // Used in conditional compilation (UNITY_WEBGL)
        private int _webglPoolSize = 8;
#pragma warning restore CS0414

        [Tooltip("Pause all audio when browser tab loses focus")]
        [SerializeField]
        private bool _pauseOnFocusLost = true;

        [Title("Ducking")]
        [Tooltip("Automatically duck Music when Dialogue plays")]
        [SerializeField]
        private bool _duckingEnabled = true;

        [Tooltip("Music volume when ducking (0.3 = 30%)")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _duckingAmount = 0.3f;

        [Title("Cross-fade")]
        [Tooltip("Default duration for music cross-fade")]
        [Range(0.5f, 5f)]
        [SerializeField]
        private float _defaultMusicFadeDuration = 1f;

#if UNITY_EDITOR
        [Title("Editor")]
        [Tooltip("Show debug logs")]
        [SerializeField]
        private bool _debugLogs = true;

        [Tooltip("Output path for generated AudioKeys.cs")]
        [FolderPath]
        [SerializeField]
        private string _generatedKeysPath = "Assets/Scripts/Generated";

        public bool DebugLogs => _debugLogs;
        public string GeneratedKeysPath => _generatedKeysPath;
#endif

        // Properties
        public AudioClipDatabase Database => _database;

        public int SfxPoolSize =>
#if UNITY_WEBGL && !UNITY_EDITOR
            _webglPoolSize;
#else
            _sfxPoolSize;
#endif

        public int MusicSources => _musicSources;
        public int DialogueSources => _dialogueSources;
        public bool PauseOnFocusLost => _pauseOnFocusLost;
        public bool DuckingEnabled => _duckingEnabled;
        public float DuckingAmount => _duckingAmount;
        public float DefaultMusicFadeDuration => _defaultMusicFadeDuration;

        /// <summary>
        /// Total number of AudioSources needed.
        /// </summary>
        public int TotalPoolSize => SfxPoolSize + MusicSources + DialogueSources;
    }
}
