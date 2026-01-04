using System;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;

namespace Audio.WebGL
{
    /// <summary>
    /// Detects first user interaction on WebGL and activates audio.
    /// Browser autoplay policy requires user interaction before playing audio.
    /// Uses JavaScript events for reliable detection (avoids Input class issues on WebGL).
    /// </summary>
    public sealed class WebGLAudioActivator : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private static bool _activated;
        private static WebGLAudioActivator _instance;

        // Delegate type for the callback
        private delegate void ActivationCallback();

        [DllImport("__Internal")]
        private static extern void AudioActivator_Init(ActivationCallback callback);

        [DllImport("__Internal")]
        private static extern int AudioActivator_IsActivated();

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            try
            {
                AudioActivator_Init(OnUserInteraction);
                Debug.Log("[WebGLAudioActivator] Initialized with JS callback");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WebGLAudioActivator] Failed to init JS plugin: {e.Message}. Using fallback.");
                // Fallback: mark as ready immediately (audio might work after user clicks on page)
                ActivateAudio();
            }
        }

        [MonoPInvokeCallback(typeof(ActivationCallback))]
        private static void OnUserInteraction()
        {
            if (_activated) return;
            _activated = true;

            Debug.Log("[WebGLAudioActivator] User interaction detected via JS callback");
            ActivateAudioStatic();
        }

        private static void ActivateAudioStatic()
        {
            // Mark audio service as ready
            Integrations.Atomic.AudioSystemProvider.MarkReady();

            Debug.Log("[WebGLAudioActivator] Audio activated");

            // Destroy the activator object
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        private void ActivateAudio()
        {
            _activated = true;
            ActivateAudioStatic();
        }
#else
        private void Start()
        {
            // Not needed in Editor - destroy immediately
            Destroy(gameObject);
        }
#endif
    }
}
