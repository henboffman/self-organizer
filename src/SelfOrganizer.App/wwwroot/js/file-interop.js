// Image resizing utility (used by ImageUploader component)
window.resizeImageFromDataUrl = function(dataUrl, maxWidth, maxHeight) {
    return new Promise((resolve) => {
        const img = new Image();

        img.onload = function() {
            try {
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');

                // Calculate new dimensions maintaining aspect ratio
                let width = img.width;
                let height = img.height;

                if (width > height) {
                    if (width > maxWidth) {
                        height = Math.round(height * (maxWidth / width));
                        width = maxWidth;
                    }
                } else {
                    if (height > maxHeight) {
                        width = Math.round(width * (maxHeight / height));
                        height = maxHeight;
                    }
                }

                // Square output for icons
                canvas.width = maxWidth;
                canvas.height = maxHeight;
                ctx.clearRect(0, 0, maxWidth, maxHeight);

                // Center the image
                const offsetX = (maxWidth - width) / 2;
                const offsetY = (maxHeight - height) / 2;
                ctx.drawImage(img, offsetX, offsetY, width, height);

                const resizedDataUrl = canvas.toDataURL('image/png', 0.9);
                resolve({ success: true, dataUrl: resizedDataUrl });
            } catch (error) {
                resolve({ success: false, error: error.message });
            }
        };

        img.onerror = function() {
            resolve({ success: false, error: 'Failed to load image' });
        };

        img.src = dataUrl;
    });
};

// File download and upload utilities for export/import functionality
window.fileInterop = {
    /**
     * Download text content as a file
     * @param {string} filename - Name of the file to download
     * @param {string} contentType - MIME type (e.g., 'text/csv', 'application/json')
     * @param {string} textContent - The text content to download
     */
    downloadText: function (filename, contentType, textContent) {
        try {
            const blob = new Blob([textContent], { type: contentType + ';charset=utf-8' });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
            return { success: true };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    /**
     * Download base64 encoded content as a file
     * @param {string} filename - Name of the file to download
     * @param {string} contentType - MIME type
     * @param {string} base64Content - Base64 encoded content
     */
    downloadBase64: function (filename, contentType, base64Content) {
        try {
            const byteCharacters = atob(base64Content);
            const byteNumbers = new Array(byteCharacters.length);
            for (let i = 0; i < byteCharacters.length; i++) {
                byteNumbers[i] = byteCharacters.charCodeAt(i);
            }
            const byteArray = new Uint8Array(byteNumbers);
            const blob = new Blob([byteArray], { type: contentType });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
            return { success: true };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    /**
     * Read a file as text from an InputFile element
     * @param {HTMLInputElement} inputElement - The file input element
     * @returns {Promise<object>} - Result with content or error
     */
    readFileAsText: function (inputElement) {
        return new Promise((resolve) => {
            if (!inputElement || !inputElement.files || inputElement.files.length === 0) {
                resolve({ success: false, error: 'No file selected' });
                return;
            }

            const file = inputElement.files[0];
            const reader = new FileReader();

            reader.onload = function (e) {
                resolve({
                    success: true,
                    content: e.target.result,
                    filename: file.name,
                    size: file.size,
                    type: file.type
                });
            };

            reader.onerror = function () {
                resolve({
                    success: false,
                    error: 'Failed to read file: ' + reader.error?.message || 'Unknown error'
                });
            };

            reader.readAsText(file);
        });
    },

    /**
     * Get file info without reading content
     * @param {HTMLInputElement} inputElement - The file input element
     * @returns {object} - File information
     */
    getFileInfo: function (inputElement) {
        if (!inputElement || !inputElement.files || inputElement.files.length === 0) {
            return { hasFile: false };
        }

        const file = inputElement.files[0];
        return {
            hasFile: true,
            filename: file.name,
            size: file.size,
            type: file.type,
            extension: file.name.split('.').pop()?.toLowerCase() || ''
        };
    },

    /**
     * Detect file format from content
     * @param {string} content - File content
     * @returns {string} - Detected format: 'json', 'csv', or 'unknown'
     */
    detectFormat: function (content) {
        if (!content || typeof content !== 'string') {
            return 'unknown';
        }

        const trimmed = content.trim();

        // Check for JSON
        if ((trimmed.startsWith('{') && trimmed.endsWith('}')) ||
            (trimmed.startsWith('[') && trimmed.endsWith(']'))) {
            try {
                JSON.parse(trimmed);
                return 'json';
            } catch {
                // Not valid JSON, continue checking
            }
        }

        // Check for CSV (has commas and newlines, or starts with header-like content)
        if (trimmed.includes(',') && (trimmed.includes('\n') || trimmed.includes('\r'))) {
            return 'csv';
        }

        // Single line with commas could still be CSV header
        if (trimmed.includes(',') && !trimmed.includes('{')) {
            return 'csv';
        }

        return 'unknown';
    },

    /**
     * Read an image file and resize it to specified dimensions
     * @param {HTMLInputElement} inputElement - The file input element
     * @param {number} maxWidth - Maximum width in pixels (default 64)
     * @param {number} maxHeight - Maximum height in pixels (default 64)
     * @returns {Promise<object>} - Result with base64 data URL or error
     */
    readImageAsBase64: function (inputElement, maxWidth = 64, maxHeight = 64) {
        return new Promise((resolve) => {
            if (!inputElement || !inputElement.files || inputElement.files.length === 0) {
                resolve({ success: false, error: 'No file selected' });
                return;
            }

            const file = inputElement.files[0];
            const maxSizeBytes = 1024 * 1024; // 1MB

            // Validate file type
            const validTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/svg+xml'];
            if (!validTypes.includes(file.type)) {
                resolve({
                    success: false,
                    error: 'Invalid file type. Please select a JPEG, PNG, GIF, WebP, or SVG image.'
                });
                return;
            }

            // Validate file size
            if (file.size > maxSizeBytes) {
                resolve({
                    success: false,
                    error: 'File too large. Maximum size is 1MB.'
                });
                return;
            }

            const reader = new FileReader();

            reader.onload = function (e) {
                const img = new Image();

                img.onload = function () {
                    try {
                        // Create canvas for resizing
                        const canvas = document.createElement('canvas');
                        const ctx = canvas.getContext('2d');

                        // Calculate new dimensions maintaining aspect ratio
                        let width = img.width;
                        let height = img.height;

                        if (width > height) {
                            if (width > maxWidth) {
                                height = Math.round(height * (maxWidth / width));
                                width = maxWidth;
                            }
                        } else {
                            if (height > maxHeight) {
                                width = Math.round(width * (maxHeight / height));
                                height = maxHeight;
                            }
                        }

                        // For icons, we want square output centered
                        canvas.width = maxWidth;
                        canvas.height = maxHeight;

                        // Fill with transparent background
                        ctx.clearRect(0, 0, maxWidth, maxHeight);

                        // Center the image
                        const offsetX = (maxWidth - width) / 2;
                        const offsetY = (maxHeight - height) / 2;

                        // Draw resized image
                        ctx.drawImage(img, offsetX, offsetY, width, height);

                        // Get base64 data URL (PNG for transparency support)
                        const dataUrl = canvas.toDataURL('image/png', 0.9);

                        resolve({
                            success: true,
                            dataUrl: dataUrl,
                            originalWidth: img.width,
                            originalHeight: img.height,
                            resizedWidth: maxWidth,
                            resizedHeight: maxHeight,
                            filename: file.name,
                            type: file.type
                        });
                    } catch (error) {
                        resolve({
                            success: false,
                            error: 'Failed to process image: ' + error.message
                        });
                    }
                };

                img.onerror = function () {
                    resolve({
                        success: false,
                        error: 'Failed to load image. The file may be corrupted.'
                    });
                };

                img.src = e.target.result;
            };

            reader.onerror = function () {
                resolve({
                    success: false,
                    error: 'Failed to read file: ' + (reader.error?.message || 'Unknown error')
                });
            };

            reader.readAsDataURL(file);
        });
    },

    /**
     * Create a preview URL for a file without processing
     * @param {HTMLInputElement} inputElement - The file input element
     * @returns {Promise<object>} - Result with preview URL or error
     */
    getImagePreviewUrl: function (inputElement) {
        return new Promise((resolve) => {
            if (!inputElement || !inputElement.files || inputElement.files.length === 0) {
                resolve({ success: false, error: 'No file selected' });
                return;
            }

            const file = inputElement.files[0];
            const validTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/svg+xml'];

            if (!validTypes.includes(file.type)) {
                resolve({
                    success: false,
                    error: 'Invalid file type. Please select an image file.'
                });
                return;
            }

            try {
                const url = URL.createObjectURL(file);
                resolve({
                    success: true,
                    previewUrl: url,
                    filename: file.name,
                    size: file.size,
                    type: file.type
                });
            } catch (error) {
                resolve({
                    success: false,
                    error: 'Failed to create preview: ' + error.message
                });
            }
        });
    },

    /**
     * Revoke a preview URL to free memory
     * @param {string} url - The preview URL to revoke
     */
    revokePreviewUrl: function (url) {
        if (url && url.startsWith('blob:')) {
            URL.revokeObjectURL(url);
        }
    }
};
