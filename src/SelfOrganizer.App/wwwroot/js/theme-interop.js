/**
 * Theme Interop - JavaScript functions for theme management
 * Called from Blazor via JSInterop
 */

window.themeInterop = {
    /**
     * Gets the current theme from localStorage or system preference
     * @returns {string} 'light' or 'dark'
     */
    getTheme: function() {
        const stored = localStorage.getItem('theme');
        if (stored) {
            return stored;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    },

    /**
     * Sets the theme and stores it in localStorage
     * @param {string} theme - 'light' or 'dark'
     */
    setTheme: function(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
    },

    /**
     * Toggles between light and dark theme
     * @returns {string} The new theme
     */
    toggleTheme: function() {
        const current = this.getTheme();
        const newTheme = current === 'light' ? 'dark' : 'light';
        this.setTheme(newTheme);
        return newTheme;
    },

    /**
     * Watches for system preference changes
     * @param {object} dotNetRef - Reference to .NET object for callbacks
     */
    watchSystemPreference: function(dotNetRef) {
        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

        mediaQuery.addEventListener('change', (e) => {
            // Only update if user hasn't explicitly set a preference
            if (!localStorage.getItem('theme')) {
                const newTheme = e.matches ? 'dark' : 'light';
                document.documentElement.setAttribute('data-theme', newTheme);
                dotNetRef.invokeMethodAsync('OnSystemThemeChanged', newTheme);
            }
        });
    },

    /**
     * Clears the stored theme preference (reverts to system)
     */
    clearThemePreference: function() {
        localStorage.removeItem('theme');
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const systemTheme = prefersDark ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', systemTheme);
        return systemTheme;
    }
};
