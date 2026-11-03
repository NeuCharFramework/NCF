'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const admin = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const script = fs.readFileSync(path.join(admin, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js'), 'utf8');
const page = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'), 'utf8');
const css = fs.readFileSync(path.join(admin, 'wwwroot/css/Admin/Pages/AdminChat/Chat.css'), 'utf8');
let options;
const storage = new Map();
const listeners = new Map();
const classes = new Set();
const errors = [];
const warnings = [];
const requests = [];
const sandbox = {
    Vue: function(value) { options = value; },
    window: {
        ChatLauncherMixin: {},
        addEventListener(name, handler) { listeners.set(name, handler); },
        removeEventListener(name) { listeners.delete(name); }
    },
    document: {
        addEventListener(name, handler) { listeners.set(name, handler); },
        removeEventListener(name) { listeners.delete(name); },
        body: { classList: { add: name => classes.add(name), remove: name => classes.delete(name) } }
    },
    localStorage: { getItem: key => storage.get(key), setItem: (key, value) => storage.set(key, value) },
    ncfT: key => key,
    console: { error() {} },
    service: {}
};
vm.runInNewContext(script, sandbox);
function createApp() {
    const app = options.data();
    for (const [name, method] of Object.entries(options.methods)) app[name] = method.bind(app);
    for (const [name, getter] of Object.entries(options.computed))
        Object.defineProperty(app, name, { get: getter.bind(app) });
    app.$nextTick = fn => fn();
    app.$message = { success() {}, error: value => errors.push(value), warning: value => warnings.push(value) };
    return app;
}
const ids = group => Array.from(group.sessions, session => session.id);
const app = createApp();

async function run() {
    app.currentUserId = 12;
    app.toggleSidebar();
    assert.strictEqual(app.sidebarCollapsed, true);
    app.toggleSessionGroup('alpha');
    app.setSidebarWidth(999);
    assert.strictEqual(app.sidebarWidth, 420);
    app.resizeSidebarByKeyboard(-999);
    assert.strictEqual(app.sidebarWidth, 215);
    const restored = createApp();
    restored.currentUserId = 12;
    restored.loadSidebarPreferences();
    assert.strictEqual(restored.sidebarCollapsed, true);
    assert.strictEqual(restored.sidebarWidth, 215);
    assert.ok(restored.isSessionGroupCollapsed('alpha'));
    restored.currentUserId = 13;
    assert.notStrictEqual(restored.sidebarStorageKey(), app.sidebarStorageKey());
    restored.loadSidebarPreferences();
    storage.set(restored.sidebarStorageKey(), '{broken');
    restored.loadSidebarPreferences();
    assert.strictEqual(warnings.length, 1, 'Corrupt preferences must be reported, not silently ignored.');

    app.sidebarCollapsed = false;
    app.sidebarWidth = 250;
    app.startSidebarResize({
        button: 0, pointerId: 7, clientX: 100, preventDefault() {},
        currentTarget: { parentElement: { getBoundingClientRect: () => ({ width: 250 }) } }
    });
    listeners.get('pointermove')({ pointerId: 8, clientX: 300 });
    assert.strictEqual(app.sidebarWidth, 250);
    listeners.get('pointermove')({ pointerId: 7, clientX: 180 });
    assert.strictEqual(app.sidebarWidth, 330, 'Resize must use pointer delta.');
    listeners.get('pointerup')({ pointerId: 7 });
    assert.strictEqual(listeners.size, 0);
    assert.strictEqual(classes.size, 0);

    app.sessionList = [
        { id: 1, workspaceId: 'alpha', sortOrder: 1 },
        { id: 2, workspaceId: 'alpha', sortOrder: 0 },
        { id: 3, workspaceId: 'alpha', sortOrder: null },
        { id: 4, workspaceId: 'beta', sortOrder: null },
        { id: 5, workspaceId: null, sortOrder: null }
    ];
    assert.deepStrictEqual(ids(app.groupedSessionList[0]), [2, 1, 3]);
    assert.deepStrictEqual(Array.from(app.sessionList, session => session.id), [1, 2, 3, 4, 5],
        'Computed sorting must not mutate the source list or change category membership.');
    const transfer = { setData() {} };
    app.startSessionDrag({ dataTransfer: transfer }, app.sessionList[0]);
    assert.strictEqual(transfer.effectAllowed, 'move');
    assert.strictEqual(app.canDropSession(app.sessionList[3]), false, 'Never move sessions across Workspaces.');
    assert.strictEqual(app.canDropSession(app.sessionList[0]), false);
    let prevented = false;
    const dragEvent = {
        clientY: 45, dataTransfer: transfer, preventDefault() { prevented = true; },
        currentTarget: { getBoundingClientRect: () => ({ top: 0, height: 50 }) }
    };
    app.handleSessionDragOver(dragEvent, app.sessionList[1]);
    assert.ok(prevented);
    assert.strictEqual(app.dropAfter, true);
    sandbox.service.post = async (url, data) => {
        requests.push({ url, data });
        return { data: { success: true, data: [{ id: 1, sortOrder: 0 }, { id: 2, sortOrder: 1 }, { id: 3, sortOrder: 2 }] } };
    };
    await app.handleSessionDrop({ ...dragEvent, clientY: 5 }, app.sessionList[1]);
    assert.strictEqual(requests[0].data.insertAfter, false, 'Drop position must be recalculated at drop time.');
    assert.deepStrictEqual(ids(app.groupedSessionList[0]), [1, 2, 3]);
    assert.strictEqual(app.draggingSessionId, 0);
    assert.strictEqual(app.dropSessionId, 0);
    assert.strictEqual(app.savingSessionOrder, false);
    await app.moveSessionByKeyboard(app.sessionList[0], 1);
    assert.strictEqual(requests.at(-1).data.targetSessionId, 2);
    assert.strictEqual(requests.at(-1).data.insertAfter, true);
    const ranksBefore = JSON.stringify(app.sessionList);
    sandbox.service.post = async () => ({ data: { success: false, errorMessage: 'forbidden' } });
    await app.saveSessionOrder(1, 2, false);
    assert.strictEqual(errors.pop(), 'forbidden');
    assert.strictEqual(JSON.stringify(app.sessionList), ranksBefore, 'Failed saves must preserve the previous order.');
    sandbox.service.post = async () => ({ data: '<html>Login</html>' });
    await app.saveSessionOrder(1, 2, false);
    assert.strictEqual(errors.pop(), 'AdminChat.Sidebar.SortFailed');

    app.currentSessionId = 2;
    app.runningSessionId = 1;
    app.isSending = true;
    assert.ok(app.isSessionRunning({ id: 1 }));
    assert.ok(!app.isSessionRunning({ id: 2 }), 'The running indicator must not follow navigation.');
    app.isSending = false;
    assert.ok(!app.isSessionRunning({ id: 1 }));

    let pages = 0;
    sandbox.service.get = async () => {
        pages++;
        return { data: { success: true, data: { sessions: [{ id: pages }], totalCount: 2 } } };
    };
    await app.loadSessionList();
    assert.strictEqual(pages, 2, 'Sorting must include sessions beyond the first page.');
    assert.strictEqual(app.sessionList.length, 2);
    assert.strictEqual(app.loadingSessions, false);
    sandbox.service.get = async () => ({ data: { success: true, data: { sessions: [], totalCount: 2 } } });
    await app.loadSessionList();
    assert.strictEqual(app.sessionList.length, 2, 'A partial list failure must not discard the prior list.');
    assert.strictEqual(errors.pop(), 'AdminChat.LoadSessionFailed');

    assert.ok(page.includes('@@pointerdown="startSidebarResize"'));
    assert.ok(page.includes('@@keydown.alt.up.prevent.stop'));
    assert.ok(page.includes('v-show="!isSessionGroupCollapsed(group.key)"'));
    assert.match(css, /prefers-reduced-motion:\s*reduce/);
    assert.match(css, /chat-session-flow/);
    assert.match(css, /\.session-item\.is-running::before/);
    for (const locale of ['', '.zh-CN', '.en', '.ja', '.fr', '.es', '.ru']) {
        const resource = fs.readFileSync(path.join(admin, `Resources/AdminResource${locale}.resx`), 'utf8');
        for (const key of ['Expand', 'Collapse', 'Resize', 'Reorder', 'Running', 'Stopping', 'SortSaved', 'SortFailed', 'PreferencesFailed'])
            assert.ok(resource.includes(`name="AdminChat.Sidebar.${key}"`), `${locale} missing ${key}`);
    }
    console.log('AdminChat sidebar tests passed.');
}
run().catch(error => { console.error(error); process.exitCode = 1; });
