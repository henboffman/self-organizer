/**
 * Self Organizer - Accessibility JavaScript Interop
 * Provides functions for applying accessibility settings from Blazor
 */

window.accessibilityInterop = {
    /**
     * Apply all accessibility settings at once
     * @param {Object} settings - AccessibilitySettings object from C#
     */
    applyAccessibilitySettings(settings) {
        if (!settings) return;

        // Apply color blindness mode
        this.applyColorBlindnessMode(settings.colorBlindnessMode);

        // Apply high contrast mode
        this.applyHighContrastMode(settings.highContrastMode);

        // Apply dyslexic font
        this.applyDyslexicFont(settings.dyslexiaFriendlyFont);

        // Apply text scaling
        this.applyTextScaling(settings.textScalingPercent);

        // Apply line height
        this.applyLineHeight(settings.lineHeightMultiplier);

        // Apply letter spacing
        this.applyLetterSpacing(settings.letterSpacing);

        // Apply reduced motion mode
        this.applyReducedMotionMode(settings.reducedMotionMode);

        // Apply focus indicator size
        this.applyFocusIndicatorSize(settings.focusIndicatorSize);

        // Apply larger click targets
        this.applyLargerClickTargets(settings.largerClickTargets);

        // Apply tooltip delay
        this.applyTooltipDelay(settings.tooltipDelayMs);

        // Apply link underlines
        this.applyLinkUnderlines(settings.alwaysShowLinkUnderlines);
    },

    /**
     * Apply color blindness mode
     * @param {number} mode - ColorBlindnessMode enum value (0=None, 1=Protanopia, 2=Deuteranopia, 3=Tritanopia, 4=Achromatopsia)
     */
    applyColorBlindnessMode(mode) {
        const body = document.body;
        const modes = ['colorblind-protanopia', 'colorblind-deuteranopia', 'colorblind-tritanopia', 'colorblind-achromatopsia'];

        // Remove all color blindness classes
        modes.forEach(m => body.classList.remove(m));

        // Apply new mode if not "None" (0)
        switch (mode) {
            case 1: body.classList.add('colorblind-protanopia'); break;
            case 2: body.classList.add('colorblind-deuteranopia'); break;
            case 3: body.classList.add('colorblind-tritanopia'); break;
            case 4: body.classList.add('colorblind-achromatopsia'); break;
        }
    },

    /**
     * Apply high contrast mode
     * @param {boolean} enabled - Whether high contrast is enabled
     */
    applyHighContrastMode(enabled) {
        document.body.classList.toggle('high-contrast', enabled);
    },

    /**
     * Apply dyslexic-friendly font
     * @param {boolean} enabled - Whether to use dyslexic-friendly font
     */
    applyDyslexicFont(enabled) {
        document.body.classList.toggle('dyslexic-font', enabled);
    },

    /**
     * Apply text scaling
     * @param {number} percent - Text scaling percentage (100-200)
     */
    applyTextScaling(percent) {
        const scale = Math.max(100, Math.min(200, percent)) / 100;
        document.documentElement.style.setProperty('--accessibility-text-scale', scale);
        document.documentElement.classList.toggle('text-scaled', percent !== 100);
    },

    /**
     * Apply line height multiplier
     * @param {number} multiplier - Line height multiplier (1.0-2.0)
     */
    applyLineHeight(multiplier) {
        const value = Math.max(1.0, Math.min(2.0, multiplier));
        document.documentElement.style.setProperty('--accessibility-line-height', value);
        document.body.classList.toggle('accessibility-text-settings', multiplier !== 1.5 || this._letterSpacing !== 0);
    },

    /**
     * Apply letter spacing
     * @param {number} spacing - Letter spacing in em units (0-0.2)
     */
    applyLetterSpacing(spacing) {
        const value = Math.max(0, Math.min(0.2, spacing));
        this._letterSpacing = value;
        document.documentElement.style.setProperty('--accessibility-letter-spacing', `${value}em`);
        document.body.classList.toggle('accessibility-text-settings', value !== 0 || this._lineHeight !== 1.5);
    },

    /**
     * Apply reduced motion mode
     * @param {number} mode - ReducedMotionMode enum (0=RespectSystem, 1=AlwaysReduce, 2=NeverReduce)
     */
    applyReducedMotionMode(mode) {
        const body = document.body;
        body.classList.remove('reduce-motion', 'allow-motion');

        switch (mode) {
            case 1: // AlwaysReduce
                body.classList.add('reduce-motion');
                break;
            case 2: // NeverReduce
                body.classList.add('allow-motion');
                break;
            // case 0: RespectSystem - no class needed, CSS media query handles it
        }
    },

    /**
     * Apply focus indicator size
     * @param {number} size - Focus indicator size in pixels (2-4)
     */
    applyFocusIndicatorSize(size) {
        const value = Math.max(2, Math.min(4, size));
        document.documentElement.style.setProperty('--accessibility-focus-size', `${value}px`);
        document.body.classList.toggle('focus-enhanced', size > 2);
    },

    /**
     * Apply larger click targets
     * @param {boolean} enabled - Whether to enable larger click targets
     */
    applyLargerClickTargets(enabled) {
        document.body.classList.toggle('larger-targets', enabled);
    },

    /**
     * Apply tooltip delay
     * @param {number} delayMs - Tooltip delay in milliseconds
     */
    applyTooltipDelay(delayMs) {
        document.documentElement.style.setProperty('--accessibility-tooltip-delay', `${delayMs}ms`);
    },

    /**
     * Apply link underlines setting
     * @param {boolean} enabled - Whether to always show link underlines
     */
    applyLinkUnderlines(enabled) {
        document.body.classList.toggle('show-link-underlines', enabled);
    },

    /**
     * Get system reduced motion preference
     * @returns {boolean} True if system prefers reduced motion
     */
    getSystemReducedMotion() {
        return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    },

    /**
     * Watch for system reduced motion preference changes
     * @param {Object} dotNetRef - .NET object reference for callback
     */
    watchReducedMotionPreference(dotNetRef) {
        const mediaQuery = window.matchMedia('(prefers-reduced-motion: reduce)');

        const handler = (e) => {
            dotNetRef.invokeMethodAsync('OnReducedMotionChanged', e.matches);
        };

        // Use addEventListener for modern browsers
        if (mediaQuery.addEventListener) {
            mediaQuery.addEventListener('change', handler);
        } else {
            // Fallback for older browsers
            mediaQuery.addListener(handler);
        }

        // Return cleanup function info
        return true;
    },

    /**
     * Get system high contrast preference (Windows)
     * @returns {boolean} True if system prefers high contrast
     */
    getSystemHighContrast() {
        return window.matchMedia('(forced-colors: active)').matches ||
               window.matchMedia('(-ms-high-contrast: active)').matches;
    },

    /**
     * Reset all accessibility settings to defaults
     */
    resetAccessibilitySettings() {
        const body = document.body;
        const html = document.documentElement;

        // Remove all accessibility classes
        body.classList.remove(
            'high-contrast',
            'dyslexic-font',
            'dyslexic-font-atkinson',
            'reduce-motion',
            'allow-motion',
            'focus-enhanced',
            'larger-targets',
            'show-link-underlines',
            'accessibility-text-settings',
            'colorblind-protanopia',
            'colorblind-deuteranopia',
            'colorblind-tritanopia',
            'colorblind-achromatopsia'
        );

        html.classList.remove('text-scaled');

        // Reset CSS variables
        html.style.removeProperty('--accessibility-text-scale');
        html.style.removeProperty('--accessibility-line-height');
        html.style.removeProperty('--accessibility-letter-spacing');
        html.style.removeProperty('--accessibility-focus-size');
        html.style.removeProperty('--accessibility-tooltip-delay');
    },

    /**
     * Announce message to screen readers
     * @param {string} message - Message to announce
     * @param {string} priority - 'polite' or 'assertive'
     */
    announceToScreenReader(message, priority = 'polite') {
        let announcer = document.getElementById('sr-announcer');

        if (!announcer) {
            announcer = document.createElement('div');
            announcer.id = 'sr-announcer';
            announcer.setAttribute('aria-live', priority);
            announcer.setAttribute('aria-atomic', 'true');
            announcer.className = 'sr-only';
            document.body.appendChild(announcer);
        }

        announcer.setAttribute('aria-live', priority);
        announcer.textContent = '';

        // Small delay ensures the change is picked up by screen readers
        setTimeout(() => {
            announcer.textContent = message;
        }, 100);
    }
};
