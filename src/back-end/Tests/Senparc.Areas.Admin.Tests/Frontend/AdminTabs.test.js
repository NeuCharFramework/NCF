const assert = require('node:assert/strict');
const test = require('node:test');
const tabs = require('../../../Senparc.Areas.Admin/wwwroot/js/Admin/Shared/AdminTabs.js');

const origin = 'http://ncf.local';
const shellHref = origin + '/Admin/Index?ncfShell=1';

function initial(extra) {
    return tabs.createInitialState(Object.assign({
        shellHref: shellHref,
        homeTitle: '首页',
        untitledTitle: '未命名页面',
        now: 10
    }, extra || {}));
}

test('后台地址会去掉外壳参数，并忽略参数顺序', function () {
    const left = tabs.parseAdminUrl('/Admin/Menu/Index?b=2&embedded=1&a=1', shellHref);
    const right = tabs.parseAdminUrl(origin + '/Admin/Menu/Index?a=1&b=2&ncfTab=1', shellHref);
    assert.equal(left.id, right.id);
    assert.equal(left.id, '/admin/menu/index?a=1&b=2');
    assert.equal(left.frameUrl, '/Admin/Menu/Index?a=1&b=2&embedded=1&ncfTab=1');
    assert.equal(left.hijack, true);
});

test('/Admin 与首页是同一个标签', function () {
    const home = tabs.parseAdminUrl('/Admin', shellHref);
    const index = tabs.parseAdminUrl('/Admin/Index/', shellHref);
    assert.equal(home.id, '/admin/index');
    assert.equal(home.id, index.id);
});

test('登录、外链和 Razor handler 不进入标签', function () {
    assert.equal(tabs.parseAdminUrl('/Admin/Login?handler=Logout', shellHref), null);
    assert.equal(tabs.parseAdminUrl('https://example.com/Admin/Index', shellHref), null);
    const handler = tabs.parseAdminUrl('/Admin/Index?handler=Export', shellHref);
    assert.equal(handler.hijack, false);
});

test('直接打开的页面会成为活动标签，首页仍保留', function () {
    const state = initial({
        openUrl: '/Admin/Role/Index?id=8'
    });
    assert.equal(state.enabled, true);
    assert.equal(state.shellId, '/admin/index');
    assert.equal(state.activeId, '/admin/role/index?id=8');
    assert.equal(state.tabs.length, 2);
    assert.equal(state.tabs[0].isShell, true);
    assert.equal(state.tabs[1].loaded, true);
    assert.equal(state.tabs[1].frameUrl.includes('embedded=1'), true);
});

test('已经打开的地址只切换标签，不新增', function () {
    const state = initial();
    const first = tabs.openTab(state, '/Admin/Menu/Index', '菜单', 12, 20, shellHref);
    const second = tabs.openTab(first.state, '/admin/menu/index/', '菜单', 12, 30, shellHref);
    assert.equal(second.handled, true);
    assert.equal(second.state.tabs.length, 2);
    assert.equal(second.state.activeId, '/admin/menu/index');
    assert.equal(second.tab.loaded, true);
    assert.equal(second.tab.openedAt, 20);
});

test('关闭当前标签后切到相邻标签，最后一个标签不能关闭', function () {
    let state = initial();
    state = tabs.openTab(state, '/Admin/Menu/Index', '菜单', 1, 20, shellHref).state;
    state = tabs.openTab(state, '/Admin/Role/Index', '角色', 2, 30, shellHref).state;
    const closed = tabs.closeTab(state, '/admin/role/index');
    assert.equal(closed.activeId, '/admin/menu/index');
    assert.equal(closed.tabs.some(function (tab) { return tab.id === '/admin/role/index'; }), false);
    const only = tabs.closeTab(initial(), '/admin/index');
    assert.equal(only.tabs.length, 1);
});

test('首页固定在最左边，并且不能关闭', function () {
    let state = initial();
    state = tabs.openTab(state, '/Admin/Menu/Index', '菜单', 1, 20, shellHref).state;
    state = tabs.openTab(state, '/Admin/Index', '控制台', 2, 30, shellHref).state;
    assert.equal(state.tabs[0].isShell, true);
    assert.equal(state.tabs[0].title, '首页');
    const closed = tabs.closeTab(state, state.shellId);
    assert.equal(closed.tabs[0].id, state.shellId);
    assert.equal(closed.tabs.length, state.tabs.length);
    assert.equal(closed.activeId, state.shellId);
});

test('菜单名称会回填到相同地址的标签', function () {
    const state = tabs.openTab(initial(), '/Admin/Menu/Index', 'Menu', null, 20, shellHref).state;
    const applied = tabs.applyMenuTitles(state, [
        { id: 7, menuName: '系统', url: '', children: [
            { id: 9, menuName: '菜单管理', url: '/Admin/Menu/Index', children: [] }
        ] }
    ], shellHref);
    assert.equal(applied.activeTab.title, '菜单管理');
    assert.equal(applied.activeTab.menuIndex, '9');
});

test('标签超出可视宽度时才显示左右滚动', function () {
    assert.deepEqual(tabs.tabScrollState(0, 200, 200), { canScrollLeft: false, canScrollRight: false });
    assert.deepEqual(tabs.tabScrollState(0, 480, 200), { canScrollLeft: false, canScrollRight: true });
    assert.deepEqual(tabs.tabScrollState(120, 480, 200), { canScrollLeft: true, canScrollRight: true });
    assert.deepEqual(tabs.tabScrollState(280, 480, 200), { canScrollLeft: true, canScrollRight: false });
});

test('当前页面写进外壳锚点，复制后能打开同一页', function () {
    const state = tabs.openTab(initial(), '/Admin/Role/Index?id=8', '角色', 2, 30, shellHref).state;
    const active = state.tabs.find(function (tab) { return tab.id === state.activeId; });
    const home = state.tabs.find(function (tab) { return tab.isShell; });
    assert.equal(tabs.shellAddress(active), '/Admin/Index?ncfShell=1#/Admin/Role/Index?id=8');
    assert.equal(tabs.shellAddress(home), '/Admin/Index?ncfShell=1');
    assert.equal(tabs.pageUrlFromLocationHash('#/Admin/Role/Index?id=8'), '/Admin/Role/Index?id=8');
    assert.equal(tabs.pageUrlFromLocationHash('#/Admin/AdminChat/Chat?mode=1#session-15'), '/Admin/AdminChat/Chat?mode=1#session-15');
    assert.equal(tabs.pageUrlFromLocationHash(''), '');
    assert.equal(tabs.pageUrlFromLocationHash('#session-15'), '');
    assert.equal(tabs.pageUrlFromLocationHash('#/Admin/Login'), '');
    assert.equal(tabs.pageUrlFromLocationHash('#/Admin/Login/Index'), '');

    const restored = initial({
        openUrl: tabs.pageUrlFromLocationHash('#/Admin/AdminChat/Chat?mode=1#session-15')
    });
    assert.equal(restored.activeId, '/admin/adminchat/chat?mode=1');
    const chat = restored.tabs.find(function (tab) { return tab.id === restored.activeId; });
    assert.equal(chat.url, '/Admin/AdminChat/Chat?mode=1#session-15');
    assert.equal(chat.frameUrl.includes('#session-15'), true);
});

test('iframe 内部的查询和锚点写回外壳地址，并且仍是同一个标签', function () {
    const state = tabs.openTab(initial(), '/Admin/AdminChat/Chat', 'Admin Chat', 3, 20, shellHref).state;
    state.tabs.forEach(function (tab) {
        if (!tab.isShell) tab.loaded = true;
    });
    const chatBefore = state.tabs.find(function (tab) { return tab.id === '/admin/adminchat/chat'; });
    const live = 'http://localhost:5080/Admin/AdminChat/Chat?embedded=1&ncfTab=1&sessionId=15&mode=1#session-15';
    const result = tabs.rememberTabAddress(state, live, live);
    assert.equal(result.changed, true);
    assert.equal(result.state.tabs.length, 2);
    assert.equal(result.tab.frameUrl, chatBefore.frameUrl);
    assert.equal(tabs.shellAddress(result.tab), '/Admin/Index?ncfShell=1#/Admin/AdminChat/Chat?mode=1&sessionId=15#session-15');
    assert.equal(tabs.rememberTabAddress(result.state, live, live).changed, false);
});

test('刷新时带会话锚点会回到原来的 AdminChat 标签', function () {
    const restored = initial({
        storedTabs: [{ url: '/Admin/AdminChat/Chat', title: 'Admin Chat', address: '/Admin/AdminChat/Chat' }],
        activeId: '/admin/adminchat/chat',
        openUrl: '/Admin/AdminChat/Chat?mode=1&sessionId=15#session-15'
    });
    const chats = restored.tabs.filter(function (tab) { return !tab.isShell; });
    assert.equal(chats.length, 1);
    assert.equal(chats[0].id, '/admin/adminchat/chat');
    assert.equal(restored.activeId, chats[0].id);
    assert.equal(chats[0].frameUrl.includes('sessionId=15'), true);
    assert.equal(chats[0].frameUrl.includes('#session-15'), true);
    assert.equal(chats[0].frameUrl.includes('embedded=1'), true);
    assert.equal(tabs.shellAddress(chats[0]), '/Admin/Index?ncfShell=1#/Admin/AdminChat/Chat?mode=1&sessionId=15#session-15');
});

test('超出上限时关闭最早且不是当前、也不是首页的标签', function () {
    let state = initial();
    for (let i = 0; i < tabs.MAX_ADMIN_TABS + 2; i++) {
        state = tabs.openTab(state, '/Admin/Page/Item?id=' + i, '页面' + i, i, i + 1, shellHref).state;
    }
    assert.equal(state.tabs.length, tabs.MAX_ADMIN_TABS);
    assert.equal(state.tabs.some(function (tab) { return tab.isShell; }), true);
    assert.equal(state.activeId, '/admin/page/item?id=' + (tabs.MAX_ADMIN_TABS + 1));
    assert.equal(state.tabs.some(function (tab) { return tab.id === '/admin/page/item?id=0'; }), false);
});
