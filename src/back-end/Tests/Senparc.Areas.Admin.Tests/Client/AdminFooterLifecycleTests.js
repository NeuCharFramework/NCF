'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Shared/AdminFooter.js');
const script = fs.readFileSync(scriptPath, 'utf8');

let stateRequestCount = 0;
let eventSourceCount = 0;
let eventSourceCloseCount = 0;
let mixinRegistrationCount = 0;
let animationFrameCount = 0;
let animationFrameCancelCount = 0;
let canvasStrokeCount = 0;
let canvasFillRectCount = 0;
let capturedMixin = null;
let timerSequence = 0;
const timeoutCallbacks = new Map();
const animationFrameCallbacks = new Map();
const footerCanvases = [];
let consumeRequest = null;
let stateProviders = [{
    providerId: 'test-provider',
    displayName: 'Test Provider',
    defaultVisible: true,
    canConsume: true,
    items: []
}];
const eventSources = [];
const layoutElement = { id: 'app' };
const fakeConsole = {
    log() { },
    info() { },
    warn() { },
    error() { }
};
const documentListeners = new Map();
const windowListeners = new Map();
const document = {
    hidden: false,
    body: {
        appendChild(element) {
            element.parentNode = this;
            footerCanvases.push(element);
        },
        removeChild(element) {
            const index = footerCanvases.indexOf(element);
            if (index >= 0) {
                footerCanvases.splice(index, 1);
            }
            element.parentNode = null;
        }
    },
    documentElement: { clientWidth: 1440, clientHeight: 900 },
    createElement(tagName) {
        assert.strictEqual(tagName, 'canvas');
        return {
            style: {},
            parentNode: null,
            setAttribute() { },
            getContext() {
                return {
                    setTransform() { }, clearRect() { }, beginPath() { }, moveTo() { }, lineTo() { },
                    stroke() { canvasStrokeCount++; }, arc() { }, fill() { }, fillRect() { canvasFillRectCount++; },
                    save() { }, restore() { }, translate() { }, rotate() { }
                };
            }
        };
    },
    getElementById(id) {
        return id === 'app' ? layoutElement : null;
    },
    addEventListener(name, handler) {
        documentListeners.set(name, handler);
    },
    removeEventListener(name, handler) {
        if (documentListeners.get(name) === handler) {
            documentListeners.delete(name);
        }
    }
};
const axios = {
    get() {
        stateRequestCount++;
        return Promise.resolve({
            data: {
                serverTime: new Date().toISOString(),
                providers: stateProviders
            }
        });
    },
    post(url, data) {
        consumeRequest = { url, data };
        stateProviders = [{
            providerId: 'test-provider',
            displayName: 'Test Provider',
            defaultVisible: true,
            canConsume: true,
            items: []
        }];
        return Promise.resolve({ data: { consumedCount: 1 } });
    },
    isCancel() {
        return false;
    },
    CancelToken: {
        source() {
            return {
                token: {},
                cancel() { }
            };
        }
    }
};
const window = {
    console: fakeConsole,
    document,
    localStorage: {
        getItem() { return null; },
        setItem() { }
    },
    addEventListener(name, handler) {
        windowListeners.set(name, handler);
    },
    removeEventListener(name, handler) {
        if (windowListeners.get(name) === handler) {
            windowListeners.delete(name);
        }
    },
    setInterval() { return ++timerSequence; },
    clearInterval() { },
    setTimeout(callback) {
        const timer = ++timerSequence;
        timeoutCallbacks.set(timer, callback);
        return timer;
    },
    clearTimeout(timer) {
        timeoutCallbacks.delete(timer);
    },
    innerWidth: 1440,
    innerHeight: 900,
    devicePixelRatio: 1,
    requestAnimationFrame(callback) {
        const frameId = ++animationFrameCount;
        animationFrameCallbacks.set(frameId, callback);
        return frameId;
    },
    cancelAnimationFrame(frameId) {
        animationFrameCancelCount++;
        animationFrameCallbacks.delete(frameId);
    },
    EventSource: function EventSource() {
        eventSourceCount++;
        const listeners = new Map();
        this.addEventListener = function (name, handler) { listeners.set(name, handler); };
        this.emit = function (name) {
            const handler = listeners.get(name);
            if (handler) {
                handler({ type: name });
            }
        };
        this.close = function () { eventSourceCloseCount++; };
        eventSources.push(this);
    },
    Vue: {
        mixin(mixin) {
            mixinRegistrationCount++;
            capturedMixin = mixin;
        }
    },
    NCF_ADMIN_FOOTER_INITIAL_STATE: {
        serverTime: new Date().toISOString(),
        account: 'test-admin',
        embedded: false
    }
};

const context = vm.createContext({
    window,
    document,
    axios,
    console: fakeConsole,
    EventSource: window.EventSource,
    Date,
    JSON,
    Math,
    Number,
    Object,
    Array,
    Error,
    Promise,
    Set,
    String
});

function createViewModel(element) {
    const viewModel = Object.assign({
        $el: element,
        $root: null,
    }, capturedMixin.data());
    viewModel.$root = viewModel;
    Object.keys(capturedMixin.methods).forEach(name => {
        viewModel[name] = capturedMixin.methods[name].bind(viewModel);
    });
    Object.keys(capturedMixin.computed).forEach(name => {
        Object.defineProperty(viewModel, name, {
            configurable: true,
            get() { return capturedMixin.computed[name].call(viewModel); }
        });
    });
    return viewModel;
}

function fireTimeout(timer) {
    const callback = timeoutCallbacks.get(timer);
    timeoutCallbacks.delete(timer);
    if (callback) {
        callback();
    }
}

function fireAnimationFrame(frameId, frameAt) {
    const callback = animationFrameCallbacks.get(frameId);
    animationFrameCallbacks.delete(frameId);
    if (callback) {
        callback(frameAt);
    }
}

async function flushPromises() {
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
}

async function run() {
    vm.runInContext(script, context, { filename: scriptPath });
    const firstConsole = window.NcfAdminConsole;

    let mirroredConsoleEntries = [];
    const unsubscribeConsole = firstConsole.subscribe(entries => { mirroredConsoleEntries = entries; });
    window.console.error('same Vue render failure');
    window.console.error('same Vue render failure');
    assert.strictEqual(mirroredConsoleEntries.length, 1,
        'The Footer must not repeatedly mirror an identical Console error into reactive state.');
    window.console.warn('a different Console entry');
    assert.strictEqual(mirroredConsoleEntries.length, 2,
        'The Footer Console must retain different diagnostics while suppressing only identical repeats.');
    unsubscribeConsole();

    // 重复加载不得再次包装 Console 或重复注册全局 mixin。
    vm.runInContext(script, context, { filename: scriptPath });
    assert.strictEqual(window.NcfAdminConsole, firstConsole);
    assert.strictEqual(mixinRegistrationCount, 1);

    const temporaryRoot = createViewModel({ id: 'temporary-element-ui-root' });
    capturedMixin.mounted.call(temporaryRoot);
    await flushPromises();
    assert.strictEqual(temporaryRoot.footerCommunicationOwner, false);
    assert.strictEqual(stateRequestCount, 0);

    const layoutRoot = createViewModel(layoutElement);
    capturedMixin.mounted.call(layoutRoot);
    await flushPromises();
    assert.strictEqual(layoutRoot.footerCommunicationOwner, true);
    assert.strictEqual(stateRequestCount, 1);
    assert.strictEqual(eventSourceCount, 1);

    // SSE 刷新后，新增提醒应显示右下角 Toast，并增加 Footer 徽标。
    stateProviders = [{
        providerId: 'test-provider',
        displayName: 'Test Provider',
        defaultVisible: true,
        canConsume: true,
        items: [{
            id: 'function-reminder',
            title: 'NeuBell test reminder',
            summary: 'One reminder is pending.',
            count: 1,
            severity: 'warning'
        }]
    }];
    eventSources[0].emit('neubell-changed');
    await flushPromises();
    assert.strictEqual(stateRequestCount, 2);
    assert.strictEqual(layoutRoot.neuBellToastEntries.length, 1);
    assert.strictEqual(capturedMixin.computed.neuBellToastVisibleItems.call(layoutRoot).length, 1);
    assert.ok(layoutRoot.neuBellToastTimers['test-provider:function-reminder']);
    assert.strictEqual(capturedMixin.computed.neuBellTotalCount.call(layoutRoot), 1);

    // 支持消费的 Provider 可从抽屉消费本条提醒，并刷新状态、关闭右下角 Toast。
    await layoutRoot.consumeNeuBell(layoutRoot.neuBellProviders[0], layoutRoot.neuBellProviders[0].items[0], false);
    await flushPromises();
    assert.strictEqual(stateRequestCount, 3);
    assert.strictEqual(consumeRequest.url, '/api/Senparc.Areas.Admin/neubell/consume');
    assert.strictEqual(consumeRequest.data.providerId, 'test-provider');
    assert.strictEqual(consumeRequest.data.itemId, 'function-reminder');
    assert.strictEqual(consumeRequest.data.consumeAll, false);
    assert.strictEqual(layoutRoot.neuBellToastEntries.length, 0);
    assert.strictEqual(capturedMixin.computed.neuBellTotalCount.call(layoutRoot), 0);

    // 同时出现多条提醒时最多展示 2 条；点击展开后可查看全部，关闭只影响视觉 Toast。
    stateProviders = [{
        providerId: 'test-provider',
        displayName: 'Test Provider',
        defaultVisible: true,
        canConsume: true,
        items: [
            { id: 'reminder-1', title: 'Reminder 1', summary: 'First reminder.', count: 1, severity: 'info' },
            { id: 'reminder-2', title: 'Reminder 2', summary: 'Second reminder.', count: 1, severity: 'warning' },
            { id: 'reminder-3', title: 'Reminder 3', summary: 'Third reminder.', count: 1, severity: 'error' }
        ]
    }];
    eventSources[0].emit('neubell-changed');
    await flushPromises();
    assert.strictEqual(stateRequestCount, 4);
    assert.strictEqual(layoutRoot.neuBellToastEntries.length, 3);
    assert.strictEqual(capturedMixin.computed.neuBellToastVisibleItems.call(layoutRoot).length, 2);
    assert.strictEqual(capturedMixin.computed.neuBellToastOverflowCount.call(layoutRoot), 1);
    layoutRoot.toggleNeuBellToastExpanded();
    assert.strictEqual(capturedMixin.computed.neuBellToastVisibleItems.call(layoutRoot).length, 3);
    layoutRoot.toggleNeuBellToastExpanded();
    assert.strictEqual(capturedMixin.computed.neuBellToastVisibleItems.call(layoutRoot).length, 2);

    const expiringToastKey = layoutRoot.neuBellToastEntries[0].key;
    fireTimeout(layoutRoot.neuBellToastTimers[expiringToastKey]);
    assert.strictEqual(layoutRoot.neuBellToastEntries.length, 2);
    layoutRoot.clearAllNeuBellToasts();
    assert.strictEqual(layoutRoot.neuBellToastEntries.length, 0);
    assert.strictEqual(capturedMixin.computed.neuBellTotalCount.call(layoutRoot), 3,
        '关闭视觉 Toast 不得自动消费服务端提醒。');

    stateProviders = [{
        providerId: 'test-provider',
        displayName: 'Test Provider',
        defaultVisible: true,
        canConsume: true,
        items: []
    }];
    eventSources[0].emit('neubell-changed');
    await flushPromises();
    assert.strictEqual(stateRequestCount, 5);
    assert.strictEqual(capturedMixin.computed.neuBellTotalCount.call(layoutRoot), 0);

    // 即使出现第二个独立 Vue 根实例，也不能增加 state/SSE 通讯通道。
    const duplicateLayoutRoot = createViewModel(layoutElement);
    capturedMixin.mounted.call(duplicateLayoutRoot);
    await flushPromises();
    assert.strictEqual(duplicateLayoutRoot.footerCommunicationOwner, false);
    assert.strictEqual(stateRequestCount, 5);
    assert.strictEqual(eventSourceCount, 1);

    // 10 次连续点击服务器时间才创建全屏 Canvas；销毁时应立刻停止动画并移除 DOM。
    for (let index = 0; index < 10; index++) {
        layoutRoot.handleFooterTimeClick();
    }
    assert.strictEqual(footerCanvases.length, 1);
    assert.strictEqual(animationFrameCallbacks.size, 1);
    fireAnimationFrame(Array.from(animationFrameCallbacks.keys())[0], 100);
    assert.ok(canvasStrokeCount > 0);
    assert.ok(canvasFillRectCount > 0);
    assert.strictEqual(animationFrameCallbacks.size, 1);

    capturedMixin.beforeDestroy.call(layoutRoot);
    assert.strictEqual(window.NcfAdminFooterRuntime.owner, null);
    assert.strictEqual(eventSourceCloseCount, 1);
    assert.strictEqual(footerCanvases.length, 0);
    assert.strictEqual(animationFrameCallbacks.size, 0);
    assert.ok(animationFrameCancelCount > 0);

    // 原 Owner 销毁后允许新的真实布局根接管，兼容局部导航和测试环境重新挂载。
    capturedMixin.mounted.call(duplicateLayoutRoot);
    await flushPromises();
    assert.strictEqual(duplicateLayoutRoot.footerCommunicationOwner, true);
    assert.strictEqual(stateRequestCount, 6);
    assert.strictEqual(eventSourceCount, 2);
    capturedMixin.beforeDestroy.call(duplicateLayoutRoot);
    assert.strictEqual(eventSourceCloseCount, 2);

    process.stdout.write('AdminFooter lifecycle tests passed.\n');
}

run().catch(error => {
    process.stderr.write(error.stack + '\n');
    process.exitCode = 1;
});
