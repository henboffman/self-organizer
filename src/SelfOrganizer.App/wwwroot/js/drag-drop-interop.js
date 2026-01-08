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
    _selectStartHandler: null,

    /**
     * Initialize hold-to-drag on an element
     * Called on mousedown - starts the hold timer
     * @param {string} itemType - 'task' or 'event'
     * @param {string} itemId - The ID of the item being dragged
     * @param {number} clientY - Optional Y coordinate of mouse for edge detection
     */
    initHoldToDrag: function (itemType, itemId, startHour, hourHeightPx, gutterWidthPx, daysInView, clientY) {
        // Find the element by data attributes first
        const selector = `[data-item-type="${itemType}"][data-item-id="${itemId}"]`;
        const element = document.querySelector(selector);

        // Check if resize interop is handling this event (resize takes priority)
        // First check: use the resize interop's tracked state
        if (window.resizeInterop && window.resizeInterop.isOnResizeEdge()) {
            this._dragReady = false;
            return false;
        }

        // Second check: If clientY is provided, directly check edge proximity on the element
        if (element && clientY !== undefined && clientY !== null) {
            const rect = element.getBoundingClientRect();
            const relativeY = clientY - rect.top;
            const edgeThreshold = 18; // Match resize interop's threshold

            if (relativeY <= edgeThreshold || relativeY >= rect.height - edgeThreshold) {
                // We're on an edge - don't allow drag, let resize handle it
                this._dragReady = false;
                return false;
            }
        }

        // Clear any existing timer
        if (this._holdTimer) {
            clearTimeout(this._holdTimer);
            this._holdTimer = null;
        }

        this._config = { startHour, hourHeightPx, gutterWidthPx, daysInView };
        this._currentDragElement = element;

        // CRITICAL: Prevent text selection during drag
        this._selectStartHandler = (e) => {
            e.preventDefault();
            return false;
        };
        document.addEventListener('selectstart', this._selectStartHandler);

        // Also prevent any existing selection
        window.getSelection()?.removeAllRanges();

        // Add dragging class to timeline body to trigger CSS that prevents selection
        this._timelineBody = document.querySelector('.timeline-body');
        if (this._timelineBody) {
            this._timelineBody.classList.add('dragging-active');
        }

        // Add visual feedback
        if (element) {
            element.style.cursor = 'grabbing';
            element.style.opacity = '0.8';
        }

        // Immediately mark as ready (removing hold delay for now to debug)
        this._dragReady = true;

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

        // Remove selectstart handler to allow text selection again
        if (this._selectStartHandler) {
            document.removeEventListener('selectstart', this._selectStartHandler);
            this._selectStartHandler = null;
        }

        // Remove dragging class from timeline body
        if (this._timelineBody) {
            this._timelineBody.classList.remove('dragging-active');
        }

        // Reset visual feedback
        if (this._currentDragElement) {
            this._currentDragElement.style.cursor = '';
            this._currentDragElement.style.opacity = '';
            this._currentDragElement.classList.remove('drag-ready');
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

        // Remove selectstart handler to allow text selection again
        if (this._selectStartHandler) {
            document.removeEventListener('selectstart', this._selectStartHandler);
            this._selectStartHandler = null;
        }

        // Remove dragging class from timeline body
        if (this._timelineBody) {
            this._timelineBody.classList.remove('dragging-active');
        }

        // Reset hold-to-drag state
        if (this._holdTimer) {
            clearTimeout(this._holdTimer);
            this._holdTimer = null;
        }

        if (this._currentDragElement) {
            this._currentDragElement.style.cursor = '';
            this._currentDragElement.style.opacity = '';
            this._currentDragElement.classList.remove('drag-ready');
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
        if (this._selectStartHandler) {
            document.removeEventListener('selectstart', this._selectStartHandler);
            this._selectStartHandler = null;
        }
        if (this._timelineBody) {
            this._timelineBody.classList.remove('dragging-active');
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

// Quick Block Creation - Click and drag on empty calendar space
window.quickBlockInterop = {
    _isCreating: false,
    _startY: 0,
    _startX: 0,
    _startHour: 0,
    _previewElement: null,
    _timelineBody: null,
    _config: null,
    _dotNetRef: null,
    _startPosition: null,
    _endPosition: null,

    /**
     * Initialize quick block creation on the timeline
     */
    init: function (dotNetRef, config) {
        this._dotNetRef = dotNetRef;
        this._config = config;
        this._timelineBody = document.querySelector('.timeline-body');

        if (!this._timelineBody) return false;

        // Create preview element
        if (!this._previewElement) {
            this._previewElement = document.createElement('div');
            this._previewElement.className = 'quick-block-preview';
            this._previewElement.innerHTML = `
                <div class="quick-block-time-start"></div>
                <div class="quick-block-duration"></div>
                <div class="quick-block-time-end"></div>
            `;
        }

        // Add event listeners
        this._boundMouseDown = this._handleMouseDown.bind(this);
        this._boundMouseMove = this._handleMouseMove.bind(this);
        this._boundMouseUp = this._handleMouseUp.bind(this);

        this._timelineBody.addEventListener('mousedown', this._boundMouseDown);
        document.addEventListener('mousemove', this._boundMouseMove);
        document.addEventListener('mouseup', this._boundMouseUp);

        return true;
    },

    /**
     * Update configuration (called when view changes)
     */
    updateConfig: function (config) {
        this._config = config;
    },

    /**
     * Handle mouse down on timeline
     */
    _handleMouseDown: function (e) {
        // Only handle left click on empty space (not on events)
        if (e.button !== 0) return;
        if (e.target.closest('.timeline-event')) return;
        if (!e.target.closest('.timeline-hour-cell') && !e.target.closest('.timeline-day-column')) return;

        e.preventDefault();

        const rect = this._timelineBody.getBoundingClientRect();
        this._startY = e.clientY - rect.top + this._timelineBody.scrollTop;
        this._startX = e.clientX - rect.left;

        this._startPosition = this._calculatePosition(e.clientX, e.clientY);
        if (!this._startPosition) return;

        this._isCreating = true;

        // Add preview to timeline
        if (!this._timelineBody.contains(this._previewElement)) {
            this._timelineBody.appendChild(this._previewElement);
        }

        this._updatePreview(this._startPosition, this._startPosition);
        this._previewElement.style.display = 'block';
        this._previewElement.classList.add('creating');
    },

    /**
     * Handle mouse move during creation
     */
    _handleMouseMove: function (e) {
        if (!this._isCreating) return;

        const currentPosition = this._calculatePosition(e.clientX, e.clientY);
        if (!currentPosition) return;

        this._endPosition = currentPosition;
        this._updatePreview(this._startPosition, this._endPosition);
    },

    /**
     * Handle mouse up - complete creation
     */
    _handleMouseUp: function (e) {
        if (!this._isCreating) return;

        this._isCreating = false;
        this._previewElement.classList.remove('creating');

        const endPosition = this._calculatePosition(e.clientX, e.clientY);
        if (!endPosition || !this._startPosition) {
            this._hidePreview();
            return;
        }

        // Calculate actual start and end times
        let startPos = this._startPosition;
        let endPos = endPosition;

        // Swap if dragged upward
        const startMinutes = startPos.hour * 60 + startPos.minute;
        const endMinutes = endPos.hour * 60 + endPos.minute;

        if (endMinutes < startMinutes) {
            [startPos, endPos] = [endPos, startPos];
        }

        // Minimum duration of 15 minutes
        const duration = (endPos.hour * 60 + endPos.minute) - (startPos.hour * 60 + startPos.minute);
        if (duration < 15) {
            endPos = {
                ...startPos,
                hour: startPos.hour + (startPos.minute + 30 >= 60 ? 1 : 0),
                minute: (startPos.minute + 30) % 60
            };
        }

        // Get center position for radial menu
        const rect = this._timelineBody.getBoundingClientRect();
        const previewRect = this._previewElement.getBoundingClientRect();
        const centerX = previewRect.left + previewRect.width / 2;
        const centerY = previewRect.top + previewRect.height / 2;

        // Hide preview with animation
        this._previewElement.classList.add('selected');
        setTimeout(() => {
            this._hidePreview();
        }, 200);

        // Notify Blazor to show radial menu
        if (this._dotNetRef) {
            this._dotNetRef.invokeMethodAsync('OnQuickBlockCreated',
                startPos.hour, startPos.minute,
                endPos.hour, endPos.minute,
                startPos.dayIndex,
                centerX, centerY
            );
        }
    },

    /**
     * Calculate position from mouse coordinates
     */
    _calculatePosition: function (clientX, clientY) {
        if (!this._timelineBody || !this._config) return null;

        const rect = this._timelineBody.getBoundingClientRect();
        const scrollTop = this._timelineBody.scrollTop || 0;

        const y = clientY - rect.top + scrollTop;
        const x = clientX - rect.left;

        const { startHour, hourHeightPx, gutterWidthPx, daysInView } = this._config;

        // Calculate time from Y position (snap to 15 minutes)
        const totalMinutes = (y / hourHeightPx) * 60;
        const rawHour = Math.floor(totalMinutes / 60) + startHour;
        const rawMinute = Math.round((totalMinutes % 60) / 15) * 15;

        let hour = rawHour;
        let minute = rawMinute;
        if (minute >= 60) {
            minute = 0;
            hour += 1;
        }

        hour = Math.max(startHour, Math.min(hour, 21));

        // Calculate day index
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
     * Update preview element
     */
    _updatePreview: function (startPos, endPos) {
        if (!this._previewElement || !this._config) return;

        const { startHour, hourHeightPx, gutterWidthPx, daysInView } = this._config;
        const rect = this._timelineBody.getBoundingClientRect();

        // Ensure start is before end
        let actualStart = startPos;
        let actualEnd = endPos;
        const startMinutes = startPos.hour * 60 + startPos.minute;
        const endMinutes = endPos.hour * 60 + endPos.minute;

        if (endMinutes < startMinutes) {
            actualStart = endPos;
            actualEnd = startPos;
        }

        // Calculate pixel positions
        const topPx = ((actualStart.hour - startHour) * 60 + actualStart.minute) * (hourHeightPx / 60);
        const bottomPx = ((actualEnd.hour - startHour) * 60 + actualEnd.minute) * (hourHeightPx / 60);
        const heightPx = Math.max(bottomPx - topPx, hourHeightPx / 4);

        // Calculate left position and width
        const availableWidth = rect.width - gutterWidthPx;
        const dayWidth = availableWidth / daysInView;
        const leftPx = gutterWidthPx + (startPos.dayIndex * dayWidth);
        const widthPx = dayWidth - 8;

        // Update element style
        this._previewElement.style.cssText = `
            position: absolute;
            top: ${topPx}px;
            left: ${leftPx}px;
            width: ${widthPx}px;
            height: ${heightPx}px;
            display: block;
        `;

        // Update time displays
        const formatTime = (h, m) => {
            const hour12 = h % 12 || 12;
            const ampm = h < 12 ? 'AM' : 'PM';
            const minuteStr = m.toString().padStart(2, '0');
            return `${hour12}:${minuteStr} ${ampm}`;
        };

        const duration = (actualEnd.hour * 60 + actualEnd.minute) - (actualStart.hour * 60 + actualStart.minute);
        const durationStr = duration >= 60
            ? `${Math.floor(duration / 60)}h ${duration % 60}m`
            : `${duration}m`;

        const startTimeEl = this._previewElement.querySelector('.quick-block-time-start');
        const endTimeEl = this._previewElement.querySelector('.quick-block-time-end');
        const durationEl = this._previewElement.querySelector('.quick-block-duration');

        if (startTimeEl) startTimeEl.textContent = formatTime(actualStart.hour, actualStart.minute);
        if (endTimeEl) endTimeEl.textContent = formatTime(actualEnd.hour, actualEnd.minute);
        if (durationEl) durationEl.textContent = durationStr;
    },

    /**
     * Hide preview
     */
    _hidePreview: function () {
        if (this._previewElement) {
            this._previewElement.style.display = 'none';
            this._previewElement.classList.remove('creating', 'selected');
        }
    },

    /**
     * Clean up
     */
    dispose: function () {
        if (this._timelineBody) {
            this._timelineBody.removeEventListener('mousedown', this._boundMouseDown);
        }
        document.removeEventListener('mousemove', this._boundMouseMove);
        document.removeEventListener('mouseup', this._boundMouseUp);

        this._hidePreview();
        if (this._previewElement && this._previewElement.parentNode) {
            this._previewElement.parentNode.removeChild(this._previewElement);
        }

        this._previewElement = null;
        this._timelineBody = null;
        this._config = null;
        this._dotNetRef = null;
    }
};

// Resize Interop - Drag from top/bottom edges to adjust duration
window.resizeInterop = {
    _isResizing: false,
    _resizeEdge: null, // 'top' or 'bottom'
    _element: null,
    _itemType: null, // 'task' or 'event'
    _itemId: null,
    _originalStartTime: null,
    _originalEndTime: null,
    _currentStartTime: null,
    _currentEndTime: null,
    _dayIndex: null,
    _previewElement: null,
    _timelineBody: null,
    _config: null,
    _dotNetRef: null,
    _existingEvents: [], // For overlap detection
    _edgeThreshold: 18, // Pixels from edge to trigger resize cursor (increased for easier targeting)
    _hoveredElement: null,
    _hoveredEdge: null,
    _preventDrag: false, // Flag to prevent drag when resizing
    _resizeStarted: false, // Flag to indicate resize has started
    _lastMouseEvent: null, // Store last mouse event for edge detection

    /**
     * Initialize resize interop
     */
    init: function (dotNetRef, config) {
        this._dotNetRef = dotNetRef;
        this._config = config;
        this._timelineBody = document.querySelector('.timeline-body');

        if (!this._timelineBody) return false;

        // Create preview element
        if (!this._previewElement) {
            this._previewElement = document.createElement('div');
            this._previewElement.className = 'resize-preview';
            this._previewElement.innerHTML = `
                <div class="resize-preview-time-start"></div>
                <div class="resize-preview-duration"></div>
                <div class="resize-preview-time-end"></div>
            `;
        }

        // Global mouse move/up handlers for resize
        this._boundGlobalMouseMove = this._handleGlobalMouseMove.bind(this);
        this._boundGlobalMouseUp = this._handleGlobalMouseUp.bind(this);
        this._boundEventMouseMove = this._handleEventMouseMove.bind(this);
        this._boundEventMouseDown = this._handleEventMouseDown.bind(this);
        this._boundEventMouseLeave = this._handleEventMouseLeave.bind(this);
        this._boundPreventDragStart = this._handlePreventDragStart.bind(this);

        document.addEventListener('mousemove', this._boundGlobalMouseMove);
        document.addEventListener('mouseup', this._boundGlobalMouseUp);
        // Capture phase dragstart handler to prevent drag when resizing
        document.addEventListener('dragstart', this._boundPreventDragStart, true);

        // Add event listeners to timeline body for event hover detection
        this._timelineBody.addEventListener('mousemove', this._boundEventMouseMove, true);
        this._timelineBody.addEventListener('mousedown', this._boundEventMouseDown, true);
        this._timelineBody.addEventListener('mouseleave', this._boundEventMouseLeave, true);

        return true;
    },

    /**
     * Prevent drag start when we're doing a resize operation
     */
    _handlePreventDragStart: function (e) {
        // First check: flags are set
        if (this._preventDrag || this._isResizing) {
            e.preventDefault();
            e.stopPropagation();
            return false;
        }

        // Second check: if we're hovering on an edge, prevent drag
        if (this._hoveredEdge) {
            e.preventDefault();
            e.stopPropagation();
            return false;
        }

        // Third check: dynamically check if the drag target is on a resize edge
        const eventEl = e.target.closest('.timeline-event');
        if (eventEl && (eventEl.classList.contains('resizable') || eventEl.classList.contains('draggable-item'))) {
            const rect = eventEl.getBoundingClientRect();
            // Use the dragstart event's clientY directly (most accurate)
            // Fall back to lastMouseEvent if dragstart doesn't have coordinates
            const clientY = e.clientY || (this._lastMouseEvent ? this._lastMouseEvent.clientY : null);

            if (clientY !== null) {
                const relativeY = clientY - rect.top;

                if (relativeY <= this._edgeThreshold || relativeY >= rect.height - this._edgeThreshold) {
                    e.preventDefault();
                    e.stopPropagation();
                    return false;
                }
            }
        }
    },

    /**
     * Handle mouse move over events to detect edge proximity
     */
    _handleEventMouseMove: function (e) {
        // Always store the last mouse event for edge detection
        this._lastMouseEvent = e;

        if (this._isResizing) return;

        // Find event element under cursor
        const eventEl = e.target.closest('.timeline-event');

        // Clear previous hover state
        if (this._hoveredElement && this._hoveredElement !== eventEl) {
            this._hoveredElement.style.cursor = '';
            this._hoveredElement.classList.remove('resize-edge-hover');
        }

        if (!eventEl) {
            this._hoveredElement = null;
            this._hoveredEdge = null;
            return;
        }

        // Check if this event is resizable (has draggable-item class or is local event)
        const isResizable = eventEl.classList.contains('draggable-item');
        if (!isResizable) {
            this._hoveredElement = null;
            this._hoveredEdge = null;
            return;
        }

        this._hoveredElement = eventEl;

        // Check edge proximity
        const rect = eventEl.getBoundingClientRect();
        const relativeY = e.clientY - rect.top;

        if (relativeY <= this._edgeThreshold) {
            eventEl.style.cursor = 'ns-resize';
            eventEl.classList.add('resize-edge-hover');
            this._hoveredEdge = 'top';
        } else if (relativeY >= rect.height - this._edgeThreshold) {
            eventEl.style.cursor = 'ns-resize';
            eventEl.classList.add('resize-edge-hover');
            this._hoveredEdge = 'bottom';
        } else {
            eventEl.style.cursor = '';
            eventEl.classList.remove('resize-edge-hover');
            this._hoveredEdge = null;
        }
    },

    /**
     * Check if currently on a resize edge (called by drag interop)
     * This is the key function that prevents drag when we should resize instead
     */
    isOnResizeEdge: function () {
        // If we're already in a resize operation, definitely on edge
        if (this._preventDrag || this._isResizing || this._resizeStarted) {
            return true;
        }

        // Check if hoveredEdge is set from the last mouse move
        if (this._hoveredEdge) {
            return true;
        }

        // Fallback: Use last mouse event to check edge proximity dynamically
        if (this._lastMouseEvent && this._hoveredElement) {
            const rect = this._hoveredElement.getBoundingClientRect();
            const relativeY = this._lastMouseEvent.clientY - rect.top;

            if (relativeY <= this._edgeThreshold || relativeY >= rect.height - this._edgeThreshold) {
                return true;
            }
        }

        return false;
    },

    /**
     * Handle mouse down to potentially start resize
     */
    _handleEventMouseDown: function (e) {
        if (this._isResizing) return;

        // Only handle left click
        if (e.button !== 0) return;

        // Find event element and check edge on mousedown directly
        const eventEl = e.target.closest('.timeline-event');
        if (!eventEl) return;

        // Check if this event is resizable (has resizable class)
        const isResizable = eventEl.classList.contains('resizable') || eventEl.classList.contains('draggable-item');
        if (!isResizable) return;

        // Check edge proximity
        const rect = eventEl.getBoundingClientRect();
        const relativeY = e.clientY - rect.top;
        let edge = null;

        if (relativeY <= this._edgeThreshold) {
            edge = 'top';
        } else if (relativeY >= rect.height - this._edgeThreshold) {
            edge = 'bottom';
        }

        if (!edge) {
            // Not on an edge, let normal drag handling proceed
            this._preventDrag = false;
            this._resizeStarted = false;
            return;
        }

        // We're on an edge - prevent the drag from starting
        this._preventDrag = true;
        this._resizeStarted = true;

        // Get item info from data attributes
        const itemType = eventEl.dataset.itemType;
        const itemId = eventEl.dataset.itemId;

        if (!itemType || !itemId) {
            this._preventDrag = false;
            this._resizeStarted = false;
            return;
        }

        // Parse time from the element's style and data
        const topPx = parseFloat(eventEl.style.top);
        const heightPx = parseFloat(eventEl.style.height);

        if (isNaN(topPx) || isNaN(heightPx) || !this._config) {
            this._preventDrag = false;
            this._resizeStarted = false;
            return;
        }

        // Calculate times from pixel positions
        const startMinutesFromTop = (topPx / this._config.hourHeightPx) * 60;
        const startHour = Math.floor(startMinutesFromTop / 60) + this._config.startHour;
        const startMinute = Math.round(startMinutesFromTop % 60);

        const endMinutesFromTop = ((topPx + heightPx) / this._config.hourHeightPx) * 60;
        const endHour = Math.floor(endMinutesFromTop / 60) + this._config.startHour;
        const endMinute = Math.round(endMinutesFromTop % 60);

        // Calculate day index from left position
        const leftStr = eventEl.style.left;
        let dayIndex = 0;
        if (leftStr && this._config.daysInView > 1) {
            const leftMatch = leftStr.match(/calc\((\d+)px/);
            if (leftMatch) {
                const leftPx = parseFloat(leftMatch[1]);
                const availableWidth = this._timelineBody.getBoundingClientRect().width - this._config.gutterWidthPx;
                const dayWidth = availableWidth / this._config.daysInView;
                dayIndex = Math.floor((leftPx - this._config.gutterWidthPx) / dayWidth);
            }
        }

        // Prevent default to stop drag from starting
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();

        // Start resize
        this.startResize(eventEl, edge, itemType, itemId, startHour, startMinute, endHour, endMinute, dayIndex);
    },

    /**
     * Handle mouse leave from timeline body
     */
    _handleEventMouseLeave: function (e) {
        if (this._hoveredElement && !this._isResizing) {
            this._hoveredElement.style.cursor = '';
            this._hoveredElement.classList.remove('resize-edge-hover');
            this._hoveredElement = null;
            this._hoveredEdge = null;
        }
    },

    /**
     * Update configuration
     */
    updateConfig: function (config) {
        this._config = config;
    },

    /**
     * Set existing events for overlap detection
     */
    setExistingEvents: function (events) {
        this._existingEvents = events || [];
    },

    /**
     * Check if mouse is near edge of an element and show appropriate cursor
     * Called on mousemove over timeline events
     */
    checkEdge: function (element, clientY) {
        if (!element) return null;

        const rect = element.getBoundingClientRect();
        const relativeY = clientY - rect.top;

        if (relativeY <= this._edgeThreshold) {
            element.style.cursor = 'ns-resize';
            return 'top';
        } else if (relativeY >= rect.height - this._edgeThreshold) {
            element.style.cursor = 'ns-resize';
            return 'bottom';
        } else {
            element.style.cursor = '';
            return null;
        }
    },

    /**
     * Start resize operation
     */
    startResize: function (element, edge, itemType, itemId, startHour, startMinute, endHour, endMinute, dayIndex) {
        if (!element || !edge) return false;

        this._isResizing = true;
        this._resizeEdge = edge;
        this._element = element;
        this._itemType = itemType;
        this._itemId = itemId;
        this._dayIndex = dayIndex;

        // Store original times
        this._originalStartTime = { hour: startHour, minute: startMinute };
        this._originalEndTime = { hour: endHour, minute: endMinute };
        this._currentStartTime = { ...this._originalStartTime };
        this._currentEndTime = { ...this._originalEndTime };

        // Add resizing class to body
        document.body.classList.add('resizing');

        // Show preview
        if (this._timelineBody && !this._timelineBody.contains(this._previewElement)) {
            this._timelineBody.appendChild(this._previewElement);
        }

        this._updatePreview();
        this._previewElement.style.display = 'block';
        this._previewElement.classList.add('active');

        // Hide the original element while resizing
        element.style.opacity = '0.3';

        return true;
    },

    /**
     * Handle global mouse move during resize
     */
    _handleGlobalMouseMove: function (e) {
        if (!this._isResizing || !this._timelineBody || !this._config) return;

        const position = this._calculateTimeFromY(e.clientY);
        if (!position) return;

        const minDuration = 15; // Minimum 15 minutes

        if (this._resizeEdge === 'top') {
            // Dragging top edge - adjust start time
            const newStartMinutes = position.hour * 60 + position.minute;
            const endMinutes = this._originalEndTime.hour * 60 + this._originalEndTime.minute;

            // Enforce minimum duration
            if (endMinutes - newStartMinutes >= minDuration) {
                this._currentStartTime = { hour: position.hour, minute: position.minute };
            }
        } else if (this._resizeEdge === 'bottom') {
            // Dragging bottom edge - adjust end time
            const startMinutes = this._originalStartTime.hour * 60 + this._originalStartTime.minute;
            const newEndMinutes = position.hour * 60 + position.minute;

            // Enforce minimum duration
            if (newEndMinutes - startMinutes >= minDuration) {
                this._currentEndTime = { hour: position.hour, minute: position.minute };
            }
        }

        this._updatePreview();
    },

    /**
     * Handle global mouse up - complete resize
     */
    _handleGlobalMouseUp: function (e) {
        if (!this._isResizing) return;

        const result = {
            itemType: this._itemType,
            itemId: this._itemId,
            dayIndex: this._dayIndex,
            startHour: this._currentStartTime.hour,
            startMinute: this._currentStartTime.minute,
            endHour: this._currentEndTime.hour,
            endMinute: this._currentEndTime.minute
        };

        // Clean up
        this._endResize();

        // Notify Blazor of the resize completion
        if (this._dotNetRef) {
            this._dotNetRef.invokeMethodAsync('OnResizeCompleted',
                result.itemType,
                result.itemId,
                result.startHour,
                result.startMinute,
                result.endHour,
                result.endMinute,
                result.dayIndex
            );
        }
    },

    /**
     * Calculate time from Y position
     */
    _calculateTimeFromY: function (clientY) {
        if (!this._timelineBody || !this._config) return null;

        const rect = this._timelineBody.getBoundingClientRect();
        const scrollTop = this._timelineBody.scrollTop || 0;
        const y = clientY - rect.top + scrollTop;

        const { startHour, hourHeightPx } = this._config;

        // Calculate time from Y position (snap to 15 minutes)
        const totalMinutes = (y / hourHeightPx) * 60;
        const rawHour = Math.floor(totalMinutes / 60) + startHour;
        const rawMinute = Math.round((totalMinutes % 60) / 15) * 15;

        let hour = rawHour;
        let minute = rawMinute;
        if (minute >= 60) {
            minute = 0;
            hour += 1;
        }

        // Clamp to valid hours
        hour = Math.max(startHour, Math.min(hour, 21));

        return { hour, minute };
    },

    /**
     * Update the resize preview
     */
    _updatePreview: function () {
        if (!this._previewElement || !this._config || !this._timelineBody) return;

        const { startHour, hourHeightPx, gutterWidthPx, daysInView } = this._config;
        const rect = this._timelineBody.getBoundingClientRect();

        // Calculate pixel positions
        const topPx = ((this._currentStartTime.hour - startHour) * 60 + this._currentStartTime.minute) * (hourHeightPx / 60);
        const bottomPx = ((this._currentEndTime.hour - startHour) * 60 + this._currentEndTime.minute) * (hourHeightPx / 60);
        const heightPx = Math.max(bottomPx - topPx, hourHeightPx / 4);

        // Calculate left position and width
        const availableWidth = rect.width - gutterWidthPx;
        const dayWidth = availableWidth / daysInView;
        const dayIndex = this._dayIndex || 0;

        // Check for overlaps with existing events
        const overlappingEvents = this._findOverlappingEvents();
        const totalColumns = overlappingEvents.length > 0 ? 2 : 1;
        const column = overlappingEvents.length > 0 ? 1 : 0;

        const leftPx = gutterWidthPx + (dayIndex * dayWidth) + (column * dayWidth / totalColumns);
        const widthPx = (dayWidth / totalColumns) - 8;

        // Update element style
        this._previewElement.style.cssText = `
            position: absolute;
            top: ${topPx}px;
            left: ${leftPx}px;
            width: ${widthPx}px;
            height: ${heightPx}px;
            display: block;
            z-index: 1000;
        `;

        // Update time displays
        const formatTime = (h, m) => {
            const hour12 = h % 12 || 12;
            const ampm = h < 12 ? 'AM' : 'PM';
            const minuteStr = m.toString().padStart(2, '0');
            return `${hour12}:${minuteStr} ${ampm}`;
        };

        const startMinutes = this._currentStartTime.hour * 60 + this._currentStartTime.minute;
        const endMinutes = this._currentEndTime.hour * 60 + this._currentEndTime.minute;
        const duration = endMinutes - startMinutes;
        const durationStr = duration >= 60
            ? `${Math.floor(duration / 60)}h ${duration % 60}m`
            : `${duration}m`;

        const startTimeEl = this._previewElement.querySelector('.resize-preview-time-start');
        const endTimeEl = this._previewElement.querySelector('.resize-preview-time-end');
        const durationEl = this._previewElement.querySelector('.resize-preview-duration');

        if (startTimeEl) startTimeEl.textContent = formatTime(this._currentStartTime.hour, this._currentStartTime.minute);
        if (endTimeEl) endTimeEl.textContent = formatTime(this._currentEndTime.hour, this._currentEndTime.minute);
        if (durationEl) durationEl.textContent = durationStr;

        // Add visual indicator for which edge is being dragged
        this._previewElement.classList.remove('dragging-top', 'dragging-bottom');
        if (this._resizeEdge === 'top') {
            this._previewElement.classList.add('dragging-top');
        } else {
            this._previewElement.classList.add('dragging-bottom');
        }
    },

    /**
     * Find events that overlap with current resize position
     */
    _findOverlappingEvents: function () {
        if (!this._existingEvents || !this._existingEvents.length) return [];

        const startMinutes = this._currentStartTime.hour * 60 + this._currentStartTime.minute;
        const endMinutes = this._currentEndTime.hour * 60 + this._currentEndTime.minute;

        return this._existingEvents.filter(evt => {
            // Skip the item being resized
            if (evt.id === this._itemId) return false;
            // Check same day
            if (evt.dayIndex !== this._dayIndex) return false;
            // Check time overlap
            const evtStart = evt.startHour * 60 + evt.startMinute;
            const evtEnd = evt.endHour * 60 + evt.endMinute;
            return startMinutes < evtEnd && endMinutes > evtStart;
        });
    },

    /**
     * End resize operation and clean up
     */
    _endResize: function () {
        // Restore original element
        if (this._element) {
            this._element.style.opacity = '';
            this._element.style.cursor = '';
        }

        // Hide preview
        if (this._previewElement) {
            this._previewElement.style.display = 'none';
            this._previewElement.classList.remove('active', 'dragging-top', 'dragging-bottom');
        }

        // Remove resizing class
        document.body.classList.remove('resizing');

        // Reset state
        this._isResizing = false;
        this._resizeEdge = null;
        this._element = null;
        this._itemType = null;
        this._itemId = null;
        this._dayIndex = null;
        this._preventDrag = false;
        this._resizeStarted = false;
    },

    /**
     * Cancel resize (called on escape or right-click)
     */
    cancelResize: function () {
        if (!this._isResizing) return;
        this._endResize();
    },

    /**
     * Check if currently resizing
     */
    isResizing: function () {
        return this._isResizing;
    },

    /**
     * Clean up
     */
    dispose: function () {
        document.removeEventListener('mousemove', this._boundGlobalMouseMove);
        document.removeEventListener('mouseup', this._boundGlobalMouseUp);
        document.removeEventListener('dragstart', this._boundPreventDragStart, true);

        if (this._timelineBody) {
            this._timelineBody.removeEventListener('mousemove', this._boundEventMouseMove, true);
            this._timelineBody.removeEventListener('mousedown', this._boundEventMouseDown, true);
            this._timelineBody.removeEventListener('mouseleave', this._boundEventMouseLeave, true);
        }

        if (this._previewElement && this._previewElement.parentNode) {
            this._previewElement.parentNode.removeChild(this._previewElement);
        }

        this._previewElement = null;
        this._timelineBody = null;
        this._config = null;
        this._dotNetRef = null;
        this._existingEvents = [];
        this._preventDrag = false;
        this._lastMouseEvent = null;
    }
};
