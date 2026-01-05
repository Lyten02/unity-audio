using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Audio.Editor
{
    /// <summary>
    /// Generates AudioKeys.cs constants from AudioClipDatabase.
    /// Uses stable hash for merge-friendly IDs.
    /// </summary>
    public static class AudioKeysGenerator
    {
        private const string DEFAULT_OUTPUT_PATH = "Assets/Scripts/Generated";
        private const string OUTPUT_FILENAME = "AudioKeys.cs";

        [MenuItem("Tools/Audio/Generate AudioKeys", priority = 2)]
        public static void Generate()
        {
            var database = FindDatabase();
            if (database == null)
            {
                Debug.LogError("[AudioKeysGenerator] AudioClipDatabase not found!");
                return;
            }

            var config = FindConfig();
            string outputPath = GetOutputPath(config);

            GenerateFromDatabase(database, outputPath);
        }

        private static string GetOutputPath(AudioConfig config)
        {
            string folder = config != null && !string.IsNullOrEmpty(config.GeneratedKeysPath)
                ? config.GeneratedKeysPath
                : DEFAULT_OUTPUT_PATH;

            return Path.Combine(folder, OUTPUT_FILENAME);
        }

        [MenuItem("Tools/Audio/Open Database", priority = 10)]
        public static void OpenDatabase()
        {
            var database = FindDatabase();
            if (database != null)
            {
                Selection.activeObject = database;
                EditorGUIUtility.PingObject(database);
            }
            else
            {
                Debug.LogWarning("[AudioKeysGenerator] No AudioClipDatabase found. Create one via Assets > Create > Audio > Clip Database");
            }
        }

        [MenuItem("Tools/Audio/Open Config", priority = 11)]
        public static void OpenConfig()
        {
            var guids = AssetDatabase.FindAssets("t:AudioConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(path);
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
            else
            {
                Debug.LogWarning("[AudioKeysGenerator] No AudioConfig found. Create one via Assets > Create > Audio > Config");
            }
        }

        public static void GenerateFromDatabase(AudioClipDatabase database, string outputPath = null)
        {
            outputPath ??= Path.Combine(DEFAULT_OUTPUT_PATH, OUTPUT_FILENAME);
            var entries = new List<(string name, int id)>();
            var usedIds = new HashSet<int>();
            bool databaseModified = false;

            // Collect regular entries
            foreach (var entry in database.Entries)
            {
                if (entry == null) continue;

                int id = entry.Id;
                if (id == 0)
                {
                    // Generate stable hash if no ID assigned
                    id = GetStableHashCode(entry.Name);
                }

                // Handle collisions
                while (usedIds.Contains(id))
                {
                    id++;
                }
                usedIds.Add(id);

                // Update entry ID if it was 0 or changed due to collision
                if (entry.Id != id)
                {
                    entry.SetId(id);
                    databaseModified = true;
                }

                entries.Add((entry.Name, id));
            }

            // Collect localized entries
            foreach (var entry in database.LocalizedEntries)
            {
                if (entry == null) continue;

                int id = entry.Id;
                if (id == 0)
                {
                    id = GetStableHashCode(entry.Name);
                }

                while (usedIds.Contains(id))
                {
                    id++;
                }
                usedIds.Add(id);

                // Update entry ID if it was 0 or changed due to collision
                if (entry.Id != id)
                {
                    entry.SetId(id);
                    databaseModified = true;
                }

                entries.Add((entry.Name, id));
            }

            // Save database if modified
            if (databaseModified)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
                database.ClearCache();
                Debug.Log("[AudioKeysGenerator] Updated IDs in AudioClipDatabase");
            }

            // Generate file content
            string content = GenerateContent(entries);

            // Ensure directory exists
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write file
            File.WriteAllText(outputPath, content);
            AssetDatabase.Refresh();

            Debug.Log($"[AudioKeysGenerator] Generated {entries.Count} keys to {outputPath}");
        }

        private static string GenerateContent(List<(string name, int id)> entries)
        {
            var sb = new StringBuilder();

            sb.AppendLine("/**");
            sb.AppendLine(" * Auto-generated. Do not modify!");
            sb.AppendLine($" * Generated by Audio/AudioKeysGenerator at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(" **/");
            sb.AppendLine();
            sb.AppendLine("namespace Audio");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Auto-generated audio keys.");
            sb.AppendLine("    /// Use with AudioService.Play(AudioKeys.CLIP_NAME)");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class AudioKeys");
            sb.AppendLine("    {");

            var usedConstNames = new HashSet<string>();

            foreach (var (name, id) in entries.OrderBy(e => e.name))
            {
                string constName = NameToConstName(name, usedConstNames);
                usedConstNames.Add(constName);

                sb.AppendLine($"        /// <summary>{EscapeXml(name)}</summary>");
                sb.AppendLine($"        public const int {constName} = {id};");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Deterministic hash using FNV-1a algorithm.
        /// Survives across Unity sessions.
        /// </summary>
        private static int GetStableHashCode(string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;

            unchecked
            {
                int hash = (int)2166136261;
                foreach (char c in str)
                {
                    hash = (hash ^ c) * 16777619;
                }
                return hash & 0x7FFFFFFF; // Ensure positive
            }
        }

        private static string NameToConstName(string name, HashSet<string> usedNames)
        {
            // Convert "ButtonClick_01" to "BUTTON_CLICK_01"
            string constName = Regex.Replace(name, "([a-z])([A-Z])", "$1_$2");
            constName = constName.ToUpperInvariant();
            constName = Regex.Replace(constName, @"[^A-Z0-9]", "_");
            constName = Regex.Replace(constName, @"_+", "_").Trim('_');

            if (string.IsNullOrEmpty(constName) || char.IsDigit(constName[0]))
            {
                constName = "_" + constName;
            }

            // Handle collisions
            string uniqueName = constName;
            int collision = 1;
            while (usedNames.Contains(uniqueName))
            {
                uniqueName = $"{constName}_{collision++}";
            }

            return uniqueName;
        }

        private static string EscapeXml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private static AudioClipDatabase FindDatabase()
        {
            var guids = AssetDatabase.FindAssets("t:AudioClipDatabase");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AudioClipDatabase>(path);
            }
            return null;
        }

        private static AudioConfig FindConfig()
        {
            var guids = AssetDatabase.FindAssets("t:AudioConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AudioConfig>(path);
            }
            return null;
        }
    }
}
