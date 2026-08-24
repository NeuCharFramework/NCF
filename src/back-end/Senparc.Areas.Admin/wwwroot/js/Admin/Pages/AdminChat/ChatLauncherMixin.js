window.ChatLauncherMixin = {
  data() {
    return {
      moduleStorageKey: 'ncf.admin.chat.selectedModuleUids',
      workflowStorageKey: 'ncf.admin.chat.selectedWorkflowIds',
      aiModelStorageKey: 'ncf.admin.chat.sessionAiModelMap',
      chatInputText: '',
      launcherAiModelId: 0,
      sessionAiModelMap: {},
      selectedModules: [],
      moduleSelectorVisible: false,
      moduleSelectorTab: 'modules',
      moduleSearchKeyword: '',
      workflowSearchKeyword: '',
      activePreviewModuleUid: '',
      activePreviewWorkflowId: 0,
      availableModules: [],
      selectedModuleUids: [],
      availableWorkflows: [],
      selectedWorkflowIds: [],
      selectedWorkflows: [],
      loadingModuleOptions: false,
      loadingWorkflowOptions: false,
      isCreatingSession: false
    };
  },
  computed: {
    filteredAvailableModules() {
      const keyword = (this.moduleSearchKeyword || '').trim().toLowerCase();
      if (!keyword) {
        return this.availableModules;
      }

      return this.availableModules.filter((item) => {
        return (
          (item.name || '').toLowerCase().includes(keyword) ||
          (item.description || '').toLowerCase().includes(keyword) ||
          (item.uid || '').toLowerCase().includes(keyword)
        );
      });
    },
    sortedFilteredAvailableModules() {
      const selectedUidSet = new Set(this.selectedModuleUids);
      return this.filteredAvailableModules.slice().sort((a, b) => {
        const aSelected = selectedUidSet.has(a.uid) ? 0 : 1;
        const bSelected = selectedUidSet.has(b.uid) ? 0 : 1;
        if (aSelected !== bSelected) {
          return aSelected - bSelected;
        }

        return (a.name || '').localeCompare(b.name || '', 'zh-Hans-CN');
      });
    },
    previewModule() {
      if (!this.activePreviewModuleUid) {
        return null;
      }

      return this.availableModules.find((item) => item.uid === this.activePreviewModuleUid) || null;
    },
    isAllFilteredSelected() {
      if (this.filteredAvailableModules.length === 0) {
        return false;
      }

      return this.filteredAvailableModules.every((item) => this.selectedModuleUids.includes(item.uid));
    },
    moduleSelectionIndeterminate() {
      if (this.filteredAvailableModules.length === 0) {
        return false;
      }

      const selectedCount = this.filteredAvailableModules.filter((item) => this.selectedModuleUids.includes(item.uid)).length;
      return selectedCount > 0 && selectedCount < this.filteredAvailableModules.length;
    },
    filteredAvailableWorkflows() {
      const keyword = (this.workflowSearchKeyword || '').trim().toLowerCase();
      if (!keyword) {
        return this.availableWorkflows;
      }

      return this.availableWorkflows.filter((item) => {
        return (
          (item.name || '').toLowerCase().includes(keyword) ||
          (item.description || '').toLowerCase().includes(keyword) ||
          (item.parameters || []).some((parameter) =>
            (parameter.name || '').toLowerCase().includes(keyword) ||
            (parameter.description || '').toLowerCase().includes(keyword))
        );
      });
    },
    sortedFilteredAvailableWorkflows() {
      const selectedIdSet = new Set(this.selectedWorkflowIds);
      return this.filteredAvailableWorkflows.slice().sort((a, b) => {
        const aSelected = selectedIdSet.has(a.id) ? 0 : 1;
        const bSelected = selectedIdSet.has(b.id) ? 0 : 1;
        if (aSelected !== bSelected) {
          return aSelected - bSelected;
        }
        return (a.name || '').localeCompare(b.name || '', 'zh-Hans-CN');
      });
    },
    previewWorkflow() {
      if (!this.activePreviewWorkflowId) {
        return null;
      }
      return this.availableWorkflows.find((item) => item.id === this.activePreviewWorkflowId) || null;
    },
    isAllFilteredWorkflowsSelected() {
      if (this.filteredAvailableWorkflows.length === 0) {
        return false;
      }
      return this.filteredAvailableWorkflows.every((item) => this.selectedWorkflowIds.includes(item.id));
    },
    workflowSelectionIndeterminate() {
      if (this.filteredAvailableWorkflows.length === 0) {
        return false;
      }
      const selectedCount = this.filteredAvailableWorkflows
        .filter((item) => this.selectedWorkflowIds.includes(item.id))
        .length;
      return selectedCount > 0 && selectedCount < this.filteredAvailableWorkflows.length;
    }
  },
  mounted() {
    this.restoreSelectedModuleUids();
    this.restoreSelectedWorkflowIds();
    this.restoreSessionAiModelMap();
    this.ensureModuleOptionsLoaded(false);
  },
  watch: {
    selectedModules: {
      deep: true,
      handler(value) {
        const selectedUids = (value || []).map((item) => item.uid);
        this.persistSelectedModuleUids(selectedUids);
      }
    },
    selectedWorkflows: {
      deep: true,
      handler(value) {
        this.persistSelectedWorkflowIds((value || []).map((item) => item.id));
      }
    },
    sortedFilteredAvailableModules: {
      deep: true,
      handler() {
        this.ensurePreviewModule();
      }
    },
    sortedFilteredAvailableWorkflows: {
      deep: true,
      handler() {
        this.ensurePreviewWorkflow();
      }
    }
  },
  methods: {
    persistSelectedModuleUids(uids) {
      try {
        localStorage.setItem(this.moduleStorageKey, JSON.stringify(uids || []));
      } catch (error) {
        console.warn('保存已选模块失败:', error);
      }
    },

    restoreSelectedModuleUids() {
      try {
        const raw = localStorage.getItem(this.moduleStorageKey);
        if (!raw) {
          return;
        }

        const uids = JSON.parse(raw);
        if (!Array.isArray(uids)) {
          return;
        }

        this.selectedModuleUids = uids.filter((uid) => typeof uid === 'string' && uid.length > 0);
      } catch (error) {
        console.warn('读取已选模块失败:', error);
      }
    },

    persistSelectedWorkflowIds(ids) {
      try {
        localStorage.setItem(this.workflowStorageKey, JSON.stringify(ids || []));
      } catch (error) {
        console.warn('保存已选 Workflow 失败:', error);
      }
    },

    restoreSelectedWorkflowIds() {
      try {
        const raw = localStorage.getItem(this.workflowStorageKey);
        if (!raw) {
          return;
        }
        const ids = JSON.parse(raw);
        if (!Array.isArray(ids)) {
          return;
        }
        this.selectedWorkflowIds = ids
          .map((id) => Number.parseInt(id, 10))
          .filter((id) => Number.isInteger(id) && id > 0);
      } catch (error) {
        console.warn('读取已选 Workflow 失败:', error);
      }
    },

    normalizeAiModelId(value) {
      const parsedValue = Number.parseInt(value, 10);
      return Number.isInteger(parsedValue) && parsedValue > 0 ? parsedValue : 0;
    },

    persistSessionAiModelMap() {
      try {
        localStorage.setItem(this.aiModelStorageKey, JSON.stringify(this.sessionAiModelMap || {}));
      } catch (error) {
        console.warn('保存会话 AI 模型失败:', error);
      }
    },

    restoreSessionAiModelMap() {
      try {
        const raw = localStorage.getItem(this.aiModelStorageKey);
        if (!raw) {
          return;
        }

        const parsed = JSON.parse(raw);
        if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
          return;
        }

        this.sessionAiModelMap = Object.keys(parsed).reduce((result, key) => {
          result[key] = this.normalizeAiModelId(parsed[key]);
          return result;
        }, {});
      } catch (error) {
        console.warn('读取会话 AI 模型失败:', error);
      }
    },

    setSessionAiModelId(sessionId, aiModelId) {
      if (!sessionId) {
        return;
      }

      this.$set(this.sessionAiModelMap, String(sessionId), this.normalizeAiModelId(aiModelId));
      this.persistSessionAiModelMap();
    },

    getSessionAiModelId(sessionId) {
      if (!sessionId) {
        return 0;
      }

      return this.normalizeAiModelId(this.sessionAiModelMap[String(sessionId)]);
    },

    syncSelectedModulesFromUids() {
      const uidSet = new Set(this.selectedModuleUids);
      this.selectedModules = this.availableModules.filter((item) => uidSet.has(item.uid));
    },

    syncSelectedWorkflowsFromIds() {
      const idSet = new Set(this.selectedWorkflowIds);
      this.selectedWorkflows = this.availableWorkflows.filter((item) => idSet.has(item.id));
    },

    setPreviewModule(uid) {
      this.activePreviewModuleUid = uid;
    },

    ensurePreviewModule() {
      if (this.sortedFilteredAvailableModules.length === 0) {
        this.activePreviewModuleUid = '';
        return;
      }

      const exists = this.sortedFilteredAvailableModules.some((item) => item.uid === this.activePreviewModuleUid);
      if (!exists) {
        this.activePreviewModuleUid = this.sortedFilteredAvailableModules[0].uid;
      }
    },

    setPreviewWorkflow(id) {
      this.activePreviewWorkflowId = id;
    },

    ensurePreviewWorkflow() {
      if (this.sortedFilteredAvailableWorkflows.length === 0) {
        this.activePreviewWorkflowId = 0;
        return;
      }
      const exists = this.sortedFilteredAvailableWorkflows.some((item) => item.id === this.activePreviewWorkflowId);
      if (!exists) {
        this.activePreviewWorkflowId = this.sortedFilteredAvailableWorkflows[0].id;
      }
    },

    handleLauncherInputKeydown(event) {
      if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
        event.preventDefault();
        this.startChatSession();
      }
    },

    normalizeModuleItem(item) {
      return {
        uid: item.uid,
        name: item.menuName || item.name || ncfT('AdminChat.UnknownModule'),
        icon: item.icon || 'fa fa-cube',
        description: item.description || ncfT('Admin.Home.NoDescription'),
        version: item.version || '',
        menus: item.menus || [],
        functions: item.functions || []
      };
    },

    normalizeWorkflowItem(item) {
      return {
        id: Number.parseInt(item.id, 10),
        name: item.name || `Workflow #${item.id}`,
        description: item.description || '',
        parameters: item.parameters || []
      };
    },

    async ensureModuleOptionsLoaded(forceReload) {
      if (!forceReload && this.availableModules.length > 0) {
        return;
      }

      this.loadingModuleOptions = true;
      this.loadingWorkflowOptions = true;
      try {
        const [moduleResult, workflowResult] = await Promise.all([
          service.get('/Admin/Index?handler=XncfOpening'),
          service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetAvailableWorkflowsAsync')
        ]);
        const moduleList = (moduleResult.data && moduleResult.data.data) ? moduleResult.data.data : [];
        this.availableModules = moduleList.map((item) => this.normalizeModuleItem(item));
        const workflowList = workflowResult.data && workflowResult.data.success && workflowResult.data.data
          ? workflowResult.data.data.workflows || []
          : [];
        this.availableWorkflows = workflowList.map((item) => this.normalizeWorkflowItem(item));
        this.syncSelectedModulesFromUids();
        this.syncSelectedWorkflowsFromIds();
        this.ensurePreviewModule();
        this.ensurePreviewWorkflow();
      } catch (error) {
        console.error('加载模块列表失败:', error);
        this.$message.error(ncfT('AdminChat.LoadModulesFailedRetry'));
      } finally {
        this.loadingModuleOptions = false;
        this.loadingWorkflowOptions = false;
      }
    },

    async openModuleSelector() {
      await this.ensureModuleOptionsLoaded(false);
      this.selectedModuleUids = this.selectedModules.map((item) => item.uid);
      this.selectedWorkflowIds = this.selectedWorkflows.map((item) => item.id);
      this.moduleSelectorTab = 'modules';
      this.ensurePreviewModule();
      this.ensurePreviewWorkflow();
      this.moduleSelectorVisible = true;
    },

    toggleModuleSelection(uid) {
      const index = this.selectedModuleUids.indexOf(uid);
      if (index >= 0) {
        this.selectedModuleUids.splice(index, 1);
      } else {
        this.selectedModuleUids.push(uid);
      }
    },

    handleSelectAllFiltered(checked) {
      const filteredUids = this.filteredAvailableModules.map((item) => item.uid);
      if (checked) {
        const merged = new Set(this.selectedModuleUids.concat(filteredUids));
        this.selectedModuleUids = Array.from(merged);
      } else {
        this.selectedModuleUids = this.selectedModuleUids.filter((uid) => !filteredUids.includes(uid));
      }
    },

    toggleWorkflowSelection(id) {
      const index = this.selectedWorkflowIds.indexOf(id);
      if (index >= 0) {
        this.selectedWorkflowIds.splice(index, 1);
      } else {
        this.selectedWorkflowIds.push(id);
      }
    },

    handleSelectAllFilteredWorkflows(checked) {
      const filteredIds = this.filteredAvailableWorkflows.map((item) => item.id);
      if (checked) {
        this.selectedWorkflowIds = Array.from(new Set(this.selectedWorkflowIds.concat(filteredIds)));
      } else {
        this.selectedWorkflowIds = this.selectedWorkflowIds.filter((id) => !filteredIds.includes(id));
      }
    },

    applyModuleSelection() {
      const uidSet = new Set(this.selectedModuleUids);
      this.selectedModules = this.availableModules.filter((item) => uidSet.has(item.uid));
      const idSet = new Set(this.selectedWorkflowIds);
      this.selectedWorkflows = this.availableWorkflows.filter((item) => idSet.has(item.id));
      this.moduleSelectorVisible = false;
    },

    clearModuleSelectionInDialog() {
      this.selectedModuleUids = [];
      this.selectedWorkflowIds = [];
    },

    removeModule(uid) {
      this.selectedModules = this.selectedModules.filter((item) => item.uid !== uid);
    },

    removeWorkflow(id) {
      this.selectedWorkflows = this.selectedWorkflows.filter((item) => item.id !== id);
    },

    clearSelectedModules() {
      this.selectedModules = [];
      this.selectedWorkflows = [];
    },

    async startChatSession() {
      if (!this.chatInputText || this.chatInputText.trim().length === 0) {
        this.$message.warning(ncfT('AdminChat.InputRequired'));
        return;
      }

      this.isCreatingSession = true;
      try {
        const requestData = {
          initialMessage: this.chatInputText.trim(),
          aiModelId: this.normalizeAiModelId(this.launcherAiModelId),
          moduleUids: this.selectedModules.map((item) => item.uid),
          workflowIds: this.selectedWorkflows.map((item) => item.id)
        };

        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.CreateSessionAsync', requestData);
        if (response.data && response.data.success && response.data.data) {
          const sessionId = response.data.data.sessionId;
          this.setSessionAiModelId(sessionId, this.launcherAiModelId);
          window.location.href = '/Admin/AdminChat/Chat?sessionId=' + sessionId;
          return;
        }

        this.$message.error((response.data && response.data.errorMessage) || ncfT('AdminChat.CreateSessionFailed'));
      } catch (error) {
        console.error('创建会话失败:', error);
        this.$message.error(ncfT('AdminChat.CreateSessionFailedRetry'));
      } finally {
        this.isCreatingSession = false;
      }
    }
  }
};
