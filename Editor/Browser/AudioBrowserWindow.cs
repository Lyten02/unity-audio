using System;
using System.Collections.Generic;
using System.Linq;
using Localization;
using UnityEditor;
using UnityEngine;

namespace Audio.Editor
{
    /// <summary>
    /// Audio Browser Window for browsing, searching, and previewing audio clips.
    /// </summary>
    public class AudioBrowserWindow : EditorWindow
    {
        private AudioClipDatabase _database;
        private AudioPreviewPlayer _previewPlayer;

        // Filters
        private string _searchFilter = "";
        private AudioLayer? _layerFilter;
        private AudioCategory? _categoryFilter;

        // UI State
        private Vector2 _listScrollPosition;
        private Vector2 _detailScrollPosition;
        private int _selectedIndex = -1;
        private bool _showLocalized = true;

        // Cached data
        private List<EntryWrapper> _filteredEntries = new();
        private double _lastFilterTime;

        [MenuItem("Tools/Audio/Audio Browser", priority = 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioBrowserWindow>();
            window.titleContent = new GUIContent("Audio Browser", EditorGUIUtility.IconContent("d_AudioSource Icon").image);
            window.minSize = new Vector2(700, 400);
        }

        private void OnEnable()
        {
            _previewPlayer = new AudioPreviewPlayer();
            RefreshDatabase();
        }

        private void OnDisable()
        {
            _previewPlayer?.Dispose();
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();

            // Left panel - list
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            DrawListPanel();
            EditorGUILayout.EndVertical();

            // Separator
            EditorGUILayout.BeginVertical(GUILayout.Width(2));
            GUILayout.Box("", GUILayout.ExpandHeight(true), GUILayout.Width(2));
            EditorGUILayout.EndVertical();

            // Right panel - details
            EditorGUILayout.BeginVertical();
            DrawDetailPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // Status bar
            DrawStatusBar();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Search
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            var newSearch = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150));
            if (newSearch != _searchFilter)
            {
                _searchFilter = newSearch;
                RefreshFilter();
            }

            // Layer filter
            EditorGUILayout.LabelField("Layer:", GUILayout.Width(40));
            var layers = new[] { "All" }.Concat(Enum.GetNames(typeof(AudioLayer))).ToArray();
            int layerIndex = _layerFilter.HasValue ? (int)_layerFilter.Value + 1 : 0;
            int newLayerIndex = EditorGUILayout.Popup(layerIndex, layers, EditorStyles.toolbarPopup, GUILayout.Width(80));
            if (newLayerIndex != layerIndex)
            {
                _layerFilter = newLayerIndex == 0 ? null : (AudioLayer?)(newLayerIndex - 1);
                RefreshFilter();
            }

            // Category filter
            EditorGUILayout.LabelField("Category:", GUILayout.Width(60));
            var categories = new[] { "All" }.Concat(Enum.GetNames(typeof(AudioCategory))).ToArray();
            int catIndex = _categoryFilter.HasValue ? (int)_categoryFilter.Value + 1 : 0;
            int newCatIndex = EditorGUILayout.Popup(catIndex, categories, EditorStyles.toolbarPopup, GUILayout.Width(80));
            if (newCatIndex != catIndex)
            {
                _categoryFilter = newCatIndex == 0 ? null : (AudioCategory?)(newCatIndex - 1);
                RefreshFilter();
            }

            // Show localized toggle
            _showLocalized = GUILayout.Toggle(_showLocalized, "Localized", EditorStyles.toolbarButton, GUILayout.Width(70));

            GUILayout.FlexibleSpace();

            // Refresh button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshDatabase();
            }

            // Generate button
            if (GUILayout.Button("Generate Keys", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                AudioKeysGenerator.Generate();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawListPanel()
        {
            _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition);

            for (int i = 0; i < _filteredEntries.Count; i++)
            {
                var entry = _filteredEntries[i];
                bool isSelected = _selectedIndex == i;

                var style = isSelected ? "SelectionRect" : "Label";
                var rect = EditorGUILayout.BeginHorizontal(style, GUILayout.Height(22));

                // Icon
                var icon = entry.IsLocalized ? "d_LocalizationAsset Icon" : "d_AudioSource Icon";
                GUILayout.Label(EditorGUIUtility.IconContent(icon), GUILayout.Width(20), GUILayout.Height(20));

                // Name
                EditorGUILayout.LabelField(entry.Name, GUILayout.ExpandWidth(true));

                // Layer badge
                var layerColor = GetLayerColor(entry.Layer);
                GUI.color = layerColor;
                GUILayout.Label(entry.Layer.ToString(), EditorStyles.miniLabel, GUILayout.Width(60));
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();

                // Handle click
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    _selectedIndex = i;
                    Event.current.Use();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDetailPanel()
        {
            _detailScrollPosition = EditorGUILayout.BeginScrollView(_detailScrollPosition);

            if (_selectedIndex < 0 || _selectedIndex >= _filteredEntries.Count)
            {
                EditorGUILayout.HelpBox("Select an entry to view details", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            var entry = _filteredEntries[_selectedIndex];

            EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Name:", entry.Name);
            EditorGUILayout.LabelField("ID:", entry.Id.ToString());
            EditorGUILayout.LabelField("Layer:", entry.Layer.ToString());
            EditorGUILayout.LabelField("Category:", entry.Category.ToString());
            EditorGUILayout.LabelField("Is Localized:", entry.IsLocalized.ToString());

            EditorGUILayout.Space(10);

            // Preview
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            if (entry.DirectClip != null)
            {
                _previewPlayer.DrawPreviewControls(entry.DirectClip);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Clip:", entry.DirectClip.name);
                EditorGUILayout.LabelField("Duration:", $"{entry.DirectClip.length:F2}s");
                EditorGUILayout.LabelField("Channels:", entry.DirectClip.channels.ToString());
                EditorGUILayout.LabelField("Frequency:", $"{entry.DirectClip.frequency} Hz");
            }
            else
            {
                EditorGUILayout.HelpBox("No direct clip available for preview", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Addressable info
            if (!string.IsNullOrEmpty(entry.AddressableKey))
            {
                EditorGUILayout.LabelField("Addressables", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Key:", entry.AddressableKey);
            }

            // Localized variants
            if (entry.IsLocalized && entry.Languages != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Localized Variants", EditorStyles.boldLabel);

                foreach (var lang in entry.Languages)
                {
                    EditorGUILayout.LabelField($"  {lang}");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (_database != null)
            {
                int regularCount = _database.Entries?.Count ?? 0;
                int localizedCount = _database.LocalizedEntries?.Count ?? 0;
                EditorGUILayout.LabelField($"Total: {regularCount + localizedCount} entries ({regularCount} regular, {localizedCount} localized)");
            }
            else
            {
                EditorGUILayout.LabelField("No database loaded");
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"Showing: {_filteredEntries.Count}", GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshDatabase()
        {
            var guids = AssetDatabase.FindAssets("t:AudioClipDatabase");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _database = AssetDatabase.LoadAssetAtPath<AudioClipDatabase>(path);
            }

            RefreshFilter();
        }

        private void RefreshFilter()
        {
            _filteredEntries.Clear();
            if (_database == null) return;

            // Add regular entries
            if (_database.Entries != null)
            {
                foreach (var entry in _database.Entries)
                {
                    if (entry == null) continue;
                    if (!MatchesFilter(entry.Name, entry.Layer, entry.Category)) continue;

                    _filteredEntries.Add(new EntryWrapper
                    {
                        Name = entry.Name,
                        Id = entry.Id,
                        Layer = entry.Layer,
                        Category = entry.Category,
                        DirectClip = entry.DirectClip,
                        AddressableKey = entry.AddressableKey,
                        IsLocalized = false
                    });
                }
            }

            // Add localized entries
            if (_showLocalized && _database.LocalizedEntries != null)
            {
                foreach (var entry in _database.LocalizedEntries)
                {
                    if (entry == null) continue;
                    if (!MatchesFilter(entry.Name, entry.Layer, entry.Category)) continue;

                    AudioClip firstClip = null;
                    var languages = new List<string>();

                    foreach (var variant in entry.Variants)
                    {
                        languages.Add(variant.Language.ToCode());
                        if (firstClip == null && variant.DirectClip != null)
                        {
                            firstClip = variant.DirectClip;
                        }
                    }

                    _filteredEntries.Add(new EntryWrapper
                    {
                        Name = entry.Name,
                        Id = entry.Id,
                        Layer = entry.Layer,
                        Category = entry.Category,
                        DirectClip = firstClip,
                        IsLocalized = true,
                        Languages = languages.ToArray()
                    });
                }
            }

            // Sort by name
            _filteredEntries = _filteredEntries.OrderBy(e => e.Name).ToList();

            // Reset selection if out of bounds
            if (_selectedIndex >= _filteredEntries.Count)
            {
                _selectedIndex = _filteredEntries.Count - 1;
            }
        }

        private bool MatchesFilter(string name, AudioLayer layer, AudioCategory category)
        {
            // Search filter
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                if (!name.ToLowerInvariant().Contains(_searchFilter.ToLowerInvariant()))
                {
                    return false;
                }
            }

            // Layer filter
            if (_layerFilter.HasValue && layer != _layerFilter.Value)
            {
                return false;
            }

            // Category filter
            if (_categoryFilter.HasValue && category != _categoryFilter.Value)
            {
                return false;
            }

            return true;
        }

        private Color GetLayerColor(AudioLayer layer)
        {
            return layer switch
            {
                AudioLayer.SFX => new Color(0.3f, 0.7f, 1f),
                AudioLayer.Music => new Color(0.5f, 1f, 0.5f),
                AudioLayer.Dialogue => new Color(1f, 0.7f, 0.3f),
                _ => Color.white
            };
        }

        private class EntryWrapper
        {
            public string Name;
            public int Id;
            public AudioLayer Layer;
            public AudioCategory Category;
            public AudioClip DirectClip;
            public string AddressableKey;
            public bool IsLocalized;
            public string[] Languages;
        }
    }
}
