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

    function normalizeLayout(value) {
        const layout = typeof value === 'string' ? parseJson(value, {}) : (value || {});
        const legacySections = Array.isArray(layout.sections) ? layout.sections : [];
        const panels = Array.isArray(layout.panels) && layout.panels.length
            ? layout.panels
            : [{
                key: 'shortcuts',
                title: '快捷操作',
                description: '常用 Function 的参数化执行面板',
                type: 'shortcuts',
                columns: layout.columns || 2,
                sections: legacySections
            }];
        return {
            ...layout,
            panels: panels.map((panel, index) => ({
                key: panel.key || `panel-${index + 1}`,
                title: panel.title || (index === 0 ? '快捷操作' : `面板 ${index + 1}`),
                description: panel.description || '',
                type: panel.type || 'shortcuts',
                columns: Math.max(1, Math.min(3, Number(panel.columns || layout.columns || 2))),
                sections: Array.isArray(panel.sections) ? panel.sections : []
            }))
        };
    }

    function loopStatus(loopTask) {
        if (!loopTask) return 'none';
        if (loopTask.isRunning) return 'running';
        if (!loopTask.enabled) return 'disabled';
        if (loopTask.lastSucceeded === false) return 'failed';
        if (loopTask.nextRunAt) return 'countdown';
        return 'due';
    }

    function formatCountdown(value, now) {
        if (!value) return '';
        const target = new Date(value).getTime();
        const remaining = target - (now ? new Date(now).getTime() : Date.now());
        if (!Number.isFinite(remaining)) return '';
        if (remaining <= 0) return '即将执行';
        const totalSeconds = Math.ceil(remaining / 1000);
        const days = Math.floor(totalSeconds / 86400);
        const hours = Math.floor((totalSeconds % 86400) / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;
        if (days > 0) return `${days}天 ${hours}小时后`;
        if (hours > 0) return `${hours}小时 ${minutes}分钟后`;
        if (minutes > 0) return `${minutes}分 ${seconds}秒后`;
        return `${seconds}秒后`;
    }

    function loopStatusText(status) {
        return {
            none: '未设置',
            disabled: '已停用',
            running: '执行中',
            countdown: '倒计时',
            due: '待执行',
            failed: '上次失败'
        }[status] || '未知';
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
        normalizeLayout,
        loopStatus,
        formatCountdown,
        loopStatusText,
        sanitizeHtml
    });
})(window);
