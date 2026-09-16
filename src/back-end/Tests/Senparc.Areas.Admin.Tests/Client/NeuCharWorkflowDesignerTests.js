'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/js/NeuCharWorkflow/Workflow.js');
const commonScriptPath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/js/NeuCharWorkflow/common.js');
const pagePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/Areas/Admin/Pages/NeuCharWorkflow/Index.cshtml');
const stylePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/css/NeuCharWorkflow/Workflow.css');
const tasksScriptPath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/js/NeuCharWorkflow/Tasks.js');
const tasksPagePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/Areas/Admin/Pages/NeuCharWorkflow/Tasks.cshtml');
const tasksStylePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/css/NeuCharWorkflow/Tasks.css');
const analyticsScriptPath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/js/NeuCharWorkflow/Analytics.js');
const analyticsPagePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/Areas/Admin/Pages/NeuCharWorkflow/Analytics.cshtml');
const analyticsStylePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/css/NeuCharWorkflow/Analytics.css');
const replayScriptPath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/js/NeuCharWorkflow/Replay.js');
const replayPagePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/Areas/Admin/Pages/NeuCharWorkflow/Replay.cshtml');
const replayStylePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/css/NeuCharWorkflow/Replay.css');
const workflowScript = fs.readFileSync(scriptPath, 'utf8');
const workflowAppServicePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/Application/AppServices/NeuCharWorkflowAppService.cs');
const runCoordinatorPath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/Domain/Services/NeuCharWorkflowRunCoordinator.cs');
const workflowEnginePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/Domain/Services/NeuCharWorkflowEngine.cs');
const agentsWorkflowObjectProviderPath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/AgentsWorkflowObjectProvider.cs');
const workflowObjectContractsPath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow.Abstractions/Workflow/WorkflowObjectContracts.cs');
const adminAxiosPath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/axios.js');
const moduleFunctionPagePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/XncfModule/Start.cshtml');
const moduleFunctionScriptPath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/XncfModule/start.js');
const moduleFunctionStylePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/css/Admin/XncfModule/XncfModule.css');
const adminLayoutPath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/Shared/_Layout_Vue.cshtml');
const workflowPageMarkup = fs.readFileSync(pagePath, 'utf8');
const adminLayoutMarkup = fs.readFileSync(adminLayoutPath, 'utf8');
const nodePickerTemplateOffset = workflowPageMarkup.indexOf('id="workflow-node-picker-template"');
const workflowRootEndOffset = workflowPageMarkup.lastIndexOf('</div>\n\n@section scripts');

assert.ok(nodePickerTemplateOffset > workflowRootEndOffset,
    'The node-picker template must be emitted outside the shared #app root so its v-for aliases are not evaluated by the page Vue instance.');
assert.match(workflowPageMarkup, /class="workflow-validation-panel"/,
    'The Workflow page must render a persistent validation error panel.');
assert.match(workflowPageMarkup, /:data-node-id="node\.id"/,
    'Each Workflow node must expose its id so validation focus can target it.');
assert.ok(workflowPageMarkup.includes('工作流变量'),
    'Workflow settings must expose declared workflow variables.');
assert.ok(workflowPageMarkup.includes('调用工作流') || workflowScript.includes('调用工作流'),
    'The picker and node inspector must expose the sub-workflow node.');
assert.ok(workflowPageMarkup.includes('安全代码'),
    'The picker and node inspector must expose the constrained Safe Code node.');
assert.ok(workflowScript.includes("type: 'human-input'") && workflowPageMarkup.includes("selectedNode.type==='human-input'"),
    'The picker and node inspector must expose the native human-input system node.');
assert.ok(workflowPageMarkup.includes('X-NeuChar-Workflow-Resume-Key'),
    'The human-input inspector must document the key-protected external resume contract.');
assert.ok(workflowPageMarkup.includes('A2A'),
    'Remote A2A objects must be visually distinguishable in the picker.');
assert.ok(workflowPageMarkup.includes('node-input-continue') && workflowPageMarkup.includes('node-input-break'),
    'Loop-end nodes must expose separate continue and break input ports.');
assert.ok(workflowPageMarkup.includes('node-output-break') && workflowPageMarkup.includes('breakOn'),
    'Condition nodes must expose a break output and an explicit break condition.');

const axiosInterceptors = {};
const axiosSandbox = {
    axios: {
        create() {
            return {
                interceptors: {
                    request: {
                        use(success) {
                            axiosInterceptors.request = success;
                        }
                    },
                    response: {
                        use(success, failure) {
                            axiosInterceptors.failure = failure;
                        }
                    }
                }
            };
        }
    },
    window: { document: { getElementsByName() { return [{ value: 'token' }]; } } },
    app: {},
    ncfT(key) { return key; },
    Promise: {
        reject(value) { return { rejected: value }; },
        resolve(value) { return { resolved: value }; }
    },
    console: { log() {}, error() {} }
};
vm.createContext(axiosSandbox);
vm.runInContext(fs.readFileSync(adminAxiosPath, 'utf8'), axiosSandbox, { filename: adminAxiosPath });
const validationResponseError = {
    message: 'Request failed with status code 400',
    config: { customAlert: true },
    response: { status: 400, data: '聚合节点必须设置输出内容。' }
};
const interceptedValidationError = axiosInterceptors.failure(validationResponseError);
assert.strictEqual(interceptedValidationError.rejected, validationResponseError,
    'A customAlert validation request must bypass the legacy global app message handler and reach Workflow unchanged.');
const ordinaryResponseError = {
    message: 'Request failed with status code 500',
    config: {},
    response: { status: 500, data: 'AIModel is unavailable.' }
};
const interceptedOrdinaryError = axiosInterceptors.failure(ordinaryResponseError);
assert.strictEqual(interceptedOrdinaryError.rejected, ordinaryResponseError,
    'A shared Axios error must remain the original request error when no global app message API is available.');
const requestConfig = { method: 'post', headers: {} };
const requestWithToken = axiosInterceptors.request(requestConfig);
assert.strictEqual(requestWithToken.headers.RequestVerificationToken, 'token',
    'The shared Axios request interceptor must attach an available anti-forgery token.');
const missingTokenSandbox = {
    ...axiosSandbox,
    window: { document: { getElementsByName() { return []; } } }
};
const missingTokenInterceptors = {};
missingTokenSandbox.axios = {
    create() {
        return {
            interceptors: {
                request: {
                    use(success) {
                        missingTokenInterceptors.request = success;
                    }
                },
                response: { use() {} }
            }
        };
    }
};
vm.createContext(missingTokenSandbox);
vm.runInContext(fs.readFileSync(adminAxiosPath, 'utf8'), missingTokenSandbox, { filename: adminAxiosPath });
const requestWithoutToken = missingTokenInterceptors.request({ method: 'post', headers: {} });
assert.deepStrictEqual(requestWithoutToken.headers, { 'x-requested-with': 'XMLHttpRequest' },
    'The shared Axios request interceptor must not throw when the page token is not parsed yet.');
assert.match(adminLayoutMarkup, /src="~\/js\/Admin\/axios\.js"\s+asp-append-version="true"/,
    'The shared Axios script must use a versioned URL so browsers do not retain an old interceptor after deployment.');
const tokenOffset = adminLayoutMarkup.indexOf('@Html.AntiForgeryToken()');
const pageScriptsOffset = adminLayoutMarkup.indexOf('@RenderSection("scripts", false)');
assert.ok(tokenOffset >= 0 && pageScriptsOffset > tokenOffset,
    'The shared anti-forgery token must be emitted before page scripts can issue an initial POST.');
assert.ok(workflowScript.includes('AIModelAppService/Xncf.AIKernel_AIModelAppService.GetListAsync') &&
    workflowScript.includes('{ customAlert: true }') &&
    workflowScript.includes('模型列表暂不可用'),
    'Optional Workflow model loading must bypass the legacy global alert and leave the designer usable on failure.');

let vueOptions = null;
function Vue(options) { vueOptions = options; }
const nodePickerTemplateMarkup = '<div class="workflow-node-picker">picker template</div>';
const registeredVueComponents = {};
Vue.component = (name, options) => { registeredVueComponents[name] = options; };

const sandbox = {
    Vue,
    document: {
        getElementById(id) {
            return id === 'workflow-node-picker-template'
                ? { innerHTML: nodePickerTemplateMarkup }
                : null;
        }
    },
    window: { addEventListener() {}, removeEventListener() {}, setTimeout(callback) { callback(); }, clearTimeout() {} },
    localStorage: { getItem() { return null; }, setItem() {} },
    service: {},
    NeuCharWorkflowUi: {
        parseJson(value, fallback) {
            try { return value ? JSON.parse(value) : fallback; } catch { return fallback; }
        },
        normalizeParameterSchema(parameters) { return parameters; }
    },
    Set,
    Math,
    Number,
    Object,
    String,
    Promise,
    console
};

vm.createContext(sandbox);
vm.runInContext(fs.readFileSync(scriptPath, 'utf8'), sandbox, { filename: scriptPath });

assert.ok(vueOptions && vueOptions.methods, 'Workflow designer should register a Vue view model.');
assert.strictEqual(registeredVueComponents['workflow-node-picker'].template, nodePickerTemplateMarkup,
    'The node picker must cache its template before Vue renders the shared #app root and removes script template nodes.');
assert.ok(registeredVueComponents['workflow-rich-text-input'],
    'Workflow text fields should share a registered rich-text formula input component.');
assert.match(registeredVueComponents['workflow-rich-text-input'].template, /workflow-rich-text-badge/,
    'The shared text input should visibly identify formula support.');
assert.match(registeredVueComponents['workflow-rich-text-input'].template, /@mouseup\.stop/,
    'Formula editor controls must keep their pointer completion event away from the canvas handler.');
assert.match(workflowPageMarkup, /@@mouseup\.native\.stop/,
    'Pointer events inside the formula dialog must not reach the global canvas pointer handler.');
assert.ok(workflowScript.includes("name: 'toArray'") && workflowScript.includes("name: 'flatten'"),
    'The formula picker should expose the shared array helpers.');
assert.ok(workflowPageMarkup.includes('template-editor-formulas') &&
    workflowPageMarkup.includes('insertTemplateFormula(formula)') &&
    workflowPageMarkup.includes('openCodeAssignmentTemplateEditor'),
    'Safe Code assignments should reuse the common formula dialog and its quick-insert catalog.');
assert.ok(fs.readFileSync(scriptPath, 'utf8').includes('subWorkflowTargets()'),
    'The designer must provide valid target workflows to the sub-workflow selector.');
assert.ok(fs.readFileSync(scriptPath, 'utf8').includes('openSubWorkflow(workflowId)'),
    'The sub-workflow selector must support opening a target workflow in a separate tab.');
assert.ok(fs.readFileSync(stylePath, 'utf8').includes('node-a2a'),
    'The canvas must assign remote A2A nodes their own visual style.');
assert.ok(fs.readFileSync(stylePath, 'utf8').includes('node-human-input'),
    'The canvas must assign human-input nodes their own visual style.');

let previewEvent = null;
const nodePickerMethods = registeredVueComponents['workflow-node-picker'].methods;
const nodePicker = registeredVueComponents['workflow-node-picker'];
const nodePickerContext = Object.assign({
    functions: [
        { functionName: '摘要', moduleName: 'PromptRange', description: '生成摘要', functionKey: 'summary', moduleUid: 'prompt-range' }
    ],
    objects: [
        { providerId: 'agents-manager', objectId: 'agent:1', kind: 'agent', name: '客服 Agent', description: '处理客服问题', metadata: { type: '独立 Agent' } },
        { providerId: 'agents-manager', objectId: 'group:2', kind: 'agent-group', name: '销售 Group', description: '销售协作组', metadata: { type: 'Agent 组' } },
        { providerId: 'agents-manager', objectId: 'a2a:3', kind: 'a2a', name: '远程助手', description: 'Remote A2A endpoint', metadata: { type: '远程 A2A Agent' } }
    ],
    pinnedFunctionKeys: [],
    module: '',
    keyword: ''
}, nodePicker.data(), nodePickerMethods);
assert.strictEqual(nodePicker.computed.filteredObjects.call(Object.assign({}, nodePickerContext, { keyword: 'group' })).length, 1,
    'The shared node search should match Agent groups by their name and kind metadata.');
assert.strictEqual(nodePicker.computed.filteredObjects.call(Object.assign({}, nodePickerContext, { keyword: 'a2a' })).length, 1,
    'The shared node search should match remote A2A objects by protocol/type text.');
assert.strictEqual(nodePicker.computed.filteredSystemNodes.call(Object.assign({}, nodePickerContext, { keyword: '循环' })).length, 2,
    'The shared node search should include system nodes by their visible names.');
assert.strictEqual(nodePicker.computed.filteredSystemNodes.call(Object.assign({}, nodePickerContext, { keyword: '人工输入' })).length, 1,
    'The shared node search should surface the human-input system node.');
assert.strictEqual(nodePicker.computed.filteredFunctions.call(Object.assign({}, nodePickerContext, { keyword: '摘要' })).length, 1,
    'The shared node search should retain Function name matching.');
nodePickerMethods.previewNode.call({
    functionIdentity: nodePickerMethods.functionIdentity,
    nodePreviewKey: nodePickerMethods.nodePreviewKey,
    previewAnchor: nodePickerMethods.previewAnchor,
    $emit(...args) { previewEvent = args; }
}, 'function', { moduleUid: 'module-1', functionKey: 'sample' }, 'hover', {
    currentTarget: {
        getBoundingClientRect() {
            return { left: 120, top: 80, right: 260, bottom: 124, width: 140, height: 44 };
        }
    }
});
assert.strictEqual(JSON.stringify(previewEvent[1].anchor), JSON.stringify({
    left: 120, top: 80, right: 260, bottom: 124, width: 140, height: 44
}), 'Node previews should retain the hovered palette button bounds for collision-aware placement.');

const scheduledWorkflow = { triggerType: 'interval', enabled: true, nextRunAt: '2026-08-12T12:15:00Z' };
const workflowScheduleContext = {
    workflowClock: Date.parse('2026-08-12T12:00:00Z'),
    isIntervalWorkflow: vueOptions.methods.isIntervalWorkflow,
    workflowNextRunDate: vueOptions.methods.workflowNextRunDate
};
assert.strictEqual(vueOptions.methods.workflowTriggerLabel.call({}, scheduledWorkflow), '定时',
    'The workflow list should expose a distinct type label for interval workflows.');
assert.strictEqual(vueOptions.methods.workflowTriggerLabel.call({}, { triggerType: 'webhook' }), 'Webhook',
    'The workflow list should expose a distinct type label for Webhook workflows.');
assert.strictEqual(vueOptions.methods.workflowScheduleText.call(workflowScheduleContext, scheduledWorkflow), '15 分钟后运行',
    'An enabled interval workflow should show its next run as a relative time.');
assert.match(vueOptions.methods.workflowScheduleTitle.call(workflowScheduleContext, scheduledWorkflow), /^下次执行：2026-08-12 /,
    'The relative next-run label should retain the exact planned local time in its tooltip.');
assert.strictEqual(vueOptions.methods.workflowScheduleText.call(workflowScheduleContext, { triggerType: 'interval', enabled: false }), '定时已暂停',
    'Disabled interval workflows should not imply that a future run is scheduled.');

const commonSandbox = { window: {}, console };
vm.createContext(commonSandbox);
vm.runInContext(fs.readFileSync(commonScriptPath, 'utf8'), commonSandbox, { filename: commonScriptPath });
const legacyParameter = commonSandbox.window.NeuCharWorkflowUi.normalizeParameterSchema([{}])[0];
assert.strictEqual(legacyParameter.name, 'parameter_1',
    'Legacy Function metadata without a field name should receive a deterministic draft key.');
assert.strictEqual(legacyParameter.title, '参数 1',
    'Legacy Function metadata without a field name should receive a visible parameter label.');
const sandboxParameter = commonSandbox.window.NeuCharWorkflowUi.normalizeParameterSchema([{
    Name: 'TemplateKey',
    Title: '模板',
    Description: '选择沙箱模板',
    ParameterType: 1,
    Options: [{ Value: 'python', Text: 'Python Exec', DefaultSelected: true }]
}])[0];
assert.strictEqual(
    JSON.stringify({
        name: sandboxParameter.name,
        title: sandboxParameter.title,
        description: sandboxParameter.description,
        parameterType: sandboxParameter.parameterType,
        option: sandboxParameter.options[0]
    }),
    JSON.stringify({
        name: 'TemplateKey',
        title: '模板',
        description: '选择沙箱模板',
        parameterType: 1,
        option: { Value: 'python', Text: 'Python Exec', DefaultSelected: true, value: 'python', text: 'Python Exec', defaultSelected: true }
    }),
    'PascalCase schemas embedded by older Designer responses must retain real field names, descriptions and selection options.');
const sandboxDefaults = commonSandbox.window.NeuCharWorkflowUi.createParameterValues({
    parameterSchemaJson: JSON.stringify([{ Name: 'TemplateKey', ParameterType: 1, DefaultValue: 'python' }]),
    defaultParametersJson: JSON.stringify({ templateKey: 'csharp' })
});
assert.strictEqual(sandboxDefaults.TemplateKey, 'csharp',
    'Default values should still match a Function field name when an older response changed only the key casing.');

const cyclicContext = {
    form: {
        graph: {
            nodes: [
                { id: 'trigger', type: 'manual-trigger', x: 0, y: 0 },
                { id: 'a', type: 'delay', x: 0, y: 0 },
                { id: 'b', type: 'condition', x: 0, y: 0 }
            ],
            edges: [
                { source: 'trigger', target: 'a' },
                { source: 'a', target: 'b' },
                { source: 'b', target: 'a' }
            ]
        }
    },
    canvasSize: {},
    updateCanvasSize: vueOptions.methods.updateCanvasSize
};

vueOptions.methods.autoLayout.call(cyclicContext);
assert.ok(cyclicContext.form.graph.nodes.every(node => Number.isFinite(node.x) && Number.isFinite(node.y)),
    'Auto layout must terminate safely even for a legacy malformed cycle.');
assert.strictEqual(cyclicContext.form.graph.layout.direction, 'vertical',
    'Legacy workflows without layout metadata should continue to use the vertical default.');

const variableNormalizationContext = {};
const normalizedGraph = vueOptions.methods.ensureGraphLayout.call(variableNormalizationContext, {
    nodes: [],
    edges: [],
    variables: [{ name: 'customerName', value: 'Ada' }]
});
assert.deepStrictEqual(JSON.parse(JSON.stringify(normalizedGraph.variables)), [{ name: 'customerName', value: 'Ada' }],
    'Workflow graph normalization should preserve declared workflow variables.');

const horizontalLayoutContext = {
    editingLocked: false,
    form: {
        graph: {
            layout: { direction: 'vertical' },
            nodes: [
                { id: 'trigger', type: 'manual-trigger', x: 40, y: 40 },
                { id: 'delay', type: 'delay', x: 80, y: 80 },
                { id: 'end', type: 'end', x: 120, y: 120 }
            ],
            edges: [
                { source: 'trigger', target: 'delay' },
                { source: 'delay', target: 'end' }
            ]
        }
    },
    canvasSize: {},
    updateCanvasSize: vueOptions.methods.updateCanvasSize
};
vueOptions.methods.autoLayout.call(horizontalLayoutContext, 'horizontal');
assert.strictEqual(horizontalLayoutContext.form.graph.layout.direction, 'horizontal',
    'Selecting horizontal layout should persist the reading direction in the graph.');
assert.ok(horizontalLayoutContext.form.graph.nodes[2].x > horizontalLayoutContext.form.graph.nodes[1].x,
    'Horizontal auto layout should place later levels to the right of earlier levels.');

const crossingReductionContext = {
    editingLocked: false,
    form: {
        graph: {
            layout: { direction: 'vertical' },
            // The two final nodes are intentionally stored in reverse order. A plain BFS
            // layer dump would therefore cross the A/B branch edges.
            nodes: [
                { id: 'trigger', type: 'manual-trigger', x: 0, y: 0 },
                { id: 'parallel', type: 'parallel', x: 0, y: 0 },
                { id: 'branch-a', type: 'delay', x: 0, y: 0 },
                { id: 'branch-b', type: 'delay', x: 0, y: 0 },
                { id: 'branch-b-end', type: 'end', x: 0, y: 0 },
                { id: 'branch-a-end', type: 'end', x: 0, y: 0 }
            ],
            edges: [
                { source: 'trigger', target: 'parallel' },
                { source: 'parallel', target: 'branch-a' },
                { source: 'parallel', target: 'branch-b' },
                { source: 'branch-a', target: 'branch-a-end' },
                { source: 'branch-b', target: 'branch-b-end' }
            ]
        }
    },
    canvasSize: {},
    updateCanvasSize: vueOptions.methods.updateCanvasSize
};
vueOptions.methods.autoLayout.call(crossingReductionContext);
const crossingReductionNodes = crossingReductionContext.form.graph.nodes;
const crossingReductionNode = id => crossingReductionNodes.find(node => node.id === id);
assert.ok(crossingReductionNode('branch-a').x < crossingReductionNode('branch-b').x,
    'Layer ordering should keep parallel branches in their original reading order.');
assert.ok(crossingReductionNode('branch-a-end').x < crossingReductionNode('branch-b-end').x,
    'Barycenter ordering should prevent two parallel branch edges from crossing.');

const conditionOrderContext = {
    editingLocked: false,
    form: {
        graph: {
            layout: { direction: 'horizontal' },
            nodes: [
                { id: 'trigger', type: 'manual-trigger', x: 0, y: 0 },
                { id: 'condition', type: 'condition', x: 0, y: 0 },
                { id: 'false-node', type: 'delay', x: 0, y: 0 },
                { id: 'true-node', type: 'delay', x: 0, y: 0 }
            ],
            edges: [
                { source: 'trigger', target: 'condition' },
                { source: 'condition', target: 'false-node', sourceHandle: 'false' },
                { source: 'condition', target: 'true-node', sourceHandle: 'true' }
            ]
        }
    },
    canvasSize: {},
    updateCanvasSize: vueOptions.methods.updateCanvasSize
};
vueOptions.methods.autoLayout.call(conditionOrderContext);
const conditionOrderNodes = conditionOrderContext.form.graph.nodes;
const conditionOrderNode = id => conditionOrderNodes.find(node => node.id === id);
assert.ok(conditionOrderNode('true-node').y < conditionOrderNode('false-node').y,
    'Condition branches should remain ordered true above false in horizontal layouts.');

const gridAlignmentContext = {
    editingLocked: false,
    form: {
        graph: {
            layout: { direction: 'vertical' },
            nodes: [
                { id: 'trigger', type: 'manual-trigger', x: 83, y: 57 },
                { id: 'delay', type: 'delay', x: 93, y: 62 },
                { id: 'end', type: 'end', x: 378, y: 177 }
            ],
            edges: [
                { source: 'trigger', target: 'delay' },
                { source: 'delay', target: 'end' }
            ]
        }
    },
    canvasSize: {},
    isHorizontalLayout() { return vueOptions.methods.isHorizontalLayout.call(this); },
    updateCanvasSize: vueOptions.methods.updateCanvasSize
};
vueOptions.methods.alignToNearbyGrid.call(gridAlignmentContext);
assert.ok(gridAlignmentContext.form.graph.nodes.every(node => node.x % 40 === 0 && node.y % 40 === 0),
    'Nearby-grid alignment should snap every node to the visible 40px grid.');
const firstGridNode = gridAlignmentContext.form.graph.nodes[0];
const secondGridNode = gridAlignmentContext.form.graph.nodes[1];
assert.ok(Math.abs(firstGridNode.x - secondGridNode.x) >= 240 || Math.abs(firstGridNode.y - secondGridNode.y) >= 112,
    'Nearby-grid alignment should minimally separate nodes that would overlap after snapping.');

const cycleContext = {
    form: { graph: { edges: [{ source: 'b', target: 'a' }] } }
};
assert.strictEqual(vueOptions.methods.wouldCreateCycle.call(cycleContext, 'a', 'b'), true,
    'The designer must reject a connection that closes a cycle.');

const draftContext = {
    form: {
        name: '草稿工作流',
        triggerType: 'manual',
        webhookMethod: 'any',
        webhookParameters: [],
        graph: {
            nodes: [
                { id: 'trigger', type: 'manual-trigger', name: '手动触发' },
                { id: 'orphan', type: 'delay', name: '草稿等待' }
            ],
            edges: []
        }
    },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    getDisconnectedNodes: vueOptions.methods.getDisconnectedNodes,
    workflowObjects: []
};
const disconnectedDraftNodes = vueOptions.methods.getDisconnectedNodes.call(draftContext);
assert.strictEqual(disconnectedDraftNodes.length, 1,
    'The designer should identify nodes that are not reachable from the trigger.');
assert.strictEqual(disconnectedDraftNodes[0].id, 'orphan',
    'The disconnected draft node should remain identifiable to the editor.');
assert.strictEqual(vueOptions.methods.validate.call(draftContext, { requireRunnable: false }), '',
    'Draft saves should permit disconnected nodes.');
assert.match(vueOptions.methods.validate.call(draftContext, { requireRunnable: true }), /未连接到触发器/,
    'Testing a workflow should continue to reject disconnected draft nodes.');

const validationContext = {
    form: {
        graph: {
            nodes: [
                { id: 'trigger', type: 'manual-trigger', name: '手动触发', x: 40, y: 40 },
                { id: 'orphan', type: 'delay', name: '草稿等待', x: 760, y: 520 }
            ],
            edges: []
        }
    },
    extractValidationNodeIds: vueOptions.methods.extractValidationNodeIds,
    inferValidationNodeIds: vueOptions.methods.inferValidationNodeIds,
    getDisconnectedNodes: vueOptions.methods.getDisconnectedNodes
};
const parsedValidationIssue = vueOptions.methods.validationIssueFromError.call(
    validationContext,
    '节点“草稿等待”缺少必填参数“输入”。',
    '节点检查失败。');
assert.strictEqual(Array.from(parsedValidationIssue.nodeIds).join(','), 'orphan',
    'Validation messages containing a node name must resolve to the node id used by the canvas.');
validationContext.validation = parsedValidationIssue;
assert.strictEqual(vueOptions.methods.nodeValidationError.call(validationContext, validationContext.form.graph.nodes[1]),
    parsedValidationIssue.message,
    'The resolved validation issue must be available to the node renderer for its error highlight.');
const disconnectedIssue = vueOptions.methods.validationIssueFromError.call(
    validationContext,
    '画布中仍有未连接到触发器的节点。',
    '节点检查失败。');
assert.strictEqual(Array.from(disconnectedIssue.nodeIds).join(','), 'orphan',
    'A disconnected-node validation message must highlight every unreachable node even without a node name in the message.');

const focusValidationContext = {
    ...validationContext,
    selectedNodeId: '',
    selectedNodeIds: [],
    inspectorCollapsed: true,
    canvasZoom: 1,
    canvasSafeInsets: { left: 0, right: 0 },
    $refs: { canvas: { clientWidth: 500, clientHeight: 300, scrollLeft: 0, scrollTop: 0 } },
    stageContentTop() { return 0; },
    updateCanvasViewport() {},
    $nextTick(callback) { callback(); },
    setSelectedNodes: vueOptions.methods.setSelectedNodes
};
assert.strictEqual(vueOptions.methods.focusValidationIssue.call(focusValidationContext, { nodeId: 'orphan' }), true,
    'Clicking a validation item must focus the referenced node.');
assert.strictEqual(focusValidationContext.selectedNodeId, 'orphan',
    'Focusing a validation issue must select the invalid node in the inspector.');
assert.ok(focusValidationContext.$refs.canvas.scrollLeft > 0 && focusValidationContext.$refs.canvas.scrollTop > 0,
    'Focusing a validation issue must scroll the canvas toward the invalid node.');

const source = { id: 'source', type: 'function' };
const oldTarget = { id: 'old', type: 'delay' };
const newTarget = { id: 'new', type: 'console' };
const connectionContext = {
    editingLocked: false,
    form: { graph: { nodes: [source, oldTarget, newTarget], edges: [{ id: 'old-edge', source: 'source', target: 'old', sourceHandle: 'default' }] } },
    makeId() { return 'new-edge'; },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    targetHandleFor: vueOptions.methods.targetHandleFor,
    wouldCreateCycle: vueOptions.methods.wouldCreateCycle
};
connectionContext.canConnect = (...args) => vueOptions.methods.canConnect.call(connectionContext, ...args);
assert.strictEqual(vueOptions.methods.setTarget.call(connectionContext, source, 'default', 'new', true), true,
    'Dragging a normal output to a new target should replace its previous edge.');
assert.deepStrictEqual(connectionContext.form.graph.edges.map(edge => edge.target), ['new']);

const parallelSource = { id: 'parallel', type: 'parallel' };
const parallelTargetA = { id: 'parallel-a', type: 'end' };
const parallelTargetB = { id: 'parallel-b', type: 'end' };
const parallelContext = {
    editingLocked: false,
    form: { graph: { nodes: [parallelSource, parallelTargetA, parallelTargetB], edges: [] } },
    makeId(prefix) { return `${prefix}-${this.form.graph.edges.length + 1}`; },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    targetHandleFor: vueOptions.methods.targetHandleFor,
    wouldCreateCycle: vueOptions.methods.wouldCreateCycle
};
parallelContext.canConnect = (...args) => vueOptions.methods.canConnect.call(parallelContext, ...args);
assert.strictEqual(vueOptions.methods.setTarget.call(parallelContext, parallelSource, 'default', 'parallel-a', true), true,
    'A parallel node should accept its first downstream branch.');
assert.strictEqual(vueOptions.methods.setTarget.call(parallelContext, parallelSource, 'default', 'parallel-b', true), true,
    'A parallel node should retain existing branches when another downstream branch is connected.');
assert.strictEqual(parallelContext.form.graph.edges.length, 2,
    'A parallel node should preserve every independently connected downstream branch.');
assert.strictEqual(vueOptions.methods.supportsMultipleOutputs(parallelSource), true,
    'The designer should identify parallel nodes as multi-output nodes.');

const loopNode = vueOptions.methods.createSimpleNode.call({ makeId(type) { return `${type}-1`; } }, 'loop', '循环（For）');
assert.strictEqual(loopNode.config.count, 3,
    'A new loop node should start with a small, bounded For count.');
assert.match(vueOptions.methods.systemNodePreview.call({}, 'loop', '循环（For）').description, /循环结束/,
    'The loop preview should clearly explain how to mark the bounded loop body.');
const loopEndNode = vueOptions.methods.createSimpleNode.call({ makeId(type) { return `${type}-1`; } }, 'loop-end', '循环结束');
assert.strictEqual(loopEndNode.type, 'loop-end',
    'The designer should provide an explicit loop-end boundary node.');
assert.strictEqual(loopEndNode.config.loopId, '',
    'A new loop-end node should start without an owner so single-layer loops can be auto-detected.');
assert.match(vueOptions.methods.systemNodePreview.call({}, 'loop-end', '循环结束').description, /continue.*break/,
    'The loop-end preview should explain its continue and break inputs.');
const conditionNode = vueOptions.methods.createSimpleNode.call({ makeId(type) { return `${type}-1`; } }, 'condition', '条件判断');
assert.strictEqual(conditionNode.config.breakOn, '',
    'A new condition node should leave loop breaking disabled until configured.');
assert.match(vueOptions.methods.systemNodePreview.call({}, 'condition', '条件判断').description, /真.*假.*break/,
    'The condition preview should explain its true, false and break outputs.');
const loopValidationContext = {
    form: {
        name: '循环测试',
        triggerType: 'manual',
        graph: {
            nodes: [{ id: 'trigger', type: 'manual-trigger' }, { id: 'loop', type: 'loop', name: '循环', config: { count: 100001 } }],
            edges: [{ id: 'edge-1', source: 'trigger', target: 'loop' }]
        }
    },
    getDisconnectedNodes() { return []; },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    isBinding: vueOptions.methods.isBinding,
    isTemplateValue: vueOptions.methods.isTemplateValue
};
assert.match(vueOptions.methods.validate.call(loopValidationContext, { requireRunnable: true }), /1 到 100000/,
    'The designer should reject a static loop count outside the safe range before run.');
const formulaLoopValidationContext = {
    ...loopValidationContext,
    form: {
        ...loopValidationContext.form,
        graph: {
            ...loopValidationContext.form.graph,
            nodes: [
                { id: 'trigger', type: 'manual-trigger' },
                {
                    id: 'loop',
                    type: 'loop',
                    name: '循环',
                    config: {
                        count: {
                            $template: {
                                text: '{{= toInt( toNumber(vars.end) - toNumber(vars.number)) }}',
                                bindings: []
                            }
                        }
                    }
                }
            ]
        }
    },
    templateFor: vueOptions.methods.templateFor,
    loopCountFormulaValidationError: vueOptions.methods.loopCountFormulaValidationError,
    loopBoundaryValidationError() { return ''; }
};
assert.strictEqual(vueOptions.methods.validate.call(formulaLoopValidationContext, { requireRunnable: true }), '',
    'The designer should accept a complete workflow-variable formula as a dynamic loop count.');

const boundedLoopValidationContext = {
    form: {
        name: '有边界循环测试',
        triggerType: 'manual',
        graph: {
            nodes: [
                { id: 'trigger', type: 'manual-trigger' },
                { id: 'loop', type: 'loop', name: '循环', config: { count: 2 } },
                { id: 'body', type: 'delay', name: '循环体' },
                { id: 'loop-end', type: 'loop-end', name: '循环结束' },
                { id: 'after', type: 'console', name: '循环后' }
            ],
            edges: [
                { id: 'edge-1', source: 'trigger', target: 'loop' },
                { id: 'edge-2', source: 'loop', target: 'body' },
                { id: 'edge-3', source: 'body', target: 'loop-end' },
                { id: 'edge-4', source: 'loop-end', target: 'after' }
            ]
        }
    },
    getDisconnectedNodes() { return []; },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    isBinding: vueOptions.methods.isBinding,
    isTemplateValue: vueOptions.methods.isTemplateValue,
    loopBoundaryNodes: vueOptions.methods.loopBoundaryNodes,
    loopBoundaryValidationError: vueOptions.methods.loopBoundaryValidationError
};
assert.strictEqual(vueOptions.methods.validate.call(boundedLoopValidationContext, { requireRunnable: true }), '',
    'A loop with a linear body and explicit loop-end should pass designer validation.');

const breakLoopValidationContext = {
    form: {
        name: '条件跳出循环测试',
        triggerType: 'manual',
        graph: {
            nodes: [
                { id: 'trigger', type: 'manual-trigger' },
                { id: 'loop', type: 'loop', name: '循环', config: { count: 5 } },
                { id: 'condition', type: 'condition', name: '条件', config: { breakOn: 'true' } },
                { id: 'body', type: 'delay', name: '循环体' },
                { id: 'loop-end', type: 'loop-end', name: '循环结束', config: { loopId: 'loop' } },
                { id: 'after', type: 'console', name: '循环后' }
            ],
            edges: [
                { id: 'edge-1', source: 'trigger', target: 'loop' },
                { id: 'edge-2', source: 'loop', target: 'condition' },
                { id: 'edge-3', source: 'condition', target: 'body', sourceHandle: 'false' },
                { id: 'edge-4', source: 'condition', target: 'loop-end', sourceHandle: 'break', targetHandle: 'break' },
                { id: 'edge-5', source: 'body', target: 'loop-end', targetHandle: 'continue' },
                { id: 'edge-6', source: 'loop-end', target: 'after' }
            ]
        }
    },
    getDisconnectedNodes() { return []; },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    isBinding: vueOptions.methods.isBinding,
    isTemplateValue: vueOptions.methods.isTemplateValue,
    loopBoundaryNodes: vueOptions.methods.loopBoundaryNodes,
    loopBoundaryValidationError: vueOptions.methods.loopBoundaryValidationError
};
assert.strictEqual(vueOptions.methods.validate.call(breakLoopValidationContext, { requireRunnable: true }), '',
    'A condition break routed to the current loop-end break input should pass designer validation.');

const loopScopeGraph = {
    nodes: [
        { id: 'trigger', type: 'manual-trigger' },
        { id: 'outer-loop', type: 'loop', name: '外层循环' },
        { id: 'inner-loop', type: 'loop', name: '内层循环' },
        { id: 'inner-body', type: 'delay' },
        { id: 'inner-end', type: 'loop-end', config: { loopId: 'inner-loop' } },
        { id: 'outer-body', type: 'console' },
        { id: 'outer-end', type: 'loop-end', config: { loopId: 'outer-loop' } },
        { id: 'after', type: 'end' }
    ],
    edges: [
        { source: 'trigger', target: 'outer-loop' },
        { source: 'outer-loop', target: 'inner-loop' },
        { source: 'inner-loop', target: 'inner-body' },
        { source: 'inner-body', target: 'inner-end', targetHandle: 'continue' },
        { source: 'inner-end', target: 'outer-body' },
        { source: 'outer-body', target: 'outer-end', targetHandle: 'continue' },
        { source: 'outer-end', target: 'after' }
    ]
};
const loopScopeContext = {
    form: { graph: loopScopeGraph },
    loopBoundaryNodes: vueOptions.methods.loopBoundaryNodes,
    loopScopeNodeIds: vueOptions.methods.loopScopeNodeIds
};
const outerLoop = loopScopeGraph.nodes.find(node => node.id === 'outer-loop');
const innerLoop = loopScopeGraph.nodes.find(node => node.id === 'inner-loop');
const outerScopeIds = vueOptions.methods.loopScopeNodeIds.call(loopScopeContext, outerLoop);
const innerScopeIds = vueOptions.methods.loopScopeNodeIds.call(loopScopeContext, innerLoop);
assert.ok(outerScopeIds.includes('inner-body') && outerScopeIds.includes('outer-end') && !outerScopeIds.includes('after'),
    'An outer loop scope should include a nested loop body through the inner boundary but stop at its own boundary.');
assert.ok(innerScopeIds.includes('inner-body') && innerScopeIds.includes('inner-end') && !innerScopeIds.includes('outer-body'),
    'An inner loop scope should stop at its explicitly owned loop-end.');
const affectingContext = {
    ...loopScopeContext,
    loopsAffectingNode: vueOptions.methods.loopsAffectingNode
};
assert.deepStrictEqual(
    vueOptions.methods.loopsAffectingNode.call(affectingContext, loopScopeGraph.nodes.find(node => node.id === 'inner-body')).map(node => node.id),
    ['outer-loop', 'inner-loop'],
    'Selecting a node inside a nested loop should identify every enclosing loop label.');

let highlightTimer = null;
const previousHighlightTimeout = sandbox.window.setTimeout;
sandbox.window.setTimeout = (callback, delay) => {
    highlightTimer = { callback, delay };
    return highlightTimer;
};
const highlightContext = {
    ...affectingContext,
    loopHighlight: { nodeIds: [], labelNodeIds: [], edgeIds: [] },
    loopHighlightTimer: null,
    loopHighlightSequence: 0,
    clearLoopHighlight: vueOptions.methods.clearLoopHighlight,
    highlightLoopContext: vueOptions.methods.highlightLoopContext,
    loopScopeLabelNodeIds: vueOptions.methods.loopScopeLabelNodeIds,
    isLoopScopeHighlighted: vueOptions.methods.isLoopScopeHighlighted,
    isLoopLabelHighlighted: vueOptions.methods.isLoopLabelHighlighted
};
vueOptions.methods.highlightLoopContext.call(highlightContext, loopScopeGraph.nodes.find(node => node.id === 'inner-body'));
assert.ok(highlightContext.loopHighlight.labelNodeIds.includes('outer-loop') &&
    highlightContext.loopHighlight.labelNodeIds.includes('outer-end') &&
    highlightContext.loopHighlight.labelNodeIds.includes('inner-loop') &&
    highlightContext.loopHighlight.labelNodeIds.includes('inner-end'),
    'Selecting a nested loop node should temporarily highlight both its own and all enclosing loop labels.');
assert.strictEqual(highlightTimer.delay, 3500,
    'Loop scope highlighting should automatically disappear after a short hint interval.');
highlightTimer.callback();
assert.strictEqual(highlightContext.loopHighlight.nodeIds.length, 0,
    'The loop scope highlight should clear when its hint interval expires.');
sandbox.window.setTimeout = previousHighlightTimeout;

const branchSource = { id: 'condition', type: 'condition' };
const branchTarget = { id: 'target', type: 'delay' };
const branchContext = {
    form: { graph: { nodes: [branchSource, branchTarget], edges: [{ source: 'condition', target: 'target', sourceHandle: 'true' }] } },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    targetHandleFor: vueOptions.methods.targetHandleFor,
    wouldCreateCycle: vueOptions.methods.wouldCreateCycle
};
assert.strictEqual(vueOptions.methods.canConnect.call(branchContext, branchSource, branchTarget, 'false'), false,
    'A second condition branch must not create two inputs on an ordinary node.');

const loopEndTarget = { id: 'loop-end-target', type: 'loop-end' };
const breakConnectionContext = {
    form: { graph: { nodes: [branchSource, loopEndTarget], edges: [] } },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    targetHandleFor: vueOptions.methods.targetHandleFor,
    wouldCreateCycle: vueOptions.methods.wouldCreateCycle
};
assert.strictEqual(vueOptions.methods.canConnect.call(breakConnectionContext, branchSource, loopEndTarget, 'break'), true,
    'A condition break output should be able to target a loop-end break input.');
assert.strictEqual(vueOptions.methods.canConnect.call(breakConnectionContext, { id: 'body', type: 'delay' }, loopEndTarget, 'default', 'break'), false,
    'An ordinary loop-body node must not be able to target the loop-end break input.');

const functionTarget = { id: 'function-target', type: 'function' };
const functionBranchContext = {
    form: { graph: { nodes: [branchSource, functionTarget], edges: [{ source: 'condition', target: 'function-target', sourceHandle: 'true' }] } },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    targetHandleFor: vueOptions.methods.targetHandleFor,
    wouldCreateCycle: vueOptions.methods.wouldCreateCycle
};
assert.strictEqual(vueOptions.methods.canConnect.call(functionBranchContext, branchSource, functionTarget, 'false'), true,
    'A Function node should accept multiple upstream branches.');

const insertSource = { id: 'source', type: 'condition', x: 80, y: 60 };
const insertTarget = { id: 'target', type: 'delay', x: 80, y: 300 };
const originalInsertEdge = { id: 'edge-original', source: 'source', target: 'target', sourceHandle: 'false' };
let generatedEdgeNumber = 0;
const insertionContext = {
    editingLocked: false,
    form: { graph: { nodes: [insertSource, insertTarget], edges: [originalInsertEdge] } },
    edgeInsertMenu: { visible: true, edge: originalInsertEdge, x: 0, y: 0 },
    selectedNodeId: '',
    selectedNodeIds: [],
    inspectorCollapsed: false,
    makeId(prefix) { generatedEdgeNumber += 1; return `${prefix}-${generatedEdgeNumber}`; },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    targetHandleFor: vueOptions.methods.targetHandleFor,
    wouldCreateCycle: vueOptions.methods.wouldCreateCycle,
    isHorizontalLayout() { return false; },
    closeEdgeInsertMenu: vueOptions.methods.closeEdgeInsertMenu,
    setSelectedNodes: vueOptions.methods.setSelectedNodes,
    cancelConnection: vueOptions.methods.cancelConnection,
    updateCanvasSize() { this.canvasUpdated = true; },
    scheduleAutoSave() { this.autoSaveScheduled = true; },
    $notify() { throw new Error('The valid inline insertion should not show a warning.'); }
};
insertionContext.canConnect = (...args) => vueOptions.methods.canConnect.call(insertionContext, ...args);
insertionContext.findInsertedNodePosition = (...args) => vueOptions.methods.findInsertedNodePosition.call(insertionContext, ...args);
const insertedNode = { id: 'inserted', type: 'delay', name: '插入等待', x: 0, y: 0, config: { seconds: 1 } };
assert.strictEqual(vueOptions.methods.insertNodeIntoEdge.call(insertionContext, insertedNode, originalInsertEdge), insertedNode,
    'Inserting a valid node should return the inserted node.');
assert.strictEqual(insertionContext.form.graph.edges.length, 2,
    'Inline insertion should replace one existing edge with two edges.');
assert.ok(insertionContext.form.graph.edges.some(edge => edge.source === 'source' && edge.target === 'inserted' && edge.sourceHandle === 'false'),
    'Inline insertion should preserve a condition branch handle on the upstream half.');
assert.ok(insertionContext.form.graph.edges.some(edge => edge.source === 'inserted' && edge.target === 'target' && edge.sourceHandle === 'default'),
    'Inline insertion should connect the new node to the original downstream target.');
assert.strictEqual(insertionContext.selectedNodeId, 'inserted',
    'Inline insertion should select the newly inserted node for follow-up configuration.');
assert.strictEqual(insertionContext.edgeInsertMenu.visible, false,
    'Inline insertion should close the picker after replacing the edge.');
assert.ok(insertionContext.canvasUpdated && insertionContext.autoSaveScheduled,
    'Inline insertion should refresh the canvas and participate in automatic saving.');

const edgeHitSource = { id: 'hit-source', type: 'delay', x: 100, y: 40 };
const edgeHitTarget = { id: 'hit-target', type: 'delay', x: 100, y: 280 };
const edgeHit = { id: 'edge-hit', source: 'hit-source', target: 'hit-target', sourceHandle: 'default' };
const edgeHitContext = {
    form: { graph: { nodes: [edgeHitSource, edgeHitTarget], edges: [edgeHit], layout: { direction: 'vertical' } } },
    isHorizontalLayout: vueOptions.methods.isHorizontalLayout,
    edgeStart: vueOptions.methods.edgeStart,
    edgeEnd: vueOptions.methods.edgeEnd,
    cubicPoint: vueOptions.methods.cubicPoint,
    pointToSegmentDistance: vueOptions.methods.pointToSegmentDistance
};
assert.strictEqual(vueOptions.methods.findEdgeAtPoint.call(edgeHitContext, { x: 210, y: 196 }), edgeHit,
    'Dragging a palette block over a Bézier link should identify the nearby edge for highlighting.');
assert.strictEqual(vueOptions.methods.findEdgeAtPoint.call(edgeHitContext, { x: 450, y: 196 }), null,
    'Only a nearby link should become an insertion target while dragging.');

let paletteDropRequest = null;
const paletteDropContext = {
    editingLocked: false,
    paletteDrag: { active: true, kind: 'system', payload: { type: 'delay', name: '等待' }, hoverEdgeId: 'edge-hit' },
    canvasPoint() { return { x: 210, y: 196 }; },
    findEdgeAtPoint() { return edgeHit; },
    endPaletteNodeDrag() { this.paletteDrag = { active: false, kind: '', payload: null, hoverEdgeId: '' }; },
    addSimpleNode(type, name, edge) { paletteDropRequest = { type, name, edge }; },
    $notify() { throw new Error('Dropping over a highlighted edge should insert rather than warn.'); }
};
vueOptions.methods.onCanvasDrop.call(paletteDropContext, {});
assert.deepStrictEqual(paletteDropRequest, { type: 'delay', name: '等待', edge: edgeHit },
    'Dropping a palette node over a highlighted edge should route through the same inline insertion pipeline.');
assert.strictEqual(paletteDropContext.paletteDrag.active, false,
    'Completing a palette drop should clear the temporary edge highlight state.');

let blankCanvasDropRequest = null;
const blankCanvasDropContext = {
    editingLocked: false,
    paletteDrag: { active: true, kind: 'system', payload: { type: 'delay', name: '等待' }, hoverEdgeId: '' },
    canvasPoint() { return { x: 360, y: 240 }; },
    findEdgeAtPoint() { return null; },
    endPaletteNodeDrag() { this.paletteDrag = { active: false, kind: '', payload: null, hoverEdgeId: '' }; },
    placePaletteNodeAtCanvas(kind, payload, point) { blankCanvasDropRequest = { kind, payload, point }; }
};
vueOptions.methods.onCanvasDrop.call(blankCanvasDropContext, {});
assert.deepStrictEqual(blankCanvasDropRequest, {
    kind: 'system', payload: { type: 'delay', name: '等待' }, point: { x: 360, y: 240 }
}, 'Dropping a palette node onto empty canvas space should add it at that location instead of rejecting the drop.');

let nodePreviewTimer = null;
const originalSetTimeout = sandbox.window.setTimeout;
sandbox.window.setTimeout = (callback, delay) => {
    nodePreviewTimer = { callback, delay };
    return nodePreviewTimer;
};
const nodePreviewContext = {
    nodePreview: { visible: false, kind: '', payload: null, key: '', mode: 'hover' },
    nodePreviewTimer: null,
    clearNodePreviewTimer: vueOptions.methods.clearNodePreviewTimer,
    dismissNodePreview: vueOptions.methods.dismissNodePreview
};
vueOptions.methods.showNodePreview.call(nodePreviewContext, {
    kind: 'function', key: 'function:sample', payload: { functionName: '创建沙箱' }
}, 'click');
assert.strictEqual(nodePreviewContext.nodePreview.mode, 'click', 'A first click should lock the node detail preview.');
assert.strictEqual(nodePreviewTimer.delay, 15000, 'A click-locked node detail preview should auto-close after 15 seconds.');
vueOptions.methods.showNodePreview.call(nodePreviewContext, {
    kind: 'system', key: 'system:delay', payload: { type: 'delay' }
}, 'hover');
assert.strictEqual(nodePreviewContext.nodePreview.key, 'function:sample', 'Hovering another block must not replace a click-locked preview.');
vueOptions.methods.hideHoveredNodePreview.call(nodePreviewContext, 'function:sample');
assert.strictEqual(nodePreviewContext.nodePreview.visible, true, 'Leaving a block must not close a click-locked preview.');
nodePreviewTimer.callback();
assert.strictEqual(nodePreviewContext.nodePreview.visible, false, 'The 15-second detail preview callback should close the preview.');
vueOptions.methods.showNodePreview.call(nodePreviewContext, {
    kind: 'system', key: 'system:delay', payload: { type: 'delay' }
}, 'hover');
vueOptions.methods.hideHoveredNodePreview.call(nodePreviewContext, 'system:delay');
assert.strictEqual(nodePreviewContext.nodePreview.visible, false, 'A hover-only node detail preview should close when leaving that block.');
sandbox.window.setTimeout = originalSetTimeout;

const originalViewport = { width: sandbox.window.innerWidth, height: sandbox.window.innerHeight };
sandbox.window.innerWidth = 1000;
sandbox.window.innerHeight = 700;
const previewStyleFor = anchor => vueOptions.computed.nodePreviewStyle.call({ nodePreview: { anchor } });
const rightPreviewStyle = previewStyleFor({ left: 100, top: 100, right: 220, bottom: 140, width: 120, height: 40 });
assert.ok(Number.parseFloat(rightPreviewStyle.left) > 220 && rightPreviewStyle.transform === 'translate(0, 0)',
    'A node preview should prefer the available space to the right of its palette button.');
const leftPreviewStyle = previewStyleFor({ left: 760, top: 100, right: 900, bottom: 140, width: 140, height: 40 });
assert.ok(Number.parseFloat(leftPreviewStyle.left) < 760,
    'A node preview should move to the left when the right side cannot fit the popup.');
sandbox.window.innerWidth = 600;
const belowPreviewStyle = previewStyleFor({ left: 220, top: 100, right: 340, bottom: 140, width: 120, height: 40 });
assert.strictEqual(belowPreviewStyle.top, '152px',
    'When neither side has room, a node preview should use the available space below the button.');
const abovePreviewStyle = previewStyleFor({ left: 220, top: 500, right: 340, bottom: 540, width: 120, height: 40 });
assert.ok(Number.parseFloat(abovePreviewStyle.top) < 500,
    'When below the button cannot fit, a node preview should use the available space above it.');
if (originalViewport.width === undefined) delete sandbox.window.innerWidth;
else sandbox.window.innerWidth = originalViewport.width;
if (originalViewport.height === undefined) delete sandbox.window.innerHeight;
else sandbox.window.innerHeight = originalViewport.height;

const duplicateNode = { id: 'node-1', type: 'delay', name: '等待', x: 80, y: 120, config: { seconds: 3 } };
const duplicateContext = {
    editingLocked: false,
    form: { graph: { nodes: [duplicateNode], edges: [] } },
    selectedNodeId: '',
    selectedNodeIds: [],
    inspectorCollapsed: false,
    makeId() { return 'delay-copy'; },
    canDuplicateNode: vueOptions.methods.canDuplicateNode,
    duplicateNodes: vueOptions.methods.duplicateNodes,
    setSelectedNodes: vueOptions.methods.setSelectedNodes,
    cancelConnection: vueOptions.methods.cancelConnection,
    closeEdgeInsertMenu: vueOptions.methods.closeEdgeInsertMenu,
    updateCanvasSize() {},
    scheduleAutoSave() {}
};
const duplicate = vueOptions.methods.duplicateNode.call(duplicateContext, duplicateNode);
assert.strictEqual(duplicate.id, 'delay-copy', 'Copying a node should assign a new node id.');
assert.strictEqual(duplicate.name, '等待（副本）', 'Copying a node should make the duplicate recognizable.');
assert.deepStrictEqual([duplicate.x, duplicate.y], [120, 160], 'Copying a node should offset the duplicate on the canvas.');
assert.strictEqual(vueOptions.methods.canDuplicateNode.call({}, { type: 'manual-trigger' }), false,
    'The workflow trigger must not be duplicated into an invalid second trigger.');

const batchSource = { id: 'batch-source', type: 'delay', name: '等待', x: 80, y: 100, config: {} };
const batchTarget = { id: 'batch-target', type: 'console', name: 'Console', x: 360, y: 100, config: {} };
const batchEdge = { id: 'batch-edge', source: 'batch-source', target: 'batch-target', sourceHandle: 'default' };
let batchCopyId = 0;
const batchCopyContext = {
    editingLocked: false,
    form: { graph: { nodes: [batchSource, batchTarget], edges: [batchEdge] } },
    selectedNodeId: 'batch-target',
    selectedNodeIds: ['batch-source', 'batch-target'],
    inspectorCollapsed: false,
    makeId(prefix) { batchCopyId += 1; return `${prefix}-copy-${batchCopyId}`; },
    canDuplicateNode: vueOptions.methods.canDuplicateNode,
    setSelectedNodes: vueOptions.methods.setSelectedNodes,
    cancelConnection: vueOptions.methods.cancelConnection,
    closeEdgeInsertMenu: vueOptions.methods.closeEdgeInsertMenu,
    updateCanvasSize() {},
    scheduleAutoSave() {}
};
const batchCopies = vueOptions.methods.duplicateNodes.call(batchCopyContext, [batchSource, batchTarget]);
assert.strictEqual(batchCopies.length, 2, 'Copying a selection should create one copy per selected eligible node.');
assert.strictEqual(batchCopyContext.form.graph.edges.length, 2,
    'Copying a selection should retain connections whose source and target are both selected.');
assert.ok(batchCopyContext.form.graph.edges.some(edge => edge.source === batchCopies[0].id && edge.target === batchCopies[1].id),
    'A copied selection should reconnect the copied nodes instead of pointing back to the originals.');
assert.strictEqual(JSON.stringify(batchCopyContext.selectedNodeIds), JSON.stringify(batchCopies.map(node => node.id)),
    'The copied nodes should become the active selection for immediate collective movement.');

const selectionNodeA = { id: 'selection-a', type: 'delay', x: 80, y: 100 };
const selectionNodeB = { id: 'selection-b', type: 'console', x: 330, y: 100 };
const selectionNodeC = { id: 'selection-c', type: 'end', x: 700, y: 100 };
const selectionContext = {
    form: { graph: { nodes: [selectionNodeA, selectionNodeB, selectionNodeC] } },
    selectedNodeId: '',
    selectedNodeIds: [],
    inspectorCollapsed: true,
    selectionBox: { active: true, startX: 40, startY: 60, endX: 600, endY: 250, additive: false },
    emptySelectionBox: vueOptions.methods.emptySelectionBox,
    clearSelectionBox: vueOptions.methods.clearSelectionBox,
    nodeIntersectsSelection: vueOptions.methods.nodeIntersectsSelection,
    setSelectedNodes: vueOptions.methods.setSelectedNodes
};
vueOptions.methods.completeSelectionBox.call(selectionContext);
assert.strictEqual(JSON.stringify(selectionContext.selectedNodeIds), JSON.stringify(['selection-a', 'selection-b']),
    'Dragging a selection rectangle should select every intersecting node and exclude nodes outside the box.');
assert.strictEqual(selectionContext.selectedNodeId, 'selection-b',
    'The latest selected node should remain the single-node inspector target after marquee selection.');

const unchangedSelectionIds = ['selection-a'];
const unchangedSelectionContext = {
    selectedNodeId: 'selection-a',
    selectedNodeIds: unchangedSelectionIds,
    inspectorCollapsed: false
};
vueOptions.methods.setSelectedNodes.call(unchangedSelectionContext, [selectionNodeA]);
assert.strictEqual(unchangedSelectionContext.selectedNodeIds, unchangedSelectionIds,
    'Selecting the already-selected node must not replace the reactive selection array.');

const editingNode = { id: 'editing-node', type: 'delay' };
const editingNodeContext = {
    editing: true,
    editingLocked: false,
    inspectorCollapsed: false,
    selectedNodeId: 'editing-node'
};
assert.strictEqual(vueOptions.methods.isNodeBeingEdited.call(editingNodeContext, editingNode), true,
    'The current inspector target should expose the active editing state on the canvas node.');
assert.strictEqual(vueOptions.methods.isNodeBeingEdited.call({ ...editingNodeContext, inspectorCollapsed: true }, editingNode), false,
    'A selected node should not be marked as being edited while the inspector is collapsed.');
assert.strictEqual(vueOptions.methods.isNodeBeingEdited.call({ ...editingNodeContext, selectedNodeId: 'other-node' }, editingNode), false,
    'Only the node currently shown in the inspector should expose the active editing state.');

const formulaPointerContext = {
    canvasPan: { active: false, moved: false },
    selectionBox: { active: true, startX: 10, startY: 10, endX: 20, endY: 20, additive: false },
    templateEditor: { visible: false },
    dragState: null,
    connectionDraft: { sourceId: '' },
    suppressCanvasContextMenuUntil: 0,
    emptySelectionBox: vueOptions.methods.emptySelectionBox,
    clearSelectionBox: vueOptions.methods.clearSelectionBox,
    completeSelectionBox() { throw new Error('Formula controls must not complete an active canvas selection.'); }
};
vueOptions.methods.onPointerUp.call(formulaPointerContext, {
    target: { closest(selector) { return selector.includes('.parameter-template-actions') ? {} : null; } }
});
assert.strictEqual(formulaPointerContext.selectionBox.active, false,
    'Releasing a formula editor control must clear, rather than complete, a stale canvas selection.');

const formulaOpenContext = {
    editingLocked: false,
    selectionBox: { active: true, startX: 10, startY: 10, endX: 20, endY: 20, additive: false },
    canvasPan: { active: true, moved: true },
    dragState: { node: selectionNodeA },
    emptySelectionBox: vueOptions.methods.emptySelectionBox,
    clearSelectionBox: vueOptions.methods.clearSelectionBox,
    clearCanvasPointerInteraction: vueOptions.methods.clearCanvasPointerInteraction,
    isBinding() { return false; },
    templateFor() { return null; },
    openTemplateEditorForValue: vueOptions.methods.openTemplateEditorForValue
};
vueOptions.methods.openNodeTemplateEditor.call(formulaOpenContext, {
    id: 'selection-a',
    config: { title: 'test' }
}, 'title');
assert.strictEqual(formulaOpenContext.selectionBox.active, false,
    'Opening the formula editor must discard a stale marquee selection before it can reach the next pointer-up.');
assert.strictEqual(formulaOpenContext.canvasPan.active, false,
    'Opening the formula editor must end an in-flight canvas pan.');
assert.strictEqual(formulaOpenContext.dragState, null,
    'Opening the formula editor must end an in-flight node drag.');

const groupDragNodeA = { id: 'group-a', x: 100, y: 100 };
const groupDragNodeB = { id: 'group-b', x: 360, y: 140 };
const groupDragContext = {
    canvasPan: { active: false },
    selectionBox: { active: false },
    dragState: {
        node: groupDragNodeA,
        nodes: [groupDragNodeA, groupDragNodeB],
        positions: [{ id: 'group-a', x: 100, y: 100 }, { id: 'group-b', x: 360, y: 140 }],
        startX: 120,
        startY: 120,
        hoverEdgeId: ''
    },
    connectionDraft: { sourceId: '' },
    canvasPoint() { return { x: 180, y: 200 }; },
    updateCanvasSize() { this.canvasUpdated = true; }
};
vueOptions.methods.onPointerMove.call(groupDragContext, {});
assert.deepStrictEqual([groupDragNodeA.x, groupDragNodeA.y, groupDragNodeB.x, groupDragNodeB.y], [160, 180, 420, 220],
    'Dragging one member of a multi-selection should move all selected nodes by the same delta.');
assert.strictEqual(groupDragContext.dragState.hoverEdgeId, '',
    'A multi-node drag must not accidentally enter the single-node edge insertion flow.');

const batchRemoveContext = {
    editingLocked: false,
    form: { graph: { nodes: [batchSource, batchTarget], edges: [batchEdge] } },
    selectedNodeId: 'batch-target',
    selectedNodeIds: ['batch-source', 'batch-target'],
    inspectorCollapsed: false,
    canDeleteNode: vueOptions.methods.canDeleteNode,
    setSelectedNodes: vueOptions.methods.setSelectedNodes,
    cancelConnection: vueOptions.methods.cancelConnection,
    closeEdgeInsertMenu: vueOptions.methods.closeEdgeInsertMenu,
    updateCanvasSize() {},
    scheduleAutoSave() {}
};
const removedNodes = vueOptions.methods.removeNodes.call(batchRemoveContext, [batchSource, batchTarget]);
assert.strictEqual(removedNodes.length, 2, 'Deleting a selection should remove every selected deletable node.');
assert.strictEqual(batchRemoveContext.form.graph.nodes.length, 0, 'Deleting a selection should remove the selected nodes from the graph.');
assert.strictEqual(batchRemoveContext.form.graph.edges.length, 0, 'Deleting a selection should remove edges attached to any selected node.');

const panCanvas = { scrollLeft: 100, scrollTop: 80 };
let panPrevented = false;
const panContext = {
    $refs: { canvas: panCanvas },
    contextMenu: { visible: true, node: { id: 'node-1' } },
    canvasContextMenu: { visible: false, x: 0, y: 0, point: null },
    canvasNodeInsertMenu: { visible: false, x: 0, y: 0, point: null },
    dragState: { node: duplicateNode },
    connectionDraft: { sourceId: '', sourceHandle: '', x: 0, y: 0 },
    closeContextMenu: vueOptions.methods.closeContextMenu,
    closeCanvasContextMenu: vueOptions.methods.closeCanvasContextMenu,
    closeCanvasNodeInsertMenu: vueOptions.methods.closeCanvasNodeInsertMenu,
    closeEdgeInsertMenu: vueOptions.methods.closeEdgeInsertMenu,
    emptySelectionBox: vueOptions.methods.emptySelectionBox,
    cancelConnection: vueOptions.methods.cancelConnection,
    startCanvasPan: vueOptions.methods.startCanvasPan,
    onPointerMove: vueOptions.methods.onPointerMove,
    onPointerUp: vueOptions.methods.onPointerUp
};
vueOptions.methods.startCanvasPan.call(panContext, {
    button: 2,
    clientX: 240,
    clientY: 160,
    preventDefault() { panPrevented = true; }
});
vueOptions.methods.onPointerMove.call(panContext, { clientX: 210, clientY: 120 });
assert.deepStrictEqual([panCanvas.scrollLeft, panCanvas.scrollTop], [130, 120],
    'Right-button dragging should move the canvas scroll position.');
assert.strictEqual(panPrevented, true, 'Canvas panning should suppress the browser context menu gesture.');
vueOptions.methods.onPointerUp.call(panContext);
assert.strictEqual(panContext.canvasPan.active, false, 'Canvas panning should stop on mouse release.');

const canvasContextMenuContext = {
    editingLocked: false,
    canvasPan: { active: false, moved: false },
    suppressCanvasContextMenuUntil: 0,
    contextMenu: { visible: false, x: 0, y: 0, node: null },
    canvasContextMenu: { visible: false, x: 0, y: 0, point: null },
    canvasNodeInsertMenu: { visible: false, x: 0, y: 0, point: null },
    edgeInsertMenu: { visible: false, edge: null, x: 0, y: 0 },
    canvasSize: { width: 1200, height: 760 },
    canvasPoint() { return { x: 280, y: 180 }; },
    closeContextMenu: vueOptions.methods.closeContextMenu,
    closeCanvasContextMenu: vueOptions.methods.closeCanvasContextMenu,
    closeCanvasNodeInsertMenu: vueOptions.methods.closeCanvasNodeInsertMenu,
    closeEdgeInsertMenu: vueOptions.methods.closeEdgeInsertMenu,
    cancelConnection() {},
    openCanvasContextMenu: vueOptions.methods.openCanvasContextMenu
};
vueOptions.methods.onCanvasContextMenu.call(canvasContextMenuContext, {
    clientX: 420, clientY: 280, target: { tagName: 'DIV' }
});
assert.strictEqual(canvasContextMenuContext.canvasContextMenu.visible, true,
    'A right click on blank canvas space should open the canvas shortcut menu.');
assert.deepStrictEqual(canvasContextMenuContext.canvasContextMenu.point, { x: 280, y: 180 },
    'The canvas shortcut menu should retain the world position for subsequent node insertion.');
canvasContextMenuContext.openCanvasNodeInsertMenu = vueOptions.methods.openCanvasNodeInsertMenu;
vueOptions.methods.openCanvasNodeInsertMenu.call(canvasContextMenuContext);
assert.strictEqual(canvasContextMenuContext.canvasNodeInsertMenu.visible, true,
    'Choosing “新增节点” should open the reusable node picker at the clicked canvas position.');
assert.deepStrictEqual(canvasContextMenuContext.canvasNodeInsertMenu.point, { x: 280, y: 180 },
    'The node picker should retain the original context-click location rather than using its own popup coordinates.');

let contextNodeAddRequest = null;
const canvasNodeAddContext = {
    canvasNodeInsertMenu: { visible: true, x: 292, y: 192, point: { x: 280, y: 180 } },
    closeCanvasNodeInsertMenu: vueOptions.methods.closeCanvasNodeInsertMenu,
    dismissNodePreview() {},
    placePaletteNodeAtCanvas(kind, payload, point) { contextNodeAddRequest = { kind, payload, point }; },
    addCanvasContextNode: vueOptions.methods.addCanvasContextNode
};
vueOptions.methods.addCanvasContextSimpleNode.call(canvasNodeAddContext, 'delay', '等待');
assert.strictEqual(JSON.stringify(contextNodeAddRequest), JSON.stringify({
    kind: 'system', payload: { type: 'delay', name: '等待' }, point: { x: 280, y: 180 }
}), 'A node selected from the blank-canvas picker should be placed at the right-click position.');

const suppressedContextMenu = {
    canvasPan: { active: false, moved: false },
    suppressCanvasContextMenuUntil: Date.now() + 1000,
    openCanvasContextMenu() { throw new Error('A completed pan must not open a canvas context menu.'); }
};
vueOptions.methods.onCanvasContextMenu.call(suppressedContextMenu, { target: { tagName: 'DIV' } });
assert.strictEqual(suppressedContextMenu.suppressCanvasContextMenuUntil, 0,
    'The first context menu event after a right-button pan should be suppressed and then cleared.');

assert.strictEqual(vueOptions.methods.clampCanvasZoom.call({}, 3), 2,
    'Canvas zoom should not exceed the supported maximum.');
assert.strictEqual(vueOptions.methods.clampCanvasZoom.call({}, .001), .02,
    'Canvas zoom should allow a loaded large workflow to fit without dropping below the supported minimum.');

const pointCanvas = { getBoundingClientRect() { return { left: 10, top: 20 }; } };
const pointStage = { getBoundingClientRect() { return { left: -90, top: -180 }; } };
const pointContext = { $refs: { canvas: pointCanvas, stage: pointStage }, canvasZoom: 2 };
const scaledPoint = vueOptions.methods.canvasPoint.call(pointContext, { clientX: 110, clientY: 20 });
assert.strictEqual(scaledPoint.x, 100, 'Scaled pointer X should convert back to world coordinates.');
assert.strictEqual(scaledPoint.y, 100, 'Scaled pointer Y should convert back to world coordinates.');

const zoomCanvas = {
    clientWidth: 400,
    clientHeight: 260,
    scrollLeft: 100,
    scrollTop: 120,
    getBoundingClientRect() { return { left: 10, top: 20 }; }
};
const zoomContext = {
    canvasZoom: 1,
    $refs: { canvas: zoomCanvas, stage: { getBoundingClientRect() { return { left: 10, top: -64 }; } } },
    clampCanvasZoom: vueOptions.methods.clampCanvasZoom,
    stageContentTop: vueOptions.methods.stageContentTop,
    updateCanvasViewport() { this.viewportUpdated = true; },
    $nextTick(callback) { callback(); }
};
vueOptions.methods.setCanvasZoom.call(zoomContext, 1.5, 210, 180);
assert.strictEqual(zoomContext.canvasZoom, 1.5, 'Wheel and button zoom should update the scale.');
assert.strictEqual(zoomCanvas.scrollLeft, 250, 'Zooming should preserve the world point under the cursor horizontally.');
assert.strictEqual(zoomCanvas.scrollTop, 242, 'Zooming should preserve the world point under the cursor vertically.');
assert.strictEqual(zoomContext.viewportUpdated, true, 'Zooming should refresh minimap viewport data.');

const fitCanvas = {
    clientWidth: 1000,
    clientHeight: 700,
    scrollLeft: 0,
    scrollTop: 0
};
const fitContext = {
    form: {
        graph: {
            nodes: [
                { id: 'first', x: 100, y: 100 },
                { id: 'last', x: 1600, y: 700 }
            ]
        }
    },
    canvasZoom: 1,
    $refs: { canvas: fitCanvas },
    clampCanvasZoom: vueOptions.methods.clampCanvasZoom,
    stageContentTop() { return 40; },
    updateCanvasViewport() { this.viewportUpdated = true; },
    $nextTick(callback) { callback(); }
};
assert.strictEqual(vueOptions.methods.fitCanvasToNodes.call(fitContext), true,
    'Loading a workflow with nodes should calculate a fit-to-content viewport.');
assert.strictEqual(fitContext.canvasZoom, .54,
    'Fit-to-content should zoom out just enough to include the full node bounds and padding.');
assert.ok(fitCanvas.scrollLeft > 0 && fitContext.viewportUpdated,
    'Fit-to-content should centre the loaded graph and refresh the visible viewport.');

const overlayCanvas = {
    clientWidth: 1000,
    clientHeight: 640,
    scrollLeft: 0,
    scrollTop: 0,
    getBoundingClientRect() { return { left: 80, right: 1080, bottom: 700, width: 1000 }; }
};
const overlayViewportContext = {
    $refs: {
        canvas: overlayCanvas,
        palette: { getBoundingClientRect() { return { left: 80, right: 360 }; } },
        inspector: { getBoundingClientRect() { return { left: 750, right: 1080 }; } }
    },
    canvasSafeInsets: { left: 0, right: 0 },
    canvasViewport: {}
};
vueOptions.methods.updateCanvasViewport.call(overlayViewportContext);
assert.strictEqual(JSON.stringify(overlayViewportContext.canvasSafeInsets), JSON.stringify({ left: 298, right: 348 }),
    'The canvas should reserve the actual overlay widths plus a readable gap on both sides.');
const unchangedSafeInsets = overlayViewportContext.canvasSafeInsets;
const unchangedViewport = overlayViewportContext.canvasViewport;
vueOptions.methods.updateCanvasViewport.call(overlayViewportContext);
assert.strictEqual(overlayViewportContext.canvasSafeInsets, unchangedSafeInsets,
    'Repeated equal overlay measurements must not replace the reactive safe-inset object.');
assert.strictEqual(overlayViewportContext.canvasViewport, unchangedViewport,
    'Repeated equal viewport measurements must not replace the reactive viewport object.');
const canvasSurfaceStyle = vueOptions.computed.canvasSurfaceStyle.call({
    scaledCanvasSize: { width: 1200, height: 700 },
    canvasSafeInsets: overlayViewportContext.canvasSafeInsets
});
assert.strictEqual(canvasSurfaceStyle.width, '1846px',
    'The scroll surface should include both safety gutters in addition to the scaled world width.');
const canvasStageStyle = vueOptions.computed.canvasStageStyle.call({
    canvasSize: { width: 1200, height: 700 },
    canvasZoom: 1,
    canvasSafeInsets: overlayViewportContext.canvasSafeInsets
});
assert.strictEqual(canvasStageStyle.left, '298px',
    'The world origin should begin after the left overlay-safe gutter.');

const safeFitCanvas = {
    clientWidth: 1000,
    clientHeight: 700,
    scrollLeft: 0,
    scrollTop: 0
};
const safeFitContext = {
    form: { graph: { nodes: [{ id: 'first', x: 100, y: 100 }, { id: 'last', x: 1600, y: 700 }] } },
    canvasZoom: 1,
    canvasSafeInsets: { left: 298, right: 348 },
    $refs: { canvas: safeFitCanvas },
    clampCanvasZoom: vueOptions.methods.clampCanvasZoom,
    stageContentTop() { return 40; },
    updateCanvasViewport() { this.viewportUpdated = true; },
    $nextTick(callback) { callback(); }
};
vueOptions.methods.fitCanvasToNodes.call(safeFitContext);
assert.ok(safeFitContext.canvasZoom < fitContext.canvasZoom,
    'Fit-to-content should use only the visible centre area when side panes cover the canvas.');
assert.strictEqual(safeFitContext.viewportUpdated, true,
    'Fitting with side safe areas should still refresh the viewport and minimap state.');

let shortcutSource = null;
let shortcutPrevented = false;
vueOptions.methods.onSaveShortcut.call({
    editing: true,
    editingLocked: false,
    saveWorkflow(options) { shortcutSource = options.source; }
}, {
    metaKey: true,
    ctrlKey: false,
    key: 's',
    preventDefault() { shortcutPrevented = true; }
});
assert.strictEqual(shortcutSource, 'shortcut', 'Command+S should trigger a shortcut save.');
assert.strictEqual(shortcutPrevented, true, 'Command+S should prevent the browser save dialog.');

const page = fs.readFileSync(pagePath, 'utf8');
const styles = fs.readFileSync(stylePath, 'utf8');
assert.ok(page.includes("['workflow-stage', {'is-horizontal-layout':layoutDirection==='horizontal'}]"),
    'Workflow page should render the visual graph stage and expose the horizontal layout state.');
assert.ok(page.includes('ref="palette"') && page.includes('ref="inspector"') && page.includes(':style="canvasSurfaceStyle"'),
    'The designer should measure its two overlay panes and render a separate scroll surface for the protected canvas area.');
assert.ok(styles.includes('.workflow-stage {\n    position: absolute;') && styles.includes('.workflow-scale-surface'),
    'The graph stage should be positioned inside the scroll surface so left and right safe gutters remain unscaled screen space.');
assert.ok(page.includes('id="workflow-node-picker-template"') && page.includes('<workflow-node-picker'),
    'The sidebar and inline picker should share one node-picker template.');
assert.ok(page.includes('@@drag-node="beginPaletteNodeDrag"') && page.includes('@@drop.prevent="onCanvasDrop"'),
    'Node-picker blocks should be draggable onto the canvas and handled by one insertion drop path.');
assert.ok(page.includes('@@preview-node="showNodePreview"') && page.includes('@@dblclick="selectFunction(fn)"') && page.includes('nodePreviewDetails.actionText'),
    'Node picker blocks should preview on the first interaction and reserve double-click for adding a node.');
assert.ok(page.includes('filteredSystemNodes') && workflowScript.includes("type: 'condition', name: '条件判断'"), 'The shared picker should expose condition nodes.');
assert.ok(workflowScript.includes("type: 'loop', name: '循环（For）'") && page.includes('重复次数（For）') && page.includes('loopCountOutputOptions()') &&
    page.includes(':max="100000"') && workflowScript.includes('MaxWorkflowLoopIterations = 100000'),
    'The shared picker should expose a novice-friendly bounded For loop with a selectable upstream count.');
assert.ok(workflowScript.includes("type: 'aggregate', name: '聚合'"), 'The shared picker should expose multi-input aggregate nodes.');
assert.ok(workflowScript.includes("type: 'parallel', name: '并行'"), 'The shared picker should expose a parallel fan-out node.');
assert.ok(workflowScript.includes("type: 'console', name: 'Console 打印'"), 'The shared picker should expose console output nodes.');
assert.ok(workflowScript.includes("type: 'neubell', name: '发送纽铃'"), 'The shared picker should expose a NeuBell notification node.');
assert.ok(workflowScript.includes("type: 'end', name: '结束'"), 'The shared picker should expose end nodes.');
assert.ok(page.includes('class="edge-delete"'), 'Every edge should expose a midpoint delete control.');
assert.ok(page.includes('class="edge-insert"') && page.includes('workflow-edge-insert-menu'),
    'Every edge should expose a plus action that opens an inline insertion picker.');
assert.ok(styles.includes('.workflow-edge-insert-menu') && styles.includes('.edge-insert'),
    'The inline picker and plus action should receive dedicated canvas styles.');
assert.ok(page.includes(':style="nodePreviewStyle"') && fs.readFileSync(scriptPath, 'utf8').includes('getBoundingClientRect') &&
    styles.includes('.workflow-node-preview') && styles.includes('rgba(28, 42, 59, .80)'),
    'Node detail previews should anchor near the hovered button and use the translucent overlay treatment.');
assert.ok(page.includes("'is-hover-preview':nodePreview.mode==='hover'") &&
    /\.workflow-node-preview\.is-hover-preview\s*\{[^}]*pointer-events:\s*none;/s.test(styles),
    'Hover-only node previews must not intercept the pointer, so covered palette buttons remain clickable without flicker.');
assert.ok(styles.includes('.edge-insert-candidate path') && styles.includes('.node-picker-function') && styles.includes('.node-picker-agent'),
    'Dragging should visibly highlight an eligible edge and palette blocks should mirror node-type colors.');
assert.ok(styles.includes('.node-picker-loop') && styles.includes('.workflow-loop-count-binding'),
    'The loop palette entry and upstream-count binding state should have dedicated visual treatment.');
assert.ok(page.includes('@@mousedown="onCanvasMouseDown"') && fs.readFileSync(scriptPath, 'utf8').includes('startCanvasPan(event)'),
    'The canvas should preserve right-button panning alongside drag selection.');
assert.ok(page.includes('@@contextmenu.prevent="onCanvasContextMenu"') && page.includes('workflow-canvas-context-menu') && page.includes('canvasNodeInsertMenu.visible'),
    'Blank canvas right clicks should expose a shortcut menu and a position-aware node picker.');
assert.match(page, /<section v-if="canvasNodeInsertMenu\.visible"[\s\S]*?@@wheel\.stop>/,
    'The blank-canvas node picker must keep wheel events inside its fixed popup instead of zooming the canvas.');
assert.ok(page.includes('按设置自动排版</button>') && page.includes('就近网格对齐</button>') && page.includes('适应画布</button>'),
    'The canvas shortcut menu should surface the common overflow layout actions.');
assert.ok(page.includes('onCanvasMouseDown') && page.includes('class="workflow-selection-box"') && page.includes('selectedNodeIds'),
    'The canvas should expose a drag-selection rectangle and a multi-node selection state.');
assert.ok(page.includes("'is-editing':isNodeBeingEdited(node)") && workflowScript.includes('isNodeBeingEdited(node)'),
    'The canvas should distinguish the node currently being edited from the rest of the selection.');
assert.ok(page.includes('openNodeContextMenu'), 'Nodes should expose a context menu on right click.');
assert.ok(page.includes('class="workflow-context-menu"'), 'The node context menu should be rendered in the workflow page.');
assert.ok(page.includes('>复制</button>') && page.includes('>删除</button>'), 'The node context menu should expose copy and delete actions.');
assert.ok(styles.includes('.workflow-selection-box') && styles.includes('.workflow-node.is-multi-selected'),
    'Multi-selection should have a visible marquee and selected-node treatment.');
assert.ok(styles.includes('.workflow-node.is-editing') && styles.includes('outline-offset: 3px'),
    'The node currently being edited should have a distinct outer focus treatment.');
assert.ok(styles.includes('.workflow-canvas-context-menu') && styles.includes('.workflow-canvas-node-insert-menu'),
    'Canvas shortcut commands and the contextual node picker should share dedicated styles.');
assert.ok(page.includes('value="webhook"'), 'Workflow trigger settings should expose a Webhook mode.');
assert.ok(page.includes('webhookMethod'), 'Webhook settings should allow choosing the HTTP method.');
assert.ok(page.includes('addWebhookParameter'), 'Webhook settings should allow defining request parameters.');
assert.ok(page.includes('X-NeuChar-Webhook-Token'), 'Webhook settings should document the secure request header.');
assert.ok(page.includes('webhookHelpVisible=true') && page.includes('Webhook 使用说明'),
    'Webhook guidance should be available on demand in a help dialog instead of permanently consuming editor height.');
assert.ok(!page.includes('class="webhook-url-hint"'),
    'The lengthy inline Webhook URL guidance should be moved out of the always-visible configuration area.');
assert.ok(!page.includes('<header class="neuchar-page-header">'),
    'The redundant workflow page header should be removed to return vertical space to the canvas.');
assert.ok(page.includes('class="workflow-list-actions"') && page.includes('@@click="createWorkflow"'),
    'Workflow creation and refresh should move into the workflow list instead of using a page-wide header.');
assert.ok(page.includes('workflowTriggerLabel(item)') && page.includes('workflowScheduleText(item)') && page.includes('workflowScheduleTitle(item)'),
    'The workflow list should render a trigger type and an accessible next-run label for interval workflows.');
assert.ok(styles.includes('.workflow-list-badges') && styles.includes('.workflow-next-run'),
    'Workflow type badges and the next-run hint should have compact list-specific styling.');
assert.ok(page.includes('class="workflow-command-bar"') && page.includes('class="workflow-command-actions"'),
    'The editor should retain a compact one-row command bar for the current workflow.');
assert.ok(page.includes('>保存</el-button>') && page.includes('>运行</el-button>') && page.includes('>添加节点</el-button>'),
    'Primary save, run and node-creation actions should remain visible with textual labels.');
assert.ok(page.includes(':visible.sync="workflowSettingsVisible"') && page.includes('工作流设置'),
    'Low-frequency workflow settings should move into an on-demand dialog.');
assert.ok(page.includes('form.graph.layout.direction') && page.includes('按方向重新排列') && page.includes('就近网格对齐'),
    'Workflow settings should select a persisted layout direction and expose a non-destructive nearby-grid alignment action.');
assert.ok(styles.includes('.workflow-stage.is-horizontal-layout .node-input') && styles.includes('.workflow-layout-help'),
    'Horizontal layout should switch the ports to left-to-right while nearby-grid guidance remains visible in settings.');
assert.ok(page.includes('@@command="handleWorkflowAction"') && page.includes('删除工作流'),
    'Destructive workflow actions should live in a compact overflow menu.');
assert.ok(page.includes('selectedWorkflowObject'), 'Agent nodes should show the selected workflow object details.');
assert.ok(page.includes('openWorkflowObjectEditor'), 'Agent nodes should expose an edit action.');
assert.ok(page.includes('workflow-object-card'), 'Agent nodes should render a compact basic information card.');
assert.ok(page.includes('selectedNode.config.humanInTheLoopLevel') &&
    page.includes('selectedNode.config.pluginToolPermission') &&
    page.includes('selectedNode.config.mcpToolPermission') &&
    page.includes('selectedNode.config.includeHumanParticipant'),
    'Local Agent and Group nodes should expose the same HIL and tool permission controls as AgentsManager.');
const workflowAgentNode = vueOptions.methods.createObjectNode.call({
    makeId() { return 'agent-node'; }
}, {
    providerId: 'agents-manager',
    objectId: 'agent:42',
    kind: 'agent',
    name: '审批 Agent',
    metadata: { supportsHumanInTheLoop: 'true', supportsHumanParticipant: 'false' }
});
assert.deepStrictEqual(JSON.parse(JSON.stringify(workflowAgentNode.config)), {
    providerId: 'agents-manager',
    objectId: 'agent:42',
    prompt: '处理以下输入：{{input}}',
    aiModelId: null,
    personality: true,
    allowFunctionCalls: false,
    humanInTheLoopLevel: 0,
    pluginToolPermission: 0,
    mcpToolPermission: 0,
    includeHumanParticipant: false,
    chatMaxRound: 20
}, 'New independent Agent nodes should preserve the legacy no-tools default while retaining explicit HIL policy fields.');
const legacyGroupNode = { type: 'agent-group', config: {} };
vueOptions.methods.ensureWorkflowObjectPolicyConfig.call({
    $set(target, key, value) { target[key] = value; }
}, legacyGroupNode);
assert.strictEqual(legacyGroupNode.config.allowFunctionCalls, true);
assert.strictEqual(legacyGroupNode.config.aiModelId, 0);
assert.strictEqual(legacyGroupNode.config.personality, false,
    'Existing Group nodes should keep their original task-model execution behavior until explicitly changed.');
assert.strictEqual(legacyGroupNode.config.humanInTheLoopLevel, 0);
assert.strictEqual(legacyGroupNode.config.includeHumanParticipant, false,
    'Legacy Group nodes should receive stable automatic HIL defaults without enabling Human turns unexpectedly.');
assert.strictEqual(legacyGroupNode.config.chatMaxRound, 20,
    'Legacy Group nodes should retain the existing 20-round runtime default.');
assert.ok(page.includes('selectedNode.config.aiModelId') && page.includes('selectedNode.config.personality'),
    'Workflow Agent and Group nodes should expose a task model plus the per-Agent model binding switch.');
const agentManagerPage = fs.readFileSync(
    path.resolve(__dirname,
        '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Areas/Admin/Pages/AgentsManager/Index.cshtml'),
    'utf8');
const groupDrawerStart = agentManagerPage.indexOf('<el-drawer :visible.sync="visible.drawerGroup"');
const groupDrawerEnd = agentManagerPage.indexOf('</el-drawer>', groupDrawerStart);
assert.ok(groupDrawerStart >= 0 && groupDrawerEnd > groupDrawerStart,
    'AgentsManager Group editor should retain a distinct drawer boundary.');
assert.ok(!agentManagerPage.slice(groupDrawerStart, groupDrawerEnd).includes('agentForm.modelBinding'),
    'Agent model binding controls must not be rendered inside the Group editor.');
assert.ok(vueOptions.methods.workflowObjectEditUrl, 'Workflow objects should expose a safe editor URL resolver.');
assert.strictEqual(vueOptions.methods.workflowObjectEditUrl.call({}, { editUrl: 'https://example.invalid' }), '',
    'Workflow object edit links must not open arbitrary external URLs.');
assert.strictEqual(vueOptions.methods.workflowObjectEditUrl.call({}, { providerId: 'agents-manager', objectId: 'agent:42' }),
    '/Admin/AgentsManager/Index#tab=first&view=edit&agentId=42',
    'AgentsManager objects should resolve to the in-app agent editor anchor.');
assert.strictEqual(vueOptions.methods.workflowObjectEditUrl.call({}, { providerId: 'agents-manager', objectId: 'a2a:42' }),
    '/Admin/AgentsManager/Index#tab=remoteA2A&view=edit&remoteAgentId=42',
    'Remote A2A objects should resolve to their in-app editor anchor.');
assert.strictEqual(vueOptions.methods.functionPageUrl(
    { moduleUid: 'Senparc.Xncf.SenMapic', functionKey: 'Crawl Page' },
    'run'),
    '/Admin/XncfModule/Start/?uid=Senparc.Xncf.SenMapic&functionKey=Crawl%20Page&action=run#function-Crawl%20Page',
    'Function execution links should use a same-site module page, a targeted action and a stable anchor.');
assert.strictEqual(vueOptions.methods.parameterDisplayName({ title: '网址', name: 'Url' }), '网址',
    'A parameter title should be preferred when supplied by a module.');
assert.strictEqual(vueOptions.methods.parameterDisplayName({ title: '   ', name: 'Url' }), 'Url',
    'A blank parameter title must fall back to the field name.');
assert.strictEqual(vueOptions.methods.parameterDisplayName({}, 0), '参数 1',
    'Incomplete legacy metadata should still expose an actionable parameter label.');
assert.strictEqual(vueOptions.methods.hasParameterFieldName({ title: '网址', name: 'Url' }), true,
    'A localized parameter title should retain the underlying field name as a visual aid.');
assert.strictEqual(vueOptions.methods.parameterDescription({ description: '  请输入要爬取的网址  ' }), '请输入要爬取的网址',
    'Parameter descriptions should be trimmed before they are rendered in the tooltip.');
const selectionSourceContext = {
    functionParameters() {
        return [{
            name: 'crawlMode',
            title: '抓取模式',
            parameterType: 1,
            systemType: 'String',
            options: [{ value: 'fast', text: '快速' }, { value: 'full', text: '完整' }]
        }, {
            name: 'tags',
            title: '标签',
            parameterType: 2,
            systemType: 'String[]',
            options: [{ value: 'news', text: '新闻' }]
        }];
    },
    expectedShape: vueOptions.methods.expectedShape,
    parameterDisplayName: vueOptions.methods.parameterDisplayName
};
const selectionFields = vueOptions.methods.functionSelectionInputFields.call(selectionSourceContext, {});
assert.strictEqual(selectionFields.length, 2,
    'Function dropdown and multi-select inputs should be available as binding sources.');
assert.deepStrictEqual(
    { path: selectionFields[0].path, sourceKind: selectionFields[0].sourceKind, sourceParameterName: selectionFields[0].sourceParameterName },
    { path: '$.__functionInput.crawlMode', sourceKind: 'function-selection', sourceParameterName: 'crawlMode' },
    'Selection bindings should preserve the source parameter identity for runtime resolution.');
assert.strictEqual(selectionFields[1].isArray, true,
    'A multi-select Function input should remain an array when bound downstream.');
const templateNormalizationContext = {
    templatePlaceholder: vueOptions.methods.templatePlaceholder,
    templateUsesBindingToken: vueOptions.methods.templateUsesBindingToken
};
const normalizedTemplateBindings = vueOptions.methods.normalizeTemplateBindings.call(templateNormalizationContext,
    '保留 {{value_1}}，第二个变量已删除', [
        { token: 'value_1', source: { nodeId: 'source-a', path: '$' } },
        { token: 'value_2', source: { nodeId: 'source-b', path: '$' } }
    ]);
assert.strictEqual(JSON.stringify(normalizedTemplateBindings), JSON.stringify([
    { token: 'value_1', source: { nodeId: 'source-a', path: '$' } }
]), 'Applying the variable editor must discard bindings whose placeholders were manually removed from the text.');
const formulaTemplateBindings = vueOptions.methods.normalizeTemplateBindings.call(templateNormalizationContext,
    '{{= substring(value_1, 0, 10) }}', [
        { token: 'value_1', source: { nodeId: 'source-a', path: '$' } }
    ]);
assert.strictEqual(JSON.stringify(formulaTemplateBindings), JSON.stringify([
    { token: 'value_1', source: { nodeId: 'source-a', path: '$' } }
]), 'Applying a formula that references value_1 must retain its inserted upstream binding.');
const formulaCompatibilityContext = {
    expectedShape: vueOptions.methods.expectedShape,
    isTemplateValue: vueOptions.methods.isTemplateValue,
    templateFor: vueOptions.methods.templateFor,
    formulaValueText: vueOptions.methods.formulaValueText,
    isPureFormulaExpression: vueOptions.methods.isPureFormulaExpression,
    inferredFormulaType: vueOptions.methods.inferredFormulaType
};
const numericFormulaParameter = { parameterType: 0, systemType: 'System.Int32' };
const typedFormulaStatus = vueOptions.methods.formulaParameterCompatibility.call(formulaCompatibilityContext,
    numericFormulaParameter, { $template: { text: '{{= toInt(value_1) }}', bindings: [] } });
assert.strictEqual(typedFormulaStatus.level, 'success',
    'A complete toInt formula should be visibly accepted for an Int32 Function parameter.');
assert.match(typedFormulaStatus.text, /number/,
    'Formula compatibility should expose the inferred result type to the designer.');
const mixedFormulaStatus = vueOptions.methods.formulaParameterCompatibility.call(formulaCompatibilityContext,
    numericFormulaParameter, '{{= toInt(value_1) }} 个');
assert.strictEqual(mixedFormulaStatus.level, 'danger',
    'An Int32 parameter must reject formula text with surrounding characters because it becomes a string.');
const stringFormulaStatus = vueOptions.methods.formulaParameterCompatibility.call(formulaCompatibilityContext,
    numericFormulaParameter, '{{= toString(value_1) }}');
assert.strictEqual(stringFormulaStatus.level, 'warning',
    'An explicitly string-valued formula should be identified before it is used for an Int32 parameter.');
const templateSaveNode = { id: 'target', config: { parameters: {} } };
const templateSaveContext = {
    templateEditor: {
        visible: true,
        nodeId: 'target',
        parameterName: 'message',
        text: '固定文本',
        bindings: [{ token: 'value_1', source: { nodeId: 'source-a', path: '$' } }]
    },
    form: { graph: { nodes: [templateSaveNode] } },
    normalizeTemplateBindings: vueOptions.methods.normalizeTemplateBindings,
    templatePlaceholder: vueOptions.methods.templatePlaceholder,
    templateUsesBindingToken: vueOptions.methods.templateUsesBindingToken,
    $set(target, key, value) { target[key] = value; }
};
vueOptions.methods.saveParameterTemplate.call(templateSaveContext);
assert.strictEqual(templateSaveNode.config.parameters.message, '固定文本',
    'A removed placeholder must be saved as ordinary text rather than an invalid template that the server rejects.');
assert.strictEqual(templateSaveContext.templateEditor.visible, false,
    'Applying a normalized template should still close the explicit variable editor.');
const formulaInsertContext = {
    editingLocked: false,
    templateEditor: { text: 'hello world', selectionStart: 6, selectionEnd: 11 },
    templateEditorInputElement() { return null; },
    $nextTick() { }
};
vueOptions.methods.insertTemplateFormula.call(formulaInsertContext,
    { name: 'toArray', arity: 1 });
assert.strictEqual(formulaInsertContext.templateEditor.text, 'hello {{= toArray(world) }}',
    'Selecting text before clicking a formula should wrap the selection as the first argument.');
formulaInsertContext.templateEditor.text = "{{= upper(split(vars.msg, ',')) }}";
formulaInsertContext.templateEditor.selectionStart = 0;
formulaInsertContext.templateEditor.selectionEnd = formulaInsertContext.templateEditor.text.length;
vueOptions.methods.insertTemplateFormula.call(formulaInsertContext,
    { name: 'toArray', arity: 1 });
assert.strictEqual(formulaInsertContext.templateEditor.text,
    "{{= toArray(upper(split(vars.msg, ','))) }}",
    'Selecting an existing whole formula should remove its old template wrapper before applying the new formula.');
formulaInsertContext.templateEditor.text = "{{= upper(split(vars.msg, ',')) }}";
formulaInsertContext.templateEditor.selectionStart = 3;
formulaInsertContext.templateEditor.selectionEnd = formulaInsertContext.templateEditor.text.length - 3;
vueOptions.methods.insertTemplateFormula.call(formulaInsertContext,
    { name: 'toArray', arity: 1 });
assert.strictEqual(formulaInsertContext.templateEditor.text,
    "{{= toArray(upper(split(vars.msg, ','))) }}",
    'Selecting only the expression inside an external formula wrapper should replace the whole wrapper instead of nesting it.');
formulaInsertContext.templateEditor.text = '';
formulaInsertContext.templateEditor.selectionStart = 0;
formulaInsertContext.templateEditor.selectionEnd = 0;
vueOptions.methods.insertTemplateFormula.call(formulaInsertContext,
    { name: 'substring', arity: 3 });
assert.strictEqual(formulaInsertContext.templateEditor.text, '{{= substring(, , ) }}',
    'Multi-argument formulas should leave editable empty argument positions when no text is selected.');
const assignmentSaveNode = { id: 'code-node', config: { assignments: [{ name: 'result', value: '' }] } };
const assignmentSaveContext = {
    templateEditor: {
        visible: true,
        nodeId: 'code-node',
        configKey: 'assignments',
        targetType: 'code-assignment',
        assignmentIndex: 0,
        text: '{{= toArray(input) }}',
        bindings: []
    },
    form: { graph: { nodes: [assignmentSaveNode] } },
    normalizeTemplateBindings: vueOptions.methods.normalizeTemplateBindings,
    templateUsesBindingToken: vueOptions.methods.templateUsesBindingToken,
    $set(target, key, value) { target[key] = value; }
};
vueOptions.methods.saveParameterTemplate.call(assignmentSaveContext);
assert.strictEqual(assignmentSaveNode.config.assignments[0].value.$template.text,
    '{{= toArray(input) }}',
    'Safe Code formula edits should persist through the shared template contract.');
assert.doesNotThrow(() => vueOptions.methods.setConfigBinding.call({
    form: { graph: { nodes: [] } },
    nodeOutputFields() { return []; },
    $set() { throw new Error('A stale cascader value must not be persisted.'); }
}, { config: {} }, 'left', ['removed-node', '$']),
    'A stale upstream-node cascader value must be ignored instead of causing a client error.');
const originalAutoSaveSetTimeout = sandbox.window.setTimeout;
let autoSaveDelay = null;
sandbox.window.setTimeout = (callback, delay) => { autoSaveDelay = delay; return 7; };
const autoSaveContext = {
    editing: true,
    form: { id: 42, autoSaveMinutes: 1 },
    saveState: { timer: null, autoSaveBlockedSignature: 'invalid-save' },
    currentSaveSignature: 'invalid-save',
    clearAutoSaveTimer: vueOptions.methods.clearAutoSaveTimer,
    normalizedAutoSaveMinutes: vueOptions.methods.normalizedAutoSaveMinutes,
    runAutoSave() {}
};
vueOptions.methods.scheduleAutoSave.call(autoSaveContext);
assert.strictEqual(autoSaveDelay, null,
    'A failed automatic save must stop retries while the invalid workflow content remains unchanged.');
autoSaveContext.currentSaveSignature = 'changed-content';
vueOptions.methods.scheduleAutoSave.call(autoSaveContext);
assert.strictEqual(autoSaveDelay, 60000,
    'Editing after an automatic-save failure should allow one fresh autosave attempt using the minimum whole-minute interval.');
sandbox.window.setTimeout = originalAutoSaveSetTimeout;
assert.ok(page.includes('workflow-run-dock'), 'Workflow execution should use a persistent status dock instead of a modal.');
assert.ok(page.includes('关联上游 Output'), 'Node parameters should expose upstream output binding controls.');
assert.ok(page.includes('Function 预载输入选择'), 'Function parameters should explain that upstream Selection values can be bound.');
assert.ok(page.includes('预载输入选择'),
    'Function Selection values loaded with the Function should be specially identified as preloaded binding sources.');
assert.ok(page.includes(':title="templateEditorTitle"') && page.includes('saveParameterTemplate') && page.includes('appendTemplateBinding'),
    'Text parameters should offer a beginner-friendly dialog for inserting multiple upstream bindings into manual text.');
assert.ok(page.includes(':close-on-click-modal="false"') && page.includes(':close-on-press-escape="false"'),
    'The variable editor must only close through an explicit close, cancel, or apply action so edits are not lost accidentally.');
assert.ok(page.includes('parameter-template-card') && page.includes('templateEditor.bindings'),
    'Mixed text bindings should remain visible and editable after the dialog is closed.');
assert.ok(page.includes("selectedNode.config.title") && page.includes("selectedNode.config.summary") &&
    page.includes("selectedNode.config.prompt") && page.includes('workflow-rich-text-input'),
    'NeuBell title/content and Agent Prompt should use the shared formula text input.');
assert.ok(styles.includes('.workflow-rich-text-input') && styles.includes('.workflow-rich-text-info'),
    'Formula text inputs should have a distinct visual treatment and an info icon.');
assert.ok(styles.includes('container-type: inline-size') &&
    styles.includes('@container (max-width: 360px)') &&
    styles.includes('.workflow-rich-text-input-control { flex-direction: column; }'),
    'Shared formula inputs should stack their textarea and editor action when the Inspector makes the component narrow.');
assert.ok(styles.includes('@container (max-width: 560px)') &&
    styles.includes('.workflow-code-assignment-row { grid-template-columns: minmax(0, 1fr); }'),
    'Safe Code assignment rows should stack variable, formula input, and delete action in narrow containers.');
assert.ok(styles.includes('@container (max-width: 440px)') &&
    styles.includes('.workflow-function-actions { width: 100%; flex-wrap: wrap; }'),
    'Function and Agent object cards should let action buttons wrap instead of squeezing their titles.');
assert.ok(fs.readFileSync(scriptPath, 'utf8').includes('$template'),
    'The designer should persist mixed text binding values with an explicit template contract.');
assert.ok(!page.includes("{{'{{'+item.token+'}}'}}"),
    'Vue templates must not embed a literal closing interpolation delimiter inside another interpolation.');
assert.ok(fs.readFileSync(scriptPath, 'utf8').includes('templatePlaceholder(token)'),
    'Mixed-text variable tags should render their placeholder through a method, avoiding Vue 2 interpolation parsing errors.');
assert.ok(page.includes(':placeholder="templateEditorPlaceholder"') && page.includes(':title="templateEditorBindingHelpText"'),
    'Literal variable examples must come from view-model strings rather than nested Vue interpolations in Razor markup.');
assert.ok(page.includes("openFunctionPage(selectedFunction,'settings')") && page.includes("openFunctionPage(selectedFunction,'run')"),
    'Function nodes should offer separate settings and execution links.');
assert.ok(page.includes('@@wheel.prevent="zoomCanvas"'), 'The workflow canvas should use mouse-wheel zooming.');
assert.ok(page.includes('class="canvas-zoom-controls"'), 'The workflow canvas should render zoom controls.');
assert.ok(page.includes('type="range"'), 'The zoom controls should include a slider.');
assert.ok(page.includes('min="0.02"'), 'The zoom slider should expose the low zoom range used to fit large loaded workflows.');
assert.ok(page.includes('class="canvas-minimap"'), 'Zoomed canvases should render a minimap.');
assert.ok(page.includes('disconnectedNodes.length>0'), 'Disconnected draft nodes should disable test execution in the page.');
assert.ok(page.includes('class="parameter-field-name"'), 'Function parameter field names should be visible in the node settings.');
assert.ok(page.includes('parameter-description-icon'), 'Function parameter descriptions should have an info icon.');
assert.ok(page.includes('parameterDisplayName(parameter)'), 'Function parameters should always resolve to a visible name.');
assert.ok(page.includes('parameterDescription(parameter)'), 'Function parameter descriptions should be shown through a tooltip.');
assert.ok(page.includes('consumeMode') && page.includes('消费当前这一条提醒') && page.includes('消费这个订阅下全部提醒'),
    'NeuBell nodes should allow choosing no consumption, item consumption, or provider-wide consumption after task navigation.');
assert.ok(fs.readFileSync(scriptPath, 'utf8').includes("consumeMode: 'item'"),
    'New NeuBell nodes should default to consuming only their own reminder after the task is opened.');
const tasksScript = fs.readFileSync(tasksScriptPath, 'utf8');
assert.ok(tasksScript.includes("neuBellProvider") && tasksScript.includes("service.post('/api/Senparc.Areas.Admin/neubell/consume'"),
    'Workflow task links should consume their matching NeuBell notification through the protected API.');
assert.ok(sourceIncludesFitOnLoad(), 'Editing an existing workflow should fit all nodes after its canvas has rendered.');
assert.ok(page.includes('自动保存'), 'Workflow settings should expose the auto-save interval.');
assert.ok(page.includes(':precision="0"'), 'The auto-save editor must only accept whole minutes so a fractional interval cannot create a rapid save loop.');
assert.ok(workflowScript.includes("type: 'loop-end', name: '循环结束'") && page.includes('循环体边界'),
    'The designer should expose an explicit loop-end node and explain its continuation semantics.');
assert.ok(page.includes('Command/Ctrl + S'), 'Workflow save should advertise the system save shortcut.');
assert.ok(!page.includes(':visible.sync="runDialogVisible"'),
    'Workflow execution should remain in the persistent dock instead of using a modal dialog.');
assert.match(styles, /\.workflow-list\s*\{[^}]*overflow-y:\s*auto;[^}]*overscroll-behavior:\s*contain;/s,
    'The workflow name list should scroll independently without chaining to the editor.');
assert.match(styles, /\.workflow-list\s*\{[^}]*padding-top:\s*1px;[^}]*overflow-y:\s*auto;/s,
    'The workflow list should leave room for the active first item border above its translated position.');
assert.match(styles, /\.palette-content,\s*\.inspector-content\s*\{[^}]*overflow-y:\s*auto;[^}]*overscroll-behavior:\s*contain;/s,
    'The node palette and inspector should each own their vertical scroll area.');
assert.match(styles, /\.workflow-canvas\s*\{[^}]*height:\s*100%;[^}]*overflow:\s*auto;[^}]*overscroll-behavior:\s*contain;/s,
    'The canvas should stay inside its own scroll container.');
assert.match(styles, /\.canvas-zoom-controls,[\s\S]*?\.canvas-minimap\s*\{[^}]*position:\s*fixed;[^}]*opacity:\s*\.48;/s,
    'Canvas navigation controls should stay in the viewport and be translucent by default.');
assert.match(styles, /\.canvas-zoom-controls:hover,[\s\S]*?\.canvas-minimap:focus-within\s*\{[^}]*opacity:\s*1;/s,
    'Canvas navigation controls should become opaque on hover or focus.');
assert.match(styles, /\.workflow-context-menu\s*\{[^}]*position:\s*fixed;/s,
    'The node context menu should stay anchored to the viewport instead of scrolling with the canvas.');
assert.match(styles, /\.workflow-command-bar\s*\{[^}]*min-height:\s*42px;[^}]*flex:\s*0 0 auto;/s,
    'The command bar should remain a compact, non-scrolling top-level surface.');
assert.match(styles, /\.workflow-palette, \.workflow-inspector\s*\{[^}]*position:\s*absolute;[^}]*z-index:\s*60;/s,
    'The node palette and inspector should overlay the canvas instead of permanently reserving canvas width.');
assert.match(styles, /\.workflow-designer\s*\{[^}]*position:\s*relative;[^}]*display:\s*block;/s,
    'The canvas container should no longer use a three-column grid that constrains its width.');
assert.match(styles, /\.workflow-page\s*\{[^}]*height:\s*100%;[^}]*overflow:\s*hidden;/s,
    'The Workflow page should fit the available Admin content area without outer scrolling.');
assert.match(styles, /\.admin-content:has\(\.workflow-page\)\s*\{[^}]*overflow:\s*hidden;/s,
    'The Admin content scroller should be disabled for the fixed Workflow editor.');
assert.match(styles, /\.admin-content:has\(\.workflow-page\) \.ifram-wrapper\s*\{[^}]*height:\s*100%;/s,
    'The Workflow host should provide a definite height to the fixed editor.');

const moduleFunctionPage = fs.readFileSync(moduleFunctionPagePath, 'utf8');
const moduleFunctionScript = fs.readFileSync(moduleFunctionScriptPath, 'utf8');
const moduleFunctionStyles = fs.readFileSync(moduleFunctionStylePath, 'utf8');
assert.ok(moduleFunctionPage.includes(':id="functionAnchorId(item)"'),
    'The XNCF Function page should provide a stable target anchor for Workflow navigation.');
assert.ok(moduleFunctionScript.includes('applyFunctionNavigation') && moduleFunctionScript.includes('scrollIntoView'),
    'The XNCF Function page should scroll to the requested Function after loading it.');
assert.ok(moduleFunctionScript.includes("requestedFunctionAction === 'run'") && moduleFunctionScript.includes('this.openRun(item'),
    'A Function execution link should open the corresponding run panel after navigation.');
assert.match(moduleFunctionStyles, /\.function-card-highlight\s*\{[^}]*animation:\s*function-card-highlight/s,
    'The anchored Function card should visibly flash after navigation.');

let tasksVueOptions = null;
function TasksVue(options) { tasksVueOptions = options; }
let navigatedTaskUrl = '';
const tasksSandbox = {
    Vue: TasksVue,
    window: {
        setTimeout() { return 1; },
        clearTimeout() {},
        location: { assign(url) { navigatedTaskUrl = url; } }
    },
    service: {},
    NeuCharWorkflowUi: { unwrap(response) { return response; } },
    URLSearchParams,
    String,
    Number,
    Object,
    Array,
    console
};
vm.createContext(tasksSandbox);
vm.runInContext(fs.readFileSync(tasksScriptPath, 'utf8'), tasksSandbox, { filename: tasksScriptPath });
assert.ok(tasksVueOptions && tasksVueOptions.methods,
    'Workflow tasks should register an independent Vue task-list view model.');

const taskRows = [
    { workflowId: 21, workflowName: '正在运行的任务', status: 'running', source: 'interval', runId: 'f6d7e0a2-4f33-46f8-a9e3-116a272bab58', summary: '节点正在执行' },
    { workflowId: 22, workflowName: '已完成任务', status: 'success', source: 'history', summary: '完成', executionLogId: 88, replayAvailable: true },
    { workflowId: 23, workflowName: '失败任务', status: 'failed', source: 'history', errorMessage: 'Function 调用失败' }
];
assert.strictEqual(tasksVueOptions.methods.statusCount.call({ tasks: taskRows }, 'running'), 1,
    'The task overview must count currently running tasks separately.');
assert.strictEqual(tasksVueOptions.computed.hasRunningTasks.call({ tasks: taskRows }), true,
    'The task list should continue polling only while a task remains active.');
const filteredTaskRows = tasksVueOptions.computed.filteredTasks.call({ tasks: taskRows, keyword: '失败', statusFilter: 'failed' });
assert.strictEqual(filteredTaskRows.length, 1,
    'Task search and status filtering should compose without hiding matching failed tasks.');
assert.ok(fs.readFileSync(tasksScriptPath, 'utf8').includes('selectStatus(status)') &&
    fs.readFileSync(tasksScriptPath, 'utf8').includes('syncRoute()') &&
    fs.readFileSync(tasksPagePath, 'utf8').includes('>当前</el-tag>') &&
    fs.readFileSync(workflowAppServicePath, 'utf8').includes('WorkflowAnalyticsSummary'),
    'Task status cards must expose the current filter, synchronize the URL, and use full summary counts.');
tasksVueOptions.methods.openTask.call({ $message: { warning() {} } }, taskRows[0]);
assert.match(navigatedTaskUrl, /NeuCharWorkflow\/Index\?workflowId=21&runId=f6d7e0a2-4f33-46f8-a9e3-116a272bab58/,
    'An active task must reopen the locked Workflow editor and resume live status polling.');
tasksVueOptions.methods.openTask.call({}, taskRows[1]);
assert.match(navigatedTaskUrl, /NeuCharWorkflow\/Replay\?executionLogId=88/,
    'Opening a completed task should navigate to its immutable run replay instead of the live editor.');

const tasksPage = fs.readFileSync(tasksPagePath, 'utf8');
const tasksStyles = fs.readFileSync(tasksStylePath, 'utf8');
const workflowAppService = fs.readFileSync(workflowAppServicePath, 'utf8');
const runCoordinator = fs.readFileSync(runCoordinatorPath, 'utf8');
const workflowEngine = fs.readFileSync(workflowEnginePath, 'utf8');
assert.ok(fs.readFileSync(tasksScriptPath, 'utf8').includes('handler=List') && tasksPage.includes('回看运行'),
    'The task page should expose a task-list endpoint and an explicit replay action.');
assert.ok(tasksPage.includes('当前工作流 #{{workflowIdFilter}}') &&
    fs.readFileSync(tasksScriptPath, 'utf8').includes('workflowIdFilter') &&
    fs.readFileSync(tasksScriptPath, 'utf8').includes('workflowId=${encodeURIComponent(this.workflowIdFilter)}') &&
    fs.readFileSync(tasksScriptPath, 'utf8').includes('status=${encodeURIComponent(this.statusFilter)}') &&
    tasksPage.includes('正在运行') &&
    workflowAppService.includes('int? workflowId = null') &&
    workflowAppService.includes('run.WorkflowId == workflowId.Value'),
    'The task list should apply a workflow-scoped server query for direct links from detail and replay pages.');
assert.ok(tasksPage.includes('@@click.stop="abortTask(scope.row)"') && fs.readFileSync(tasksScriptPath, 'utf8').includes('handler=Abort'),
    'The task page should expose a manual abort action for active runs.');
assert.match(tasksPage, /src="~\/js\/NeuCharWorkflow\/Tasks\.js"\s+asp-append-version="true"/,
    'The task abort script must use a versioned URL so browsers receive the latest restart-recovery behavior.');
assert.ok(fs.readFileSync(tasksScriptPath, 'utf8').includes('executionLogId: task.executionLogId || null') &&
    workflowAppService.includes('GetUnfinishedByIdAsync'),
    'A persisted running task must remain abortable after the process that owned its run ID has restarted.');
assert.ok(tasksPage.includes('没有更多记录') && tasksPage.includes('快速清理') &&
    fs.readFileSync(tasksScriptPath, 'utf8').includes('beforeExecutionLogId') &&
    fs.readFileSync(tasksScriptPath, 'utf8').includes('loadMoreTasks') &&
    fs.readFileSync(tasksScriptPath, 'utf8').includes('handleScroll'),
    'The task page should use a cursor-backed incremental loader and explicitly signal the end of history.');
assert.ok(fs.readFileSync(tasksScriptPath, 'utf8').includes('handler=CleanupPreview') &&
    fs.readFileSync(tasksScriptPath, 'utf8').includes('handler=Cleanup') &&
    workflowAppService.includes('PreviewTaskCleanupAsync') && workflowAppService.includes('CleanupCompletedTasksAsync') &&
    workflowAppService.includes('z.FinishedAt != null'),
    'Quick cleanup should preview then delete only completed task logs, never active tasks.');
assert.ok(page.includes('@@click="abortWorkflow"') && fs.readFileSync(scriptPath, 'utf8').includes('handler=AbortRun'),
    'The workflow editor should keep a visible abort action while a test run is active.');
assert.ok(page.includes('运行中 {{form.runningCount}}') &&
    workflowScript.includes("openWorkflowTasks(status)") &&
    workflowScript.includes("query.set('status', status)") &&
    workflowScript.includes('snapshot.runningCount') &&
    workflowAppService.includes('GetActiveRunCount') &&
    runCoordinator.includes('public int GetActiveRunCount') &&
    runCoordinator.includes('RunningCount = GetActiveRunCount'),
    'The Workflow detail page should expose the active run count and link directly to its running-task filter.');
assert.ok(!runCoordinator.includes('当前工作流已有运行正在执行'),
    'A Workflow must be able to register more than one active run at the same time.');
assert.ok(page.includes('@@click="openWorkflowTasks"') &&
    workflowScript.includes("new URLSearchParams({ workflowId: String(workflowId) })"),
    'The Workflow detail page should jump directly to a list containing only its own tasks.');
assert.match(tasksStyles, /\.workflow-task-table \.el-table__row\s*\{[^}]*cursor:\s*pointer;/s,
    'Task rows should make their workflow-board navigation discoverable.');
assert.ok(workflowAppService.includes('GetTaskListAsync') && workflowAppService.includes('WorkflowTaskListItem'),
    'The application service should combine execution task data behind a dedicated contract.');
const analyticsScript = fs.readFileSync(analyticsScriptPath, 'utf8');
const analyticsPage = fs.readFileSync(analyticsPagePath, 'utf8');
const analyticsStyles = fs.readFileSync(analyticsStylePath, 'utf8');
const analyticsServicePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/Domain/Services/NeuCharWorkflowAnalyticsService.cs');
const analyticsService = fs.readFileSync(analyticsServicePath, 'utf8');
const workflowContracts = fs.readFileSync(workflowObjectContractsPath, 'utf8');
const agentsWorkflowObjectProvider = fs.readFileSync(agentsWorkflowObjectProviderPath, 'utf8');
assert.ok(analyticsPage.includes('Workflow 统计分析') &&
    analyticsPage.includes('daterange') &&
    analyticsPage.includes('全部外部资源') &&
    analyticsPage.includes('全部资源类型') &&
    analyticsPage.includes('运行明细') &&
    analyticsScript.includes('resourceType') &&
    analyticsScript.includes('resourceId') &&
    analyticsScript.includes("params.set('from', range[0])") &&
    analyticsScript.includes('openRun(run)') &&
    analyticsScript.includes('openTasks'),
    'Workflow analytics should expose date, workflow, status, resource filters and drill-down run navigation.');
assert.ok(analyticsStyles.includes('.workflow-analytics-summary') &&
    analyticsStyles.includes('.workflow-analytics-trend-bars') &&
    analyticsStyles.includes('.workflow-analytics-table'),
    'Workflow analytics should provide stable responsive dashboard and table layout styles.');
assert.ok(workflowAppService.includes('GetAnalyticsAsync') &&
    analyticsService.includes('WorkflowAnalyticsSummary') &&
    analyticsService.includes('ParseResources') &&
    analyticsService.includes('GetAnalyticsLogsAsync') &&
    analyticsService.includes('JsonDocument.Parse') &&
    !analyticsService.includes('GetFullListAsync(') &&
    analyticsService.includes('"function"') &&
    analyticsService.includes('"agent-group"'),
    'The server analytics contract should use a projected log query and lightweight resource parsing for run health and persisted references.');
assert.ok(workflowContracts.includes('string ObjectId = null') &&
    agentsWorkflowObjectProvider.includes('ObjectId: request.ObjectId') &&
    agentsWorkflowObjectProvider.includes('Reference = reference'),
    'Agent, AgentGroup and A2A execution references should carry stable resource identifiers into replay data.');
assert.ok(page.includes('openWorkflowAnalytics') &&
    workflowScript.includes('openWorkflowAnalytics') &&
    fs.readFileSync(replayPagePath, 'utf8').includes('查看统计') &&
    fs.readFileSync(replayScriptPath, 'utf8').includes('openWorkflowAnalytics'),
    'Workflow detail and replay pages should provide coordinated navigation into analytics.');
assert.ok(workflowAppService.includes('private static DateTimeOffset ToUtcOffset') &&
    workflowAppService.includes('DateTimeOffset StartedAt') &&
    workflowAppService.includes('ToUtcOffset(log.StartedAt)'),
    'Persisted Workflow timestamps must be serialized with an explicit UTC offset so browsers render them in the local time zone.');
assert.ok(workflowAppService.includes('GetReplayAsync') && workflowAppService.includes('CopyReplayAsDraftAsync'),
    'The application service should retrieve immutable task replays and create a disabled editable draft from them.');
assert.ok(runCoordinator.includes('GetActiveRuns') && runCoordinator.includes('NeuCharWorkflowActiveRun'),
    'The task list should use the coordinator for currently running node-level task state.');
assert.ok(runCoordinator.includes('TryAbort') && runCoordinator.includes('手动中止') &&
    workflowAppService.includes('AbortRunAsync') &&
    workflowAppService.includes('GetUnfinishedByRunIdAsync') &&
    workflowAppService.includes('aborted-after-restart'),
    'Manual aborts should cancel live runs and finalize abandoned persisted runs after a restart.');
assert.ok(workflowScript.includes("type: 'merge', name: '逐项合流'") && page.includes('汇总输出内容') &&
    workflowScript.includes("['aggregate', 'merge', 'function', 'loop-end']") &&
    workflowScript.includes('outputTemplate'),
    'The designer should distinguish once-only aggregation from per-item merge and expose aggregate output content.');
assert.ok(page.includes('Console 打印内容') && page.includes('仅影响 Console 展示，不改变下游输入') &&
    fs.readFileSync(scriptPath, 'utf8').includes("printTemplate: '{{input}}'") &&
    workflowEngine.includes('ResolveConsolePrintOutput') && workflowEngine.includes('"printTemplate"'),
    'The Console node should expose a configurable print template while preserving its raw downstream input.');
assert.ok(workflowEngine.includes('"merge"') && workflowEngine.includes('MaxStreamActivations') &&
    workflowEngine.includes('MaxReplayEvents') && workflowEngine.includes('5_000') &&
    workflowEngine.includes('ResolveAggregateOutput') && workflowEngine.includes('ValidateAggregateOutputTemplate') &&
    workflowEngine.includes('不能位于逐项合流节点之后'),
    'The engine should serially propagate merge activations, retain at least 5000 replay events, validate aggregate output templates, and reject ambiguous merge-to-aggregate chains.');
assert.ok(runCoordinator.includes('MaxLiveRunEvents') &&
    fs.readFileSync(scriptPath, 'utf8').includes('MaxWorkflowConsoleEvents') &&
    fs.readFileSync(scriptPath, 'utf8').includes('const MaxWorkflowConsoleEvents = 5000'),
    'Live Workflow Console and server-side run snapshots should retain at least 5000 events.');
assert.ok(workflowEngine.includes('MaxLoopIterations') && workflowEngine.includes('TryResolveLoopCount') &&
    workflowEngine.includes('循环或逐项合流产生的执行次数超过') &&
    workflowEngine.includes('不能位于循环节点之后'),
    'The engine should enforce bounded loop counts and protect loop/merge stream activations with one global cap.');
const replayPage = fs.readFileSync(replayPagePath, 'utf8');
const replayScript = fs.readFileSync(replayScriptPath, 'utf8');
const replayStyles = fs.readFileSync(replayStylePath, 'utf8');
assert.ok(replayPage.includes('只读运行回看') && replayPage.includes('复制当前工作流并编辑') && replayPage.includes('查看最新工作流'),
    'The separate replay page should clearly be read-only and expose both requested exit actions.');
assert.ok(replayPage.includes('查看此工作流任务') &&
    replayScript.includes('Tasks?workflowId=${encodeURIComponent(this.replay.workflowId)}'),
    'The replay page should jump directly to a list containing only the replayed Workflow tasks.');
assert.ok(replayPage.includes('workflow-replay-canvas') && replayPage.includes('执行步骤'),
    'The replay page should render the frozen workflow canvas and a step timeline.');
assert.ok(replayScript.includes('togglePlayback') && replayScript.includes('nextStep') && replayScript.includes('rebuildNodeStates'),
    'The replay client should play node events one step at a time and project their states onto the frozen canvas.');
assert.ok(replayScript.includes('visibleTimelineEvents') && replayScript.includes('timelineRenderLimit') &&
    replayPage.includes('v-for="(item,index) in visibleTimelineEvents"') &&
    replayPage.includes('asp-append-version="true"'),
    'Large replays should bound the initial timeline DOM and load a versioned replay client asset.');
assert.ok(replayStyles.includes('.workflow-replay-node.state-running') && replayStyles.includes('.workflow-replay-timeline-item.active'),
    'Replay styling should visually distinguish active nodes and the selected timeline step.');
assert.ok(replayPage.includes('输入参数') && replayPage.includes('currentEvent.input'),
    'The replay detail panel should show the recorded input parameters alongside output.');
assert.ok(replayPage.includes('查看 Agent Group 对话') &&
    replayScript.includes('openAgentGroupConversation') &&
    fs.readFileSync(scriptPath, 'utf8').includes('hasAgentGroupConversation(event)') &&
    fs.readFileSync(agentsWorkflowObjectProviderPath, 'utf8').includes('RunChatGroupAwaitWithResultAsync') &&
    fs.readFileSync(agentsWorkflowObjectProviderPath, 'utf8').includes('WorkflowObjectExecutionReference') &&
    workflowEngine.includes('ObjectReference') &&
    fs.readFileSync(workflowObjectContractsPath, 'utf8').includes('ChatTaskId'),
    'Agent Group Workflow events should retain the ChatTask reference so live Console and replay can open the persisted conversation.');
assert.ok(replayScript.includes('centerCurrentNode') && replayScript.includes("behavior: 'smooth'"),
    'Selecting or playing a replay step should smoothly center its node in the canvas viewport.');
assert.match(replayStyles, /\.workflow-replay-canvas-wrap\s*\{[^}]*scroll-behavior:\s*smooth;/s,
    'The replay canvas should retain smooth scrolling when the browser handles the scroll transition.');
assert.ok(replayPage.includes('workflow-replay-timeline-list'),
    'Replay steps should live in a dedicated scrollable list beneath the fixed heading.');
assert.match(replayStyles, /\.workflow-replay-layout\s*\{[^}]*grid-template-rows:\s*minmax\(0,\s*1fr\);[^}]*height:\s*clamp\(420px,\s*calc\(100vh - 310px\),\s*720px\);[^}]*min-height:\s*0;/s,
    'The replay layout should stay within the available viewport instead of growing with the event count.');
assert.match(replayStyles, /\.workflow-replay-timeline-list\s*\{[^}]*overflow-y:\s*auto;[^}]*overscroll-behavior:\s*contain;/s,
    'The replay step list should scroll independently without chaining into the page or canvas.');
let replayVueOptions = null;
function ReplayVue(options) { replayVueOptions = options; }
const replaySandbox = {
    Vue: ReplayVue,
    window: { location: { search: '' }, clearInterval() {}, setInterval() {} },
    service: {},
    NeuCharWorkflowUi: { unwrap(value) { return value; }, parseJson(value, fallback) { return fallback; } },
    URLSearchParams,
    Math,
    Number,
    String,
    Array,
    Object,
    console
};
vm.createContext(replaySandbox);
vm.runInContext(replayScript, replaySandbox, { filename: replayScriptPath });
let replayScrollTarget = null;
const replayCenterContext = {
    currentEvent: { nodeId: 'first' },
    graph: { nodes: [{ id: 'first', x: 0, y: 0 }] },
    canvasInset: 240,
    findNode: replayVueOptions.methods.findNode,
    $nextTick(callback) { callback(); },
    $refs: {
        canvasViewport: {
            clientWidth: 400, clientHeight: 300, scrollWidth: 1200, scrollHeight: 900,
            scrollTo(target) { replayScrollTarget = target; }
        }
    }
};
replayVueOptions.methods.centerCurrentNode.call(replayCenterContext);
assert.strictEqual(replayScrollTarget.left, 178,
    'Replay step navigation should center the selected node horizontally.');
assert.strictEqual(replayScrollTarget.top, 160,
    'Replay step navigation should center the selected node vertically.');
assert.strictEqual(replayScrollTarget.behavior, 'smooth',
    'Replay step navigation should use a smooth scroll request.');

async function verifyUnsavedChangeGuards() {
    let modalArguments = null;
    const dirtyContext = {
        saveDirty: true,
        discardConfirming: false,
        $confirm(...args) {
            modalArguments = args;
            return Promise.resolve();
        }
    };
    assert.strictEqual(await vueOptions.methods.confirmDiscardChanges.call(dirtyContext, '新建工作流'), true,
        'Confirming the warning should allow replacing a dirty workflow.');
    assert.strictEqual(dirtyContext.discardConfirming, false,
        'The replacement confirmation lock should be released after the dialog closes.');
    assert.match(modalArguments[0], /未保存的更改/,
        'The replacement warning should clearly explain that unsaved work will be lost.');
    assert.strictEqual(modalArguments[2].confirmButtonText, '放弃更改',
        'The destructive dialog action should be explicit.');

    const cancelledContext = {
        saveDirty: true,
        discardConfirming: false,
        $confirm() { return Promise.reject(new Error('cancelled')); }
    };
    assert.strictEqual(await vueOptions.methods.confirmDiscardChanges.call(cancelledContext, '切换工作流'), false,
        'Cancelling the warning should keep the user on the dirty workflow.');

    const blockedNewWorkflowContext = {
        editingLocked: false,
        saveState: { saving: false },
        confirmDiscardChanges() { return Promise.resolve(false); },
        emptyForm() { throw new Error('A cancelled confirmation must not replace the current form.'); }
    };
    await vueOptions.methods.createWorkflow.call(blockedNewWorkflowContext);

    const unloadEvent = {
        prevented: false,
        preventDefault() { this.prevented = true; },
        returnValue: undefined
    };
    assert.strictEqual(vueOptions.methods.onBeforeUnload.call({ saveDirty: true }, unloadEvent), '',
        'Leaving a page with unsaved changes should request the browser confirmation prompt.');
    assert.strictEqual(unloadEvent.prevented, true,
        'The browser navigation event should be cancelled while it asks for confirmation.');
    assert.strictEqual(unloadEvent.returnValue, '',
        'The browser confirmation prompt requires returnValue to be assigned.');
    assert.strictEqual(vueOptions.methods.onBeforeUnload.call({ saveDirty: false }, {}), undefined,
        'A saved workflow should not block normal page navigation.');

    const source = fs.readFileSync(scriptPath, 'utf8');
    assert.ok(source.includes("window.addEventListener('beforeunload', this.onBeforeUnload)"),
        'The workflow editor should subscribe to browser leave-page confirmation.');
assert.ok(source.includes("window.removeEventListener('beforeunload', this.onBeforeUnload)"),
        'The workflow editor should remove its leave-page confirmation listener when destroyed.');
    assert.ok(workflowPageMarkup.includes('@@click="refreshWorkflowList"'),
        'Refreshing the Workflow list should use the lightweight list-only request.');
    assert.match(source, /async refreshWorkflowList\(\)[\s\S]*?handler=List[\s\S]*?finally \{ this\.loading = false; \}/,
        'The Workflow list refresh should not wait for DesignerData or AIModel metadata.');
    assert.match(source, /this\.loadDesignerData\(\);\s*this\.loadChatModels\(\);/,
        'Initial page loading should start optional designer metadata without blocking the list.');
    assert.match(source, /async editWorkflow\(id\)\s*\{\s*if \(this\.editingLocked \|\| this\.saveState\.saving \|\| Number\(id\) === Number\(this\.form\.id\)\) return;\s*if \(!await this\.confirmDiscardChanges\('切换工作流'\)\) return;/s,
        'Switching workflows should ask before discarding unsaved changes.');

    let deleted = false;
    const deleteActionContext = {
        form: { id: 7, name: '待删除工作流' },
        editingLocked: false,
        saveState: { saving: false },
        $confirm() { return Promise.resolve(); },
        deleteWorkflow() { deleted = true; return Promise.resolve(); }
    };
    await vueOptions.methods.handleWorkflowAction.call(deleteActionContext, 'delete');
    assert.strictEqual(deleted, true,
        'The overflow menu should require confirmation, then invoke the existing workflow deletion action.');

    const viewModel = vueOptions.data();
    assert.strictEqual(viewModel.webhookHelpVisible, false,
        'Webhook help should stay collapsed until the user explicitly requests it.');
    assert.strictEqual(viewModel.workflowSettingsVisible, false,
        'Workflow settings should stay out of the canvas area until the user opens them.');
    assert.strictEqual(viewModel.paletteCollapsed, true,
        'The node palette should start collapsed so it does not permanently consume canvas width.');
    assert.strictEqual(viewModel.inspectorCollapsed, true,
        'The node inspector should start collapsed so it does not permanently consume canvas width.');
    assert.strictEqual(viewModel.run.consoleOpen, false,
        'The execution console should start collapsed so the canvas receives the available vertical space.');

    const settingsActionContext = { editingLocked: false, workflowSettingsVisible: false };
    await vueOptions.methods.handleWorkflowAction.call(settingsActionContext, 'settings');
    assert.strictEqual(settingsActionContext.workflowSettingsVisible, true,
        'The overflow menu should open the workflow settings dialog without changing the canvas layout.');

    let newWorkflowRequested = false;
    await vueOptions.methods.handleWorkflowAction.call({
        createWorkflow() { newWorkflowRequested = true; return Promise.resolve(); }
    }, 'new');
    assert.strictEqual(newWorkflowRequested, true,
        'The overflow menu should retain a new-workflow fallback when the list is unavailable on a narrow screen.');

    let autoLayoutCalled = false;
    let gridAlignmentCalled = false;
    let fitCanvasCalled = false;
    const canvasActionContext = {
        autoLayout() { autoLayoutCalled = true; },
        alignToNearbyGrid() { gridAlignmentCalled = true; },
        fitCanvasToNodes() { fitCanvasCalled = true; }
    };
    await vueOptions.methods.handleWorkflowAction.call(canvasActionContext, 'auto-layout');
    await vueOptions.methods.handleWorkflowAction.call(canvasActionContext, 'align-grid');
    await vueOptions.methods.handleWorkflowAction.call(canvasActionContext, 'fit-canvas');
    assert.strictEqual(autoLayoutCalled, true,
        'The compact overflow menu should retain the automatic layout action.');
    assert.strictEqual(gridAlignmentCalled, true,
        'The compact overflow menu should expose nearby-grid alignment without forcing a full re-layout.');
    assert.strictEqual(fitCanvasCalled, true,
        'The compact overflow menu should retain the fit-canvas action.');
}

async function verifyWorkflowHumanInteractionSubmission() {
    const submittedRequests = [];
    sandbox.service.post = async (url, body, options) => {
        submittedRequests.push({ url, body, options });
        return { data: { success: true } };
    };

    const humanTurnContext = {
        run: {
            runId: 'a08b0f18-1e76-42d4-aa1b-d0aaee5a9071',
            humanReplyRequest: { requestId: 'human-turn-1', requestType: 'humanTurn' },
            humanReplyInput: '  补充给 Group 的 Human 输入  ',
            humanReplySubmitting: false,
            humanInteractions: [{ requestId: 'human-turn-1', requestType: 'humanTurn' }],
            humanReplyVisible: true
        },
        requiresHumanTextInput: vueOptions.methods.requiresHumanTextInput,
        errorMessage(error) { return error.message; },
        $notify() { }
    };

    await vueOptions.methods.resolveHumanInteraction.call(humanTurnContext, true);
    assert.strictEqual(submittedRequests.length, 1,
        'Submitting a Human turn should issue one workflow resolution request.');
    assert.strictEqual(submittedRequests[0].url, '/Admin/NeuCharWorkflow/Index?handler=ResolveHuman');
    assert.deepStrictEqual(JSON.parse(JSON.stringify(submittedRequests[0].body)), {
        runId: 'a08b0f18-1e76-42d4-aa1b-d0aaee5a9071',
        requestId: 'human-turn-1',
        approved: true,
        input: '补充给 Group 的 Human 输入',
        reason: 'Workflow 快速输入'
    }, 'Human turns should submit trimmed input and the workflow-input reason without referencing an undefined variable.');

    const approvalContext = {
        run: {
            runId: 'a08b0f18-1e76-42d4-aa1b-d0aaee5a9071',
            humanReplyRequest: { requestId: 'tool-approval-1', requestType: 'toolApproval', prompt: '允许调用工具' },
            humanReplyInput: '不应发送为工具审批输入',
            humanReplySubmitting: false,
            humanInteractions: [{ requestId: 'tool-approval-1', requestType: 'toolApproval' }],
            humanReplyVisible: true
        },
        requiresHumanTextInput: vueOptions.methods.requiresHumanTextInput,
        errorMessage(error) { return error.message; },
        $notify() { }
    };

    await vueOptions.methods.resolveHumanInteraction.call(approvalContext, false);
    assert.strictEqual(submittedRequests.length, 2,
        'Rejecting a tool approval should issue a separate workflow resolution request.');
    assert.deepStrictEqual(JSON.parse(JSON.stringify(submittedRequests[1].body)), {
        runId: 'a08b0f18-1e76-42d4-aa1b-d0aaee5a9071',
        requestId: 'tool-approval-1',
        approved: false,
        input: '',
        reason: 'Workflow 快速审批'
    }, 'Tool approvals should keep the approval reason and omit Human text input.');
}

Promise.all([verifyUnsavedChangeGuards(), verifyWorkflowHumanInteractionSubmission()])
    .then(() => console.log('NeuChar Workflow designer tests passed.'))
    .catch(error => {
        console.error(error);
        process.exitCode = 1;
    });

function sourceIncludesFitOnLoad() {
    const source = fs.readFileSync(scriptPath, 'utf8');
    return source.includes('this.$nextTick(() => this.fitCanvasToNodes())');
}
