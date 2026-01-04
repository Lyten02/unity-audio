namespace Audio
{
    /// <summary>
    /// State of a single mixer layer.
    /// </summary>
    public sealed class MixerLayerState
    {
        /// <summary>
        /// Volume level (0-1).
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// Whether the layer is muted.
        /// </summary>
        public bool IsMuted { get; set; }

        public MixerLayerState(float volume, bool isMuted)
        {
            Volume = volume;
            IsMuted = isMuted;
        }
    }
}
