using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Audio.Editor
{
    /// <summary>
    /// Property drawer that shows a dropdown of available audio keys from AudioKeys class.
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioKeyAttribute))]
    public class AudioKeyDrawer : PropertyDrawer
    {
        private static List<(string name, int value)> _cachedKeys;
        private static double _lastCacheTime;
        private const double CacheLifetime = 5.0;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var attr = attribute as AudioKeyAttribute;
            var keys = GetAllKeys(attr?.AllowNone ?? true);

            if (keys == null || keys.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            // Build display names array
            var displayNames = new string[keys.Count];
            int currentIndex = 0;

            for (int i = 0; i < keys.Count; i++)
            {
                displayNames[i] = keys[i].name;
                if (keys[i].value == property.intValue)
                {
                    currentIndex = i;
                }
            }

            EditorGUI.BeginProperty(position, label, property);

            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayNames);
            if (newIndex != currentIndex || (newIndex >= 0 && newIndex < keys.Count))
            {
                property.intValue = keys[newIndex].value;
            }

            EditorGUI.EndProperty();
        }

        private static List<(string name, int value)> GetAllKeys(bool allowNone)
        {
            // Use cache if still valid
            if (_cachedKeys != null && EditorApplication.timeSinceStartup - _lastCacheTime < CacheLifetime)
            {
                return GetFilteredKeys(allowNone);
            }

            _cachedKeys = new List<(string name, int value)>();

            // Get from AudioKeys class via reflection
            var audioKeysType = GetAudioKeysType();
            if (audioKeysType != null)
            {
                var fields = audioKeysType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(int) && field.IsLiteral)
                    {
                        int value = (int)field.GetValue(null);
                        string displayName = FormatDisplayName(field.Name);
                        _cachedKeys.Add((displayName, value));
                    }
                }
            }

            _cachedKeys.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            _lastCacheTime = EditorApplication.timeSinceStartup;

            return GetFilteredKeys(allowNone);
        }

        private static List<(string name, int value)> GetFilteredKeys(bool allowNone)
        {
            var result = new List<(string name, int value)>();

            if (allowNone)
            {
                result.Add(("None", 0));
            }

            if (_cachedKeys != null)
            {
                result.AddRange(_cachedKeys);
            }

            return result;
        }

        private static string FormatDisplayName(string constName)
        {
            // Convert SNAKE_CASE to Title Case
            // Example: TURTLE_GREETING -> Turtle Greeting
            var parts = constName.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i][1..].ToLower();
                }
            }
            return string.Join(" ", parts);
        }

        private static System.Type GetAudioKeysType()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("Audio.AudioKeys");
                if (type != null) return type;
            }
            return null;
        }

        /// <summary>
        /// Force refresh the cached keys (call after regenerating AudioKeys).
        /// </summary>
        public static void InvalidateCache()
        {
            _cachedKeys = null;
        }
    }
}
