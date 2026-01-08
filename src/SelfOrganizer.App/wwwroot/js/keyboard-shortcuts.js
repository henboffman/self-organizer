/**
 * Keyboard Shortcuts Interop - JavaScript functions for global keyboard shortcuts
 * Called from Blazor via JSInterop
 */

window.keyboardShortcuts = {
    _dotNetRef: null,
    _handler: null,

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
            // Cmd+Shift+Space for global search
            if ((e.metaKey || e.ctrlKey) && e.shiftKey && e.code === 'Space') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnGlobalSearchShortcut');
                }
            }
            // Cmd+Z for undo
            if ((e.metaKey || e.ctrlKey) && !e.shiftKey && e.key === 'z') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnUndoShortcut');
                }
            }
            // Cmd+Shift+Z for redo
            if ((e.metaKey || e.ctrlKey) && e.shiftKey && e.key === 'z') {
                e.preventDefault();
                if (this._dotNetRef) {
                    this._dotNetRef.invokeMethodAsync('OnRedoShortcut');
                }
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
        this._dotNetRef = null;
    }
};
