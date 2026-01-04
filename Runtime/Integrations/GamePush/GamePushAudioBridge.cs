using System;
using UnityEngine;

#if GAME_PUSH_ENABLED
using GamePush;
#endif

namespace Audio.Integrations.GamePush
{
    /// <summary>
    /// Bridge between Audio module and GamePush GP_Sounds.
    /// Syncs mute state bidirectionally.
    /// </summary>
    public sealed class GamePushAudioBridge : IDisposable
    {
        private readonly ISoftwareMixer _mixer;
        private bool _initialized;
        private bool _subscribedToSdkReady;

        // Saved mute states for pause/resume
        private bool _isPaused;
        private bool _savedMasterMuted;
        private bool _savedSfxMuted;
        private bool _savedMusicMuted;

        public GamePushAudioBridge(ISoftwareMixer mixer)
        {
            _mixer = mixer;
        }

#if GAME_PUSH_ENABLED
        /// <summary>
        /// Initialize bridge and subscribe to events.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Subscribe to GamePush mute events
            GP_Sounds.OnMute += OnGlobalMute;
            GP_Sounds.OnUnmute += OnGlobalUnmute;
            GP_Sounds.OnMuteSFX += OnSfxMute;
            GP_Sounds.OnUnmuteSFX += OnSfxUnmute;
            GP_Sounds.OnMuteMusic += OnMusicMute;
            GP_Sounds.OnUnmuteMusic += OnMusicUnmute;

            // Subscribe to mixer changes to sync back
            _mixer.OnMuteChanged += OnMixerMuteChanged;

            // Subscribe to pause/resume events
            GP_Game.OnPause += OnGamePause;
            GP_Game.OnResume += OnGameResume;

            // Check if SDK is already ready
            if (GP_Init.isReady)
            {
                // SDK is ready, sync initial state now
                SyncFromGamePush();
                Debug.Log("[GamePushAudioBridge] Initialized (SDK ready)");
            }
            else
            {
                // Wait for SDK to be ready before syncing
                GP_Init.OnReady += HandleSdkReady;
                _subscribedToSdkReady = true;
                Debug.Log("[GamePushAudioBridge] Initialized (waiting for SDK)");
            }
        }

        private void HandleSdkReady()
        {
            Debug.Log("[GamePushAudioBridge] SDK ready - syncing state");
            SyncFromGamePush();
        }

        private void SyncFromGamePush()
        {
            // Check if globally muted
            if (GP_Sounds.IsMuted(SoundType.All))
            {
                _mixer.SetMuted(AudioLayer.Master, true);
            }
            else
            {
                _mixer.SetMuted(AudioLayer.SFX, GP_Sounds.IsMuted(SoundType.SFX));
                _mixer.SetMuted(AudioLayer.Music, GP_Sounds.IsMuted(SoundType.Music));
            }
        }

        private void OnMixerMuteChanged(AudioLayer layer, bool muted)
        {
            // Sync mixer mute changes back to GamePush
            SoundType? soundType = layer switch
            {
                AudioLayer.Master => SoundType.All,
                AudioLayer.SFX => SoundType.SFX,
                AudioLayer.Music => SoundType.Music,
                _ => null
            };

            if (soundType.HasValue)
            {
                if (muted)
                {
                    GP_Sounds.Mute(soundType.Value);
                }
                else
                {
                    GP_Sounds.Unmute(soundType.Value);
                }
            }
        }

        // GamePush event handlers
        private void OnGlobalMute()
        {
            _mixer.SetMuted(AudioLayer.Master, true);
        }

        private void OnGlobalUnmute()
        {
            _mixer.SetMuted(AudioLayer.Master, false);
        }

        private void OnSfxMute()
        {
            _mixer.SetMuted(AudioLayer.SFX, true);
        }

        private void OnSfxUnmute()
        {
            _mixer.SetMuted(AudioLayer.SFX, false);
        }

        private void OnMusicMute()
        {
            _mixer.SetMuted(AudioLayer.Music, true);
        }

        private void OnMusicUnmute()
        {
            _mixer.SetMuted(AudioLayer.Music, false);
        }

        // Pause/Resume handlers
        private void OnGamePause()
        {
            if (_isPaused) return;
            _isPaused = true;

            // Save current mute states before GamePush mutes everything
            _savedMasterMuted = _mixer.IsMuted(AudioLayer.Master);
            _savedSfxMuted = _mixer.IsMuted(AudioLayer.SFX);
            _savedMusicMuted = _mixer.IsMuted(AudioLayer.Music);

            Debug.Log($"[GamePushAudioBridge] Game paused - saved mute states: Master={_savedMasterMuted}, SFX={_savedSfxMuted}, Music={_savedMusicMuted}");
        }

        private void OnGameResume()
        {
            if (!_isPaused) return;
            _isPaused = false;

            // Restore mute states from before pause
            _mixer.SetMuted(AudioLayer.Master, _savedMasterMuted);
            _mixer.SetMuted(AudioLayer.SFX, _savedSfxMuted);
            _mixer.SetMuted(AudioLayer.Music, _savedMusicMuted);

            Debug.Log($"[GamePushAudioBridge] Game resumed - restored mute states: Master={_savedMasterMuted}, SFX={_savedSfxMuted}, Music={_savedMusicMuted}");
        }

        public void Dispose()
        {
            if (!_initialized) return;

            GP_Sounds.OnMute -= OnGlobalMute;
            GP_Sounds.OnUnmute -= OnGlobalUnmute;
            GP_Sounds.OnMuteSFX -= OnSfxMute;
            GP_Sounds.OnUnmuteSFX -= OnSfxUnmute;
            GP_Sounds.OnMuteMusic -= OnMusicMute;
            GP_Sounds.OnUnmuteMusic -= OnMusicUnmute;

            if (_subscribedToSdkReady)
            {
                GP_Init.OnReady -= HandleSdkReady;
                _subscribedToSdkReady = false;
            }

            GP_Game.OnPause -= OnGamePause;
            GP_Game.OnResume -= OnGameResume;

            _mixer.OnMuteChanged -= OnMixerMuteChanged;

            _initialized = false;
        }
#else
        public void Initialize()
        {
            Debug.Log("[GamePushAudioBridge] GamePush not enabled, skipping initialization");
        }

        public void Dispose() { }
#endif
    }
}
