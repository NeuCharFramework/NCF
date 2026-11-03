'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const admin = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
let options;
const sandbox = {
    Vue: function(value) { options = value; },
    window: { ChatLauncherMixin: {} },
    console: { error() {} },
    ncfT: key => key,
    AbortController, TextDecoder, Uint8Array,
    service: {}
};
vm.runInNewContext(fs.readFileSync(path.join(admin, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js'), 'utf8'), sandbox);
const deferred = () => {
    let resolve, reject;
    const promise = new Promise((yes, no) => { resolve = yes; reject = no; });
    return { promise, resolve, reject };
};
const result = data => ({ data: { success: true, data } });
function createApp() {
    const app = options.data();
    for (const [name, method] of Object.entries(options.methods)) app[name] = method.bind(app);
    for (const [name, getter] of Object.entries(options.computed))
        Object.defineProperty(app, name, { get: getter.bind(app) });
    app.$set = (object, key, value) => { object[key] = value; };
    app.$nextTick = fn => fn();
    app.$refs = {};
    app.$message = { success() {}, error() {}, info() {}, warning() {} };
    app.scrollToBottom = () => {};
    app.stopTrajectoryReplay = () => {};
    app.resetWorkspacePreview = () => {};
    app.deletePendingAttachments = async () => {};
    app.clearPendingAttachments = () => {};
    app.getSessionAiModelId = () => 0;
    app.normalizeAiModelId = value => value;
    app.loadMemoryMode = async () => {};
    app.loadSessionList = async () => {};
    app.loadTrajectoryList = async () => {};
    app.reportMemoryUpdate = () => {};
    app.generateSessionTitleAsync = async (...args) => { app.generatedTitleArgs = args; };
    return app;
}

async function streamingNavigation() {
    const app = createApp();
    app.currentSessionId = 1;
    app.currentSessionAiModelId = 7;
    app.memoryReady = true;
    app.inputMessage = 'A request';
    const stream = deferred();
    app.consumeAdminChatStream = () => stream.promise;
    sandbox.service.get = async url => {
        const sessionId = Number(new URL('http://test' + url).searchParams.get('sessionId'));
        return result({ session: { title: `session ${sessionId}`, messages: [{ id: sessionId * 100, content: 'history' }] } });
    };
    const sending = app.sendMessage();
    const tempId = app.runningMessages[0].id;
    assert.ok(app.isCurrentSessionRunning);
    app.handleLiveStreamEvent('user-message', { id: 11, content: 'A request' }, tempId, 1);
    await app.switchSession(2);
    const bMessages = app.messageList;
    assert.ok(!app.isCurrentSessionRunning, 'B must not show A thinking or stop controls.');
    assert.ok(app.isSessionRunning({ id: 1 }), 'A must retain its sidebar activity.');
    app.handleLiveStreamEvent('assistant-phase', {
        trajectoryId: 10, text: 'A reasoning',
        trajectoryEvent: { eventType: 'TextReasoningContent', sequence: 1, content: 'A reasoning' }
    }, tempId, 1);
    app.handleLiveStreamEvent('trajectory-event', {
        trajectoryId: 10, trajectoryEvent: { eventType: 'tool.call', sequence: 2, content: 'A tool' }
    }, tempId, 1);
    app.handleLiveStreamEvent('usage', { totalTokens: 99 }, tempId, 1);
    assert.strictEqual(app.latestUsage, null, 'Background token usage must not leak into B.');
    assert.strictEqual(app.messageList, bMessages);
    let stopped = false;
    app.activeRequestController = { abort() { stopped = true; } };
    app.stopCurrentRequest();
    assert.ok(!stopped, 'B cannot stop A through the current-session stop action.');
    await app.switchSession(1);
    assert.ok(app.isCurrentSessionRunning);
    assert.strictEqual(app.liveThinkingText, 'A reasoning');
    assert.strictEqual(app.liveTrajectoryEvents.length, 2);
    assert.strictEqual(app.recentTasks.length, 1, 'Returning to A must preserve its active task.');
    assert.strictEqual(app.latestUsage.totalTokens, 99);
    assert.strictEqual(app.messageList, app.runningMessages, 'Returning to A must reattach its stream buffer.');
    app.handleLiveStreamEvent('token', { text: 'A answer' }, tempId, 1);
    await app.switchSession(2);
    app.inputMessage = 'B draft';
    app.handleLiveStreamEvent('assistant-message', { id: 12, content: 'A final' }, tempId, 1);
    assert.strictEqual(app.messageList[0].id, 200);
    assert.strictEqual(app.runningMessages.at(-1).id, 12);
    stream.resolve();
    await sending;
    assert.strictEqual(app.inputMessage, 'B draft');
    assert.strictEqual(app.currentSessionId, 2);
    assert.strictEqual(app.messageList.length, 1);
    assert.deepStrictEqual(app.generatedTitleArgs, [1, 7], 'Title generation must target A, not the current session.');
    assert.ok(!app.isSending);
}

async function backgroundFailure() {
    for (const aborted of [false, true]) {
        const app = createApp();
        app.currentSessionId = 1;
        app.memoryReady = true;
        app.inputMessage = 'A request';
        const stream = deferred();
        app.consumeAdminChatStream = () => stream.promise;
        const sending = app.sendMessage();
        app.currentSessionId = 2;
        app.messageList = [{ id: 200 }];
        app.inputMessage = 'B draft';
        app.pendingAttachments = [{ relativePath: 'B attachment' }];
        const error = new Error('A failed');
        if (aborted) error.name = 'AbortError';
        stream.reject(error);
        await sending;
        assert.strictEqual(app.messageList[0].id, 200);
        assert.strictEqual(app.inputMessage, 'B draft', 'A failure must never restore A input into B.');
        assert.strictEqual(app.pendingAttachments[0].relativePath, 'B attachment');
    }
}

async function staleLoads() {
    const app = createApp();
    const a = deferred(), b = deferred();
    app.currentSessionId = 1;
    sandbox.service.get = () => a.promise;
    const loadA = app.loadSessionDetail();
    app.currentSessionId = 2;
    sandbox.service.get = () => b.promise;
    const loadB = app.loadSessionDetail();
    a.resolve(result({ session: { title: 'stale A', messages: [{ id: 11 }] } }));
    await loadA;
    assert.strictEqual(app.currentSessionTitle, '');
    assert.strictEqual(app.loadingMessages, true, 'A finally must not clear B loading state.');
    b.resolve(result({ session: { title: 'B', messages: [{ id: 22 }] } }));
    await loadB;
    assert.strictEqual(app.currentSessionTitle, 'B');
    assert.strictEqual(app.messageList[0].id, 22);

    // Same-session requests must also be ordered (A -> B -> A).
    app.currentSessionId = 1;
    const older = deferred(), newer = deferred();
    sandbox.service.get = () => older.promise;
    const oldLoad = app.loadSessionDetail();
    sandbox.service.get = () => newer.promise;
    const newLoad = app.loadSessionDetail();
    newer.resolve(result({ session: { title: 'new A', messages: [] } }));
    await newLoad;
    older.resolve(result({ session: { title: 'old A', messages: [] } }));
    await oldLoad;
    assert.strictEqual(app.currentSessionTitle, 'new A');

    const trajectory = deferred();
    sandbox.service.get = () => trajectory.promise;
    const oldTrajectory = options.methods.loadTrajectoryList.call(app);
    app.currentSessionId = 2;
    app.resetSessionTrajectoryView();
    trajectory.resolve(result({ trajectories: [{ id: 10 }] }));
    await oldTrajectory;
    assert.strictEqual(app.trajectoryList.length, 0);
    app.currentSessionId = 1;
    const detail = deferred();
    sandbox.service.get = () => detail.promise;
    const oldDetail = app.openTrajectory(10);
    app.currentSessionId = 2;
    app.resetSessionTrajectoryView();
    detail.resolve(result({ trajectory: { id: 10 }, events: [{ content: 'A trace' }] }));
    await oldDetail;
    assert.strictEqual(app.selectedTrajectory, null);
    assert.strictEqual(app.selectedTrajectoryEvents.length, 0);
    assert.strictEqual(app.trajectoryDialogVisible, false);

    app.currentSessionId = 1;
    app.runningSessionId = 1;
    app.isSending = true;
    app.runningMessages = [{ id: 11 }];
    const pending = deferred();
    sandbox.service.get = () => pending.promise;
    const whileRunning = app.loadSessionDetail();
    const completedMessages = app.runningMessages;
    completedMessages.push({ id: 12, content: 'final answer' });
    app.isSending = false;
    app.runningSessionId = 0;
    app.runningMessages = [];
    pending.resolve(result({ session: { title: 'A', messages: [{ id: 11 }] } }));
    await whileRunning;
    assert.strictEqual(app.messageList, completedMessages,
        'A detail load started during generation must not overwrite the final streamed answer after completion.');
}

async function lateHarnessResults() {
    for (const action of ['resume', 'fork', 'approval']) {
        const app = createApp();
        app.currentSessionId = 1;
        const response = deferred();
        sandbox.service.post = () => response.promise;
        const task = action === 'approval'
            ? app.respondApproval(10, { requestId: 'approval' }, true)
            : app.runTrajectoryAction(action, { trajectoryId: 10 });
        app.currentSessionId = 2;
        app.messageList = [{ id: 200 }];
        response.resolve(result({
            sessionId: action === 'fork' ? 3 : 1,
            assistantMessage: { id: 12 }, harness: { trajectoryId: 10 }
        }));
        await task;
        assert.strictEqual(app.currentSessionId, 2, `${action} must not navigate away from B.`);
        assert.strictEqual(app.messageList.length, 1);
        assert.strictEqual(app.messageList[0].id, 200);
        assert.strictEqual(app.selectedTrajectory, null);
    }
}

async function streamRouting() {
    const app = createApp();
    app.currentSessionId = 2;
    app.runningSessionId = 1;
    let loadedMemory = false;
    app.loadMemoryMode = async () => { loadedMemory = true; };
    const encoder = new TextEncoder();
    let read = false;
    sandbox.fetch = async () => ({
        ok: true, body: { getReader: () => ({ async read() {
            if (read) return { done: true };
            read = true;
            return { done: false, value: encoder.encode(
                'event: assistant-message\ndata: {"id":12,"content":"A final"}\n\n' +
                'event: done\ndata: {}\n\n') };
        } }) }
    });
    await app.consumeAdminChatStream({ sessionId: 1 }, 'temp');
    assert.strictEqual(app.messageList.length, 0);
    assert.strictEqual(app.runningMessages[0].id, 12);
    assert.ok(!loadedMemory, 'Background completion must not reload B Memory mode.');

    const before = app.liveAssistantText;
    app.handleLiveStreamEvent('token', { text: 'wrong session' }, 'temp', 99);
    assert.strictEqual(app.liveAssistantText, before, 'Events with a different run owner must be rejected.');
}

async function run() {
    await streamingNavigation();
    await backgroundFailure();
    await staleLoads();
    await lateHarnessResults();
    await streamRouting();
    const page = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'), 'utf8');
    assert.ok(page.includes('v-if="isAIResponding && isCurrentSessionRunning"'));
    assert.ok(page.includes('v-if="isCurrentSessionRunning"'));
    console.log('AdminChat session isolation tests passed.');
}
run().catch(error => { console.error(error); process.exitCode = 1; });
