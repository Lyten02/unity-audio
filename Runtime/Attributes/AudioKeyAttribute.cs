using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Attribute for int fields that should show a dropdown of available audio keys.
    /// Use on int fields that reference audio keys from AudioKeys class.
    /// </summary>
    public class AudioKeyAttribute : PropertyAttribute
    {
        /// <summary>
        /// If true, includes "None" option with value 0.
        /// </summary>
        public bool AllowNone { get; }

        public AudioKeyAttribute(bool allowNone = true)
        {
            AllowNone = allowNone;
        }
    }
}
