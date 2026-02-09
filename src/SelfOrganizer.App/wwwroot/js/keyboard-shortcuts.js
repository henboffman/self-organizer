/**
 * Keyboard Shortcuts Interop - JavaScript functions for global keyboard shortcuts
 * Called from Blazor via JSInterop
 */

window.keyboardShortcuts = {
    _dotNetRef: null,
    _handler: null,
    _pendingNavKey: null,
    _navTimeout: null,

    /**
     * Initializes keyboard shortcuts with a .NET reference for callbacks
     * @param {object} dotNetRef - Reference to .NET object for callbacks
     */
    init: function(dotNetRef) {
        this._dotNetRef = dotNetRef;

        // Remove any existing handler
        if (this._handler) {
            document.removeEventListener('keydown', this._handler);
        }

        this._handler = (e) => {
            // Skip if user is typing in an input field
            const activeElement = document.activeElement;
            const isTyping = activeElement && (
                activeElement.tagName === 'INPUT' ||
                activeElement.tagName === 'TEXTAREA' ||
                activeElement.isContentEditable
            );

            // Cmd+Shift+Space for global search (works even when typing)
            if ((e.metaKey || e.ctrlKey) && e.shiftKey && e.code === 'Space') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnGlobalSearchShortcut');
                }
                return;
            }

            // Cmd+K for global search (works even when typing)
            if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnGlobalSearchShortcut');
                }
                return;
            }

            // Cmd+Enter for quick capture (works even when typing, but not inside modals)
            if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') {
                // If a modal is open, let the event propagate to the modal's own handler
                if (document.querySelector('.modal.show')) {
                    return;
                }
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnQuickCaptureShortcut');
                }
                return;
            }

            // Cmd+N for Quick Add (works even when typing)
            if ((e.metaKey || e.ctrlKey) && e.key === 'n') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnQuickAddShortcut');
                }
                return;
            }

            // Cmd+Z for undo (works even when typing)
            if ((e.metaKey || e.ctrlKey) && !e.shiftKey && e.key === 'z') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnUndoShortcut');
                }
                return;
            }

            // Cmd+Shift+Z for redo (works even when typing)
            if ((e.metaKey || e.ctrlKey) && e.shiftKey && e.key === 'z') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnRedoShortcut');
                }
                return;
            }

            // Skip non-modifier shortcuts if user is typing
            if (isTyping) return;

            // ? for help overlay
            if (e.key === '?' || (e.shiftKey && e.key === '/')) {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnHelpShortcut');
                }
                return;
            }

            // Escape to close modals
            if (e.key === 'Escape') {
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnEscapeShortcut');
                }
                return;
            }

            // '/' for command palette / quick search (like VS Code)
            if (e.key === '/' && !e.shiftKey && !e.metaKey && !e.ctrlKey) {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnGlobalSearchShortcut');
                }
                return;
            }

            // Navigation shortcuts: g + [key]
            if (e.key === 'g' && !this._pendingNavKey) {
                this._pendingNavKey = 'g';
                // Clear after 1 second if no second key
                this._navTimeout = setTimeout(() => {
                    this._pendingNavKey = null;
                }, 1000);
                return;
            }

            // Handle second key in navigation sequence
            if (this._pendingNavKey === 'g') {
                clearTimeout(this._navTimeout);
                this._pendingNavKey = null;

                const navRoutes = {
                    'h': '',            // Home
                    'i': 'inbox',       // Inbox
                    't': 'tasks',       // Tasks
                    'p': 'projects',    // Projects
                    'c': 'calendar',    // Calendar
                    'g': 'goals',       // Goals
                    'd': 'ideas',       // Ideas
                    's': 'settings',    // Settings
                    'f': 'focus',       // Focus timer
                    'r': 'review/daily' // Review
                };

                const route = navRoutes[e.key];
                if (route && this._dotNetRef) {
                    e.preventDefault();
                    this._dotNetRef.invokeMethodAsync('OnNavigateShortcut', route);
                }
                return;
            }

            // 'n' for new task (on task-related pages)
            if (e.key === 'n') {
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnNewItemShortcut');
                }
                return;
            }

            // j/k for list navigation (vim-style)
            if (e.key === 'j') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnListNavigateShortcut', 'down');
                }
                return;
            }

            if (e.key === 'k') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnListNavigateShortcut', 'up');
                }
                return;
            }

            // Enter to open selected item
            if (e.key === 'Enter') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnListSelectShortcut');
                }
                return;
            }

            // Space to toggle complete on selected item
            if (e.key === ' ') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnListToggleShortcut');
                }
                return;
            }

            // 'e' to edit selected item
            if (e.key === 'e') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnListEditShortcut');
                }
                return;
            }

            // 'd' to delete selected item (with confirmation)
            if (e.key === 'd') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnListDeleteShortcut');
                }
                return;
            }
        };

        document.addEventListener('keydown', this._handler);
    },

    /**
     * Cleans up keyboard shortcut listeners
     */
    dispose: function() {
        if (this._handler) {
            document.removeEventListener('keydown', this._handler);
            this._handler = null;
        }
        if (this._navTimeout) {
            clearTimeout(this._navTimeout);
        }
        this._dotNetRef = null;
        this._pendingNavKey = null;
    }
};
