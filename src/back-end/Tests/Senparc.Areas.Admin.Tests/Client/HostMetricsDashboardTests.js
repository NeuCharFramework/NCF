'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/Index/Index.js');
const page = fs.readFileSync(path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml'), 'utf8');
let options;
let chartOptions;
let createCount = 0;
const liveSamples = [];
const historyUpdates = [];
const intervals = [];
const clearedTimers = [];
let disposed = false;
let selectedRange;
let resetCount = 0;
let returnCount = 0;

const chart = {
    addLiveSample(point) { liveSamples.push(point); },
    update(payload) { historyUpdates.push(payload); },
    setRange(range) { selectedRange = range; },
    reset() { resetCount++; },
    returnToLive() { returnCount++; },
    dispose() { disposed = true; }
};
const document = {
    hidden: false,
    getElementById(id) { return id === 'resourceMonitorChart' ? { id } : null; },
    querySelector() { return null; },
    querySelectorAll() { return []; }
};
const window = {
    ChatLauncherMixin: {},
    ResourceMonitorChart: {
        create(element, config) {
            assert.strictEqual(element.id, 'resourceMonitorChart');
            chartOptions = config;
            createCount++;
            return chart;
        }
    },
    setInterval(handler, delay) {
        intervals.push(delay);
        return intervals.length;
    },
    clearInterval(timer) { clearedTimers.push(timer); }
};
function Vue(value) { options = value; return value; }
let hostResponse;
let historyResponse;
let hostFailure;
let historyFailure;
const urls = [];
const service = {
    async get(url) {
        urls.push(url);
        if (url.endsWith('GetHostMetrics')) {
            if (hostFailure) throw hostFailure;
            return { data: { data: hostResponse } };
        }
        if (url.endsWith('GetResourceMonitor')) {
            if (historyFailure) throw historyFailure;
            return { data: { data: historyResponse } };
        }
        throw new Error('Unexpected request: ' + url);
    }
};
const context = vm.createContext({
    window, document, Vue, service,
    ncfT(key) { return key; },
    console: { log() {}, warn() {}, error() {} },
    Date, Math, Number, Object, Array, Error, Promise, String, setTimeout() {}
});
vm.runInContext(fs.readFileSync(scriptPath, 'utf8'), context, { filename: scriptPath });
assert.ok(options, 'Vue page options should be captured.');

function createViewModel() {
    const viewModel = Object.assign({}, options.data());
    Object.keys(options.methods).forEach(name => {
        viewModel[name] = options.methods[name].bind(viewModel);
    });
    return viewModel;
}

async function run() {
    const viewModel = createViewModel();
    hostResponse = {
        sampledAt: '2026-08-06T08:00:02Z',
        hostName: 'test-host',
        operatingSystem: 'Test OS',
        cpuUsagePercent: 12.34,
        processCpuUsagePercent: 2.5,
        memoryUsagePercent: 51.2,
        processWorkingSetBytes: 1024 * 1024 * 1024,
        memoryTotalBytes: 32 * 1024 * 1024 * 1024,
        networkReceiveBytesPerSecond: 125000,
        networkSendBytesPerSecond: 250000,
        warnings: []
    };
    await viewModel.fetchHostMetrics();
    assert.ok(urls[0].endsWith('/Areas.Admin_StatAppService.GetHostMetrics'));
    assert.strictEqual(viewModel.hostMetrics.hostName, 'test-host');
    assert.strictEqual(createCount, 1);
    assert.strictEqual(chartOptions.rangeMs, 30 * 60 * 1000);
    assert.strictEqual(liveSamples.length, 1);
    assert.strictEqual(liveSamples[0].t, Date.parse(hostResponse.sampledAt));
    assert.strictEqual(liveSamples[0].cpu, 12.34);
    assert.strictEqual(liveSamples[0].pcpu, 2.5);
    assert.strictEqual(liveSamples[0].pmem, 3.125);
    assert.strictEqual(liveSamples[0].rx, 1);
    assert.strictEqual(liveSamples[0].tx, 2);

    historyResponse = { now: Date.parse(hostResponse.sampledAt), windows: [
        { resolutionSeconds: 10, points: [{ t: Date.parse(hostResponse.sampledAt), cpu: 11 }] },
        { resolutionSeconds: 60, points: [] },
        { resolutionSeconds: 300, points: [] }
    ] };
    await viewModel.fetchResourceMonitor();
    assert.ok(urls[1].endsWith('/Areas.Admin_StatAppService.GetResourceMonitor'));
    assert.strictEqual(historyUpdates[0], historyResponse);
    assert.strictEqual(createCount, 1, 'Both endpoints must update the same chart.');
    assert.strictEqual(viewModel.resourceMonitorError, '');
    chartOptions.onViewChange({ start: 100, end: 200, live: false, zoomed: true });
    assert.strictEqual(viewModel.resourceMonitorStart, 100);
    assert.strictEqual(viewModel.resourceMonitorEnd, 200);
    assert.strictEqual(viewModel.resourceMonitorLive, false);
    assert.strictEqual(viewModel.resourceMonitorZoomed, true);
    viewModel.selectResourceMonitorRange(86400000);
    viewModel.resetResourceMonitorZoom();
    viewModel.returnResourceMonitorToLive();
    assert.strictEqual(selectedRange, 86400000);
    assert.strictEqual(resetCount, 1);
    assert.strictEqual(returnCount, 1);

    assert.strictEqual(viewModel.formatPercent(12.34), '12.3%');
    assert.strictEqual(viewModel.barWidth(12.34), '12.340%');
    assert.strictEqual(viewModel.boundedSubsetWidth(20, 12.34), '12.340%');
    assert.strictEqual(viewModel.formatBytes(1024 * 1024), '1.00 MB');
    assert.strictEqual(viewModel.formatRate(null), '--');
    assert.strictEqual(viewModel.formatDuration(90061), '1d 1h 1m');
    assert.strictEqual(viewModel.processMemoryPercent(), 3.125);
    assert.strictEqual((page.match(/class="host-comparison-bar"/g) || []).length, 2);
    assert.ok(page.includes('@AR["Admin.Home.HostScope"]'));
    assert.ok(page.includes('@AR["Admin.Home.WebScope"]'));

    historyFailure = new Error('History unavailable');
    await viewModel.fetchResourceMonitor();
    assert.strictEqual(viewModel.resourceMonitorError, 'History unavailable');
    assert.strictEqual(viewModel.hostMetricsError, '');
    historyFailure = null;
    hostResponse = { ...hostResponse, sampledAt: 'not-a-date' };
    await viewModel.fetchHostMetrics();
    assert.strictEqual(viewModel.hostMetricsError, 'Admin.Home.HostMetricsInvalidResponse');
    assert.strictEqual(liveSamples.length, 1);
    historyResponse = { now: Date.now(), windows: [{ resolutionSeconds: 10, points: null }] };
    await viewModel.fetchResourceMonitor();
    assert.strictEqual(viewModel.resourceMonitorError, 'Admin.Home.ResourceMonitorUnavailable');

    document.hidden = true;
    const count = urls.length;
    await viewModel.fetchHostMetrics();
    await viewModel.fetchResourceMonitor();
    assert.strictEqual(urls.length, count, 'Hidden pages must not poll either endpoint.');
    document.hidden = false;
    viewModel.startHostMetricsPolling();
    viewModel.startResourceMonitorPolling();
    assert.deepStrictEqual(intervals, [2000, 10000]);
    options.beforeDestroy.call(viewModel);
    assert.deepStrictEqual(clearedTimers, [1, 2]);
    assert.strictEqual(disposed, true);
    console.log('Merged host metrics dashboard polling and error tests passed.');
}

run().catch(error => {
    console.error(error);
    process.exitCode = 1;
});
