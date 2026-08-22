'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/XncfModule/index.js');
const homePagePath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml');
const script = fs.readFileSync(scriptPath, 'utf8');
const homePage = fs.readFileSync(homePagePath, 'utf8');

let capturedOptions = null;
let replacedUrl = null;
let postedRequest = null;
let navRefreshCount = 0;
const requestedUrls = [];

const updatedModules = [{
    uid: 'module-a',
    name: 'Module.A',
    menuName: 'Module A',
    version: '1.0.0 -> 2.0.0',
    icon: 'fa fa-cube'
}, {
    uid: 'module-b',
    name: 'Module.B',
    menuName: 'Module B',
    version: '1.0.0 -> 3.0.0',
    icon: 'fa fa-cube'
}];

const window = {
    location: {
        search: '?tab=updates',
        href: 'https://example.test/Admin/XncfModule/Index?tab=updates'
    },
    history: {
        replaceState(state, title, url) {
            replacedUrl = url;
        }
    },
    sessionStorage: {
        setItem() { }
    }
};

function Vue(options) {
    capturedOptions = options;
    return options;
}

const service = {
    async get(url) {
        requestedUrls.push(url);
        if (url.includes('UpdatedMofules')) {
            return { data: { data: updatedModules.slice() } };
        }
        if (url.includes('UnMofules')) {
            return { data: { data: [] } };
        }
        return {
            data: {
                data: {
                    result: [],
                    hideModuleManager: false
                }
            }
        };
    },
    async post(url, body) {
        postedRequest = { url, body };
        return {
            data: {
                data: {
                    success: true,
                    totalCount: 2,
                    successCount: 2,
                    failureCount: 0,
                    items: updatedModules.map(item => ({
                        uid: item.uid,
                        moduleName: item.menuName,
                        previousVersion: '1.0.0',
                        targetVersion: '2.0.0',
                        finalVersion: '2.0.0',
                        updateSucceeded: true,
                        enableSucceeded: true,
                        finalState: 1,
                        message: 'ok'
                    }))
                }
            }
        };
    }
};

const context = vm.createContext({
    window,
    Vue,
    service,
    ncfT(key) { return key; },
    getNavMenu() { navRefreshCount++; },
    formaTableTime(value) { return value; },
    URL,
    URLSearchParams,
    console: { log() { }, warn() { }, error() { } },
    Array,
    Error,
    Object,
    Promise,
    String,
    setTimeout() { }
});

function createViewModel() {
    const table = {
        clearSelectionCount: 0,
        toggledRow: null,
        clearSelection() { this.clearSelectionCount++; },
        toggleRowSelection(row) { this.toggledRow = row; }
    };
    const viewModel = Object.assign({
        $refs: { updatedModulesTable: table },
        $message: {
            warning() { },
            error() { }
        },
        async $confirm() { return true; }
    }, capturedOptions.data());

    Object.keys(capturedOptions.methods).forEach(name => {
        viewModel[name] = capturedOptions.methods[name].bind(viewModel);
    });
    Object.keys(capturedOptions.computed).forEach(name => {
        Object.defineProperty(viewModel, name, {
            get: capturedOptions.computed[name].bind(viewModel)
        });
    });
    return viewModel;
}

async function run() {
    vm.runInContext(script, context, { filename: scriptPath });
    assert.ok(capturedOptions, 'Vue page options should be captured.');

    assert.ok(homePage.includes('XncfModule/Index?tab=installed'));
    assert.ok(homePage.includes('XncfModule/Index?tab=updates'));
    assert.ok(homePage.includes('XncfModule/Index?tab=new'));

    const viewModel = createViewModel();
    assert.strictEqual(viewModel.getRequestedTab(), 'updates');
    viewModel.activeTab = 'installed';
    viewModel.handleTabClick({ name: 'installed' });
    assert.strictEqual(replacedUrl, '/Admin/XncfModule/Index?tab=installed');

    await viewModel.getList();
    assert.deepStrictEqual(requestedUrls.slice(0, 3), [
        '/Admin/XncfModule/Index?handler=Mofules',
        '/Admin/XncfModule/Index?handler=UnMofules',
        '/Admin/XncfModule/Index?handler=UpdatedMofules'
    ]);
    assert.strictEqual(viewModel.updatedTableData.length, 2);

    viewModel.updatedTableSearch = 'module.b';
    assert.strictEqual(viewModel.filteredUpdatedTableData.length, 1);
    assert.strictEqual(viewModel.filteredUpdatedTableData[0].uid, 'module-b');

    viewModel.handleUpdatedSelectionChange(updatedModules.slice());
    await viewModel.confirmBatchUpdate();
    assert.strictEqual(postedRequest.url, '/Admin/XncfModule/Index?handler=BatchUpdateAndEnable');
    assert.deepStrictEqual(Array.from(postedRequest.body.uids), ['module-a', 'module-b']);
    assert.strictEqual(viewModel.batchUpdate.resultVisible, true);
    assert.strictEqual(viewModel.batchUpdate.result.successCount, 2);
    assert.strictEqual(navRefreshCount, 1);

    process.stdout.write('XNCF module management tests passed.\n');
}

run().catch(error => {
    process.stderr.write(error.stack + '\n');
    process.exitCode = 1;
});
