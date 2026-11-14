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
let openingResponse = { data: { data: [{ uid: 'module-1', menuName: 'Module One' }] } };

const context = vm.createContext({
    window: {
        setInterval() { return 1; },
        clearInterval() { },
        addEventListener() { },
        removeEventListener() { }
    },
    document: {
        hidden: false,
        getElementById() { return null; },
        querySelector() { return null; },
        querySelectorAll() { return []; }
    },
    Vue: function Vue(options) {
        capturedOptions = options;
        return options;
    },
    axios: {},
    service: {
        async get() {
            return openingResponse;
        }
    },
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
    const viewModel = Object.assign({}, capturedOptions.data());
    Object.keys(capturedOptions.methods).forEach(name => {
        viewModel[name] = capturedOptions.methods[name].bind(viewModel);
    });
    return viewModel;
}

async function run() {
    vm.runInContext(script, context, { filename: scriptPath });
    assert.ok(capturedOptions, 'Vue page options should be captured.');

    const viewModel = createViewModel();
    assert.deepStrictEqual(Array.from(viewModel.xncfOpeningList), []);
    assert.strictEqual(viewModel.pivotBlockModuleLabel('module-1'), 'module-1');

    await viewModel.getXncfOpening();
    assert.strictEqual(viewModel.pivotBlockModuleLabel('module-1'), 'Module One');

    openingResponse = { data: { data: { module: 'not-an-array' } } };
    await viewModel.getXncfOpening();
    assert.deepStrictEqual(Array.from(viewModel.xncfOpeningList), []);
    assert.strictEqual(viewModel.pivotBlockModuleLabel('module-1'), 'module-1');
}

run().then(() => {
    console.log('Admin index dashboard module list tests passed.');
}).catch(error => {
    console.error(error);
    process.exitCode = 1;
});
