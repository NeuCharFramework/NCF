/*----------------------------------------------------------------
    文件名：Access.js
    文件功能描述：Function 全局 Provit 访问控制页：
    代码基线 + 数据库策略 + 生效策略的完整展示、单条/批量设置与清除

    创建标识：Senparc - 20260917
    修改描述：v0.9.1 新增 Function 全局 Provit 数据库访问策略映射
----------------------------------------------------------------*/
new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            saving: false,
            batchSaving: false,
            items: [],
            policyCount: 0,
            orphanCount: 0,
            users: [],
            roles: [],
            keyword: '',
            selection: [],
            editVisible: false,
            editing: null,
            form: { accessMode: 0, roleCodes: [], permissionCodes: [], userIds: [], remark: '' },
            batchRestrictVisible: false,
            batchForm: { roleCodes: [], permissionCodes: [], userIds: [], remark: '' }
        };
    },
    computed: {
        filteredItems() {
            const keyword = this.keyword.trim().toLowerCase();
            if (!keyword) {
                return this.items;
            }
            return this.items.filter(item =>
                [item.moduleUid, item.moduleName, item.functionKey, item.functionName]
                    .some(value => String(value || '').toLowerCase().includes(keyword)));
        }
    },
    created() {
        this.load();
    },
    methods: {
        async load() {
            this.loading = true;
            try {
                const [listResponse, usersResponse, rolesResponse] = await Promise.all([
                    service.get('/Admin/NeuCharPivot/Access?handler=List'),
                    service.get('/Admin/NeuCharPivot/Access?handler=Users'),
                    service.get('/Admin/NeuCharPivot/Access?handler=Roles')
                ]);
                const list = NeuCharPivotUi.unwrap(listResponse) || {};
                this.items = (Array.isArray(list.items) ? list.items : []).map(item => ({
                    ...item,
                    rowKey: item.moduleUid + '|' + item.functionKey
                }));
                this.policyCount = list.policyCount || 0;
                this.orphanCount = list.orphanCount || 0;
                this.users = NeuCharPivotUi.unwrap(usersResponse) || [];
                this.roles = NeuCharPivotUi.unwrap(rolesResponse) || [];
            } finally {
                this.loading = false;
            }
        },
        onSelectionChange(rows) {
            this.selection = rows;
        },
        modeLabel(mode) {
            return { 0: '继承代码', 1: '开放', 2: '受限', 3: '禁用' }[mode] || '未知';
        },
        modeType(mode) {
            return { 0: 'info', 1: 'success', 2: 'warning', 3: 'danger' }[mode] || 'info';
        },
        modeHint(mode) {
            return {
                0: '不使用数据库策略，完全按代码属性（AllowGlobalPivot / GlobalPivotRoleCodes / GlobalPivotPermissionCodes）执行。',
                1: '任意已登录后台管理员均可通过全局 Provit 访问该 Function（覆盖代码约束，请谨慎）。',
                2: '仅下方绑定的用户 / 角色 / 权限码（任一命中）可访问，其余账号一律拒绝。',
                3: '显式拒绝所有全局 Provit 访问，即使代码属性声明允许。'
            }[mode] || '';
        },
        effectiveLabel(row) {
            if (row.orphan) {
                return '待重装生效';
            }
            const mode = row.effective.effectiveMode;
            const label = { open: '开放', restricted: '受限', deny: '禁用' }[mode] || mode;
            return row.effective.effectiveSource === 'db' ? label + '（DB）' : label;
        },
        effectiveType(row) {
            if (row.orphan) {
                return 'warning';
            }
            const mode = row.effective.effectiveMode;
            return { open: 'success', restricted: 'warning', deny: 'danger' }[mode] || 'info';
        },
        userLabel(uid) {
            const user = this.users.find(z => z.id === uid);
            return user ? (user.userName || user.realName || ('#' + uid)) : ('#' + uid);
        },
        userLabelByUser(user) {
            const name = user.userName || '';
            return user.realName && user.realName !== name ? `${name}（${user.realName}）` : name;
        },
        formatTime(value) {
            if (!value) {
                return '';
            }
            const date = new Date(value);
            if (Number.isNaN(date.getTime())) {
                return String(value);
            }
            const pad = n => (n < 10 ? '0' + n : '' + n);
            return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
        },
        openEdit(row) {
            this.editing = row;
            this.form = {
                accessMode: row.db ? row.db.accessMode : 0,
                roleCodes: row.db ? [...(row.db.roleCodes || [])] : [],
                permissionCodes: row.db ? [...(row.db.permissionCodes || [])] : [],
                userIds: row.db ? [...(row.db.userIds || [])] : [],
                remark: row.db ? (row.db.remark || '') : ''
            };
            this.editVisible = true;
        },
        async savePolicy() {
            if (this.form.accessMode === 2 &&
                !(this.form.roleCodes || []).length &&
                !(this.form.permissionCodes || []).length &&
                !(this.form.userIds || []).length) {
                this.$message.warning('受限策略必须至少绑定一个用户、角色或权限码');
                return;
            }
            this.saving = true;
            try {
                const response = await service.post('/Admin/NeuCharPivot/Access?handler=Save', {
                    moduleUid: this.editing.moduleUid,
                    functionKey: this.editing.functionKey,
                    accessMode: this.form.accessMode,
                    allowedRoleCodes: this.form.accessMode === 2 ? this.form.roleCodes : [],
                    allowedPermissionCodes: this.form.accessMode === 2 ? this.form.permissionCodes : [],
                    allowedUserIds: this.form.accessMode === 2 ? this.form.userIds : [],
                    remark: this.form.remark
                }, { customAlert: true });
                const body = response && response.data;
                if (body && body.success !== false) {
                    this.$message.success('策略已保存');
                    this.editVisible = false;
                    await this.load();
                } else {
                    this.$message.error((body && body.msg) || '保存失败');
                }
            } catch (error) {
                console.warn('savePolicy failed:', error);
            } finally {
                this.saving = false;
            }
        },
        async clearPolicy(row) {
            try {
                await this.$confirm(
                    `确定清除 ${row.moduleUid} / ${row.functionKey} 的数据库策略？清除后恢复为按代码基线执行。`,
                    '清除策略',
                    { type: 'warning', confirmButtonText: '清除', cancelButtonText: '取消' });
            } catch (error) {
                return;
            }
            try {
                const response = await service.post('/Admin/NeuCharPivot/Access?handler=Clear', {
                    moduleUid: row.moduleUid,
                    functionKey: row.functionKey
                }, { customAlert: true });
                const body = response && response.data;
                if (body && body.success !== false) {
                    this.$message.success('策略已清除');
                    await this.load();
                } else {
                    this.$message.error((body && body.msg) || '清除失败');
                }
            } catch (error) {
                console.warn('clearPolicy failed:', error);
            }
        },
        openBatchRestrict() {
            this.batchForm = { roleCodes: [], permissionCodes: [], userIds: [], remark: '' };
            this.batchRestrictVisible = true;
        },
        async batchApply(operation, fromDialog) {
            if (!this.selection.length) {
                return;
            }
            if (operation === 'apply-restrict') {
                if (!(this.batchForm.roleCodes || []).length &&
                    !(this.batchForm.permissionCodes || []).length &&
                    !(this.batchForm.userIds || []).length) {
                    this.$message.warning('受限策略必须至少绑定一个用户、角色或权限码');
                    return;
                }
            }
            if (operation !== 'apply-restrict' || fromDialog !== true) {
                const messages = {
                    'apply-open': `将选中的 ${this.selection.length} 个 Function 全部“开放”（任意登录管理员可访问）？`,
                    'apply-deny': `将选中的 ${this.selection.length} 个 Function 全部“禁用”（全局 Provit 一律拒绝）？`,
                    'apply-inherit': `将选中的 ${this.selection.length} 个 Function 恢复为“继承代码”（数据库策略不再覆盖代码基线）？`,
                    'clear': `清除选中的 ${this.selection.length} 个 Function 的数据库策略（恢复代码基线）？此操作不可恢复。`
                };
                try {
                    await this.$confirm(messages[operation] || '确定执行？', '批量操作', {
                        type: operation === 'clear' || operation === 'apply-deny' ? 'warning' : 'info',
                        confirmButtonText: '确定',
                        cancelButtonText: '取消'
                    });
                } catch (error) {
                    return;
                }
            }
            this.batchSaving = true;
            try {
                const response = await service.post('/Admin/NeuCharPivot/Access?handler=Batch', {
                    operation: operation,
                    items: this.selection.map(row => ({
                        moduleUid: row.moduleUid,
                        functionKey: row.functionKey
                    })),
                    allowedRoleCodes: operation === 'apply-restrict' ? this.batchForm.roleCodes : [],
                    allowedPermissionCodes: operation === 'apply-restrict' ? this.batchForm.permissionCodes : [],
                    allowedUserIds: operation === 'apply-restrict' ? this.batchForm.userIds : [],
                    remark: operation === 'apply-restrict' ? this.batchForm.remark : ''
                }, { customAlert: true });
                const body = response && response.data;
                if (body && body.success !== false) {
                    const affected = body.data && typeof body.data.affected === 'number' ? body.data.affected : this.selection.length;
                    this.$message.success(`批量操作完成，影响 ${affected} 条策略`);
                    this.batchRestrictVisible = false;
                    await this.load();
                } else {
                    this.$message.error((body && body.msg) || '批量操作失败');
                }
            } catch (error) {
                console.warn('batchApply failed:', error);
            } finally {
                this.batchSaving = false;
            }
        }
    }
});
