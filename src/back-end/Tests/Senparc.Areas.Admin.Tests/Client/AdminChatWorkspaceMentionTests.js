'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const adminPath = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const mentionSource = fs.readFileSync(path.join(adminPath, 'wwwroot/js/Admin/Pages/AdminChat/WorkspaceMentions.js'), 'utf8');
const mentionWindow = {};
vm.runInNewContext(mentionSource, { window: mentionWindow }, { filename: 'WorkspaceMentions.js' });
const api = mentionWindow.NcfWorkspaceMentions;

assert.strictEqual(api.findActive('请看 @src/Ad', '请看 @src/Ad'.length).query, 'src/Ad');
assert.strictEqual(api.findActive('user@host.com', 10), null, 'An email address must not open path suggestions.');
assert.strictEqual(api.findActive('先看 @docs/ 再继续', 9).query, 'docs/');
assert.strictEqual(api.findActive('@notes', 0), null);

const inserted = api.apply('请看 @src/Ad', { start: 3, end: 10 }, { relativePath: 'src/Admin/Chat.cs', isDirectory: false });
assert.strictEqual(inserted.value, '请看 @src/Admin/Chat.cs ');
assert.strictEqual(inserted.caret, '请看 @src/Admin/Chat.cs '.length);
const directory = api.apply('@sr', { start: 0, end: 3 }, { relativePath: 'src', isDirectory: true });
assert.strictEqual(directory.value, '@src/');
assert.strictEqual(api.findActive(directory.value, directory.caret).query, 'src/');

const page = fs.readFileSync(path.join(adminPath, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'), 'utf8');
assert.ok(page.includes("workspaceMention.field === 'inputMessage'"));
assert.ok(page.includes("workspaceMention.field === 'chatInputText'"));
assert.ok(page.includes('SuggestWorkspacePathsAsync') === false, 'The page only renders the picker; Chat.js calls the API.');

const scriptPath = path.join(adminPath, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js');
let options;
const requests = [];
const sandbox = {
    Vue: function (value) { options = value; },
    window: { ChatLauncherMixin: {}, NcfWorkspaceMentions: api, innerWidth: 1440 },
    getComputedStyle() { return { getPropertyValue() { return '360px'; } }; },
    document: {
        addEventListener() {},
        removeEventListener() {},
        body: { classList: { add() {}, remove() {} } },
        querySelector() { return null; }
    },
    service: {
        async get(url, config) {
            requests.push({ url, params: config.params });
            return { data: { success: true, data: { items: [
                { relativePath: 'src/Admin', name: 'Admin', isDirectory: true },
                { relativePath: 'src/Admin/Chat.cs', name: 'Chat.cs', isDirectory: false }
            ] } } };
        }
    },
    console,
    setTimeout,
    clearTimeout
};
sandbox.marked = { parse: value => value };
sandbox.DOMPurify = { sanitize: html => html };
vm.runInNewContext(fs.readFileSync(scriptPath, 'utf8'), sandbox, { filename: scriptPath });
const app = options.data();
Object.entries(options.methods).forEach(([name, method]) => { app[name] = method.bind(app); });
app.$nextTick = callback => callback();
app.$message = { warning() {}, error(message) { throw new Error(message); } };
app.isSuperAdmin = true;
app.canUploadWorkspaceFiles = true;
app.currentSessionId = 7;
app.currentSessionWorkspaceId = 'ws_demo';
app.isCurrentSessionRunning = false;
const textarea = { value: '', selectionStart: 0, focus() {}, setSelectionRange(start) { this.selectionStart = start; } };
const input = { $refs: { textarea } };
app.$refs = { promptMessageInput: input, promptLauncherInput: input };

async function run() {
    textarea.value = '请修改 @src/';
    textarea.selectionStart = textarea.value.length;
    app.handleWorkspaceMentionEdit('inputMessage', { type: 'input', target: textarea });
    await new Promise(resolve => setTimeout(resolve, 220));
    assert.strictEqual(app.workspaceMention.open, true);
    assert.strictEqual(requests[0].params.sessionId, 7);
    assert.strictEqual(requests[0].params.query, 'src/');
    assert.strictEqual(app.workspaceMention.items.length, 2);

    const event = { key: 'ArrowDown', preventDefault() { this.defaultPrevented = true; } };
    assert.strictEqual(app.handleWorkspaceMentionKeydown(event, 'inputMessage'), true);
    assert.strictEqual(app.workspaceMention.active, 1);
    app.selectWorkspaceMention(app.workspaceMention.items[1]);
    assert.strictEqual(app.inputMessage, '请修改 @src/Admin/Chat.cs ');
    assert.strictEqual(app.workspaceMention.open, false, 'Selecting a file closes the picker.');

    textarea.value = '@';
    textarea.selectionStart = 1;
    app.currentSessionId = 0;
    app.selectedWorkspaceId = 'ws_launcher';
    app.handleWorkspaceMentionEdit('chatInputText', { type: 'input', target: textarea });
    await new Promise(resolve => setTimeout(resolve, 20));
    assert.strictEqual(requests[1].params.workspaceId, 'ws_launcher');
    assert.strictEqual(requests[1].params.query, '');
    app.selectWorkspaceMention(app.workspaceMention.items[0]);
    assert.strictEqual(app.chatInputText, '@src/Admin/');
    assert.strictEqual(app.workspaceMention.open, true, 'Selecting a directory keeps the picker open for the next segment.');
    console.log('AdminChat Workspace mention tests passed.');
}

run().catch(error => {
    console.error(error);
    process.exit(1);
});
