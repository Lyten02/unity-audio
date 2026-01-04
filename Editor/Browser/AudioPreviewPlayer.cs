using System;
using Localization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Audio.Editor
{
    /// <summary>
    /// Editor-only audio preview player.
    /// Uses EditorApplication.update for non-blocking playback.
    /// </summary>
    public class AudioPreviewPlayer : IDisposable
    {
        private AudioSource _previewSource;
        private GameObject _previewObject;
        private float _previewVolume = 1f;
        private LanguageCode _selectedLanguage = LanguageCode.En;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying && _previewSource != null && _previewSource.isPlaying;

        public float PreviewVolume
        {
            get => _previewVolume;
            set
            {
                _previewVolume = Mathf.Clamp01(value);
                if (_previewSource != null)
                {
                    _previewSource.volume = _previewVolume;
                }
            }
        }

        public LanguageCode SelectedLanguage
        {
            get => _selectedLanguage;
            set => _selectedLanguage = value;
        }

        public AudioPreviewPlayer()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        public void Dispose()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopPreview();
            DestroyPreviewSource();
        }

        /// <summary>
        /// Play preview of an audio clip entry.
        /// </summary>
        public void PlayPreview(AudioClipEntry entry)
        {
            if (entry == null || entry.DirectClip == null)
            {
                Debug.LogWarning("[AudioPreviewPlayer] No clip to preview");
                return;
            }

            PlayClip(entry.DirectClip, entry.Volume);
        }

        /// <summary>
        /// Play preview of a localized audio clip entry.
        /// </summary>
        public void PlayPreview(LocalizedAudioClipEntry entry)
        {
            if (entry == null)
            {
                Debug.LogWarning("[AudioPreviewPlayer] No entry to preview");
                return;
            }

            if (entry.TryGetVariant(_selectedLanguage, out var variant) && variant.DirectClip != null)
            {
                PlayClip(variant.DirectClip, entry.Volume);
            }
            else
            {
                Debug.LogWarning($"[AudioPreviewPlayer] No clip for language '{_selectedLanguage.ToCode()}'");
            }
        }

        /// <summary>
        /// Play any audio clip.
        /// </summary>
        public void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            EnsurePreviewSource();

            _previewSource.clip = clip;
            _previewSource.volume = volume * _previewVolume;
            _previewSource.Play();
            _isPlaying = true;
        }

        /// <summary>
        /// Stop current preview.
        /// </summary>
        public void StopPreview()
        {
            if (_previewSource != null)
            {
                _previewSource.Stop();
                _previewSource.clip = null;
            }
            _isPlaying = false;
        }

        /// <summary>
        /// Get playback progress (0-1).
        /// </summary>
        public float GetPlaybackProgress()
        {
            if (_previewSource == null || _previewSource.clip == null)
            {
                return 0f;
            }
            return _previewSource.time / _previewSource.clip.length;
        }

        /// <summary>
        /// Get playback time as formatted string.
        /// </summary>
        public string GetPlaybackTimeString()
        {
            if (_previewSource == null || _previewSource.clip == null)
            {
                return "0:00 / 0:00";
            }

            var current = TimeSpan.FromSeconds(_previewSource.time);
            var total = TimeSpan.FromSeconds(_previewSource.clip.length);

            return $"{current:m\\:ss} / {total:m\\:ss}";
        }

        private void EnsurePreviewSource()
        {
            if (_previewSource != null) return;

            _previewObject = new GameObject("[Audio Preview]")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _previewSource = _previewObject.AddComponent<AudioSource>();
            _previewSource.playOnAwake = false;
        }

        private void DestroyPreviewSource()
        {
            if (_previewObject != null)
            {
                Object.DestroyImmediate(_previewObject);
                _previewObject = null;
                _previewSource = null;
            }
        }

        private void OnEditorUpdate()
        {
            // Auto-stop when playback completes
            if (_isPlaying && _previewSource != null && !_previewSource.isPlaying)
            {
                _isPlaying = false;
            }
        }

        /// <summary>
        /// Draw preview controls using IMGUI.
        /// Returns true if play button was clicked.
        /// </summary>
        public bool DrawPreviewControls(AudioClip clip)
        {
            bool clicked = false;

            EditorGUILayout.BeginHorizontal();

            // Play/Stop button
            bool hasClip = clip != null;
            EditorGUI.BeginDisabledGroup(!hasClip);

            if (GUILayout.Button(IsPlaying ? "Stop" : "Play", GUILayout.Width(60)))
            {
                if (IsPlaying)
                {
                    StopPreview();
                }
                else if (hasClip)
                {
                    PlayClip(clip);
                    clicked = true;
                }
            }

            EditorGUI.EndDisabledGroup();

            // Volume slider
            EditorGUILayout.LabelField("Vol:", GUILayout.Width(30));
            PreviewVolume = EditorGUILayout.Slider(PreviewVolume, 0f, 1f, GUILayout.Width(100));

            // Time display
            EditorGUILayout.LabelField(GetPlaybackTimeString(), GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();

            // Progress bar
            if (IsPlaying)
            {
                var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(4));
                EditorGUI.ProgressBar(rect, GetPlaybackProgress(), "");
            }

            return clicked;
        }
    }
}
