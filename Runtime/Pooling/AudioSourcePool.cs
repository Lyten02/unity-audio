using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Audio
{
    /// <summary>
    /// Pool of AudioSources for efficient playback.
    /// Reserves sources for Music (cross-fade) and Dialogue.
    /// </summary>
    public sealed class AudioSourcePool : IAudioSourcePool
    {
        private readonly Transform _poolRoot;
        private readonly int _sfxPoolSize;

        // Reserved sources
        private readonly AudioSource[] _musicSources;
        private readonly AudioSource[] _dialogueSources;

        // SFX pool
        private readonly Stack<AudioSource> _availableSfx;
        private readonly List<AudioSource> _activeSfx;

        // Music rotation for cross-fade
        private int _currentMusicIndex;

        // Pause state
        private readonly List<PausedSource> _pausedSources = new();

        public AudioSourcePool(int sfxPoolSize, int musicSources, int dialogueSources, Transform parent = null)
        {
            _sfxPoolSize = sfxPoolSize;

            // Create pool root
            var go = new GameObject("[AudioSourcePool]");
            if (parent != null)
            {
                go.transform.SetParent(parent);
            }
            else
            {
                Object.DontDestroyOnLoad(go);
            }
            _poolRoot = go.transform;

            // Create music sources (reserved for cross-fade)
            _musicSources = new AudioSource[musicSources];
            for (int i = 0; i < musicSources; i++)
            {
                var musicGo = new GameObject($"Music_{i}");
                musicGo.transform.SetParent(_poolRoot);
                _musicSources[i] = musicGo.AddComponent<AudioSource>();
                ConfigureSource(_musicSources[i], loop: true);
            }

            // Create dialogue sources
            _dialogueSources = new AudioSource[dialogueSources];
            for (int i = 0; i < dialogueSources; i++)
            {
                var dialogueGo = new GameObject($"Dialogue_{i}");
                dialogueGo.transform.SetParent(_poolRoot);
                _dialogueSources[i] = dialogueGo.AddComponent<AudioSource>();
                ConfigureSource(_dialogueSources[i]);
            }

            // Create SFX pool
            _availableSfx = new Stack<AudioSource>(sfxPoolSize);
            _activeSfx = new List<AudioSource>(sfxPoolSize);

            for (int i = 0; i < sfxPoolSize; i++)
            {
                _availableSfx.Push(CreateSfxSource(i));
            }
        }

        public AudioSource GetMusicSource()
        {
            var source = _musicSources[_currentMusicIndex];
            _currentMusicIndex = (_currentMusicIndex + 1) % _musicSources.Length;
            return source;
        }

        public AudioSource GetPreviousMusicSource()
        {
            int prevIndex = (_currentMusicIndex - 1 + _musicSources.Length) % _musicSources.Length;
            return _musicSources[prevIndex];
        }

        public AudioSource GetDialogueSource()
        {
            // Find available dialogue source
            foreach (var source in _dialogueSources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            // All busy - interrupt first one
            var first = _dialogueSources[0];
            first.Stop();
            return first;
        }

        public AudioSource GetSfxSource()
        {
            // Try get from pool
            if (_availableSfx.Count > 0)
            {
                var source = _availableSfx.Pop();
                _activeSfx.Add(source);
                return source;
            }

            // Pool exhausted - try to recycle stopped source
            for (int i = 0; i < _activeSfx.Count; i++)
            {
                var source = _activeSfx[i];
                if (!source.isPlaying)
                {
                    // Move to end of active list (LRU)
                    _activeSfx.RemoveAt(i);
                    _activeSfx.Add(source);
                    return source;
                }
            }

            // All sources playing - steal oldest
            if (_activeSfx.Count > 0)
            {
                var oldest = _activeSfx[0];
                oldest.Stop();
                _activeSfx.RemoveAt(0);
                _activeSfx.Add(oldest);
                return oldest;
            }

            // Create emergency source
            var emergency = CreateSfxSource(_sfxPoolSize + _activeSfx.Count);
            _activeSfx.Add(emergency);
            return emergency;
        }

        public void ReturnSfxSource(AudioSource source)
        {
            if (_activeSfx.Remove(source))
            {
                source.Stop();
                source.clip = null;
                _availableSfx.Push(source);
            }
        }

        public void StopAll()
        {
            foreach (var source in _musicSources)
            {
                source.Stop();
            }

            foreach (var source in _dialogueSources)
            {
                source.Stop();
            }

            foreach (var source in _activeSfx)
            {
                source.Stop();
            }
        }

        public void PauseAll()
        {
            _pausedSources.Clear();

            // Pause music
            foreach (var source in _musicSources)
            {
                if (source.isPlaying)
                {
                    _pausedSources.Add(new PausedSource { Source = source, Time = source.time });
                    source.Pause();
                }
            }

            // Pause dialogue
            foreach (var source in _dialogueSources)
            {
                if (source.isPlaying)
                {
                    _pausedSources.Add(new PausedSource { Source = source, Time = source.time });
                    source.Pause();
                }
            }

            // Pause SFX
            foreach (var source in _activeSfx)
            {
                if (source.isPlaying)
                {
                    _pausedSources.Add(new PausedSource { Source = source, Time = source.time });
                    source.Pause();
                }
            }
        }

        public void ResumeAll()
        {
            foreach (var paused in _pausedSources)
            {
                if (paused.Source != null)
                {
                    paused.Source.time = paused.Time;
                    paused.Source.UnPause();
                }
            }
            _pausedSources.Clear();
        }

        public void Dispose()
        {
            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot.gameObject);
            }
        }

        private AudioSource CreateSfxSource(int index)
        {
            var go = new GameObject($"SFX_{index}");
            go.transform.SetParent(_poolRoot);
            var source = go.AddComponent<AudioSource>();
            ConfigureSource(source);
            return source;
        }

        private void ConfigureSource(AudioSource source, bool loop = false)
        {
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D by default
        }

        private struct PausedSource
        {
            public AudioSource Source;
            public float Time;
        }
    }
}
