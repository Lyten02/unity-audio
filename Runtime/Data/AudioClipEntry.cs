using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Single audio clip entry in the database.
    /// Contains all metadata for playback and management.
    /// </summary>
    [Serializable]
    public sealed class AudioClipEntry
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

        [HorizontalGroup("Main")]
        [VerticalGroup("Main/Right")]
        [HideLabel]
        [SerializeField]
        private AudioClip _directClip;

        [FoldoutGroup("Settings")]
        [LabelText("Addressable Key")]
        [SerializeField]
        private string _addressableKey;

        [FoldoutGroup("Settings")]
        [EnumToggleButtons]
        [SerializeField]
        private AudioLayer _layer = AudioLayer.SFX;

        [FoldoutGroup("Settings")]
        [EnumToggleButtons]
        [SerializeField]
        private AudioCategory _category = AudioCategory.Gameplay;

        [FoldoutGroup("Settings")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _volume = 1f;

        [FoldoutGroup("Settings")]
        [MinMaxSlider(0.5f, 2f, true)]
        [SerializeField]
        private Vector2 _pitchRange = new(1f, 1f);

        [FoldoutGroup("Settings")]
        [SerializeField]
        private bool _loop;

        [FoldoutGroup("Settings")]
        [Range(0f, 1f)]
        [LabelText("Spatial Blend (0=2D, 1=3D)")]
        [SerializeField]
        private float _spatialBlend;

        [FoldoutGroup("Settings")]
        [EnumToggleButtons]
        [SerializeField]
        private UnloadPolicy _unloadPolicy = UnloadPolicy.UnloadOnSceneChange;

        // Properties
        public int Id => _id;
        public string Name => _name;
        public AudioClip DirectClip => _directClip;
        public string AddressableKey => _addressableKey;
        public AudioLayer Layer => _layer;
        public AudioCategory Category => _category;
        public float Volume => _volume;
        public Vector2 PitchRange => _pitchRange;
        public bool Loop => _loop;
        public float SpatialBlend => _spatialBlend;
        public UnloadPolicy UnloadPolicy => _unloadPolicy;

        /// <summary>
        /// Whether this entry uses Addressables for loading.
        /// </summary>
        public bool UsesAddressables => !string.IsNullOrEmpty(_addressableKey);

        /// <summary>
        /// Get random pitch within configured range.
        /// </summary>
        public float GetRandomPitch()
        {
            return UnityEngine.Random.Range(_pitchRange.x, _pitchRange.y);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Set ID (Editor only, for generator).
        /// </summary>
        public void SetId(int id) => _id = id;

        /// <summary>
        /// Set name (Editor only, for generator).
        /// </summary>
        public void SetName(string name) => _name = name;
#endif
    }
}
