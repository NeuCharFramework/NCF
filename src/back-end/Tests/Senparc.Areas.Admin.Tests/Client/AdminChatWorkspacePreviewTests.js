'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const adminPath = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const page = fs.readFileSync(path.join(adminPath, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'), 'utf8');
const selector = fs.readFileSync(path.join(adminPath, 'Areas/Admin/Pages/Shared/_ChatModuleSelectorDialog.cshtml'), 'utf8');
const scriptPath = path.join(adminPath, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js');
const chatScript = fs.readFileSync(scriptPath, 'utf8');
const styles = fs.readFileSync(path.join(adminPath, 'wwwroot/css/Admin/Pages/AdminChat/Chat.css'), 'utf8');
const template = page.slice(page.indexOf('<div class="admin-chat-container">'), page.indexOf('@section scripts'))
    .replace(/@await Html\.PartialAsync\("[^"]+_ChatModuleSelectorDialog\.cshtml"\)/g, selector)
    .replace(/@AR\["([^"]+)"\]/g, '$1')
    .replace(/<!--[\s\S]*?-->/g, '');

function validateTemplate(markup) {
    const voidTags = new Set(['area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr']);
    const stack = [];
    const tags = /<\/?([a-z][a-z0-9-]*)\b(?:[^>"']|"[^"]*"|'[^']*')*>/gi;
    let panelCount = 0;
    for (const token of markup.matchAll(tags)) {
        const tag = token[1].toLowerCase();
        if (token[0].startsWith('</')) {
            assert.strictEqual(stack.pop()?.tag, tag,
                `Unbalanced ${tag} closing tag can move Workspace UI into an inactive Vue branch.`);
        } else if (!voidTags.has(tag) && !token[0].endsWith('/>')) {
            if (/\bclass="chat-workspace-preview"/.test(token[0])) {
                panelCount++;
                assert.strictEqual(stack[stack.length - 1].tag, 'el-container',
                    'Workspace panel must be a direct child of the Chat split container.');
                assert.ok(!stack.some(parent => parent.launcher),
                    'Workspace panel must not be nested in the no-session v-else branch.');
            }
            stack.push({ tag, launcher: /\bclass="chat-empty-launcher"/.test(token[0]) });
        }
    }
    assert.strictEqual(stack.length, 0, 'Chat template must close every container.');
    assert.strictEqual(panelCount, 1, 'Render exactly one Workspace panel without redundant Drawer fallbacks.');
}

validateTemplate(template);
assert.throws(() => validateTemplate(template.replace(
    /<\/div>\s*(<el-dialog\s+title="AdminChat.AddReferenceModule")/,
    '$1'
)), /Unbalanced|Workspace panel must/,
'The template regression must detect the missing launcher closing div.');
assert.ok(template.includes('workspaceEntryName(entry.relativePath)'),
    'File names must use the WorkspaceEntry RelativePath contract.');
assert.ok(template.includes('formatWorkspaceFileSize(entry.length)'),
    'File sizes must use the WorkspaceEntry Length contract.');
const headerActions = template.slice(template.indexOf('<div class="chat-header-actions">'), template.indexOf('<div v-if="currentSessionWorkspaceId"'));
assert.ok(headerActions.lastIndexOf('toggleWorkspacePreview') > headerActions.lastIndexOf('openTrajectoryDialog'),
    'Assets must be the rightmost Chat header action.');
assert.ok(template.includes('v-html="formatMessageContent(liveThinkingText)"'),
    'Live reasoning must render Markdown consistently with the final answer.');
assert.ok(template.includes('sandbox="allow-scripts"'),
    'Workspace HTML preview must allow scripts only inside the iframe sandbox.');
assert.ok(!template.includes('sandbox="allow-same-origin"'),
    'Workspace HTML preview must not grant same-origin access to sandboxed scripts.');
assert.ok(chatScript.includes('document.head.insertBefore(csp, document.head.firstChild);'),
    'Workspace preview CSP must be parsed before any user-provided head script.');
for (const selector of ['live-answer-preview', 'live-thinking-content', 'harness-step-body']) {
    const rule = styles.match(new RegExp(`\\.${selector}\\s*\\{([^}]+)\\}`));
    assert.match(rule[1], /white-space:\s*normal/,
        'Rendered Markdown must not preserve serializer newlines on top of <br> and paragraph breaks.');
}

let options;
function workspacePreviewFixture(relativePath) {
    const path = String(relativePath || '');
    if (path.endsWith('.md')) {
        return { path, isText: true, contentType: 'text/markdown', content: '# Workspace guide', etag: 'etag-md' };
    }
    if (path.endsWith('.py')) {
        return {
            path, isText: true, contentType: 'text/x-python', etag: 'etag-py',
            content: 'def greet():\n    return "hi"  # ok\n'
        };
    }
    if (path.endsWith('.png')) {
        return { path, isText: false, contentType: 'image/png', content: null, etag: 'etag-png' };
    }
    return { path, isText: true, contentType: 'text/plain', content: 'Workspace text', etag: 'etag-txt' };
}

const requests = [];
const listeners = new Map();
const classes = new Set();
const container = {
    clientWidth: 1800,
    querySelector() {
        return { getBoundingClientRect() { return { width: 250 }; } };
    }
};
const sandbox = {
    Vue: function (value) { options = value; },
    window: { ChatLauncherMixin: {}, innerWidth: 1440 },
    getComputedStyle() {
        return { getPropertyValue() { return '360px'; } };
    },
    document: {
        addEventListener(name, handler) { listeners.set(name, handler); },
        removeEventListener(name) { listeners.delete(name); },
        body: { classList: { add(name) { classes.add(name); }, remove(name) { classes.delete(name); } } }
    },
    service: {
        async get(url, config) {
            requests.push({ url, params: config.params });
            return { data: { success: true, data: url.includes('GetWorkspaceEntriesAsync')
                ? { entries: [{ relativePath: 'output/readme.txt', isDirectory: false, length: 12 }] }
                : workspacePreviewFixture(config.params.relativePath) } };
        }
    },
    console
};
sandbox.marked = require(path.join(adminPath, 'wwwroot/js/Admin/lib/marked.min.js'));
sandbox.DOMPurify = { sanitize: html => html };
vm.runInNewContext(chatScript, sandbox, { filename: scriptPath });
const app = options.data();
Object.entries(options.methods).forEach(([name, method]) => { app[name] = method.bind(app); });
Object.entries(options.computed).forEach(([name, getter]) => {
    Object.defineProperty(app, name, { get: getter.bind(app) });
});
app.$nextTick = callback => callback();
app.$refs = { workspaceContainer: { $el: container } };
app.$notify = notification => { throw new Error(notification.message); };
app.currentSessionId = 42;
app.currentSessionWorkspaceId = 'ws_test';
app.$message = { success() {}, error(message) { throw new Error(message); }, warning() {} };

async function run() {
    app.toggleWorkspacePreview();
    await new Promise(resolve => setImmediate(resolve));
    assert.strictEqual(app.workspacePreviewOpen, true);
    assert.strictEqual(requests[0].params.sessionId, 42);
    assert.strictEqual(requests[0].params.relativePath, '');
    assert.strictEqual(app.workspaceEntries[0].relativePath, 'output/readme.txt');

    await app.handleWorkspaceEntryClick({ relativePath: 'output', isDirectory: true, length: 0 });
    assert.strictEqual(app.workspacePreviewPath, 'output');
    assert.strictEqual(requests[1].params.relativePath, 'output');
    await app.handleWorkspaceEntryClick(app.workspaceEntries[0]);
    assert.strictEqual(requests[2].params.relativePath, 'output/readme.txt');
    assert.strictEqual(app.workspacePreviewData.content, 'Workspace text');

    app.startWorkspacePreviewResize({
        preventDefault() {},
        clientX: 900,
        currentTarget: { parentElement: { getBoundingClientRect() { return { width: 420 }; } } }
    });
    listeners.get('mousemove')({ clientX: 800 });
    assert.strictEqual(app.workspacePreviewWidth, 520, 'Drag width must follow pointer delta, not viewport right edge.');
    listeners.get('mouseup')();
    assert.strictEqual(listeners.size, 0);
    assert.strictEqual(classes.size, 0);
    app.expandWorkspacePreview();
    assert.strictEqual(app.workspacePreviewWidth, 1190,
        'Assets must use all remaining container width, even above 720px or 55vw.');
    assert.strictEqual(container.clientWidth - 250 - app.workspacePreviewWidth, 360,
        'Maximizing assets must preserve the Chat minimum width.');
    container.clientWidth = 1200;
    app.updateWorkspacePreviewLayout();
    assert.strictEqual(app.workspacePreviewWidth, 590, 'Resizing the container must clamp the preview.');
    assert.strictEqual(app.workspacePreviewOverlay, false);
    container.clientWidth = 900;
    app.updateWorkspacePreviewLayout();
    assert.strictEqual(app.workspacePreviewOverlay, true,
        'Use an overlay when the actual container cannot fit both usable panes.');
    app.expandWorkspacePreview();
    assert.strictEqual(app.workspacePreviewWidth, 900);
    app.toggleWorkspacePreview();
    assert.strictEqual(app.workspacePreviewOpen, false);
    assert.strictEqual(requests.length, 3, 'Closing must not reload assets.');

    let renderedMarkdown = null;
    app.formatMessageContent = content => {
        assert.strictEqual(content, '# Workspace guide');
        return '<h1>Workspace guide</h1>';
    };
    app.buildWorkspaceHtmlPreview = async (content, filePath, isMarkdown) => {
        renderedMarkdown = { content, filePath, isMarkdown };
        return '<!doctype html><h1>Workspace guide</h1>';
    };
    app.workspacePreviewSource = true;
    await app.handleWorkspaceEntryClick({ relativePath: 'guide.md', isDirectory: false });
    assert.strictEqual(app.workspacePreviewSource, false, 'Opening Markdown must default to rendered preview.');
    assert.strictEqual(app.workspacePreviewIsDocument, true);
    assert.strictEqual(app.workspacePreviewCanShowSource, true);
    assert.strictEqual(renderedMarkdown.content, '<h1>Workspace guide</h1>');
    assert.strictEqual(renderedMarkdown.filePath, 'guide.md');
    assert.strictEqual(renderedMarkdown.isMarkdown, true, 'Markdown must use the isolated HTML preview pipeline.');
    assert.strictEqual(app.workspacePreviewData.content, '# Workspace guide', 'Source must remain unchanged.');
    assert.strictEqual(app.workspacePreviewSrcdoc, '<!doctype html><h1>Workspace guide</h1>');
    const highlighted = app.highlightWorkspaceCode('def greet():\n    return "hi"  # ok\nprint("<script>")\n', 'python');
    assert.ok(highlighted.includes('class="tok-keyword">def'), 'Python preview must highlight keywords.');
    assert.ok(highlighted.includes('class="tok-string">"hi"'), 'Python preview must highlight strings.');
    assert.ok(highlighted.includes('class="tok-comment"># ok'), 'Python preview must highlight comments.');
    assert.ok(!highlighted.includes('<script>'), 'Code preview must escape markup.');
    assert.ok(highlighted.includes('&lt;script&gt;'));
    assert.ok(highlighted.includes('data-ln="1"'), 'Line numbers stay in an attribute, not the copied text.');
    assert.ok(highlighted.includes('class="code-line"'));
    assert.ok(!highlighted.includes('>1<'), 'Line number text must not be part of the code.');
    assert.match(styles, /content:\s*attr\(data-ln\)/);
    assert.match(styles, /user-select:\s*none/);
    const block = sandbox.highlightWorkspaceLine(' still', 'javascript', true, true);
    assert.strictEqual(block.inBlock, true);
    assert.ok(block.html.includes('tok-comment'));
    await app.handleWorkspaceEntryClick({ relativePath: 'tools/main.py', isDirectory: false });
    assert.strictEqual(app.workspacePreviewIsCode, true);
    assert.strictEqual(app.workspacePreviewCanShowSource, true);
    assert.strictEqual(app.workspacePreviewCanEdit, true);
    assert.strictEqual(app.workspacePreviewSource, false, 'Opening a script must default to highlighted preview.');
    assert.strictEqual(app.workspacePreviewSrcdoc, '', 'Code preview stays in the page so line numbers are not a second document.');
    assert.ok(app.workspacePreviewCodeHtml.includes('tok-keyword'));
    assert.ok(app.workspacePreviewCodeHtml.includes('data-ln="1"'));
    const jsonRoot = sandbox.buildWorkspaceJsonRoot('{"name":"ncf","ok":true,"items":[1,null]}', false);
    assert.strictEqual(jsonRoot.open, true);
    const jsonRows = sandbox.flattenWorkspaceJson(jsonRoot);
    assert.ok(jsonRows.some(row => row.node && row.node.key === 'name' && row.node.type === 'string'));
    const jsonlRoot = sandbox.buildWorkspaceJsonRoot('{"a":1}\n{bad\n{"b":2}\n', true);
    assert.strictEqual(jsonlRoot.type, 'array');
    assert.strictEqual(jsonlRoot.size, 3);
    assert.strictEqual(sandbox.workspaceJsonChild(jsonlRoot, 1).type, 'invalid');
    assert.ok(template.includes('toggleWorkspaceJsonTree'));
    assert.ok(template.includes('chat-workspace-json-tree'));
    assert.ok(template.includes('chat-workspace-window-bar'));
    app.beginWorkspacePreviewEdit();
    assert.strictEqual(app.workspacePreviewEditing, true);
    app.workspacePreviewDraft += 'print(1)\n';
    assert.strictEqual(app.workspacePreviewDirty, true);
    let saved;
    sandbox.service.post = async (url, body) => {
        saved = { url, body };
        return { data: { success: true, data: { ...workspacePreviewFixture('tools/main.py'), content: body.content, etag: 'etag-py-2', size: 24 } } };
    };
    await app.saveWorkspacePreview();
    assert.ok(saved.url.includes('SaveWorkspaceFileAsync'));
    assert.strictEqual(saved.body.relativePath, 'tools/main.py');
    assert.strictEqual(saved.body.expectedEtag, 'etag-py');
    assert.ok(saved.body.content.includes('print(1)'));
    assert.strictEqual(app.workspacePreviewEditing, false);
    assert.strictEqual(app.workspacePreviewSaving, false);
    await app.handleWorkspaceEntryClick({ relativePath: 'image.png', isDirectory: false });
    assert.strictEqual(app.workspacePreviewCanEdit, false);
    assert.strictEqual(app.workspacePreviewIsCode, false);
    app.workspacePreviewData = {
        path: 'events.jsonl', isText: true, contentType: 'application/jsonl', content: '{"a":1}\n{bad\n{"b":2}\n', etag: 'etag-jsonl'
    };
    assert.strictEqual(app.workspaceCodeLanguage, 'json');
    assert.strictEqual(app.workspacePreviewIsJson, true);
    assert.strictEqual(app.workspacePreviewIsCode, true);
    app.workspacePreviewData = { path: 'notes.txt', isText: true, contentType: 'text/plain', content: 'a\nb', windowed: true };
    assert.strictEqual(app.workspacePreviewCanEdit, false, 'A partial text window must not be saved over the whole file.');
    assert.ok(template.includes('beginWorkspacePreviewEdit'));
    assert.ok(template.includes('chat-workspace-text-editor'));
    assert.ok(styles.includes('.chat-workspace-text-editor'));
    app.workspacePreviewSource = true;
    app.resetWorkspacePreview();
    assert.strictEqual(app.workspaceEntries.length, 0);
    assert.strictEqual(app.workspacePreviewData, null);
    assert.strictEqual(app.workspacePreviewSource, false);
    assert.strictEqual(app.workspaceShareDialogVisible, false);

    app.formatMessageContent = options.methods.formatMessageContent.bind(app);
    const original = 'Line 1\nLine 2\n\n- Item one\n- Item two\n\n```js\nconst x = 1;\n\n\nconst y = 2;\n```';
    const rendered = app.formatMessageContent(original);
    assert.ok(rendered.includes('Line 1<br>'), 'Keep explicit soft line breaks once.');
    assert.ok(rendered.includes('<li>Item one</li>'), 'Keep list structure.');
    assert.ok(rendered.includes('const x = 1;\n\n\nconst y = 2;'), 'Never collapse whitespace inside code blocks.');
    app.scrollLivePreviewToBottom = () => {};
    app.updateActiveTask = () => {};
    app.handleLiveStreamEvent('token', { text: 'A: B; C，D。' });
    app.handleLiveStreamEvent('token', { text: '\nNext line' });
    assert.strictEqual(app.liveAssistantText, 'A: B; C，D。\nNext line',
        'Stream chunks must not turn punctuation or token boundaries into line breaks.');
    app.handleLiveStreamEvent('assistant-phase', {
        text: original, trajectoryEvent: { id: 1, eventType: 'TextReasoningContent', content: original }
    });
    assert.strictEqual(app.liveThinkingText, original, 'Keep original reasoning source untouched.');

    const assetErrors = [];
    app.showWorkspaceAssetError = async error => assetErrors.push(error.message);
    app.workspaceSharePath = 'index.html';
    const share = { id: 'share-1', path: 'index.html', url: '/shared/example' };
    sandbox.service.get = async () => ({ data: { shares: [share, { id: 'other', path: 'other.html' }] } });
    await app.loadWorkspaceShares();
    assert.strictEqual(app.workspaceShares.length, 1, 'Share dialog must show the selected file only.');
    let postRequest;
    sandbox.service.post = async (url, body) => {
        postRequest = { url, body };
        return { data: { share } };
    };
    await app.createWorkspaceShare();
    assert.strictEqual(postRequest.body.path, 'index.html');
    assert.strictEqual(postRequest.body.expiresInHours, 24);
    assert.strictEqual(app.workspaceShareCreating, false);
    sandbox.service.post = async () => ({ data: {} });
    await app.revokeWorkspaceShare(share);
    assert.strictEqual(app.workspaceShares.length, 2, 'Do not report revocation on an invalid/login-shaped response.');
    assert.strictEqual(assetErrors.length, 1);
    sandbox.service.post = async () => ({ data: { revoked: true } });
    await app.revokeWorkspaceShare(share);
    assert.strictEqual(app.workspaceShares.length, 0);
    assert.strictEqual(app.workspaceShareRevoking, '');
    const downloads = [];
    sandbox.URL = { createObjectURL() { return 'blob:fixture'; }, revokeObjectURL() {} };
    sandbox.setTimeout = callback => callback();
    sandbox.document.body.appendChild = () => {};
    sandbox.document.createElement = () => ({
        click() { downloads.push(this.download); }, remove() {}
    });
    sandbox.service.get = async () => ({
        data: 'binary-fixture', headers: { 'content-disposition': "attachment; filename*=UTF-8''%E6%96%87%E4%BB%B6.zip" }
    });
    await app.downloadWorkspaceEntry('folder');
    assert.deepStrictEqual(downloads, ['文件.zip'], 'Use the server filename, including UTF-8 ZIP names.');
    sandbox.service.get = async () => ({ data: '<html>Login</html>', headers: {} });
    await app.downloadWorkspaceEntry('folder');
    assert.strictEqual(downloads.length, 1, 'Never download a login page as a successful asset.');
    assert.strictEqual(app.workspaceDownloading, false);
    assert.strictEqual(assetErrors.length, 2);
    const removedUrls = [];
    sandbox.URL.revokeObjectURL = url => removedUrls.push(url);
    app.pendingAttachments = [
        { relativePath: 'first', useVision: true, localPreviewUrl: 'blob:first' },
        { relativePath: 'second', useVision: true, localPreviewUrl: 'blob:second' },
        { relativePath: 'third', useVision: true, localPreviewUrl: 'blob:third' }
    ];
    app.validatePendingVision(app.pendingAttachments[2]);
    assert.strictEqual(app.pendingAttachments[2].useVision, false, 'Vision must be an explicit opt-in with a two-image limit.');
    sandbox.service.post = async () => ({ data: { deleted: true } });
    await app.removePendingAttachment(app.pendingAttachments[0]);
    assert.strictEqual(app.pendingAttachments.length, 2);
    assert.deepStrictEqual(removedUrls, ['blob:first']);
    app.clearPendingAttachments();
    assert.strictEqual(app.pendingAttachments.length, 0);
    assert.strictEqual(removedUrls.length, 3, 'Removing pending attachments must release local preview URLs.');
    assert.ok(template.includes('@@paste.native="handleWorkspacePaste"'),
        'Both composer surfaces must support file paste.');
    assert.ok(template.includes('@@drop="handleWorkspaceDrop"'),
        'Composer must support dropping files without affecting unrelated drag operations.');
    const fetches = [];
    let attachReads = 0;
    sandbox.TextDecoder = TextDecoder;
    sandbox.Uint8Array = Uint8Array;
    app.loadMemoryMode = async () => {};
    sandbox.fetch = async (url) => {
        fetches.push(String(url));
        if (String(url).includes('/status')) {
            return { ok: true, status: 200, json: async () => ({ running: true, justFinished: false, text: 'partial', thinking: '', status: '正在生成回答…' }) };
        }
        const encoder = new TextEncoder();
        return {
            ok: true,
            status: 200,
            body: { getReader: () => ({ async read() {
                attachReads += 1;
                if (attachReads > 1) return { done: true, value: undefined };
                return { done: false, value: encoder.encode('event: resume\ndata: {"text":"partial"}\n\nevent: done\ndata: {}\n\n') };
            } }) }
        };
    };
    app.currentSessionId = 42;
    app.isSending = false;
    app.messageList = [{ id: 1, content: 'hi' }];
    app.reloaded = false;
    app.loadSessionDetail = async () => { app.reloaded = true; };
    app.createRecentTask = () => { app.taskCreated = true; };
    app.finishActiveTask = () => {};
    app.resetLiveRun = () => { app.liveAssistantText = ''; app.liveThinkingText = ''; app.liveStatusText = ''; };
    app.scrollLivePreviewToBottom = () => {};
    app.updateActiveTask = () => {};
    await app.resumeAdminChatRun();
    assert.ok(fetches.some(url => url.includes('/status')), 'Refresh must ask whether the task is still running.');
    assert.ok(fetches.some(url => url.includes('/attach')), 'A running task must be observed again instead of restarted.');
    assert.ok(!fetches.some(url => url.includes('/send')), 'Reconnecting must not send the prompt again.');
    assert.strictEqual(app.reloaded, true);
    assert.strictEqual(app.isSending, false);
    assert.strictEqual(app.liveAssistantText, 'partial');

    console.log('AdminChat Workspace preview tests passed.');
}

run().catch(error => {
    console.error(error);
    process.exitCode = 1;
});
