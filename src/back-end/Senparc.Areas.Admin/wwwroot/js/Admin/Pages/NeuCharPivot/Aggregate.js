new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            modules: [],
            inputs: {},
            keyword: '',
            openedModules: [],
            result: { visible: false, title: '', content: '', html: '', htmlMode: false }
        };
    },
    computed: {
        filteredModules() {
            const keyword = this.keyword.trim().toLowerCase();
            if (!keyword) return this.modules;
            return this.modules.map(module => ({
                ...module,
                functions: module.functions.filter(fn =>
                    String(fn.functionName || '').toLowerCase().includes(keyword) ||
                    String(fn.description || '').toLowerCase().includes(keyword))
            })).filter(module =>
                String(module.configuration.name || '').toLowerCase().includes(keyword) || module.functions.length);
        }
    },
    created() { this.load(); },
    methods: {
        async load() {
            this.loading = true;
            try {
                const response = await service.get('/Admin/NeuCharPivot/Aggregate?handler=List');
                this.modules = NeuCharPivotUi.unwrap(response) || [];
                const inputs = {};
                this.modules.forEach(module => module.functions.forEach(fn => {
                    inputs[fn.id] = NeuCharPivotUi.createParameterValues(fn);
                }));
                this.inputs = inputs;
                this.openedModules = this.modules.slice(0, 3).map(module => module.configuration.moduleUid);
            } finally {
                this.loading = false;
            }
        },
        getParameters(fn) { return NeuCharPivotUi.parseJson(fn.parameterSchemaJson, []); },
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
