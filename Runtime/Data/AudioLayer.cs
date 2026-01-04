namespace Audio
{
    /// <summary>
    /// Audio mixing layers for volume control.
    /// Master affects all other layers.
    /// </summary>
    public enum AudioLayer
    {
        /// <summary>
        /// Master volume, affects all layers.
        /// </summary>
        Master = 0,

        /// <summary>
        /// Sound effects (UI clicks, gameplay sounds).
        /// Bound to SettingsConfig.AudioVolume.
        /// </summary>
        SFX = 1,

        /// <summary>
        /// Background music.
        /// Bound to SettingsConfig.MusicVolume.
        /// Subject to ducking when Dialogue plays.
        /// </summary>
        Music = 2,

        /// <summary>
        /// Voice and dialogue.
        /// Bound to SettingsConfig.DialogueVolume.
        /// Triggers ducking on Music layer.
        /// </summary>
        Dialogue = 3
    }
}
