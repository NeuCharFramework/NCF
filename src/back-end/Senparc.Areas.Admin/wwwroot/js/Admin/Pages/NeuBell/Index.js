/*----------------------------------------------------------------
    文件名：Index.js
    文件功能描述：NeuBell WebHook（WebAPI）设置页：
    通知端点 CRUD（含请求方式/请求体模板/占位符）、启用开关、测试发送、请求日志（请求数据与结果）
----------------------------------------------------------------*/
// 统一响应解包
window.NeuBellWebHookUi = {
    unwrap(response) {
        const body = response && response.data;
        if (body && body.success && 'data' in body) {
            return body.data;
        }
        return null;
    }
};

new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            items: [],
            providers: [],
            keyword: '',
            formDialogVisible: false,
            formSaving: false,
            form: {
                id: 0, name: '', webHookUrl: '', httpMethod: 'POST', bodyTemplate: '',
                providerFilter: '', secret: '',
                notifyOnAdd: true, notifyOnRemove: true, isEnabled: true, showSecret: false
            },
            logRows: [],
            logLoading: false,
            payloadVisible: false,
            payloadLoading: false,
            payloadText: '',
            payloadRow: null
        };
    },
    computed: {
        filteredItems() {
            const keyword = (this.keyword || '').trim().toLowerCase();
            return this.items.filter(item => !keyword
                || [item.name, item.webHookUrl, item.httpMethod, item.providerFilter]
                    .some(value => String(value || '').toLowerCase().includes(keyword)));
        }
    },
    created() {
        this.load();
        this.loadLogs();
    },
    methods: {
        async load() {
            this.loading = true;
            try {
                const itemsResponse = await service.get('/Admin/NeuBell/Index?handler=List');
                const providersResponse = await service.get('/Admin/NeuBell/Index?handler=Providers');
                const items = NeuBellWebHookUi.unwrap(itemsResponse);
                this.items = (Array.isArray(items) ? items : []).map(item => ({
                    ...item,
                    testing: false
                }));
                const providers = NeuBellWebHookUi.unwrap(providersResponse);
                this.providers = Array.isArray(providers) ? providers : [];
            } finally {
                this.loading = false;
            }
        },
        openCreate() {
            this.form = {
                id: 0, name: '', webHookUrl: '', httpMethod: 'POST', bodyTemplate: '',
                providerFilter: '', secret: '',
                notifyOnAdd: true, notifyOnRemove: true, isEnabled: true, showSecret: false
            };
            this.formDialogVisible = true;
        },
        openEdit(row) {
            this.form = {
                id: row.id,
                name: row.name || '',
                webHookUrl: row.webHookUrl || '',
                httpMethod: row.httpMethod || 'POST',
                bodyTemplate: row.bodyTemplate || '',
                providerFilter: row.providerFilter || '',
                secret: '',
                notifyOnAdd: !!row.notifyOnAdd,
                notifyOnRemove: !!row.notifyOnRemove,
                isEnabled: !!row.isEnabled,
                showSecret: false
            };
            this.formDialogVisible = true;
        },
        async saveRow() {
            if (!this.form.name || !String(this.form.name).trim()) {
                this.$message.warning('名称不能为空');
                return;
            }
            if (!this.form.webHookUrl || !String(this.form.webHookUrl).trim()) {
                this.$message.warning('WebHook 地址不能为空');
                return;
            }
            const url = String(this.form.webHookUrl).trim();
            if (!/^https?:\/\//i.test(url)) {
                this.$message.warning('WebHook 地址必须是合法的 http/https 地址（支持 {{占位符}}）');
                return;
            }
            const method = String(this.form.httpMethod || 'POST').trim().toUpperCase();
            if (!['POST', 'GET', 'PUT'].includes(method)) {
                this.$message.warning('请求方式必须是 GET、POST 或 PUT');
                return;
            }
            if (String(this.form.bodyTemplate || '').length > 20000) {
                this.$message.warning('请求体模板过长（最多 20000 个字符）');
                return;
            }
            if (!this.form.notifyOnAdd && !this.form.notifyOnRemove) {
                this.$message.warning('请至少开启“新增时通知”或“移除时通知”');
                return;
            }
            this.formSaving = true;
            try {
                const response = await service.post('/Admin/NeuBell/Index?handler=Save', this.form, { customAlert: true });
                const body = response && response.data;
                if (body && body.success) {
                    this.$message.success('已保存');
                    this.formDialogVisible = false;
                    await this.load();
                } else {
                    this.$message.error((body && body.msg) || '保存失败');
                }
            } finally {
                this.formSaving = false;
            }
        },
        async toggleEnabled(row) {
            const response = await service.post('/Admin/NeuBell/Index?handler=Save', {
                id: row.id,
                name: row.name,
                webHookUrl: row.webHookUrl,
                httpMethod: row.httpMethod || 'POST',
                bodyTemplate: row.bodyTemplate || '',
                providerFilter: row.providerFilter,
                secret: '',
                notifyOnAdd: row.notifyOnAdd,
                notifyOnRemove: row.notifyOnRemove,
                isEnabled: row.isEnabled
            }, { customAlert: true });
            const body = response && response.data;
            if (!body || !body.success) {
                row.isEnabled = !row.isEnabled;
            }
        },
        async sendTest(row) {
            row.testing = true;
            try {
                const response = await service.post('/Admin/NeuBell/Index?handler=Test', { id: row.id }, { customAlert: true });
                const body = response && response.data;
                const data = body && body.data;
                if (body && body.success && data && data.success) {
                    this.$message.success('测试成功：' + (data.message || ''));
                } else {
                    this.$message.error('测试失败：' + ((data && data.message) || (body && body.msg) || '未知错误'));
                }
            } finally {
                row.testing = false;
            }
        },
        async removeRow(row) {
            try {
                await this.$confirm('确定删除 WebHook“' + (row.name || row.id) + '”？删除后将不再向该地址发送通知。', '删除 WebHook', { type: 'warning' });
            } catch (error) {
                return;
            }
            const response = await service.post('/Admin/NeuBell/Index?handler=Delete', { id: row.id });
            if (response && response.data && response.data.success) {
                this.$message.success('已删除');
                await this.load();
            }
        },
        async loadLogs() {
            this.logLoading = true;
            try {
                const response = await service.get('/Admin/NeuBell/Index?handler=LogList&take=200');
                const rows = NeuBellWebHookUi.unwrap(response);
                this.logRows = Array.isArray(rows) ? rows : [];
            } finally {
                this.logLoading = false;
            }
        },
        kindLabel(kind) {
            switch (kind) {
                case 'item-created': return '创建通知';
                case 'items-changed': return '变更通知';
                case 'test': return '测试';
                default: return kind || '—';
            }
        },
        kindTagType(kind) {
            switch (kind) {
                case 'item-created': return 'primary';
                case 'items-changed': return 'warning';
                case 'test': return 'info';
                default: return 'info';
            }
        },
        statusLabel(status) {
            switch (status) {
                case 'sending': return '发送中';
                case 'success': return '成功';
                case 'failed': return '失败';
                default: return status || '—';
            }
        },
        statusTagType(status) {
            switch (status) {
                case 'sending': return 'info';
                case 'success': return 'success';
                case 'failed': return 'danger';
                default: return 'info';
            }
        },
        formatTime(value) {
            if (!value) {
                return '—';
            }
            const date = new Date(value);
            if (isNaN(date.getTime())) {
                return String(value);
            }
            const pad = n => String(n).padStart(2, '0');
            return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate())
                + ' ' + pad(date.getHours()) + ':' + pad(date.getMinutes()) + ':' + pad(date.getSeconds());
        },
        async showPayload(row) {
            this.payloadRow = row;
            this.payloadText = '加载中…';
            this.payloadVisible = true;
            this.payloadLoading = true;
            try {
                const response = await service.get('/Admin/NeuBell/Index?handler=LogPayload&id=' + row.id);
                const body = response && response.data;
                const data = body && body.data;
                let text = (data && data.payload) || '（无报文）';
                try {
                    text = JSON.stringify(JSON.parse(text), null, 2);
                } catch (error) {
                    // 非 JSON 时原样展示
                }
                this.payloadText = text;
            } finally {
                this.payloadLoading = false;
            }
        },
        async removeLog(row) {
            try {
                await this.$confirm('确定删除这条请求日志？', '删除日志', { type: 'warning' });
            } catch (error) {
                return;
            }
            const response = await service.post('/Admin/NeuBell/Index?handler=DeleteLog', { id: row.id });
            if (response && response.data && response.data.success) {
                this.$message.success('已删除');
                await this.loadLogs();
            }
        },
        async clearLogs() {
            try {
                await this.$confirm('确定清空请求日志？将保留最近 50 条。', '清空日志', { type: 'warning' });
            } catch (error) {
                return;
            }
            const response = await service.post('/Admin/NeuBell/Index?handler=ClearLogs', { keep: 50 });
            const body = response && response.data;
            if (body && body.success) {
                this.$message.success('已清空' + ((body.data && body.data.removed != null) ? '（删除 ' + body.data.removed + ' 条）' : ''));
                await this.loadLogs();
            }
        }
    }
});

