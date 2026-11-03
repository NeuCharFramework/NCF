'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { execFileSync } = require('node:child_process');
const { test } = require('node:test');
const admin = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const shared = path.join(admin, 'wwwroot/js/Admin/Shared');

function setup() {
    let component;
    let dialog;
    let creations = 0;
    const timers = new Map();
    const errors = [];
    const messages = [];
    let timerId = 0;
    const document = {
        documentElement: { lang: 'zh-CN' },
        createElement: () => ({}),
        body: { appendChild() {} }
    };
    function Vue(options) {
        creations++;
        dialog = options.data();
        for (const [key, value] of Object.entries(options.methods)) dialog[key] = value.bind(dialog);
        return dialog;
    }
    Vue.component = (name, value) => {
        assert.equal(name, 'ncf-doc-help');
        component = value;
    };
    Vue.prototype.$message = { error: message => messages.push(message) };
    const context = {
        window: { ncfT: key => key }, document, Vue,
        console: { error: (...args) => errors.push(args) },
        setTimeout: (callback, delay) => { timers.set(++timerId, { callback, delay }); return timerId; },
        clearTimeout: id => timers.delete(id)
    };
    for (const name of ['NcfDocHelpCatalog.js', 'NcfDocHelp.js']) {
        vm.runInNewContext(fs.readFileSync(path.join(shared, name), 'utf8'), context);
    }
    return {
        context, timers, errors, messages, component,
        api: context.window.NcfDocHelp,
        get dialog() { return dialog; },
        get creations() { return creations; }
    };
}

test('Chinese locales use /zh/; all other UI languages use the English root', () => {
    const { api } = setup();
    for (const culture of ['zh', 'zh-CN', 'zh-TW', 'ZH-hans']) {
        assert.equal(api.resolve('mcp.home', culture), 'https://doc.ncf.pub/zh/MCP/home/index.html');
    }
    for (const culture of ['en', 'en-US', 'ja', 'fr', 'es', 'ru', '', undefined, 'zhinvalid']) {
        assert.equal(api.resolve('mcp.home', culture), 'https://doc.ncf.pub/MCP/home/index.html');
    }
    assert.equal(decodeURI(api.resolve('modules.publish', 'zh-CN')).split('#')[1], '切换-发布-状态');
    assert.equal(api.resolve('modules.publish', 'fr').split('#')[1], 'switch-publish-status');
});

test('global routing accepts Razor Index aliases and ignores unknown pages and module IDs', () => {
    const { api, component } = setup();
    for (const route of ['/Admin', '/Admin/', '/ADMIN/Index/?culture=ja#test']) {
        assert.equal(api.pageTopic(route), 'admin.home');
    }
    assert.equal(api.pageTopic('/Admin/XncfModule?uid=secret'), 'modules.manage');
    assert.equal(api.pageTopic('/Admin/MCP/Index/'), 'mcp.home');
    assert.equal(api.pageTopic('/Admin/KnowledgeBase/Index'), 'knowledge.manage');
    for (const route of ['/Admin/Unknown/Index', '/Admin/MCP/Other', '/Admin/MCPFake', '', null]) {
        assert.equal(api.pageTopic(route), null);
    }
    assert.equal(component.computed.resolvedTopic.call({ topic: 'modules.publish', pagePath: '/Admin' }), 'modules.publish');
});

test('Chat and Memory help cover both composers, the sidebar and settings without business handlers', () => {
    const { api } = setup();
    assert.equal(api.pageTopic('/Admin/AdminChat/Chat?embedded=1'), 'admin.chat');
    const source = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'), 'utf8');
    for (const [topic, count] of Object.entries({
        'admin.chat': 2, 'admin.memory': 2, 'admin.memory.modes': 2,
        'admin.memory.entries': 1, 'admin.memory.loaded': 1,
        'admin.memory.markdown': 1, 'admin.memory.system': 1
    })) {
        assert.equal(source.split(`<ncf-doc-help topic="${topic}"></ncf-doc-help>`).length - 1, count, topic);
        for (const culture of ['zh-CN', 'en', 'ja']) {
            const url = new URL(api.resolve(topic, culture));
            assert.equal(url.pathname.startsWith('/zh/'), culture === 'zh-CN');
            if (topic.startsWith('admin.memory')) {
                assert.ok(url.pathname.endsWith('/NcfPackageSources/xncf/adminchat-memory.html'));
                assert.equal(url.hash, topic === 'admin.memory' ? '' : '#help-memory-' + topic.split('.').pop());
            }
        }
    }
    for (const handler of ['handleMemoryCommand($event)', 'handleMemoryCommand($event, true)']) {
        const dropdown = source.slice(source.indexOf('@@command="' + handler + '"'));
        assert.match(dropdown, /<\/el-dropdown>\s*<ncf-doc-help topic="admin.memory.modes"><\/ncf-doc-help>/);
    }
});

test('invalid keys, paths and incomplete bilingual anchors fail explicitly without opening a dialog', () => {
    const state = setup();
    for (const key of ['missing', '__proto__', 'constructor']) assert.throws(() => state.api.resolve(key, 'en'));
    for (const invalid of ['https://evil.test/test.html', '//evil.test/a.html', '../a.html', 'a/../b.html',
        'zh/a.html', 'a.html?x=1', 'a.html#chapter', 'a\\b.html']) {
        state.context.window.NcfDocHelpCatalog.topics.bad = { path: invalid };
        assert.throws(() => state.api.resolve('bad', 'en'));
    }
    state.context.window.NcfDocHelpCatalog.topics.bad = { path: 'test.html', anchors: { zh: 'test' } };
    assert.throws(() => state.api.resolve('bad', 'en'));
    state.api.open('missing');
    assert.equal(state.errors.length, 1);
    assert.deepEqual(state.messages, ['DocHelp.InvalidTopic']);
    assert.equal(state.creations, 0);
});

test('dialog is lazy and shared; stale loads, timeout, retry, locale changes and close are handled', () => {
    const state = setup();
    const opener = { isConnected: true, focus() { this.focused = true; } };
    assert.equal(state.creations, 0);
    assert.equal(state.timers.size, 0);
    state.api.open('modules.publish', opener);
    assert.equal(state.creations, 1);
    const dialog = state.dialog;
    assert.equal(dialog.visible, true);
    assert.equal(dialog.loading, true);
    assert.equal(state.timers.values().next().value.delay, 15000);
    const oldVersion = dialog.frameVersion;
    state.context.document.documentElement.lang = 'ja';
    state.api.open('mcp.home', opener);
    assert.equal(state.creations, 1);
    assert.equal(dialog.url, 'https://doc.ncf.pub/MCP/home/index.html');
    assert.equal(state.timers.size, 1);
    dialog.frameLoaded(oldVersion);
    assert.equal(dialog.loading, true);
    state.timers.values().next().value.callback();
    assert.equal(dialog.failed, true);
    dialog.startLoading();
    assert.equal(dialog.failed, false);
    dialog.frameLoaded(dialog.frameVersion);
    assert.equal(dialog.loading, false);
    assert.equal(state.timers.size, 0);
    dialog.frameFailed(dialog.frameVersion);
    assert.equal(dialog.failed, true);
    dialog.visible = false;
    dialog.cleanup();
    assert.equal(dialog.url, '');
    assert.equal(state.timers.size, 0);
    assert.equal(opener.focused, true);
});

test('all supported UI locales contain every help resource and Razor loads help before page scripts', () => {
    const keys = ['Help', 'Title', 'OpenWindow', 'Close', 'Loading', 'Notice', 'LoadFailed', 'Retry', 'InvalidTopic'];
    for (const locale of ['', '.zh-CN', '.en', '.ja', '.fr', '.es', '.ru']) {
        const resource = fs.readFileSync(path.join(admin, 'Resources/AdminResource' + locale + '.resx'), 'utf8');
        for (const key of keys) assert.equal(resource.split('name="DocHelp.' + key + '"').length - 1, 1);
    }
    const layout = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/Shared/_Layout_Vue.cshtml'), 'utf8');
    assert.ok(layout.includes('page-path="@Context.Request.Path"'));
    assert.ok(layout.indexOf('NcfDocHelpCatalog.js') < layout.indexOf('NcfDocHelp.js'));
    assert.ok(layout.indexOf('NcfDocHelp.js') < layout.indexOf('@RenderSection("scripts"'));
    const page = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/XncfModule/Index.cshtml'), 'utf8');
    assert.ok(page.includes('<ncf-doc-help topic="modules.publish"></ncf-doc-help>'));
});

test('every runtime Admin page is explicitly mapped or excluded and every inline key exists', () => {
    const { api, context } = setup();
    const catalog = context.window.NcfDocHelpCatalog;
    const root = path.resolve(admin, '../../..');
    const files = execFileSync('git', ['ls-files', '*Areas/Admin/Pages/*.cshtml'], { cwd: root, encoding: 'utf8' })
        .trim().split('\n').filter(file => !/(\/Tests\/|\/templates\/|\/Template_)/.test(file));
    const routes = new Set();
    for (const file of files) {
        const source = fs.readFileSync(path.join(root, file), 'utf8');
        if (/^\uFEFF?@page/m.test(source)) {
            const route = ('/Admin/' + file.split('/Areas/Admin/Pages/')[1].replace(/\.cshtml$/, ''))
                .toLowerCase().replace(/\/index$/, '');
            routes.add(route);
            assert.ok(api.pageTopic(route) || catalog.excludedPages[route], 'Unclassified page: ' + file);
            if (api.pageTopic(route)) {
                assert.ok(source.includes('Layout = "_Layout_Vue"') || source.includes('PartialAsync("_DocHelpStandalone"'),
                    'Mapped page has no guaranteed help host: ' + file);
            }
        }
        for (const match of source.matchAll(/<ncf-doc-help\b[^>]*\btopic="([^"@]+)"|PartialAsync\("_DocHelpStandalone", "([^"]+)"\)/g)) {
            assert.ok(catalog.topics[match[1] || match[2]], 'Unknown inline topic in ' + file);
        }
    }
    for (const [route, topic] of Object.entries(catalog.pages)) {
        assert.ok(routes.has(route), 'Route does not correspond to a runtime page: ' + route);
        assert.ok(catalog.topics[topic], 'Unknown mapped topic: ' + topic);
        assert.equal(api.pageTopic(route + '/Index/?uid=private'), topic);
    }
    for (const [route, reason] of Object.entries(catalog.excludedPages)) {
        assert.ok(routes.has(route) && reason.length > 10);
        assert.equal(api.pageTopic(route), null);
    }
});

test('local VuePress rendering validates every bilingual document and exact anchor', { skip: !process.env.NCF_DOCS_ROOT }, () => {
    const { context } = setup();
    const targets = [];
    for (const topic of Object.values(context.window.NcfDocHelpCatalog.topics)) {
        for (const locale of ['en', 'zh']) {
            const relative = path.join(locale === 'zh' ? 'zh' : '', topic.path.replace(/\.html$/, '.md'));
            const file = path.join(process.env.NCF_DOCS_ROOT, 'docs', relative);
            assert.ok(fs.existsSync(file), 'Missing document: ' + file);
            targets.push({ file: relative, anchor: topic.anchors && topic.anchors[locale] });
        }
    }
    const rendered = JSON.parse(execFileSync(process.execPath, ['--input-type=module', '-e', `
        import { readFileSync } from 'node:fs';
        import { createMarkdown } from 'vuepress/markdown';
        const markdown = createMarkdown();
        const targets = JSON.parse(readFileSync(0, 'utf8'));
        console.log(JSON.stringify(targets.map(target => ({
            ...target,
            ids: Array.from(markdown.render(readFileSync('docs/' + target.file, 'utf8'))
                .matchAll(/\\bid="([^"]+)"/g), match => match[1])
        }))));
    `], { cwd: process.env.NCF_DOCS_ROOT, input: JSON.stringify(targets), encoding: 'utf8' }));
    for (const target of rendered) {
        if (target.anchor) assert.equal(target.ids.filter(id => id === target.anchor).length, 1,
            'Missing or duplicate anchor: ' + target.file + '#' + target.anchor);
    }
});

test('standalone forms and login load the shared help without replacing their layout', () => {
    for (const page of ['Login', 'AdminUserInfo/Edit', 'AdminUserInfo/AuthorizationPage', 'Menu/Edit', 'Role/Edit', 'Role/Permission']) {
        const source = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages', page + '.cshtml'), 'utf8');
        assert.ok(source.includes('PartialAsync("_DocHelpStandalone"'), page);
    }
});

test('standalone loader defers to an existing header help control', () => {
    let removed = false;
    const host = { dataset: {}, remove() { removed = true; } };
    vm.runInNewContext(fs.readFileSync(path.join(shared, 'NcfDocHelpStandalone.js'), 'utf8'), {
        window: {},
        document: {
            readyState: 'complete',
            querySelectorAll: () => [host],
            querySelector: () => ({})
        }
    });
    assert.equal(removed, true);
    assert.equal(host.dataset.mounted, 'true');
});

test('built VuePress output contains every final URL and anchor', { skip: !process.env.NCF_DOCS_DIST }, () => {
    const { api, context } = setup();
    for (const topic of Object.keys(context.window.NcfDocHelpCatalog.topics)) {
        for (const culture of ['zh-CN', 'en']) {
            const url = new URL(api.resolve(topic, culture));
            const html = fs.readFileSync(path.join(process.env.NCF_DOCS_DIST, url.pathname), 'utf8');
            assert.ok(/<h1\b/.test(html), 'No document heading: ' + url);
            if (url.hash) {
                const id = decodeURIComponent(url.hash.slice(1));
                assert.equal(html.split('id="' + id + '"').length - 1, 1, 'Invalid built anchor: ' + url);
            }
        }
    }
});
