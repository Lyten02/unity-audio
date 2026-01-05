using System;

namespace Audio
{
    /// <summary>
    /// Handle for controlling a music playlist.
    /// </summary>
    public readonly struct PlaylistHandle : IEquatable<PlaylistHandle>
    {
        /// <summary>
        /// Invalid handle constant.
        /// </summary>
        public static readonly PlaylistHandle Invalid = default;

        /// <summary>
        /// Unique ID for this playlist instance.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// Audio group ID that this playlist is based on.
        /// </summary>
        public readonly int GroupId;

        /// <summary>
        /// Reference to the playlist controller.
        /// </summary>
        internal readonly WeakReference<PlaylistController> ControllerRef;

        internal PlaylistHandle(int id, int groupId, PlaylistController controller)
        {
            Id = id;
            GroupId = groupId;
            ControllerRef = new WeakReference<PlaylistController>(controller);
        }

        /// <summary>
        /// Whether this handle is valid (has an ID).
        /// </summary>
        public bool IsValid => Id != 0;

        /// <summary>
        /// Whether the playlist is currently playing.
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                if (!IsValid) return false;
                if (ControllerRef?.TryGetTarget(out var controller) == true)
                {
                    return controller.IsPlaying;
                }
                return false;
            }
        }

        /// <summary>
        /// Whether the playlist is paused.
        /// </summary>
        public bool IsPaused
        {
            get
            {
                if (!IsValid) return false;
                if (ControllerRef?.TryGetTarget(out var controller) == true)
                {
                    return controller.IsPaused;
                }
                return false;
            }
        }

        /// <summary>
        /// Current track index in playlist.
        /// </summary>
        public int CurrentTrackIndex
        {
            get
            {
                if (ControllerRef?.TryGetTarget(out var controller) == true)
                {
                    return controller.CurrentIndex;
                }
                return -1;
            }
        }

        /// <summary>
        /// Current track audio key being played.
        /// </summary>
        public int CurrentTrackKey
        {
            get
            {
                if (ControllerRef?.TryGetTarget(out var controller) == true)
                {
                    return controller.CurrentClipId;
                }
                return 0;
            }
        }

        /// <summary>
        /// Pause the playlist.
        /// </summary>
        public void Pause()
        {
            if (ControllerRef?.TryGetTarget(out var controller) == true)
            {
                controller.Pause();
            }
        }

        /// <summary>
        /// Resume the playlist.
        /// </summary>
        public void Resume()
        {
            if (ControllerRef?.TryGetTarget(out var controller) == true)
            {
                controller.Resume();
            }
        }

        /// <summary>
        /// Skip to next track.
        /// </summary>
        public void Skip()
        {
            if (ControllerRef?.TryGetTarget(out var controller) == true)
            {
                controller.Skip();
            }
        }

        /// <summary>
        /// Stop the playlist.
        /// </summary>
        public void Stop()
        {
            if (ControllerRef?.TryGetTarget(out var controller) == true)
            {
                controller.Stop();
            }
        }

        public bool Equals(PlaylistHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is PlaylistHandle other && Equals(other);
        public override int GetHashCode() => Id;

        public static bool operator ==(PlaylistHandle left, PlaylistHandle right) => left.Equals(right);
        public static bool operator !=(PlaylistHandle left, PlaylistHandle right) => !left.Equals(right);

        public override string ToString() => $"PlaylistHandle({Id}, GroupId={GroupId})";
    }
}
