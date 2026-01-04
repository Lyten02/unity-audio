namespace Audio
{
    /// <summary>
    /// Audio categories for organizing and preloading clips.
    /// </summary>
    public enum AudioCategory
    {
        /// <summary>
        /// UI sounds (clicks, hovers, transitions).
        /// Typically use DirectClipProvider for instant playback.
        /// </summary>
        UI = 0,

        /// <summary>
        /// Gameplay sounds (actions, events, effects).
        /// </summary>
        Gameplay = 1,

        /// <summary>
        /// Ambient sounds and background music.
        /// </summary>
        Ambient = 2,

        /// <summary>
        /// Voice and dialogue clips.
        /// Often localized.
        /// </summary>
        Voice = 3
    }
}
