/*----------------------------------------------------------------
    文件名：Board.js
    文件功能描述：Pivot 面板（Provit Panel）管理页：
    面板 CRUD、Provit Block 增删与拖动排序、AI Chat 创建或修改面板
----------------------------------------------------------------*/
new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            boards: [],
            catalog: [],
            keyword: '',
            dragState: null,
            formDialogVisible: false,
            formSaving: false,
            form: { id: 0, name: '', pageKey: '', description: '', columns: 2, isEnabled: true },
            pickerVisible: false,
            pickerBoardId: 0,
            picker: { moduleUid: '', functionKey: '', title: '', summary: '', accent: 'blue' }
        };
    },
    computed: {
        filteredBoards() {
            const keyword = this.keyword.trim().toLowerCase();
            return this.boards.filter(board => !keyword
                || [board.name, board.pageKey, board.description]
                    .some(value => String(value || '').toLowerCase().includes(keyword)));
        },
        groupedBoards() {
            const groups = {};
            this.filteredBoards.forEach(board => {
                if (!groups[board.pageKey]) {
                    groups[board.pageKey] = { pageKey: board.pageKey, boards: [] };
                }
                groups[board.pageKey].boards.push(board);
            });
            return Object.keys(groups).map(pageKey => groups[pageKey])
                .sort((left, right) => left.pageKey.localeCompare(right.pageKey));
        },
        pickerFunctions() {
            const group = this.catalog.find(z => z.moduleUid === this.picker.moduleUid);
            return group ? group.functions : [];
        },
        pickerFunction() {
            return this.pickerFunctions.find(z => z.functionKey === this.picker.functionKey) || null;
        }
    },
    created() {
        this.load();
    },
    methods: {
        async load() {
            this.loading = true;
            try {
                const boardsResponse = await service.get('/Admin/NeuCharPivot/Board?handler=Boards');
                const catalogResponse = await service.get('/Admin/NeuCharPivot/Board?handler=Functions');
                const boards = NeuCharPivotUi.unwrap(boardsResponse) || [];
                this.boards = (Array.isArray(boards) ? boards : []).map(board => ({
                    ...board,
                    blocks: Array.isArray(board.blocks) ? board.blocks : [],
                    aiVisible: false,
                    aiLoading: false,
                    aiInstruction: '',
                    aiReply: ''
                }));
                this.catalog = NeuCharPivotUi.unwrap(catalogResponse) || [];
            } finally {
                this.loading = false;
            }
        },
        moduleLabel(moduleUid) {
            const group = this.catalog.find(z => z.moduleUid === moduleUid);
            return group ? group.moduleName : moduleUid;
        },
        openCreate() {
            this.form = { id: 0, name: '', pageKey: '', description: '', columns: 2, isEnabled: true };
            this.formDialogVisible = true;
        },
        openEdit(board) {
            this.form = {
                id: board.id,
                name: board.name,
                pageKey: board.pageKey,
                description: board.description || '',
                columns: board.columns || 2,
                isEnabled: !!board.isEnabled
            };
            this.formDialogVisible = true;
        },
        async saveBoard() {
            if (!this.form.name || !String(this.form.name).trim()) {
                this.$message.warning('面板名称不能为空');
                return;
            }
            if (!this.form.pageKey || !String(this.form.pageKey).trim()) {
                this.$message.warning('页面标识不能为空');
                return;
            }
            this.formSaving = true;
            try {
                const handler = this.form.id > 0 ? 'Update' : 'Create';
                const response = await service.post('/Admin/NeuCharPivot/Board?handler=' + handler, this.form, { customAlert: true });
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
        async removeBoard(board) {
            try {
                await this.$confirm('确定删除面板“' + (board.name || board.id) + '”？此操作不可恢复。', '删除面板', { type: 'warning' });
            } catch (error) {
                return;
            }
            const response = await service.post('/Admin/NeuCharPivot/Board?handler=Delete', { id: board.id });
            if (response && response.data && response.data.success) {
                this.$message.success('已删除');
                await this.load();
            }
        },
        async toggleEnabled(board) {
            const response = await service.post('/Admin/NeuCharPivot/Board?handler=Update', {
                id: board.id,
                name: board.name,
                pageKey: board.pageKey,
                description: board.description,
                columns: board.columns,
                isEnabled: board.isEnabled
            }, { customAlert: true });
            if (!response || !response.data || !response.data.success) {
                board.isEnabled = !board.isEnabled;
            }
        },
        async saveBlocks(board) {
            const response = await service.post('/Admin/NeuCharPivot/Board?handler=SetBlocks', {
                boardId: board.id,
                blocks: board.blocks
            }, { customAlert: true });
            const body = response && response.data;
            if (body && body.success) {
                const updated = body.data;
                if (updated && Array.isArray(updated.blocks)) {
                    board.blocks = updated.blocks;
                }
                return true;
            }
            this.$message.error((body && body.msg) || '块保存失败');
            this.load();
            return false;
        },
        async moveBlock(board, index, delta) {
            const target = index + delta;
            if (target < 0 || target >= board.blocks.length) {
                return;
            }
            const blocks = board.blocks.slice();
            const moved = blocks.splice(index, 1)[0];
            blocks.splice(target, 0, moved);
            board.blocks = blocks;
            await this.saveBlocks(board);
        },
        async removeBlock(board, index) {
            board.blocks.splice(index, 1);
            await this.saveBlocks(board);
        },
        openBlockPicker(board) {
            this.pickerBoardId = board.id;
            this.picker = { moduleUid: '', functionKey: '', title: '', summary: '', accent: 'blue' };
            this.pickerVisible = true;
        },
        onPickModule() {
            this.picker.functionKey = '';
        },
        async addBlock() {
            const board = this.boards.find(z => z.id === this.pickerBoardId);
            const fn = this.pickerFunction;
            if (!board || !fn) {
                return;
            }
            board.blocks.push({
                key: '',
                moduleUid: this.picker.moduleUid,
                functionKey: this.picker.functionKey,
                functionName: fn.functionName || this.picker.functionKey,
                title: (this.picker.title || '').trim() || fn.functionName || this.picker.functionKey,
                summary: (this.picker.summary || '').trim(),
                accent: this.picker.accent,
                exposedParameters: []
            });
            const ok = await this.saveBlocks(board);
            if (ok) {
                this.pickerVisible = false;
            }
        },
        dragStart(board, index) {
            this.dragState = { boardId: board.id, index: index };
        },
        dragEnd() {
            this.dragState = null;
        },
        async dropBlock(board, targetIndex) {
            if (!this.dragState || this.dragState.boardId !== board.id) {
                return;
            }
            const from = this.dragState.index;
            this.dragState = null;
            if (from === targetIndex) {
                return;
            }
            const blocks = board.blocks.slice();
            const moved = blocks.splice(from, 1)[0];
            blocks.splice(targetIndex, 0, moved);
            board.blocks = blocks;
            await this.saveBlocks(board);
        },
        async sendAi(board) {
            const instruction = (board.aiInstruction || '').trim();
            if (!instruction) {
                this.$message.warning('请输入面板需求描述');
                return;
            }
            board.aiLoading = true;
            board.aiReply = '';
            const previousInstruction = instruction;
            try {
                const response = await service.post('/Admin/NeuCharPivot/Board?handler=Ai', {
                    boardId: board.id,
                    instruction: instruction
                }, { customAlert: true });
                const body = response && response.data;
                const aiReply = body && body.data && body.data.aiReply ? body.data.aiReply : '';
                if (body && body.success) {
                    this.$message.success('AI 已更新面板');
                    await this.load();
                    const refreshed = this.boards.find(z => z.id === board.id);
                    if (refreshed) {
                        refreshed.aiVisible = true;
                        refreshed.aiInstruction = previousInstruction;
                        refreshed.aiReply = aiReply;
                    }
                } else {
                    board.aiReply = aiReply;
                    this.$message.error((body && body.msg) || 'AI 处理失败');
                }
            } finally {
                board.aiLoading = false;
            }
        }
    }
});
