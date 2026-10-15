'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

const root = path.resolve(__dirname, '../../../../..');
const source = fs.readFileSync(path.join(root,
    'src/Extensions/Senparc.Xncf.MCP/wwwroot/js/MCP/index.js'), 'utf8');
const apiBase = '/api/Senparc.Xncf.MCP/MCPEndpointAppService/Xncf.MCP_MCPEndpointAppService';
const endpoint = 'http://localhost:5080/mcp-senparc-xncf-mcp/sse';

function createHarness(responder) {
    let options;
    const requests = [];
    const messages = [];
    const copied = [];
    const clipboard = { writeText: async value => { copied.push(value); } };
    const context = vm.createContext({
        Vue: function (value) { options = value; },
        axios: async config => {
            requests.push(JSON.parse(JSON.stringify(config)));
            return responder(config);
        },
        console: { error() {} },
        navigator: { clipboard }
    });
    vm.runInContext(source, context);
    const model = Object.assign(options.data(), {
        $set(target, key, value) { target[key] = value; },
        $nextTick(callback) { callback(); },
        $refs: { mcpForm: { validate: callback => callback(true), clearValidate() {} } },
        $message: {
            error: message => messages.push({ type: 'error', message }),
            success: message => messages.push({ type: 'success', message })
        },
        $confirm: async () => {}
    });
    for (const [name, method] of Object.entries(options.methods)) model[name] = method.bind(model);
    for (const [name, getter] of Object.entries(options.computed)) {
        Object.defineProperty(model, name, { get: getter.bind(model) });
    }
    return { model, requests, messages, copied, clipboard, options };
}

function success(data) {
    return { data: { success: true, data } };
}

const publishedServer = {
    serverName: 'ncf-mcp-server-Senparc-Xncf-MCP', xncfName: 'Senparc.Xncf.MCP',
    xncfUid: '149d8021-1783-4fc9-97a8-f1a1ba60245b', route: '/mcp-senparc-xncf-mcp',
    endpoints: [{ endpointType: 'sse', endpoint }]
};

test('published servers load and filter independently from client configurations', async () => {
    const { model, requests, copied } = createHarness(() => success([publishedServer]));
    model.tableData = [{ id: 42, name: 'Existing client' }];
    await model.getPublishedServers();
    assert.equal(requests[0].url, apiBase + '.GetPublishedServers');
    assert.equal(requests[0].method, 'post');
    assert.equal(model.publishedLoading, false);
    assert.equal(model.publishedServers[0].endpoints[0].endpoint, endpoint);
    assert.equal(model.tableData[0].id, 42);
    model.publishedFilterText = 'SENPARC.XNCF.MCP';
    assert.equal(model.filteredPublishedServers.length, 1);
    model.publishedFilterText = 'localhost:5080';
    assert.equal(model.filteredPublishedServers.length, 1);
    model.publishedFilterText = 'not-published';
    assert.equal(model.filteredPublishedServers.length, 0);
    await model.copyPublishedEndpoint(endpoint);
    assert.deepEqual(copied, [endpoint]);
    assert.equal(requests.length, 1, 'Copying must not persist or alter client configurations.');
});

test('page initializes both lists even if the client list fails', async () => {
    const { model, requests, messages, options } = createHarness(config =>
        config.url.endsWith('.GetAllEndpoints')
            ? Promise.reject({ response: { status: 500 } })
            : success([publishedServer]));
    options.created.call(model);
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(requests.length, 2);
    assert.equal(model.publishedServers.length, 1);
    assert.equal(model.loading, false);
    assert.equal(model.publishedLoading, false);
    assert.match(messages[0].message, /读取 MCP Endpoint 列表失败/);
});

test('failed published-server refresh retains previously loaded rows and reports the error', async () => {
    const { model, messages } = createHarness(() => Promise.reject({ response: { status: 404 } }));
    model.publishedServers = [publishedServer];
    await model.getPublishedServers();
    assert.equal(model.publishedServers.length, 1);
    assert.equal(model.publishedLoading, false);
    assert.match(messages[0].message, /读取本站发布的 MCP 服务失败.*HTTP 404/);
});

test('clipboard failure is visible rather than reported as a successful copy', async () => {
    const { model, messages, clipboard } = createHarness(() => success([]));
    clipboard.writeText = async () => { throw new Error('Clipboard permission denied'); };
    await model.copyPublishedEndpoint(endpoint);
    assert.equal(messages[0].type, 'error');
    assert.match(messages[0].message, /Clipboard permission denied/);
});

test('page keeps published endpoints read-only and separates editable client configurations', () => {
    const page = fs.readFileSync(path.join(root,
        'src/Extensions/Senparc.Xncf.MCP/Areas/Admin/Pages/MCP/Index.cshtml'), 'utf8');
    const panel = page.substring(page.indexOf('<section class="mcp-published-panel">'), page.indexOf('</section>'));
    assert.match(panel, /filteredPublishedServers/);
    assert.match(panel, /只读/);
    assert.match(page, /客户端连接配置/);
    assert.doesNotMatch(panel, /handle(Add|Edit|Delete|ToggleEnabled)/);
});

for (const data of [null, {}, [null], [{ serverName: 'Broken', xncfName: 'MCP', route: '/mcp', endpoints: {} }]]) {
    test(`invalid published-server payload is rejected: ${JSON.stringify(data)}`, async () => {
        const { model, messages } = createHarness(() => success(data));
        await model.getPublishedServers();
        assert.equal(model.publishedLoading, false);
        assert.equal(messages[0].type, 'error');
        assert.match(messages[0].message, /格式不正确/);
    });
}

test('list uses the registered POST route and retains endpoint metadata', async () => {
    const row = { id: 1, name: 'Local MCP', endpoint, enabled: true,
        authConfig: JSON.stringify({ token: 'test-token' }), lastToolCount: 3 };
    const { model, requests } = createHarness(() => success([row]));
    await model.getList();
    assert.equal(requests[0].url, apiBase + '.GetAllEndpoints');
    assert.equal(requests[0].method, 'post');
    assert.equal(model.tableData[0].endpoint, endpoint);
    assert.equal(model.tableData[0].bearerToken, 'test-token');
    assert.equal(model.tableData[0].lastToolCount, 3);
    assert.equal(model.loading, false);
    model.filterText = 'LOCAL';
    assert.equal(model.filteredTableData.length, 1);
    model.filterText = 'not-found';
    assert.equal(model.filteredTableData.length, 0);
    model.filterText = '';
    model.onlyEnabled = true;
    model.tableData[0].enabled = false;
    assert.equal(model.filteredTableData.length, 0);
});

test('create and edit save exact URLs, preserve extra config, and refresh the list', async () => {
    const { model, requests, messages } = createHarness(config =>
        success(config.url.endsWith('.GetAllEndpoints') ? [] : 'saved'));
    model.handleAdd();
    Object.assign(model.form, { name: ' Local MCP ', endpoint: ' ' + endpoint + ' ',
        bearerToken: ' test-token ', extraConfig: '{"custom":true}' });
    await model.handleSubmit();
    assert.equal(requests[0].url, apiBase + '.SaveEndpoint');
    assert.deepEqual(requests[0].data, {
        id: 0, name: 'Local MCP', endpoint, endpointType: 'sse', protocolVersion: '',
        description: '', enabled: true, authConfig: '{"token":"test-token"}',
        extraConfig: '{"custom":true}'
    });
    assert.equal(model.dialogVisible, false);
    assert.equal(model.saving, false);
    assert.equal(requests[1].url, apiBase + '.GetAllEndpoints');
    assert.equal(messages[0].type, 'success');

    model.handleEdit({ id: 1, name: 'Edited', endpoint: 'https://example.test/custom/events',
        endpointType: 'sse', enabled: false, authConfig: '', extraConfig: '{"custom":true}' });
    await model.handleSubmit();
    assert.equal(requests[2].data.id, 1);
    assert.equal(requests[2].data.endpoint, 'https://example.test/custom/events');
    assert.equal(requests[2].data.enabled, false);
});

test('save failure preserves dialog and reports server validation errors', async () => {
    const { model, messages, requests } = createHarness(() =>
        ({ data: { success: false, errorMessage: 'Duplicate endpoint name' } }));
    model.handleAdd();
    Object.assign(model.form, { name: 'Duplicate', endpoint });
    await model.handleSubmit();
    assert.equal(model.dialogVisible, true);
    assert.equal(model.saving, false);
    assert.equal(requests.length, 1);
    assert.match(messages[0].message, /Duplicate endpoint name/);
    assert.equal(messages[0].type, 'error');
});

test('editing unrelated fields preserves existing auth aliases and metadata or raw tokens', async () => {
    for (const authConfig of ['{"accessToken":"test-token","custom":"metadata"}', 'test-token']) {
        const { model, requests } = createHarness(config =>
            success(config.url.endsWith('.GetAllEndpoints') ? [] : 'saved'));
        model.handleEdit({ id: 1, name: 'Local MCP', endpoint, endpointType: 'sse', authConfig });
        await model.handleSubmit();
        assert.equal(requests[0].data.authConfig, authConfig);
    }
});

test('duplicate submits are blocked while the first save is in flight', async () => {
    let finishSave;
    const { model, requests } = createHarness(config =>
        config.url.endsWith('.SaveEndpoint')
            ? new Promise(resolve => { finishSave = resolve; })
            : success([]));
    model.handleAdd();
    Object.assign(model.form, { name: 'Local MCP', endpoint });
    const first = model.handleSubmit();
    assert.equal(model.saving, true);
    await model.handleSubmit();
    assert.equal(requests.length, 1);
    finishSave(success('saved'));
    await first;
    assert.equal(model.saving, false);
});

test('toggle persists and failed toggles revert with a visible error', async () => {
    const { model, requests } = createHarness(() => success('saved'));
    const row = { id: 1, name: 'Local MCP', endpoint, endpointType: 'sse',
        enabled: false, authConfig: '{"token":"test-token"}', lastToolCount: 3 };
    await model.handleToggleEnabled(row);
    assert.equal(requests[0].data.enabled, false);
    assert.equal(requests[0].data.authConfig, row.authConfig);
    assert.equal(requests[0].data.lastToolCount, undefined);
    assert.equal(row.saving, false);

    const failed = createHarness(() => Promise.reject({ response: { status: 500,
        data: { errorMessage: 'Database write failed' } } }));
    await failed.model.handleToggleEnabled(row);
    assert.equal(row.enabled, true);
    assert.equal(row.saving, false);
    assert.match(failed.messages[0].message, /Database write failed/);
});

test('delete refreshes only after successful server confirmation', async () => {
    const { model, requests } = createHarness(config =>
        success(config.url.endsWith('.GetAllEndpoints') ? [] : 'deleted'));
    await model.handleDelete({ id: 1, name: 'Local MCP' });
    assert.equal(requests[0].url, apiBase + '.DeleteEndpoint');
    assert.deepEqual(requests[0].data, { id: 1 });
    assert.equal(requests[1].url, apiBase + '.GetAllEndpoints');

    const failed = createHarness(() =>
        ({ data: { success: false, errorMessage: 'Not found' } }));
    await failed.model.handleDelete({ id: 1, name: 'Missing' });
    assert.equal(failed.requests.length, 1);
    assert.equal(failed.messages[0].type, 'error');
});

test('connection tests pass transport mode and display tools or remote failures', async () => {
    const result = { success: true, toolCount: 1, statusMessage: 'Connected',
        tools: [{ name: 'Echo', parameters: [{ name: 'message', required: true }],
            inputSchemaJson: '{"type":"object"}' }] };
    const { model, requests } = createHarness(config =>
        success(config.url.endsWith('.GetAllEndpoints') ? [] : result));
    model.handleAdd();
    Object.assign(model.form, { name: 'Local MCP', endpoint, endpointType: 'http' });
    await model.handleTestBeforeSave();
    assert.equal(requests[0].data.endpointType, 'http');
    assert.equal(requests[0].data.endpoint, endpoint);
    assert.equal(model.preTestResult.toolCount, 1);
    assert.equal(model.preTestLoading, false);
    await model.handleTest({ id: 1, name: 'Local MCP' });
    assert.equal(requests[1].url, apiBase + '.TestEndpoint');
    assert.equal(model.testResultTools[0].name, 'Echo');
    assert.equal(model.testDialogVisible, true);
    assert.equal(model.testLoadingId, 0);
    model.handleToolExpandChange(result.tools[0], result.tools);
    assert.equal(model.expandedTools[0], 'Echo');
    model.handleToolExpandChange(result.tools[0], []);
    assert.equal(model.expandedTools.length, 0);
    model.showSchema(result.tools[0]);
    assert.equal(model.schemaDialogVisible, true);
    assert.match(model.schemaText, /"type": "object"/);

    const failed = createHarness(() => success({
        success: false, statusMessage: 'Connection timed out', tools: []
    }));
    await failed.model.handleTestBeforeSave();
    assert.equal(failed.model.preTestResult.success, false);
    assert.match(failed.messages[0].message, /Connection timed out/);
});

for (const [name, response, expected] of [
    ['missing routes', { response: { status: 404 } }, /HTTP 404/],
    ['expired authentication', { response: { status: 401 } }, /登录/],
    ['forbidden access', { response: { status: 403 } }, /权限/],
    ['validation errors', { response: { status: 400, data: { title: 'Invalid request' } } }, /Invalid request/]
]) {
    test(`list and save surface ${name}`, async () => {
        const { model, messages } = createHarness(() => Promise.reject(response));
        await model.getList();
        model.handleAdd();
        await model.handleSubmit();
        assert.equal(model.loading, false);
        assert.equal(model.saving, false);
        assert.equal(model.dialogVisible, true);
        assert.equal(messages.length, 2);
        assert.ok(messages.every(item => item.type === 'error' && expected.test(item.message)));
    });
}

for (const data of ['<html>Login</html>', null, {}, { success: true, data: {} }]) {
    test(`malformed list response is not treated as success: ${JSON.stringify(data)}`, async () => {
        const { model, messages } = createHarness(() => ({ data }));
        model.tableData = [{ id: 1, name: 'Existing' }];
        await model.getList();
        assert.equal(model.tableData.length, 1);
        assert.equal(messages[0].type, 'error');
        assert.match(messages[0].message, /格式/);
    });
}
