namespace Audio
{
    /// <summary>
    /// Playback mode for audio groups.
    /// </summary>
    public enum PlaybackMode
    {
        /// <summary>
        /// Fully random selection, repeats possible.
        /// </summary>
        Random = 0,

        /// <summary>
        /// Each clip plays once before reshuffling (no-repeat until all played).
        /// </summary>
        Shuffle = 1,

        /// <summary>
        /// Sequential playback in defined order.
        /// </summary>
        Sequential = 2
    }
}
