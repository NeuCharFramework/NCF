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
let capturedMixin = null;
let timerSequence = 0;
let notificationCount = 0;
let notificationCloseCount = 0;
let lastNotificationOptions = null;
let stateProviders = [{
    providerId: 'test-provider',
    displayName: 'Test Provider',
    defaultVisible: true,
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
    setTimeout() { return ++timerSequence; },
    clearTimeout() { },
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
        $notify(options) {
            notificationCount++;
            lastNotificationOptions = options;
            let closed = false;
            return {
                close() {
                    if (closed) {
                        return;
                    }
                    closed = true;
                    notificationCloseCount++;
                    if (typeof options.onClose === 'function') {
                        options.onClose();
                    }
                }
            };
        }
    }, capturedMixin.data());
    viewModel.$root = viewModel;
    Object.keys(capturedMixin.methods).forEach(name => {
        viewModel[name] = capturedMixin.methods[name].bind(viewModel);
    });
    return viewModel;
}

async function flushPromises() {
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
}

async function run() {
    vm.runInContext(script, context, { filename: scriptPath });
    const firstConsole = window.NcfAdminConsole;

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
    assert.strictEqual(notificationCount, 0);

    // SSE 刷新后，新增提醒应显示持久弹窗并增加 Footer 徽标。
    stateProviders = [{
        providerId: 'test-provider',
        displayName: 'Test Provider',
        defaultVisible: true,
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
    assert.strictEqual(notificationCount, 1);
    assert.strictEqual(lastNotificationOptions.title, 'NeuBell test reminder');
    assert.strictEqual(lastNotificationOptions.duration, 0);
    assert.strictEqual(capturedMixin.computed.neuBellTotalCount.call(layoutRoot), 1);

    // 消费提醒后，SSE 刷新应主动关闭弹窗并清空徽标。
    stateProviders = [{
        providerId: 'test-provider',
        displayName: 'Test Provider',
        defaultVisible: true,
        items: []
    }];
    eventSources[0].emit('neubell-changed');
    await flushPromises();
    assert.strictEqual(stateRequestCount, 3);
    assert.strictEqual(notificationCloseCount, 1);
    assert.strictEqual(capturedMixin.computed.neuBellTotalCount.call(layoutRoot), 0);

    // 即使出现第二个独立 Vue 根实例，也不能增加 state/SSE 通讯通道。
    const duplicateLayoutRoot = createViewModel(layoutElement);
    capturedMixin.mounted.call(duplicateLayoutRoot);
    await flushPromises();
    assert.strictEqual(duplicateLayoutRoot.footerCommunicationOwner, false);
    assert.strictEqual(stateRequestCount, 3);
    assert.strictEqual(eventSourceCount, 1);

    capturedMixin.beforeDestroy.call(layoutRoot);
    assert.strictEqual(window.NcfAdminFooterRuntime.owner, null);
    assert.strictEqual(eventSourceCloseCount, 1);

    // 原 Owner 销毁后允许新的真实布局根接管，兼容局部导航和测试环境重新挂载。
    capturedMixin.mounted.call(duplicateLayoutRoot);
    await flushPromises();
    assert.strictEqual(duplicateLayoutRoot.footerCommunicationOwner, true);
    assert.strictEqual(stateRequestCount, 4);
    assert.strictEqual(eventSourceCount, 2);
    capturedMixin.beforeDestroy.call(duplicateLayoutRoot);
    assert.strictEqual(eventSourceCloseCount, 2);

    process.stdout.write('AdminFooter lifecycle tests passed.\n');
}

run().catch(error => {
    process.stderr.write(error.stack + '\n');
    process.exitCode = 1;
});
