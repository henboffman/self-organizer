// Drag and drop interop for calendar timeline
// Handles real-time preview tracking in JavaScript for smooth performance
// Uses "hold to drag" pattern - must hold for 200ms before drag activates
window.dragDropInterop = {
    _previewElement: null,
    _timelineBody: null,
    _config: null,
    _isActive: false,
    _lastPosition: null,

    // Hold-to-drag state
    _holdTimer: null,
    _holdDelay: 200, // ms to hold before drag activates
    _dragReady: false,
    _currentDragElement: null,

    /**
     * Initialize hold-to-drag on an element
     * Called on mousedown - starts the hold timer
     */
    initHoldToDrag: function (element, startHour, hourHeightPx, gutterWidthPx, daysInView) {
        // Clear any existing timer
        if (this._holdTimer) {
            clearTimeout(this._holdTimer);
            this._holdTimer = null;
        }

        this._dragReady = false;
        this._currentDragElement = element;
        this._config = { startHour, hourHeightPx, gutterWidthPx, daysInView };

        // Start hold timer
        this._holdTimer = setTimeout(() => {
            this._dragReady = true;
            this._currentDragElement = element;

            // Add visual feedback that drag is ready
            if (element) {
                element.style.cursor = 'grabbing';
                element.style.opacity = '0.8';
            }
        }, this._holdDelay);

        return true;
    },

    /**
     * Cancel hold-to-drag (called on mouseup if drag hasn't started)
     */
    cancelHoldToDrag: function () {
        if (this._holdTimer) {
            clearTimeout(this._holdTimer);
            this._holdTimer = null;
        }

        // Reset visual feedback
        if (this._currentDragElement) {
            this._currentDragElement.style.cursor = '';
            this._currentDragElement.style.opacity = '';
        }

        this._dragReady = false;
        this._currentDragElement = null;

        return true;
    },

    /**
     * Check if drag is ready (hold time elapsed)
     */
    isDragReady: function () {
        return this._dragReady;
    },

    /**
     * Initialize drag tracking when a drag starts (after hold threshold met)
     * Called from Blazor when drag begins
     */
    startDrag: function (startHour, hourHeightPx, gutterWidthPx, daysInView) {
        // Clear hold timer if still running
        if (this._holdTimer) {
            clearTimeout(this._holdTimer);
            this._holdTimer = null;
        }

        this._config = { startHour, hourHeightPx, gutterWidthPx, daysInView };
        this._timelineBody = document.querySelector('.timeline-body');
        this._isActive = true;
        this._lastPosition = null;

        // Create preview element if it doesn't exist
        if (!this._previewElement) {
            this._previewElement = document.createElement('div');
            this._previewElement.className = 'drop-preview-indicator';
            this._previewElement.innerHTML = '<span class="drop-preview-time"></span>';
        }

        // Add event listeners for real-time tracking
        if (this._timelineBody) {
            this._boundDragOver = this._handleDragOver.bind(this);
            this._boundDragLeave = this._handleDragLeave.bind(this);
            this._timelineBody.addEventListener('dragover', this._boundDragOver);
            this._timelineBody.addEventListener('dragleave', this._boundDragLeave);
        }
    },

    /**
     * Handle dragover - updates preview position in real-time
     */
    _handleDragOver: function (e) {
        if (!this._isActive || !this._timelineBody || !this._config) return;

        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';

        const position = this._calculatePosition(e.clientX, e.clientY);
        if (!position) return;

        this._lastPosition = position;
        this._updatePreview(position);
    },

    /**
     * Handle dragleave - hide preview when leaving timeline
     */
    _handleDragLeave: function (e) {
        // Only hide if truly leaving the timeline body
        const relatedTarget = e.relatedTarget;
        if (!this._timelineBody || !this._timelineBody.contains(relatedTarget)) {
            this._hidePreview();
        }
    },

    /**
     * Calculate position from mouse coordinates
     */
    _calculatePosition: function (clientX, clientY) {
        if (!this._timelineBody || !this._config) return null;

        const rect = this._timelineBody.getBoundingClientRect();
        const scrollTop = this._timelineBody.scrollTop || 0;

        // Y position relative to timeline content (accounting for scroll)
        const y = clientY - rect.top + scrollTop;
        const x = clientX - rect.left;

        const { startHour, hourHeightPx, gutterWidthPx, daysInView } = this._config;

        // Calculate time from Y position
        const totalMinutes = (y / hourHeightPx) * 60;
        const rawHour = Math.floor(totalMinutes / 60) + startHour;
        const rawMinute = Math.round((totalMinutes % 60) / 15) * 15; // Snap to 15-minute intervals

        // Handle minute overflow
        let hour = rawHour;
        let minute = rawMinute;
        if (minute >= 60) {
            minute = 0;
            hour += 1;
        }

        // Clamp hour to valid range (startHour to 21)
        hour = Math.max(startHour, Math.min(hour, 21));

        // Calculate day index from X position (for multi-day views)
        const availableWidth = rect.width - gutterWidthPx;
        const xInTimeline = x - gutterWidthPx;

        let dayIndex = 0;
        if (daysInView > 1 && xInTimeline >= 0) {
            const dayWidth = availableWidth / daysInView;
            dayIndex = Math.floor(xInTimeline / dayWidth);
            dayIndex = Math.max(0, Math.min(dayIndex, daysInView - 1));
        }

        return { hour, minute, dayIndex };
    },

    /**
     * Update preview element position and time display
     */
    _updatePreview: function (position) {
        if (!this._previewElement || !this._timelineBody || !this._config) return;

        const { startHour, hourHeightPx, gutterWidthPx, daysInView } = this._config;
        const rect = this._timelineBody.getBoundingClientRect();

        // Calculate pixel position
        const topPx = ((position.hour - startHour) * 60 + position.minute) * (hourHeightPx / 60);

        // Calculate left position and width for multi-day view
        const availableWidth = rect.width - gutterWidthPx;
        const dayWidth = availableWidth / daysInView;
        const leftPx = gutterWidthPx + (position.dayIndex * dayWidth);
        const widthPx = dayWidth - 4; // Small margin

        // Format time for display
        const hour12 = position.hour % 12 || 12;
        const ampm = position.hour < 12 ? 'AM' : 'PM';
        const minuteStr = position.minute.toString().padStart(2, '0');
        const timeText = `${hour12}:${minuteStr} ${ampm}`;

        // Update preview element
        this._previewElement.style.cssText = `
            position: absolute;
            top: ${topPx}px;
            left: ${leftPx}px;
            width: ${widthPx}px;
            height: 4px;
            background-color: var(--color-primary, #6366f1);
            border-radius: 2px;
            pointer-events: none;
            z-index: 1000;
            box-shadow: 0 0 12px rgba(99, 102, 241, 0.6);
            animation: pulse-glow 1s ease-in-out infinite;
        `;

        const timeSpan = this._previewElement.querySelector('.drop-preview-time');
        if (timeSpan) {
            timeSpan.textContent = timeText;
            timeSpan.style.cssText = `
                position: absolute;
                left: 12px;
                top: -24px;
                font-size: 0.8rem;
                font-weight: 700;
                color: white;
                background-color: var(--color-primary, #6366f1);
                padding: 3px 8px;
                border-radius: 4px;
                white-space: nowrap;
                box-shadow: 0 2px 6px rgba(0, 0, 0, 0.2);
            `;
        }

        // Add dot indicator
        if (!this._previewElement.querySelector('.preview-dot')) {
            const dot = document.createElement('div');
            dot.className = 'preview-dot';
            dot.style.cssText = `
                position: absolute;
                left: -5px;
                top: -3px;
                width: 10px;
                height: 10px;
                background-color: var(--color-primary, #6366f1);
                border-radius: 50%;
                box-shadow: 0 0 4px rgba(99, 102, 241, 0.5);
            `;
            this._previewElement.appendChild(dot);
        }

        // Ensure preview is in the timeline body
        if (!this._timelineBody.contains(this._previewElement)) {
            this._timelineBody.appendChild(this._previewElement);
        }

        this._previewElement.style.display = 'block';
    },

    /**
     * Hide the preview element
     */
    _hidePreview: function () {
        if (this._previewElement) {
            this._previewElement.style.display = 'none';
        }
    },

    /**
     * End drag tracking and clean up
     * Returns the last calculated position for the drop
     */
    endDrag: function () {
        const position = this._lastPosition;

        // Clean up
        this._hidePreview();
        if (this._timelineBody && this._boundDragOver) {
            this._timelineBody.removeEventListener('dragover', this._boundDragOver);
            this._timelineBody.removeEventListener('dragleave', this._boundDragLeave);
        }

        // Reset hold-to-drag state
        if (this._holdTimer) {
            clearTimeout(this._holdTimer);
            this._holdTimer = null;
        }
        if (this._currentDragElement) {
            this._currentDragElement.style.cursor = '';
            this._currentDragElement.style.opacity = '';
        }

        this._isActive = false;
        this._lastPosition = null;
        this._config = null;
        this._dragReady = false;
        this._currentDragElement = null;

        return position;
    },

    /**
     * Get the last calculated position (for drop handling)
     */
    getLastPosition: function () {
        return this._lastPosition;
    },

    /**
     * Calculate drop position from client coordinates (fallback)
     * Called from Blazor when a drop event occurs
     */
    calculateDropFromEvent: function (clientX, clientY, startHour, hourHeightPx, gutterWidthPx, daysInView) {
        this._config = { startHour, hourHeightPx, gutterWidthPx, daysInView };
        this._timelineBody = document.querySelector('.timeline-body');
        return this._calculatePosition(clientX, clientY);
    },

    /**
     * Clean up resources
     */
    dispose: function () {
        this._hidePreview();
        if (this._previewElement && this._previewElement.parentNode) {
            this._previewElement.parentNode.removeChild(this._previewElement);
        }
        if (this._holdTimer) {
            clearTimeout(this._holdTimer);
        }
        this._previewElement = null;
        this._timelineBody = null;
        this._config = null;
        this._isActive = false;
        this._lastPosition = null;
        this._holdTimer = null;
        this._dragReady = false;
        this._currentDragElement = null;
    }
};
