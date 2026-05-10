var chatApp = new Vue({
  el: '#app',
  mixins: [window.ChatLauncherMixin],
  data() {
    return {
      currentSessionId: 0,
      currentSessionTitle: '',
      currentSessionModules: [],
      currentSessionAiModelId: 0,
      aiModelOptions: [],
      aiKernelAvailable: false,
      loadingAiModelOptions: false,
      sessionList: [],
      messageList: [],
      inputMessage: '',
      loadingSessions: false,
      loadingMessages: false,
      isSending: false,
      isAIResponding: false,
      currentUserId: 0,
      autoScrollEnabled: true,
      isManageMode: false,
      selectedMessageIds: []
    };
  },
  mounted() {
    if (window.INITIAL_DATA) {
      this.currentSessionId = window.INITIAL_DATA.sessionId || 0;
      this.currentUserId = window.INITIAL_DATA.currentUserId || 0;
      
      if (window.INITIAL_DATA.initialMessage) {
        const initialMessage = decodeURIComponent(window.INITIAL_DATA.initialMessage);
        if (this.currentSessionId > 0) {
          this.inputMessage = initialMessage;
        } else {
          this.chatInputText = initialMessage;
        }
      }
    }

    this.currentSessionAiModelId = this.getSessionAiModelId(this.currentSessionId);
    this.launcherAiModelId = this.currentSessionAiModelId;

    this.loadAiModelOptions();
    this.loadSessionList();
    
    if (this.currentSessionId > 0) {
      this.loadSessionDetail();
    }
  },
  methods: {
    async loadAiModelOptions() {
      this.loadingAiModelOptions = true;
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetAiModelOptionsAsync');

        if (response.data && response.data.success && response.data.data) {
          this.aiKernelAvailable = !!response.data.data.aiKernelAvailable;
          this.aiModelOptions = response.data.data.models || [];
        } else {
          this.aiModelOptions = [{ id: 0, name: '系统级 SenparcAiSetting', description: '使用 appsettings.json 中当前生效的默认 Chat 配置', isDefault: true }];
        }
      } catch (error) {
        console.error('加载 AI 模型失败:', error);
        this.aiModelOptions = [{ id: 0, name: '系统级 SenparcAiSetting', description: '使用 appsettings.json 中当前生效的默认 Chat 配置', isDefault: true }];
      } finally {
        this.loadingAiModelOptions = false;
      }
    },

    handleCurrentSessionAiModelChange(value) {
      this.currentSessionAiModelId = this.normalizeAiModelId(value);
      this.setSessionAiModelId(this.currentSessionId, this.currentSessionAiModelId);
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
        const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionListAsync?pageIndex=1&pageSize=50');
        
        if (response.data && response.data.success && response.data.data) {
          this.sessionList = response.data.data.sessions || [];
        } else {
          console.error('加载会话列表失败:', response.data.errorMessage);
        }
      } catch (error) {
        console.error('加载会话列表异常:', error);
        this.$message.error('加载会话列表失败');
      } finally {
        this.loadingSessions = false;
      }
    },

    async loadSessionDetail() {
      if (!this.currentSessionId) return;

      this.loadingMessages = true;
      try {
        const response = await service.get(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionDetailAsync?sessionId=${this.currentSessionId}`);
        
        if (response.data && response.data.success && response.data.data) {
          const session = response.data.data.session;
          this.currentSessionTitle = session.title;
          this.messageList = session.messages || [];
          this.currentSessionModules = session.modules || [];
          this.currentSessionAiModelId = this.getSessionAiModelId(this.currentSessionId);
          this.clearMessageSelection();
          this.isManageMode = false;
          
          this.$nextTick(() => {
            this.scrollToBottom();
          });
        } else {
          console.error('加载会话详情失败:', response.data.errorMessage);
          this.$message.error('加载会话详情失败');
        }
      } catch (error) {
        console.error('加载会话详情异常:', error);
        this.$message.error('加载会话详情失败');
      } finally {
        this.loadingMessages = false;
      }
    },

    async sendMessage() {
      if (!this.inputMessage || this.inputMessage.trim().length === 0) {
        this.$message.warning('请输入消息内容');
        return;
      }

      if (!this.currentSessionId) {
        this.$message.error('请先选择或创建会话');
        return;
      }

      const messageContent = this.inputMessage.trim();
      this.inputMessage = '';
      this.isSending = true;
      this.isAIResponding = true;

      // 乐观渲染：立即显示“我”的消息，避免等待接口返回期间出现空白。
      const tempMessageId = `temp-${Date.now()}`;
      const tempUserMessage = {
        id: tempMessageId,
        roleType: 0,
        content: messageContent,
        addTime: new Date().toISOString(),
        userFeedback: 0
      };
      this.messageList.push(tempUserMessage);
      this.$nextTick(() => {
        this.scrollToBottom();
      });

      try {
        const requestData = {
          sessionId: this.currentSessionId,
          aiModelId: this.normalizeAiModelId(this.currentSessionAiModelId),
          content: messageContent
        };

        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SendMessageAsync', requestData);
        
        if (response.data && response.data.success && response.data.data) {
          const { userMessage, assistantMessage } = response.data.data;

                  const tempIndex = this.messageList.findIndex((item) => item.id === tempMessageId);
                  if (tempIndex >= 0) {
                    // 用服务端正式消息替换临时消息，确保时间、ID等数据准确。
                    this.messageList.splice(tempIndex, 1, userMessage || tempUserMessage);
                  }

                  if (assistantMessage) {
                    this.messageList.push(assistantMessage);
                  }
          
          await this.loadSessionList();
          
          this.$nextTick(() => {
            this.scrollToBottom();
          });
        } else {
          console.error('发送消息失败:', response.data.errorMessage);
          this.$message.error(response.data.errorMessage || '发送消息失败');
          this.messageList = this.messageList.filter((item) => item.id !== tempMessageId);
          this.inputMessage = messageContent;
        }
      } catch (error) {
        console.error('发送消息异常:', error);
        this.$message.error('发送消息失败，请稍后重试');
        this.messageList = this.messageList.filter((item) => item.id !== tempMessageId);
        this.inputMessage = messageContent;
      } finally {
        this.isSending = false;
        this.isAIResponding = false;
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
          this.$message.success('反馈成功');
        } else {
          this.$message.error('反馈失败');
        }
      } catch (error) {
        console.error('设置反馈异常:', error);
        this.$message.error('反馈失败');
      }
    },

    async switchSession(sessionId) {
      if (this.currentSessionId === sessionId) return;

      this.currentSessionId = sessionId;
      this.currentSessionAiModelId = this.getSessionAiModelId(sessionId);
      this.messageList = [];
      this.currentSessionModules = [];
      this.clearMessageSelection();
      this.isManageMode = false;
      await this.loadSessionDetail();
    },

    async createNewSession() {
      this.launcherAiModelId = this.currentSessionAiModelId || this.launcherAiModelId || 0;
      this.currentSessionId = 0;
      this.currentSessionTitle = '';
      this.currentSessionModules = [];
      this.messageList = [];
      this.inputMessage = '';
      this.chatInputText = '';
      this.clearMessageSelection();
      this.isManageMode = false;
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
        this.$message.warning('请先选择要复制的消息');
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

        this.$message.success(`已复制 ${selectedMessages.length} 条消息`);
      } catch (error) {
        console.error('复制消息失败:', error);
        this.$message.error('复制失败，请手动复制');
      }
    },

    async deleteSelectedMessages() {
      const selectedIds = this.selectedMessageIds
        .map((id) => parseInt(id, 10))
        .filter((id) => Number.isInteger(id) && id > 0);

      if (selectedIds.length === 0) {
        this.$message.warning('请先选择要删除的消息');
        return;
      }

      try {
        await this.$confirm(`确定删除选中的 ${selectedIds.length} 条消息吗？此操作不可撤销。`, '删除确认', {
          confirmButtonText: '删除',
          cancelButtonText: '取消',
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
          this.$message.success(response.data.data || '删除成功');
        } else {
          this.$message.error((response.data && response.data.errorMessage) || '删除失败');
        }
      } catch (error) {
        console.error('批量删除消息异常:', error);
        this.$message.error('删除失败，请稍后重试');
      }
    },

    async handleSessionCommand(command) {
      if (command.action === 'delete') {
        this.$confirm('确定要删除这个会话吗？', '提示', {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning'
        }).then(async () => {
          try {
            const response = await service.delete(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.DeleteSessionAsync?sessionId=${command.id}`);
            
            if (response.data && response.data.success) {
              this.$message.success('删除成功');
              
              if (this.currentSessionId === command.id) {
                this.currentSessionId = 0;
                this.messageList = [];
                this.currentSessionTitle = '';
                this.currentSessionModules = [];
              }
              
              await this.loadSessionList();
            } else {
              this.$message.error('删除失败');
            }
          } catch (error) {
            console.error('删除会话异常:', error);
            this.$message.error('删除失败');
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
        0: '我',
        1: 'AI 助手',
        2: '系统'
      };
      return nameMap[roleType] || '未知';
    },

    getMessageIcon(roleType) {
      const iconMap = {
        0: 'fa fa-user',
        1: 'fa fa-robot',
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
        name: (module && (module.displayName || module.menuName || module.moduleName)) || (matched && matched.name) || uid || '未命名模块',
        icon: (matched && matched.icon) || 'fa fa-cube',
        description: (module && module.moduleDescription) || (matched && matched.description) || '暂无描述',
        version: (module && module.moduleVersion) || (matched && matched.version) || '',
        menus: (matched && matched.menus) || [],
        functions: (matched && matched.functions) || []
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

        if (diffMins < 1) return '刚刚';
        if (diffMins < 60) return `${diffMins}分钟前`;
        if (diffHours < 24) return `${diffHours}小时前`;
        if (diffDays < 7) return `${diffDays}天前`;
        
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
      
      let formatted = content
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#x27;')
        .replace(/\//g, '&#x2F;');
      
      formatted = formatted
        .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
        .replace(/\*(.+?)\*/g, '<em>$1</em>')
        .replace(/`(.+?)`/g, '<code>$1</code>')
        .replace(/\n/g, '<br/>');
      
      return formatted;
    }
  }
});
