using System.Collections.Generic;

namespace Audio
{
    /// <summary>
    /// Tracks shuffle state for a group to ensure all clips play before repeat.
    /// </summary>
    internal sealed class ShuffleState
    {
        private readonly List<int> _remainingIndices = new();
        private readonly int _totalCount;

        public ShuffleState(int clipCount)
        {
            _totalCount = clipCount;
            Reset();
        }

        /// <summary>
        /// Get next random index and mark as played.
        /// Returns -1 if all clips have been played (need to call Reset).
        /// </summary>
        public int GetNextIndex()
        {
            if (_remainingIndices.Count == 0)
            {
                return -1;
            }

            int randomIndex = UnityEngine.Random.Range(0, _remainingIndices.Count);
            int clipIndex = _remainingIndices[randomIndex];
            _remainingIndices.RemoveAt(randomIndex);
            return clipIndex;
        }

        /// <summary>
        /// Reset to start a new shuffle cycle.
        /// </summary>
        public void Reset()
        {
            _remainingIndices.Clear();
            for (int i = 0; i < _totalCount; i++)
            {
                _remainingIndices.Add(i);
            }
        }

        /// <summary>
        /// Whether all clips have been played.
        /// </summary>
        public bool IsEmpty => _remainingIndices.Count == 0;

        /// <summary>
        /// Number of remaining clips to play.
        /// </summary>
        public int RemainingCount => _remainingIndices.Count;
    }
}
