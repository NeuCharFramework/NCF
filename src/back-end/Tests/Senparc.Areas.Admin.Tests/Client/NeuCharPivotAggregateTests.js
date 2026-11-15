'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const commonPath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/NeuCharPivot/common.js');
const aggregatePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/NeuCharPivot/Aggregate.js');
const globalFunctionPath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/GlobalNeuCharPivot.js');
const pagePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/NeuCharPivot/Aggregate.cshtml');
const homePagePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml');
const homeScriptPath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/Index/Index.js');
const boardPagePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/NeuCharPivot/Board.cshtml');
const boardScriptPath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/NeuCharPivot/Board.js');
const sandboxPagePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.Sandbox/Areas/Admin/Pages/Sandbox/Index.cshtml');

let receivedValue = null;
let receivedOptions = null;
const sandbox = {
    window: {
        DOMPurify: {
            sanitize(value, options) {
                receivedValue = value;
                receivedOptions = options;
                return '<p>safe</p>';
            }
        }
    },
    console
};
vm.createContext(sandbox);
vm.runInContext(fs.readFileSync(commonPath, 'utf8'), sandbox, { filename: commonPath });

const html = sandbox.window.NeuCharPivotUi.sanitizeHtml(
    '<p onclick="attack()">safe</p><script>attack()</script>');
assert.strictEqual(html, '<p>safe</p>');
assert.ok(receivedValue.includes('<script>'), 'The complete Function result must be sent to the sanitizer.');
assert.ok(receivedOptions.ALLOWED_TAGS.includes('table'), 'Safe rich-text tables should remain available.');
assert.ok(!receivedOptions.ALLOWED_TAGS.includes('script'), 'Executable elements must not be allowed.');
assert.deepStrictEqual(Array.from(receivedOptions.ALLOWED_ATTR), ['href', 'title']);
assert.strictEqual(receivedOptions.ALLOW_ARIA_ATTR, false);
assert.strictEqual(receivedOptions.ALLOW_DATA_ATTR, false);
assert.strictEqual(receivedOptions.ALLOWED_URI_REGEXP.test('javascript:alert(1)'), false);
assert.strictEqual(receivedOptions.ALLOWED_URI_REGEXP.test('https://www.senparc.com/'), true);

const normalizedLayout = sandbox.window.NeuCharPivotUi.normalizeLayout({
    title: 'Panel test',
    sections: [{ title: 'Legacy', functions: [] }]
});
assert.strictEqual(normalizedLayout.panels.length, 1);
assert.strictEqual(normalizedLayout.panels[0].key, 'shortcuts');
assert.strictEqual(sandbox.window.NeuCharPivotUi.loopStatus({ enabled: true, nextRunAt: '2999-01-01T00:00:00Z' }), 'countdown');
assert.strictEqual(sandbox.window.NeuCharPivotUi.loopStatus({ enabled: true, isRunning: true }), 'running');

const aggregateScript = fs.readFileSync(aggregatePath, 'utf8');
const globalFunctionScript = fs.readFileSync(globalFunctionPath, 'utf8');
const page = fs.readFileSync(pagePath, 'utf8');
const homePage = fs.readFileSync(homePagePath, 'utf8');
const homeScript = fs.readFileSync(homeScriptPath, 'utf8');
const boardPage = fs.readFileSync(boardPagePath, 'utf8');
const boardScript = fs.readFileSync(boardScriptPath, 'utf8');
const sandboxPage = fs.readFileSync(sandboxPagePath, 'utf8');
assert.ok(aggregateScript.includes('NeuCharPivotUi.sanitizeHtml(data.data)'),
    'String Function results must pass through the shared sanitizer.');
assert.ok(aggregateScript.includes('workflowFilter'),
    'Aggregate filters must include Workflow associations.');
assert.ok(aggregateScript.includes('formatCountdown'),
    'Aggregate must display LoopTask countdown information.');
assert.ok(aggregateScript.includes('jumpToModule'),
    'Aggregate must provide fast module navigation.');
assert.ok(page.includes('v-html="result.html"'), 'Sanitized Function HTML should be rendered as HTML.');
assert.ok(page.includes('pivot-stat-grid'), 'Aggregate must render global NeuCharPivot statistics.');
assert.ok(page.includes('aggregate-panel-tabs'), 'Aggregate must render multiple NeuCharPivot panels.');
assert.ok(page.includes('aggregate-module-nav'), 'Aggregate must render a scrollable module index.');
assert.ok(page.includes('显示不可用模块'), 'Unavailable modules must be opt-in in the main list.');
assert.ok(page.includes('模块已关闭'), 'Closed modules must be distinguished from removed Functions.');
assert.ok(page.indexOf('dompurify.min.js') < page.indexOf('common.js'),
    'DOMPurify must load before the shared NeuCharPivot helpers.');
assert.ok(page.includes('v-else class="aggregate-result aggregate-result-text"'),
    'Structured results and errors must retain a text-only rendering path.');
assert.ok(globalFunctionScript.includes('global.NeuCharPivotFunction'),
    'The global Pivot invocation API must be exposed to every Admin page.');
assert.ok(globalFunctionScript.includes('/Admin/NeuCharPivot/Function?handler=Describe'),
    'The global floating dialog must resolve the server-side Function mapping.');
assert.ok(globalFunctionScript.includes('/Admin/NeuCharPivot/Function?handler=Run'),
    'The global floating dialog must execute through the unified server endpoint.');
assert.ok(homeScript.includes('block.available === false'),
    'Admin home must identify Pivot blocks missing from the database catalog.');
assert.ok(homePage.includes('pivot-missing-popover'),
    'Missing Admin home Pivot blocks must provide actionable details instead of an error popup.');
assert.ok(homePage.includes('打开面板设置并预填代码'),
    'Missing Admin home Pivot blocks must link to prefilled settings.');
assert.ok(boardScript.includes('readSetupRequest'),
    'Pivot settings must consume the missing block setup parameters.');
assert.ok(boardPage.includes('Function Code：'),
    'Pivot settings must display the prefilled Function code.');
assert.ok(sandboxPage.includes('window.NeuCharPivotFunction.open'),
    'Sandbox shortcuts must use the global floating Function mapping.');
assert.ok(sandboxPage.includes('window.location.assign(this.getFunctionUrl'),
    'Sandbox must retain the existing Function page as a fallback.');

console.log('NeuCharPivot Aggregate safe HTML tests passed.');

async function testGlobalFunctionAccessErrors() {
    let options;
    let failure;
    let message;
    function Vue(config) {
        if (config) {
            options = config;
        }
    }
    const context = vm.createContext({
        window: {
            Vue,
            document: { getElementById() { return {}; } },
            service: { async get() { throw failure; } }
        }
    });
    vm.runInContext(globalFunctionScript, context, { filename: globalFunctionPath });
    const viewModel = Object.assign(options.data(), {
        $message: { error(value) { message = value; } }
    });
    const request = { moduleUid: 'module', functionKey: 'function' };
    failure = { response: { status: 403, data: 'Function is not enabled for global Pivot.' } };
    await options.methods.open.call(viewModel, request);
    assert.strictEqual(message, failure.response.data, 'Show the actual server denial reason.');
    assert.strictEqual(viewModel.visible, false);
    assert.strictEqual(viewModel.loading, false);

    const fallback = '当前账号无权访问该 Function，或模块已不可用。';
    for (failure of [
        new Error('Network failure'),
        { response: { status: 403, data: {} } },
        { response: { status: 403, data: ' ' } },
        { response: { status: 500, data: '<html>Internal server error</html>' } }
    ]) {
        await options.methods.open.call(viewModel, request);
        assert.strictEqual(message, fallback, 'Unexpected responses should retain the existing fallback.');
    }
    console.log('Global Pivot access error tests passed.');
}

testGlobalFunctionAccessErrors().catch(error => {
    console.error(error);
    process.exitCode = 1;
});
