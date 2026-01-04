var AudioActivatorPlugin = {
    $AudioActivatorState: {
        callback: null,
        activated: false
    },

    AudioActivator_Init: function(callbackPtr) {
        AudioActivatorState.callback = callbackPtr;
        AudioActivatorState.activated = false;

        var activateHandler = function(event) {
            if (AudioActivatorState.activated) return;
            AudioActivatorState.activated = true;

            // Remove all listeners
            document.removeEventListener('click', activateHandler, true);
            document.removeEventListener('touchstart', activateHandler, true);
            document.removeEventListener('touchend', activateHandler, true);
            document.removeEventListener('pointerdown', activateHandler, true);
            document.removeEventListener('keydown', activateHandler, true);

            console.log('[AudioActivator] User interaction detected:', event.type);

            // Resume AudioContext (critical for mobile browsers)
            try {
                var audioContext = window.AudioContext || window.webkitAudioContext;
                if (audioContext) {
                    // Find Unity's audio context and resume it
                    if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) {
                        WEBAudio.audioContext.resume().then(function() {
                            console.log('[AudioActivator] Unity AudioContext resumed');
                        }).catch(function(err) {
                            console.warn('[AudioActivator] AudioContext resume failed:', err);
                        });
                    }
                }
            } catch (e) {
                console.warn('[AudioActivator] AudioContext resume error:', e);
            }

            // Call Unity callback
            if (AudioActivatorState.callback) {
                try {
                    Module.dynCall_v(AudioActivatorState.callback);
                } catch (e) {
                    console.error('[AudioActivator] Callback error:', e);
                }
            }
        };

        // Listen for any user interaction on document with capture phase
        // Using capture phase to catch events before they're handled by other elements
        document.addEventListener('click', activateHandler, { capture: true, passive: true });
        document.addEventListener('touchstart', activateHandler, { capture: true, passive: true });
        document.addEventListener('touchend', activateHandler, { capture: true, passive: true });
        document.addEventListener('pointerdown', activateHandler, { capture: true, passive: true });
        document.addEventListener('keydown', activateHandler, { capture: true, passive: true });

        // Also listen on canvas element directly (Unity WebGL canvas)
        var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
        if (canvas) {
            canvas.addEventListener('click', activateHandler, { capture: true, passive: true });
            canvas.addEventListener('touchstart', activateHandler, { capture: true, passive: true });
            canvas.addEventListener('touchend', activateHandler, { capture: true, passive: true });
            canvas.addEventListener('pointerdown', activateHandler, { capture: true, passive: true });
        }

        console.log('[AudioActivator] Initialized, waiting for user interaction (mobile-optimized)');
    },

    AudioActivator_IsActivated: function() {
        return AudioActivatorState.activated ? 1 : 0;
    }
};

autoAddDeps(AudioActivatorPlugin, '$AudioActivatorState');
mergeInto(LibraryManager.library, AudioActivatorPlugin);
