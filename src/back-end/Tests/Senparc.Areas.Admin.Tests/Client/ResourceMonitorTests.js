'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const admin = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const scriptPath = path.join(admin, 'wwwroot/js/Admin/Pages/Index/ResourceMonitor.js');
const page = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/Index.cshtml'), 'utf8');
const chartScript = fs.readFileSync(scriptPath, 'utf8');

function createHarness() {
    let option;
    let lastView;
    let disposed = false;
    let resizeCount = 0;
    let observer;
    const elementListeners = {};
    const windowListeners = {};
    const chartListeners = {};
    const children = [];
    const element = {
        style: {},
        clientWidth: 1064,
        clientHeight: 340,
        getBoundingClientRect() { return { left: 0, top: 0 }; },
        appendChild(child) { children.push(child); child.parentNode = this; },
        removeChild(child) { children.splice(children.indexOf(child), 1); child.parentNode = null; },
        addEventListener(name, handler) { elementListeners[name] = handler; },
        removeEventListener(name) { delete elementListeners[name]; }
    };
    const window = {
        addEventListener(name, handler) { windowListeners[name] = handler; },
        removeEventListener(name) { delete windowListeners[name]; },
        ResizeObserver: class {
            constructor(callback) { this.callback = callback; observer = this; }
            observe(target) { this.target = target; }
            disconnect() { this.disconnected = true; }
        }
    };
    vm.runInNewContext(chartScript, {
        window,
        document: { createElement() { return { style: {} }; } },
        echarts: {
            init() {
                return {
                    on(name, handler) { chartListeners[name] = handler; },
                    setOption(value) {
                        value.series.forEach(series => series.data.forEach(point => {
                            assert.ok(Array.isArray(point));
                            assert.notStrictEqual(point[1], null, 'ECharts 3 cannot consume a null item.');
                            assert.notStrictEqual(point[1], undefined);
                        }));
                        option = value;
                    },
                    resize() { resizeCount++; },
                    dispose() { disposed = true; }
                };
            }
        }
    }, { filename: scriptPath });
    const monitor = window.ResourceMonitorChart.create(element, {
        i18n: { empty: 'No samples', cpuHost: 'Host CPU', cpuWeb: 'Web CPU',
            memHost: 'Host memory', memWeb: 'Web memory', rx: 'Receive', tx: 'Send' },
        onViewChange(view) { lastView = view; }
    });
    return {
        monitor, children, elementListeners, windowListeners, chartListeners,
        get option() { return option; },
        get view() { return lastView; },
        get disposed() { return disposed; },
        get resizeCount() { return resizeCount; },
        get observer() { return observer; },
        element
    };
}

const now = Date.UTC(2026, 10, 2, 12);
const minute = 60 * 1000;
const hour = 60 * minute;
const h = createHarness();
h.monitor.update({ now, windows: [] });
assert.strictEqual(h.option.xAxis.type, 'time', 'The horizontal axis must be linear time.');
assert.strictEqual(h.option.xAxis.min, now - 30 * minute);
assert.strictEqual(h.option.xAxis.max, now);
assert.strictEqual(h.option.series.length, 6);
assert.strictEqual(h.option.yAxis[0].name, '%');
assert.strictEqual(h.option.yAxis[1].name, 'Mbps');
assert.ok(h.option.series.slice(4).every(series => series.yAxisIndex === 1));
assert.strictEqual(h.option.series[0].itemStyle.normal.color, '#8c52ff');
assert.strictEqual(h.option.series[1].lineStyle.normal.type, 'dashed');
assert.strictEqual(h.option.series[4].lineStyle.normal.color, '#409eff');
assert.strictEqual(h.children[0].style.display, 'flex', 'An empty cache must display the empty state.');

h.monitor.addLiveSample({ t: now, cpu: 20, pcpu: 0, mem: 50, pmem: null, rx: 1, tx: 2 });
assert.strictEqual(h.children[0].style.display, 'none');
assert.strictEqual(h.option.series[0].data[0][1], 20);
assert.strictEqual(h.option.series[1].data[0][1], 0, 'Zero is a valid sample.');
assert.strictEqual(h.option.series[3].data[0][1], '-', 'Missing values use the ECharts 3 sentinel.');
assert.strictEqual(h.option.series[0].showSymbol, true, 'A single sample should be visible.');

const payload = {
    now,
    windows: [
        { resolutionSeconds: 300, points: [
            { t: now - 12 * hour, cpu: 12, rx: 0.1 },
            { t: now - 5 * hour, cpu: 99 }
        ] },
        { resolutionSeconds: 60, points: [
            { t: now - 5 * hour, cpu: 50, rx: 0.5 },
            { t: now - 20 * minute, cpu: 99 }
        ] },
        { resolutionSeconds: 10, points: [
            { t: now - 20 * minute, cpu: 30, rx: 0.3 },
            { t: now, cpu: 99 }
        ] }
    ]
};
h.monitor.update(payload);
h.monitor.setRange(24 * hour);
assert.deepStrictEqual(Array.from(h.option.series[0].data, item => item[1]).filter(value => value !== '-'),
    [12, 50, 30, 20],
    'Overlapping tiers must prefer live, then 10-second, minute and five-minute samples.');
assert.deepStrictEqual(Array.from(h.option.series[4].data, item => item[1]).filter(value => value !== '-'),
    [0.1, 0.5, 0.3, 1],
    'Historical and live network rates must be retained in Mbps.');
assert.ok(h.option.series[0].data.some(item => item[1] === '-'),
    'Recording gaps must not be joined into a misleading continuous line.');
assert.strictEqual(h.option.xAxis.max - h.option.xAxis.min, 24 * hour);
h.monitor.setRange(5 * minute);
assert.strictEqual(h.option.xAxis.max - h.option.xAxis.min, 5 * minute);
assert.deepStrictEqual(Array.from(h.option.series[0].data, item => item[1]), [20]);
h.monitor.setRange(6 * hour);
assert.strictEqual(h.option.xAxis.max - h.option.xAxis.min, 6 * hour);

const tooltip = h.option.tooltip.formatter([
    { value: [now, '-'], seriesIndex: 0, seriesName: 'Missing', marker: '' },
    { value: [now, 0], seriesIndex: 1, seriesName: 'Zero', marker: '' },
    { value: [now, 2.5], seriesIndex: 4, seriesName: 'Receive', marker: '' }
]);
assert.ok(!tooltip.includes('Missing'));
assert.ok(!tooltip.includes('NaN'));
assert.ok(!tooltip.includes('undefined'), 'ECharts 3 does not provide a marker field.');
assert.ok(tooltip.includes('Zero: 0.0%'));
assert.ok(tooltip.includes('Receive: 2.5 Mbps'));
assert.ok(tooltip.includes('#409eff'));

h.chartListeners.legendselectchanged({ selected: { 'Host CPU': false, 'Receive': true } });
h.monitor.setRange(30 * minute);
assert.strictEqual(h.option.legend.selected['Host CPU'], false);

let prevented = false;
h.elementListeners.wheel({ clientX: 500, clientY: 10, deltaY: -1,
    preventDefault() { prevented = true; } });
assert.strictEqual(prevented, false, 'Wheel events outside the plotting area must not be captured.');
h.elementListeners.wheel({ clientX: 500, clientY: 150, deltaY: -1,
    preventDefault() { prevented = true; } });
assert.strictEqual(prevented, true);
assert.strictEqual(h.view.live, false);
assert.strictEqual(h.view.zoomed, true);
assert.strictEqual(h.view.end - h.view.start, 24 * minute);
const pausedStart = h.view.start;
const pausedEnd = h.view.end;
h.monitor.addLiveSample({ t: now + 2000, cpu: 21 });
h.monitor.update({ ...payload, now: now + 10000 });
assert.strictEqual(h.view.start, pausedStart, 'Polling must not move a paused viewport.');
assert.strictEqual(h.view.end, pausedEnd);
h.monitor.reset();
assert.strictEqual(h.view.live, false, 'Resetting the range must not silently resume live following.');
assert.strictEqual(h.view.zoomed, false);
assert.strictEqual(h.view.end - h.view.start, 30 * minute);
h.monitor.returnToLive();
assert.strictEqual(h.view.live, true);
assert.strictEqual(h.view.end, now + 10000);

h.elementListeners.mousedown({ button: 0, clientX: 400, clientY: 150, preventDefault() {} });
h.elementListeners.mousemove({ clientX: 700, clientY: 150 });
assert.strictEqual(h.children[1].style.display, 'block');
h.windowListeners.mouseup({ clientX: 700, clientY: 150 });
assert.strictEqual(h.view.live, false, 'Dragging a valid range must pause live following.');
assert.strictEqual(h.view.zoomed, true);
assert.strictEqual(h.children[1].style.display, 'none');
h.monitor.returnToLive();
assert.strictEqual(h.view.live, true);

for (let i = 0; i < 50; i++) {
    h.elementListeners.wheel({ clientX: 500, clientY: 150, deltaY: -1, preventDefault() {} });
}
assert.ok(h.view.end - h.view.start >= 30 * 1000, 'Zoom must not go below 30 seconds.');
for (let i = 0; i < 100; i++) {
    h.elementListeners.wheel({ clientX: 500, clientY: 150, deltaY: 1, preventDefault() {} });
}
assert.ok(h.view.end - h.view.start <= 24 * hour, 'Zoom must not exceed the 24-hour retention window.');
assert.ok(h.view.end <= now + 10000);
assert.ok(h.view.start >= now + 10000 - 24 * hour);

h.windowListeners.resize();
assert.strictEqual(h.resizeCount, 1);
assert.strictEqual(h.observer.target, h.element);
h.observer.callback();
assert.strictEqual(h.resizeCount, 2, 'Sidebar/layout resizing must resize the chart.');
h.monitor.dispose();
assert.strictEqual(h.disposed, true);
assert.strictEqual(h.observer.disconnected, true);
assert.strictEqual(h.children.length, 0);
assert.deepStrictEqual(Object.keys(h.windowListeners), []);
assert.deepStrictEqual(Object.keys(h.elementListeners), []);

const live = createHarness();
live.monitor.update({ now, windows: [] });
live.monitor.setRange(5 * minute);
for (let index = 0; index < 180; index++) {
    live.monitor.addLiveSample({ t: now + index * 2000, cpu: index });
}
assert.strictEqual(live.option.series[0].data.length, 151, 'Live samples must be bounded to five minutes.');
assert.strictEqual(live.option.series[0].data[0][0], now + 29 * 2000);
assert.strictEqual(live.option.series[0].data[150][0], now + 179 * 2000);
live.monitor.dispose();

assert.strictEqual((page.match(/class="host-metrics-card"/g) || []).length, 1,
    'Realtime and historical monitoring must use one card.');
assert.strictEqual((page.match(/id="resourceMonitorChart"/g) || []).length, 1);
assert.ok(!page.includes('id="hostMetricsChart"'));
assert.ok(page.includes('class="resource-monitor-chart-scroll"'));
assert.ok(page.includes('min-width: 280px'));
for (const range of [300000, 1800000, 21600000, 86400000]) {
    assert.ok(page.includes(`:label="${range}"`), `Missing range ${range}.`);
}
for (const culture of ['', '.zh-CN', '.en', '.ja', '.fr', '.es', '.ru']) {
    const resource = fs.readFileSync(path.join(admin, `Resources/AdminResource${culture}.resx`), 'utf8');
    for (const suffix of ['Range', '5Minutes', '30Minutes', '6Hours', '24Hours',
        'ReturnLive', 'Following', 'Paused']) {
        assert.ok(resource.includes(`name="Admin.Home.ResourceMonitor${suffix}"`),
            `Missing ${suffix} translation in ${culture || 'default'} resources.`);
    }
}
console.log('Merged resource monitor timeline, data precedence, zoom and localization tests passed.');
