'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/Index/Index.js');
const pagePath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml');
const servicePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/ChatGroupAppService.cs');
const script = fs.readFileSync(scriptPath, 'utf8');
const page = fs.readFileSync(pagePath, 'utf8');
const serviceSource = fs.readFileSync(servicePath, 'utf8');

let capturedOptions = null;
let requestedUrl = null;
let requestedHeaders = null;
let intervalDelay = null;

const dashboardOverview = {
    localAgentCount: 4,
    localAgentEnabledCount: 3,
    remoteA2AAgentCount: 2,
    remoteA2AAgentEnabledCount: 1,
    publishedA2AAgentCount: 3,
    publishedA2AAgentEnabledCount: 2,
    groupCount: 5,
    groupEnabledCount: 4,
    activeGroupCount: 2,
    chattingGroupCount: 1,
    activeTaskCount: 3,
    chattingTaskCount: 1,
    waitingOrPausedTaskCount: 2,
    activeLocalAgentCount: 2,
    activeRemoteA2AAgentCount: 1,
    chattingLocalAgentCount: 1,
    chattingRemoteA2AAgentCount: 1
};

const document = {
    hidden: false,
    getElementById() { return null; },
    querySelector() { return null; },
    querySelectorAll() { return []; }
};

const window = {
    ncfJwtToken: 'test-token',
    setInterval(handler, delay) {
        intervalDelay = delay;
        return 17;
    },
    clearInterval() { },
    addEventListener() { },
    removeEventListener() { }
};

function Vue(options) {
    capturedOptions = options;
    return options;
}

const axios = {
    async get(url, options) {
        requestedUrl = url;
        requestedHeaders = options.headers;
        return { data: { success: true, data: dashboardOverview } };
    }
};

const context = vm.createContext({
    window,
    document,
    Vue,
    axios,
    service: {},
    echarts: {},
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
    assert.ok(!script.includes('ChatLauncherMixin'), 'The home page should use the Footer dialog instead of loading a second chat launcher.');

    const viewModel = createViewModel();
    await viewModel.fetchAgentsOverview();

    assert.strictEqual(
        requestedUrl,
        '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetDashboardOverview');
    assert.strictEqual(requestedHeaders.Authorization, 'Bearer test-token');
    assert.strictEqual(viewModel.agentsOverview.available, true);
    assert.strictEqual(viewModel.agentsTotal(), 6);
    assert.strictEqual(viewModel.agentsEnabledTotal(), 4);
    assert.strictEqual(viewModel.agentsDisabledTotal(), 2);
    assert.strictEqual(viewModel.agentsActiveTotal(), 3);
    assert.strictEqual(viewModel.agentsChattingTotal(), 2);
    assert.strictEqual(viewModel.disabledCount(3, 2), 1);

    viewModel.startAgentsOverviewPolling();
    assert.strictEqual(intervalDelay, 5000, 'The dashboard overview should poll at a modest five-second interval.');
    assert.strictEqual(viewModel.agentsOverviewTimer, 17);

    assert.ok(page.includes('v-if="agentsOverview.available"'), 'The overview must remain hidden when AgentsManager is unavailable.');
    assert.ok(page.includes('@AR["Admin.Home.AgentsPublishedA2A"]'));
    assert.ok(page.includes('@AR["Admin.Home.AgentsRemoteA2A"]'));
    assert.ok(page.includes('@AR["Admin.Home.AgentsGroups"]'));
    assert.ok(page.includes('@@click="openFooterAi"'), 'The compact AI area must open the Footer dialog.');
    assert.ok(!page.includes('ChatLauncherMixin.js'), 'The full home-page chat launcher should not be loaded.');

    assert.ok(serviceSource.includes('GetDashboardOverview'));
    assert.ok(serviceSource.includes('PublishedA2AAgentEnabledCount'));
    assert.ok(serviceSource.includes('ActiveRemoteA2AAgentCount'));
}

run().then(() => {
    console.log('Agents overview dashboard tests passed.');
}).catch(error => {
    console.error(error);
    process.exitCode = 1;
});
