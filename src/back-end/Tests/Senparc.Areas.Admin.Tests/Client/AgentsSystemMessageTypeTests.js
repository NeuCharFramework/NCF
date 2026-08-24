'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/wwwroot/js/AgentsManager/index.js');
const pagePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Areas/Admin/Pages/AgentsManager/Index.cshtml');
const script = fs.readFileSync(scriptPath, 'utf8');
const page = fs.readFileSync(pagePath, 'utf8');

let capturedOptions = null;
const components = {};

function Vue(options) {
    capturedOptions = options;
    return options;
}
Vue.component = (name, options) => {
    components[name] = options;
};
Vue.directive = () => { };

const serviceAM = {
    async get() {
        return {
            data: {
                success: true,
                data: [{ text: '⊙ 2026.08.10.1（搜索引擎引导）', value: '2026.08.10.1' }]
            }
        };
    },
    async post() {
        return { data: { success: true, data: [] } };
    }
};

const context = vm.createContext({
    Vue,
    serviceAM,
    console: { log() { }, warn() { }, error() { } },
    window: {},
    document: {},
    setTimeout,
    clearTimeout,
    setImmediate,
    Date,
    Math,
    Number,
    Object,
    Array,
    Error,
    Promise,
    String,
    Map,
    Set,
    URLSearchParams,
    formatDate(value) { return value; }
});

function createViewModel() {
    const viewModel = Object.assign({
        $refs: {},
        $options: { data: capturedOptions.data },
        $set(target, key, value) { target[key] = value; },
        $nextTick(callback) { callback(); }
    }, capturedOptions.data());
    Object.keys(capturedOptions.methods).forEach(name => {
        viewModel[name] = capturedOptions.methods[name].bind(viewModel);
    });
    return viewModel;
}

async function run() {
    vm.runInContext(script, context, { filename: scriptPath });
    assert.ok(capturedOptions, 'AgentsManager Vue options should be captured.');
    assert.ok(components['load-more-select'], 'The shared load-more select should be registered.');
    assert.match(page, /v-show="agentForm\.systemMessageType === '1'"/,
        'The PromptRange selector must stay mounted while its type is being determined.');
    assert.match(page, /@@options-loaded="handleSystemMessageOptionsLoaded"/,
        'The agent editors must wait for PromptRange options before deciding the input type.');

    const viewModel = createViewModel();
    viewModel.agentForm = { id: 9, systemMessage: '2026.08.10.1', systemMessageType: '1' };
    viewModel.agentSystemMessageTypeDetectionPending = true;
    viewModel.handleSystemMessageOptionsLoaded([{ value: '2026.08.10.1' }]);
    assert.strictEqual(viewModel.agentForm.systemMessageType, '1',
        'A PromptCode returned by the selector must be classified as self-selected.');
    assert.strictEqual(viewModel.agentSystemMessageTypeDetectionPending, false);

    viewModel.agentForm = { id: 10, systemMessage: '这是一段手动 SystemMessage', systemMessageType: '1' };
    viewModel.agentSystemMessageTypeDetectionPending = true;
    viewModel.handleSystemMessageOptionsLoaded([{ value: '2026.08.10.1' }]);
    assert.strictEqual(viewModel.agentForm.systemMessageType, '2',
        'A value absent from PromptRange options must remain manual input.');

    viewModel.agentSystemMessageTypeDetectionPending = true;
    viewModel.handleSystemMessageTypeChange();
    viewModel.handleSystemMessageOptionsLoaded([{ value: '这是一段手动 SystemMessage' }]);
    assert.strictEqual(viewModel.agentForm.systemMessageType, '2',
        'An explicit user type selection must not be overwritten by a delayed response.');

    let openButtonType = '';
    viewModel.handleElVisibleOpenBtn = buttonType => {
        openButtonType = buttonType;
    };
    await viewModel.handleEditDrawerOpenBtn('drawerAgent', { id: 11, systemMessage: '2026.08.10.1' });
    assert.strictEqual(openButtonType, 'drawerAgent');
    assert.strictEqual(viewModel.agentForm.systemMessageType, '1',
        'Editing should first mount the self-select control so it can load candidates.');
    assert.strictEqual(viewModel.agentSystemMessageTypeDetectionPending, true);

    const selectOptions = components['load-more-select'];
    let emittedOptions = null;
    const selectViewModel = Object.assign({
        serviceType: 'systemMessage',
        $emit(eventName, options) {
            if (eventName === 'options-loaded') emittedOptions = options;
        }
    }, selectOptions.data());
    Object.keys(selectOptions.methods).forEach(name => {
        selectViewModel[name] = selectOptions.methods[name].bind(selectViewModel);
    });
    selectViewModel.managementListOption();
    await new Promise(resolve => setImmediate(resolve));
    assert.deepStrictEqual(JSON.parse(JSON.stringify(emittedOptions)), [{
        text: '⊙ 2026.08.10.1（搜索引擎引导）',
        value: '2026.08.10.1',
        label: '⊙ 2026.08.10.1（搜索引擎引导）',
        disabled: false
    }], 'The selector must publish its loaded PromptRange options to the editor.');

    console.log('AgentsSystemMessageTypeTests passed');
}

run().catch(error => {
    console.error(error);
    process.exitCode = 1;
});
