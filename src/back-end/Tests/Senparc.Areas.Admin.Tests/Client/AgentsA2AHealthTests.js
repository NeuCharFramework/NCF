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
const remoteServicePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/RemoteAgentAppService.cs');
const remoteResponsePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Application/DTOs/RemoteAgentResponse.cs');
const agentDtoPath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Domain/Models/DatabaseModel/Dto/AgentTemplateDto.cs');
const agentServicePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/AgentTemplateAppService.cs');
const stylePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/wwwroot/css/AgentsManager/index.css');
const script = fs.readFileSync(scriptPath, 'utf8');
const page = fs.readFileSync(pagePath, 'utf8');
const remoteServiceSource = fs.readFileSync(remoteServicePath, 'utf8');
const remoteResponseSource = fs.readFileSync(remoteResponsePath, 'utf8');
const agentDtoSource = fs.readFileSync(agentDtoPath, 'utf8');
const agentServiceSource = fs.readFileSync(agentServicePath, 'utf8');
const styleSource = fs.readFileSync(stylePath, 'utf8');

let capturedOptions = null;
let capturedRequest = null;

function Vue(options) {
    capturedOptions = options;
    return options;
}
Vue.component = function () { };
Vue.directive = function () { };

const serviceAM = {
    async post(url, body) {
        capturedRequest = { url, body };
        return {
            data: {
                success: true,
                data: {
                    results: [{
                        remoteAgentId: 7,
                        name: '外部研究员',
                        success: true,
                        message: 'A2A Agent Card 读取成功：研究员',
                        remoteAgentDto: {
                            id: 7,
                            name: '外部研究员',
                            connectionStatus: 1,
                            lastHealthCheckAt: '2026-08-13T10:00:00Z',
                            lastHealthCheckMessage: 'A2A Agent Card 读取成功：研究员'
                        }
                    }]
                }
            }
        };
    },
    async get() { return { data: { success: true, data: {} } }; }
};

const context = vm.createContext({
    Vue,
    serviceAM,
    console: { log() { }, warn() { }, error() { } },
    window: {},
    document: {},
    setTimeout() { },
    clearTimeout() { },
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
    formatDate(value) { return `formatted:${value}`; }
});

function createViewModel() {
    const viewModel = Object.assign({
        $refs: {},
        $options: { data: capturedOptions.data },
        $set(target, key, value) { target[key] = value; },
        $nextTick(callback) { callback(); },
        $message: { success() { }, error() { }, warning() { } },
        $alert() { },
        $confirm() {
            viewModel.confirmCount += 1;
            return Promise.resolve();
        },
        confirmCount: 0
    }, capturedOptions.data());
    Object.keys(capturedOptions.methods).forEach(name => {
        viewModel[name] = capturedOptions.methods[name].bind(viewModel);
    });
    Object.keys(capturedOptions.computed).forEach(name => {
        Object.defineProperty(viewModel, name, {
            configurable: true,
            get: capturedOptions.computed[name].bind(viewModel)
        });
    });
    return viewModel;
}

async function run() {
    vm.runInContext(script, context, { filename: scriptPath });
    assert.ok(capturedOptions, 'AgentsManager Vue options should be captured.');

    const viewModel = createViewModel();
    viewModel.remoteAgentList = [{ id: 7, name: '外部研究员', connectionStatus: 0 }];
    viewModel.groupStartParticipants = [{ id: 7, name: '外部研究员', agentKind: 'RemoteA2A', connectionStatus: 0, enable: true }];
    viewModel.taskMemberList = [{
        id: 7,
        name: '外部研究员',
        description: '负责提供外部检索建议。',
        participantKey: 'remote:7',
        agentKind: 'RemoteA2A',
        connectionStatus: 0,
        enable: true
    }];
    viewModel.groupDetails = {
        remoteMemberDtoList: [{ remoteAgentDto: { id: 7, connectionStatus: 0 } }]
    };

    const results = await viewModel.testRemoteAgentConnections([7], { silent: true });
    assert.strictEqual(capturedRequest.url,
        '/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.TestConnections');
    assert.deepStrictEqual(JSON.parse(JSON.stringify(capturedRequest.body)), { remoteAgentIds: [7] });
    assert.strictEqual(results[0].success, true);
    assert.strictEqual(viewModel.remoteAgentList[0].connectionStatus, 1, 'Remote manager rows should refresh immediately.');
    assert.strictEqual(viewModel.groupStartParticipants[0].connectionStatus, 1, 'Group-start A2A chips should refresh immediately.');
    assert.strictEqual(viewModel.taskMemberList[0].connectionStatus, 1, 'Task A2A rows should refresh immediately.');
    assert.strictEqual(viewModel.groupDetails.remoteMemberDtoList[0].remoteAgentDto.connectionStatus, 1,
        'Group detail data should retain the latest remote health state.');
    assert.strictEqual(viewModel.remoteParticipantAvailabilityText(viewModel.taskMemberList[0]), '可用');
    assert.strictEqual(viewModel.remoteParticipantAvailabilityType(viewModel.taskMemberList[0]), 'success');

    let editedRemoteAgentId = null;
    viewModel.getRemoteAgentListData = async () => {
        viewModel.remoteAgentList = [{ id: 7, name: '外部研究员' }];
    };
    viewModel.openRemoteAgentEditor = remoteAgent => {
        editedRemoteAgentId = remoteAgent.id;
    };
    context.window.location = { hash: '#tab=remoteA2A&view=edit&remoteAgentId=7' };
    await viewModel.applyHashRoute();
    assert.strictEqual(viewModel.visible.drawerRemoteAgent, true,
        'A remote-A2A route should open the remote agent manager.');
    assert.strictEqual(editedRemoteAgentId, 7,
        'A remote-A2A edit route should open the requested remote agent editor.');

    viewModel.taskHistoryList = [{
        fromParticipantKey: 'remote:7',
        promptTokens: 20,
        completionTokens: 30,
        totalTokens: 50,
        responseMilliseconds: 800
    }];
    const quickInfo = viewModel.getParticipantQuickInfo('task', viewModel.taskMemberList[0]);
    assert.strictEqual(quickInfo.kindText, '远程 A2A');
    assert.strictEqual(quickInfo.statusText, '可用');
    assert.strictEqual(quickInfo.currentUsageText, '1 条回复 · 50 Token');
    assert.strictEqual(quickInfo.responseTimeText, '800ms');

    const humanParticipant = {
        id: 9,
        name: 'Human',
        description: '系统保留的 Human-in-the-Loop 文本参与者。',
        participantKey: 'human:9',
        agentKind: 'Human',
        isHuman: true,
        enable: true
    };
    viewModel.taskMemberList = [humanParticipant];
    viewModel.taskHistoryList = [];
    viewModel.renderSafeMarkdown = text => text;
    viewModel.flushTaskStreamMessage('task', {
        data: JSON.stringify({
            chatTaskId: 101,
            historyId: 99,
            fromAgentTemplateId: 9,
            fromParticipantKey: 'human:9',
            fromParticipantKind: 'Human',
            fromAgentName: 'Human',
            responseId: 'human-response-1',
            text: '请继续处理下一步。',
            isFinal: true,
            timestamp: '2026-08-17T11:07:55Z'
        })
    });
    const humanQuickInfo = viewModel.getParticipantQuickInfo('task', humanParticipant);
    assert.strictEqual(viewModel.taskHistoryList[0].fromParticipantKey, 'human:9',
        'A finalized stream message must retain the Human participant key.');
    assert.strictEqual(humanQuickInfo.currentUsageText, '已输入 1 条文本',
        'Human input should count as a task contribution without being treated as a model response.');
    assert.strictEqual(humanQuickInfo.totalUsageText, '不产生模型 Token');
    assert.strictEqual(humanQuickInfo.canOpenEditor, false,
        'The system Human participant must not expose an editor.');

    let openedAgentEditor = null;
    context.window.open = (url, target) => {
        openedAgentEditor = { url, target, focused: false };
        return {
            focus() {
                openedAgentEditor.focused = true;
            }
        };
    };
    viewModel.openParticipantAgentEditor({ id: 12, agentKind: 'Local' });
    assert.deepStrictEqual(JSON.parse(JSON.stringify(openedAgentEditor)), {
        url: '/Admin/AgentsManager/Index#tab=first&view=edit&agentId=12',
        target: 'NcfAgentsManager_Local_12',
        focused: true
    }, 'Local Agent task members should open their editor in a separate window.');
    viewModel.openParticipantAgentEditor({ id: 7, agentKind: 'RemoteA2A' });
    assert.strictEqual(openedAgentEditor.url,
        '/Admin/AgentsManager/Index#tab=remoteA2A&view=edit&remoteAgentId=7',
        'Remote A2A task members should open their own editor route.');

    viewModel.agentList = [
        { id: 1, name: '高用量 Agent', enable: true, totalTokens: 1600, completedConversationRounds: 12, chattingCount: 2, score: 96 },
        { id: 2, name: '低用量 Agent', enable: true, totalTokens: 100, completedConversationRounds: 4, chattingCount: 0, score: 88, hasPublishedA2A: true }
    ];
    viewModel.agentStatisticMetric = 'totalTokens';
    assert.strictEqual(viewModel.agentStatisticMetricTotal, 1700, 'The statistics view should aggregate Agent Token usage.');
    assert.strictEqual(viewModel.agentStatisticTiles[0].agent.id, 1, 'The largest metric should be displayed first.');
    assert.ok(viewModel.agentStatisticTiles[0].span > viewModel.agentStatisticTiles[1].span,
        'Treemap tile area should scale with the selected metric.');
    assert.deepStrictEqual(JSON.parse(JSON.stringify(viewModel.agentStatisticTileStyle(viewModel.agentStatisticTiles[0]))), {
        gridColumn: `span ${viewModel.agentStatisticTiles[0].span}`,
        gridRow: `span ${viewModel.agentStatisticTiles[0].span}`
    });

    viewModel.visible.drawerAgent = true;
    viewModel.agentForm = { id: 1, name: '高用量 Agent', enable: true };
    viewModel.functionCallTags = ['PluginA'];
    viewModel.captureEditorFormSnapshot('drawerAgent');
    viewModel.handleElVisibleClose('drawerAgent');
    assert.strictEqual(viewModel.confirmCount, 0, 'Cancelling an unchanged Agent editor should close without a confirmation.');
    assert.strictEqual(viewModel.visible.drawerAgent, false);

    viewModel.visible.drawerGroup = true;
    viewModel.groupForm = {
        id: 3,
        name: '测试组',
        members: [{ id: 2, name: 'B' }, { id: 1, name: 'A' }],
        remoteMembers: [{ id: 9, name: 'Remote' }]
    };
    viewModel.captureEditorFormSnapshot('drawerGroup');
    viewModel.groupForm.members.reverse();
    assert.strictEqual(viewModel.isEditorFormDirty('drawerGroup'), false,
        'Group member control ordering should not be treated as a user change.');
    viewModel.handleElVisibleClose('drawerGroup');
    assert.strictEqual(viewModel.confirmCount, 0, 'Cancelling an unchanged Group editor should close without a confirmation.');
    assert.strictEqual(viewModel.visible.drawerGroup, false);

    viewModel.visible.drawerGroupStart = true;
    viewModel.groupStartForm = { chatGroupId: 3, name: '任务', promptCommand: '原始任务' };
    viewModel.captureEditorFormSnapshot('drawerGroupStart');
    viewModel.handleElVisibleClose('drawerGroupStart');
    assert.strictEqual(viewModel.confirmCount, 0, 'Cancelling an unchanged task editor should close without a confirmation.');
    assert.strictEqual(viewModel.visible.drawerGroupStart, false);

    viewModel.visible.drawerGroupStart = true;
    viewModel.groupStartForm = { chatGroupId: 3, name: '任务', promptCommand: '原始任务' };
    viewModel.captureEditorFormSnapshot('drawerGroupStart');
    viewModel.groupStartForm.promptCommand = '已修改任务';
    assert.strictEqual(viewModel.isEditorFormDirty('drawerGroupStart'), true,
        'A modified task-start form should retain the discard confirmation.');

    assert.ok(page.includes('testAllRemoteAgents'), 'The remote A2A drawer should provide a batch test action.');
    assert.ok(page.includes('participant-quick-action'), 'Task details should use the shared participant quick-info action.');
    assert.ok(page.includes("getParticipantQuickInfo('task', mitem)"), 'Task details should provide current task usage to quick info.');
    assert.ok(page.includes('@@edit-agent="openParticipantAgentEditor(mitem)"'),
        'Task member quick info should expose the new-window Agent editor action.');
    assert.ok(page.includes("aitem.hasPublishedA2A ? '管理 A2A' : '发布 A2A'"),
        'Published local Agents should expose a management action.');
    assert.ok(page.includes('agentCard-a2aBadge'), 'Published local Agents should display a highlighted A2A badge.');
    assert.ok(page.includes('remoteParticipantAvailabilityText(participant)'),
        'Group-start participants should show A2A availability.');
    assert.ok(page.includes('<el-radio-button label="stats">统计图</el-radio-button>'));
    assert.ok(page.includes('agent-statistics-grid'));
    assert.ok(page.includes('handleAgentStatisticTileClick(tile.agent)'));
    assert.ok(script.includes("Vue.component('participant-quick-action'"));
    assert.ok(script.includes('Token 未由远端反馈'));
    assert.ok(script.includes('已输入 ${this.formatUsageCount(usage.messageCount)} 条文本'));
    assert.ok(script.includes('el-icon-top-right'));
    assert.ok(script.includes('buildParticipantEditorUrl'));
    assert.ok(script.includes('editorFormInitialSnapshots'));
    assert.ok(script.includes('当前修改尚未保存，确认关闭？'));
    assert.ok(styleSource.includes('.taskmain-member-action__name'));
    assert.ok(styleSource.includes('white-space: normal'));
    assert.ok(styleSource.includes('.agent-statistics-grid'));
    assert.ok(styleSource.includes('grid-auto-flow: dense'));

    assert.ok(remoteResponseSource.includes('RemoteAgent_TestConnectionsRequest'));
    assert.ok(remoteResponseSource.includes('RemoteAgent_TestConnectionsResponse'));
    assert.ok(remoteServiceSource.includes('TestConnections'));
    assert.ok(remoteServiceSource.includes('单项失败不会中止整批检测'));
    assert.ok(agentDtoSource.includes('HasPublishedA2A'));
    assert.ok(agentServiceSource.includes('PublishedA2AEnabled'));
}

run().then(() => {
    console.log('Agents A2A health tests passed.');
}).catch(error => {
    console.error(error);
    process.exitCode = 1;
});
