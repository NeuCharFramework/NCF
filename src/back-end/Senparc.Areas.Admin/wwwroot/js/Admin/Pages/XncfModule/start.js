var app = new Vue({
    el: "#app",
    data() {
        return {
            data: [], // 数据
            tooltip: {
                "IAreaRegister": ncfT('Xncf.AreaFeature'),
                "IXncfDatabase": ncfT('Xncf.DatabaseFeature'),
                "IXncfMiddleware": ncfT('Xncf.MiddlewareFeature'),
                "IXncfRazorRuntimeCompilation": ncfT('Xncf.ThreadFeature')
            },
            state: {
                'String': ncfT('Xncf.TypeText'),
                'Int32': ncfT('Xncf.TypeNumber'),
                'Int64': ncfT('Xncf.TypeNumber'),
                'DateTime': ncfT('Xncf.TypeDate'),
                'String[]': ncfT('Xncf.TypeOption'),
                'Boolean': ncfT('Xncf.TypeBoolean')
            },
            xNcfModules_State: {
                0: ncfT('Xncf.Close'),
                1: ncfT('Xncf.Open'),
                2: ncfT('Xncf.NewModules'),
                3: ncfT('Xncf.PendingUpdates')
            },
            // 执行弹窗
            run: {
                data: {},
                visible: false,
                loading: false
            },
            runData: {
                // 绑定数据
            },
            runResult: {
                visible: false,
                tit: '',
                tip: '',
                msg: '',
                url: '',
                tempId: '',
                hasLog: false
            },
            //查看线程 
            thread: {
                visible: false
            },
            activeModuleTab: 'function',
            requestedFunctionKey: '',
            requestedFunctionAction: '',
            highlightedFunctionKey: '',
            functionNavigationApplied: false,
            updateLogVisible: false,
            pivot: {
                loading: false,
                requirement: 'Generate a concise and clean operation panel for all Functions.',
                aiModelId: 0,
                aiModels: [],
                configuration: null,
                functions: [],
                layout: { title: 'NeuCharPivot', description: '', columns: 2, sections: [] },
                inputs: {},
                moduleAvailable: false,
                moduleState: 'missing'
            },
            chat: {
                visible: false,
                message: ''
            },
            loop: {
                visible: false,
                saving: false,
                function: null,
                enabled: false,
                intervalSeconds: 300,
                useNeuBell: false
            }
        };
    },
    created() {
        this.readFunctionNavigation();
        this.getList();
    },
    methods: {
        safeHtml(value) {
            return typeof DOMPurify === 'undefined' ? '' : DOMPurify.sanitize(String(value || ''));
        },
        decodeFunctionResult(value) {
            if (typeof document === 'undefined') {
                return '';
            }

            // Function 返回值在服务端会先进行 HTML 编码。使用 textarea 仅解码文本，
            // 实际输出仍必须经过 safeFunctionResultHtml() 的严格净化。
            const decoder = document.createElement('textarea');
            decoder.innerHTML = String(value || '');
            return decoder.value.replace(/\\r\\n|\\n|\\r/g, '\n');
        },
        safeFunctionResultHtml(value) {
            if (typeof DOMPurify === 'undefined') {
                return '';
            }

            return DOMPurify.sanitize(this.decodeFunctionResult(value), {
                ALLOWED_TAGS: [
                    'a', 'b', 'blockquote', 'br', 'code', 'div', 'em',
                    'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'hr', 'i',
                    'li', 'mark', 'ol', 'p', 'pre', 'small', 'span',
                    'strong', 'sub', 'sup', 'table', 'tbody', 'td',
                    'th', 'thead', 'tr', 'u', 'ul'
                ],
                ALLOWED_ATTR: ['href', 'title'],
                ALLOW_ARIA_ATTR: false,
                ALLOW_DATA_ATTR: false,
                ALLOWED_URI_REGEXP: /^https?:\/\/[^\s<>"']+$/i
            });
        },
        normalizeMultiValue(value) {
            if (Array.isArray(value)) {
                return value;
            }

            if (typeof value !== 'string' || value.length === 0) {
                return [];
            }

            return value
                .split(/[;,，；\n\r|]+/)
                .map(item => item.trim())
                .filter(item => item.length > 0);
        },
        async  getList() {
            const uid = resizeUrl().uid;
            const res = await service.get(`/Admin/XncfModule/Start?handler=Detail&uid=${uid}`);
            this.data = res.data.data;
            this.data.xncfRegister.interfaces = this.data.xncfRegister.interfaces.splice(1);
            window.document.title = this.data.xncfModule.menuName;
            await Promise.all([this.loadPivot(), this.loadAiModels()]);
            this.applyFunctionNavigation();
        },

        readFunctionNavigation() {
            if (typeof window === 'undefined' || typeof URLSearchParams === 'undefined') {
                return;
            }
            const params = new URLSearchParams(window.location.search || '');
            this.requestedFunctionKey = String(params.get('functionKey') || '').trim();
            this.requestedFunctionAction = String(params.get('action') || 'settings').trim().toLowerCase();
        },
        functionKey(item) {
            return String(item && item.key && (item.key.functionKey || item.key.name) || '').trim();
        },
        functionAnchorId(item) {
            const key = this.functionKey(item);
            return key ? `function-${encodeURIComponent(key)}` : '';
        },
        isFocusedFunction(item) {
            return !!this.highlightedFunctionKey &&
                this.functionKey(item).toLowerCase() === this.highlightedFunctionKey.toLowerCase();
        },
        applyFunctionNavigation() {
            if (this.functionNavigationApplied || !this.requestedFunctionKey) {
                return;
            }
            this.functionNavigationApplied = true;
            const items = (this.data && this.data.functionParameterInfoCollection) || [];
            const item = items.find(candidate =>
                this.functionKey(candidate).toLowerCase() === this.requestedFunctionKey.toLowerCase());
            if (!item) {
                if (this.$notify) {
                    this.$notify({
                        title: ncfT('Xncf.Prompt'),
                        message: '目标 Function 已被移除或在模块更新后发生变化。',
                        type: 'warning'
                    });
                }
                return;
            }

            this.highlightedFunctionKey = this.functionKey(item);
            this.$nextTick(() => {
                const target = document.getElementById(this.functionAnchorId(item));
                if (target && typeof target.scrollIntoView === 'function') {
                    target.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
                if (this.requestedFunctionAction === 'run') {
                    this.openRun(item, this.data.xncfModule.state);
                }
                window.setTimeout(() => { this.highlightedFunctionKey = ''; }, 2600);
            });
        },

        unwrapResponse(response) {
            if (!response || !response.data) {
                return null;
            }
            return Object.prototype.hasOwnProperty.call(response.data, 'data')
                ? response.data.data
                : response.data;
        },
        parseJson(value, fallback) {
            try {
                return value ? JSON.parse(value) : fallback;
            } catch (error) {
                console.warn('NeuCharPivot JSON 解析失败：', error);
                return fallback;
            }
        },
        async loadAiModels() {
            try {
                const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetAiModelOptionsAsync');
                const payload = this.unwrapResponse(response) || {};
                this.pivot.aiModels = payload.models || [{ id: 0, name: '系统默认模型' }];
                if (!this.pivot.aiModels.some(model => Number(model.id) === Number(this.pivot.aiModelId))) {
                    this.pivot.aiModelId = Number(this.pivot.aiModels[0].id || 0);
                }
            } catch (error) {
                console.warn('加载 AI 模型失败：', error);
                this.pivot.aiModels = [{ id: 0, name: '系统默认模型' }];
            }
        },
        async loadPivot() {
            const uid = resizeUrl().uid;
            if (!uid) {
                return;
            }
            this.pivot.loading = true;
            try {
                const response = await service.get(`/Admin/XncfModule/Start?handler=NeuCharPivot&uid=${encodeURIComponent(uid)}`);
                this.applyPivotSnapshot(this.unwrapResponse(response));
            } catch (error) {
                console.error('加载 NeuCharPivot 失败：', error);
            } finally {
                this.pivot.loading = false;
            }
        },
        applyPivotSnapshot(snapshot) {
            if (!snapshot) {
                this.pivot.configuration = null;
                this.pivot.functions = [];
                this.pivot.inputs = {};
                this.pivot.moduleAvailable = this.data.xncfModule && Number(this.data.xncfModule.state) === 1;
                return;
            }
            this.pivot.configuration = snapshot.configuration || null;
            this.pivot.functions = snapshot.functions || [];
            this.pivot.moduleAvailable = !!snapshot.moduleAvailable;
            this.pivot.moduleState = snapshot.moduleState || 'missing';
            this.pivot.layout = this.parseJson(
                snapshot.configuration && snapshot.configuration.layoutSchemaJson,
                { title: 'NeuCharPivot', description: '', columns: 2, sections: [] });
            this.pivot.requirement = (snapshot.configuration && snapshot.configuration.userRequirement) || this.pivot.requirement;
            this.pivot.aiModelId = Number((snapshot.configuration && snapshot.configuration.aiModelId) || this.pivot.aiModelId || 0);
            const nextInputs = {};
            this.pivot.functions.forEach(fn => {
                const defaults = this.parseJson(fn.defaultParametersJson, {});
                const schema = this.getPivotParameters(fn);
                nextInputs[fn.id] = {};
                schema.forEach(parameter => {
                    let value = Object.prototype.hasOwnProperty.call(defaults, parameter.name)
                        ? defaults[parameter.name]
                        : parameter.defaultValue;
                    if (parameter.parameterType === 2 && !Array.isArray(value)) {
                        value = this.normalizeMultiValue(value);
                    } else if (parameter.parameterType === 4) {
                        value = value === true || value === 'true' || value === 'True';
                    } else if (value === null || typeof value === 'undefined') {
                        value = '';
                    }
                    nextInputs[fn.id][parameter.name] = value;
                });
            });
            this.pivot.inputs = nextInputs;
        },
        getPivotFunction(functionKey) {
            return this.pivot.functions.find(fn => String(fn.functionKey).toLowerCase() === String(functionKey).toLowerCase());
        },
        getPivotParameters(fn) {
            return fn ? this.parseJson(fn.parameterSchemaJson, []) : [];
        },
        getPivotParameterJson(fn) {
            return JSON.stringify((fn && this.pivot.inputs[fn.id]) || {});
        },
        validatePivotRequired(fn) {
            const values = (fn && this.pivot.inputs[fn.id]) || {};
            const missing = this.getPivotParameters(fn).find(parameter => {
                if (!parameter.required) {
                    return false;
                }
                const value = values[parameter.name];
                return value === null || typeof value === 'undefined' || value === '' || (Array.isArray(value) && value.length === 0);
            });
            if (missing) {
                this.$notify({
                    title: '必填参数',
                    message: `请先填写“${missing.title || missing.name}”。`,
                    type: 'warning'
                });
                return false;
            }
            return true;
        },
        async requestPivot(requirement) {
            if (!String(requirement || '').trim()) {
                this.$notify({ title: '提示', message: '请先输入生成或修改要求。', type: 'warning' });
                return;
            }
            this.pivot.loading = true;
            try {
                const response = await service.post('/Admin/XncfModule/Start?handler=GenerateNeuCharPivot', {
                    xncfUid: this.data.xncfModule.uid,
                    userRequirement: String(requirement).trim(),
                    aiModelId: Number(this.pivot.aiModelId || 0)
                }, { customAlert: true });
                this.applyPivotSnapshot(this.unwrapResponse(response));
                this.chat.message = '';
                this.$notify({ title: 'NeuCharPivot', message: '界面已生成并保存。', type: 'success' });
            } catch (error) {
                console.error('生成 NeuCharPivot 失败：', error);
                const message = error.response && error.response.data
                    ? (error.response.data.title || error.response.data)
                    : '生成失败，请检查 AI 模型配置和系统日志。';
                this.$notify({ title: '生成失败', message: String(message), type: 'error' });
            } finally {
                this.pivot.loading = false;
            }
        },
        generatePivot() {
            return this.requestPivot(this.pivot.requirement);
        },
        refinePivotFromChat() {
            return this.requestPivot(this.chat.message);
        },
        openModuleChat() {
            this.chat.visible = true;
        },
        openFullModuleChat() {
            const uid = this.data.xncfModule.uid;
            window.location.href = `/Admin/AdminChat/Chat?moduleUids=${encodeURIComponent(uid)}`;
        },
        async runPivotFunction(fn) {
            if (!this.pivot.moduleAvailable || !fn || !fn.available || !this.validatePivotRequired(fn)) {
                return;
            }
            this.pivot.loading = true;
            try {
                const response = await service.post('/Admin/XncfModule/Start?handler=RunPivotFunction', {
                    functionId: fn.id,
                    parametersJson: this.getPivotParameterJson(fn)
                }, { customAlert: true });
                const result = this.unwrapResponse(response) || {};
                this.showFunctionResult(result.success, result.data, result.errorMessage, result.requestTempId);
            } catch (error) {
                this.$notify({ title: '执行失败', message: 'Function 执行请求失败。', type: 'error' });
            } finally {
                this.pivot.loading = false;
            }
        },
        showFunctionResult(success, data, errorMessage, requestTempId) {
            const value = typeof data === 'string' ? data : JSON.stringify(data === undefined ? null : data, null, 2);
            this.runResult.url = '';
            this.runResult.tempId = requestTempId || '';
            this.runResult.hasLog = !!requestTempId;
            this.runResult.tit = success ? ncfT('Xncf.RunSuccess') : ncfT('Xncf.RunError');
            this.runResult.tip = success ? ncfT('Xncf.ReturnInfo') : ncfT('Xncf.ErrorInfo');
            this.runResult.msg = success ? value : (errorMessage || value || '执行失败');
            this.runResult.visible = true;
        },
        openLoopTask(fn) {
            const task = fn.loopTask || {};
            this.loop.function = fn;
            this.loop.enabled = !!task.enabled;
            this.loop.intervalSeconds = Number(task.intervalSeconds || 300);
            this.loop.useNeuBell = !!task.useNeuBell;
            this.loop.visible = true;
        },
        async saveLoopTask() {
            const fn = this.loop.function;
            if (!fn || (this.loop.enabled && !this.validatePivotRequired(fn))) {
                return;
            }
            this.loop.saving = true;
            try {
                const response = await service.post('/Admin/XncfModule/Start?handler=SaveLoopTask', {
                    functionId: fn.id,
                    parametersJson: this.getPivotParameterJson(fn),
                    enabled: !!this.loop.enabled,
                    intervalSeconds: Number(this.loop.intervalSeconds || 300),
                    useNeuBell: !!this.loop.useNeuBell
                }, { customAlert: true });
                fn.loopTask = this.unwrapResponse(response);
                this.loop.visible = false;
                this.$notify({ title: 'Loop Task', message: '定时任务配置已保存。', type: 'success' });
            } catch (error) {
                this.$notify({ title: '保存失败', message: '请检查必填参数和间隔设置。', type: 'error' });
            } finally {
                this.loop.saving = false;
            }
        },


        // 打开首页
        openUrl(url, flag) {
            // 关闭状态返回
            flag = flag + '';
            if (flag !== '1') {
                this.$notify({
                    title: ncfT('Xncf.Prompt'),
                    message: ncfT('Xncf.EnableFirst'),
                    type: 'warning'
                });
                return;
            }
            window.location.href = url;
        },
        // 打开执行
        openRun(item, flag) {
            // 关闭状态返回
            flag = flag + '';
            if (flag !== '1') {
                this.$notify({
                    title: ncfT('Xncf.Prompt'),
                    message: ncfT('Xncf.EnableFirst'),
                    type: 'warning'
                });
                return;
            }
            this.run.data = item;
            this.runData = {};
            this.run.data.value.map(res => {
                // 动态model绑定生成
                // 默认选择赋值
                // 多选
                if (res.parameterType === 2 && res.selectionList.items) {
                    this.runData[res.name] = {};
                    this.runData[res.name].value = this.normalizeMultiValue(res.value);
                    this.runData[res.name].item = res;
                    res.selectionList.items.map(ele => {
                        if (ele.defaultSelected && this.runData[res.name].value.indexOf(ele.value) < 0) {
                            this.runData[res.name].value.push(ele.value);
                        }
                    });
                }
                // 下拉框value
                if (res.parameterType === 1 && res.selectionList.items) {
                    this.runData[res.name] = {};
                    this.runData[res.name].value = res.value === null || typeof res.value === 'undefined' ? '' : res.value;
                    this.runData[res.name].item = res;
                    res.selectionList.items.map(ele => {
                        if (!this.runData[res.name].value && ele.defaultSelected) {
                            this.runData[res.name].value = ele.value;
                        }
                    });
                    // 如果没有默认给第一个
                    if (this.runData[res.name].value.length === 0) {
                        this.runData[res.name].value = res.selectionList.items[0].value;
                    }
                }
                // 输入框
                if (res.parameterType === 0 || res.parameterType === 3) {
                    this.runData[res.name] = {};
                    this.runData[res.name].item = res;
                    this.runData[res.name].value = res.value === null || typeof res.value === 'undefined' ? '' : res.value;
                }
                // 布尔（单个复选框）
                if (res.parameterType === 4) {
                    this.runData[res.name] = {};
                    this.runData[res.name].item = res;
                    this.runData[res.name].value = res.value === true || res.value === 'true' || res.value === 'True';
                }
            });
            this.runData = Object.assign({}, this.runData);
            //  this.runData数组结构
            //在接口传输时，将下拉单选转成数组
            //{
            //   // parameterType === 2 多选
            //    Modules: {
            //        item: {},
            //        value: []
            //    },
            //   // parameterType === 1 下拉单选
            //    ReferenceType: {
            //        item: {},
            //        value: []
            //    },
            //   // parameterType === 0 input
            //    SourcePath: {
            //        item: {},
            //        value: ''
            //    }
            //};
            this.run.visible = true;
        },
        // 执行
        async handleRun() {
            // 物理路径校验
            if (this.runData.hasOwnProperty('SourcePath') && this.runData.SourcePath.length < 1) {
                this.$notify({
                    title: ncfT('Xncf.Prompt'),
                    message: ncfT('Xncf.SourcePathRequired'),
                    type: 'warning'
                });
                return;
            }

            // 设置 loading 状态
            this.run.loading = true;

            try {
                let xncfFunctionParams = {};
                for (var i in this.runData) {
                    // 多选
                    if (this.runData[i].item.parameterType === 2) {
                        if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                            this.$notify({
                                title: ncfT('Xncf.Prompt'),
                                message: ncfT('Xncf.RequiredOption', this.runData[i].item.title),
                                type: 'warning'
                            });
                            return;
                        } else {
                            xncfFunctionParams[i] = this.runData[i].value;
                        }
                    }
                    // 下拉框
                    if (this.runData[i].item.parameterType === 1) {
                        if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                            this.$notify({
                                title: ncfT('Xncf.Prompt'),
                                message: ncfT('Xncf.RequiredField', this.runData[i].item.title),
                                type: 'warning'
                            });
                            return;
                        } else {
                            xncfFunctionParams[i] = this.runData[i].value;
                        }
                    }
                    // 输入框
                    if (this.runData[i].item.parameterType === 0 || this.runData[i].item.parameterType === 3) {
                        if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                            this.$notify({
                                title: ncfT('Xncf.Prompt'),
                                message: ncfT('Xncf.RequiredField', this.runData[i].item.title),
                                type: 'warning'
                            });
                            return;
                        } else {
                            xncfFunctionParams[i] = this.runData[i].value;
                        }
                    }
                    // 布尔
                    if (this.runData[i].item.parameterType === 4) {
                        xncfFunctionParams[i] = !!this.runData[i].value;
                    }
                }
                const data = {
                    xncfUid: this.data.xncfModule.uid,
                    xncfFunctionName: this.run.data.key.functionKey || this.run.data.key.name,
                    xncfFunctionParams: JSON.stringify(xncfFunctionParams)
                };

                const res = await service.post(`/Admin/XncfModule/Start?handler=RunFunction`, data, { customAlert: true });

                this.runResult.url = '';
                this.runResult.hasLog = false;
                this.runResult.tempId = res.data.tempId;
                if ((res.data.log || '').length > 0 && (res.data.tempId || '').length > 0) {
                    this.runResult.hasLog = true;
                }

                const rawMsg = res.data.msg || '';
                const decodedMsg = this.decodeFunctionResult(rawMsg);

                if (!res.data.success) {
                    this.runResult.tit = ncfT('Xncf.RunError');
                    this.runResult.tip = ncfT('Xncf.ErrorInfo');
                    this.runResult.msg = rawMsg || res.data.exception || '';
                    this.runResult.visible = true;
                    return;
                }
                if (decodedMsg && (decodedMsg.indexOf('http://') === 0 || decodedMsg.indexOf('https://') === 0)) {
                    this.runResult.tit = ncfT('Xncf.RunSuccess');
                    this.runResult.tip = ncfT('Xncf.UrlResultTip');
                    try {
                        const safeUrl = new URL(decodedMsg, window.location.origin);
                        if (safeUrl.protocol !== 'http:' && safeUrl.protocol !== 'https:') {
                            throw new Error('unsupported URL scheme');
                        }
                        this.runResult.url = safeUrl.href;
                        this.runResult.msg = '';
                    } catch {
                        this.runResult.msg = rawMsg;
                    }
                }
                else {
                    this.runResult.tit = ncfT('Xncf.RunSuccess');
                    this.runResult.tip = ncfT('Xncf.ReturnInfo');
                    this.runResult.msg = rawMsg;
                }
                // 打开执行结果弹窗
                this.runResult.visible = true;
                this.getList();
            } catch (error) {
                console.error('执行出错:', error);
                this.$notify({
                    title: ncfT('Xncf.RunError'),
                    message: ncfT('Xncf.ExecutionError'),
                    type: 'error'
                });
            } finally {
                // 无论成功失败都取消 loading 状态
                this.run.loading = false;
            }
        },
        // 关闭和开启
        async updataState(state) {
            const id = this.data.xncfModule.id;
            const res = await service.get(`/Admin/XncfModule/Start?handler=ChangeState&id=${id}&tostate=${state}`);
            window.location.reload();
        },
        // 更新版本
        async  updataVersion() {
            const uid = resizeUrl().uid;
            await service.get(`/Admin/XncfModule/Index?handler=ScanAjax&uid=${uid}`);
            window.location.reload();
        },
        // 删除
        async handleDelete() {
            const id = this.data.xncfModule.id;
            const res = await service.post(`/Admin/XncfModule/Start?handler=Delete&id=${id}`);
            window.sessionStorage.setItem('setNavMenuActive', ncfT('Xncf.Title'));
            getNavMenu();
            setTimeout(function () {
                window.location.href = '/Admin/XncfModule/Index';
            }, 100);
        }
    }
});
