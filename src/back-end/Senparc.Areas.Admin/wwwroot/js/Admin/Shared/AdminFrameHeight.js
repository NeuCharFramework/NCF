/*
 * 标签 iframe 里，布局顶栏（60px）和底栏（36px）已经隐藏。
 * 页面里原有的 calc(100vh - Npx) 仍按完整后台扣掉这两段，内容就会比框架短。
 * 这里只补偿这种减法；单独的 100vh（遮罩、全屏层）保持为 iframe 自己的视口。
 */
(function (root) {
    var TAB_CHROME = '96px';
    var LENGTH_PROPERTIES = ['height', 'min-height', 'max-height'];

    function boostTabFrameLength(value) {
        var text = String(value == null ? '' : value);
        if (!text || text.indexOf('100vh') === -1 || text.indexOf('--ncf-tab-chrome') !== -1) {
            return text;
        }
        return text.replace(/calc\(\s*100vh\s*-\s*/gi, 'calc(100vh + var(--ncf-tab-chrome) - ');
    }

    function rewriteStyle(style) {
        if (!style) {
            return 0;
        }
        var changed = 0;
        LENGTH_PROPERTIES.forEach(function (name) {
            var value = style.getPropertyValue(name);
            var next = boostTabFrameLength(value);
            if (!value || next === value) {
                return;
            }
            var priority = style.getPropertyPriority(name);
            style.setProperty(name, next, priority);
            changed += 1;
        });
        return changed;
    }

    function rewriteRules(rules, changed) {
        if (!rules) {
            return changed;
        }
        for (var i = 0; i < rules.length; i++) {
            var rule = rules[i];
            if (rule.type === 1) {
                changed.count += rewriteStyle(rule.style);
            } else if ((rule.type === 4 || rule.type === 12) && rule.cssRules) {
                rewriteRules(rule.cssRules, changed);
            }
        }
        return changed;
    }

    function rewriteSheets(doc) {
        var changed = { count: 0 };
        var sheets;
        try {
            sheets = doc.styleSheets;
        } catch (error) {
            console.debug('[NCF][AdminFrame] 无法读取样式表', error && error.message);
            return changed.count;
        }
        for (var i = 0; i < sheets.length; i++) {
            try {
                rewriteRules(sheets[i].cssRules, changed);
            } catch (error) {
                console.debug('[NCF][AdminFrame] 跳过不可读样式表', error && error.message);
            }
        }
        var nodes = doc.querySelectorAll('[style*="100vh"]');
        for (var n = 0; n < nodes.length; n++) {
            var current = nodes[n].getAttribute('style') || '';
            var next = boostTabFrameLength(current);
            if (next !== current) {
                nodes[n].setAttribute('style', next);
                changed.count += 1;
            }
        }
        return changed.count;
    }

    function patchStyleDeclaration(doc) {
        var proto = doc.defaultView.CSSStyleDeclaration.prototype;
        if (proto.__ncfTabChromePatched) {
            return;
        }
        proto.__ncfTabChromePatched = true;
        try {
            var setProperty = proto.setProperty;
            proto.setProperty = function (name, value, priority) {
                if (typeof value === 'string' && LENGTH_PROPERTIES.indexOf(String(name || '').toLowerCase()) !== -1) {
                    value = boostTabFrameLength(value);
                }
                return setProperty.call(this, name, value, priority);
            };
            ['height', 'minHeight', 'maxHeight'].forEach(function (prop) {
                var descriptor = Object.getOwnPropertyDescriptor(proto, prop);
                if (!descriptor || !descriptor.set || !descriptor.get) {
                    return;
                }
                Object.defineProperty(proto, prop, {
                    configurable: true,
                    enumerable: descriptor.enumerable,
                    get: descriptor.get,
                    set: function (value) {
                        descriptor.set.call(this, typeof value === 'string' ? boostTabFrameLength(value) : value);
                    }
                });
            });
        } catch (error) {
            console.debug('[NCF][AdminFrame] 无法拦截元素高度赋值', error && error.message);
        }
    }

    function install(doc) {
        var params = new URLSearchParams((doc.defaultView.location && doc.defaultView.location.search) || '');
        if (params.get('ncfTab') !== '1' || doc.documentElement.__ncfFrameHeightInstalled) {
            return;
        }
        doc.documentElement.__ncfFrameHeightInstalled = true;
        doc.documentElement.style.setProperty('--ncf-tab-chrome', TAB_CHROME);
        patchStyleDeclaration(doc);
        var apply = function () {
            var count = rewriteSheets(doc);
            console.debug('[NCF][AdminFrame] 已补偿标签页高度', { chrome: TAB_CHROME, rules: count });
        };
        apply();
        doc.addEventListener('DOMContentLoaded', apply);
        if (doc.defaultView.MutationObserver) {
            var observer = new doc.defaultView.MutationObserver(function (mutations) {
                var added = false;
                mutations.forEach(function (mutation) {
                    for (var i = 0; i < mutation.addedNodes.length; i++) {
                        var tag = mutation.addedNodes[i].tagName;
                        if (tag && (tag === 'STYLE' || tag === 'LINK')) {
                            added = true;
                        }
                    }
                });
                if (added) {
                    apply();
                }
            });
            observer.observe(doc.documentElement, { childList: true, subtree: true });
        }
    }

    var api = {
        TAB_CHROME: TAB_CHROME,
        boostTabFrameLength: boostTabFrameLength,
        rewriteStyle: rewriteStyle
    };
    root.NcfAdminFrameHeight = api;
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = api;
    }
    if (root.document && root.document.documentElement) {
        install(root.document);
    }
})(typeof window !== 'undefined' ? window : global);
