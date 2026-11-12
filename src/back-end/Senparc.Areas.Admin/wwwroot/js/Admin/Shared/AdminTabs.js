// 后台顶部标签页。
// 外壳固定为 /Admin/Index?ncfShell=1，左侧菜单打开的页面放进 iframe（embedded=1）。
// 当前页面写在锚点里，复制后可直接打开。页面自己的查询和锚点接在后面，例如：
// /Admin/Index?ncfShell=1#/Admin/AdminChat/Chat?sessionId=15&mode=1#session-15
// 同一个地址只保留一个标签；再次打开时切回该标签，不重新加载。
(function (root) {
    'use strict';

    var MAX_ADMIN_TABS = 16;
    var STORAGE_KEY = 'ncf.admin.pageTabs';
    var STRIPPED_PARAMS = { embedded: true, ncfShell: true, ncfOpen: true, ncfTab: true };

    function textOf(name, fallback) {
        var bag = root.NCF_ADMIN_TABS_TEXT || {};
        return bag[name] || fallback;
    }

    function parseAdminUrl(input, baseHref) {
        if (input === undefined || input === null) {
            return null;
        }
        var raw = String(input).trim();
        if (!raw || raw.charAt(0) === '#' || /^\s*javascript:/i.test(raw)) {
            return null;
        }

        var base = baseHref || 'http://localhost/Admin/Index';
        var url;
        try {
            url = new URL(raw, base);
        } catch (error) {
            return null;
        }
        var baseUrl;
        try {
            baseUrl = new URL(base);
        } catch (error) {
            return null;
        }
        if (url.origin !== baseUrl.origin) {
            return null;
        }

        var path = String(url.pathname || '').replace(/\/+$/, '') || '/';
        if (/^\/admin$/i.test(path)) {
            path = '/Admin/Index';
        }
        if (!/^\/admin(\/|$)/i.test(path)) {
            return null;
        }
        if (/^\/admin\/login(\/|$)/i.test(path)) {
            return null;
        }

        var pairs = [];
        var hasHandler = false;
        url.searchParams.forEach(function (value, key) {
            if (Object.prototype.hasOwnProperty.call(STRIPPED_PARAMS, key)) {
                return;
            }
            if (String(key).toLowerCase() === 'handler') {
                hasHandler = true;
            }
            pairs.push([String(key), String(value)]);
        });
        pairs.sort(function (left, right) {
            if (left[0] === right[0]) {
                return left[1] < right[1] ? -1 : left[1] > right[1] ? 1 : 0;
            }
            return left[0] < right[0] ? -1 : 1;
        });

        var query = pairs.map(function (pair) {
            return encodeURIComponent(pair[0]) + '=' + encodeURIComponent(pair[1]);
        }).join('&');
        var hash = url.hash || '';
        var canonical = path + (query ? '?' + query : '') + hash;
        var framePairs = pairs.concat([['embedded', '1'], ['ncfTab', '1']]);
        framePairs.sort(function (left, right) {
            if (left[0] === right[0]) {
                return left[1] < right[1] ? -1 : left[1] > right[1] ? 1 : 0;
            }
            return left[0] < right[0] ? -1 : 1;
        });
        var frameQuery = framePairs.map(function (pair) {
            return encodeURIComponent(pair[0]) + '=' + encodeURIComponent(pair[1]);
        }).join('&');

        return {
            id: (path + (query ? '?' + query : '')).toLowerCase(),
            url: canonical,
            frameUrl: path + '?' + frameQuery + hash,
            path: path,
            hijack: !hasHandler
        };
    }

    function titleFromPath(parsed, untitled) {
        var parts = String(parsed && parsed.path || '').split('/').filter(Boolean);
        if (!parts.length) {
            return untitled;
        }
        try {
            return decodeURIComponent(parts[parts.length - 1]);
        } catch (error) {
            return parts[parts.length - 1];
        }
    }

    function cloneState(state) {
        return {
            enabled: !!state.enabled,
            shellId: state.shellId,
            activeId: state.activeId,
            tabs: (state.tabs || []).map(function (tab) {
                return {
                    id: tab.id,
                    url: tab.url,
                    address: tab.address || tab.url,
                    frameUrl: tab.frameUrl,
                    title: tab.title,
                    menuIndex: tab.menuIndex || null,
                    isShell: !!tab.isShell,
                    loaded: !!tab.loaded,
                    openedAt: tab.openedAt || 0
                };
            })
        };
    }

    function trimTabs(tabs, activeId, shellId) {
        var copy = tabs.slice();
        while (copy.length > MAX_ADMIN_TABS) {
            var index = -1;
            var oldest = Infinity;
            copy.forEach(function (tab, tabIndex) {
                if (tab.id === activeId || tab.id === shellId || tab.isShell) {
                    return;
                }
                var openedAt = tab.openedAt || 0;
                if (openedAt < oldest) {
                    oldest = openedAt;
                    index = tabIndex;
                }
            });
            if (index < 0) {
                break;
            }
            copy.splice(index, 1);
        }
        return copy;
    }

    function createTab(parsed, options) {
        var untitled = options.untitled || 'Untitled';
        var isShell = parsed.id === options.shellId;
        return {
            id: parsed.id,
            url: parsed.url,
            address: parsed.url,
            frameUrl: parsed.frameUrl,
            title: isShell ? (options.homeTitle || untitled) : (options.title || titleFromPath(parsed, untitled)),
            menuIndex: options.menuIndex ? String(options.menuIndex) : null,
            isShell: isShell,
            loaded: !isShell && !!options.loaded,
            openedAt: options.openedAt || 0
        };
    }

    function pinHomeTab(tabs) {
        var home = null;
        var rest = [];
        (tabs || []).forEach(function (tab) {
            if (tab.isShell && !home) {
                home = tab;
            } else {
                rest.push(tab);
            }
        });
        return home ? [home].concat(rest) : (tabs || []);
    }

    function createInitialState(options) {
        var settings = options || {};
        var shell = parseAdminUrl(settings.shellHref, settings.shellHref);
        if (!shell || !shell.hijack) {
            return { enabled: false, shellId: '', activeId: '', tabs: [] };
        }

        var untitled = settings.untitledTitle || 'Untitled';
        var homeTitle = settings.homeTitle || 'Home';
        var tabs = [];
        var seen = {};

        function pushTab(tab) {
            if (!tab || seen[tab.id]) {
                return;
            }
            seen[tab.id] = true;
            tabs.push(tab);
        }

        (settings.storedTabs || []).forEach(function (item) {
            var parsed = parseAdminUrl(item && item.url, settings.shellHref);
            if (!parsed || !parsed.hijack) {
                return;
            }
            var restored = createTab(parsed, {
                shellId: shell.id,
                title: item.title,
                menuIndex: item.menuIndex,
                untitled: untitled,
                homeTitle: homeTitle,
                openedAt: item.openedAt || 0,
                loaded: false
            });
            var storedAddress = parseAdminUrl(item.address, settings.shellHref);
            if (storedAddress && storedAddress.hijack && !restored.isShell) {
                restored.address = storedAddress.url;
                restored.frameUrl = storedAddress.frameUrl;
            }
            pushTab(restored);
        });

        if (!seen[shell.id]) {
            tabs.unshift(createTab(shell, {
                shellId: shell.id,
                title: homeTitle,
                untitled: untitled,
                homeTitle: homeTitle,
                openedAt: 0,
                loaded: false
            }));
            seen[shell.id] = true;
        }

        tabs.forEach(function (tab) {
            tab.isShell = tab.id === shell.id;
            if (tab.isShell) {
                tab.loaded = false;
                tab.title = homeTitle;
            }
        });
        tabs = pinHomeTab(tabs);

        var activeId = shell.id;
        var opened = settings.openUrl ? parseAdminUrl(settings.openUrl, settings.shellHref) : null;
        if (opened && opened.hijack) {
            var exact = null;
            var samePath = [];
            tabs.forEach(function (tab) {
                if (tab.id === opened.id) {
                    exact = tab;
                }
                if (!tab.isShell && tabPath(tab).toLowerCase() === opened.path.toLowerCase()) {
                    samePath.push(tab);
                }
            });
            var adopted = exact || (samePath.length === 1 ? samePath[0] : null);
            if (adopted) {
                adopted.address = opened.url;
                if (!adopted.isShell && !adopted.loaded) {
                    adopted.frameUrl = opened.frameUrl;
                }
                activeId = adopted.id;
            } else if (!seen[opened.id]) {
                pushTab(createTab(opened, {
                    shellId: shell.id,
                    untitled: untitled,
                    homeTitle: homeTitle,
                    openedAt: settings.now || 0,
                    loaded: opened.id !== shell.id
                }));
                activeId = opened.id;
            } else {
                activeId = opened.id;
            }
        } else if (settings.activeId && seen[settings.activeId]) {
            activeId = settings.activeId;
        }

        tabs.forEach(function (tab) {
            if (tab.id === activeId && !tab.isShell) {
                tab.loaded = true;
            }
        });

        return {
            enabled: true,
            shellId: shell.id,
            activeId: activeId,
            tabs: pinHomeTab(trimTabs(tabs, activeId, shell.id))
        };
    }

    function openTab(state, rawUrl, title, menuIndex, now, baseHref) {
        var current = cloneState(state || { enabled: true, shellId: '', activeId: '', tabs: [] });
        var parsed = parseAdminUrl(rawUrl, baseHref || 'http://localhost/Admin/Index');
        if (!parsed || !parsed.hijack) {
            return { state: current, handled: false, tab: null };
        }

        var existing = null;
        current.tabs.forEach(function (tab) {
            if (tab.id === parsed.id) {
                existing = tab;
            }
        });
        if (existing) {
            if (title && !existing.isShell) {
                existing.title = title;
            }
            if (menuIndex !== undefined && menuIndex !== null && menuIndex !== '') {
                existing.menuIndex = String(menuIndex);
            }
            if (!existing.isShell) {
                existing.loaded = true;
            }
            current.activeId = existing.id;
            return { state: current, handled: true, tab: existing };
        }

        var created = createTab(parsed, {
            shellId: current.shellId,
            title: title,
            menuIndex: menuIndex,
            untitled: textOf('untitled', 'Untitled'),
            homeTitle: textOf('home', 'Home'),
            openedAt: now || 0,
            loaded: true
        });
        current.tabs.push(created);
        current.activeId = created.id;
        current.tabs = pinHomeTab(trimTabs(current.tabs, current.activeId, current.shellId));
        var active = null;
        current.tabs.forEach(function (tab) {
            if (tab.id === current.activeId) {
                active = tab;
            }
        });
        return { state: current, handled: !!active, tab: active };
    }

    function activateTab(state, id) {
        var current = cloneState(state);
        var found = null;
        current.tabs.forEach(function (tab) {
            if (tab.id === id) {
                found = tab;
                if (!tab.isShell) {
                    tab.loaded = true;
                }
            }
        });
        if (!found) {
            return current;
        }
        current.activeId = found.id;
        return current;
    }

    function closeTab(state, id) {
        var current = cloneState(state);
        if (current.tabs.length <= 1) {
            return current;
        }
        var index = -1;
        current.tabs.forEach(function (tab, tabIndex) {
            if (tab.id === id) {
                index = tabIndex;
            }
        });
        if (index < 0 || current.tabs[index].isShell) {
            return current;
        }
        var closingActive = current.activeId === id;
        current.tabs.splice(index, 1);
        if (closingActive) {
            var neighbor = current.tabs[index] || current.tabs[index - 1] || current.tabs[0];
            current.activeId = neighbor.id;
            current.tabs.forEach(function (tab) {
                if (tab.id === current.activeId && !tab.isShell) {
                    tab.loaded = true;
                }
            });
        }
        return current;
    }

    function eachMenu(list, visit) {
        (list || []).forEach(function (item) {
            visit(item);
            if (item && item.children && item.children.length) {
                eachMenu(item.children, visit);
            }
        });
    }

    function applyMenuTitles(state, menuList, baseHref) {
        var current = cloneState(state);
        var byUrl = {};
        eachMenu(menuList, function (item) {
            if (!item || !item.url) {
                return;
            }
            var parsed = parseAdminUrl(item.url, baseHref || 'http://localhost/Admin/Index');
            if (!parsed || !parsed.hijack) {
                return;
            }
            byUrl[parsed.id] = item;
        });
        current.tabs.forEach(function (tab) {
            var item = byUrl[tab.id];
            if (!item) {
                return;
            }
            if (item.menuName && !tab.isShell) {
                tab.title = item.menuName;
            }
            if (item.id !== undefined && item.id !== null) {
                tab.menuIndex = String(item.id);
            }
        });
        var active = null;
        current.tabs.forEach(function (tab) {
            if (tab.id === current.activeId) {
                active = tab;
            }
        });
        return { state: current, activeTab: active };
    }

    function tabPath(tab) {
        var source = tab && (tab.address || tab.url);
        if (!source) {
            return '';
        }
        var parsed = parseAdminUrl(source, 'http://localhost' + source);
        return parsed && parsed.path ? parsed.path : '';
    }

    function shellPageHash(tab) {
        if (!tab || tab.isShell) {
            return '';
        }
        var url = String(tab.address || tab.url || '');
        if (url.charAt(0) !== '/') {
            return '';
        }
        return '#' + url;
    }

    // iframe 已经自己完成跳转时，只记录真实地址，不改 iframe 的 src，避免整页重载。
    function rememberTabAddress(state, liveHref, baseHref) {
        var parsed = parseAdminUrl(liveHref, baseHref || liveHref);
        var current = cloneState(state || { enabled: true, shellId: '', activeId: '', tabs: [] });
        if (!parsed || !parsed.hijack) {
            return { state: current, changed: false, tab: null };
        }
        var target = null;
        current.tabs.forEach(function (tab) {
            if (tab.id === parsed.id) {
                target = tab;
            }
        });
        if (!target) {
            var samePath = [];
            current.tabs.forEach(function (tab) {
                if (!tab.isShell && tabPath(tab).toLowerCase() === parsed.path.toLowerCase()) {
                    samePath.push(tab);
                }
            });
            if (samePath.length === 1) {
                target = samePath[0];
            }
        }
        if (!target || target.isShell || (target.address || target.url) === parsed.url) {
            return { state: current, changed: false, tab: target };
        }
        target.address = parsed.url;
        return { state: current, changed: true, tab: target };
    }

    function shellAddress(tab) {
        return '/Admin/Index?ncfShell=1' + shellPageHash(tab);
    }

    // 锚点只接受后台页面。#session-15 这类页面内部锚点、登录地址和其他片段都留给原页面。
    function pageUrlFromLocationHash(hash) {
        var value = String(hash || '');
        if (value.charAt(0) === '#') {
            value = value.slice(1);
        }
        if (!value) {
            return '';
        }
        try {
            value = decodeURIComponent(value);
        } catch (error) {
            console.debug('[NCF][AdminTabs] 锚点无法解码', error && error.message);
        }
        if (value.charAt(0) !== '/') {
            return '';
        }
        var path = value.split('?')[0].split('#')[0];
        if (!/^\/admin(\/|$)/i.test(path) || /^\/admin\/login(\/|$)/i.test(path)) {
            return '';
        }
        return value;
    }

    function syncShellLocation(state) {
        if (!root.history || !root.location || typeof root.history.replaceState !== 'function') {
            return;
        }
        var active = null;
        (state && state.tabs || []).forEach(function (tab) {
            if (tab.id === state.activeId) {
                active = tab;
            }
        });
        var next = root.location.pathname + '?ncfShell=1' + shellPageHash(active);
        var current = root.location.pathname + root.location.search + root.location.hash;
        if (next === current) {
            return;
        }
        root.history.replaceState(root.history.state, '', next);
        console.debug('[NCF][AdminTabs] 页面锚点', shellPageHash(active) || '(首页)');
    }

    function readStorage() {
        try {
            var parsed = JSON.parse(root.sessionStorage.getItem(STORAGE_KEY) || '');
            return {
                activeId: parsed && parsed.activeId || '',
                tabs: parsed && Array.isArray(parsed.tabs) ? parsed.tabs : []
            };
        } catch (error) {
            return { activeId: '', tabs: [] };
        }
    }

    function writeStorage(state) {
        try {
            root.sessionStorage.setItem(STORAGE_KEY, JSON.stringify({
                activeId: state.activeId,
                tabs: (state.tabs || []).map(function (tab) {
                    return {
                        id: tab.id,
                        url: tab.url,
                        address: tab.address || tab.url,
                        title: tab.title,
                        menuIndex: tab.menuIndex,
                        openedAt: tab.openedAt
                    };
                })
            }));
        } catch (error) {
            console.debug('[NCF][AdminTabs] 会话存储不可用', error && error.message);
        }
    }

    function commitTabs(next) {
        if (!root.Store || !root.Store.state || !root.Store.state.adminTabs) {
            return;
        }
        var current = root.Store.state.adminTabs;
        current.enabled = !!next.enabled;
        current.shellId = next.shellId || '';
        current.activeId = next.activeId || '';
        var ordered = [];
        (next.tabs || []).forEach(function (tab) {
            var found = null;
            current.tabs.forEach(function (item) {
                if (item.id === tab.id) {
                    found = item;
                }
            });
            if (!found) {
                ordered.push({
                    id: tab.id,
                    url: tab.url,
                    address: tab.address || tab.url,
                    frameUrl: tab.frameUrl,
                    title: tab.title,
                    menuIndex: tab.menuIndex || null,
                    isShell: !!tab.isShell,
                    loaded: !!tab.loaded,
                    openedAt: tab.openedAt || 0
                });
                return;
            }
            found.url = tab.url;
            found.address = tab.address || found.address || tab.url;
            found.frameUrl = tab.frameUrl;
            found.title = tab.title;
            found.menuIndex = tab.menuIndex || null;
            found.isShell = !!tab.isShell;
            found.loaded = !!found.loaded || !!tab.loaded;
            found.openedAt = tab.openedAt || found.openedAt || 0;
            ordered.push(found);
        });
        current.tabs.splice(0, current.tabs.length);
        ordered.forEach(function (tab) {
            current.tabs.push(tab);
        });
    }

    function snapshot() {
        var current = root.Store.state.adminTabs;
        return cloneState(current);
    }

    function highlightMenu(menuIndex) {
        if (menuIndex === undefined || menuIndex === null || menuIndex === '') {
            return;
        }
        var key = String(menuIndex);
        try {
            root.sessionStorage.setItem('activeMenu', key);
        } catch (error) {
            console.debug('[NCF][AdminTabs] 无法记录当前菜单', error && error.message);
        }
        if (root.Store && root.Store.state && root.Store.state.navMenu) {
            root.Store.state.navMenu.activeMenu = key;
        }
        var nodes = root.document ? root.document.querySelectorAll('.el-aside-index .el-menu') : [];
        for (var i = 0; i < nodes.length; i++) {
            if (nodes[i].__vue__ && nodes[i].__vue__.activeIndex !== undefined) {
                nodes[i].__vue__.activeIndex = key;
            }
        }
    }

    function tabScrollState(scrollLeft, scrollWidth, clientWidth) {
        var max = Math.max(0, Number(scrollWidth) - Number(clientWidth));
        var left = Number(scrollLeft) || 0;
        return {
            canScrollLeft: left > 2,
            canScrollRight: max - left > 2
        };
    }

    function updateTabScrollState() {
        if (!root.document || !root.Store || !root.Store.state || !root.Store.state.adminTabs) {
            return;
        }
        var scroller = root.document.querySelector('.admin-page-tabs-scroller');
        var next = scroller
            ? tabScrollState(scroller.scrollLeft, scroller.scrollWidth, scroller.clientWidth)
            : { canScrollLeft: false, canScrollRight: false };
        root.Store.state.adminTabs.canScrollLeft = next.canScrollLeft;
        root.Store.state.adminTabs.canScrollRight = next.canScrollRight;
    }

    function bindTabScroller() {
        if (!root.document || root.document._ncfAdminTabScrollerBound) {
            updateTabScrollState();
            return;
        }
        var scroller = root.document.querySelector('.admin-page-tabs-scroller');
        if (!scroller) {
            return;
        }
        root.document._ncfAdminTabScrollerBound = true;
        scroller.addEventListener('scroll', updateTabScrollState, { passive: true });
        scroller.addEventListener('wheel', function (event) {
            if (scroller.scrollWidth <= scroller.clientWidth + 2) {
                return;
            }
            var delta = Math.abs(event.deltaX) > Math.abs(event.deltaY) ? event.deltaX : event.deltaY;
            if (!delta) {
                return;
            }
            scroller.scrollLeft += delta;
            event.preventDefault();
        }, { passive: false });
        root.addEventListener('resize', updateTabScrollState);
        if (typeof root.ResizeObserver === 'function') {
            var observer = new root.ResizeObserver(updateTabScrollState);
            observer.observe(scroller);
        }
        updateTabScrollState();
        console.debug('[NCF][AdminTabs] 标签滚动已就绪');
    }

    function scrollActiveTabIntoView() {
        if (!root.Vue || !root.document) {
            return;
        }
        root.Vue.nextTick(function () {
            bindTabScroller();
            var scroller = root.document.querySelector('.admin-page-tabs-scroller');
            var active = root.document.querySelector('.admin-page-tab.is-active');
            if (scroller && active) {
                var left = active.offsetLeft;
                var right = left + active.offsetWidth;
                if (left < scroller.scrollLeft) {
                    scroller.scrollLeft = Math.max(0, left - 8);
                } else if (right > scroller.scrollLeft + scroller.clientWidth) {
                    scroller.scrollLeft = right - scroller.clientWidth + 8;
                }
            }
            updateTabScrollState();
        });
    }

    function scrollAdminTabs(direction) {
        if (!root.document) {
            return;
        }
        var scroller = root.document.querySelector('.admin-page-tabs-scroller');
        if (!scroller) {
            return;
        }
        var amount = Math.max(160, Math.round(scroller.clientWidth * 0.7));
        var left = scroller.scrollLeft + (direction < 0 ? -amount : amount);
        if (scroller.scrollBy) {
            scroller.scrollBy({ left: direction < 0 ? -amount : amount, behavior: 'smooth' });
        } else {
            scroller.scrollLeft = left;
        }
        console.debug('[NCF][AdminTabs] 滚动标签', direction < 0 ? 'left' : 'right');
    }

    function syncDocumentTitle(state) {
        if (!root.document) {
            return;
        }
        var active = null;
        (state.tabs || []).forEach(function (tab) {
            if (tab.id === state.activeId) {
                active = tab;
            }
        });
        if (active && active.title) {
            root.document.title = active.title;
        }
    }

    function publish(next, reason) {
        commitTabs(next);
        writeStorage(next);
        syncDocumentTitle(next);
        var active = null;
        (next.tabs || []).forEach(function (tab) {
            if (tab.id === next.activeId) {
                active = tab;
            }
        });
        if (active) {
            highlightMenu(active.menuIndex);
        }
        scrollActiveTabIntoView();
        syncShellLocation(next);
        console.debug('[NCF][AdminTabs]', reason, active && active.url, 'tabs', (next.tabs || []).length);
        return active;
    }

    function installFrameLocationMirror() {
        if (!root.history || root.top === root || root.history._ncfTabMirror) {
            return;
        }
        root.history._ncfTabMirror = true;
        var replaceState = root.history.replaceState.bind(root.history);
        var pushState = root.history.pushState.bind(root.history);
        function reportFrameLocation() {
            try {
                if (root.parent && root.parent !== root && typeof root.parent.ncfReportAdminTabLocation === 'function') {
                    root.parent.ncfReportAdminTabLocation(root.location.href);
                }
            } catch (error) {
                console.debug('[NCF][AdminTabs] 无法同步 iframe 地址', error && error.message);
            }
        }
        root.history.replaceState = function () {
            var result = replaceState.apply(root.history, arguments);
            reportFrameLocation();
            return result;
        };
        root.history.pushState = function () {
            var result = pushState.apply(root.history, arguments);
            reportFrameLocation();
            return result;
        };
        root.addEventListener('hashchange', reportFrameLocation);
    }

    function installPageBridge() {
        root.ncfOpenAdminPage = function (url, title, menuIndex) {
            try {
                if (root.parent && root.parent !== root && typeof root.parent.ncfOpenAdminPage === 'function') {
                    return root.parent.ncfOpenAdminPage(url, title, menuIndex);
                }
            } catch (error) {
                console.debug('[NCF][AdminTabs] 无法通知外壳打开标签', error && error.message);
            }
            return false;
        };

        if (!root.document || root.top === root) {
            return;
        }
        installFrameLocationMirror();
        root.document.addEventListener('click', function (event) {
            if (event.defaultPrevented || event.button !== 0) {
                return;
            }
            if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
                return;
            }
            var target = event.target;
            var anchor = target && target.closest ? target.closest('a[href]') : null;
            if (!anchor || anchor.hasAttribute('download') || (anchor.target && anchor.target !== '_self')) {
                return;
            }
            var href = anchor.href || anchor.getAttribute('href');
            var parentWindow = root.parent;
            if (!parentWindow || typeof parentWindow.ncfHasAdminTab !== 'function' || !parentWindow.ncfHasAdminTab(href)) {
                return;
            }
            var label = (anchor.getAttribute('data-ncf-tab-title') || anchor.textContent || '').replace(/\s+/g, ' ').trim();
            if (typeof parentWindow.ncfOpenAdminPage === 'function' && parentWindow.ncfOpenAdminPage(href, label)) {
                event.preventDefault();
                console.debug('[NCF][AdminTabs] 页面已打开，切到已有标签', href);
            }
        }, true);
    }

    function installShell(locationHref) {
        var currentUrl = new URL(locationHref);
        var stored = readStorage();
        var hashPage = pageUrlFromLocationHash(currentUrl.hash);
        var initial = createInitialState({
            shellHref: locationHref,
            storedTabs: stored.tabs,
            activeId: stored.activeId,
            openUrl: currentUrl.searchParams.get('ncfOpen') || hashPage,
            homeTitle: textOf('home', '首页'),
            untitledTitle: textOf('untitled', '未命名页面'),
            now: Date.now()
        });
        publish(initial, 'ready');

        root.addEventListener('hashchange', function () {
            var pageUrl = pageUrlFromLocationHash(root.location.hash);
            var current = snapshot();
            if (!current.enabled) {
                return;
            }
            if (!pageUrl) {
                if (current.activeId !== current.shellId) {
                    publish(activateTab(current, current.shellId), 'hash-home');
                }
                return;
            }
            var followed = rememberTabAddress(current, pageUrl, root.location.href);
            if (followed.tab) {
                var parsed = parseAdminUrl(pageUrl, root.location.href);
                var frameChanged = !!(parsed && !followed.tab.isShell && followed.tab.frameUrl !== parsed.frameUrl);
                if (frameChanged) {
                    followed.tab.frameUrl = parsed.frameUrl;
                }
                if (!followed.changed && !frameChanged && followed.state.activeId === followed.tab.id) {
                    return;
                }
                var next = followed.state;
                if (next.activeId !== followed.tab.id) {
                    next = activateTab(next, followed.tab.id);
                }
                publish(next, 'hash');
                return;
            }
            var result = openTab(current, pageUrl, '', null, Date.now(), root.location.href);
            if (result.handled) {
                publish(result.state, 'hash');
            }
        });

        root.ncfReportAdminTabLocation = function (liveHref) {
            var current = snapshot();
            if (!current.enabled) {
                return;
            }
            var followed = rememberTabAddress(current, liveHref, liveHref);
            if (!followed.changed) {
                return;
            }
            publish(followed.state, 'iframe-url');
        };

        root.ncfHasAdminTab = function (url) {
            var parsed = parseAdminUrl(url, root.location.href);
            if (!parsed || !parsed.hijack) {
                return false;
            }
            return snapshot().tabs.some(function (tab) {
                return tab.id === parsed.id;
            });
        };

        root.ncfOpenAdminPage = function (url, title, menuIndex) {
            var result = openTab(snapshot(), url, title, menuIndex, Date.now(), root.location.href);
            if (!result.handled) {
                console.debug('[NCF][AdminTabs] 交给浏览器打开', url);
                return false;
            }
            publish(result.state, 'open');
            return true;
        };

        root.ncfActivateAdminTab = function (id) {
            publish(activateTab(snapshot(), id), 'activate');
        };

        root.ncfScrollAdminTabs = scrollAdminTabs;

        root.ncfCloseAdminTab = function (id) {
            var next = closeTab(snapshot(), id);
            if (next.activeId === snapshot().activeId && next.tabs.length === snapshot().tabs.length) {
                return;
            }
            publish(next, 'close');
        };

        root.ncfSyncAdminTabsWithMenu = function (menuList) {
            var applied = applyMenuTitles(snapshot(), menuList, root.location.href);
            publish(applied.state, 'menu');
        };

        root.ncfAdminTabFrameLoad = function (tab, event) {
            var frame = event && event.target;
            var frameWindow = frame && frame.contentWindow;
            if (!frameWindow) {
                return;
            }
            try {
                var path = frameWindow.location && frameWindow.location.pathname || '';
                console.debug('[NCF][AdminTabs] iframe 已加载', tab && tab.url, path);
                if (/^\/admin\/login(\/|$)/i.test(path)) {
                    root.location.href = frameWindow.location.href;
                    return;
                }
                if (typeof root.ncfReportAdminTabLocation === 'function') {
                    root.ncfReportAdminTabLocation(frameWindow.location.href);
                }
            } catch (error) {
                console.debug('[NCF][AdminTabs] 无法读取 iframe 地址', error && error.message);
            }
        };

        if (root.document) {
            root.document.addEventListener('click', function (event) {
                if (event.defaultPrevented || event.button !== 0) {
                    return;
                }
                if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
                    return;
                }
                var target = event.target;
                var anchor = target && target.closest ? target.closest('a[href]') : null;
                if (!anchor || anchor.hasAttribute('download')) {
                    return;
                }
                if (anchor.target && anchor.target !== '_self') {
                    return;
                }
                var href = anchor.href || anchor.getAttribute('href');
                var label = (anchor.getAttribute('data-ncf-tab-title') || anchor.textContent || '').replace(/\s+/g, ' ').trim();
                if (root.ncfOpenAdminPage(href, label)) {
                    event.preventDefault();
                }
            }, true);
        }
    }

    function install() {
        if (!root.document || !root.Vue) {
            return;
        }
        root.Vue.prototype.ncfActivateAdminTab = function (id) {
            if (typeof root.ncfActivateAdminTab === 'function') {
                root.ncfActivateAdminTab(id);
            }
        };
        root.Vue.prototype.ncfCloseAdminTab = function (id) {
            if (typeof root.ncfCloseAdminTab === 'function') {
                root.ncfCloseAdminTab(id);
            }
        };
        root.Vue.prototype.ncfAdminTabFrameLoad = function (tab, event) {
            if (typeof root.ncfAdminTabFrameLoad === 'function') {
                root.ncfAdminTabFrameLoad(tab, event);
            }
        };
        root.Vue.prototype.ncfAuxCloseAdminTab = function (id, event) {
            if (!event || event.button !== 1 || typeof root.ncfCloseAdminTab !== 'function') {
                return;
            }
            event.preventDefault();
            root.ncfCloseAdminTab(id);
        };
        root.Vue.prototype.ncfScrollAdminTabs = function (direction) {
            if (typeof root.ncfScrollAdminTabs === 'function') {
                root.ncfScrollAdminTabs(direction);
            }
        };
        root.Vue.prototype.ncfAdminTabCloseLabel = textOf('close', '关闭');
        root.Vue.prototype.ncfAdminTabScrollLeftLabel = textOf('scrollLeft', '向左查看标签');
        root.Vue.prototype.ncfAdminTabScrollRightLabel = textOf('scrollRight', '向右查看标签');
        if (root.Vue.mixin) {
            root.Vue.mixin({
                mounted: function () {
                    if (this.$el && this.$el.id === 'app') {
                        bindTabScroller();
                    }
                }
            });
        }

        var embedded = root.document.body && root.document.body.classList.contains('ncf-admin-embedded');
        var params = new URLSearchParams(root.location.search || '');
        if (root.top !== root || embedded || params.get('embedded') === '1' || params.get('ncfShell') !== '1') {
            installPageBridge();
            console.debug('[NCF][AdminTabs] 当前文档不是标签外壳');
            return;
        }
        installShell(root.location.href);
    }

    var api = {
        MAX_ADMIN_TABS: MAX_ADMIN_TABS,
        STORAGE_KEY: STORAGE_KEY,
        parseAdminUrl: parseAdminUrl,
        createInitialState: createInitialState,
        openTab: openTab,
        activateTab: activateTab,
        closeTab: closeTab,
        applyMenuTitles: applyMenuTitles,
        tabScrollState: tabScrollState,
        shellPageHash: shellPageHash,
        shellAddress: shellAddress,
        pageUrlFromLocationHash: pageUrlFromLocationHash,
        rememberTabAddress: rememberTabAddress
    };

    root.NcfAdminTabs = api;
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = api;
    }
    if (root.document && root.document.body) {
        install();
    } else if (root.document) {
        root.document.addEventListener('DOMContentLoaded', install);
    }
})(typeof window !== 'undefined' ? window : global);
