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
const responsePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Application/DTOs/ChatGroupResponse.cs');
const servicePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/ChatGroupAppService.cs');
const script = fs.readFileSync(scriptPath, 'utf8');
const page = fs.readFileSync(pagePath, 'utf8');
const responseSource = fs.readFileSync(responsePath, 'utf8');
const serviceSource = fs.readFileSync(servicePath, 'utf8');

let capturedOptions = null;
let capturedStartRequest = null;
let groupDetailResponse = null;

function Vue(options) {
    capturedOptions = options;
    return options;
}
Vue.component = function () { };
Vue.directive = function () { };

const serviceAM = {
    async post(url, body) {
        if (url.includes('GetChatGroupItem')) {
            return { data: { success: true, data: groupDetailResponse } };
        }
        capturedStartRequest = { url, body };
        return { data: { success: true } };
    }
};

const context = vm.createContext({
    Vue,
    serviceAM,
    console: { log() { }, warn() { }, error() { } },
    window: { location: { hash: '' } },
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
    Set
});

function createViewModel() {
    const viewModel = Object.assign({
        $refs: {},
        $set(target, key, value) { target[key] = value; },
        $delete(target, key) { delete target[key]; },
        $nextTick(callback) { callback(); },
        $message: { warning() { } }
    }, capturedOptions.data());
    Object.keys(capturedOptions.methods).forEach(name => {
        viewModel[name] = capturedOptions.methods[name].bind(viewModel);
    });
    return viewModel;
}

vm.runInContext(script, context, { filename: scriptPath });
assert.ok(capturedOptions, 'AgentsManager Vue options should be captured.');

const viewModel = createViewModel();
const participants = viewModel.buildGroupStartParticipants({
    chatGroupDto: {
        adminAgentTemplateId: 2,
        adminAgentTemplateName: '主持人',
        enterAgentTemplateId: 1,
        enterAgentTemplateName: '分析师'
    },
    agentTemplateDtoList: [{ id: 1, name: '分析师' }],
    remoteMemberDtoList: [{
        enable: true,
        remoteAgentDto: { id: 8, name: '外部研究员', enable: true }
    }],
    roleAgentTemplateDtoList: [
        { roleName: '群主', agentTemplateDto: { id: 2, name: '主持人' } },
        { roleName: '对接人', agentTemplateDto: { id: 1, name: '分析师' } }
    ]
});

assert.strictEqual(participants.length, 3, 'Local members, role-only members, and remote A2A members should all be mentionable.');
assert.deepStrictEqual(Array.from(participants.find(item => item.name === '主持人').roles), ['群主']);
assert.deepStrictEqual(Array.from(participants.find(item => item.name === '分析师').roles), ['对接人']);
assert.strictEqual(participants.find(item => item.name === '外部研究员').agentKind, 'RemoteA2A');

const textarea = {
    selectionStart: 3,
    selectionEnd: 3,
    focus() { this.focused = true; },
    setSelectionRange(start, end) {
        this.selectionStart = start;
        this.selectionEnd = end;
    }
};
viewModel.$refs.groupStartPromptCommand = { $refs: { textarea } };
viewModel.groupStartForm.promptCommand = '请先';
viewModel.insertGroupStartMention({ name: '主持人' });
assert.strictEqual(viewModel.groupStartForm.promptCommand, '请先 @主持人');
assert.strictEqual(textarea.selectionStart, '请先 @主持人'.length, 'The caret should remain after the inserted mention.');
assert.strictEqual(textarea.focused, true, 'The task description should regain focus after insertion.');

assert.ok(page.includes('groupStartParticipants'), 'The group-start drawer should render mentionable members.');
assert.ok(page.includes('ref="groupStartPromptCommand"'), 'The task description must expose its textarea for caret-aware insertion.');
assert.ok(responseSource.includes('RoleAgentTemplateDtoList'), 'The group detail contract should return role members separately.');
assert.ok(serviceSource.includes('RoleName = "群主"') && serviceSource.includes('RoleName = "对接人"'),
    'The group detail service should supply both group roles.');
assert.strictEqual(
    context.getInterfaceQueryStr({ requestId: 'approval-1', approved: false, reason: '用户拒绝' }),
    'requestId=approval-1&approved=false&reason=%E7%94%A8%E6%88%B7%E6%8B%92%E7%BB%9D',
    'Human approval query serialization must retain false boolean decisions.');
assert.ok(page.includes('groupStartForm.pluginToolPermission') &&
    page.includes('groupStartForm.mcpToolPermission') &&
    page.includes('groupStartForm.includeHumanParticipant'),
    'AgentsManager group start should expose plugin, MCP, and Human participant policies.');
assert.ok(page.includes('toolApprovalDialogVisible') &&
    page.includes('toolApprovalArgumentText') &&
    page.includes('resolveToolApproval(true)'),
    'Pending tool approvals should use a reusable task-page dialog instead of a transient browser confirmation.');

const largeChineseArguments =
    `{"text":"\\u9999\\u6e2f\\u7406\\u5de5\\u5927\\u5b66","content":${JSON.stringify('长参数'.repeat(2500))}}`;
viewModel.handleHumanApprovalRequest({
    requestId: 'dto-approval-1',
    requestType: 'toolApproval',
    toolName: 'Translate',
    toolArguments: largeChineseArguments,
    agentName: '翻译 Agent'
});
assert.strictEqual(viewModel.toolApprovalDialogVisible, true,
    'A pending request loaded from GetHumanRequests should recognize requestId and open the approval dialog.');
assert.strictEqual(viewModel.toolApprovalRequest.requestId, 'dto-approval-1');
assert.ok(viewModel.toolApprovalArgumentText.includes('香港理工大学'),
    'Escaped Unicode JSON arguments should be decoded for human review.');
assert.ok(viewModel.toolApprovalArgumentText.length > 5000,
    'Large approval arguments must remain complete instead of being truncated.');
viewModel.removeResolvedHumanRequest('dto-approval-1');
assert.strictEqual(viewModel.toolApprovalDialogVisible, false,
    'A resolution received from Workflow should close the matching AgentsManager approval dialog.');

viewModel.viewTaskDescription({
    promptCommand: '执行任务',
    executionPolicyCaptured: true,
    humanInTheLoopLevel: 2,
    pluginToolPermission: 2,
    mcpToolPermission: 3,
    includeHumanParticipant: false,
    chatMaxRound: 4,
    isPersonality: true,
    requireHumanApproval: false
});
const taskDescriptionText = viewModel.taskDescriptionCopyText();
assert.ok(taskDescriptionText.includes('HIL 等级：L2 工具审批'));
assert.ok(taskDescriptionText.includes('MCP 工具权限：禁止使用'));
assert.ok(taskDescriptionText.includes('最大对话轮数：4'));
assert.ok(page.includes('任务描述与执行策略') && page.includes('taskDescriptionDetails.executionPolicyCaptured'),
    'The task description dialog should display the persisted execution policy.');
assert.ok(page.includes('Human 参与者：{{ taskHumanParticipantStatusText(groupTaskDetails) }}'),
    'The Group task sidebar should visibly state the Human participation policy.');

viewModel.$refs.groupStartELForm = { resetFields() { } };
viewModel.tabsActiveName = 'third';
viewModel.gettaskListData = async () => { };
groupDetailResponse = {
    chatGroupDto: { id: 5010, name: '带 Human 的群组' },
    agentTemplateDtoList: [
        { id: 2, name: '翻译 Agent', isHuman: false },
        { id: 7, name: 'Human', isHuman: true }
    ],
    roleAgentTemplateDtoList: [],
    remoteMemberDtoList: []
};
viewModel.groupStartForm.chatGroupId = 5010;
viewModel.groupStartForm.includeHumanParticipant = false;
viewModel.groupStartHumanParticipantTouched = false;
const startRequest = {
    name: '从群组详情启动',
    chatGroupId: 5010,
    aiModelId: 1,
    promptCommand: '验证 Group ID 双通道提交'
};

viewModel.loadGroupStartParticipants(5010).then(() => {
    assert.strictEqual(viewModel.groupStartForm.includeHumanParticipant, true,
        'A Group-configured Human participant must be included after the async member list loads.');
    startRequest.includeHumanParticipant = viewModel.groupStartForm.includeHumanParticipant;
    viewModel.groupStartForm.includeHumanParticipant = false;
    viewModel.markGroupStartHumanParticipantTouched();
    return viewModel.loadGroupStartParticipants(5010);
}).then(() => {
    assert.strictEqual(viewModel.groupStartForm.includeHumanParticipant, false,
        'A user-selected Human opt-out must survive a later member-list refresh.');
    return viewModel.saveSubmitFormData('drawerGroupStart', startRequest);
}).then(() => {
    assert.strictEqual(
        capturedStartRequest.url,
        '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.RunGroup?chatGroupId=5010',
        'Group start must submit the selected ID as an explicit query parameter.');
    assert.strictEqual(capturedStartRequest.body.chatGroupId, 5010,
        'Group start must retain the selected ID in the request body for compatibility.');
    assert.strictEqual(capturedStartRequest.body.includeHumanParticipant, true,
        'The default Group Human participant must be sent in the task-start request.');
    assert.ok(serviceSource.includes('[FromBody] ChatGroup_RunGroupRequest request'),
        'RunGroup should explicitly bind its task payload from the request body.');
    assert.ok(serviceSource.includes('[FromQuery] int chatGroupId = 0'),
        'RunGroup should explicitly bind the fallback Group ID from the query string.');
    console.log('Agents group-start request binding tests passed.');
}).catch(error => {
    console.error(error);
    process.exitCode = 1;
});

console.log('Agents group-start mention tests passed.');
