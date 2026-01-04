using System.Runtime.CompilerServices;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Pure static class for calculating final audio volume.
    /// Formula: finalVolume = clipVolume × layerVolume × masterVolume × duckingMultiplier
    /// </summary>
    public static class VolumeCalculator
    {
        /// <summary>
        /// Calculate final volume from all factors.
        /// </summary>
        /// <param name="clipVolume">Base volume of the audio clip (0-1).</param>
        /// <param name="layerVolume">Volume of the audio layer (0-1).</param>
        /// <param name="masterVolume">Master volume (0-1).</param>
        /// <param name="isMuted">Whether the layer or master is muted.</param>
        /// <param name="duckingMultiplier">Ducking factor (1 = no ducking, 0.3 = typical ducking).</param>
        /// <returns>Final volume (0-1).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Calculate(
            float clipVolume,
            float layerVolume,
            float masterVolume,
            bool isMuted,
            float duckingMultiplier = 1f)
        {
            if (isMuted)
            {
                return 0f;
            }

            return Mathf.Clamp01(clipVolume * layerVolume * masterVolume * duckingMultiplier);
        }

        /// <summary>
        /// Calculate final volume using AudioPlaySettings.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateWithSettings(
            in AudioPlaySettings settings,
            float layerVolume,
            float masterVolume,
            bool isMuted,
            float duckingMultiplier = 1f)
        {
            return Calculate(settings.Volume, layerVolume, masterVolume, isMuted, duckingMultiplier);
        }

        /// <summary>
        /// Calculate ducking multiplier based on active dialogue count.
        /// </summary>
        /// <param name="activeDialogueCount">Number of currently playing dialogues.</param>
        /// <param name="duckingAmount">Target volume when ducking (e.g., 0.3 = 30%).</param>
        /// <returns>Ducking multiplier (1 when no ducking, duckingAmount when ducking).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetDuckingMultiplier(int activeDialogueCount, float duckingAmount)
        {
            return activeDialogueCount > 0 ? duckingAmount : 1f;
        }
    }
}
