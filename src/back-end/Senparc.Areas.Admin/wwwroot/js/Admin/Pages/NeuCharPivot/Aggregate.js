new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            modules: [],
            inputs: {},
            keyword: '',
            moduleStateFilter: '',
            functionStateFilter: '',
            loopStatusFilter: '',
            workflowFilter: '',
            openedModules: [],
            activePanels: {},
            workflowOptions: [],
            summary: {},
            showUnavailableModules: false,
            now: new Date(),
            clockTimer: null,
            result: { visible: false, title: '', content: '', html: '', htmlMode: false }
        };
    },
    computed: {
        filteredModules() {
            return this.modules
                .map(module => ({
                    ...module,
                    functions: module.functions.filter(fn => this.matchesFunction(module, fn))
                }))
                .filter(module =>
                    this.matchesModule(module) &&
                    (this.showUnavailableModules ||
                        this.moduleStateFilter === 'unavailable' ||
                        module.moduleAvailable) &&
                    (module.functions.length > 0 || this.moduleMatchesKeyword(module)));
        }
    },
    created() {
        this.load();
        this.clockTimer = window.setInterval(() => { this.now = new Date(); }, 1000);
    },
    beforeDestroy() {
        if (this.clockTimer) window.clearInterval(this.clockTimer);
    },
    methods: {
        async load() {
            this.loading = true;
            try {
                const response = await service.get('/Admin/NeuCharPivot/Aggregate?handler=List');
                const body = NeuCharPivotUi.unwrap(response) || {};
                const modules = Array.isArray(body) ? body : (body.modules || []);
                this.workflowOptions = Array.isArray(body.workflowOptions) ? body.workflowOptions : [];
                this.summary = body.summary || {};
                const inputs = {};
                this.modules = modules.map(module => {
                    const layout = NeuCharPivotUi.normalizeLayout(module.layoutSchemaJson ||
                        (module.configuration && module.configuration.layoutSchemaJson));
                    module.layout = layout;
                    (module.functions || []).forEach(fn => {
                        inputs[fn.id] = NeuCharPivotUi.createParameterValues(fn);
                    });
                    this.$set(this.activePanels, module.configuration.moduleUid,
                        this.activePanels[module.configuration.moduleUid] ||
                        (layout.panels[0] && layout.panels[0].key));
                    return module;
                }).sort((left, right) => Number(right.moduleAvailable) - Number(left.moduleAvailable));
                this.inputs = inputs;
                this.openedModules = this.modules.slice(0, 3)
                    .map(module => module.configuration.moduleUid);
            } finally {
                this.loading = false;
            }
        },
        resetFilters() {
            this.keyword = '';
            this.moduleStateFilter = '';
            this.functionStateFilter = '';
            this.loopStatusFilter = '';
            this.workflowFilter = '';
        },
        moduleAnchorId(module) {
            return `aggregate-module-${module.configuration.id}`;
        },
        jumpToModule(module) {
            const uid = module.configuration.moduleUid;
            if (!this.openedModules.includes(uid)) {
                this.openedModules = this.openedModules.concat(uid);
            }
            this.$nextTick(() => {
                const target = document.getElementById(this.moduleAnchorId(module));
                if (target && typeof target.scrollIntoView === 'function') {
                    target.scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            });
        },
        moduleMatchesKeyword(module) {
            const keyword = this.keyword.trim().toLowerCase();
            if (!keyword) return true;
            return [
                module.configuration.name,
                module.configuration.moduleUid,
                module.moduleState
            ].some(value => String(value || '').toLowerCase().includes(keyword));
        },
        matchesModule(module) {
            if (this.moduleStateFilter === 'available' && !module.moduleAvailable) return false;
            if (this.moduleStateFilter === 'unavailable' && module.moduleAvailable) return false;
            return true;
        },
        matchesFunction(module, fn) {
            const keyword = this.keyword.trim().toLowerCase();
            const textMatched = !keyword || [
                fn.functionName,
                fn.functionKey,
                fn.description,
                module.configuration.name,
                fn.loopTask && this.workflowName(fn.loopTask.workflowId)
            ].some(value => String(value || '').toLowerCase().includes(keyword));
            if (!textMatched) return false;
            if (this.functionStateFilter === 'available' && !fn.available) return false;
            if (this.functionStateFilter === 'unavailable' && fn.available) return false;
            const status = NeuCharPivotUi.loopStatus(fn.loopTask);
            if (this.loopStatusFilter && status !== this.loopStatusFilter) return false;
            if (this.workflowFilter === 'linked' && !(fn.loopTask && fn.loopTask.workflowId)) return false;
            if (this.workflowFilter === 'unlinked' && fn.loopTask && fn.loopTask.workflowId) return false;
            if (this.workflowFilter && this.workflowFilter !== 'linked' && this.workflowFilter !== 'unlinked' &&
                String(fn.loopTask && fn.loopTask.workflowId || '') !== this.workflowFilter) return false;
            return true;
        },
        panels(module) {
            return module.layout && module.layout.panels ? module.layout.panels : [];
        },
        visibleFunctions(module, section) {
            const byKey = new Map((module.functions || []).map(fn => [String(fn.functionKey).toLowerCase(), fn]));
            return (section.functions || [])
                .map(item => byKey.get(String(item.functionKey || '').toLowerCase()))
                .filter(fn => fn && this.matchesFunction(module, fn));
        },
        getParameters(fn) {
            return NeuCharPivotUi.parseJson(fn.parameterSchemaJson, []);
        },
        workflowName(workflowId) {
            const item = this.workflowOptions.find(option => Number(option.id) === Number(workflowId));
            return item ? item.name : `Workflow #${workflowId}`;
        },
        loopStatus(fn) {
            return NeuCharPivotUi.loopStatus(fn.loopTask);
        },
        loopStatusText(fn) {
            return NeuCharPivotUi.loopStatusText(this.loopStatus(fn));
        },
        loopCountdown(fn) {
            return NeuCharPivotUi.formatCountdown(fn.loopTask && fn.loopTask.nextRunAt, this.now);
        },
        async run(module, fn) {
            if (!module.moduleAvailable || !fn.available) {
                this.$notify({ title: '不可执行', message: '模块未开启，或 Function 已在新版本中移除。', type: 'warning' });
                return;
            }
            const missing = NeuCharPivotUi.firstMissingRequired(fn, this.inputs[fn.id]);
            if (missing) {
                this.$notify({ title: '必填参数', message: `请先填写“${missing.title || missing.name}”。`, type: 'warning' });
                return;
            }
            this.loading = true;
            try {
                const response = await service.post('/Admin/NeuCharPivot/Aggregate?handler=Run', {
                    functionId: fn.id,
                    parametersJson: JSON.stringify(this.inputs[fn.id] || {})
                }, { customAlert: true });
                const data = NeuCharPivotUi.unwrap(response) || {};
                this.result.title = `${fn.functionName} · ${data.success ? '执行成功' : '执行失败'}`;
                this.result.htmlMode = data.success === true && typeof data.data === 'string';
                this.result.html = this.result.htmlMode
                    ? NeuCharPivotUi.sanitizeHtml(data.data)
                    : '';
                this.result.content = this.result.htmlMode
                    ? ''
                    : (data.success
                        ? JSON.stringify(data.data, null, 2)
                        : (data.errorMessage || '执行失败'));
                this.result.visible = true;
            } catch (error) {
                this.$notify({ title: '执行失败', message: '请求失败或模块已不可用。', type: 'error' });
            } finally {
                this.loading = false;
            }
        }
    }
});
