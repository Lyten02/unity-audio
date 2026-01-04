namespace Audio
{
    /// <summary>
    /// Determines when audio clips are unloaded from memory.
    /// Critical for WebGL memory management.
    /// </summary>
    public enum UnloadPolicy
    {
        /// <summary>
        /// Keep clip in memory once loaded.
        /// Use for frequently played UI sounds.
        /// </summary>
        KeepLoaded = 0,

        /// <summary>
        /// Unload when scene changes.
        /// Use for level-specific sounds.
        /// </summary>
        UnloadOnSceneChange = 1,

        /// <summary>
        /// Unload immediately after playback completes.
        /// Use for dialogue clips to save memory.
        /// </summary>
        UnloadAfterPlay = 2
    }
}
