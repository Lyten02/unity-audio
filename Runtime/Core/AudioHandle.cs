using System;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Handle for controlling a playing audio clip.
    /// Lightweight struct with weak reference to source.
    /// </summary>
    public readonly struct AudioHandle : IEquatable<AudioHandle>
    {
        /// <summary>
        /// Invalid handle constant.
        /// </summary>
        public static readonly AudioHandle Invalid = default;

        /// <summary>
        /// Unique ID for this playback instance.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// Audio key that was played.
        /// </summary>
        public readonly int AudioKey;

        /// <summary>
        /// Layer this sound is playing on.
        /// </summary>
        public readonly AudioLayer Layer;

        /// <summary>
        /// Reference to the AudioSource (may become null).
        /// </summary>
        internal readonly WeakReference<AudioSource> SourceRef;

        /// <summary>
        /// Reference to the audio service for control operations.
        /// </summary>
        internal readonly WeakReference<IAudioService> ServiceRef;

        internal AudioHandle(int id, int audioKey, AudioLayer layer, AudioSource source, IAudioService service)
        {
            Id = id;
            AudioKey = audioKey;
            Layer = layer;
            SourceRef = new WeakReference<AudioSource>(source);
            ServiceRef = new WeakReference<IAudioService>(service);
        }

        /// <summary>
        /// Whether this handle is valid (has an ID).
        /// </summary>
        public bool IsValid => Id != 0;

        /// <summary>
        /// Whether the sound is currently playing.
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                if (!IsValid) return false;
                if (SourceRef == null) return false;
                if (!SourceRef.TryGetTarget(out var source)) return false;
                return source != null && source.isPlaying;
            }
        }

        /// <summary>
        /// Current playback time in seconds.
        /// </summary>
        public float Time
        {
            get
            {
                if (SourceRef != null && SourceRef.TryGetTarget(out var source) && source != null)
                {
                    return source.time;
                }
                return 0f;
            }
        }

        /// <summary>
        /// Total clip length in seconds.
        /// </summary>
        public float Length
        {
            get
            {
                if (SourceRef != null && SourceRef.TryGetTarget(out var source) && source != null && source.clip != null)
                {
                    return source.clip.length;
                }
                return 0f;
            }
        }

        /// <summary>
        /// Stop playback.
        /// </summary>
        public void Stop()
        {
            if (ServiceRef != null && ServiceRef.TryGetTarget(out var service))
            {
                service.Stop(this);
            }
        }

        /// <summary>
        /// Set volume (0-1).
        /// </summary>
        public void SetVolume(float volume)
        {
            if (SourceRef != null && SourceRef.TryGetTarget(out var source) && source != null)
            {
                source.volume = Mathf.Clamp01(volume);
            }
        }

        /// <summary>
        /// Set pitch.
        /// </summary>
        public void SetPitch(float pitch)
        {
            if (SourceRef != null && SourceRef.TryGetTarget(out var source) && source != null)
            {
                source.pitch = pitch;
            }
        }

        public bool Equals(AudioHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is AudioHandle other && Equals(other);
        public override int GetHashCode() => Id;

        public static bool operator ==(AudioHandle left, AudioHandle right) => left.Equals(right);
        public static bool operator !=(AudioHandle left, AudioHandle right) => !left.Equals(right);

        public override string ToString() => $"AudioHandle({Id}, Key={AudioKey}, Layer={Layer})";
    }
}
