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
    }
};
