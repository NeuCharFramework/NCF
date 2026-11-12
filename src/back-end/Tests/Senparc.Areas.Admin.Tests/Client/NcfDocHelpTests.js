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
        assert.equal(api.resolve('mcp.home', culture), 'https://doc.ncf.pub/zh/NcfPackageSources/xncf/mcp-manager.html#help-mcp-manager');
    }
    for (const culture of ['en', 'en-US', 'ja', 'fr', 'es', 'ru', '', undefined, 'zhinvalid']) {
        assert.equal(api.resolve('mcp.home', culture), 'https://doc.ncf.pub/NcfPackageSources/xncf/mcp-manager.html#help-mcp-manager');
    }
    assert.equal(api.resolve('modules.publish', 'zh-CN').split('#')[1], 'help-module-publish');
    assert.equal(api.resolve('modules.publish', 'fr').split('#')[1], 'help-module-publish');
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

test('Chat and Memory help cover both composers and settings without crowding sidebar actions', () => {
    const { api } = setup();
    assert.equal(api.pageTopic('/Admin/AdminChat/Chat?embedded=1'), 'admin.chat');
    const source = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'), 'utf8');
    for (const [topic, count] of Object.entries({
        'admin.chat': 2, 'admin.memory': 1, 'admin.memory.modes': 2,
        'admin.memory.entries': 1, 'admin.memory.loaded': 1,
        'admin.memory.markdown': 1, 'admin.memory.system': 1
    })) {
        assert.equal(source.split(`<ncf-doc-help topic="${topic}"></ncf-doc-help>`).length - 1, count, topic);
        for (const culture of ['zh-CN', 'en', 'ja']) {
            const url = new URL(api.resolve(topic, culture));
            assert.equal(url.pathname.startsWith('/zh/'), culture === 'zh-CN');
            if (topic.startsWith('admin.memory')) {
                assert.ok(url.pathname.endsWith('/NcfPackageSources/xncf/adminchat-memory.html'));
                assert.equal(url.hash, '#help-memory-' + (topic === 'admin.memory' ? 'start' : topic.split('.').pop()));
            }
        }
    }
    for (const handler of ['handleMemoryCommand($event)', 'handleMemoryCommand($event, true)']) {
        const dropdown = source.slice(source.indexOf('@@command="' + handler + '"'));
        assert.match(dropdown, /<\/el-dropdown>\s*<ncf-doc-help topic="admin.memory.modes"><\/ncf-doc-help>/);
    }
    const sidebar = source.slice(source.indexOf('<el-aside'), source.indexOf('</el-aside>'));
    assert.ok(!sidebar.includes('<ncf-doc-help'), 'Sidebar actions must not be squeezed by redundant help');
    assert.equal((source.match(/<span class="ncf-doc-help-group">/g) || []).length, 5);
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
    assert.equal(dialog.url, 'https://doc.ncf.pub/NcfPackageSources/xncf/mcp-manager.html#help-mcp-manager');
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
        for (const match of source.matchAll(/<ncf-doc-help\b[^>]*\btopic="([^"@]+)"|PartialAsync\("_DocHelpStandalone", "([^"]+)"\)|NcfDocHelp\.(?:resolve|open)\(\s*['"]([^'"]+)['"]/g)) {
            assert.ok(catalog.topics[match[1] || match[2] || match[3]], 'Unknown inline/programmatic topic in ' + file);
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

test('page overviews and nearby controls retain their audited contextual topics', () => {
    const { api } = setup();
    for (const [route, topic] of Object.entries({
        '/Admin/Index': 'admin.home',
        '/Admin/SenparcTrace/DateLog': 'admin.logs.details',
        '/Admin/XncfModule/Start': 'modules.lifecycle',
        '/Admin/MCP/Index': 'mcp.home',
        '/Admin/AgentsManager/Index': 'agents.manage',
        '/Admin/PromptRange/Index': 'prompts.dashboard',
        '/Admin/PromptRange/Prompt': 'prompts.workbench',
        '/Admin/Workspace/Index': 'workspace.manage',
        '/Admin/Sandbox/Index': 'sandbox.manage',
        '/Admin/Sandbox/Setup': 'sandbox.setup',
        '/Admin/KnowledgeBase/Index': 'knowledge.manage',
        '/Admin/KnowledgeBase/RecallTest': 'knowledge.recall',
        '/Admin/DynamicData/RenderLayoutPage': 'dynamic.render',
        '/Admin/SenMapic/Task/Detail': 'crawler.detail',
        '/Admin/WeixinManager/Index': 'weixin.overview'
    })) assert.equal(api.pageTopic(route), topic, route);

    const root = path.resolve(admin, '../../..');
    for (const [module, page, pattern] of [
        ['Workspace', 'Workspace/Index', /我的工作区<ncf-doc-help topic="workspace.manage"/],
        ['KnowledgeBase', 'KnowledgeBase/Index', /新建知识库[\s\S]{0,250}<ncf-doc-help topic="knowledge.create"/],
        ['KnowledgeBase', 'KnowledgeBase/RecallTest', /Top K）<ncf-doc-help topic="knowledge.recall.top-k"/],
        ['PromptRange', 'PromptRange/Prompt', /选择靶场[\s\S]{0,160}<ncf-doc-help topic="prompts.manage"/],
        ['Sandbox', 'Sandbox/Setup', /<ncf-doc-help topic="sandbox.setup"/],
        ['Sandbox', 'Sandbox/Setup', /NcfDocHelp\.resolve\('sandbox.setup', document.documentElement.lang\)/],
        ['SenMapic', 'SenMapic/Task/Detail', /PartialAsync\("_DocHelpStandalone", "crawler.detail"\)/]
    ]) {
        const file = path.join(root, 'src/Extensions/Senparc.Xncf.' + module, 'Areas/Admin/Pages', page + '.cshtml');
        assert.match(fs.readFileSync(file, 'utf8'), pattern, file);
    }
    const recall = fs.readFileSync(path.join(root,
        'src/Extensions/Senparc.Xncf.KnowledgeBase/Areas/Admin/Pages/KnowledgeBase/RecallTest.cshtml'), 'utf8');
    const topKControls = [...recall.matchAll(/<el-(?:slider|input-number)\b[^>]*v-model="topK"[^>]*>/g)];
    assert.equal(topKControls.length, 2);
    for (const [control] of topKControls) {
        for (const attribute of [':min="1"', ':max="20"', ':step="1"']) assert.ok(control.includes(attribute));
    }
    assert.match(recall, /先从 5 开始/);
});

test('substantial help opens its owning guide directly, not the general directory', () => {
    const { api, context } = setup();
    const owners = [
        ['start/start-develop/admin-background', ['admin.home', 'admin.monitor', 'admin.users', 'admin.roles', 'admin.menus']],
        ['start/start-develop/admin-login', ['admin.login']],
        ['start/start-develop/admin-module-manage', ['modules.manage', 'modules.publish', 'modules.lifecycle', 'modules.functions']],
        ['start/config/mutiple-tenant', ['admin.tenants']],
        ['start/xncf-develop/isolated-xncf-development', ['builder.preview']],
        ['admin-operations', ['admin.config', 'admin.accounts', 'agents.hub', 'modules.database-sample']],
        ['admin-logs', ['admin.logs', 'admin.logs.details']],
        ['adminchat', ['admin.chat', 'admin.harness', 'admin.attachments']],
        ['adminchat-memory', ['admin.memory', 'admin.memory.modes', 'admin.memory.entries', 'admin.memory.loaded', 'admin.memory.markdown', 'admin.memory.system']],
        ['neubell-webhooks', ['admin.neubell']],
        ['neuchar-pivot', ['pivot.boards', 'pivot.aggregate', 'pivot.access']],
        ['mcp-manager', ['mcp.home', 'mcp.connections']],
        ['aikernel-admin', ['ai.models', 'ai.vectors', 'ai.tokens', 'ai.dashboard']],
        ['aikernel-local-fine-tuning', ['ai.fine-tuning', 'ai.workers']],
        ['knowledge-base', ['knowledge.manage', 'knowledge.create', 'knowledge.recall', 'knowledge.recall.top-k']],
        ['prompt-range', ['prompts.dashboard', 'prompts.workbench', 'prompts.manage', 'prompts.models']],
        ['skills', ['prompts.skills']],
        ['file-manager', ['files.manage', 'files.uploads']],
        ['agents-manager', ['agents.manage']],
        ['agents-manager-3d-status', ['agents.status']],
        ['neuchar-workflow', ['workflow.designer', 'workflow.tasks', 'workflow.replay', 'workflow.analytics']],
        ['workspace-adminchat', ['workspace.manage', 'workspace.permissions', 'desktop.pairing']],
        ['sandbox-environment', ['sandbox.manage', 'sandbox.setup']],
        ['dynamic-data', ['dynamic.home', 'dynamic.tables', 'dynamic.layouts', 'dynamic.pages', 'dynamic.render']],
        ['senmapic', ['crawler.tasks', 'crawler.detail']],
        ['weixin-manager', ['weixin.overview', 'weixin.accounts', 'weixin.users']],
        ['weixin-bot', ['weixin.claw', 'weixin.integration']]
    ];
    const classified = [];
    for (const [document, topics] of owners) {
        for (const topic of topics) {
            classified.push(topic);
            for (const culture of ['zh-CN', 'en', 'ja']) {
                const url = new URL(api.resolve(topic, culture));
                const expected = (culture === 'zh-CN' ? '/zh/' : '/') +
                    (document.startsWith('start/') ? document : 'NcfPackageSources/xncf/' + document) + '.html';
                assert.equal(url.pathname, expected, topic);
                assert.match(url.hash, /^#help-/);
            }
        }
    }
    assert.deepEqual(classified.sort(), Object.keys(context.window.NcfDocHelpCatalog.topics).sort());
});

test('audited chapters describe the specific controls in both languages', { skip: !process.env.NCF_DOCS_ROOT }, () => {
    const { api, context } = setup();
    const contracts = [
        ['admin.home', /后台首页/, /Admin home/, /Host/, /Host/],
        ['admin.config', /系统配置/, /System configuration/, /Footer/, /Footer/],
        ['admin.accounts', /Accounts/, /Accounts/, /没有业务用户查询/, /no business-user search/],
        ['admin.logs.details', /日志详情/, /Logs for a selected date/, /线程/, /thread/],
        ['modules.lifecycle', /生命周期/, /lifecycle/, /开启/, /Enable/],
        ['modules.functions', /Function/, /Function/, /参数/, /parameters/],
        ['mcp.home', /MCP Manager/, /MCP Manager/, /本站发布/, /published by this host/],
        ['mcp.connections', /客户端连接配置/, /client connection configuration/, /保存前测试/, /Test before saving/],
        ['ai.fine-tuning', /微调管理/, /fine-tuning management/, /数据集/, /dataset/],
        ['ai.vectors', /向量/, /Vector/, /不是 AdminChat/, /not AdminChat/],
        ['knowledge.create', /新建知识库/, /Create a knowledge base/, /Embedding/, /embedding/],
        ['knowledge.recall.top-k', /Top K/, /Top K/, /1–20/, /1–20/],
        ['prompts.dashboard', /统计首页/, /dashboard/, /调用趋势/, /usage trends/],
        ['prompts.workbench', /工作台/, /workbench/, /提示词/, /prompt/],
        ['prompts.manage', /选择靶场与靶道/, /Selecting ranges and lanes/, /靶道/, /lane/],
        ['prompts.skills', /Skill/, /Skills/, /Draft/, /Draft/],
        ['agents.manage', /Agents 管理首页/, /Agents management home/, /A2A/, /A2A/],
        ['workflow.designer', /设计器/, /designer/, /节点/, /nodes/],
        ['workspace.manage', /我的工作区与文件上传/, /My Workspaces and uploads/, /创建工作区/, /Create Workspace/],
        ['workspace.permissions', /权限/, /permissions/, /允许 AI/, /Allow AI/],
        ['sandbox.manage', /沙箱面板/, /Sandbox dashboard/, /TTL/, /TTL/],
        ['sandbox.setup', /Docker/, /Docker/, /手动安装/, /manual installation/],
        ['builder.preview', /预览监控/, /preview monitor/, /源码指纹/, /source fingerprint/],
        ['dynamic.render', /布局渲染/, /layout rendering/, /提交/, /Submit/],
        ['crawler.detail', /任务详情/, /task details/, /domain/, /domain/],
        ['weixin.overview', /公众号控制中心/, /Official account control center/, /查看用户/, /View users/]
    ];
    for (const [topic, zhTitle, enTitle, zhContent, enContent] of contracts) {
        assert.ok(context.window.NcfDocHelpCatalog.topics[topic], topic);
        for (const locale of ['zh', 'en']) {
            const url = new URL(api.resolve(topic, locale));
            const file = path.join(process.env.NCF_DOCS_ROOT, 'docs', url.pathname.replace(/\.html$/, '.md'));
            const text = fs.readFileSync(file, 'utf8');
            const marker = '<a id="' + url.hash.slice(1) + '"></a>';
            assert.equal(text.split(marker).length, 2, topic + ': unique explicit anchor');
            const section = text.split(marker)[1].split(/<a id="help-/)[0];
            const heading = section.match(/^## .+$/m);
            assert.ok(heading, topic + ': missing section heading');
            assert.match(heading[0], locale === 'zh' ? zhTitle : enTitle, topic + ': wrong chapter');
            assert.match(section, locale === 'zh' ? zhContent : enContent, topic + ': missing control explanation');
        }
    }
});

test('local VuePress rendering validates every bilingual document and exact anchor', { skip: !process.env.NCF_DOCS_ROOT }, () => {
    const { context } = setup();
    const targets = [];
    for (const topic of Object.values(context.window.NcfDocHelpCatalog.topics)) {
        for (const locale of ['en', 'zh']) {
            assert.match(topic.anchors[locale], /^help-[a-z0-9-]+$/, 'Use explicit stable anchors, not generated heading slugs');
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
