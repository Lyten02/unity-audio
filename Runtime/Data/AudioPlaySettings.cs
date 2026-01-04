using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Settings for audio playback.
    /// Passed to Play() methods to customize playback behavior.
    /// </summary>
    public struct AudioPlaySettings
    {
        /// <summary>
        /// Volume multiplier (0-1). Applied on top of clip and layer volumes.
        /// </summary>
        public float Volume;

        /// <summary>
        /// Pitch multiplier (0.5-2). Default is 1.
        /// </summary>
        public float Pitch;

        /// <summary>
        /// Delay in seconds before playback starts.
        /// </summary>
        public float Delay;

        /// <summary>
        /// Whether to loop the audio.
        /// </summary>
        public bool Loop;

        /// <summary>
        /// Spatial blend (0 = 2D, 1 = 3D).
        /// </summary>
        public float SpatialBlend;

        /// <summary>
        /// World position for 3D audio. Null for 2D.
        /// </summary>
        public Vector3? Position;

        /// <summary>
        /// Transform to follow for 3D audio.
        /// </summary>
        public Transform Parent;

        /// <summary>
        /// If true, clip will be loaded synchronously if not cached.
        /// Use for critical sounds that must play immediately.
        /// </summary>
        public bool PreloadIfNeeded;

        /// <summary>
        /// Default settings for 2D playback.
        /// </summary>
        public static AudioPlaySettings Default => new()
        {
            Volume = 1f,
            Pitch = 1f,
            Delay = 0f,
            Loop = false,
            SpatialBlend = 0f,
            Position = null,
            Parent = null,
            PreloadIfNeeded = false
        };

        /// <summary>
        /// Creates settings for 3D positional audio.
        /// </summary>
        public static AudioPlaySettings At(Vector3 position) => new()
        {
            Volume = 1f,
            Pitch = 1f,
            Delay = 0f,
            Loop = false,
            SpatialBlend = 1f,
            Position = position,
            Parent = null,
            PreloadIfNeeded = false
        };

        /// <summary>
        /// Creates settings for looping audio.
        /// </summary>
        public static AudioPlaySettings Looped => new()
        {
            Volume = 1f,
            Pitch = 1f,
            Delay = 0f,
            Loop = true,
            SpatialBlend = 0f,
            Position = null,
            Parent = null,
            PreloadIfNeeded = false
        };
    }
}
