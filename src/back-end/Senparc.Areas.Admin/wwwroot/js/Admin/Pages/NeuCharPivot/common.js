(function (global) {
    'use strict';

    function parseJson(value, fallback) {
        try {
            return value ? JSON.parse(value) : fallback;
        } catch (error) {
            console.warn('NeuCharPivot JSON parse failed:', error);
            return fallback;
        }
    }

    function unwrap(response) {
        if (!response || !response.data) {
            return null;
        }
        return Object.prototype.hasOwnProperty.call(response.data, 'data')
            ? response.data.data
            : response.data;
    }

    function createParameterValues(fn) {
        const schema = parseJson(fn.parameterSchemaJson, []);
        const defaults = parseJson(fn.defaultParametersJson, {});
        const values = {};
        schema.forEach(parameter => {
            let value = Object.prototype.hasOwnProperty.call(defaults, parameter.name)
                ? defaults[parameter.name]
                : parameter.defaultValue;
            if (parameter.parameterType === 2 && !Array.isArray(value)) {
                value = typeof value === 'string'
                    ? value.split(/[;,，；\n\r|]+/).map(item => item.trim()).filter(Boolean)
                    : [];
            } else if (parameter.parameterType === 4) {
                value = value === true || value === 'true' || value === 'True';
            } else if (value === null || typeof value === 'undefined') {
                value = '';
            }
            values[parameter.name] = value;
        });
        return values;
    }

    function firstMissingRequired(fn, values) {
        return parseJson(fn.parameterSchemaJson, []).find(parameter => {
            if (!parameter.required) {
                return false;
            }
            const value = values[parameter.name];
            return value === null || typeof value === 'undefined' || value === '' ||
                (Array.isArray(value) && value.length === 0);
        });
    }

    function sanitizeHtml(value) {
        if (!global.DOMPurify || typeof global.DOMPurify.sanitize !== 'function') {
            return '';
        }

        return global.DOMPurify.sanitize(String(value == null ? '' : value), {
            ALLOWED_TAGS: [
                'a', 'b', 'blockquote', 'br', 'code', 'div', 'em',
                'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'hr', 'i',
                'li', 'mark', 'ol', 'p', 'pre', 'small', 'span',
                'strong', 'sub', 'sup', 'table', 'tbody', 'td',
                'th', 'thead', 'tr', 'u', 'ul'
            ],
            ALLOWED_ATTR: ['href', 'title'],
            ALLOW_ARIA_ATTR: false,
            ALLOW_DATA_ATTR: false,
            ALLOWED_URI_REGEXP: /^https?:\/\/[^\s<>"']+$/i
        });
    }

    global.NeuCharPivotUi = Object.freeze({
        parseJson,
        unwrap,
        createParameterValues,
        firstMissingRequired,
        sanitizeHtml
    });
})(window);
