'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const admin = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const components = {};
const Vue = function (options) {
    return Object.assign(options.data, { $el: { remove() {} }, $destroy() {} });
};
Vue.observable = value => value;
Vue.set = (object, key, value) => { object[key] = value; };
Vue.component = (name, component) => { components[name] = component; };
const window = { ncfT: key => key, dispatchEvent() {} };
const sandbox = {
    window, Vue, document: { createElement: () => ({}) },
    CustomEvent: function (type, options) { this.type = type; this.detail = options.detail; },
    service: {}
};
vm.runInNewContext(fs.readFileSync(path.join(admin, 'wwwroot/js/Admin/Shared/PromptReferences.js'), 'utf8'), sandbox);
const refs = window.NcfPromptReferences;
const clone = value => JSON.parse(JSON.stringify(value));
const reference = (id, start, length) => ({ promptItemId: id, promptCode: `code-${id}`, start, length });
const original = 'aaPROMPTbbOTHERcc';
const both = [reference(1, 2, 6), reference(2, 10, 5)];
assert.deepEqual(clone(refs.updateRanges(original, 'x' + original, both)), [reference(1, 3, 6), reference(2, 11, 5)]);
assert.deepEqual(clone(refs.updateRanges(original, 'aaPROxMPTbbOTHERcc', both)), [reference(2, 11, 5)]);
assert.deepEqual(clone(refs.updateRanges(original, 'aaPROMPTbbOTHxERcc', both)), [reference(1, 2, 6)]);
assert.deepEqual(clone(refs.updateRanges(original, 'aaPROMPTXbbOTHERcc', both)), [reference(1, 2, 6), reference(2, 11, 5)]);
assert.deepEqual(clone(refs.updateRanges(original, 'aaxcc', both)), []);
assert.deepEqual(clone(refs.updateRanges('aaaa', 'aaa', [reference(1, 0, 2), reference(2, 2, 2)], { start: 0, end: 1 })),
    [reference(2, 1, 2)], 'Identical text must use the edited occurrence, not a guessed suffix.');
const input = {
    value: '', listeners: {}, events: [],
    addEventListener(name, callback) { this.listeners[name] = callback; },
    removeEventListener(name) { delete this.listeners[name]; },
    dispatchEvent(event) { this.events.push(event); }
};
refs.attach(input, { appendChild() {} });
input.value = 'PROMPT';
refs.inserted(input, '', 0, 0, { id: 1, fullVersion: 'code-1', content: 'PROMPT' });
input.listeners.input();
assert.deepEqual(clone(refs.capture(input)), [reference(1, 0, 6)], 'Input dispatch must not double-shift inserted references.');
refs.inserted(input, 'PROMPT', 0, 6, { id: 1, fullVersion: 'code-1', content: 'PROMPT' });
assert.deepEqual(clone(refs.capture(input)), [reference(1, 0, 6)], 'Replacing a selected prompt with itself must not duplicate its reference.');
input.selectionStart = 3; input.selectionEnd = 3;
input.listeners.beforeinput({ inputType: 'insertText' });
input.value = 'PROxMPT'; input.listeners.input();
assert.equal(refs.capture(input).length, 0);
input.value = 'PROMPT'; input.listeners.input();
assert.equal(refs.capture(input).length, 0, 'Undo must not infer a cancelled reference.');
refs.restore(input, [reference(1, 0, 6)]);
input.value = 'other'; refs.sync(input);
assert.equal(refs.capture(input).length, 0, 'Programmatic replacement cancels annotations.');
assert.throws(() => refs.restore(input, [reference(1, 0, 6)]), /InvalidReference/);
assert.throws(() => refs.restore(input, [null]), /InvalidReference/);
assert.throws(() => refs.restore(input, [reference(1, 0, 1.5)]), /InvalidReference/);
assert.throws(() => refs.restore(input, {}), /InvalidReference/);
refs.detach(input);
assert.equal(Object.keys(input.listeners).length, 0);
assert.ok(components['prompt-reference-content'].template.includes('{{ part.text }}'), 'History content must render literal text.');
assert.ok(!components['prompt-reference-content'].template.includes('v-html'));
const entity = fs.readFileSync(path.join(admin, 'Domain/Models/DatabaseModel/AdminChatMessage.cs'), 'utf8');
assert.match(entity, /PromptReferencesJson/);
const chat = fs.readFileSync(path.join(admin, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js'), 'utf8');
assert.match(chat, /content: messageContent,\s+promptReferences/);
async function checkScorePreviewIsolation() {
    const options = components['prompt-reference-score-chart'];
    const chart = Object.assign(options.data(), {
        reference: reference(1, 0, 6), score: 6.4,
        $el: { querySelector: () => null }, $nextTick: callback => callback()
    });
    for (const [name, method] of Object.entries(options.methods)) chart[name] = method.bind(chart);
    Object.defineProperty(chart, 'axisMax', { get: () => options.computed.axisMax.call(chart) });
    chart.items = [{ id: 1, currentScore: 8 }, { id: 2, currentScore: 4 }];
    assert.equal(chart.height(chart.items[0]), '64%');
    assert.equal(chart.height(chart.items[1]), '40%');
    const resolves = new Map();
    sandbox.service.get = async (url, config) => {
        const id = config.params.id;
        if (url.endsWith('GetReferenceInfoAsync')) {
            return { data: { success: true, data: { promptItemId: id, currentScore: id === 1 ? 8 : 4 } } };
        }
        return new Promise(resolve => resolves.set(id, resolve));
    };
    sandbox.service.post = () => { throw new Error('Preview must never write scores'); };
    const first = chart.select(1);
    const second = chart.select(2);
    resolves.get(2)({ data: { success: true, data: { id: 2, content: 'Second' } } });
    await second;
    resolves.get(1)({ data: { success: true, data: { id: 1, content: 'Late first' } } });
    await first;
    assert.equal(chart.preview.id, 2, 'Late responses must not replace the most recently clicked preview.');
    assert.equal(chart.reference.promptItemId, 1);
    assert.equal(chart.score, 6.4);
    assert.equal(chart.height(chart.items[0]), '64%');
    assert.equal(refs.scores[1].currentScore, 8, 'Draft scores must not leak into the shared saved-score cache.');
    const failed = chart.select(3);
    resolves.get(3)({ data: { success: false, errorMessage: 'No access' } });
    await failed;
    assert.equal(chart.previewError, 'No access');
    assert.equal(chart.preview, null);
    const previewGet = sandbox.service.get;
    const select = chart.select;
    chart.select = async () => {};
    refs.scores[2] = { promptItemId: 2, currentScore: 1 };
    sandbox.service.get = async () => ({ data: { success: true, data: chart.items } });
    await chart.reload();
    assert.equal(refs.scores[2].currentScore, 4, 'A fresh Range read must replace an unchanged stale cache.');
    let finishRange;
    sandbox.service.get = () => new Promise(resolve => { finishRange = resolve; });
    const rangeLoading = chart.reload();
    refs.scores[2] = { promptItemId: 2, currentScore: 9 };
    finishRange({ data: { success: true, data: chart.items } });
    await rangeLoading;
    assert.equal(chart.columnScore(chart.items[1]), 9, 'A Range read must not overwrite a newer completed score.');
    chart.select = select;
    sandbox.service.get = previewGet;
    const closed = chart.select(4);
    options.beforeDestroy.call(chart);
    resolves.get(4)({ data: { success: true, data: { id: 4 } } });
    await closed;
    assert.equal(chart.preview, null, 'Disposed dialogs must ignore outstanding responses.');
}

checkScorePreviewIsolation().then(() => {
    console.log('Prompt reference tracking and score preview tests passed.');
}).catch(error => { console.error(error); process.exitCode = 1; });
