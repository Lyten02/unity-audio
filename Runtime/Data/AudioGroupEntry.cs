using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace Audio
{
    /// <summary>
    /// Audio group entry containing multiple clips for random/shuffle/sequential playback.
    /// </summary>
    [Serializable]
    public sealed class AudioGroupEntry
    {
        [HorizontalGroup("Main", Width = 60)]
        [VerticalGroup("Main/Left")]
        [LabelWidth(20)]
        [SerializeField]
        private int _id;

        [VerticalGroup("Main/Left")]
        [LabelWidth(50)]
        [SerializeField]
        private string _name;

        [FoldoutGroup("Settings")]
        [EnumToggleButtons]
        [SerializeField]
        private AudioLayer _layer = AudioLayer.SFX;

        [FoldoutGroup("Settings")]
        [EnumToggleButtons]
        [SerializeField]
        private AudioCategory _category = AudioCategory.Gameplay;

        [FoldoutGroup("Settings")]
        [EnumToggleButtons]
        [LabelText("Playback Mode")]
        [SerializeField]
        private PlaybackMode _playbackMode = PlaybackMode.Random;

        [FoldoutGroup("Settings")]
        [Tooltip("For Music groups: auto-play next track when current ends")]
        [SerializeField]
        private bool _autoPlayNext;

        [FoldoutGroup("Settings")]
        [Tooltip("For Music groups: loop entire playlist")]
        [ShowIf("_autoPlayNext")]
        [SerializeField]
        private bool _loopPlaylist = true;

        [FoldoutGroup("Settings")]
        [Tooltip("Crossfade duration between tracks (Music only)")]
        [Range(0f, 5f)]
        [ShowIf("_autoPlayNext")]
        [SerializeField]
        private float _crossfadeDuration = 1f;

        [Title("Clips")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField]
        private List<AudioGroupClipRef> _clips = new();

        // Properties
        public int Id => _id;
        public string Name => _name;
        public AudioLayer Layer => _layer;
        public AudioCategory Category => _category;
        public PlaybackMode PlaybackMode => _playbackMode;
        public bool AutoPlayNext => _autoPlayNext;
        public bool LoopPlaylist => _loopPlaylist;
        public float CrossfadeDuration => _crossfadeDuration;
        public IReadOnlyList<AudioGroupClipRef> Clips => _clips;

        /// <summary>
        /// Whether this is a music playlist (Music layer with AutoPlayNext).
        /// </summary>
        public bool IsMusicPlaylist => _layer == AudioLayer.Music && _autoPlayNext;

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

    /// <summary>
    /// Reference to a clip within a group.
    /// References existing AudioClipEntry by ID.
    /// </summary>
    [Serializable]
    public sealed class AudioGroupClipRef
    {
        [HorizontalGroup("Ref")]
        [LabelText("Clip")]
        [ValueDropdown("@Audio.AudioGroupClipRef.GetAvailableClips()")]
        [SerializeField]
        private int _clipId;

        [HorizontalGroup("Ref")]
        [LabelText("Weight")]
        [Range(0.1f, 10f)]
        [SerializeField]
        private float _weight = 1f;

        public int ClipId => _clipId;
        public float Weight => _weight;

#if UNITY_EDITOR
        private static IEnumerable<ValueDropdownItem<int>> GetAvailableClips()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClipDatabase");
            if (guids.Length == 0) yield break;

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            var database = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClipDatabase>(path);
            if (database == null) yield break;

            foreach (var entry in database.Entries)
            {
                if (entry != null)
                {
                    yield return new ValueDropdownItem<int>(entry.Name, entry.Id);
                }
            }

            foreach (var entry in database.LocalizedEntries)
            {
                if (entry != null)
                {
                    yield return new ValueDropdownItem<int>($"[L] {entry.Name}", entry.Id);
                }
            }
        }
#endif
    }
}
