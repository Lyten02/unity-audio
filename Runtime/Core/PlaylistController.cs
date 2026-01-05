using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Internal controller for music playlist playback.
    /// Manages auto-play, crossfade, pause/resume, and state.
    /// </summary>
    internal sealed class PlaylistController : IDisposable
    {
        private static int _nextId = 1;

        private readonly int _id;
        private readonly AudioGroupEntry _group;
        private readonly AudioClipDatabase _database;
        private readonly ISoftwareMixer _mixer;
        private readonly IAudioSourcePool _pool;
        private readonly IAudioClipProvider _directProvider;
        private readonly IAudioClipProvider _addressableProvider;

        private ShuffleState _shuffleState;
        private int _currentIndex = -1;
        private int _currentClipId;
        private AudioChannel _currentChannel;
        private CancellationTokenSource _cts;

        private bool _isPaused;
        private bool _isPlaying;
        private float _pausedTime;

        public int Id => _id;
        public bool IsPlaying => _isPlaying && !_isPaused;
        public bool IsPaused => _isPaused;
        public int CurrentIndex => _currentIndex;
        public int CurrentClipId => _currentClipId;

        public event Action<int> OnTrackChanged;
        public event Action OnPlaylistEnded;

        public PlaylistController(
            AudioGroupEntry group,
            AudioClipDatabase database,
            ISoftwareMixer mixer,
            IAudioSourcePool pool,
            IAudioClipProvider directProvider,
            IAudioClipProvider addressableProvider)
        {
            _id = _nextId++;
            _group = group;
            _database = database;
            _mixer = mixer;
            _pool = pool;
            _directProvider = directProvider;
            _addressableProvider = addressableProvider;

            if (group.PlaybackMode == PlaybackMode.Shuffle)
            {
                _shuffleState = new ShuffleState(group.Clips.Count);
            }
        }

        public async UniTask StartAsync(float fadeDuration, CancellationToken externalToken = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _isPlaying = true;

            await PlayNextTrackAsync(fadeDuration, _cts.Token);

            // Start auto-play loop if enabled
            if (_group.AutoPlayNext && _isPlaying)
            {
                TrackCompletionLoopAsync(_cts.Token).Forget();
            }
        }

        private async UniTaskVoid TrackCompletionLoopAsync(CancellationToken token)
        {
            while (_isPlaying && !token.IsCancellationRequested)
            {
                // Wait for current track to finish
                if (_currentChannel != null && _currentChannel.Source != null)
                {
                    await UniTask.WaitUntil(
                        () => !_currentChannel.IsPlaying || _isPaused || token.IsCancellationRequested,
                        cancellationToken: token);

                    // If paused, wait for resume
                    if (_isPaused)
                    {
                        await UniTask.WaitUntil(() => !_isPaused || token.IsCancellationRequested, cancellationToken: token);
                        continue;
                    }

                    if (token.IsCancellationRequested) break;

                    // Play next track
                    await PlayNextTrackAsync(_group.CrossfadeDuration, token);
                }
                else
                {
                    await UniTask.Yield(token);
                }
            }
        }

        private async UniTask PlayNextTrackAsync(float fadeDuration, CancellationToken token)
        {
            int nextClipId = GetNextClipId();
            if (nextClipId == -1)
            {
                // Playlist ended
                _isPlaying = false;
                OnPlaylistEnded?.Invoke();
                return;
            }

            _currentClipId = nextClipId;

            // Crossfade: fade out current
            if (_currentChannel != null && _currentChannel.IsPlaying)
            {
                _currentChannel.FadeOutAsync(fadeDuration).Forget();
            }

            // Load clip
            var clip = await GetClipAsync(nextClipId, token);
            if (clip == null)
            {
                Debug.LogWarning($"[Audio] Failed to load playlist track: {nextClipId}");
                return;
            }

            // Get music source and configure
            var source = _pool.GetMusicSource();
            source.clip = clip;
            source.loop = false; // We handle looping ourselves
            source.volume = 0f;
            source.Play();

            // Get entry for base volume
            var entry = _database.GetEntry(nextClipId);
            var baseVolume = entry?.Volume ?? 1f;

            // Create channel
            _currentChannel = new AudioChannel(nextClipId, AudioLayer.Music, source, _mixer, baseVolume);

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeDuration && !token.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                _currentChannel.SetSettingsVolume(t);
                await UniTask.Yield(token);
            }

            if (!token.IsCancellationRequested)
            {
                _currentChannel.SetSettingsVolume(1f);
            }

            OnTrackChanged?.Invoke(nextClipId);
        }

        private int GetNextClipId()
        {
            if (_group.Clips.Count == 0) return -1;

            switch (_group.PlaybackMode)
            {
                case PlaybackMode.Random:
                    return GetRandomClipId();

                case PlaybackMode.Shuffle:
                    return GetShuffleClipId();

                case PlaybackMode.Sequential:
                    return GetSequentialClipId();

                default:
                    return -1;
            }
        }

        private int GetRandomClipId()
        {
            // Weighted random
            float totalWeight = 0f;
            foreach (var clip in _group.Clips)
            {
                totalWeight += clip.Weight;
            }

            float random = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var clip in _group.Clips)
            {
                cumulative += clip.Weight;
                if (random <= cumulative)
                {
                    return clip.ClipId;
                }
            }

            return _group.Clips[0].ClipId;
        }

        private int GetShuffleClipId()
        {
            int index = _shuffleState.GetNextIndex();
            if (index == -1)
            {
                if (_group.LoopPlaylist)
                {
                    _shuffleState.Reset();
                    index = _shuffleState.GetNextIndex();
                }
                else
                {
                    return -1; // Playlist ended
                }
            }

            _currentIndex = index;
            return _group.Clips[index].ClipId;
        }

        private int GetSequentialClipId()
        {
            _currentIndex++;
            if (_currentIndex >= _group.Clips.Count)
            {
                if (_group.LoopPlaylist)
                {
                    _currentIndex = 0;
                }
                else
                {
                    return -1; // Playlist ended
                }
            }

            return _group.Clips[_currentIndex].ClipId;
        }

        private async UniTask<AudioClip> GetClipAsync(int audioKey, CancellationToken cancellationToken)
        {
            // Try direct provider first
            if (_directProvider != null && _directProvider.CanHandle(audioKey))
            {
                return await _directProvider.GetClipAsync(audioKey, cancellationToken);
            }

            // Try addressable provider
            if (_addressableProvider != null && _addressableProvider.CanHandle(audioKey))
            {
                return await _addressableProvider.GetClipAsync(audioKey, cancellationToken);
            }

            return null;
        }

        public void Pause()
        {
            if (!_isPlaying || _isPaused) return;

            _isPaused = true;
            if (_currentChannel?.Source != null)
            {
                _pausedTime = _currentChannel.Source.time;
                _currentChannel.Source.Pause();
            }
        }

        public void Resume()
        {
            if (!_isPaused) return;

            _isPaused = false;
            if (_currentChannel?.Source != null)
            {
                _currentChannel.Source.time = _pausedTime;
                _currentChannel.Source.UnPause();
            }
        }

        public void Skip()
        {
            if (!_isPlaying) return;
            PlayNextTrackAsync(_group.CrossfadeDuration, _cts?.Token ?? default).Forget();
        }

        public void Stop()
        {
            _isPlaying = false;
            _isPaused = false;
            _cts?.Cancel();
            _currentChannel?.Stop();
            _currentChannel = null;
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
