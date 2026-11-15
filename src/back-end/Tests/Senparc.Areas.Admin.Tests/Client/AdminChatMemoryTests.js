'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const admin = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
let options;
const requests = [];
const errors = [];
const labels = {
    'AdminChat.Memory.UpdateSaved': 'Saved: global {0}, workspace {1}',
    'AdminChat.Memory.UpdateReason.no-candidates': 'No candidates; not saved',
    'AdminChat.Memory.UpdateReason.intent-deferred': 'Waiting for content; not saved',
    'AdminChat.Memory.UpdateReason.no-save-intent': 'No current save intent; not saved',
    'AdminChat.Memory.UpdateReason.update-failed': 'Update failed'
};
const sandbox = {
    window: { ChatLauncherMixin: {} },
    Vue: function (value) { options = value; },
    ncfT: (key, ...args) => (labels[key] || key).replace(/\{(\d+)\}/g, (_, index) => args[index]),
    console,
    service: {
        get: async url => {
            requests.push({ url });
            return { data: { success: true, data: {
                revision: 'r1', markdown: '# Memory\n\n## Core\n- Keep replies concise\n',
                systemMessage: 'Use short answers', enabled: true, mode: 2, entries: [], history: []
            } } };
        },
        post: async (url, data) => {
            requests.push({ url, data });
            return { data: { success: true, data: {
                revision: 'r2', markdown: data.markdown, enabled: data.mode > 0, mode: data.mode, entries: [], history: []
            } } };
        }
    }
};
vm.runInNewContext(fs.readFileSync(path.join(admin, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js'), 'utf8'), sandbox);
const app = options.data();
for (const [key, value] of Object.entries(options.methods)) app[key] = value.bind(app);
app.$message = { error: message => errors.push(message), success() {}, warning: message => errors.push(message) };

async function run() {
    assert.strictEqual(app.memoryEnabled, false, 'Memory must default off.');
    app.currentSessionId = 42;
    await app.loadMemoryMode();
    assert.strictEqual(app.memoryEnabled, true);
    assert.strictEqual(app.memoryReady, true);
    assert.strictEqual(app.memoryRevision, 'r1');
    await app.changeMemoryMode(false);
    assert.strictEqual(app.memoryEnabled, false);
    const toggle = requests.at(-1);
    assert.strictEqual(toggle.data.revision, 'r1');
    assert.strictEqual(toggle.data.sessionId, 42);
    assert.strictEqual(toggle.data.mode, 0);
    await app.changeMemoryMode(1);
    assert.strictEqual(app.memoryMode, 1);
    assert.strictEqual(app.memoryEnabled, true);
    assert.strictEqual(requests.at(-1).data.mode, 1);
    assert.ok(!('userId' in toggle.data), 'The user identity must come from the server principal.');

    app.isSuperAdmin = false;
    await app.openMemoryDialog();
    assert.strictEqual(app.memoryDialogVisible, true);
    assert.strictEqual(app.memoryDraftRevision, 'r1');
    assert.ok(!requests.some(item => item.url.includes('GlobalChat')), 'Do not fetch tenant settings for ordinary users.');
    app.memoryMarkdown = '';
    await app.saveMemory();
    assert.strictEqual(requests.at(-1).data.markdown, '', 'Empty Markdown must support deletion.');
    assert.strictEqual(app.memoryDraftRevision, 'r2');
    assert.strictEqual(requests.at(-1).data.systemMessage, 'Use short answers');

    app.memoryMarkdown = 'unsaved edit';
    const originalPost = sandbox.service.post;
    sandbox.service.post = async () => ({ data: { success: false, errorMessage: 'revision conflict' } });
    await app.saveMemory();
    assert.strictEqual(app.memoryMarkdown, 'unsaved edit', 'Conflict must preserve the editor draft.');
    assert.strictEqual(app.memoryDraftRevision, 'r2', 'Conflict must not claim a new revision.');
    assert.ok(errors.includes('revision conflict'));
    sandbox.service.post = originalPost;

    let finish;
    sandbox.service.get = () => new Promise(resolve => { finish = resolve; });
    const loading = app.loadMemoryMode();
    app.currentSessionId = 43;
    finish({ data: { success: true, data: { enabled: true, revision: 'old-session' } } });
    await loading;
    assert.strictEqual(app.memoryEnabled, false, 'Stale session responses must not enable a different session.');
    assert.notStrictEqual(app.memoryRevision, 'old-session');

    sandbox.service.get = async () => ({ data: '<html>Login</html>' });
    await app.loadMemoryMode();
    assert.strictEqual(app.memoryReady, false, 'A login page is not a successful Memory load.');
    assert.strictEqual(app.memoryBusy, false);

    const page = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'), 'utf8');
    assert.ok(page.includes('v-html="formatMemoryContent(memoryMarkdown)"'), 'Preview must use the resource-blocking sanitizing renderer.');
    assert.ok(page.includes(':disabled="memoryBusy || isCurrentSessionRunning || !memoryReady"'));
    assert.ok(page.includes('handleMemoryCommand($event, true)'));
    assert.ok(!page.includes('v-model="launcherUseMemory"'), 'The oversized Memory checkbox must be removed.');
    assert.ok(page.includes('memoryModeLabel(memoryMode)'));
    assert.ok(page.includes('@@on-confirm="saveMemoryEntry(true)"'), 'Use the installed Element UI confirmation event.');
    assert.ok(page.includes('memoryUpdateText(memoryUpdateResult)'), 'Show the server save result independently of the answer.');
    assert.ok(page.includes('AdminChat.Memory.StorageNotice'), 'Explain database storage versus Workspace files.');
    const dialog = page.slice(page.indexOf('<el-dialog :title="ncfT(\'AdminChat.Memory.Settings\')'));
    assert.ok(dialog.includes('slot="title" class="chat-memory-heading"'), 'Keep the dialog title and subtitle structured.');
    assert.ok(dialog.includes('<details class="chat-memory-help">'), 'Keep long guidance collapsed and keyboard-accessible.');
    assert.ok(!dialog.includes('<details class="chat-memory-help" open'), 'Guidance should not crowd the default view.');
    assert.ok(dialog.includes('class="chat-memory-footer"'), 'Group dialog-level actions in a separate footer.');
    assert.ok(dialog.includes('@@click="memoryEntryDraft = null"'), 'Entry drafts must have a cancel action.');
    assert.ok(dialog.includes('AdminChat.Memory.SaveEntry'), 'Distinguish entry saves from dialog settings saves.');
    assert.ok(dialog.includes('AdminChat.Memory.EmptyLoaded'), 'Explain the absence of a loading report.');
    const css = fs.readFileSync(path.join(admin, 'wwwroot/css/Admin/Pages/AdminChat/Chat.css'), 'utf8');
    assert.ok(css.includes('.chat-memory-dialog .el-dialog__footer'), 'The appended dialog must have independently scoped styles.');
    assert.ok(css.includes('grid-template-columns: repeat(2, minmax(0, 1fr))'), 'Keep narrow-screen footer actions aligned.');
    vm.runInNewContext(fs.readFileSync(path.join(admin, 'wwwroot/js/Admin/Pages/AdminChat/ChatLauncherMixin.js'), 'utf8'), sandbox);
    const mixin = sandbox.window.ChatLauncherMixin;
    const launcher = Object.assign(mixin.data(), {
        normalizeAiModelId: id => Number(id || 0), isSuperAdmin: false
    });
    assert.strictEqual(mixin.methods.launcherSessionRequest.call(launcher, 'new chat').useMemory, false);
    launcher.launcherUseMemory = true;
    assert.strictEqual(mixin.methods.launcherSessionRequest.call(launcher, 'new chat').useMemory, true);
    launcher.launcherMemoryMode = 1;
    assert.strictEqual(mixin.methods.launcherSessionRequest.call(launcher, 'new chat').memoryMode, 1);
    app.reportMemoryUpdate('read-only');
    app.reportMemoryUpdate('skipped-input');
    assert.ok(!errors.some(error => error.includes('UpdateWarning')), 'Expected skipped/read-only states are not failures.');
    app.memoryMarkdown = 'unsaved';
    app.memorySavedMarkdown = '';
    app.editMemoryEntry(null);
    assert.strictEqual(app.memoryEntryDraft, null, 'Entry editing must not discard an unsaved Markdown draft.');
    app.reportMemoryUpdate('unchanged', null, 42,
        { status: 'unchanged', reason: 'no-candidates', requested: true, globalChanges: 0, workspaceChanges: 0 });
    assert.strictEqual(app.memoryUpdateSessionId, 42);
    assert.strictEqual(app.memoryUpdateText(app.memoryUpdateResult), 'No candidates; not saved');
    for (const reason of ['intent-deferred', 'no-save-intent']) {
        app.reportMemoryUpdate('unchanged', null, 42,
            { status: 'unchanged', reason, requested: true, globalChanges: 0, workspaceChanges: 0 });
        assert.strictEqual(app.memoryUpdateText(app.memoryUpdateResult), labels['AdminChat.Memory.UpdateReason.' + reason]);
        assert.ok(!errors.some(error => error.includes('UpdateWarning')));
    }
    assert.ok(page.includes("['intent-deferred', 'no-save-intent'].includes(memoryUpdateResult.reason) ? 'info'"),
        'Non-saving intent is informational, not a failure or successful save.');
    app.reportMemoryUpdate('updated', null, 43,
        { status: 'updated', reason: 'saved', requested: true, globalChanges: 1, workspaceChanges: 0 });
    assert.strictEqual(app.memoryUpdateText(app.memoryUpdateResult), 'Saved: global 1, workspace 0');
    assert.strictEqual(app.memoryUpdateText({ status: 'failed', reason: '<script>untrusted</script>' }), 'Update failed',
        'Never render exception/model text as a save reason.');
    app.reportMemoryUpdate('disabled', null, 43);
    assert.strictEqual(app.memoryUpdateResult, null, 'A response without a report must not retain an older save confirmation.');
    const read = async () => ({ data: { success: true, data: {
        revision: 'r1', markdown: '# Memory\n', systemMessage: '', entries: [], history: [], workspaceId: ''
    } } });
    sandbox.service.get = read;
    app.currentSessionId = 42;
    app.isSuperAdmin = false;
    app.memoryDialogVisible = false;
    const candidate = () => ({
        entry: { id: '', version: 0, text: 'Fictional character alias: Alpha', topic: 'Alpha',
            tier: 'topic', kind: 'reference', pinned: true, workspaceId: '', expiresAt: null },
        remove: false
    });
    const propose = proposals => {
        const result = { revision: 'r1', status: 'pending-review', reason: 'pending-review',
            requested: true, proposals };
        app.reportMemoryUpdate('pending-review', null, 42, result);
        return result;
    };
    const first = candidate();
    const second = candidate();
    second.entry.text = 'Fictional character alias: Beta';
    propose([first, second]);
    const writes = () => requests.filter(item => item.data).length;
    const before = writes();
    await app.reviewMemoryProposal(first);
    assert.strictEqual(writes(), before, 'Opening review must never save.');
    assert.strictEqual(app.memoryEntryDraft.text, first.entry.text);
    assert.strictEqual(app.memoryEntryDraft.scope, 'global');
    assert.strictEqual(app.memoryTab, 'entries');
    assert.strictEqual(app.memoryProposalReview.proposal, first);
    sandbox.service.post = async () => ({ data: { success: false, errorMessage: 'candidate conflict' } });
    await app.saveMemoryEntry(false);
    assert.strictEqual(app.memoryEntryDraft.text, first.entry.text, 'Failed approval preserves the draft.');
    assert.strictEqual(app.memoryUpdateResult.proposals.length, 2);
    sandbox.service.post = originalPost;
    await app.saveMemoryEntry(false);
    assert.strictEqual(writes(), before + 1);
    assert.strictEqual(requests.at(-1).data.entry.kind, 'reference');
    assert.strictEqual(requests.at(-1).data.revision, 'r1');
    assert.strictEqual(requests.at(-1).data.sessionId, 42);
    assert.strictEqual(app.memoryUpdateResult.proposals.length, 1);
    assert.strictEqual(app.memoryUpdateResult.revision, 'r2');
    assert.strictEqual(app.memoryProposalReview, null);
    app.memoryDialogVisible = false;
    await app.reviewMemoryProposal(second);
    assert.strictEqual(app.memoryEntryDraft, null, 'A stale candidate cannot overwrite a newer revision.');
    assert.ok(errors.includes('AdminChat.Memory.UpdateReason.revision-conflict'));

    app.memoryDialogVisible = false;
    propose([second]);
    app.currentSessionId = 43;
    const beforeReads = requests.length;
    await app.reviewMemoryProposal(second);
    assert.strictEqual(requests.length, beforeReads, 'A proposal from another session must not load or save.');
    app.currentSessionId = 42;
    sandbox.service.get = () => new Promise(resolve => { finish = resolve; });
    const reviewing = app.reviewMemoryProposal(second);
    app.currentSessionId = 43;
    finish(await read());
    await reviewing;
    assert.strictEqual(app.memoryDialogVisible, false, 'Navigation while loading must not open a stale candidate.');
    sandbox.service.get = read;
    app.currentSessionId = 42;
    app.memoryBusy = false;
    propose([second]);
    await app.reviewMemoryProposal(second);
    app.memoryDialogVisible = false;
    app.memoryEntryDraft = null;
    assert.strictEqual(app.memoryUpdateResult.proposals.length, 1, 'Closing review must leave an unsaved candidate.');
    await app.reviewMemoryProposal(second);
    await app.saveMemoryEntry(false);
    assert.strictEqual(app.memoryUpdateResult.status, 'reviewed');
    assert.strictEqual(app.memoryUpdateResult.proposals.length, 0);

    for (const locale of ['', '.zh-CN', '.en', '.ja', '.fr', '.es', '.ru']) {
        const resource = fs.readFileSync(path.join(admin, 'Resources/AdminResource' + locale + '.resx'), 'utf8');
        for (const key of ['ReviewCandidate', 'ReviewNotice', 'ReviewClose', 'CandidateEvidence',
            'UpdateReason.pending-review', 'UpdateReason.unresolved-reference', 'UpdateReason.review-complete',
            'UpdateReason.intent-deferred', 'UpdateReason.no-save-intent']) {
            assert.strictEqual(resource.split('name="AdminChat.Memory.' + key + '"').length - 1, 1);
        }
    }
    console.log('AdminChat Memory client tests passed.');
}

run().catch(error => { console.error(error); process.exitCode = 1; });
