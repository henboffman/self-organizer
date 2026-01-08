/**
 * Focus Timer Interop - JavaScript functions for focus timer management
 * Handles BroadcastChannel communication and mini window management
 */

window.focusTimerInterop = {
    dotNetRef: null,
    broadcastChannel: null,
    miniWindow: null,
    miniWindowCheckInterval: null,

    /**
     * Initialize the focus timer interop with .NET reference
     * @param {object} dotNetRef - Reference to .NET object for callbacks
     */
    initialize: function(dotNetRef) {
        this.dotNetRef = dotNetRef;

        // Create BroadcastChannel for cross-window communication
        if ('BroadcastChannel' in window) {
            this.broadcastChannel = new BroadcastChannel('focus-timer-sync');
            this.broadcastChannel.onmessage = (event) => {
                if (event.data.type === 'state-update') {
                    this.dotNetRef.invokeMethodAsync('OnStateReceived', event.data.state);
                } else if (event.data.type === 'mini-window-closed') {
                    this.dotNetRef.invokeMethodAsync('OnMiniWindowStateChanged', false);
                } else if (event.data.type === 'request-state') {
                    // Another window is requesting current state - respond if we have it
                    const storedState = localStorage.getItem('focusTimerState');
                    if (storedState) {
                        this.broadcastChannel.postMessage({
                            type: 'state-update',
                            state: JSON.parse(storedState)
                        });
                    }
                }
            };

            // Request current state from other windows
            this.broadcastChannel.postMessage({ type: 'request-state' });
        }

        // Also listen to localStorage for fallback sync
        window.addEventListener('storage', (event) => {
            if (event.key === 'focusTimerState' && event.newValue) {
                const state = JSON.parse(event.newValue);
                this.dotNetRef.invokeMethodAsync('OnStateReceived', state);
            }
        });

        // Load initial state from localStorage
        const storedState = localStorage.getItem('focusTimerState');
        if (storedState) {
            try {
                const state = JSON.parse(storedState);
                this.dotNetRef.invokeMethodAsync('OnStateReceived', state);
            } catch (e) {
                console.error('Failed to parse stored focus timer state', e);
            }
        }
    },

    /**
     * Broadcast state to other windows
     * @param {object} state - The focus timer state
     */
    broadcastState: function(state) {
        // Store in localStorage for persistence and fallback sync
        localStorage.setItem('focusTimerState', JSON.stringify(state));

        // Broadcast via BroadcastChannel
        if (this.broadcastChannel) {
            this.broadcastChannel.postMessage({
                type: 'state-update',
                state: state
            });
        }
    },

    /**
     * Open the mini timer window
     */
    openMiniWindow: function() {
        // Calculate position (top-right corner of screen)
        const width = 320;
        const height = 220;
        const left = window.screen.availWidth - width - 20;
        const top = 20;

        const features = [
            `width=${width}`,
            `height=${height}`,
            `left=${left}`,
            `top=${top}`,
            'toolbar=no',
            'menubar=no',
            'scrollbars=no',
            'resizable=yes',
            'status=no',
            'location=no'
        ].join(',');

        // Close existing mini window if open
        if (this.miniWindow && !this.miniWindow.closed) {
            this.miniWindow.focus();
            return;
        }

        this.miniWindow = window.open('/focus-timer-mini', 'FocusTimerMini', features);

        // Check if popup was blocked
        if (!this.miniWindow) {
            alert('Please allow popups for this site to use the mini timer window.');
            return;
        }

        // Monitor mini window state
        this.miniWindowCheckInterval = setInterval(() => {
            if (this.miniWindow && this.miniWindow.closed) {
                clearInterval(this.miniWindowCheckInterval);
                this.miniWindow = null;
                if (this.dotNetRef) {
                    this.dotNetRef.invokeMethodAsync('OnMiniWindowStateChanged', false);
                }
                // Notify other windows
                if (this.broadcastChannel) {
                    this.broadcastChannel.postMessage({ type: 'mini-window-closed' });
                }
            }
        }, 500);

        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnMiniWindowStateChanged', true);
        }
    },

    /**
     * Close the mini timer window
     */
    closeMiniWindow: function() {
        if (this.miniWindow && !this.miniWindow.closed) {
            this.miniWindow.close();
        }
        this.miniWindow = null;
        if (this.miniWindowCheckInterval) {
            clearInterval(this.miniWindowCheckInterval);
        }
    },

    /**
     * Play a notification sound and show browser notification
     * @param {string} message - Notification message
     */
    playNotification: function(message) {
        // Play a simple beep sound
        try {
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);

            oscillator.frequency.value = 800;
            oscillator.type = 'sine';

            gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.5);

            oscillator.start(audioContext.currentTime);
            oscillator.stop(audioContext.currentTime + 0.5);

            // Play second beep
            setTimeout(() => {
                const osc2 = audioContext.createOscillator();
                const gain2 = audioContext.createGain();
                osc2.connect(gain2);
                gain2.connect(audioContext.destination);
                osc2.frequency.value = 1000;
                osc2.type = 'sine';
                gain2.gain.setValueAtTime(0.3, audioContext.currentTime);
                gain2.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.5);
                osc2.start(audioContext.currentTime);
                osc2.stop(audioContext.currentTime + 0.5);
            }, 200);
        } catch (e) {
            console.error('Failed to play notification sound', e);
        }

        // Show browser notification if permitted
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification('Focus Timer', {
                body: message,
                icon: '/favicon.png',
                tag: 'focus-timer'
            });
        } else if ('Notification' in window && Notification.permission !== 'denied') {
            Notification.requestPermission();
        }
    },

    /**
     * Request notification permission
     */
    requestNotificationPermission: function() {
        if ('Notification' in window && Notification.permission !== 'granted') {
            Notification.requestPermission();
        }
    },

    /**
     * Format seconds to MM:SS
     * @param {number} totalSeconds
     * @returns {string}
     */
    formatTime: function(totalSeconds) {
        const minutes = Math.floor(totalSeconds / 60);
        const seconds = totalSeconds % 60;
        return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    },

    /**
     * Cleanup resources
     */
    dispose: function() {
        if (this.broadcastChannel) {
            this.broadcastChannel.close();
            this.broadcastChannel = null;
        }
        if (this.miniWindowCheckInterval) {
            clearInterval(this.miniWindowCheckInterval);
        }
        this.dotNetRef = null;
    }
};

// Mini window specific functions (used in the popup window)
window.focusTimerMini = {
    dotNetRef: null,
    broadcastChannel: null,
    isDragging: false,
    dragOffsetX: 0,
    dragOffsetY: 0,

    /**
     * Initialize the mini window
     * @param {object} dotNetRef - Reference to .NET object for callbacks
     */
    initialize: function(dotNetRef) {
        this.dotNetRef = dotNetRef;

        // Set up BroadcastChannel for receiving state updates
        if ('BroadcastChannel' in window) {
            this.broadcastChannel = new BroadcastChannel('focus-timer-sync');
            this.broadcastChannel.onmessage = (event) => {
                if (event.data.type === 'state-update') {
                    this.dotNetRef.invokeMethodAsync('OnStateReceived', event.data.state);
                }
            };

            // Request current state from main window
            this.broadcastChannel.postMessage({ type: 'request-state' });
        }

        // Load state from localStorage
        const storedState = localStorage.getItem('focusTimerState');
        if (storedState) {
            try {
                const state = JSON.parse(storedState);
                this.dotNetRef.invokeMethodAsync('OnStateReceived', state);
            } catch (e) {
                console.error('Failed to parse stored focus timer state', e);
            }
        }

        // Set up window close notification
        window.addEventListener('beforeunload', () => {
            if (this.broadcastChannel) {
                this.broadcastChannel.postMessage({ type: 'mini-window-closed' });
            }
        });

        // Try to request always-on-top (won't work in most browsers for security)
        // The window.focus() can help keep it visible
        window.focus();
    },

    /**
     * Broadcast state change back to main window
     * @param {object} state - The focus timer state
     */
    broadcastState: function(state) {
        localStorage.setItem('focusTimerState', JSON.stringify(state));

        if (this.broadcastChannel) {
            this.broadcastChannel.postMessage({
                type: 'state-update',
                state: state
            });
        }
    },

    /**
     * Close this mini window
     */
    closeWindow: function() {
        if (this.broadcastChannel) {
            this.broadcastChannel.postMessage({ type: 'mini-window-closed' });
        }
        window.close();
    },

    /**
     * Cleanup resources
     */
    dispose: function() {
        if (this.broadcastChannel) {
            this.broadcastChannel.close();
        }
        this.dotNetRef = null;
    }
};
