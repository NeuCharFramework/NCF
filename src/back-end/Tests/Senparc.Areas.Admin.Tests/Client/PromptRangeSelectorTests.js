'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const admin = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const source = fs.readFileSync(path.join(admin, 'wwwroot/js/Admin/Shared/PromptRangeSelector.js'), 'utf8');
const requests = [];
let directive;
let picker;
const defaultHandler = async method => {
    if (method === 'GetSelectorOptionsAsync') return [
        { rangeId: 1, rangeName: 'range', rangeAlias: 'Support', tactic: '1' },
        { rangeId: 1, rangeName: 'range', rangeAlias: 'Support', tactic: '1.1' },
        { rangeId: 2, rangeName: 'other', tactic: '2' }
    ];
    if (method === 'SearchSelectorAsync') return { items: [{ id: 7, content: 'preview' }], totalCount: 1 };
    return { id: 7, content: 'FULL <script>literal</script>\n{{variable}}' };
};
let handler = defaultHandler;
function Vue(options) {
    picker = Object.assign(options.data(), { $nextTick: callback => callback() });
    for (const [name, method] of Object.entries(options.methods)) picker[name] = method.bind(picker);
    for (const [name, getter] of Object.entries(options.computed)) {
        Object.defineProperty(picker, name, { get: getter.bind(picker) });
    }
    return picker;
}
Vue.directive = (name, value) => {
    assert.strictEqual(name, 'prompt-range');
    directive = value;
};
Vue.nextTick = callback => callback();
function element() {
    return {
        classList: { add() {}, remove() {} },
        listeners: {},
        setAttribute() {},
        addEventListener(name, callback) {
            this.listeners[name] = event => callback(event || { stopPropagation() {}, preventDefault() {} });
        },
        appendChild(child) { this.child = child; },
        remove() { this.removed = true; }
    };
}
const sandbox = {
    Vue, Event: class { constructor(type, options) { this.type = type; this.bubbles = options.bubbles; } },
    window: { ncfT: key => key, addEventListener() {} },
    document: { createElement: element, body: element() },
    service: { async get(url, config) {
        const method = url.split('.').pop();
        requests.push({ method, config });
        return { data: { success: true, data: await handler(method, config.params) } };
    } }
};
vm.runInNewContext(source, sandbox);

function inputHost(value) {
    const input = {
        isConnected: true, value, selectionStart: 2, selectionEnd: 4, maxLength: -1,
        focus() { this.focused = true; },
        setSelectionRange(start, end) { this.selectionStart = start; this.selectionEnd = end; },
        dispatchEvent(event) { this.event = event; this.modelValue = this.value; }
    };
    const host = Object.assign(element(), {
        querySelector() { return input; }, contains(value) { return value === input; }
    });
    directive.inserted(host, {});
    return { host, input, button: host.child };
}
const flush = () => new Promise(resolve => setImmediate(resolve));
function deferred() {
    let resolve;
    const promise = new Promise(done => { resolve = done; });
    return { promise, resolve };
}

(async () => {
    assert.strictEqual(requests.length, 0, 'No API traffic before opening the picker.');
    const first = inputHost('abcdef');
    assert.strictEqual(first.button.disabled, false);
    let stopped = false;
    first.button.listeners.keydown({ stopPropagation() { stopped = true; } });
    assert.ok(stopped, 'Picker shortcuts must not bubble to the chat send handler.');
    first.button.listeners.click();
    await flush();
    assert.strictEqual(picker.sortBy, 'score');
    assert.strictEqual(requests.at(-1).config.params.sortBy, 'score');
    assert.strictEqual(picker.formatScore(0), '0.0');
    assert.strictEqual(picker.formatScore(9.86), '9.9');
    for (const score of [-1, null, undefined]) {
        assert.strictEqual(picker.formatScore(score), 'PromptSelector.Unrated');
    }
    picker.sortBy = 'promptCode';
    await picker.search(1);
    assert.strictEqual(requests.at(-1).config.params.sortBy, 'promptCode');
    assert.strictEqual(picker.page, 1);
    assert.strictEqual(picker.ranges.length, 2);
    picker.rangeId = 1;
    assert.strictEqual(picker.tactics.length, 2);
    picker.keyword = '  search  ';
    picker.tactic = '1';
    await picker.search(3);
    const query = requests.at(-1);
    assert.strictEqual(query.config.params.keyword, 'search');
    assert.strictEqual(query.config.params.pageIndex, 3);
    assert.strictEqual(query.config.params.tactic, '1');
    assert.strictEqual(query.config.params.sortBy, 'promptCode', 'Paging preserves the selected sort.');
    picker.changeRange();
    await flush();
    assert.strictEqual(picker.tactic, '');
    assert.strictEqual(requests.at(-1).config.params.pageIndex, 1);
    await picker.select({ id: 7 });
    picker.insert();
    assert.strictEqual(first.input.modelValue, 'abFULL <script>literal</script>\n{{variable}}ef');
    assert.strictEqual(first.input.event.type, 'input');
    assert.strictEqual(first.input.event.bubbles, true);
    assert.strictEqual(first.input.selectionStart, first.input.value.length - 2);
    picker.close();

    const second = inputHost('second');
    second.input.selectionStart = 6;
    second.input.selectionEnd = 6;
    second.button.listeners.click();
    await flush();
    assert.strictEqual(picker.sortBy, 'score', 'Reopening defaults to score sorting.');
    await picker.select({ id: 7 });
    second.input.value = 'external edit';
    picker.insert();
    assert.strictEqual(picker.error, 'PromptSelector.TargetChanged');
    assert.strictEqual(second.input.value, 'external edit');
    picker.visible = false;
    picker.close();
    second.button.listeners.click();
    await flush();
    await picker.select({ id: 7 });
    second.input.maxLength = 12;
    picker.insert();
    assert.strictEqual(picker.error, 'PromptSelector.TooLong');
    second.input.maxLength = -1;
    second.input.readOnly = true;
    directive.componentUpdated(second.host, {});
    assert.strictEqual(second.button.disabled, true);
    picker.insert();
    assert.strictEqual(picker.error, 'PromptSelector.TargetChanged');
    second.input.readOnly = false;
    directive.componentUpdated(second.host, { value: false });
    assert.strictEqual(second.button.disabled, true);
    picker.insert();
    assert.strictEqual(picker.error, 'PromptSelector.TargetChanged');
    directive.componentUpdated(second.host, { value: true });
    second.input.disabled = true;
    second.button.disabled = true;
    let afterRender;
    Vue.nextTick = callback => { afterRender = callback; };
    directive.componentUpdated(second.host, { value: true });
    second.input.disabled = false;
    afterRender();
    assert.strictEqual(second.button.disabled, false, 'Read native disabled state after Element UI renders.');
    Vue.nextTick = callback => callback();
    picker.visible = false;
    picker.close();

    second.button.listeners.click();
    await flush();
    const pending = deferred();
    handler = method => method === 'SearchSelectorAsync'
        ? pending.promise : Promise.resolve({ id: 8, content: 'new selection' });
    const oldSearch = picker.search(1);
    handler = async () => ({ items: [{ id: 8 }], totalCount: 1 });
    await picker.search(2);
    pending.resolve({ items: [{ id: 99 }], totalCount: 99 });
    await oldSearch;
    assert.strictEqual(picker.items[0].id, 8, 'A stale search must not replace the new page.');
    const pendingDetail = deferred();
    handler = () => pendingDetail.promise;
    const oldDetail = picker.select({ id: 99 });
    handler = async () => ({ id: 8, content: 'latest' });
    await picker.select({ id: 8 });
    pendingDetail.resolve({ id: 99, content: 'stale' });
    await oldDetail;
    assert.strictEqual(picker.selected.id, 8, 'A stale detail must not become insertable.');
    directive.unbind(second.host);
    assert.strictEqual(picker.visible, false);
    assert.strictEqual(second.button.removed, true);
    assert.strictEqual(second.input.value, 'external edit');

    handler = defaultHandler;
    first.button.listeners.click();
    await flush();
    for (const [status, key] of [[404, 'Unavailable'], [401, 'AccessDenied'], [403, 'AccessDenied']]) {
        handler = async () => { throw { response: { status } }; };
        await picker.search(1);
        assert.strictEqual(picker.error, 'PromptSelector.' + key);
        assert.strictEqual(picker.selected, null);
        assert.strictEqual(picker.loading, false);
    }
    handler = async () => { throw new Error('offline'); };
    await picker.search(1);
    assert.match(picker.error, /LoadFailed.*offline/);
    const originalGet = sandbox.service.get;
    sandbox.service.get = async () => ({ data: { success: false, errorMessage: 'Application failure' } });
    await picker.search(1);
    assert.match(picker.error, /Application failure/);
    assert.strictEqual(picker.items.length, 0);
    sandbox.service.get = originalGet;
    const closing = deferred();
    handler = () => closing.promise;
    const closingSearch = picker.search(1);
    picker.visible = false;
    picker.close();
    closing.resolve({ items: [{ id: 99 }], totalCount: 1 });
    await closingSearch;
    assert.strictEqual(picker.items.length, 0, 'A closed picker must ignore in-flight responses.');
    assert.strictEqual(first.input.modelValue, first.input.value);
    directive.unbind(first.host);

    const layout = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/Shared/_Layout_Vue.cshtml'), 'utf8');
    assert.ok(layout.indexOf('PromptRangeSelector.js') < layout.indexOf('@RenderSection("scripts"'));
    const chat = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'), 'utf8');
    assert.match(chat, /v-model="inputMessage"\s+ref="promptMessageInput"\s+v-prompt-range="!isSending"/);
    assert.match(chat, /v-model="chatInputText"\s+ref="promptLauncherInput"\s+v-prompt-range="!isCreatingSession"/);
    assert.ok(!source.includes('v-html'), 'Prompt content must be rendered as literal text.');
    assert.match(source, /v-model="sortBy"[\s\S]*?@change="search\(1\)"/);
    for (const record of ['item', 'selected']) {
        assert.ok(source.includes(`formatScore(${record}.currentScore)`));
    }
    for (const language of ['', '.en']) {
        const resources = fs.readFileSync(path.join(admin, 'Resources/AdminResource' + language + '.resx'), 'utf8');
        for (const match of source.matchAll(/\bt\('(\w+)'\)/g)) {
            assert.ok(resources.includes('name="PromptSelector.' + match[1] + '"'), 'Missing localized label: ' + match[1]);
        }
    }
    console.log('PromptRange selector client tests passed.');
})().catch(error => { console.error(error); process.exitCode = 1; });
