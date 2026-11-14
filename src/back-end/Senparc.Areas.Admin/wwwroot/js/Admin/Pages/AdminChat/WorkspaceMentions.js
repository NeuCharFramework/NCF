(function (window) {
    'use strict';

    function findActive(value, caret) {
        const text = String(value || '');
        if (!Number.isInteger(caret) || caret < 0 || caret > text.length) return null;
        const before = text.slice(0, caret);
        const at = before.lastIndexOf('@');
        if (at < 0) return null;
        if (at > 0 && !/\s/.test(before.charAt(at - 1))) return null;
        const query = before.slice(at + 1);
        if (/\s/.test(query) || query.length > 200) return null;
        return { start: at, end: caret, query: query };
    }

    function apply(value, mention, item) {
        const text = String(value || '');
        const path = String(item && item.relativePath || '').replace(/\\/g, '/').replace(/^\/+/, '');
        if (!mention || !path || mention.start < 0 || mention.end < mention.start || mention.end > text.length) return null;
        const insertion = '@' + path + (item.isDirectory ? '/' : ' ');
        return {
            value: text.slice(0, mention.start) + insertion + text.slice(mention.end),
            caret: mention.start + insertion.length
        };
    }

    window.NcfWorkspaceMentions = { findActive: findActive, apply: apply };
})(window);
