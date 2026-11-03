var chatApp = new Vue({
  el: '#app',
  mixins: [window.ChatLauncherMixin],
  data() {
    return {
      currentSessionId: 0,
      memoryEnabled: false,
      memoryMode: 0,
      memoryEntries: [],
      memoryHistory: [],
      memoryWorkspaceId: '',
      memoryEntryDraft: null,
      memoryUsage: null,
      memoryUsageSessionId: 0,
      memoryUpdateResult: null,
      memoryUpdateSessionId: 0,
      memoryRevision: '',
      memoryBusy: false,
      memoryReady: false,
      memoryLoadSequence: 0,
      memoryDialogVisible: false,
      memoryDialogSessionId: 0,
      memoryDraftRevision: '',
      memoryMarkdown: '',
      memorySavedMarkdown: '',
      memorySystemMessage: '',
      memoryTab: 'preview',
      globalChatRevision: '',
      globalChatSystemMessage: '',
      globalChatLoaded: false,
      currentSessionTitle: '',
      currentSessionModules: [],
      currentSessionWorkflows: [],
      currentSessionWorkspaceId: '',
      currentSessionWorkspaceName: '',
      currentSessionWorkspaceWriteEnabled: false,
      currentSessionWorkspaceCliEnabled: false,
      currentSessionAiModelId: 0,
      chatMode: 'harness',
      trajectoryDialogVisible: false,
      trajectoryLoading: false,
      trajectoryDetailLoading: false,
      trajectoryList: [],
      selectedTrajectory: null,
      selectedTrajectoryEvents: [],
      trajectorySearchText: '',
      replayIndex: -1,
      replayPlaying: false,
      replayTimer: null,
      aiModelOptions: [],
      aiKernelAvailable: false,
      loadingAiModelOptions: false,
      sessionList: [],
      sidebarWidth: 250,
      sidebarCollapsed: false,
      collapsedSessionGroups: [],
      sidebarResizeHandler: null,
      sidebarResizeEndHandler: null,
      draggingSessionId: 0,
      dropSessionId: 0,
      dropAfter: false,
      savingSessionOrder: false,
      runningSessionId: 0,
      runningMessages: [],
      sessionUsage: {},
      sessionDetailSequence: 0,
      trajectoryListSequence: 0,
      trajectoryDetailSequence: 0,
      messageList: [],
      inputMessage: '',
      loadingSessions: false,
      loadingMessages: false,
      isSending: false,
      isStopping: false,
      activeRequestController: null,
      isAIResponding: false,
      liveAssistantText: '',
      liveThinkingText: '',
      liveTrajectoryEvents: [],
      livePendingApprovals: [],
      liveTrajectoryId: 0,
      liveStatusText: '',
      recentTasks: [],
      activeTaskId: '',
      latestUsage: null,
      pendingInitialSend: false,
      currentUserId: 0,
      autoScrollEnabled: true,
      isManageMode: false,
      selectedMessageIds: [],
      isSuperAdmin: false,
      userStatsDrawerVisible: false,
      userStatsLoading: false,
      userStatsList: [],
      workspacePreviewOpen: false,
      workspacePreviewWidth: 420,
      workspacePreviewMaxWidth: 420,
      workspacePreviewOverlay: false,
      workspacePreviewSource: false,
      pendingAttachments: [],
      workspaceUploading: false,
      workspaceUploadCapabilities: null,
      workspaceDownloading: false,
      workspaceShareDialogVisible: false,
      workspaceSharePath: '',
      workspaceShareHours: 24,
      workspaceShares: [],
      workspaceSharesLoading: false,
      workspaceShareCreating: false,
      workspaceShareRevoking: '',
      workspaceEntries: [],
      workspacePreviewPath: '',
      workspacePreviewFilePath: '',
      workspacePreviewData: null,
      workspacePreviewSrcdoc: '',
      workspaceEntriesLoading: false,
      workspacePreviewLoading: false,
      workspaceResizeHandler: null,
      workspaceResizeEndHandler: null
    };
  },
  computed: {
    isCurrentSessionRunning() {
      return this.isSending && this.runningSessionId === this.currentSessionId;
    },
    canUploadWorkspaceFiles() {
      return this.isSuperAdmin && !!(this.currentSessionId ? this.currentSessionWorkspaceId : this.selectedWorkspaceId);
    },
    attachmentUploadHint() {
      if (!this.canUploadWorkspaceFiles) return '先选择或创建 Workspace，再上传资料';
      const size = this.workspaceUploadCapabilities
        ? ` · 单文件最大 ${this.formatWorkspaceFileSize(this.workspaceUploadCapabilities.maxFileBytes)}` : '';
      const parseLimit = this.workspaceUploadCapabilities
        ? `超过 ${this.formatWorkspaceFileSize(this.workspaceUploadCapabilities.inlineParseMaxFileBytes)} 的文件将保存后由 Workspace/Python 分批处理`
        : '大文件将保存后由 Workspace/Python 分批处理';
      return `上传 / 拖放 / 粘贴资料到 Workspace（最多 4 个）${size}。${parseLimit}，不会自动把全文发送给模型。`;
    },
    workspaceExternalPreviewUrl() {
      return this.workspaceAssetsUrl('preview') + '?path=' + encodeURIComponent(this.workspacePreviewFilePath);
    },
    workspacePreviewIsDocument() {
      return !!this.workspacePreviewData
        && ['text/html', 'text/markdown'].includes(this.workspacePreviewData.contentType);
    },
    workspacePreviewCanShowSource() {
      return !!this.workspacePreviewData && this.workspacePreviewData.isText
        && (this.workspacePreviewIsDocument || this.workspacePreviewData.contentType === 'image/svg+xml');
    },
    usageStatusText() {
      if (!this.latestUsage || !this.hasUsage(this.latestUsage)) {
        return 'Token：—';
      }

      return `Token：输入 ${this.formatTokenCount(this.latestUsage.inputTokens)} · 输出 ${this.formatTokenCount(this.latestUsage.outputTokens)} · 总计 ${this.formatTokenCount(this.latestUsage.totalTokens)}`;
    },
    replayEvent() {
      return this.replayIndex >= 0 ? this.selectedTrajectoryEvents[this.replayIndex] || null : null;
    },
    groupedSessionList() {
      const groups = [];
      const groupMap = new Map();
      (this.sessionList || []).forEach((session) => {
        const key = session.workspaceId || '__no_workspace__';
        if (!groupMap.has(key)) {
          const group = {
            key,
            name: session.workspaceName || (key === '__no_workspace__' ? '未使用 Workspace' : key),
            sessions: []
          };
          groupMap.set(key, group);
          groups.push(group);
        }
        groupMap.get(key).sessions.push(session);
      });
      groups.forEach(group => group.sessions.sort((a, b) =>
        (a.sortOrder ?? Number.MAX_SAFE_INTEGER) - (b.sortOrder ?? Number.MAX_SAFE_INTEGER)));
      return groups;
    }
  },
  mounted() {
    this.workspacePreviewResizeObserver = new ResizeObserver(() => this.updateWorkspacePreviewLayout());
    this.workspacePreviewResizeObserver.observe(this.$refs.workspaceContainer.$el);
    if (window.INITIAL_DATA) {
      this.currentSessionId = window.INITIAL_DATA.sessionId || 0;
      this.currentUserId = window.INITIAL_DATA.currentUserId || 0;
      this.isSuperAdmin = !!window.INITIAL_DATA.isSuperAdmin;
      this.ensureWorkspaceOptionsLoaded();
      
      if (window.INITIAL_DATA.initialMessage) {
        const initialMessage = decodeURIComponent(window.INITIAL_DATA.initialMessage);
        if (this.currentSessionId > 0) {
          this.inputMessage = initialMessage;
        } else {
          this.chatInputText = initialMessage;
        }
      }
    }
    const modeParam = new URLSearchParams(window.location.search || '').get('mode');
    if (modeParam === '0') this.chatMode = 'simple';
    if (modeParam === '1') this.chatMode = 'harness';
    const initialMessageKey = `ncf.admin.chat.initialMessage.${this.currentSessionId}`;
    const initialMessageParam = this.currentSessionId > 0 ? sessionStorage.getItem(initialMessageKey) : '';
    if (initialMessageParam) {
      sessionStorage.removeItem(initialMessageKey);
      this.inputMessage = initialMessageParam;
      this.pendingInitialSend = true;
    }

    this.currentSessionAiModelId = this.getSessionAiModelId(this.currentSessionId);
    this.launcherAiModelId = this.currentSessionAiModelId;
    this.loadSidebarPreferences();

    this.loadAiModelOptions();
    this.loadSessionList();
    
    if (this.currentSessionId > 0) {
      this.loadSessionDetail().then(() => {
        this.loadTrajectoryList();
        if (this.pendingInitialSend) {
          this.pendingInitialSend = false;
          this.sendMessage();
        }
      });
    }
  },
  beforeDestroy() {
    this.workspacePreviewResizeObserver.disconnect();
    if (this.activeRequestController) {
      this.activeRequestController.abort();
      this.activeRequestController = null;
    }
    this.stopWorkspacePreviewResize();
    this.stopSidebarResize();
    this.clearPendingAttachments();
  },
  methods: {
    sidebarStorageKey() {
      return `ncf.admin.chat.sidebar.${this.currentUserId}`;
    },
    loadSidebarPreferences() {
      try {
        const raw = localStorage.getItem(this.sidebarStorageKey());
        if (!raw) return;
        const preferences = JSON.parse(raw);
        if (!preferences || typeof preferences !== 'object'
          || !Number.isFinite(preferences.width) || typeof preferences.collapsed !== 'boolean'
          || !Array.isArray(preferences.groups) || !preferences.groups.every(key => typeof key === 'string')) {
          throw new Error('Invalid sidebar preferences.');
        }
        this.sidebarWidth = Math.max(215, Math.min(420, preferences.width));
        this.sidebarCollapsed = preferences.collapsed;
        this.collapsedSessionGroups = preferences.groups;
      } catch (error) {
        console.error('Loading AdminChat sidebar preferences failed:', error);
        this.$message.warning(ncfT('AdminChat.Sidebar.PreferencesFailed'));
      }
    },
    saveSidebarPreferences() {
      try {
        localStorage.setItem(this.sidebarStorageKey(), JSON.stringify({
          width: this.sidebarWidth, collapsed: this.sidebarCollapsed, groups: this.collapsedSessionGroups
        }));
      } catch (error) {
        console.error('Saving AdminChat sidebar preferences failed:', error);
        this.$message.warning(ncfT('AdminChat.Sidebar.PreferencesFailed'));
      }
    },
    toggleSidebar() {
      this.sidebarCollapsed = !this.sidebarCollapsed;
      this.saveSidebarPreferences();
      this.$nextTick(() => this.updateWorkspacePreviewLayout());
    },
    isSessionGroupCollapsed(key) {
      return this.collapsedSessionGroups.includes(key);
    },
    toggleSessionGroup(key) {
      this.collapsedSessionGroups = this.isSessionGroupCollapsed(key)
        ? this.collapsedSessionGroups.filter(item => item !== key)
        : this.collapsedSessionGroups.concat(key);
      this.saveSidebarPreferences();
    },
    setSidebarWidth(width) {
      this.sidebarWidth = Math.max(215, Math.min(420, width));
      this.$nextTick(() => this.updateWorkspacePreviewLayout());
    },
    resizeSidebarByKeyboard(delta) {
      this.setSidebarWidth(this.sidebarWidth + delta);
      this.saveSidebarPreferences();
    },
    startSidebarResize(event) {
      if (event.button !== 0 || this.sidebarCollapsed) return;
      event.preventDefault();
      this.stopSidebarResize();
      const startX = event.clientX;
      const startWidth = event.currentTarget.parentElement.getBoundingClientRect().width;
      const pointerId = event.pointerId;
      this.sidebarResizeHandler = move => {
        if (move.pointerId === pointerId) this.setSidebarWidth(startWidth + move.clientX - startX);
      };
      this.sidebarResizeEndHandler = end => {
        if (end.pointerId !== undefined && end.pointerId !== pointerId) return;
        this.stopSidebarResize();
        this.saveSidebarPreferences();
      };
      document.addEventListener('pointermove', this.sidebarResizeHandler);
      document.addEventListener('pointerup', this.sidebarResizeEndHandler);
      document.addEventListener('pointercancel', this.sidebarResizeEndHandler);
      window.addEventListener('blur', this.sidebarResizeEndHandler);
      document.body.classList.add('is-resizing-chat-sidebar');
    },
    stopSidebarResize() {
      document.removeEventListener('pointermove', this.sidebarResizeHandler);
      document.removeEventListener('pointerup', this.sidebarResizeEndHandler);
      document.removeEventListener('pointercancel', this.sidebarResizeEndHandler);
      window.removeEventListener('blur', this.sidebarResizeEndHandler);
      document.body.classList.remove('is-resizing-chat-sidebar');
      this.sidebarResizeHandler = null;
      this.sidebarResizeEndHandler = null;
    },
    isSessionRunning(session) {
      return this.isSending && this.runningSessionId === session.id;
    },
    startSessionDrag(event, session) {
      if (this.savingSessionOrder) { event.preventDefault(); return; }
      this.draggingSessionId = session.id;
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', String(session.id));
    },
    canDropSession(session) {
      const source = this.sessionList.find(item => item.id === this.draggingSessionId);
      return !this.savingSessionOrder && !!source && source.id !== session.id
        && (source.workspaceId || '') === (session.workspaceId || '');
    },
    handleSessionDragOver(event, session) {
      if (!this.canDropSession(session)) {
        this.dropSessionId = 0;
        return;
      }
      event.preventDefault();
      event.dataTransfer.dropEffect = 'move';
      this.dropSessionId = session.id;
      const bounds = event.currentTarget.getBoundingClientRect();
      this.dropAfter = event.clientY > bounds.top + bounds.height / 2;
    },
    async handleSessionDrop(event, session) {
      if (!this.canDropSession(session)) return;
      event.preventDefault();
      const bounds = event.currentTarget.getBoundingClientRect();
      const insertAfter = event.clientY > bounds.top + bounds.height / 2;
      const sessionId = this.draggingSessionId;
      this.endSessionDrag();
      await this.saveSessionOrder(sessionId, session.id, insertAfter);
    },
    endSessionDrag() {
      this.draggingSessionId = 0;
      this.dropSessionId = 0;
      this.dropAfter = false;
    },
    async moveSessionByKeyboard(session, direction) {
      if (this.savingSessionOrder) return;
      const group = this.groupedSessionList.find(item => item.sessions.some(value => value.id === session.id));
      const index = group.sessions.findIndex(item => item.id === session.id);
      const target = group.sessions[index + direction];
      if (target) await this.saveSessionOrder(session.id, target.id, direction > 0);
    },
    async saveSessionOrder(sessionId, targetSessionId, insertAfter) {
      if (this.savingSessionOrder) return;
      this.savingSessionOrder = true;
      try {
        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.ReorderSessionAsync',
          { sessionId, targetSessionId, insertAfter });
        if (!response.data || !response.data.success || !Array.isArray(response.data.data)) {
          throw new Error(response.data && response.data.errorMessage || ncfT('AdminChat.Sidebar.SortFailed'));
        }
        const ranks = new Map(response.data.data.map(session => [session.id, session.sortOrder]));
        this.sessionList = this.sessionList.map(session => ranks.has(session.id)
          ? { ...session, sortOrder: ranks.get(session.id) } : session);
        this.$message.success(ncfT('AdminChat.Sidebar.SortSaved'));
      } catch (error) {
        console.error('Reordering AdminChat sessions failed:', error);
        this.$message.error(error.message || ncfT('AdminChat.Sidebar.SortFailed'));
      } finally {
        this.savingSessionOrder = false;
      }
    },
    async memoryRequest(method, body, get = false) {
      const base = '/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.';
      const response = get ? await service.get(base + method) : await service.post(base + method, body);
      if (!response.data || !response.data.success || !response.data.data) {
        throw new Error((response.data && response.data.errorMessage) || ncfT('AdminChat.Memory.Failed'));
      }
      return response.data.data;
    },

    reportMemoryUpdate(status, usage, sessionId = this.currentSessionId, result = null) {
      if (usage) {
        this.memoryUsage = usage;
        this.memoryUsageSessionId = sessionId;
      }
      this.memoryUpdateResult = result;
      this.memoryUpdateSessionId = sessionId;
      if (status && !['disabled', 'updated', 'unchanged', 'read-only', 'skipped-input'].includes(status)) {
        this.$message.warning(ncfT('AdminChat.Memory.UpdateWarning') + ' (' + status + ')');
      }
    },

    memoryUpdateText(result) {
      if (!result) return '';
      if (result.status === 'updated') {
        return ncfT('AdminChat.Memory.UpdateSaved', result.globalChanges, result.workspaceChanges);
      }
      const knownReasons = ['mode-off', 'read-only', 'input-not-eligible', 'no-candidates',
        'pinned', 'no-difference', 'revision-conflict', 'timeout', 'invalid-change', 'capacity', 'invalid-json', 'pending'];
      const reason = knownReasons.includes(result.reason) ? result.reason : 'update-failed';
      return ncfT('AdminChat.Memory.UpdateReason.' + reason);
    },

    formatMemoryContent(markdown) {
      if (typeof DOMPurify === 'undefined') return '';
      return DOMPurify.sanitize(this.formatMessageContent(markdown), {
        FORBID_TAGS: ['img', 'iframe', 'video', 'audio', 'source', 'svg', 'math', 'style', 'form'],
        FORBID_ATTR: ['src', 'srcset', 'href', 'style']
      });
    },

    async loadMemoryMode() {
      const sessionId = this.currentSessionId;
      const sequence = ++this.memoryLoadSequence;
      this.memoryEnabled = false;
      this.memoryMode = 0;
      this.memoryRevision = '';
      this.memoryReady = false;
      this.memoryBusy = true;
      try {
        const data = await this.memoryRequest(`GetMemoryAsync?sessionId=${sessionId}`, null, true);
        if (sessionId !== this.currentSessionId || sequence !== this.memoryLoadSequence) return;
        this.memoryEnabled = data.enabled;
        this.memoryMode = data.mode == null ? (data.enabled ? 2 : 0) : data.mode;
        this.memoryRevision = data.revision;
        this.memoryReady = true;
      } catch (error) {
        if (sequence === this.memoryLoadSequence) this.$message.error(error.message);
      } finally {
        if (sessionId === this.currentSessionId && sequence === this.memoryLoadSequence) this.memoryBusy = false;
      }
    },

    memoryModeLabel(mode) {
      return ncfT('AdminChat.Memory.Mode' + ['Off', 'ReadOnly', 'Learn'][Number(mode) || 0]);
    },

    handleMemoryCommand(command, launcher = false) {
      if (command === 'settings') return this.openMemoryDialog();
      const mode = Number(command);
      if (launcher) this.launcherMemoryMode = mode;
      else return this.changeMemoryMode(mode);
    },

    async changeMemoryMode(mode) {
      if (typeof mode === 'boolean') mode = mode ? 2 : 0;
      const sessionId = this.currentSessionId;
      if (this.memoryBusy || this.isSending) return;
      this.memoryBusy = true;
      try {
        const data = await this.memoryRequest('SetMemoryEnabledAsync', {
          sessionId, revision: this.memoryRevision, mode
        });
        if (sessionId !== this.currentSessionId) return;
        this.memoryEnabled = data.enabled;
        this.memoryMode = data.mode == null ? (data.enabled ? 2 : 0) : data.mode;
        this.memoryRevision = data.revision;
      } catch (error) {
        this.$message.error(error.message);
        await this.loadMemoryMode();
      } finally {
        if (sessionId === this.currentSessionId) this.memoryBusy = false;
      }
    },

    async openMemoryDialog() {
      if (this.memoryDialogVisible || this.memoryBusy) return;
      this.memoryBusy = true;
      const sessionId = this.currentSessionId;
      try {
        const data = await this.memoryRequest(`GetMemoryAsync?sessionId=${sessionId}`, null, true);
        if (sessionId !== this.currentSessionId) return;
        this.memoryDialogSessionId = sessionId;
        this.memoryDraftRevision = data.revision;
        this.memoryMarkdown = data.markdown;
        this.memorySavedMarkdown = data.markdown;
        this.memorySystemMessage = data.systemMessage;
        this.memoryEntries = data.entries || [];
        this.memoryHistory = data.history || [];
        this.memoryWorkspaceId = data.workspaceId || '';
        this.memoryEntryDraft = null;
        this.globalChatLoaded = false;
        this.memoryTab = 'entries';
        this.memoryDialogVisible = true;
        if (this.isSuperAdmin) {
          const global = await this.memoryRequest('GetGlobalChatSystemMessageAsync', null, true);
          this.globalChatRevision = global.revision;
          this.globalChatSystemMessage = global.systemMessage;
          this.globalChatLoaded = true;
        }
      } catch (error) {
        this.$message.error(error.message);
      } finally {
        this.memoryBusy = false;
      }
    },

    async saveMemory() {
      this.memoryBusy = true;
      try {
        const data = await this.memoryRequest('SaveMemoryAsync', {
          sessionId: this.memoryDialogSessionId, revision: this.memoryDraftRevision,
          markdown: this.memoryMarkdown, systemMessage: this.memorySystemMessage
        });
        this.memoryDraftRevision = data.revision;
        this.memoryMarkdown = data.markdown;
        this.memorySavedMarkdown = data.markdown;
        this.memoryEntries = data.entries || [];
        this.memoryHistory = data.history || [];
        if (this.memoryDialogSessionId === this.currentSessionId) this.memoryRevision = data.revision;
        this.$message.success(ncfT('AdminChat.Memory.Saved'));
      } catch (error) {
        this.$message.error(error.message);
      } finally {
        this.memoryBusy = false;
      }
    },

    editMemoryEntry(entry) {
      if (this.memoryMarkdown !== this.memorySavedMarkdown) {
        this.$message.warning(ncfT('AdminChat.Memory.UnsavedMarkdown'));
        return;
      }
      this.memoryEntryDraft = entry ? {
        id: entry.id, version: entry.version, text: entry.text, topic: entry.topic,
        tier: entry.tier, kind: entry.kind, pinned: entry.pinned,
        scope: entry.workspaceId ? 'workspace' : 'global', expiresAt: entry.expiresAt, remove: false
      } : {
        id: '', version: 0, text: '', topic: '', tier: 'core', kind: 'preference',
        pinned: true, scope: 'global', expiresAt: null, remove: false
      };
    },

    async saveMemoryEntry(remove = false) {
      if (!this.memoryEntryDraft || this.memoryBusy) return;
      if (this.memoryMarkdown !== this.memorySavedMarkdown) {
        this.$message.warning(ncfT('AdminChat.Memory.UnsavedMarkdown'));
        return;
      }
      this.memoryBusy = true;
      try {
        const data = await this.memoryRequest('EditMemoryEntryAsync', {
          sessionId: this.memoryDialogSessionId, revision: this.memoryDraftRevision,
          entry: { ...this.memoryEntryDraft, remove }
        });
        this.memoryDraftRevision = data.revision;
        this.memoryEntries = data.entries || [];
        this.memoryHistory = data.history || [];
        this.memoryEntryDraft = null;
        this.memoryMarkdown = data.markdown;
        this.memorySavedMarkdown = data.markdown;
        if (this.memoryDialogSessionId === this.currentSessionId) this.memoryRevision = data.revision;
        this.$message.success(ncfT('AdminChat.Memory.Saved'));
      } catch (error) {
        this.$message.error(error.message);
      } finally {
        this.memoryBusy = false;
      }
    },

    async saveGlobalChatSystemMessage() {
      this.memoryBusy = true;
      try {
        const data = await this.memoryRequest('SaveGlobalChatSystemMessageAsync', {
          revision: this.globalChatRevision, systemMessage: this.globalChatSystemMessage
        });
        this.globalChatRevision = data.revision;
        this.$message.success(ncfT('AdminChat.Memory.Saved'));
      } catch (error) {
        this.$message.error(error.message);
      } finally {
        this.memoryBusy = false;
      }
    },

    downloadMemory() {
      const url = URL.createObjectURL(new Blob([this.memoryMarkdown], { type: 'text/markdown;charset=utf-8' }));
      const link = document.createElement('a');
      link.href = url;
      link.download = 'Memory.md';
      link.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    },

    chooseWorkspaceFiles() {
      this.$refs.workspaceFileInput.click();
    },

    handleWorkspaceFilesSelected(event) {
      const files = Array.from(event.target.files || []);
      event.target.value = '';
      if (files.length) this.stageWorkspaceFiles(files);
    },

    handleWorkspaceDragOver(event) {
      if (Array.from(event.dataTransfer.types || []).includes('Files')) event.preventDefault();
    },

    handleWorkspaceDrop(event) {
      const files = Array.from(event.dataTransfer.files || []);
      if (!files.length) return;
      event.preventDefault();
      this.stageWorkspaceFiles(files);
    },

    handleWorkspacePaste(event) {
      const files = Array.from((event.clipboardData && event.clipboardData.files) || []);
      if (!files.length) return;
      event.preventDefault();
      this.stageWorkspaceFiles(files);
    },

    async ensureAttachmentSession() {
      if (this.currentSessionId) return;
      const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.CreateSessionAsync',
        this.launcherSessionRequest((this.chatInputText || '').trim().slice(0, 60) || '附件对话'));
      if (!response.data || !response.data.success || !response.data.data || !response.data.data.sessionId) {
        throw new Error((response.data && (response.data.msg || response.data.errorMessage)) || '创建附件会话失败。');
      }
      this.currentSessionId = response.data.data.sessionId;
      this.currentSessionAiModelId = this.normalizeAiModelId(this.launcherAiModelId);
      this.setSessionAiModelId(this.currentSessionId, this.currentSessionAiModelId);
      this.chatMode = this.launcherChatMode;
      this.inputMessage = this.chatInputText;
      this.chatInputText = '';
      await this.loadSessionDetail();
      if (!this.currentSessionWorkspaceId) throw new Error('会话已创建，但没有读到 Workspace，请刷新后重试。');
      const url = new URL(window.location.href);
      url.searchParams.set('sessionId', this.currentSessionId);
      url.searchParams.set('mode', this.chatMode === 'harness' ? '1' : '0');
      window.history.replaceState(window.history.state, '', url.href);
      await this.loadSessionList();
    },

    async prepareVisionPreview(file) {
      const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' });
      try {
        if (bitmap.width * bitmap.height > 40_000_000) throw new Error('图片像素过大，不能生成视觉预览。');
        const canvas = document.createElement('canvas');
        const scale = Math.min(1, 1536 / Math.max(bitmap.width, bitmap.height));
        canvas.width = Math.max(1, Math.round(bitmap.width * scale));
        canvas.height = Math.max(1, Math.round(bitmap.height * scale));
        const context = canvas.getContext('2d');
        if (!context) throw new Error('浏览器无法生成图片预览。');
        context.fillStyle = '#fff';
        context.fillRect(0, 0, canvas.width, canvas.height);
        context.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
        for (const quality of [0.82, 0.6, 0.4]) {
          const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/jpeg', quality));
          if (blob && blob.size <= 512 * 1024) return blob;
        }
        throw new Error('图片预览仍超过 512 KB，请压缩图片后重试。');
      } finally { bitmap.close(); }
    },

    async stageWorkspaceFiles(files) {
      if (!this.canUploadWorkspaceFiles) {
        this.$message.warning('请先选择或创建 Workspace。');
        return;
      }
      if (this.workspaceUploading || this.isSending) {
        this.$message.warning('正在上传或生成，请稍后再添加附件。');
        return;
      }
      if (files.length + this.pendingAttachments.length > 4) {
        this.$message.warning('每条消息最多附加 4 个文件，请减少本次选择。');
        return;
      }
      this.workspaceUploading = true;
      try {
        await this.ensureAttachmentSession();
        const capabilities = await service.get(this.workspaceAssetsUrl('attachments/capabilities'), { customAlert: true });
        if (!capabilities.data || !Number.isFinite(capabilities.data.maxFileBytes) || capabilities.data.maxFileBytes <= 0) {
          throw new Error('无法取得附件大小限制，未开始上传。');
        }
        this.workspaceUploadCapabilities = capabilities.data;
        const inlineParseMaxFileBytes = Number(capabilities.data.inlineParseMaxFileBytes);
        for (const file of files) {
          let localPreviewUrl = '';
          try {
            if (file.size > capabilities.data.maxFileBytes || file.size === 0) {
              throw new Error(`${file.name}：文件为空或超过 ${this.formatWorkspaceFileSize(capabilities.data.maxFileBytes)} 限制。`);
            }
            const extension = '.' + file.name.split('.').pop().toLowerCase();
            if (!capabilities.data.extensions.includes(extension)) throw new Error(`${file.name}：不支持此文件类型。`);
            const form = new FormData();
            form.append('file', file, file.name);
            const isLargeUpload = Number.isFinite(inlineParseMaxFileBytes)
              && file.size > inlineParseMaxFileBytes;
            if (!isLargeUpload && ['.png', '.jpg', '.jpeg', '.webp'].includes(extension)) {
              try {
                const preview = await this.prepareVisionPreview(file);
                form.append('visionPreview', preview, 'vision.jpg');
                localPreviewUrl = URL.createObjectURL(preview);
              } catch (error) {
                console.error('无法生成视觉预览:', error);
                this.$message.warning(`${file.name}：${error.message} 仍会保存原文件，但不能启用视觉。`);
              }
            }
            const uploadAction = isLargeUpload ? 'attachments/large' : 'attachments';
            const response = await service.post(this.workspaceAssetsUrl(uploadAction), form, {
              customAlert: true,
              timeout: isLargeUpload ? 0 : undefined
            });
            if (!response.data || !response.data.attachment) throw new Error(`${file.name}：上传响应无效。`);
            this.pendingAttachments.push({ ...response.data.attachment, useVision: false, localPreviewUrl });
          } catch (error) {
            if (localPreviewUrl) URL.revokeObjectURL(localPreviewUrl);
            await this.showWorkspaceAssetError(error);
          }
        }
        if (this.workspacePreviewOpen) await this.loadWorkspaceEntries(this.workspacePreviewPath);
      } catch (error) {
        await this.showWorkspaceAssetError(error);
      } finally { this.workspaceUploading = false; }
    },

    validatePendingVision(attachment) {
      if (this.pendingAttachments.filter(item => item.useVision).length > 2) {
        attachment.useVision = false;
        this.$message.warning('每条消息最多对 2 张图片启用视觉处理。');
      }
    },

    async removePendingAttachment(attachment) {
      if (this.currentSessionId) {
        try {
          const response = await service.post(
            this.workspaceAssetsUrl('attachments/delete'),
            { relativePath: attachment.relativePath, eTag: attachment.eTag },
            { customAlert: true });
          if (!response.data || response.data.deleted !== true) {
            throw new Error('附件删除响应无效。');
          }
        } catch (error) {
          await this.showWorkspaceAssetError(error);
          return;
        }
      }
      if (attachment.localPreviewUrl) URL.revokeObjectURL(attachment.localPreviewUrl);
      this.pendingAttachments = this.pendingAttachments.filter(item => item !== attachment);
    },

    async deletePendingAttachments() {
      const attachments = this.pendingAttachments.slice();
      if (!attachments.length) return;

      const failed = [];
      for (const attachment of attachments) {
        try {
          if (this.currentSessionId) {
            const response = await service.post(
              this.workspaceAssetsUrl('attachments/delete'),
              { relativePath: attachment.relativePath, eTag: attachment.eTag },
              { customAlert: true });
            if (!response.data || response.data.deleted !== true) throw new Error('附件删除响应无效。');
          }
          if (attachment.localPreviewUrl) URL.revokeObjectURL(attachment.localPreviewUrl);
        } catch (error) {
          console.error('清理待提交 Workspace 附件失败:', error);
          failed.push(attachment);
        }
      }
      this.pendingAttachments = failed;
      if (failed.length) {
        this.$message.warning('部分待提交附件未能自动清理，请在原会话中重试。');
      }
      this.workspaceUploadCapabilities = null;
    },

    clearPendingAttachments() {
      this.pendingAttachments.forEach(attachment => {
        if (attachment.localPreviewUrl) URL.revokeObjectURL(attachment.localPreviewUrl);
      });
      this.pendingAttachments = [];
      this.workspaceUploadCapabilities = null;
    },

    workspaceAssetsUrl(action) {
      return `/api/Senparc.Areas.Admin/WorkspaceAssets/${this.currentSessionId}/${action}`;
    },

    async showWorkspaceAssetError(error) {
      console.error('Workspace 资产操作失败:', error);
      let data = error.response && error.response.data;
      if (data instanceof Blob) {
        const text = await data.text();
        try { data = JSON.parse(text); } catch (parseError) { data = null; }
      }
      this.$message.error((data && (data.message || data.title)) || error.message || 'Workspace 资产操作失败。');
    },

    async downloadWorkspaceEntry(path) {
      if (this.workspaceDownloading) return;
      this.workspaceDownloading = true;
      try {
        const response = await service.get(this.workspaceAssetsUrl('download'), {
          params: { path }, responseType: 'blob', customAlert: true
        });
        const disposition = response.headers['content-disposition'] || '';
        const utf8Name = disposition.match(/filename\*=UTF-8''([^;]+)/i);
        const plainName = disposition.match(/filename="([^"]+)"|filename=([^;]+)/i);
        if (!utf8Name && !plainName) {
          throw new Error('下载未成功，请确认登录状态和 Workspace 访问权限。');
        }
        const filename = utf8Name
          ? decodeURIComponent(utf8Name[1])
          : (plainName[1] || plainName[2]).trim();
        const link = document.createElement('a');
        const url = URL.createObjectURL(response.data);
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 60000);
      } catch (error) {
        await this.showWorkspaceAssetError(error);
      } finally {
        this.workspaceDownloading = false;
      }
    },

    async openWorkspaceShareDialog() {
      this.workspaceSharePath = this.workspacePreviewFilePath;
      this.workspaceShares = [];
      this.workspaceShareDialogVisible = true;
      await this.loadWorkspaceShares();
    },

    async loadWorkspaceShares() {
      this.workspaceSharesLoading = true;
      try {
        const response = await service.get(this.workspaceAssetsUrl('shares'), { customAlert: true });
        if (!response.data || !Array.isArray(response.data.shares)) throw new Error('无法加载分享列表。');
        this.workspaceShares = response.data.shares.filter(share => share.path === this.workspaceSharePath);
      } catch (error) {
        await this.showWorkspaceAssetError(error);
      } finally { this.workspaceSharesLoading = false; }
    },

    async createWorkspaceShare() {
      this.workspaceShareCreating = true;
      try {
        const response = await service.post(this.workspaceAssetsUrl('shares'), {
          path: this.workspaceSharePath, expiresInHours: this.workspaceShareHours
        }, { customAlert: true });
        if (!response.data || !response.data.share) throw new Error('创建分享失败。');
        this.workspaceShares.unshift(response.data.share);
        this.$message.success('只读快照已创建，可复制链接。');
      } catch (error) {
        await this.showWorkspaceAssetError(error);
      } finally { this.workspaceShareCreating = false; }
    },

    async revokeWorkspaceShare(share) {
      this.workspaceShareRevoking = share.id;
      try {
        const response = await service.post(this.workspaceAssetsUrl(`shares/${encodeURIComponent(share.id)}/revoke`), null,
          { customAlert: true });
        if (!response.data || response.data.revoked !== true) throw new Error('撤销分享失败，请确认登录状态。');
        this.workspaceShares = this.workspaceShares.filter(item => item.id !== share.id);
        this.$message.success('分享已撤销。');
      } catch (error) {
        await this.showWorkspaceAssetError(error);
      } finally { this.workspaceShareRevoking = ''; }
    },

    workspaceShareAbsoluteUrl(url) {
      return new URL(url, window.location.origin).href;
    },

    async copyWorkspaceShareLink(share) {
      try {
        if (!navigator.clipboard || !navigator.clipboard.writeText) {
          this.$message.warning('当前浏览器不支持自动复制，请选中链接手动复制。');
          return;
        }
        await navigator.clipboard.writeText(this.workspaceShareAbsoluteUrl(share.url));
        this.$message.success('分享链接已复制。');
      } catch (error) {
        await this.showWorkspaceAssetError(error);
      }
    },

    toggleWorkspacePreview() {
      this.workspacePreviewOpen = !this.workspacePreviewOpen;
      if (this.workspacePreviewOpen) {
        this.$nextTick(() => this.updateWorkspacePreviewLayout());
        this.loadWorkspaceEntries(this.workspacePreviewPath);
      }
    },

    updateWorkspacePreviewLayout() {
      if (!this.workspacePreviewOpen) return;
      const container = this.$refs.workspaceContainer.$el;
      const sidebarWidth = container.querySelector('.chat-sidebar').getBoundingClientRect().width;
      const chatMinWidth = parseFloat(getComputedStyle(container).getPropertyValue('--chat-content-min-width'));
      const availableWidth = container.clientWidth - sidebarWidth;
      this.workspacePreviewOverlay = availableWidth < chatMinWidth + 320;
      this.workspacePreviewMaxWidth = this.workspacePreviewOverlay
        ? container.clientWidth
        : availableWidth - chatMinWidth;
      this.workspacePreviewWidth = Math.min(this.workspacePreviewWidth, this.workspacePreviewMaxWidth);
    },

    expandWorkspacePreview() {
      this.updateWorkspacePreviewLayout();
      this.workspacePreviewWidth = this.workspacePreviewMaxWidth;
    },

    async loadWorkspaceEntries(relativePath) {
      if (!this.currentSessionWorkspaceId || !this.currentSessionId) return;
      const path = this.normalizeWorkspacePath(relativePath);
      this.workspaceEntriesLoading = true;
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetWorkspaceEntriesAsync', {
          params: { sessionId: this.currentSessionId, relativePath: path }
        });
        if (!response.data || !response.data.success || !response.data.data) {
          throw new Error((response.data && response.data.msg) || 'Workspace 目录读取失败。');
        }
        this.workspacePreviewPath = path;
        this.workspaceEntries = response.data.data.entries || [];
      } catch (error) {
        console.error('加载 Workspace 目录失败:', error);
        this.$notify({ title: 'Workspace', message: error.message || 'Workspace 目录读取失败。', type: 'error', duration: 3000 });
      } finally {
        this.workspaceEntriesLoading = false;
      }
    },

    async handleWorkspaceEntryClick(entry) {
      if (!entry || !entry.relativePath) return;
      this.workspacePreviewSource = false;
      if (entry.isDirectory) {
        this.workspacePreviewFilePath = '';
        this.workspacePreviewData = null;
        this.workspacePreviewSrcdoc = '';
        await this.loadWorkspaceEntries(entry.relativePath);
        return;
      }

      this.workspacePreviewFilePath = entry.relativePath;
      this.workspacePreviewLoading = true;
      this.workspacePreviewData = null;
      this.workspacePreviewSrcdoc = '';
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetWorkspaceFilePreviewAsync', {
          params: { sessionId: this.currentSessionId, relativePath: entry.relativePath }
        });
        if (!response.data || !response.data.success || !response.data.data) {
          throw new Error((response.data && response.data.msg) || '文件预览加载失败。');
        }
        this.workspacePreviewData = response.data.data;
        if (this.workspacePreviewIsDocument) {
          const isMarkdown = this.workspacePreviewData.contentType === 'text/markdown';
          this.workspacePreviewSrcdoc = await this.buildWorkspaceHtmlPreview(
            isMarkdown
              ? this.formatMessageContent(this.workspacePreviewData.content)
              : this.workspacePreviewData.content,
            this.workspacePreviewData.path,
            isMarkdown);
        }
      } catch (error) {
        console.error('加载 Workspace 文件预览失败:', error);
        this.$notify({ title: 'Workspace', message: error.message || '文件预览加载失败。', type: 'error', duration: 3000 });
      } finally {
        this.workspacePreviewLoading = false;
      }
    },

    async buildWorkspaceHtmlPreview(content, filePath, isMarkdown = false) {
      const parser = new DOMParser();
      const document = parser.parseFromString(content, 'text/html');
      document.querySelectorAll('script, iframe, object, embed, form, base, meta[http-equiv="refresh"]').forEach((element) => element.remove());
      document.querySelectorAll('*').forEach((element) => {
        Array.from(element.attributes).forEach((attribute) => {
          if (attribute.name.toLowerCase().startsWith('on')) {
            element.removeAttribute(attribute.name);
          }
          if ((attribute.name === 'src' || attribute.name === 'href')
            && /^\s*javascript:/i.test(attribute.value)) {
            element.removeAttribute(attribute.name);
          }
        });
        element.removeAttribute('srcset');
      });

      const resources = [];
      document.querySelectorAll('img[src], video[src], audio[src], source[src], link[href]').forEach((element) => {
        const attribute = element.hasAttribute('src') ? 'src' : 'href';
        const value = element.getAttribute(attribute);
        if (!value || value.startsWith('#') || /^(data:|blob:|https?:|\/\/)/i.test(value)) {
          if (value && /^(https?:|\/\/)/i.test(value)) element.removeAttribute(attribute);
          return;
        }
        resources.push({ element, attribute, path: this.resolveWorkspaceAssetPath(filePath, value) });
      });

      for (const resource of resources) {
        try {
          const asset = await this.loadWorkspacePreviewAsset(resource.path);
          if (asset && asset.contentBase64) {
            let contentBase64 = asset.contentBase64;
            if (asset.contentType === 'text/css' && asset.isText && asset.content) {
              const css = await this.inlineWorkspaceCss(asset.content, resource.path);
              contentBase64 = this.encodeWorkspaceText(css);
            }
            resource.element.setAttribute(
              resource.attribute,
              `data:${asset.contentType};base64,${contentBase64}`);
          } else {
            resource.element.removeAttribute(resource.attribute);
          }
        } catch (error) {
          resource.element.removeAttribute(resource.attribute);
        }
      }

      const style = document.createElement('style');
      style.textContent = 'html,body{max-width:100%;overflow-x:hidden;}';
      if (isMarkdown) {
        style.textContent += `
          body{margin:0;padding:20px;box-sizing:border-box;color:#303133;font:14px/1.7 system-ui,sans-serif;overflow-wrap:anywhere;}
          h1,h2,h3,h4,h5,h6{line-height:1.3;margin:1.2em 0 .6em;}
          h1,h2{padding-bottom:.3em;border-bottom:1px solid #e5e9f0;}
          body>:first-child{margin-top:0;}
          p,ul,ol,pre,table,blockquote{margin:0 0 1em;}
          code{padding:2px 4px;border-radius:4px;background:#f1f3f5;font-family:monospace;}
          pre{padding:14px;overflow:auto;border-radius:6px;background:#f6f8fa;white-space:pre;}
          pre code{padding:0;background:none;}
          table{display:block;max-width:100%;overflow:auto;border-collapse:collapse;}
          th,td{padding:6px 12px;border:1px solid #e5e9f0;}
          th{background:#f6f8fa;}
          blockquote{margin-left:0;padding-left:14px;border-left:3px solid #d8dee8;color:#667085;}
          img{max-width:100%;height:auto;}
          a{color:#5267d9;}
        `;
      }
      document.head.appendChild(style);
      const csp = document.createElement('meta');
      csp.httpEquiv = 'Content-Security-Policy';
      csp.content = "default-src 'none'; img-src data:; style-src 'unsafe-inline' data:; font-src data:; media-src data:;";
      document.head.appendChild(csp);
      return '<!doctype html>' + document.documentElement.outerHTML;
    },

    async inlineWorkspaceCss(css, cssPath) {
      const urlPattern = /url\(\s*(['"]?)([^'")]+)\1\s*\)/gi;
      const matches = Array.from(css.matchAll(urlPattern));
      const replacements = await Promise.all(matches.map(async (match) => {
        const value = match[2].trim();
        if (!value || value.startsWith('#') || /^(data:|blob:|https?:|\/\/)/i.test(value)) {
          return { match: match[0], replacement: value ? `url(${value})` : match[0] };
        }

        try {
          const asset = await this.loadWorkspacePreviewAsset(
            this.resolveWorkspaceAssetPath(cssPath, value));
          return {
            match: match[0],
            replacement: asset && asset.contentBase64
              ? `url(data:${asset.contentType};base64,${asset.contentBase64})`
              : `url("")`
          };
        } catch (error) {
          return { match: match[0], replacement: 'url("")' };
        }
      }));

      return replacements.reduce(
        (result, item) => result.replace(item.match, item.replacement),
        css);
    },

    encodeWorkspaceText(text) {
      const bytes = new TextEncoder().encode(text);
      let binary = '';
      bytes.forEach((byte) => { binary += String.fromCharCode(byte); });
      return btoa(binary);
    },

    async loadWorkspacePreviewAsset(relativePath) {
      const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetWorkspaceFilePreviewAsync', {
        params: { sessionId: this.currentSessionId, relativePath: this.normalizeWorkspacePath(relativePath) }
      });
      if (!response.data || !response.data.success || !response.data.data) {
        throw new Error((response.data && response.data.msg) || 'Workspace 资产读取失败。');
      }
      return response.data.data;
    },

    resolveWorkspaceAssetPath(filePath, assetPath) {
      try {
        const base = filePath.split('/').slice(0, -1).join('/');
        const resolved = new URL(assetPath, `https://workspace.local/${base ? `${base}/` : ''}`).pathname;
        return this.normalizeWorkspacePath(decodeURIComponent(resolved.replace(/^\/+/, '')));
      } catch (error) {
        return '';
      }
    },

    normalizeWorkspacePath(path) {
      const parts = String(path || '').replace(/\\/g, '/').split('/');
      const result = [];
      parts.forEach((part) => {
        if (!part || part === '.') return;
        if (part === '..') {
          result.pop();
          return;
        }
        result.push(part);
      });
      return result.join('/');
    },

    workspaceParentPath(path) {
      const normalized = this.normalizeWorkspacePath(path);
      return normalized.includes('/') ? normalized.substring(0, normalized.lastIndexOf('/')) : '';
    },

    workspaceEntryName(path) {
      const normalized = String(path || '').replace(/\/+$/, '');
      return normalized.substring(normalized.lastIndexOf('/') + 1) || '/';
    },

    workspaceFileIcon(path) {
      const extension = this.workspaceEntryName(path).split('.').pop().toLowerCase();
      return {
        html: 'fa fa-file-code-o',
        htm: 'fa fa-file-code-o',
        css: 'fa fa-file-code-o',
        js: 'fa fa-file-code-o',
        json: 'fa fa-file-text-o',
        md: 'fa fa-file-text-o',
        png: 'fa fa-file-image-o',
        jpg: 'fa fa-file-image-o',
        jpeg: 'fa fa-file-image-o',
        gif: 'fa fa-file-image-o',
        svg: 'fa fa-file-image-o'
      }[extension] || 'fa fa-file-o';
    },

    formatWorkspaceFileSize(size) {
      if (!size) return '';
      if (size < 1024) return `${size} B`;
      if (size < 1024 * 1024) return `${Math.round(size / 1024)} KB`;
      return `${(size / (1024 * 1024)).toFixed(1)} MB`;
    },

    workspacePreviewDataUrl() {
      if (!this.workspacePreviewData || !this.workspacePreviewData.contentBase64) return '';
      return `data:${this.workspacePreviewData.contentType};base64,${this.workspacePreviewData.contentBase64}`;
    },

    startWorkspacePreviewResize(event) {
      event.preventDefault();
      this.stopWorkspacePreviewResize();
      this.updateWorkspacePreviewLayout();
      const startX = event.clientX;
      const startWidth = event.currentTarget.parentElement.getBoundingClientRect().width;
      this.workspaceResizeHandler = (moveEvent) => {
        const width = startWidth + startX - moveEvent.clientX;
        this.workspacePreviewWidth = Math.min(this.workspacePreviewMaxWidth, Math.max(320, width));
      };
      this.workspaceResizeEndHandler = () => this.stopWorkspacePreviewResize();
      document.addEventListener('mousemove', this.workspaceResizeHandler);
      document.addEventListener('mouseup', this.workspaceResizeEndHandler, { once: true });
      document.body.classList.add('is-resizing-workspace-preview');
    },

    stopWorkspacePreviewResize() {
      if (this.workspaceResizeHandler) {
        document.removeEventListener('mousemove', this.workspaceResizeHandler);
        this.workspaceResizeHandler = null;
      }
      if (this.workspaceResizeEndHandler) {
        document.removeEventListener('mouseup', this.workspaceResizeEndHandler);
        this.workspaceResizeEndHandler = null;
      }
      document.body.classList.remove('is-resizing-workspace-preview');
    },

    openUserStatsDrawer() {
      this.userStatsDrawerVisible = true;
      this.loadUserStats();
    },
    async loadUserStats() {
      this.userStatsLoading = true;
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetUserSessionStatsAsync');
        if (response.data && response.data.success && response.data.data) {
          this.userStatsList = response.data.data.stats || [];
        } else {
          this.userStatsList = [];
          this.$notify({ title: ncfT('Admin.Common.Error'), message: (response.data && response.data.msg) || ncfT('AdminChat.UserStatsLoadFailed'), type: 'error', duration: 2500 });
        }
      } catch (error) {
        console.error('加载用户用量统计失败:', error);
        this.$notify({ title: ncfT('Admin.Common.Error'), message: ncfT('AdminChat.UserStatsLoadFailed'), type: 'error', duration: 2500 });
      } finally {
        this.userStatsLoading = false;
      }
    },
    async loadAiModelOptions() {
      this.loadingAiModelOptions = true;
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetAiModelOptionsAsync');

        if (response.data && response.data.success && response.data.data) {
          this.aiKernelAvailable = !!response.data.data.aiKernelAvailable;
          this.aiModelOptions = response.data.data.models || [];
        } else {
          this.aiModelOptions = [{ id: 0, name: ncfT('AdminChat.FallbackModelName'), description: ncfT('AdminChat.FallbackModelDescription'), isDefault: true }];
        }
      } catch (error) {
        console.error('加载 AI 模型失败:', error);
        this.aiModelOptions = [{ id: 0, name: ncfT('AdminChat.FallbackModelName'), description: ncfT('AdminChat.FallbackModelDescription'), isDefault: true }];
      } finally {
        this.loadingAiModelOptions = false;
      }
    },

    handleCurrentSessionAiModelChange(value) {
      this.currentSessionAiModelId = this.normalizeAiModelId(value);
      this.setSessionAiModelId(this.currentSessionId, this.currentSessionAiModelId);
    },

    harnessStepsTitle(steps) {
      const label = (typeof ncfT === 'function' && ncfT('AdminChat.HarnessSteps')) || 'Harness';
      return label + ' · ' + (steps ? steps.length : 0);
    },

    trajectoryStatusLabel(status) {
      return {
        0: '执行中',
        1: '等待审批',
        2: '已完成',
        3: '失败',
        4: '已取消'
      }[status] || '未知状态';
    },

    trajectoryEventLabel(eventType) {
      return {
        'request': '任务请求',
        'resume': '恢复任务',
        'fork': '创建分支',
        'assistant.text': '阶段输出',
        'tool.call': '调用工具',
        'tool.result': '工具结果',
        'approval.request': '等待确认',
        'approval.response': '确认结果',
        'error': '执行错误',
        'cancelled': '已取消'
      }[eventType] || eventType || '执行事件';
    },

    canResumeMessage(message) {
      if (!message || !message.trajectoryId) return false;
      const trajectory = this.trajectoryList.find(item => item.id === message.trajectoryId);
      return !trajectory || !message.trajectorySequence || trajectory.lastSequence === message.trajectorySequence;
    },

    formatTrajectoryPayload(payload) {
      if (!payload) return '';
      try {
        return JSON.stringify(JSON.parse(payload), null, 2);
      } catch (error) {
        return payload;
      }
    },

    trajectoryEventDomId(sequence) {
      return `admin-chat-trajectory-event-${sequence}`;
    },

    moveToReplayIndex(index) {
      if (!this.selectedTrajectoryEvents.length) return;
      this.replayIndex = Math.max(0, Math.min(index, this.selectedTrajectoryEvents.length - 1));
      this.$nextTick(() => this.scrollToReplayEvent());
    },

    selectReplayEvent(index) {
      this.stopTrajectoryReplay(false);
      this.moveToReplayIndex(index);
    },

    scrollToReplayEvent() {
      const event = this.replayEvent;
      if (!event) return;

      const container = this.$refs.trajectoryTimeline;
      const target = this.$el.querySelector(`#${this.trajectoryEventDomId(event.sequence)}`);
      if (!container || !target) return;

      const containerRect = container.getBoundingClientRect();
      const targetRect = target.getBoundingClientRect();
      const targetTop = container.scrollTop
        + targetRect.top
        - containerRect.top
        - (container.clientHeight - targetRect.height) / 2;
      const maxScrollTop = Math.max(0, container.scrollHeight - container.clientHeight);
      const nextScrollTop = Math.max(0, Math.min(targetTop, maxScrollTop));

      if (typeof container.scrollTo === 'function') {
        container.scrollTo({ top: nextScrollTop, behavior: 'smooth' });
      } else {
        container.scrollTop = nextScrollTop;
      }
    },

    async loadTrajectoryList() {
      if (!this.currentSessionId) return;
      const sessionId = this.currentSessionId;
      const sequence = ++this.trajectoryListSequence;
      this.trajectoryLoading = true;
      try {
        const response = await service.get(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionTrajectoriesAsync?sessionId=${sessionId}`);
        if (sessionId !== this.currentSessionId || sequence !== this.trajectoryListSequence) return;
        if (response.data && response.data.success && response.data.data) {
          this.trajectoryList = response.data.data.trajectories || [];
        }
      } catch (error) {
        console.error('加载 Harness 轨迹失败:', error);
      } finally {
        if (sessionId === this.currentSessionId && sequence === this.trajectoryListSequence) this.trajectoryLoading = false;
      }
    },

    openTrajectoryDialog() {
      this.trajectoryDialogVisible = true;
      this.loadTrajectoryList();
    },

    async openTrajectory(trajectoryId) {
      if (!trajectoryId) return;
      const sessionId = this.currentSessionId;
      const sequence = ++this.trajectoryDetailSequence;
      this.trajectoryDialogVisible = true;
      this.trajectoryDetailLoading = true;
      try {
        const query = this.trajectorySearchText ? `&query=${encodeURIComponent(this.trajectorySearchText)}` : '';
        const response = await service.get(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetTrajectoryAsync?trajectoryId=${trajectoryId}${query}`);
        if (sessionId !== this.currentSessionId || sequence !== this.trajectoryDetailSequence) return;
        if (response.data && response.data.success && response.data.data) {
          this.selectedTrajectory = response.data.data.trajectory;
          this.selectedTrajectoryEvents = response.data.data.events || [];
          this.stopTrajectoryReplay(true);
        }
      } catch (error) {
        if (sessionId !== this.currentSessionId || sequence !== this.trajectoryDetailSequence) return;
        console.error('加载 Harness 轨迹详情失败:', error);
        this.$message.error('轨迹加载失败');
      } finally {
        if (sessionId === this.currentSessionId && sequence === this.trajectoryDetailSequence) this.trajectoryDetailLoading = false;
      }
    },

    async searchTrajectories() {
      if (!this.trajectorySearchText || !this.trajectorySearchText.trim()) {
        await this.loadTrajectoryList();
        return;
      }
      const sessionId = this.currentSessionId;
      const sequence = ++this.trajectoryListSequence;
      this.trajectoryLoading = true;
      try {
        const response = await service.get(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SearchTrajectoriesAsync?query=${encodeURIComponent(this.trajectorySearchText.trim())}`);
        if (sessionId !== this.currentSessionId || sequence !== this.trajectoryListSequence) return;
        if (response.data && response.data.success && response.data.data) {
          this.trajectoryList = response.data.data.trajectories || [];
          if (this.selectedTrajectory) {
            await this.openTrajectory(this.selectedTrajectory.id);
          }
        }
      } catch (error) {
        console.error('检索 Harness 轨迹失败:', error);
      } finally {
        if (sessionId === this.currentSessionId && sequence === this.trajectoryListSequence) this.trajectoryLoading = false;
      }
    },

    async resumeTrajectory(trajectoryId) {
      const prompt = await this.askTrajectoryInstruction('恢复 Harness 任务', '继续执行上次尚未完成的任务');
      if (prompt === null) return;
      await this.runTrajectoryAction('resume', { trajectoryId, instruction: prompt });
    },

    async forkTrajectory(trajectoryId, forkFromSequence) {
      const trajectory = this.trajectoryList.find(item => item.id === trajectoryId) || this.selectedTrajectory;
      const prompt = await this.askTrajectoryInstruction('分叉 Harness 任务', '从当前轨迹最后一步创建一个新分支');
      if (prompt === null) return;
      await this.runTrajectoryAction('fork', {
        trajectoryId,
        forkFromSequence: forkFromSequence || (trajectory ? trajectory.lastSequence : 0),
        instruction: prompt
      });
    },

    resumeSelectedTrajectory() {
      return this.selectedTrajectory ? this.resumeTrajectory(this.selectedTrajectory.id) : null;
    },

    forkSelectedTrajectory() {
      return this.selectedTrajectory ? this.forkTrajectory(this.selectedTrajectory.id) : null;
    },

    toggleTrajectoryReplay() {
      if (this.replayPlaying) {
        this.stopTrajectoryReplay();
      } else {
        this.startTrajectoryReplay();
      }
    },

    startTrajectoryReplay() {
      if (!this.selectedTrajectoryEvents.length) return;
      this.stopTrajectoryReplay(false);
      if (this.replayIndex < 0 || this.replayIndex >= this.selectedTrajectoryEvents.length - 1) {
        this.replayIndex = 0;
      }
      this.moveToReplayIndex(this.replayIndex);
      this.replayPlaying = true;
      this.replayTimer = window.setInterval(() => {
        if (this.replayIndex >= this.selectedTrajectoryEvents.length - 1) {
          this.stopTrajectoryReplay(false);
          return;
        }
        this.moveToReplayIndex(this.replayIndex + 1);
      }, 650);
    },

    stopTrajectoryReplay(resetPosition = false) {
      if (this.replayTimer) {
        window.clearInterval(this.replayTimer);
        this.replayTimer = null;
      }
      this.replayPlaying = false;
      if (resetPosition) {
        this.replayIndex = -1;
      }
    },

    askTrajectoryInstruction(title, defaultValue) {
      return this.$prompt('可以直接确认默认指令，也可以补充新的执行要求。', title, {
        confirmButtonText: '执行',
        cancelButtonText: '取消',
        inputValue: defaultValue,
        inputType: 'textarea'
      }).then(({ value }) => value).catch(() => null);
    },

    async runTrajectoryAction(action, payload) {
      if (this.isSending) return;
      const sessionId = this.currentSessionId;
      this.runningSessionId = sessionId;
      this.runningMessages = this.messageList;
      this.resetLiveRun();
      this.clearRecentTasks();
      this.isSending = true;
      this.isAIResponding = true;
      try {
        const method = action === 'fork' ? 'ForkTrajectoryAsync' : 'ResumeTrajectoryAsync';
        const response = await service.post(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.${method}`, payload);
        if (!(response.data && response.data.success && response.data.data)) {
          throw new Error(response.data && response.data.errorMessage ? response.data.errorMessage : '任务执行失败');
        }
        const data = response.data.data;
        const harness = data.harness || {};
        this.reportMemoryUpdate(data.memoryUpdateStatus, data.memoryUsage, data.sessionId || sessionId, data.memoryUpdateResult);
        await this.loadSessionList();
        if (this.currentSessionId !== sessionId) return;
        await this.loadMemoryMode();
        if (this.currentSessionId !== sessionId) return;
        if (action === 'fork' && data.sessionId) {
          await this.switchSession(data.sessionId);
          this.trajectoryDialogVisible = false;
          this.$message.success('已创建新的任务分支');
          return;
        }
        if (data.userMessage) this.messageList.push(data.userMessage);
        if (data.assistantMessage) {
          data.assistantMessage.trajectoryId = harness.trajectoryId || 0;
          data.assistantMessage.trajectorySequence = harness.trajectorySequence || 0;
          data.assistantMessage.trajectoryEvents = harness.trajectoryEvents || [];
          data.assistantMessage.pendingApprovals = harness.pendingApprovals || [];
          this.messageList.push(data.assistantMessage);
        }
        await this.loadTrajectoryList();
        if (this.currentSessionId === sessionId && harness.trajectoryId) await this.openTrajectory(harness.trajectoryId);
        this.$message.success('已继续执行任务');
      } catch (error) {
        console.error('Harness 轨迹操作失败:', error);
        this.$message.error(error.message || '任务执行失败');
      } finally {
        this.isSending = false;
        this.runningSessionId = 0;
        this.isAIResponding = false;
        this.runningMessages = [];
      }
    },

    async respondApproval(trajectoryId, approval, approved) {
      if (this.isSending) return;
      const sessionId = this.currentSessionId;
      this.runningSessionId = sessionId;
      this.runningMessages = this.messageList;
      this.resetLiveRun();
      this.clearRecentTasks();
      this.isSending = true;
      this.isAIResponding = true;
      try {
        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.RespondTrajectoryApprovalAsync', {
          trajectoryId,
          requestId: approval.requestId,
          toolCallId: approval.toolCallId,
          toolName: approval.toolName,
          argumentsJson: approval.argumentsJson || '{}',
          approved,
          reason: approved ? '管理员允许执行' : '管理员拒绝执行'
        });
        if (!(response.data && response.data.success && response.data.data)) {
          throw new Error(response.data && response.data.errorMessage ? response.data.errorMessage : '审批处理失败');
        }
        const data = response.data.data;
        const harness = data.harness || {};
        this.reportMemoryUpdate(data.memoryUpdateStatus, data.memoryUsage, data.sessionId || sessionId, data.memoryUpdateResult);
        if (this.currentSessionId !== sessionId) return;
        await this.loadMemoryMode();
        if (this.currentSessionId !== sessionId) return;
        if (data.assistantMessage) {
          data.assistantMessage.trajectoryId = harness.trajectoryId || trajectoryId;
          data.assistantMessage.trajectorySequence = harness.trajectorySequence || 0;
          data.assistantMessage.trajectoryEvents = harness.trajectoryEvents || [];
          data.assistantMessage.pendingApprovals = harness.pendingApprovals || [];
          this.messageList.push(data.assistantMessage);
        }
        await this.loadTrajectoryList();
        if (this.currentSessionId === sessionId) await this.openTrajectory(harness.trajectoryId || trajectoryId);
      } catch (error) {
        console.error('处理 Harness 审批失败:', error);
        this.$message.error(error.message || '审批处理失败');
      } finally {
        this.isSending = false;
        this.runningSessionId = 0;
        this.isAIResponding = false;
        this.runningMessages = [];
      }
    },

    handleChatInputKeydown(event) {
      // 保持与首页一致：Ctrl+Enter (Windows/Linux) 或 Cmd+Enter (Mac) 发送。
      if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
        event.preventDefault();
        this.sendMessage();
      }
      // 普通 Enter 保留换行行为。
    },

    async loadSessionList() {
      this.loadingSessions = true;
      try {
        const sessions = [];
        let totalCount;
        let pageIndex = 1;
        do {
          const response = await service.get(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionListAsync?pageIndex=${pageIndex++}&pageSize=100`);
          const data = response.data && response.data.success && response.data.data;
          if (!data || !Array.isArray(data.sessions) || !Number.isInteger(data.totalCount)
            || (data.sessions.length === 0 && sessions.length < data.totalCount)) {
            throw new Error(response.data && response.data.errorMessage || ncfT('AdminChat.LoadSessionFailed'));
          }
          sessions.push(...data.sessions);
          totalCount = data.totalCount;
        } while (sessions.length < totalCount);
        this.sessionList = Array.from(new Map(sessions.map(session => [session.id, session])).values());
      } catch (error) {
        console.error('加载会话列表异常:', error);
        this.$message.error(ncfT('AdminChat.LoadSessionFailed'));
      } finally {
        this.loadingSessions = false;
      }
    },

    async loadSessionDetail(preserveRunningMessages = true) {
      if (!this.currentSessionId) return;

      const sessionId = this.currentSessionId;
      const sequence = ++this.sessionDetailSequence;
      const runningMessages = preserveRunningMessages && this.isCurrentSessionRunning ? this.runningMessages : null;
      this.loadingMessages = true;
      try {
        const response = await service.get(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionDetailAsync?sessionId=${sessionId}`);
        if (sessionId !== this.currentSessionId || sequence !== this.sessionDetailSequence) return;
        
        if (response.data && response.data.success && response.data.data) {
          const session = response.data.data.session;
          this.currentSessionTitle = session.title;
          this.messageList = preserveRunningMessages && this.isCurrentSessionRunning
            ? this.runningMessages : runningMessages || session.messages || [];
          this.currentSessionModules = session.modules || [];
          this.currentSessionWorkflows = session.workflows || [];
          this.currentSessionWorkspaceId = session.workspaceId || '';
          this.currentSessionWorkspaceName = session.workspaceName || '';
          this.currentSessionWorkspaceWriteEnabled = !!session.workspaceWriteEnabled;
          this.currentSessionWorkspaceCliEnabled = !!session.workspaceCliEnabled;
          this.currentSessionAiModelId = this.getSessionAiModelId(this.currentSessionId);
          this.clearMessageSelection();
          this.isManageMode = false;
          await this.loadMemoryMode();
          
          this.$nextTick(() => {
            if (sessionId === this.currentSessionId && sequence === this.sessionDetailSequence) this.scrollToBottom();
          });
        } else {
          console.error('加载会话详情失败:', response.data.errorMessage);
          this.$message.error(ncfT('AdminChat.LoadDetailFailed'));
        }
      } catch (error) {
        if (sessionId !== this.currentSessionId || sequence !== this.sessionDetailSequence) return;
        console.error('加载会话详情异常:', error);
        this.$message.error(ncfT('AdminChat.LoadDetailFailed'));
      } finally {
        if (sessionId === this.currentSessionId && sequence === this.sessionDetailSequence) this.loadingMessages = false;
      }
    },

    resetLiveRun() {
      this.liveAssistantText = '';
      this.liveThinkingText = '';
      this.liveTrajectoryEvents = [];
      this.livePendingApprovals = [];
      this.liveTrajectoryId = 0;
      this.liveStatusText = '';
    },

    createRecentTask(content) {
      const taskId = `task-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
      this.recentTasks = this.recentTasks.concat({
        id: taskId,
        title: this.compactTaskText(content, 42),
        preview: '',
        status: '执行中',
        mode: this.chatMode
      }).slice(-2);
      this.activeTaskId = taskId;
    },

    updateActiveTask(changes) {
      if (!this.activeTaskId) return;
      const index = this.recentTasks.findIndex((task) => task.id === this.activeTaskId);
      if (index < 0) return;
      const current = this.recentTasks[index];
      const next = Object.assign({}, current, changes, {
        preview: changes.preview === undefined
          ? current.preview
          : this.compactTaskText(changes.preview, 120)
      });
      this.$set(this.recentTasks, index, next);
    },

    finishActiveTask(status) {
      if (!this.activeTaskId) return;
      this.updateActiveTask({ status });
      this.activeTaskId = '';
    },

    clearRecentTasks() {
      this.recentTasks = [];
      this.activeTaskId = '';
    },

    compactTaskText(text, maxLength) {
      const value = (text || '').replace(/\s+/g, ' ').trim();
      if (!value) return '';
      return value.length > maxLength ? value.slice(0, maxLength) + '…' : value;
    },

    hasUsage(usage) {
      return usage && ['inputTokens', 'outputTokens', 'totalTokens']
        .some((key) => Number(usage[key]) > 0);
    },

    formatTokenCount(value) {
      const number = Number(value);
      return Number.isFinite(number) && number >= 0 ? number.toLocaleString() : '0';
    },

    async generateSessionTitleAsync(sessionId = this.currentSessionId, aiModelId = this.currentSessionAiModelId) {
      if (!sessionId) return;
      try {
        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GenerateSessionTitleAsync', {
          sessionId,
          aiModelId: this.normalizeAiModelId(aiModelId)
        });
        if (response.data && response.data.success && response.data.data && response.data.data.title) {
          if (sessionId === this.currentSessionId) this.currentSessionTitle = response.data.data.title;
          await this.loadSessionList();
        }
      } catch (error) {
        console.error('异步生成会话标题失败:', error);
      }
    },

    handleLiveStreamEvent(eventName, payload, tempMessageId, sessionId = this.runningSessionId) {
      if (sessionId !== this.runningSessionId) return;
      if (eventName === 'user-message') {
        const tempIndex = this.runningMessages.findIndex((item) => item.id === tempMessageId);
        if (tempIndex >= 0 && payload) {
          this.runningMessages.splice(tempIndex, 1, payload);
        }
        return;
      }

      if (eventName === 'token') {
        this.liveAssistantText += payload && payload.text ? payload.text : '';
        this.liveThinkingText = '';
        this.liveStatusText = '正在生成回答…';
        this.updateActiveTask({ preview: this.liveAssistantText, status: '生成中' });
        this.scrollLivePreviewToBottom();
        return;
      }

      if (eventName === 'usage') {
        this.$set(this.sessionUsage, sessionId, payload || null);
        if (sessionId === this.currentSessionId) this.latestUsage = payload || null;
        return;
      }

      if (eventName === 'assistant-phase') {
        if (payload && payload.trajectoryId) {
          this.liveTrajectoryId = payload.trajectoryId;
        }
        if (payload && payload.trajectoryEvent) {
          const event = payload.trajectoryEvent;
          this.upsertLiveTrajectoryEvent(event);
          if (event.eventType === 'TextReasoningContent') {
            this.liveThinkingText = payload.text || event.content || '';
            this.liveStatusText = '正在思考…';
          } else {
            this.liveAssistantText = payload.text || event.content || '';
            this.liveStatusText = '正在生成回答…';
          }
          this.updateActiveTask({
            preview: event.content || payload.text || '',
            status: '生成中'
          });
          this.scrollLivePreviewToBottom();
        }
        return;
      }

      if (eventName === 'trajectory-event') {
        if (payload && payload.trajectoryId) {
          this.liveTrajectoryId = payload.trajectoryId;
        }
        if (payload && payload.trajectoryEvent) {
          if (payload.trajectoryEvent.eventType === 'assistant.text') {
            this.upsertLiveTrajectoryEvent(payload.trajectoryEvent);
          } else {
            this.liveTrajectoryEvents.push(payload.trajectoryEvent);
          }
          const eventType = payload.trajectoryEvent.eventType || '';
          if (eventType === 'assistant.text') {
            this.updateActiveTask({
              preview: payload.trajectoryEvent.content || payload.text || '',
              status: '生成中'
            });
          }
          this.liveStatusText = {
            'tool.call': '正在调用工具…',
            'tool.result': '正在整理工具结果…',
            'approval.request': '等待你的确认…',
            'assistant.text': '正在组织回答…'
          }[eventType] || '正在处理任务…';
        }
        this.livePendingApprovals = payload && payload.pendingApprovals ? payload.pendingApprovals : [];
        return;
      }

      if (eventName === 'trajectory-complete') {
        this.livePendingApprovals = payload && payload.pendingApprovals ? payload.pendingApprovals : [];
        this.liveStatusText = this.livePendingApprovals.length > 0 ? '等待你的确认…' : '任务已完成，正在保存…';
        return;
      }

      if (eventName === 'assistant-message') {
        if (payload) {
          payload.trajectoryId = payload.trajectoryId || this.liveTrajectoryId;
          payload.trajectoryEvents = this.liveTrajectoryEvents.slice();
          payload.pendingApprovals = this.livePendingApprovals.slice();
          this.runningMessages.push(payload);
        }
        this.finishActiveTask('已完成');
        return;
      }

      if (eventName === 'error') {
        this.finishActiveTask('失败');
        throw new Error(payload && payload.message ? payload.message : '对话执行失败');
      }
    },

    scrollLivePreviewToBottom() {
      const sessionId = this.runningSessionId;
      this.$nextTick(() => {
        if (sessionId !== this.currentSessionId) return;
        ['liveThinkingPreview', 'liveAnswerPreview'].forEach((refName) => {
          const element = this.$refs[refName];
          if (element) {
            element.scrollTop = element.scrollHeight;
          }
        });
      });
    },

    upsertLiveTrajectoryEvent(event) {
      const key = event.correlationId || `${event.eventType || 'event'}-${event.sequence || 0}`;
      const index = this.liveTrajectoryEvents.findIndex((item) => {
        const itemKey = item.correlationId || `${item.eventType || 'event'}-${item.sequence || 0}`;
        return itemKey === key;
      });
      if (index >= 0) {
        this.liveTrajectoryEvents.splice(index, 1, event);
      } else {
        this.liveTrajectoryEvents.push(event);
      }
    },

    async consumeAdminChatStream(requestData, tempMessageId, signal) {
      const response = await fetch('/api/Senparc.Areas.Admin/AdminChatStream/send', {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
          'Content-Type': 'application/json',
          'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify(requestData),
        signal
      });
      if (!response.ok || !response.body) {
        const data = (response.headers.get('content-type') || '').includes('json') ? await response.json() : null;
        throw new Error((data && (data.errorMessage || data.message || data.title)) || `流式接口请求失败 (${response.status})`);
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder('utf-8');
      let buffer = '';
      let streamDone = false;

      const consumeBlock = (block) => {
        const lines = block.split(/\r?\n/);
        let eventName = 'message';
        const dataLines = [];
        lines.forEach((line) => {
          if (line.startsWith('event:')) eventName = line.slice(6).trim();
          if (line.startsWith('data:')) dataLines.push(line.slice(5).trim());
        });
        if (!dataLines.length) return;
        const raw = dataLines.join('\n');
        let payload = raw;
        try { payload = JSON.parse(raw); } catch (error) { /* keep text payload */ }
        if (eventName === 'done') {
          streamDone = true;
          this.reportMemoryUpdate(payload && payload.memoryUpdateStatus, payload && payload.memoryUsage,
            requestData.sessionId, payload && payload.memoryUpdateResult);
          if (requestData.sessionId === this.currentSessionId) this.loadMemoryMode();
          return;
        }
        this.handleLiveStreamEvent(eventName, payload, tempMessageId, requestData.sessionId);
      };

      while (true) {
        const { value, done } = await reader.read();
        buffer += decoder.decode(value || new Uint8Array(), { stream: !done });
        const blocks = buffer.split(/\r?\n\r?\n/);
        buffer = blocks.pop() || '';
        blocks.forEach(consumeBlock);
        if (done) break;
      }
      if (buffer.trim()) consumeBlock(buffer);
      if (!streamDone) throw new Error('流式接口未正常结束');
    },

    stopCurrentRequest() {
      if (!this.activeRequestController || !this.isCurrentSessionRunning) return;

      this.isStopping = true;
      this.activeRequestController.abort();
    },

    async sendMessage() {
      if (this.isSending || this.workspaceUploading || this.memoryBusy || !this.memoryReady) return;

      if ((!this.inputMessage || this.inputMessage.trim().length === 0) && this.pendingAttachments.length === 0) {
        this.$message.warning(ncfT('AdminChat.InputRequired'));
        return;
      }

      if (!this.currentSessionId) {
        this.$message.error(ncfT('AdminChat.SessionRequired'));
        return;
      }

      const messageContent = (this.inputMessage || '').trim() || '请先查看本轮附件的目录和解析限制，并询问我希望如何处理。';
      const sendingAttachments = this.pendingAttachments.slice();
      const sendingSessionId = this.currentSessionId;
      const sendingAiModelId = this.currentSessionAiModelId;
      this.runningSessionId = sendingSessionId;
      this.pendingAttachments = [];
      const isFirstGeneration = !this.messageList.some((message) => message.roleType === 1);
      this.inputMessage = '';
      this.isSending = true;
      this.isStopping = false;
      this.isAIResponding = true;
      this.resetLiveRun();
      this.createRecentTask(messageContent);
      const requestController = typeof AbortController !== 'undefined'
        ? new AbortController()
        : null;
      this.activeRequestController = requestController;

      // 乐观渲染：立即显示“我”的消息，避免等待接口返回期间出现空白。
      const tempMessageId = `temp-${Date.now()}`;
      const tempUserMessage = {
        id: tempMessageId,
        roleType: 0,
        content: messageContent,
        addTime: new Date().toISOString(),
        userFeedback: 0,
        attachments: sendingAttachments
      };
      this.messageList.push(tempUserMessage);
      this.runningMessages = this.messageList;
      this.$nextTick(() => {
        this.scrollToBottom();
      });

      try {
        const requestData = {
          sessionId: this.currentSessionId,
          aiModelId: this.normalizeAiModelId(this.currentSessionAiModelId),
          content: messageContent,
          mode: this.chatMode === 'harness' ? 1 : 0,
          attachments: sendingAttachments.map(attachment => ({
            relativePath: attachment.relativePath, eTag: attachment.eTag,
            visionETag: attachment.visionETag || '', useVision: !!attachment.useVision
          }))
        };

        await this.consumeAdminChatStream(
          requestData,
          tempMessageId,
          requestController ? requestController.signal : undefined
        );
        await this.loadSessionList();
        if (this.currentSessionId === sendingSessionId) await this.loadTrajectoryList();
        if (isFirstGeneration) {
          this.generateSessionTitleAsync(sendingSessionId, sendingAiModelId);
        }
        this.$nextTick(() => {
          if (this.currentSessionId === sendingSessionId) this.scrollToBottom();
        });
      } catch (error) {
        if (this.currentSessionId === sendingSessionId) this.pendingAttachments = sendingAttachments;
        const wasAborted = (error && error.name === 'AbortError')
          || (requestController && requestController.signal.aborted);
        if (wasAborted) {
          this.finishActiveTask('已终止');
          this.runningMessages = this.runningMessages.filter((item) => item.id !== tempMessageId);
          if (this.currentSessionId === sendingSessionId) {
            this.messageList = this.runningMessages;
            this.inputMessage = messageContent;
            await this.loadSessionDetail(false);
            if (this.currentSessionId === sendingSessionId) await this.loadTrajectoryList();
          }
          this.$message.info('本次请求已终止');
        } else {
          this.finishActiveTask('失败');
          console.error('发送消息异常:', error);
          this.$message.error(error.message || ncfT('AdminChat.SendFailedRetry'));
          this.runningMessages = this.runningMessages.filter((item) => item.id !== tempMessageId);
          if (this.currentSessionId === sendingSessionId) {
            this.messageList = this.runningMessages;
            this.inputMessage = messageContent;
          }
        }
      } finally {
        sendingAttachments.filter(attachment => !this.pendingAttachments.includes(attachment)).forEach(attachment => {
          if (attachment.localPreviewUrl) URL.revokeObjectURL(attachment.localPreviewUrl);
        });
        if (this.activeRequestController === requestController) {
          this.activeRequestController = null;
        }
        this.isStopping = false;
        this.isSending = false;
        this.runningSessionId = 0;
        this.isAIResponding = false;
        this.runningMessages = [];
      }
    },

    async setFeedback(messageId, feedbackType) {
      try {
        const response = await service.put(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SetMessageFeedbackAsync?messageId=${messageId}&feedback=${feedbackType}`);
        
        if (response.data && response.data.success) {
          const message = this.messageList.find(m => m.id === messageId);
          if (message) {
            message.userFeedback = feedbackType;
          }
          this.$message.success(ncfT('AdminChat.SetFeedbackSuccess'));
        } else {
          this.$message.error(ncfT('AdminChat.FeedbackFailed'));
        }
      } catch (error) {
        console.error('设置反馈异常:', error);
        this.$message.error(ncfT('AdminChat.FeedbackFailed'));
      }
    },

    async switchSession(sessionId) {
      if (this.workspaceUploading) { this.$message.warning('请等待附件上传完成后再切换会话。'); return; }
      if (this.currentSessionId === sessionId) return;

      this.resetWorkspacePreview();
      await this.deletePendingAttachments();
      this.clearPendingAttachments();
      this.currentSessionId = sessionId;
      this.resetSessionTrajectoryView();
      this.currentSessionAiModelId = this.getSessionAiModelId(sessionId);
      this.messageList = [];
      this.currentSessionModules = [];
      this.currentSessionWorkflows = [];
      this.currentSessionWorkspaceId = '';
      this.currentSessionWorkspaceName = '';
      this.currentSessionWorkspaceWriteEnabled = false;
      this.currentSessionWorkspaceCliEnabled = false;
      if (!this.isSending) this.clearRecentTasks();
      this.latestUsage = this.sessionUsage[sessionId] || null;
      this.inputMessage = '';
      this.clearMessageSelection();
      this.isManageMode = false;
      await this.loadSessionDetail();
      if (this.currentSessionId === sessionId) await this.loadTrajectoryList();
    },

    async createNewSession() {
      if (this.workspaceUploading) { this.$message.warning('请等待附件上传完成后再新建会话。'); return; }
      this.resetWorkspacePreview();
      await this.deletePendingAttachments();
      this.clearPendingAttachments();
      this.launcherAiModelId = this.currentSessionAiModelId || this.launcherAiModelId || 0;
      this.currentSessionId = 0;
      this.resetSessionTrajectoryView();
      this.currentSessionTitle = '';
      this.currentSessionModules = [];
      this.currentSessionWorkflows = [];
      this.currentSessionWorkspaceId = '';
      this.currentSessionWorkspaceName = '';
      this.currentSessionWorkspaceWriteEnabled = false;
      this.currentSessionWorkspaceCliEnabled = false;
      if (!this.isSending) this.clearRecentTasks();
      this.latestUsage = null;
      this.messageList = [];
      this.inputMessage = '';
      this.chatInputText = '';
      this.trajectoryList = [];
      this.selectedTrajectory = null;
      this.selectedTrajectoryEvents = [];
      this.clearMessageSelection();
      this.isManageMode = false;
    },

    resetWorkspacePreview() {
      this.workspacePreviewOpen = false;
      this.workspacePreviewSource = false;
      this.workspacePreviewOverlay = false;
      this.workspaceShareDialogVisible = false;
      this.workspaceShares = [];
      this.workspaceSharePath = '';
      this.workspaceEntries = [];
      this.workspacePreviewPath = '';
      this.workspacePreviewFilePath = '';
      this.workspacePreviewData = null;
      this.workspacePreviewSrcdoc = '';
    },

    resetSessionTrajectoryView() {
      this.sessionDetailSequence++;
      this.trajectoryListSequence++;
      this.trajectoryDetailSequence++;
      this.loadingMessages = false;
      this.trajectoryLoading = false;
      this.trajectoryDetailLoading = false;
      this.trajectoryDialogVisible = false;
      this.trajectoryList = [];
      this.selectedTrajectory = null;
      this.selectedTrajectoryEvents = [];
      this.trajectorySearchText = '';
      this.stopTrajectoryReplay(true);
    },

    toggleManageMode() {
      this.isManageMode = !this.isManageMode;
      if (!this.isManageMode) {
        this.clearMessageSelection();
      }
    },

    isMessageSelected(messageId) {
      return this.selectedMessageIds.includes(String(messageId));
    },

    toggleMessageSelection(messageId) {
      const key = String(messageId);
      const index = this.selectedMessageIds.indexOf(key);
      if (index >= 0) {
        this.selectedMessageIds.splice(index, 1);
      } else {
        this.selectedMessageIds.push(key);
      }
    },

    toggleSelectAllMessages(checked) {
      if (!checked) {
        this.selectedMessageIds = [];
        return;
      }

      this.selectedMessageIds = this.messageList.map((m) => String(m.id));
    },

    clearMessageSelection() {
      this.selectedMessageIds = [];
    },

    async copySelectedMessages() {
      const selectedSet = new Set(this.selectedMessageIds);
      const selectedMessages = this.messageList.filter((m) => selectedSet.has(String(m.id)));
      if (selectedMessages.length === 0) {
        this.$message.warning(ncfT('AdminChat.SelectMessages'));
        return;
      }

      const plainText = selectedMessages
        .map((m) => `[${this.getRoleTypeName(m.roleType)}] ${m.content || ''}`)
        .join('\n\n');

      try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
          await navigator.clipboard.writeText(plainText);
        } else {
          const textarea = document.createElement('textarea');
          textarea.value = plainText;
          textarea.style.position = 'fixed';
          textarea.style.opacity = '0';
          document.body.appendChild(textarea);
          textarea.select();
          document.execCommand('copy');
          document.body.removeChild(textarea);
        }

        this.$message.success(ncfT('AdminChat.CopyCount', selectedMessages.length));
      } catch (error) {
        console.error('复制消息失败:', error);
        this.$message.error(ncfT('AdminChat.CopyFailed'));
      }
    },

    async deleteSelectedMessages() {
      const selectedIds = this.selectedMessageIds
        .map((id) => parseInt(id, 10))
        .filter((id) => Number.isInteger(id) && id > 0);

      if (selectedIds.length === 0) {
        this.$message.warning(ncfT('AdminChat.SelectMessages'));
        return;
      }

      try {
        await this.$confirm(ncfT('AdminChat.DeleteMessagesConfirm', selectedIds.length), ncfT('AdminChat.DeleteConfirmTitle'), {
          confirmButtonText: ncfT('Common.删除'),
          cancelButtonText: ncfT('Common.取消'),
          type: 'warning'
        });
      } catch (error) {
        return;
      }

      try {
        const response = await service.delete(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.DeleteMessagesAsync?sessionId=${this.currentSessionId}&messageIds=${selectedIds.join(',')}`);

        if (response.data && response.data.success) {
          const selectedSet = new Set(selectedIds.map((id) => String(id)));
          this.messageList = this.messageList.filter((m) => !selectedSet.has(String(m.id)));
          this.clearMessageSelection();
          this.$message.success(response.data.data || ncfT('AdminChat.DeleteMessagesSuccess'));
        } else {
          this.$message.error((response.data && response.data.errorMessage) || ncfT('AdminChat.DeleteFailed'));
        }
      } catch (error) {
        console.error('批量删除消息异常:', error);
        this.$message.error(ncfT('AdminChat.DeleteFailedRetry'));
      }
    },

    async handleSessionCommand(command) {
      if (command.action === 'delete') {
        this.$confirm(ncfT('AdminChat.DeleteConfirm'), ncfT('AdminChat.DeleteConfirmTitle'), {
          confirmButtonText: ncfT('Common.确认'),
          cancelButtonText: ncfT('Common.取消'),
          type: 'warning'
        }).then(async () => {
          try {
            const response = await service.delete(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.DeleteSessionAsync?sessionId=${command.id}`);
            
            if (response.data && response.data.success) {
              this.$message.success(ncfT('AdminChat.DeleteSessionSuccess'));
              
              if (this.currentSessionId === command.id) {
                this.currentSessionId = 0;
                this.messageList = [];
                this.currentSessionTitle = '';
                this.currentSessionModules = [];
                this.currentSessionWorkflows = [];
                this.currentSessionWorkspaceId = '';
                this.currentSessionWorkspaceName = '';
                this.clearRecentTasks();
                this.latestUsage = null;
              }
              
              await this.loadSessionList();
            } else {
              this.$message.error(ncfT('AdminChat.DeleteFailed'));
            }
          } catch (error) {
            console.error('删除会话异常:', error);
            this.$message.error(ncfT('AdminChat.DeleteFailed'));
          }
        }).catch(() => {
          console.log('取消删除');
        });
      }
    },

    scrollToBottom() {
      if (!this.autoScrollEnabled) return;
      
      const container = this.$refs.messagesContainer;
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
    },

    getRoleTypeClass(roleType) {
      const roleMap = {
        0: 'user',
        1: 'assistant',
        2: 'system'
      };
      return roleMap[roleType] || 'user';
    },

    getRoleTypeName(roleType) {
      const nameMap = {
        0: ncfT('AdminChat.RoleMe'),
        1: ncfT('AdminChat.RoleAssistant'),
        2: ncfT('AdminChat.RoleSystem')
      };
      return nameMap[roleType] || ncfT('AdminChat.RoleUnknown');
    },

    getMessageIcon(roleType) {
      const iconMap = {
        0: 'fa fa-user',
        1: 'fa fa-android',
        2: 'fa fa-info-circle'
      };
      return iconMap[roleType] || 'fa fa-user';
    },

    getModuleUid(module) {
      return (module && (module.xncfModuleUid || module.uid)) || '';
    },

    resolveModuleDetail(module) {
      const uid = this.getModuleUid(module);
      const matched = this.availableModules.find((item) => item.uid === uid) || null;

      return {
        uid,
        name: (module && (module.displayName || module.menuName || module.moduleName)) || (matched && matched.name) || uid || ncfT('AdminChat.UnknownModule'),
        icon: (matched && matched.icon) || 'fa fa-cube',
        description: (module && module.moduleDescription) || (matched && matched.description) || ncfT('Admin.Home.NoDescription'),
        version: (module && module.moduleVersion) || (matched && matched.version) || '',
        menus: (matched && matched.menus) || [],
        functions: (matched && matched.functions) || []
      };
    },

    resolveWorkflowDetail(workflow) {
      return {
        id: workflow && workflow.workflowId,
        name: workflow && workflow.workflowName ? workflow.workflowName : `Workflow #${workflow && workflow.workflowId}`,
        description: workflow && workflow.workflowDescription ? workflow.workflowDescription : '',
        parameters: workflow && workflow.parameters ? workflow.parameters : []
      };
    },

    formatTime(dateTimeStr) {
      if (!dateTimeStr) return '';
      
      try {
        const date = new Date(dateTimeStr);
        const now = new Date();
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return ncfT('AdminChat.JustNow');
        if (diffMins < 60) return ncfT('AdminChat.MinutesAgo', diffMins);
        if (diffHours < 24) return ncfT('AdminChat.HoursAgo', diffHours);
        if (diffDays < 7) return ncfT('AdminChat.DaysAgo', diffDays);
        
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        
        if (year === now.getFullYear()) {
          return `${month}-${day} ${hours}:${minutes}`;
        } else {
          return `${year}-${month}-${day} ${hours}:${minutes}`;
        }
      } catch (error) {
        console.error('时间格式化失败:', error);
        return dateTimeStr;
      }
    },

    formatMessageContent(content) {
      if (!content) return '';

      const normalizedContent = content.replace(
        /```([^\r\n`]+)(?:\r?\n|(?=\s*```)|$)/g,
        (fullMatch, header) => {
          const match = header.trim().match(/^([A-Za-z0-9_+#.-]+)(?:\s+([\s\S]*))?$/);
          if (!match) return fullMatch;

          const language = match[1];
          const inlineCode = match[2] ? match[2].trim() : '';
          return inlineCode
            ? `\`\`\`${language}\n${inlineCode}\n`
            : `\`\`\`${language}\n`;
        }
      );
      const rendered = marked.parse(normalizedContent, { breaks: true });
      return typeof DOMPurify === 'undefined' ? '' : DOMPurify.sanitize(rendered);
    }
  }
});
