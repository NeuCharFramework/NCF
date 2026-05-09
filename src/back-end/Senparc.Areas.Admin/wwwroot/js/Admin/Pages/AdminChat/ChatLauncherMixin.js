window.ChatLauncherMixin = {
  data() {
    return {
      moduleStorageKey: 'ncf.admin.chat.selectedModuleUids',
      aiModelStorageKey: 'ncf.admin.chat.sessionAiModelMap',
      chatInputText: '',
      launcherAiModelId: 0,
      sessionAiModelMap: {},
      selectedModules: [],
      moduleSelectorVisible: false,
      moduleSearchKeyword: '',
      activePreviewModuleUid: '',
      availableModules: [],
      selectedModuleUids: [],
      loadingModuleOptions: false,
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
    }
  },
  mounted() {
    this.restoreSelectedModuleUids();
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
    sortedFilteredAvailableModules: {
      deep: true,
      handler() {
        this.ensurePreviewModule();
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

    handleLauncherInputKeydown(event) {
      if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
        event.preventDefault();
        this.startChatSession();
      }
    },

    normalizeModuleItem(item) {
      return {
        uid: item.uid,
        name: item.menuName || item.name || '未命名模块',
        icon: item.icon || 'fa fa-cube',
        description: item.description || '暂无描述',
        version: item.version || '',
        menus: item.menus || [],
        functions: item.functions || []
      };
    },

    async ensureModuleOptionsLoaded(forceReload) {
      if (!forceReload && this.availableModules.length > 0) {
        return;
      }

      this.loadingModuleOptions = true;
      try {
        const response = await service.get('/Admin/Index?handler=XncfOpening');
        const moduleList = (response.data && response.data.data) ? response.data.data : [];
        this.availableModules = moduleList.map((item) => this.normalizeModuleItem(item));
        this.syncSelectedModulesFromUids();
        this.ensurePreviewModule();
      } catch (error) {
        console.error('加载模块列表失败:', error);
        this.$message.error('加载模块列表失败，请稍后重试');
      } finally {
        this.loadingModuleOptions = false;
      }
    },

    async openModuleSelector() {
      await this.ensureModuleOptionsLoaded(false);
      this.selectedModuleUids = this.selectedModules.map((item) => item.uid);
      this.ensurePreviewModule();
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

    applyModuleSelection() {
      const uidSet = new Set(this.selectedModuleUids);
      this.selectedModules = this.availableModules.filter((item) => uidSet.has(item.uid));
      this.moduleSelectorVisible = false;
    },

    clearModuleSelectionInDialog() {
      this.selectedModuleUids = [];
    },

    removeModule(uid) {
      this.selectedModules = this.selectedModules.filter((item) => item.uid !== uid);
    },

    clearSelectedModules() {
      this.selectedModules = [];
    },

    async startChatSession() {
      if (!this.chatInputText || this.chatInputText.trim().length === 0) {
        this.$message.warning('请输入对话内容');
        return;
      }

      this.isCreatingSession = true;
      try {
        const requestData = {
          initialMessage: this.chatInputText.trim(),
          aiModelId: this.normalizeAiModelId(this.launcherAiModelId),
          moduleUids: this.selectedModules.map((item) => item.uid)
        };

        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.CreateSessionAsync', requestData);
        if (response.data && response.data.success && response.data.data) {
          const sessionId = response.data.data.sessionId;
          this.setSessionAiModelId(sessionId, this.launcherAiModelId);
          window.location.href = '/Admin/AdminChat/Chat?sessionId=' + sessionId;
          return;
        }

        this.$message.error((response.data && response.data.errorMessage) || '创建会话失败');
      } catch (error) {
        console.error('创建会话失败:', error);
        this.$message.error('创建会话失败，请稍后重试');
      } finally {
        this.isCreatingSession = false;
      }
    }
  }
};
