'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/Index/Index.js');
const script = fs.readFileSync(scriptPath, 'utf8');

let capturedOptions = null;
let requestedUrl = null;
let intervalDelay = null;
let clearedTimer = null;
let removedResizeHandler = null;
let disposeCount = 0;

const samples = [{
    sampledAt: '2026-08-06T08:00:00Z',
    hostName: 'test-host',
    operatingSystem: 'Test OS',
    cpuUsagePercent: null,
    memoryUsagePercent: 50,
    networkReceiveBytesPerSecond: null,
    networkSendBytesPerSecond: null,
    warnings: []
}, {
    sampledAt: '2026-08-06T08:00:02Z',
    hostName: 'test-host',
    operatingSystem: 'Test OS',
    cpuUsagePercent: 12.34,
    memoryUsagePercent: 51.2,
    networkReceiveBytesPerSecond: 125000,
    networkSendBytesPerSecond: 250000,
    warnings: []
}];

const document = {
    hidden: false,
    getElementById() { return null; },
    querySelector() { return null; },
    querySelectorAll() { return []; }
};

const window = {
    ChatLauncherMixin: {},
    setInterval(handler, delay) {
        intervalDelay = delay;
        return 42;
    },
    clearInterval(timer) {
        clearedTimer = timer;
    },
    addEventListener() { },
    removeEventListener(name, handler) {
        if (name === 'resize') {
            removedResizeHandler = handler;
        }
    }
};

function Vue(options) {
    capturedOptions = options;
    return options;
}

const service = {
    async get(url) {
        requestedUrl = url;
        return { data: { data: samples.shift() } };
    }
};

const context = vm.createContext({
    window,
    document,
    Vue,
    service,
    echarts: { init() { throw new Error('Chart should not initialize without a DOM element.'); } },
    ncfT(key) { return key; },
    console: { log() { }, warn() { }, error() { } },
    Date,
    Math,
    Number,
    Object,
    Array,
    Error,
    Promise,
    String,
    setTimeout() { }
});

function createViewModel() {
    const viewModel = Object.assign({
        $nextTick(callback) { callback(); }
    }, capturedOptions.data());
    Object.keys(capturedOptions.methods).forEach(name => {
        viewModel[name] = capturedOptions.methods[name].bind(viewModel);
    });
    return viewModel;
}

async function run() {
    vm.runInContext(script, context, { filename: scriptPath });
    assert.ok(capturedOptions, 'Vue page options should be captured.');

    const viewModel = createViewModel();
    await viewModel.fetchHostMetrics();
    assert.strictEqual(
        requestedUrl,
        '/api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.GetHostMetrics');
    assert.strictEqual(viewModel.hostMetrics.hostName, 'test-host');
    assert.strictEqual(viewModel.hostMetricsHistory.length, 1);
    assert.strictEqual(viewModel.hostMetricsHistory[0].cpu, null);

    await viewModel.fetchHostMetrics();
    assert.strictEqual(viewModel.hostMetricsHistory.length, 2);
    assert.strictEqual(viewModel.hostMetricsHistory[1].cpu, 12.34);
    assert.strictEqual(viewModel.hostMetricsHistory[1].receiveMbps, 1);
    assert.strictEqual(viewModel.hostMetricsHistory[1].sendMbps, 2);
    assert.strictEqual(viewModel.formatPercent(12.34), '12.3%');
    assert.strictEqual(viewModel.formatBytes(1024 * 1024), '1.00 MB');
    assert.strictEqual(viewModel.formatRate(null), '--');
    assert.strictEqual(viewModel.formatDuration(90061), '1d 1h 1m');

    for (let index = 0; index < 35; index++) {
        viewModel.appendHostMetricsHistory({
            sampledAt: new Date(2026, 7, 6, 8, 0, index).toISOString(),
            cpuUsagePercent: index,
            memoryUsagePercent: index,
            networkReceiveBytesPerSecond: index,
            networkSendBytesPerSecond: index
        });
    }
    assert.strictEqual(viewModel.hostMetricsHistory.length, 30);

    viewModel.startHostMetricsPolling();
    assert.strictEqual(intervalDelay, 2000);
    assert.strictEqual(viewModel.hostMetricsTimer, 42);

    const resizeHandler = () => { };
    viewModel.hostMetricsResizeHandler = resizeHandler;
    viewModel.hostMetricsChart = {
        dispose() { disposeCount++; }
    };
    capturedOptions.beforeDestroy.call(viewModel);
    assert.strictEqual(clearedTimer, 42);
    assert.strictEqual(removedResizeHandler, resizeHandler);
    assert.strictEqual(disposeCount, 1);

    process.stdout.write('Host metrics dashboard tests passed.\n');
}

run().catch(error => {
    process.stderr.write(error.stack + '\n');
    process.exitCode = 1;
});
