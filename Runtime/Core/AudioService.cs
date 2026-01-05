using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Main audio service implementation.
    /// Manages playback, pooling, and volume control.
    /// </summary>
    public sealed class AudioService : IAudioService
    {
        private readonly AudioConfig _config;
        private readonly AudioClipDatabase _database;
        private readonly ISoftwareMixer _mixer;
        private readonly IAudioSourcePool _pool;
        private readonly IAudioClipProvider _directProvider;
        private readonly IAudioClipProvider _addressableProvider;

        private readonly Dictionary<int, AudioChannel> _activeChannels = new();
        private readonly List<int> _channelsToRemove = new();
        private readonly Dictionary<int, ShuffleState> _groupShuffleStates = new();

        private AudioChannel _currentMusicChannel;
        private PlaylistController _currentPlaylist;
        private bool _isReady = true; // Will be set to false for WebGL until user interaction
        private bool _isPaused;

        public bool IsReady => _isReady;

        public PlaylistHandle CurrentPlaylist => _currentPlaylist != null
            ? new PlaylistHandle(_currentPlaylist.Id, 0, _currentPlaylist)
            : PlaylistHandle.Invalid;

        public event Action<AudioHandle> OnSoundStarted;
        public event Action<AudioHandle> OnSoundStopped;
        public event Action<int> OnPlaylistTrackChanged;
        public event Action OnPlaylistEnded;

        public AudioService(
            AudioConfig config,
            ISoftwareMixer mixer,
            IAudioSourcePool pool,
            IAudioClipProvider directProvider,
            IAudioClipProvider addressableProvider = null)
        {
            _config = config;
            _database = config.Database;
            _mixer = mixer;
            _pool = pool;
            _directProvider = directProvider;
            _addressableProvider = addressableProvider;

            // Initialize database
            _database.Initialize();

            // Configure mixer
            _mixer.SetDuckingEnabled(config.DuckingEnabled);
            _mixer.SetDuckingAmount(config.DuckingAmount);

            // Subscribe to mixer events for volume updates
            _mixer.OnVolumeChanged += OnMixerVolumeChanged;
            _mixer.OnMuteChanged += OnMixerMuteChanged;

#if UNITY_WEBGL && !UNITY_EDITOR
            _isReady = false;
#endif
        }

        // === Play ===

        public AudioHandle Play(int audioKey)
        {
            return Play(audioKey, AudioPlaySettings.Default);
        }

        public AudioHandle Play(int audioKey, AudioPlaySettings settings)
        {
            if (!_isReady)
            {
                Debug.LogWarning("[Audio] Service not ready. Waiting for user interaction on WebGL.");
                return AudioHandle.Invalid;
            }

            var clip = GetClipSync(audioKey);
            if (clip == null)
            {
                Debug.LogWarning($"[Audio] Clip not found or not loaded for key: {audioKey}");
                return AudioHandle.Invalid;
            }

            return PlayClip(audioKey, clip, settings);
        }

        public async UniTask<AudioHandle> PlayAsync(int audioKey, CancellationToken cancellationToken = default)
        {
            return await PlayAsync(audioKey, AudioPlaySettings.Default, cancellationToken);
        }

        public async UniTask<AudioHandle> PlayAsync(int audioKey, AudioPlaySettings settings, CancellationToken cancellationToken = default)
        {
            if (!_isReady)
            {
                Debug.LogWarning("[Audio] Service not ready. Waiting for user interaction on WebGL.");
                return AudioHandle.Invalid;
            }

            var clip = await GetClipAsync(audioKey, cancellationToken);
            if (clip == null)
            {
                Debug.LogWarning($"[Audio] Failed to load clip for key: {audioKey}");
                return AudioHandle.Invalid;
            }

            return PlayClip(audioKey, clip, settings);
        }

        private AudioHandle PlayClip(int audioKey, AudioClip clip, AudioPlaySettings settings)
        {
            var entry = _database.GetEntry(audioKey);
            var layer = entry?.Layer ?? AudioLayer.SFX;
            var baseVolume = (entry?.Volume ?? 1f) * settings.Volume;
            var pitch = settings.Pitch > 0 ? settings.Pitch : entry?.GetRandomPitch() ?? 1f;
            var loop = settings.Loop || (entry?.Loop ?? false);

            // Get source from pool
            AudioSource source;
            switch (layer)
            {
                case AudioLayer.Music:
                    source = _pool.GetMusicSource();
                    break;
                case AudioLayer.Dialogue:
                    source = _pool.GetDialogueSource();
                    _mixer.OnDialogueStarted();
                    break;
                default:
                    source = _pool.GetSfxSource();
                    break;
            }

            // Configure source
            source.clip = clip;
            source.pitch = pitch;
            source.loop = loop;
            source.spatialBlend = settings.SpatialBlend;

            if (settings.Position.HasValue)
            {
                source.transform.position = settings.Position.Value;
            }

            if (settings.Parent != null)
            {
                source.transform.SetParent(settings.Parent);
                source.transform.localPosition = Vector3.zero;
            }

            // Create channel
            var channel = new AudioChannel(audioKey, layer, source, _mixer, baseVolume);
            channel.SetSettingsVolume(settings.Volume);
            _activeChannels[channel.Id] = channel;

            // Play
            if (settings.Delay > 0)
            {
                source.PlayDelayed(settings.Delay);
            }
            else
            {
                source.Play();
            }

            var handle = channel.CreateHandle(this);
            OnSoundStarted?.Invoke(handle);

            // Track completion for non-looping sounds
            if (!loop)
            {
                TrackCompletion(channel, handle).Forget();
            }

            return handle;
        }

        private async UniTaskVoid TrackCompletion(AudioChannel channel, AudioHandle handle)
        {
            await UniTask.WaitUntil(() => !channel.IsPlaying);

            if (_activeChannels.ContainsKey(channel.Id))
            {
                _activeChannels.Remove(channel.Id);

                if (channel.Layer == AudioLayer.Dialogue)
                {
                    _mixer.OnDialogueStopped();
                }

                if (channel.Layer == AudioLayer.SFX)
                {
                    _pool.ReturnSfxSource(channel.Source);
                }

                OnSoundStopped?.Invoke(handle);
            }
        }

        // === Music ===

        public void PlayMusic(int audioKey, float fadeDuration = 1f)
        {
            PlayMusicAsync(audioKey, fadeDuration).Forget();
        }

        public async UniTask PlayMusicAsync(int audioKey, float fadeDuration = 1f, CancellationToken cancellationToken = default)
        {
            // Fade out current music
            if (_currentMusicChannel != null && _currentMusicChannel.IsPlaying)
            {
                await _currentMusicChannel.FadeOutAsync(fadeDuration);
            }

            // Load and play new music
            var clip = await GetClipAsync(audioKey, cancellationToken);
            if (clip == null)
            {
                Debug.LogWarning($"[Audio] Failed to load music clip: {audioKey}");
                return;
            }

            var entry = _database.GetEntry(audioKey);
            var baseVolume = entry?.Volume ?? 1f;

            var source = _pool.GetMusicSource();
            source.clip = clip;
            source.volume = 0f;
            source.loop = true;
            source.Play();

            var channel = new AudioChannel(audioKey, AudioLayer.Music, source, _mixer, baseVolume);
            _activeChannels[channel.Id] = channel;
            _currentMusicChannel = channel;

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                channel.SetSettingsVolume(t);
                await UniTask.Yield(cancellationToken);
            }

            channel.SetSettingsVolume(1f);

            var handle = channel.CreateHandle(this);
            OnSoundStarted?.Invoke(handle);
        }

        public void StopMusic(float fadeDuration = 1f)
        {
            if (_currentMusicChannel != null && _currentMusicChannel.IsPlaying)
            {
                _currentMusicChannel.FadeOutAsync(fadeDuration).Forget();
                _currentMusicChannel = null;
            }
        }

        // === Audio Groups ===

        public AudioHandle PlayGroup(int groupKey)
        {
            return PlayGroup(groupKey, AudioPlaySettings.Default);
        }

        public AudioHandle PlayGroup(int groupKey, AudioPlaySettings settings)
        {
            if (!_database.TryGetGroup(groupKey, out var group))
            {
                Debug.LogWarning($"[Audio] Group not found: {groupKey}");
                return AudioHandle.Invalid;
            }

            int clipId = SelectClipFromGroup(group);
            if (clipId == -1)
            {
                Debug.LogWarning($"[Audio] Group has no clips: {group.Name}");
                return AudioHandle.Invalid;
            }

            return Play(clipId, settings);
        }

        private int SelectClipFromGroup(AudioGroupEntry group)
        {
            if (group.Clips.Count == 0) return -1;

            switch (group.PlaybackMode)
            {
                case PlaybackMode.Random:
                    return SelectRandomWeighted(group);

                case PlaybackMode.Shuffle:
                    return SelectShuffle(group);

                case PlaybackMode.Sequential:
                    return SelectSequential(group);

                default:
                    return group.Clips[0].ClipId;
            }
        }

        private int SelectRandomWeighted(AudioGroupEntry group)
        {
            float totalWeight = 0f;
            foreach (var clip in group.Clips)
            {
                totalWeight += clip.Weight;
            }

            float random = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var clip in group.Clips)
            {
                cumulative += clip.Weight;
                if (random <= cumulative)
                {
                    return clip.ClipId;
                }
            }

            return group.Clips[0].ClipId;
        }

        private int SelectShuffle(AudioGroupEntry group)
        {
            if (!_groupShuffleStates.TryGetValue(group.Id, out var state))
            {
                state = new ShuffleState(group.Clips.Count);
                _groupShuffleStates[group.Id] = state;
            }

            int index = state.GetNextIndex();
            if (index == -1)
            {
                state.Reset();
                index = state.GetNextIndex();
            }

            return group.Clips[index].ClipId;
        }

        private int SelectSequential(AudioGroupEntry group)
        {
            // For SFX groups, sequential doesn't track state - just return first clip
            // For playlists, PlaylistController handles sequential
            return group.Clips[0].ClipId;
        }

        // === Playlists ===

        public PlaylistHandle PlayPlaylist(int groupKey, float fadeDuration = 1f)
        {
            PlayPlaylistAsync(groupKey, fadeDuration).Forget();
            return CurrentPlaylist;
        }

        public async UniTask<PlaylistHandle> PlayPlaylistAsync(int groupKey, float fadeDuration = 1f, CancellationToken cancellationToken = default)
        {
            if (!_database.TryGetGroup(groupKey, out var group))
            {
                Debug.LogWarning($"[Audio] Group not found: {groupKey}");
                return PlaylistHandle.Invalid;
            }

            if (!group.IsMusicPlaylist)
            {
                Debug.LogWarning($"[Audio] Group is not a music playlist (requires Layer=Music and AutoPlayNext=true): {group.Name}");
                return PlaylistHandle.Invalid;
            }

            // Stop current playlist
            _currentPlaylist?.Dispose();

            // Stop current music
            if (_currentMusicChannel != null && _currentMusicChannel.IsPlaying)
            {
                await _currentMusicChannel.FadeOutAsync(fadeDuration);
            }

            // Create new controller
            _currentPlaylist = new PlaylistController(
                group,
                _database,
                _mixer,
                _pool,
                _directProvider,
                _addressableProvider);

            _currentPlaylist.OnTrackChanged += id => OnPlaylistTrackChanged?.Invoke(id);
            _currentPlaylist.OnPlaylistEnded += () => OnPlaylistEnded?.Invoke();

            await _currentPlaylist.StartAsync(fadeDuration, cancellationToken);

            return new PlaylistHandle(_currentPlaylist.Id, group.Id, _currentPlaylist);
        }

        public void StopPlaylist(float fadeDuration = 1f)
        {
            _currentPlaylist?.Stop();
            _currentPlaylist?.Dispose();
            _currentPlaylist = null;
        }

        public void PausePlaylist()
        {
            _currentPlaylist?.Pause();
        }

        public void ResumePlaylist()
        {
            _currentPlaylist?.Resume();
        }

        public void SkipTrack()
        {
            _currentPlaylist?.Skip();
        }

        // === Stop ===

        public void Stop(AudioHandle handle)
        {
            if (!handle.IsValid) return;

            if (_activeChannels.TryGetValue(handle.Id, out var channel))
            {
                channel.Stop();
                _activeChannels.Remove(handle.Id);

                if (channel.Layer == AudioLayer.Dialogue)
                {
                    _mixer.OnDialogueStopped();
                }

                if (channel.Layer == AudioLayer.SFX)
                {
                    _pool.ReturnSfxSource(channel.Source);
                }

                OnSoundStopped?.Invoke(handle);
            }
        }

        public void StopAll(AudioLayer layer)
        {
            _channelsToRemove.Clear();

            foreach (var kvp in _activeChannels)
            {
                if (kvp.Value.Layer == layer)
                {
                    kvp.Value.Stop();
                    _channelsToRemove.Add(kvp.Key);

                    if (layer == AudioLayer.SFX)
                    {
                        _pool.ReturnSfxSource(kvp.Value.Source);
                    }
                }
            }

            foreach (var id in _channelsToRemove)
            {
                _activeChannels.Remove(id);
            }

            if (layer == AudioLayer.Music)
            {
                _currentMusicChannel = null;
            }
        }

        public void StopAllWithFade(AudioLayer layer, float duration)
        {
            foreach (var kvp in _activeChannels)
            {
                if (kvp.Value.Layer == layer)
                {
                    kvp.Value.FadeOutAsync(duration).Forget();
                }
            }
        }

        public void StopAll()
        {
            _pool.StopAll();
            _activeChannels.Clear();
            _currentMusicChannel = null;
        }

        // === Volume ===

        public void SetVolume(AudioLayer layer, float volume)
        {
            _mixer.SetVolume(layer, volume);
        }

        public float GetVolume(AudioLayer layer)
        {
            return _mixer.GetVolume(layer);
        }

        // === Mute ===

        public void SetMuted(AudioLayer layer, bool muted)
        {
            _mixer.SetMuted(layer, muted);
        }

        public bool IsMuted(AudioLayer layer)
        {
            return _mixer.IsMuted(layer);
        }

        // === Ducking ===

        public void SetDuckingEnabled(bool enabled)
        {
            _mixer.SetDuckingEnabled(enabled);
        }

        public void SetDuckingAmount(float amount)
        {
            _mixer.SetDuckingAmount(amount);
        }

        // === Preload ===

        public async UniTask PreloadCategoryAsync(AudioCategory category, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            var keys = _database.GetKeysByCategory(category);
            await PreloadAsync(keys, progress, cancellationToken);
        }

        public async UniTask PreloadAsync(int[] audioKeys, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (_addressableProvider != null)
            {
                await _addressableProvider.PreloadAsync(audioKeys, progress, cancellationToken);
            }
            else
            {
                progress?.Report(1f);
            }
        }

        // === Focus ===

        public void OnApplicationFocus(bool hasFocus)
        {
            if (!_config.PauseOnFocusLost) return;

            if (!hasFocus && !_isPaused)
            {
                _pool.PauseAll();
                _isPaused = true;
            }
            else if (hasFocus && _isPaused)
            {
                _pool.ResumeAll();
                _isPaused = false;
            }
        }

        /// <summary>
        /// Mark audio system as ready (call after user interaction on WebGL).
        /// </summary>
        public void SetReady()
        {
            _isReady = true;
        }

        // === Private ===

        private AudioClip GetClipSync(int audioKey)
        {
            // Try direct provider first (for UI sounds)
            if (_directProvider != null && _directProvider.CanHandle(audioKey))
            {
                return _directProvider.GetClipSync(audioKey);
            }

            // Try addressable cache
            if (_addressableProvider != null)
            {
                return _addressableProvider.GetClipSync(audioKey);
            }

            return null;
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

        private void OnMixerVolumeChanged(AudioLayer layer, float volume)
        {
            UpdateChannelVolumes(layer);
        }

        private void OnMixerMuteChanged(AudioLayer layer, bool muted)
        {
            UpdateChannelVolumes(layer);
        }

        private void UpdateChannelVolumes(AudioLayer layer)
        {
            foreach (var channel in _activeChannels.Values)
            {
                if (channel.Layer == layer || layer == AudioLayer.Master)
                {
                    channel.UpdateVolume();
                }
            }
        }

        public void Dispose()
        {
            _mixer.OnVolumeChanged -= OnMixerVolumeChanged;
            _mixer.OnMuteChanged -= OnMixerMuteChanged;

            _currentPlaylist?.Dispose();
            _currentPlaylist = null;

            StopAll();
            _pool.Dispose();
            _directProvider?.ReleaseAll();
            _addressableProvider?.ReleaseAll();
        }
    }
}
