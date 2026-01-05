// Platform detection utilities
window.platformInterop = {
    isMac: function () {
        // Check for Mac using multiple methods for reliability
        const platform = navigator.platform?.toLowerCase() || '';
        const userAgent = navigator.userAgent?.toLowerCase() || '';

        return platform.includes('mac') ||
               userAgent.includes('macintosh') ||
               userAgent.includes('mac os');
    },

    getPlatform: function () {
        if (this.isMac()) return 'mac';
        if (navigator.platform?.toLowerCase().includes('win')) return 'windows';
        if (navigator.platform?.toLowerCase().includes('linux')) return 'linux';
        return 'unknown';
    },

    getModifierKeySymbol: function () {
        return this.isMac() ? '⌘' : 'Ctrl';
    }
};
