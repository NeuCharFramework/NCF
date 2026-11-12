function workspaceCodeWords(text, ignoreCase) {
  const values = text.split(/\s+/).filter(Boolean);
  return new Set(ignoreCase ? values.map(value => value.toLowerCase()) : values);
}

function workspaceCodeProfiles() {
  const cLike = (keywords, ignoreCase) => ({
    lines: ['//'], block: ['/*', '*/'], keywords: workspaceCodeWords(keywords, ignoreCase)
  });
  return {
    python: {
      lines: ['#'],
      keywords: workspaceCodeWords('False None True and as assert async await break class continue def del elif else except finally for from global if import in is lambda nonlocal not or pass raise return try while with yield')
    },
    javascript: cLike('async await break case catch class const continue debugger default delete do else export extends false finally for from function if import in instanceof let new null of return static super switch this throw true try typeof var void while with yield'),
    csharp: cLike('abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while'),
    java: cLike('abstract assert boolean break byte case catch char class const continue default do double else enum extends final finally float for goto if implements import instanceof int interface long native new null package private protected public return short static strictfp super switch synchronized this throw throws transient true false try void volatile while'),
    go: cLike('break case chan const continue default defer else fallthrough for func go goto if import interface map package range return select struct switch type var true false nil'),
    rust: cLike('as async await break const continue crate dyn else enum extern false fn for if impl in let loop match mod move mut pub ref return self Self static struct super trait true type unsafe use where while'),
    ruby: { lines: ['#'], keywords: workspaceCodeWords('BEGIN END alias and begin break case class def defined do else elsif end ensure false for if in module next nil not or redo rescue retry return self super then true undef unless until when while yield') },
    php: { lines: ['//', '#'], block: ['/*', '*/'], keywords: workspaceCodeWords('abstract and array as break callable case catch class clone const continue declare default die do echo else elseif empty enddeclare endfor endforeach endif endswitch endwhile eval exit extends false final finally fn for foreach function global goto if implements include include_once instanceof insteadof interface isset list match namespace new null or print private protected public require require_once return static switch throw trait true try unset use var while xor yield') },
    shell: { lines: ['#'], keywords: workspaceCodeWords('if then else elif fi for while do done case esac function return exit echo local export') },
    powershell: { lines: ['#'], block: ['<#', '#>'], keywords: workspaceCodeWords('if else elseif for foreach while do until switch function return param begin process end try catch finally throw') },
    sql: { lines: ['--'], block: ['/*', '*/'], ignoreCase: true, keywords: workspaceCodeWords('select from where and or not insert update delete create drop table join left right inner outer on group by order having limit as null is in like between distinct values set into', true) },
    css: { block: ['/*', '*/'], keywords: new Set() },
    json: { keywords: workspaceCodeWords('true false null') },
    yaml: { lines: ['#'], keywords: workspaceCodeWords('true false null yes no') },
    toml: { lines: ['#'], keywords: new Set() },
    markup: { block: ['<!--', '-->'], keywords: new Set() },
    lua: { lines: ['--'], block: ['--[[', ']]'], keywords: workspaceCodeWords('and break do else elseif end false for function goto if in local nil not or repeat return then true until while') },
    plain: { keywords: new Set() }
  };
}

const WORKSPACE_LINE_HEIGHT = 20;
const WORKSPACE_VIRTUAL_THRESHOLD = 600;
const WORKSPACE_VISIBLE_LINES = 160;
const WORKSPACE_PREVIEW_PAGE_LINES = 800;

function highlightWorkspaceLine(line, language, inBlock, capture) {
  const profiles = highlightWorkspaceSource.profiles || (highlightWorkspaceSource.profiles = workspaceCodeProfiles());
  const profile = profiles[language] || profiles.plain;
  const text = String(line == null ? '' : line);
  const write = capture !== false;
  const parts = write ? [] : null;
  const push = (chunk, cls) => {
    if (!write || !chunk) return;
    const escaped = chunk.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    parts.push(cls ? `<span class="${cls}">${escaped}</span>` : escaped);
  };
  const isIdentStart = ch => ch === '_' || ch === '$' || (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
  const isIdent = ch => isIdentStart(ch) || (ch >= '0' && ch <= '9');
  const isDigit = ch => ch >= '0' && ch <= '9';
  const done = block => ({ html: write ? parts.join('') : '', inBlock: !!block });
  let i = 0;
  let block = !!inBlock;
  const blockOpen = profile.block && profile.block[0];
  const blockClose = profile.block && profile.block[1];
  if (block && blockClose) {
    const end = text.indexOf(blockClose, i);
    if (end < 0) {
      push(text, 'tok-comment');
      return done(true);
    }
    push(text.slice(0, end + blockClose.length), 'tok-comment');
    i = end + blockClose.length;
    block = false;
  }
  while (i < text.length) {
    const markers = profile.lines || [];
    const marker = markers.find(item => text.startsWith(item, i) && (item !== '//' || i === 0 || text[i - 1] !== ':'));
    if (marker) {
      push(text.slice(i), 'tok-comment');
      return done(false);
    }
    if (blockOpen && text.startsWith(blockOpen, i)) {
      const end = text.indexOf(blockClose, i + blockOpen.length);
      if (end < 0) {
        push(text.slice(i), 'tok-comment');
        return done(true);
      }
      push(text.slice(i, end + blockClose.length), 'tok-comment');
      i = end + blockClose.length;
      continue;
    }
    const quote = text[i];
    if (quote === '"' || quote === "'" || quote === '`') {
      const triple = quote !== '`' && text[i + 1] === quote && text[i + 2] === quote;
      let j = i + (triple ? 3 : 1);
      while (j < text.length) {
        if (text[j] === '\\') { j += 2; continue; }
        if (triple && text[j] === quote && text[j + 1] === quote && text[j + 2] === quote) { j += 3; break; }
        if (!triple && text[j] === quote) { j += 1; break; }
        j += 1;
      }
      push(text.slice(i, j), 'tok-string');
      i = j;
      continue;
    }
    if (isDigit(text[i])) {
      let j = i + 1;
      const hex = text[i] === '0' && (text[j] === 'x' || text[j] === 'X');
      if (hex) j += 1;
      while (j < text.length) {
        const ch = text[j];
        if (isDigit(ch) || ch === '_' || ch === '.') { j += 1; continue; }
        if (hex && ((ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))) { j += 1; continue; }
        break;
      }
      push(text.slice(i, j), 'tok-number');
      i = j;
      continue;
    }
    if (isIdentStart(text[i])) {
      let j = i + 1;
      while (j < text.length && isIdent(text[j])) j += 1;
      const word = text.slice(i, j);
      const lookup = profile.ignoreCase ? word.toLowerCase() : word;
      push(word, profile.keywords.has(lookup) ? 'tok-keyword' : '');
      i = j;
      continue;
    }
    push(text[i], '');
    i += 1;
  }
  return done(block);
}

function workspacePreviewLineList(source) {
  const normalized = String(source || '').replace(/\r\n/g, '\n').replace(/\r/g, '\n');
  const lines = normalized.split('\n');
  if (lines.length > 1 && lines[lines.length - 1] === '') lines.pop();
  return lines;
}

function highlightWorkspaceLines(source, language, startLine) {
  const lines = workspacePreviewLineList(source);
  const origin = Math.max(1, startLine || 1);
  let inBlock = false;
  return lines.map((line, index) => {
    const highlighted = highlightWorkspaceLine(line, language, inBlock, true);
    inBlock = highlighted.inBlock;
    return `<span class="code-line" data-ln="${origin + index}"><span class="code-text">${highlighted.html}</span></span>`;
  }).join('');
}

function highlightWorkspaceSource(source, language) {
  return `<pre class="code-preview"><code>${highlightWorkspaceLines(source, language, 1)}</code></pre>`;
}

function workspaceBlockStates(lines, language) {
  const states = new Uint8Array(lines.length);
  let inBlock = false;
  for (let index = 0; index < lines.length; index += 1) {
    states[index] = inBlock ? 1 : 0;
    inBlock = highlightWorkspaceLine(lines[index], language, inBlock, false).inBlock;
  }
  return states;
}

let workspaceJsonNodeSeq = 1;

function workspaceJsonNode(key, value) {
  const id = 'json-' + (workspaceJsonNodeSeq++);
  if (value && value.__workspaceInvalidJson) {
    return { id, key: '第 ' + value.lineNumber + ' 行', type: 'invalid', text: value.raw };
  }
  if (value === null) return { id, key, type: 'null', text: 'null' };
  if (Array.isArray(value)) return { id, key, type: 'array', size: value.length, value, open: false };
  if (typeof value === 'object') {
    return { id, key, type: 'object', size: Object.keys(value).length, keys: Object.keys(value), value, open: false };
  }
  if (typeof value === 'string') return { id, key, type: 'string', text: value };
  if (typeof value === 'number') return { id, key, type: 'number', text: String(value) };
  if (typeof value === 'boolean') return { id, key, type: 'boolean', text: String(value) };
  return { id, key, type: 'string', text: String(value) };
}

function workspaceJsonChild(node, index) {
  if (!node.cache) node.cache = new Array(node.size);
  if (!node.cache[index]) {
    if (node.type === 'array') node.cache[index] = workspaceJsonNode(String(index), node.value[index]);
    else node.cache[index] = workspaceJsonNode(node.keys[index], node.value[node.keys[index]]);
  }
  return node.cache[index];
}

function workspaceJsonCollapsed(node) {
  if (node.type === 'array') return node.size ? 'Array(' + node.size + ')' : '[]';
  if (!node.size) return '{}';
  const sample = node.keys.slice(0, 3).map(key => JSON.stringify(key) + ': …').join(', ');
  return node.size > 3 ? '{' + sample + ', …}' : '{' + sample + '}';
}

function buildWorkspaceJsonRoot(content, jsonLines) {
  let value;
  if (jsonLines) {
    value = [];
    String(content || '').split('\n').forEach((line, index) => {
      if (!line.trim()) return;
      try {
        value.push(JSON.parse(line));
      } catch (error) {
        value.push({
          __workspaceInvalidJson: true,
          lineNumber: index + 1,
          raw: line.length > 180 ? line.slice(0, 180) + '…' : line
        });
      }
    });
  } else {
    value = JSON.parse(content);
  }
  const root = workspaceJsonNode(null, value);
  if (root.size != null) {
    root.open = true;
    root.shown = Math.min(root.size, 200);
  }
  return root;
}

function flattenWorkspaceJson(root) {
  const rows = [];
  const walk = (node, depth) => {
    rows.push({ id: node.id, depth, node, closing: false });
    if (node.size == null || !node.open) return;
    const shown = Math.min(node.size, node.shown || Math.min(node.size, 200));
    node.shown = shown;
    for (let index = 0; index < shown; index += 1) walk(workspaceJsonChild(node, index), depth + 1);
    if (shown < node.size) {
      rows.push({ id: node.id + '-more', depth: depth + 1, more: true, remaining: node.size - shown, node });
    }
    rows.push({ id: node.id + '-end', depth, node, closing: true });
  };
  if (root) walk(root, 0);
  return rows;
}

function logAdminChat(level, message, detail) {
  if (typeof console === 'undefined' || !console) return;
  const write = console[level] || console.error || console.log;
  if (typeof write === 'function') write.call(console, message, detail === undefined ? '' : detail);
}

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
      memoryProposalReview: null,
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
      archivedSessionList: [],
      sidebarToolsVisible: false,
      historySearchVisible: false,
      historySearchTab: 'chat',
      historySearchKeyword: '',
      historySearchHits: [],
      historySearchLoading: false,
      historySearchSubmitted: false,
      historySearchTimer: null,
      historySearchSequence: 0,
      historySearchFlashTimer: null,
      pendingHistorySearchMessageId: 0,
      archivingSessionId: 0,
      restoringSessionId: 0,
      currentSessionStatus: 0,
      messageRail: [],
      messageRailOpen: false,
      messageRailActiveId: 0,
      messageRailMenuTop: 0,
      messageRailMenuHover: false,
      messageRailCloseTimer: null,
      messageRailObserver: null,
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
      sessionRuns: {},
      sessionDrafts: {},
      unreadSessionIds: [],
      unreadPersistGeneration: {},
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
      workspacePreviewEditing: false,
      workspacePreviewDraft: '',
      workspacePreviewSaving: false,
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
      workspacePreviewJsonTree: false,
      workspaceJsonRoot: null,
      workspaceJsonRevision: 0,
      workspacePreviewLines: [],
      workspacePreviewLineOffset: 0,
      workspacePreviewVirtual: false,
      workspaceLineBlockStates: null,
      workspacePreviewCodeHtml: '',
      workspacePreviewJumpLine: 1,
      workspacePreviewWindowLoading: false,
      workspaceEntriesLoading: false,
      workspaceMention: {
        open: false, field: '', start: 0, end: 0, query: '', items: [], active: 0,
        loading: false, error: '', sequence: 0, loadedQuery: null
      },
      workspaceMentionTimer: null,
      workspaceMentionApplying: false,
      workspacePreviewLoading: false,
      workspaceResizeHandler: null,
      workspaceResizeEndHandler: null
    };
  },
  computed: {
    isCurrentSessionRunning() {
      return !!this.sessionRuns[this.currentSessionId]
        || (this.isSending && this.runningSessionId === this.currentSessionId);
    },
    hasRunningSession() {
      return Object.keys(this.sessionRuns).length > 0
        || (this.isSending && Number(this.runningSessionId) > 0);
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
    workspacePreviewIsCode() {
      const data = this.workspacePreviewData;
      if (!data || !data.isText || this.workspacePreviewIsDocument) return false;
      return data.contentType !== 'image/svg+xml';
    },
    workspacePreviewIsJson() {
      const type = this.workspacePreviewData && this.workspacePreviewData.contentType;
      return type === 'application/json' || type === 'application/jsonl';
    },
    workspacePreviewShowsLines() {
      if (this.workspacePreviewEditing || this.workspacePreviewSource || this.workspacePreviewJsonTree) return false;
      return this.workspacePreviewIsCode;
    },
    workspacePreviewWindowBar() {
      if (this.workspacePreviewEditing || !this.workspacePreviewData || !this.workspacePreviewData.isText) return false;
      return !!this.workspacePreviewData.windowed || !!this.workspacePreviewVirtual;
    },
    workspacePreviewWindowEnd() {
      const start = (this.workspacePreviewData && this.workspacePreviewData.startLine) || 1;
      const count = (this.workspacePreviewLines || []).length;
      return count ? start + count - 1 : start;
    },
    workspacePreviewWindowLabel() {
      const data = this.workspacePreviewData || {};
      const total = (this.workspacePreviewLines || []).length;
      const start = data.startLine || 1;
      const end = total ? start + total - 1 : start;
      if (data.windowed) {
        const truncated = data.truncated ? ' · 超长行已截断' : '';
        return `第 ${start}–${end} 行 · ${this.formatWorkspaceFileSize(data.size)} · 大文件按片段加载${truncated}`;
      }
      return `共 ${total} 行 · 只渲染可见区域`;
    },
    workspaceLineBottomPad() {
      const total = (this.workspacePreviewLines || []).length;
      const consumed = (this.workspacePreviewLineOffset || 0) + WORKSPACE_VISIBLE_LINES;
      return Math.max(0, total - consumed) * WORKSPACE_LINE_HEIGHT;
    },
    workspacePreviewCanShowSource() {
      return !!this.workspacePreviewData && this.workspacePreviewData.isText
        && !this.workspacePreviewEditing
        && (this.workspacePreviewIsDocument || this.workspacePreviewIsCode || this.workspacePreviewData.contentType === 'image/svg+xml');
    },
    workspacePreviewCanEdit() {
      return !!this.workspacePreviewData && this.workspacePreviewData.isText
        && !this.workspacePreviewData.windowed
        && !this.workspacePreviewEditing;
    },
    workspacePreviewDirty() {
      if (!this.workspacePreviewEditing || !this.workspacePreviewData) return false;
      return this.workspacePreviewDraft !== (this.workspacePreviewData.content || '');
    },
    workspacePreviewFrameTitle() {
      const type = this.workspacePreviewData && this.workspacePreviewData.contentType;
      if (type === 'text/markdown') return 'Workspace Markdown 预览';
      if (type === 'text/html') return 'Workspace HTML 预览';
      if (this.workspacePreviewIsCode) return 'Workspace 代码预览';
      return 'Workspace 预览';
    },
    workspaceCodeLanguage() {
      const name = this.workspaceEntryName((this.workspacePreviewData && this.workspacePreviewData.path) || this.workspacePreviewFilePath).toLowerCase();
      const extension = name.includes('.') ? name.slice(name.lastIndexOf('.') + 1) : name;
      return {
        py: 'python', pyw: 'python', pyi: 'python',
        js: 'javascript', mjs: 'javascript', cjs: 'javascript', jsx: 'javascript',
        ts: 'javascript', tsx: 'javascript', mts: 'javascript', cts: 'javascript', jsonc: 'javascript', json5: 'javascript',
        cs: 'csharp', csx: 'csharp',
        java: 'java', kt: 'java', kts: 'java', scala: 'java', gradle: 'java', dart: 'java', swift: 'java',
        go: 'go', rs: 'rust', rb: 'ruby', php: 'php', gemfile: 'ruby', rakefile: 'ruby',
        sh: 'shell', bash: 'shell', zsh: 'shell', bat: 'shell', cmd: 'shell', dockerfile: 'shell', makefile: 'shell', jenkinsfile: 'shell',
        ps1: 'powershell', psm1: 'powershell',
        sql: 'sql', css: 'css', scss: 'css', sass: 'css', less: 'css',
        json: 'json', jsonl: 'json', ndjson: 'json', map: 'json', ipynb: 'json',
        yml: 'yaml', yaml: 'yaml', toml: 'toml',
        html: 'markup', htm: 'markup', xml: 'markup', vue: 'markup', cshtml: 'markup', razor: 'markup',
        svg: 'markup', csproj: 'markup', lua: 'lua'
      }[extension] || 'plain';
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
  watch: {
    messageList() {
      this.$nextTick(() => {
        this.observeMessageRail();
        this.refreshMessageRail();
      });
    },
    historySearchVisible(visible) {
      if (!visible) return;
      this.$nextTick(() => {
        const input = this.$refs.historySearchInput;
        if (input && typeof input.focus === 'function') input.focus();
      });
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
    if (!(Number(this.currentSessionId) > 0)) {
      const linkedSessionId = this.sessionIdFromLocation();
      if (linkedSessionId > 0) this.currentSessionId = linkedSessionId;
    }
    this.syncAdminChatLocation();
    const initialMessageKey = `ncf.admin.chat.initialMessage.${this.currentSessionId}`;
    const initialMessageParam = this.currentSessionId > 0 ? sessionStorage.getItem(initialMessageKey) : '';
    if (initialMessageParam) {
      sessionStorage.removeItem(initialMessageKey);
      this.inputMessage = initialMessageParam;
      this.pendingInitialSend = true;
      const referenceKey = `ncf.admin.chat.initialReferences.${this.currentSessionId}`;
      const references = sessionStorage.getItem(referenceKey);
      sessionStorage.removeItem(referenceKey);
      if (references) {
        try {
          this.pendingReferenceRestore = this.restorePromptReferences(
            'promptMessageInput', JSON.parse(references));
        } catch (error) {
          this.$message.error(error.message);
        }
      }
    }

    this.currentSessionAiModelId = this.getSessionAiModelId(this.currentSessionId);
    this.launcherAiModelId = this.currentSessionAiModelId;
    this.loadSidebarPreferences();

    this.loadAiModelOptions();
    this.loadSessionList();
    this.loadArchivedSessionList();
    this.bindChatSettingsDismiss();
    
      if (this.currentSessionId > 0) {
      this.loadSessionDetail().then(async () => {
        this.loadTrajectoryList();
        await this.resumeAdminChatRun();
        if (this.pendingReferenceRestore) await this.pendingReferenceRestore;
        if (this.pendingInitialSend && !this.isSending) {
          this.pendingInitialSend = false;
          this.sendMessage();
        } else {
          this.pendingInitialSend = false;
        }
      });
    }
  },
  beforeDestroy() {
    if (this.messageRailObserver) this.messageRailObserver.disconnect();
    clearTimeout(this.messageRailCloseTimer);
    clearTimeout(this.historySearchTimer);
    clearTimeout(this.historySearchFlashTimer);
    this.workspacePreviewResizeObserver.disconnect();
    Object.keys(this.sessionRuns).forEach((id) => {
      const controller = this.sessionRuns[id] && this.sessionRuns[id].controller;
      if (controller) controller.abort();
    });
    if (this.activeRequestController) {
      // 刷新或离开页面只断开观察，不请求 cancel，服务端任务继续执行。
      this.activeRequestController.abort();
      this.activeRequestController = null;
    }
    this.stopWorkspacePreviewResize();
    this.stopSidebarResize();
    this.clearPendingAttachments();
    this.unbindChatSettingsDismiss();
  },
  methods: {
    bindChatSettingsDismiss() {
      if (this.chatSettingsPointerHandler) return;
      this.chatSettingsPointerHandler = (event) => {
        const openPanel = document.querySelector('details.chat-inline-settings[open]');
        if (!openPanel) return;
        const target = event.target;
        // 模型下拉、Memory 菜单和说明弹层都挂在 body 上，点它们不算离开设置面板。
        if (target && target.closest && target.closest('details.chat-inline-settings, .el-popper, .el-select-dropdown, .el-dropdown-menu, .el-dialog__wrapper, .el-message-box__wrapper, .ncf-doc-help-dialog')) {
          return;
        }
        openPanel.open = false;
        console.debug('[NCF][AdminChat] 点击页面其他区域，关闭对话设置');
      };
      this.chatSettingsBlurHandler = () => {
        const openPanel = document.querySelector('details.chat-inline-settings[open]');
        if (!openPanel) return;
        openPanel.open = false;
        console.debug('[NCF][AdminChat] 焦点离开对话页面，关闭对话设置');
      };
      document.addEventListener('pointerdown', this.chatSettingsPointerHandler);
      window.addEventListener('blur', this.chatSettingsBlurHandler);
    },
    unbindChatSettingsDismiss() {
      if (this.chatSettingsPointerHandler) {
        document.removeEventListener('pointerdown', this.chatSettingsPointerHandler);
        this.chatSettingsPointerHandler = null;
      }
      if (this.chatSettingsBlurHandler) {
        window.removeEventListener('blur', this.chatSettingsBlurHandler);
        this.chatSettingsBlurHandler = null;
      }
    },
    closeInlineChatSettings(event) {
      const host = event && event.currentTarget && event.currentTarget.closest
        ? event.currentTarget.closest('details.chat-inline-settings')
        : document.querySelector('details.chat-inline-settings[open]');
      if (host) host.open = false;
      console.debug('[NCF][AdminChat] 关闭对话设置');
    },
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
      return !!session && (!!this.sessionRuns[session.id]
        || (this.isSending && this.runningSessionId === session.id));
    },
    isSessionUnread(session) {
      return !!session && !this.isSessionRunning(session) && this.unreadSessionIds.includes(session.id);
    },
    markSessionUnread(sessionId) {
      const id = Number(sessionId);
      if (!id || id === this.currentSessionId || this.unreadSessionIds.includes(id)) return Promise.resolve();
      this.unreadSessionIds.push(id);
      console.info('AdminChat session unread:', id);
      return this.persistSessionUnread(id, true);
    },
    clearSessionUnread(sessionId) {
      const id = Number(sessionId);
      if (!id || !this.unreadSessionIds.includes(id)) return Promise.resolve();
      this.unreadSessionIds = this.unreadSessionIds.filter(item => item !== id);
      return this.persistSessionUnread(id, false);
    },
    applyPersistedUnread(sessions) {
      const current = Number(this.currentSessionId) || 0;
      (sessions || []).forEach(session => {
        const id = Number(session && session.id);
        if (!id) return;
        const unread = session.hasUnread === true || session.hasUnread === 1;
        if (id === current) {
          this.unreadSessionIds = this.unreadSessionIds.filter(item => item !== id);
          if (unread) this.persistSessionUnread(id, false);
          session.hasUnread = false;
          return;
        }
        if (unread && !this.unreadSessionIds.includes(id)) this.unreadSessionIds.push(id);
        if (!unread && this.unreadSessionIds.includes(id) && !(this.unreadPersistPending && this.unreadPersistPending[id])) {
          this.unreadSessionIds = this.unreadSessionIds.filter(item => item !== id);
        }
      });
    },
    persistSessionUnread(sessionId, unread) {
      const id = Number(sessionId);
      if (!id || typeof service === 'undefined' || !service || typeof service.post !== 'function') return Promise.resolve();
      const generation = (this.unreadPersistGeneration[id] || 0) + 1;
      this.unreadPersistGeneration[id] = generation;
      if (!this.unreadPersistPending) this.unreadPersistPending = {};
      this.unreadPersistPending[id] = unread;
      const session = (this.sessionList || []).find(item => item.id === id);
      if (session) session.hasUnread = unread;
      console.info('AdminChat persist unread:', id, unread);
      return service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SetSessionUnreadAsync', {
        sessionId: id,
        unread: unread
      }).then(response => {
        if (!(response && response.data && response.data.success)) {
          console.error('AdminChat unread persist failed:', id, response && response.data);
          return;
        }
        if (this.unreadPersistGeneration[id] !== generation) {
          const latest = this.unreadSessionIds.includes(id);
          if (latest !== unread) return this.persistSessionUnread(id, latest);
          return;
        }
        if (this.unreadPersistPending) this.unreadPersistPending[id] = false;
      }).catch(error => {
        console.error('AdminChat unread persist failed:', id, error);
      });
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
      if (status && !['disabled', 'updated', 'unchanged', 'read-only', 'skipped-input', 'pending-review'].includes(status)) {
        this.$message.warning(ncfT('AdminChat.Memory.UpdateWarning') + ' (' + status + ')');
      }
    },

    memoryUpdateText(result) {
      if (!result) return '';
      if (result.status === 'updated') {
        return ncfT('AdminChat.Memory.UpdateSaved', result.globalChanges, result.workspaceChanges);
      }
      const knownReasons = ['mode-off', 'read-only', 'input-not-eligible', 'no-candidates',
        'pinned', 'no-difference', 'revision-conflict', 'timeout', 'invalid-change', 'capacity', 'invalid-json', 'pending',
        'pending-review', 'unresolved-reference', 'review-complete', 'intent-deferred', 'no-save-intent'];
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
      if (this.memoryBusy || this.isCurrentSessionRunning) return;
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
        this.memoryProposalReview = null;
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
      this.memoryProposalReview = null;
      this.memoryEntryDraft = entry ? {
        id: entry.id, version: entry.version, text: entry.text, topic: entry.topic,
        tier: entry.tier, kind: entry.kind, pinned: entry.pinned,
        scope: entry.workspaceId ? 'workspace' : 'global', expiresAt: entry.expiresAt, remove: false
      } : {
        id: '', version: 0, text: '', topic: '', tier: 'core', kind: 'preference',
        pinned: true, scope: 'global', expiresAt: null, remove: false
      };
    },

    async reviewMemoryProposal(proposal) {
      const result = this.memoryUpdateResult;
      const sessionId = this.currentSessionId;
      if (this.memoryBusy || this.isCurrentSessionRunning || !result || this.memoryUpdateSessionId !== sessionId
          || !(result.proposals || []).includes(proposal)) return;
      if (this.memoryDialogVisible) {
        this.$message.warning(ncfT('AdminChat.Memory.ReviewClose'));
        return;
      }
      await this.openMemoryDialog();
      if (!this.memoryDialogVisible || this.currentSessionId !== sessionId
          || this.memoryDialogSessionId !== sessionId || this.memoryUpdateResult !== result) return;
      if (this.memoryDraftRevision !== result.revision) {
        this.$message.warning(ncfT('AdminChat.Memory.UpdateReason.revision-conflict'));
        return;
      }
      this.editMemoryEntry(proposal.entry);
      this.memoryTab = 'entries';
      this.memoryProposalReview = { result, proposal, draft: this.memoryEntryDraft };
    },

    async saveMemoryEntry(remove = false) {
      if (!this.memoryEntryDraft || this.memoryBusy) return;
      if (this.memoryMarkdown !== this.memorySavedMarkdown) {
        this.$message.warning(ncfT('AdminChat.Memory.UnsavedMarkdown'));
        return;
      }
      const review = this.memoryProposalReview;
      const reviewing = review && review.draft === this.memoryEntryDraft;
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
        this.memoryProposalReview = null;
        this.memoryMarkdown = data.markdown;
        this.memorySavedMarkdown = data.markdown;
        if (this.memoryDialogSessionId === this.currentSessionId) this.memoryRevision = data.revision;
        if (reviewing && this.memoryUpdateResult === review.result) {
          const proposals = review.result.proposals.filter(proposal => proposal !== review.proposal);
          this.memoryUpdateResult = {
            ...review.result, revision: data.revision, proposals,
            status: proposals.length ? 'pending-review' : 'reviewed',
            reason: proposals.length ? 'pending-review' : 'review-complete'
          };
        }
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
      const promptReferences = this.capturePromptReferences('promptLauncherInput');
      const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.CreateSessionAsync',
        this.launcherSessionRequest((this.chatInputText || '').trim().slice(0, 60) || '附件对话'));
      if (!response.data || !response.data.success || !response.data.data || !response.data.data.sessionId) {
        throw new Error((response.data && (response.data.msg || response.data.errorMessage)) || '创建附件会话失败。');
      }
      this.currentSessionId = response.data.data.sessionId;
      this.currentSessionAiModelId = this.normalizeAiModelId(this.launcherAiModelId);
      this.setSessionAiModelId(this.currentSessionId, this.currentSessionAiModelId);
      this.chatMode = this.launcherChatMode;
      this.syncAdminChatLocation();
      this.inputMessage = this.chatInputText;
      this.chatInputText = '';
      await this.restorePromptReferences('promptMessageInput', promptReferences);
      await this.loadSessionDetail();
      if (!this.currentSessionWorkspaceId) throw new Error('会话已创建，但没有读到 Workspace，请刷新后重试。');
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
      if (this.workspaceUploading || this.isCurrentSessionRunning) {
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

    async confirmDiscardWorkspaceEdit() {
      if (!this.workspacePreviewDirty) return true;
      if (typeof this.$confirm !== 'function') return true;
      try {
        await this.$confirm('当前文件有未保存的修改，确定放弃吗？', '放弃修改', {
          type: 'warning',
          confirmButtonText: '放弃修改',
          cancelButtonText: '继续编辑'
        });
        return true;
      } catch (error) {
        return false;
      }
    },

    beginWorkspacePreviewEdit() {
      if (!this.workspacePreviewData || !this.workspacePreviewData.isText || this.workspacePreviewData.windowed) return;
      this.workspacePreviewDraft = this.workspacePreviewData.content || '';
      this.workspacePreviewEditing = true;
      this.workspacePreviewSource = false;
      this.workspacePreviewJsonTree = false;
    },

    cancelWorkspacePreviewEdit() {
      this.workspacePreviewEditing = false;
      this.workspacePreviewDraft = this.workspacePreviewData ? (this.workspacePreviewData.content || '') : '';
    },

    async closeWorkspacePreview() {
      if (!(await this.confirmDiscardWorkspaceEdit())) return;
      this.workspacePreviewEditing = false;
      this.workspacePreviewOpen = false;
    },

    workspaceFileEtag(data) {
      if (!data) return '';
      return data.etag || data.eTag || data.Etag || '';
    },

    async applyWorkspacePreview(data) {
      this.workspacePreviewData = data;
      this.workspacePreviewEditing = false;
      this.workspacePreviewDraft = data && data.isText ? (data.content || '') : '';
      this.workspacePreviewSrcdoc = '';
      this.workspacePreviewJsonTree = false;
      this.workspaceJsonRoot = null;
      this.prepareWorkspaceLinePreview(data);
      if (data && data.startLine) this.workspacePreviewJumpLine = data.startLine;
      if (!data || !this.workspacePreviewIsDocument) return;
      const isMarkdown = data.contentType === 'text/markdown';
      const html = isMarkdown ? this.formatMessageContent(data.content || '') : (data.content || '');
      console.debug('Workspace 预览:', data.path, data.contentType, isMarkdown ? 'markdown' : 'document');
      this.workspacePreviewSrcdoc = await this.buildWorkspaceHtmlPreview(html, data.path, isMarkdown, '');
    },

    prepareWorkspaceLinePreview(data) {
      this.workspacePreviewLineOffset = 0;
      this.workspaceLineBlockStates = null;
      this.workspacePreviewCodeHtml = '';
      this.workspacePreviewVirtual = false;
      if (!data || !data.isText || data.contentType === 'text/html' || data.contentType === 'text/markdown' || data.contentType === 'image/svg+xml') {
        this.workspacePreviewLines = [];
        return;
      }
      const lines = workspacePreviewLineList(data.content || '');
      this.workspacePreviewLines = lines;
      const virtual = lines.length > WORKSPACE_VIRTUAL_THRESHOLD;
      this.workspacePreviewVirtual = virtual;
      if (virtual) {
        this.workspaceLineBlockStates = workspaceBlockStates(lines, this.workspaceCodeLanguage);
        console.debug('Workspace 大文本预览:', data.path, lines.length, data.windowed ? 'window' : 'virtual');
        return;
      }
      this.workspacePreviewCodeHtml = highlightWorkspaceLines(data.content || '', this.workspaceCodeLanguage, data.startLine || 1);
    },

    visibleWorkspaceLines() {
      const lines = this.workspacePreviewLines || [];
      const start = this.workspacePreviewLineOffset || 0;
      const end = Math.min(lines.length, start + WORKSPACE_VISIBLE_LINES);
      const language = this.workspaceCodeLanguage;
      const origin = (this.workspacePreviewData && this.workspacePreviewData.startLine) || 1;
      const states = this.workspaceLineBlockStates;
      const items = [];
      for (let index = start; index < end; index += 1) {
        const highlighted = highlightWorkspaceLine(lines[index], language, !!(states && states[index]), true);
        items.push({ number: origin + index, html: highlighted.html });
      }
      return items;
    },

    onWorkspaceLineScroll(event) {
      const scroller = event.target;
      const top = scroller.scrollTop;
      const start = Math.max(0, Math.floor(top / WORKSPACE_LINE_HEIGHT) - 40);
      if (Math.abs(start - this.workspacePreviewLineOffset) < 20) return;
      this.workspacePreviewLineOffset = start;
      this.$nextTick(() => {
        if (scroller.scrollTop !== top) scroller.scrollTop = top;
      });
    },

    toggleWorkspacePreviewSource() {
      this.workspacePreviewSource = !this.workspacePreviewSource;
      if (this.workspacePreviewSource) this.workspacePreviewJsonTree = false;
    },

    toggleWorkspaceJsonTree() {
      if (!this.workspacePreviewIsJson || !this.workspacePreviewData) return;
      if (this.workspacePreviewJsonTree) {
        this.workspacePreviewJsonTree = false;
        return;
      }
      try {
        const jsonl = this.workspacePreviewData.contentType === 'application/jsonl';
        this.workspaceJsonRoot = buildWorkspaceJsonRoot(this.workspacePreviewData.content || '', jsonl);
        this.workspaceJsonRevision += 1;
        this.workspacePreviewJsonTree = true;
        this.workspacePreviewSource = false;
        console.debug('Workspace JSON 树:', this.workspacePreviewFilePath, this.workspaceJsonRoot && this.workspaceJsonRoot.size);
      } catch (error) {
        console.error('Workspace JSON 解析失败:', error);
        this.$message.warning('这份 JSON 无法解析，已保留文本预览。');
      }
    },

    workspaceJsonRows() {
      if (!this.workspaceJsonRevision || !this.workspaceJsonRoot) return [];
      return flattenWorkspaceJson(this.workspaceJsonRoot);
    },

    toggleWorkspaceJson(node) {
      if (!node || node.size == null) return;
      node.open = !node.open;
      if (node.open && node.shown == null) node.shown = Math.min(node.size, 200);
      this.workspaceJsonRevision += 1;
    },

    showMoreWorkspaceJson(node) {
      if (!node || node.size == null) return;
      node.shown = Math.min(node.size, 1000, (node.shown || 0) + 200);
      this.workspaceJsonRevision += 1;
      if (node.shown >= 1000 && node.size > 1000) {
        this.$message.info('树状预览一次最多展开 1000 项，其余请用文本查看。');
      }
    },

    jsonKeyLabel(node) {
      const key = String(node.key);
      return /^\d+$/.test(key) ? key : JSON.stringify(key);
    },

    jsonRowText(row) {
      const node = row.node;
      if (row.closing) return node.type === 'array' ? ']' : '}';
      if (node.size == null) {
        if (node.type === 'string') {
          const text = node.text.length > 240 ? node.text.slice(0, 240) + '…' : node.text;
          return JSON.stringify(text);
        }
        return node.text;
      }
      if (!node.open) return workspaceJsonCollapsed(node);
      return node.type === 'array' ? '[' : '{';
    },

    jsonValueClass(row) {
      if (row.closing || row.node.size != null) return 'json-meta';
      return 'json-' + row.node.type;
    },

    jsonRowTitle(row) {
      if (!row || row.closing || !row.node || row.node.type !== 'string') return '';
      return row.node.text || '';
    },

    async loadWorkspacePreviewWindow(startLine) {
      if (!this.workspacePreviewFilePath || this.workspacePreviewWindowLoading) return;
      const line = Math.max(1, Number(startLine) || 1);
      this.workspacePreviewWindowLoading = true;
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetWorkspaceTextWindowAsync', {
          params: { sessionId: this.currentSessionId, relativePath: this.workspacePreviewFilePath, startLine: line }
        });
        if (!response.data || !response.data.success || !response.data.data) {
          throw new Error((response.data && response.data.msg) || '文本片段加载失败。');
        }
        await this.applyWorkspacePreview(response.data.data);
        console.debug('Workspace 文本片段:', this.workspacePreviewFilePath, response.data.data.startLine, response.data.data.lineCount);
      } catch (error) {
        console.error('加载 Workspace 文本片段失败:', error);
        this.$notify({ title: 'Workspace', message: error.message || '文本片段加载失败。', type: 'error', duration: 3000 });
      } finally {
        this.workspacePreviewWindowLoading = false;
      }
    },

    jumpWorkspacePreviewLine() {
      const line = Math.max(1, Number(this.workspacePreviewJumpLine) || 1);
      if (this.workspacePreviewData && this.workspacePreviewData.windowed) {
        return this.loadWorkspacePreviewWindow(line);
      }
      const index = Math.max(0, Math.min((this.workspacePreviewLines || []).length - 1, line - 1));
      this.workspacePreviewLineOffset = Math.max(0, index - 40);
      this.$nextTick(() => {
        const scroller = this.$refs.workspaceLineScroller;
        if (scroller) scroller.scrollTop = index * WORKSPACE_LINE_HEIGHT;
      });
    },

    highlightWorkspaceCode(source, language) {
      return highlightWorkspaceSource(source, language);
    },

    async saveWorkspacePreview() {
      if (!this.workspacePreviewEditing || !this.workspacePreviewFilePath || this.workspacePreviewSaving) return;
      if (!this.workspacePreviewDirty) return;
      this.workspacePreviewSaving = true;
      try {
        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SaveWorkspaceFileAsync', {
          sessionId: this.currentSessionId,
          relativePath: this.workspacePreviewFilePath,
          content: this.workspacePreviewDraft,
          expectedEtag: this.workspaceFileEtag(this.workspacePreviewData)
        });
        if (!response.data || !response.data.success || !response.data.data) {
          throw new Error((response.data && response.data.msg) || '文件保存失败。');
        }
        await this.applyWorkspacePreview(response.data.data);
        const saved = response.data.data;
        const entry = this.workspaceEntries.find(item => item.relativePath === this.workspacePreviewFilePath);
        if (entry && saved.size != null) entry.length = saved.size;
        this.$message.success('文件已保存。');
        console.debug('Workspace 文本已保存:', this.workspacePreviewFilePath, saved.size);
      } catch (error) {
        console.error('保存 Workspace 文件失败:', error);
        this.$notify({ title: 'Workspace', message: error.message || '文件保存失败。', type: 'error', duration: 3000 });
      } finally {
        this.workspacePreviewSaving = false;
      }
    },

    async handleWorkspaceEntryClick(entry) {
      if (!entry || !entry.relativePath) return;
      if (!entry.isDirectory && entry.relativePath === this.workspacePreviewFilePath && !this.workspacePreviewEditing) return;
      if (!(await this.confirmDiscardWorkspaceEdit())) return;
      this.workspacePreviewSource = false;
      this.workspacePreviewEditing = false;
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
        await this.applyWorkspacePreview(response.data.data);
      } catch (error) {
        console.error('加载 Workspace 文件预览失败:', error);
        this.$notify({ title: 'Workspace', message: error.message || '文件预览加载失败。', type: 'error', duration: 3000 });
      } finally {
        this.workspacePreviewLoading = false;
      }
    },

    async buildWorkspaceHtmlPreview(content, filePath, isMarkdown = false, previewKind = '') {
      const parser = new DOMParser();
      const document = parser.parseFromString(content, 'text/html');
      document.querySelectorAll('iframe, object, embed, form, base, meta[http-equiv]').forEach((element) => element.remove());
      document.querySelectorAll('*').forEach((element) => {
        Array.from(element.attributes).forEach((attribute) => {
          if (attribute.name.toLowerCase().startsWith('on')
            || ['action', 'formaction', 'ping', 'srcset'].includes(attribute.name.toLowerCase())
            || attribute.name.includes(':')) {
            element.removeAttribute(attribute.name);
          }
          if ((attribute.name === 'src' || attribute.name === 'href')
            && /^\s*javascript:/i.test(attribute.value)) {
            element.removeAttribute(attribute.name);
          }
        });
        if (element.localName === 'a') {
          const href = element.getAttribute('href');
          if (href && !href.startsWith('#')) element.removeAttribute('href');
          element.removeAttribute('target');
          element.removeAttribute('download');
        }
      });

      const resources = [];
      document.querySelectorAll('img[src], video[src], audio[src], source[src], link[href], script[src]').forEach((element) => {
        const attribute = element.hasAttribute('src') ? 'src' : 'href';
        const value = element.getAttribute(attribute);
        if (!value || value.startsWith('#') || /^(data:|blob:|https?:|\/\/)/i.test(value)) {
          if (value && /^(https?:|\/\/)/i.test(value)) element.removeAttribute(attribute);
          return;
        }
        resources.push({ element, attribute, path: this.resolveWorkspaceAssetPath(filePath, value) });
      });

      for (const resource of resources) {
        if (resource.element.localName === 'script'
          && !this.isWorkspaceExecutableScriptType(resource.element.getAttribute('type'))) {
          resource.element.remove();
          continue;
        }
        try {
          const asset = await this.loadWorkspacePreviewAsset(resource.path);
          if (asset && asset.contentBase64
            && (resource.element.localName !== 'script'
              || asset.contentType === 'text/javascript'
              || asset.contentType === 'application/javascript')) {
            let contentBase64 = asset.contentBase64;
            if (asset.contentType === 'text/css' && asset.isText && asset.content) {
              const css = await this.inlineWorkspaceCss(asset.content, resource.path);
              contentBase64 = this.encodeWorkspaceText(css);
            }
            resource.element.setAttribute(
              resource.attribute,
              `data:${asset.contentType};base64,${contentBase64}`);
            if (resource.element.localName === 'script') resource.element.textContent = '';
          } else {
            resource.element.remove();
          }
        } catch (error) {
          resource.element.remove();
        }
      }

      document.querySelectorAll('script').forEach((element) => {
        if (!this.isWorkspaceExecutableScriptType(element.getAttribute('type'))) {
          element.remove();
          return;
        }
        if (!element.hasAttribute('src')) {
          element.setAttribute(
            'src',
            `data:text/javascript;base64,${this.encodeWorkspaceText(element.textContent || '')}`);
          element.textContent = '';
        }
      });

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
      if (previewKind === 'code') {
        style.textContent += `
          body{margin:0;padding:16px 18px;box-sizing:border-box;background:#fbfcfe;color:#1f2937;font:12.5px/1.65 ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;}
          pre{margin:0;white-space:pre;}
          .tok-keyword{color:#6d28d9;font-weight:600;}
          .tok-string{color:#0f766e;}
          .tok-comment{color:#94a3b8;font-style:italic;}
          .tok-number{color:#b45309;}
        `;
      }
      document.head.appendChild(style);
      const csp = document.createElement('meta');
      csp.httpEquiv = 'Content-Security-Policy';
      csp.content = "default-src 'none'; script-src data:; img-src data:; " +
        "style-src 'unsafe-inline' data:; font-src data:; media-src data:; " +
        "base-uri 'none'; form-action 'none'; connect-src 'none'; frame-src 'none'; " +
        "child-src 'none'; object-src 'none'; worker-src 'none'; navigate-to 'none';";
      document.head.insertBefore(csp, document.head.firstChild);
      return '<!doctype html>' + document.documentElement.outerHTML;
    },

    isWorkspaceExecutableScriptType(type) {
      const normalized = String(type || '').split(';', 1)[0].trim().toLowerCase();
      return !normalized || [
        'module', 'text/javascript', 'application/javascript',
        'text/ecmascript', 'application/ecmascript'
      ].includes(normalized);
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
      const name = this.workspaceEntryName(path).toLowerCase();
      if (['dockerfile', 'makefile', 'jenkinsfile'].includes(name)) return 'fa fa-file-code-o';
      const extension = name.split('.').pop();
      const code = 'fa fa-file-code-o';
      const text = 'fa fa-file-text-o';
      const image = 'fa fa-file-image-o';
      return {
        html: code, htm: code, css: code, scss: code, js: code, jsx: code, ts: code, tsx: code,
        py: code, pyw: code, cs: code, java: code, go: code, rs: code, php: code, rb: code,
        sh: code, sql: code, vue: code, xml: code,
        json: text, jsonl: text, ndjson: text, yml: text, yaml: text, md: text, txt: text, log: text, csv: text,
        png: image, jpg: image, jpeg: image, gif: image, svg: image, webp: image
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
      const sessionId = this.currentSessionId;
      if (!sessionId || this.sessionRuns[sessionId] || (this.isSending && this.runningSessionId === sessionId)) return;
      const run = this.beginSessionRun(sessionId, this.messageList);
      this.runningSessionId = sessionId;
      this.runningMessages = run.messages;
      this.resetLiveRun();
      this.bindRunLiveView(run);
      this.clearRecentTasks();
      run.recentTasks = [];
      run.activeTaskId = '';
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
        this.markSessionUnread(sessionId);
        this.releaseSessionRun(sessionId);
      }
    },

    async respondApproval(trajectoryId, approval, approved) {
      const sessionId = this.currentSessionId;
      if (!sessionId || this.sessionRuns[sessionId] || (this.isSending && this.runningSessionId === sessionId)) return;
      const run = this.beginSessionRun(sessionId, this.messageList);
      this.runningSessionId = sessionId;
      this.runningMessages = run.messages;
      this.resetLiveRun();
      this.bindRunLiveView(run);
      this.clearRecentTasks();
      run.recentTasks = [];
      run.activeTaskId = '';
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
        this.markSessionUnread(sessionId);
        this.releaseSessionRun(sessionId);
      }
    },

    handleChatInputKeydown(event) {
      if (this.handleWorkspaceMentionKeydown(event, 'inputMessage')) return;
      // 保持与首页一致：Ctrl+Enter (Windows/Linux) 或 Cmd+Enter (Mac) 发送。
      if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
        event.preventDefault();
        this.sendMessage();
      }
      // 普通 Enter 保留换行行为。
    },

    handleWorkspaceMentionEdit(field, event) {
      if (this.workspaceMentionApplying) return;
      if (event && event.type === 'keyup' && ['ArrowUp', 'ArrowDown', 'Enter', 'Tab', 'Escape'].indexOf(event.key) >= 0) return;
      const input = event && event.target;
      if (!input || typeof input.selectionStart !== 'number') return;
      this.syncWorkspaceMention(field, input.value, input.selectionStart);
    },

    syncWorkspaceMention(field, value, caret) {
      const mention = window.NcfWorkspaceMentions && this.canUploadWorkspaceFiles && !this.isCurrentSessionRunning && !this.isCreatingSession
        ? window.NcfWorkspaceMentions.findActive(value, caret) : null;
      if (!mention) {
        this.closeWorkspaceMention();
        return;
      }
      const sameQuery = this.workspaceMention.open && this.workspaceMention.field === field
        && this.workspaceMention.query === mention.query;
      this.workspaceMention.open = true;
      this.workspaceMention.field = field;
      this.workspaceMention.start = mention.start;
      this.workspaceMention.end = mention.end;
      this.workspaceMention.query = mention.query;
      if (!sameQuery) this.scheduleWorkspaceMentionLoad(mention.query);
    },

    scheduleWorkspaceMentionLoad(query) {
      const sequence = ++this.workspaceMention.sequence;
      if (this.workspaceMentionTimer) clearTimeout(this.workspaceMentionTimer);
      if (query === this.workspaceMention.loadedQuery) {
        this.workspaceMention.loading = false;
        this.workspaceMention.error = '';
        return;
      }
      this.workspaceMention.loading = true;
      this.workspaceMention.error = '';
      const delay = query ? 160 : 0;
      this.workspaceMentionTimer = setTimeout(() => {
        this.loadWorkspaceMentionItems(query, sequence);
      }, delay);
    },

    async loadWorkspaceMentionItems(query, sequence) {
      try {
        const params = { query: query };
        if (this.currentSessionId) params.sessionId = this.currentSessionId;
        else params.workspaceId = this.selectedWorkspaceId;
        const response = await service.get(
          '/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SuggestWorkspacePathsAsync',
          { params: params, customAlert: true });
        if (sequence !== this.workspaceMention.sequence || !this.workspaceMention.open) return;
        const body = response && response.data;
        const items = body && body.success && body.data && Array.isArray(body.data.items) ? body.data.items : null;
        if (!items) throw new Error(body && (body.errorMessage || body.msg) || '无法读取 Workspace 路径');
        this.workspaceMention.items = items.filter(item => item && item.relativePath);
        this.workspaceMention.active = 0;
        this.workspaceMention.loadedQuery = query;
        this.workspaceMention.error = '';
      } catch (error) {
        if (sequence !== this.workspaceMention.sequence) return;
        this.workspaceMention.items = [];
        this.workspaceMention.error = error.message || '无法读取 Workspace 路径';
      } finally {
        if (sequence === this.workspaceMention.sequence) this.workspaceMention.loading = false;
      }
    },

    handleWorkspaceMentionKeydown(event, field) {
      if (!this.workspaceMention.open || this.workspaceMention.field !== field) return false;
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        const count = this.workspaceMention.items.length;
        if (!count) return true;
        const step = event.key === 'ArrowDown' ? 1 : -1;
        this.workspaceMention.active = (this.workspaceMention.active + step + count) % count;
        this.$nextTick(() => {
          if (typeof document === 'undefined' || typeof document.querySelector !== 'function') return;
          const active = document.querySelector('.chat-mention-anchor .chat-mention-option.is-active');
          if (active && active.scrollIntoView) active.scrollIntoView({ block: 'nearest' });
        });
        return true;
      }
      if ((event.key === 'Enter' && !event.ctrlKey && !event.metaKey) || event.key === 'Tab') {
        const item = this.workspaceMention.items[this.workspaceMention.active];
        if (!item) return false;
        event.preventDefault();
        this.selectWorkspaceMention(item);
        return true;
      }
      if (event.key === 'Escape') {
        event.preventDefault();
        this.closeWorkspaceMention();
        return true;
      }
      return false;
    },

    selectWorkspaceMention(item) {
      const field = this.workspaceMention.field === 'chatInputText' ? 'chatInputText' : 'inputMessage';
      const refName = field === 'chatInputText' ? 'promptLauncherInput' : 'promptMessageInput';
      const input = this.workspaceMentionTextarea(refName);
      const source = input && typeof input.value === 'string' ? input.value : this[field];
      const applied = window.NcfWorkspaceMentions.apply(source, this.workspaceMention, item);
      if (!applied) return;
      this.workspaceMentionApplying = true;
      this[field] = applied.value;
      if (input) input.value = applied.value;
      this.$nextTick(() => {
        this.workspaceMentionApplying = false;
        if (input) {
          input.focus();
          input.setSelectionRange(applied.caret, applied.caret);
        }
        if (item.isDirectory) this.syncWorkspaceMention(field, applied.value, applied.caret);
        else this.closeWorkspaceMention();
      });
    },

    workspaceMentionTextarea(refName) {
      const component = this.$refs && this.$refs[refName];
      return component && (component.$refs && component.$refs.textarea || component.textarea || component);
    },

    closeWorkspaceMention() {
      if (this.workspaceMentionTimer) {
        clearTimeout(this.workspaceMentionTimer);
        this.workspaceMentionTimer = null;
      }
      this.workspaceMention.sequence += 1;
      this.workspaceMention.open = false;
      this.workspaceMention.items = [];
      this.workspaceMention.active = 0;
      this.workspaceMention.loading = false;
      this.workspaceMention.error = '';
      this.workspaceMention.loadedQuery = null;
    },

    waitForSessionListRetry(attempt) {
      if (typeof setTimeout !== 'function') return Promise.resolve();
      return new Promise(resolve => setTimeout(resolve, attempt * 200));
    },

    async readSessionPage(url) {
      // customAlert 避免全局拦截器再弹一个没有原因的 Error。失败由这里统一重试和提示。
      const response = await service.get(url, { customAlert: true });
      const body = response && response.data;
      const data = body && body.success && body.data;
      if (!data || !Array.isArray(data.sessions) || !Number.isInteger(data.totalCount)) {
        throw new Error((body && (body.errorMessage || body.msg || body.exception)) || 'invalid session page');
      }
      return data;
    },

    async collectSessionPages(urlForPage) {
      const sessions = [];
      let totalCount = 0;
      let pageIndex = 1;
      do {
        const data = await this.readSessionPage(urlForPage(pageIndex));
        if (data.sessions.length === 0 && sessions.length < data.totalCount) {
          throw new Error('incomplete session page');
        }
        sessions.push(...data.sessions);
        totalCount = data.totalCount;
        pageIndex += 1;
      } while (sessions.length < totalCount);
      return Array.from(new Map(sessions.map(session => [session.id, session])).values());
    },

    async collectSessionPagesWithRetry(urlForPage) {
      let lastError;
      for (let attempt = 1; attempt <= 3; attempt++) {
        try {
          return await this.collectSessionPages(urlForPage);
        } catch (error) {
          lastError = error;
          logAdminChat('info', 'AdminChat session list attempt failed:', { attempt, message: error && error.message });
          if (attempt < 3) await this.waitForSessionListRetry(attempt);
        }
      }
      throw lastError;
    },

    async loadSessionList() {
      if (this._activeSessionListLoad) {
        this._sessionListDirty = true;
        logAdminChat('info', 'AdminChat session list load coalesced.');
        return this._activeSessionListLoad;
      }
      this._activeSessionListLoad = this.performSessionListLoad();
      try {
        await this._activeSessionListLoad;
      } finally {
        this._activeSessionListLoad = null;
        if (this._sessionListDirty) {
          this._sessionListDirty = false;
          await this.loadSessionList();
        }
      }
    },

    async performSessionListLoad() {
      this.loadingSessions = true;
      try {
        const sessions = await this.collectSessionPagesWithRetry(
          page => `/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionListAsync?pageIndex=${page}&pageSize=100`);
        this.sessionList = sessions;
        this.applyPersistedUnread(this.sessionList);
        logAdminChat('info', 'AdminChat session list loaded:', sessions.length);
      } catch (error) {
        logAdminChat('error', '加载会话列表异常:', error && error.message);
        if (!this._sessionListDirty) this.$message.error(ncfT('AdminChat.LoadSessionFailed'));
      } finally {
        if (!this._sessionListDirty) this.loadingSessions = false;
      }
    },

    async loadArchivedSessionList() {
      if (this._activeArchivedSessionListLoad) {
        this._archivedSessionListDirty = true;
        logAdminChat('info', 'AdminChat archived session list load coalesced.');
        return this._activeArchivedSessionListLoad;
      }
      this._activeArchivedSessionListLoad = this.performArchivedSessionListLoad();
      try {
        await this._activeArchivedSessionListLoad;
      } finally {
        this._activeArchivedSessionListLoad = null;
        if (this._archivedSessionListDirty) {
          this._archivedSessionListDirty = false;
          await this.loadArchivedSessionList();
        }
      }
    },

    async performArchivedSessionListLoad() {
      try {
        const sessions = await this.collectSessionPagesWithRetry(
          page => `/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetArchivedSessionListAsync?pageIndex=${page}&pageSize=100`);
        this.archivedSessionList = sessions;
        logAdminChat('info', 'AdminChat archived session list loaded:', sessions.length);
      } catch (error) {
        logAdminChat('error', '加载归档会话异常:', error && error.message);
        if (!this._archivedSessionListDirty) this.$message.error(ncfT('AdminChat.Archive.LoadFailed'));
      }
    },

    async archiveSession(session) {
      if (!session || this.isSessionRunning(session) || this.archivingSessionId) return;
      this.archivingSessionId = session.id;
      try {
        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.ArchiveSessionAsync', {
          sessionId: session.id
        });
        if (!(response.data && response.data.success)) {
          throw new Error(response.data && response.data.errorMessage || ncfT('AdminChat.Archive.Failed'));
        }
        console.info('AdminChat session archived:', session.id);
        this.clearSessionUnread(session.id);
        this.sessionList = this.sessionList.filter(item => item.id !== session.id);
        if (!this.archivedSessionList.some(item => item.id === session.id)) {
          this.archivedSessionList.unshift({ ...session, status: 1 });
        }
        if (this.currentSessionId === session.id) this.currentSessionStatus = 1;
        this.$message.success(ncfT('AdminChat.Archive.Success'));
      } catch (error) {
        console.error('归档会话异常:', error);
        this.$message.error(ncfT('AdminChat.Archive.Failed'));
        await this.loadSessionList();
        await this.loadArchivedSessionList();
      } finally {
        this.archivingSessionId = 0;
      }
    },

    async openArchivedSession(session) {
      this.sidebarToolsVisible = false;
      if (!session) return;
      if (this.currentSessionId !== session.id) await this.switchSession(session.id);
      if (this.currentSessionId === session.id) this.currentSessionStatus = 1;
    },

    deleteArchivedSession(session) {
      this.sidebarToolsVisible = false;
      if (session) this.handleSessionCommand({ action: 'delete', id: session.id });
    },

    async restoreArchivedSession(session) {
      if (!session || this.restoringSessionId) return;
      this.restoringSessionId = session.id;
      try {
        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.RestoreSessionAsync', {
          sessionId: session.id
        });
        if (!(response.data && response.data.success)) {
          throw new Error(response.data && response.data.errorMessage || ncfT('AdminChat.Archive.RestoreFailed'));
        }
        console.info('AdminChat session restored:', session.id);
        this.archivedSessionList = this.archivedSessionList.filter(item => item.id !== session.id);
        if (!this.sessionList.some(item => item.id === session.id)) {
          this.sessionList.push({ ...session, status: 0 });
        }
        if (this.currentSessionId === session.id) this.currentSessionStatus = 0;
        this.sidebarToolsVisible = false;
        this.$message.success(ncfT('AdminChat.Archive.RestoreSuccess'));
      } catch (error) {
        console.error('还原会话异常:', error);
        this.$message.error(ncfT('AdminChat.Archive.RestoreFailed'));
        await this.loadSessionList();
        await this.loadArchivedSessionList();
      } finally {
        this.restoringSessionId = 0;
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
          this.currentSessionStatus = this.normalizeSessionStatus(session.status);
          this.messageList = preserveRunningMessages && this.isCurrentSessionRunning
            ? this.runningMessages : runningMessages || session.messages || [];
          this.currentSessionModules = session.modules || [];
          this.currentSessionWorkflows = session.workflows || [];
          this.currentSessionWorkspaceId = session.workspaceId || '';
          this.currentSessionWorkspaceName = session.workspaceName || '';
          this.currentSessionWorkspaceWriteEnabled = !!session.workspaceWriteEnabled;
          this.currentSessionWorkspaceCliEnabled = !!session.workspaceCliEnabled;
          if (session.hasUnread && sessionId === this.currentSessionId) {
            this.unreadSessionIds = this.unreadSessionIds.filter(item => item !== sessionId);
            this.persistSessionUnread(sessionId, false);
          }
          this.currentSessionAiModelId = this.getSessionAiModelId(this.currentSessionId);
          this.clearMessageSelection();
          this.isManageMode = false;
          await this.loadMemoryMode();
          
          this.$nextTick(() => {
            if (sessionId !== this.currentSessionId || sequence !== this.sessionDetailSequence) return;
            const focusId = Number(this.pendingHistorySearchMessageId) || 0;
            if (focusId) {
              this.pendingHistorySearchMessageId = 0;
              this.scrollToSearchedMessage(focusId);
              return;
            }
            this.scrollToBottom();
          });
        } else {
          this.pendingHistorySearchMessageId = 0;
          console.error('加载会话详情失败:', response.data.errorMessage);
          this.$message.error(ncfT('AdminChat.LoadDetailFailed'));
        }
      } catch (error) {
        if (sessionId !== this.currentSessionId || sequence !== this.sessionDetailSequence) return;
        this.pendingHistorySearchMessageId = 0;
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

    beginSessionRun(sessionId, messages) {
      const run = {
        controller: null,
        isStopping: false,
        messages: messages || [],
        liveAssistantText: '',
        liveThinkingText: '',
        liveTrajectoryEvents: [],
        livePendingApprovals: [],
        liveTrajectoryId: 0,
        liveStatusText: '',
        recentTasks: [],
        activeTaskId: '',
        isAIResponding: true
      };
      if (this.$set) this.$set(this.sessionRuns, sessionId, run);
      else this.sessionRuns[sessionId] = run;
      return run;
    },

    bindRunLiveView(run) {
      run.liveAssistantText = this.liveAssistantText;
      run.liveThinkingText = this.liveThinkingText;
      run.liveTrajectoryEvents = this.liveTrajectoryEvents;
      run.livePendingApprovals = this.livePendingApprovals;
      run.liveTrajectoryId = this.liveTrajectoryId;
      run.liveStatusText = this.liveStatusText;
    },

    releaseSessionRun(sessionId, controller) {
      const run = this.sessionRuns[sessionId];
      if (!run) return;
      if (controller && run.controller && run.controller !== controller) return;
      if (this.$delete) this.$delete(this.sessionRuns, sessionId);
      else delete this.sessionRuns[sessionId];
      const viewingFinished = Number(this.currentSessionId) === Number(sessionId);
      const currentRun = this.sessionRuns[this.currentSessionId];
      if (!viewingFinished && currentRun) return;
      this.isSending = !!currentRun;
      this.isStopping = currentRun ? !!currentRun.isStopping : false;
      this.isAIResponding = currentRun ? currentRun.isAIResponding !== false : false;
      if (!currentRun) {
        this.runningSessionId = 0;
        this.activeRequestController = null;
        this.runningMessages = [];
      }
    },

    showSessionRun(sessionId) {
      const run = this.sessionRuns[sessionId];
      if (!run || Number(this.currentSessionId) !== Number(sessionId)) return false;
      this.isSending = true;
      this.isStopping = !!run.isStopping;
      this.isAIResponding = run.isAIResponding !== false;
      this.runningSessionId = sessionId;
      this.activeRequestController = run.controller;
      this.runningMessages = run.messages;
      this.messageList = run.messages;
      this.liveAssistantText = run.liveAssistantText;
      this.liveThinkingText = run.liveThinkingText;
      this.liveTrajectoryEvents = run.liveTrajectoryEvents;
      this.livePendingApprovals = run.livePendingApprovals;
      this.liveTrajectoryId = run.liveTrajectoryId;
      this.liveStatusText = run.liveStatusText;
      this.recentTasks = run.recentTasks;
      this.activeTaskId = run.activeTaskId;
      return true;
    },

    hideForeignRunView() {
      if (this.sessionRuns[this.currentSessionId] || Object.keys(this.sessionRuns).length === 0) return;
      this.isSending = false;
      this.isStopping = false;
      this.isAIResponding = false;
      this.resetLiveRun();
      this.recentTasks = [];
      this.activeTaskId = '';
    },

    rememberSessionDraft(sessionId = this.currentSessionId) {
      const id = Number(sessionId) || 0;
      if (id > 0) {
        if (this.$set) this.$set(this.sessionDrafts, id, this.inputMessage || '');
        else this.sessionDrafts[id] = this.inputMessage || '';
      }
    },

    restoreSessionDraft(sessionId) {
      const id = Number(sessionId) || 0;
      this.inputMessage = id > 0 && Object.prototype.hasOwnProperty.call(this.sessionDrafts, id)
        ? this.sessionDrafts[id] : '';
    },

    async openCreatedChatSession(sessionId, message, references) {
      this.currentSessionId = sessionId;
      this.currentSessionAiModelId = this.normalizeAiModelId(this.launcherAiModelId);
      this.chatMode = this.launcherChatMode;
      this.syncAdminChatLocation();
      this.messageList = [];
      this.inputMessage = message || '';
      this.chatInputText = '';
      if (this.$delete) this.$delete(this.sessionDrafts, sessionId);
      else delete this.sessionDrafts[sessionId];
      await this.loadSessionList();
      if (this.currentSessionId !== sessionId) return;
      await this.loadSessionDetail();
      if (this.currentSessionId !== sessionId || this.sessionRuns[sessionId]) return;
      this.inputMessage = message || '';
      if (references && references.length) {
        await this.restorePromptReferences('promptMessageInput', references);
      }
      if (this.currentSessionId !== sessionId || this.sessionRuns[sessionId]) return;
      await this.sendMessage(references && references.length ? references : undefined);
    },

    createRecentTask(content, sessionId = this.currentSessionId) {
      const host = this.sessionRuns[sessionId] || this;
      const taskId = `task-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
      host.recentTasks = (host.recentTasks || []).concat({
        id: taskId,
        title: this.compactTaskText(content, 42),
        preview: '',
        status: '执行中',
        mode: this.chatMode
      }).slice(-2);
      host.activeTaskId = taskId;
      if (!this.sessionRuns[sessionId] || Number(sessionId) === Number(this.currentSessionId)) {
        this.recentTasks = host.recentTasks;
        this.activeTaskId = host.activeTaskId;
      }
    },

    updateActiveTask(changes, sessionId = this.currentSessionId) {
      const host = this.sessionRuns[sessionId] || this;
      if (!host.activeTaskId) return;
      const tasks = host.recentTasks || [];
      const index = tasks.findIndex((task) => task.id === host.activeTaskId);
      if (index < 0) return;
      const current = tasks[index];
      const next = Object.assign({}, current, changes, {
        preview: changes.preview === undefined
          ? current.preview
          : this.compactTaskText(changes.preview, 120)
      });
      this.$set(tasks, index, next);
      if (!this.sessionRuns[sessionId] || Number(sessionId) === Number(this.currentSessionId)) {
        this.recentTasks = tasks;
        this.activeTaskId = host.activeTaskId;
      }
    },

    finishActiveTask(status, sessionId = this.currentSessionId) {
      const host = this.sessionRuns[sessionId] || this;
      if (!host.activeTaskId) return;
      this.updateActiveTask({ status }, sessionId);
      host.activeTaskId = '';
      if (!this.sessionRuns[sessionId] || Number(sessionId) === Number(this.currentSessionId)) this.activeTaskId = '';
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
      const run = this.sessionRuns[sessionId];
      if (!run && sessionId !== this.runningSessionId) return;
      const show = !run || Number(sessionId) === Number(this.currentSessionId);
      const write = (field, value) => {
        if (run) run[field] = value;
        if (show) this[field] = value;
      };
      const read = (field) => (run ? run[field] : this[field]);
      const messages = run ? run.messages : this.runningMessages;
      if (eventName === 'resume') {
        write('liveAssistantText', payload && payload.text ? payload.text : '');
        write('liveThinkingText', payload && payload.thinking ? payload.thinking : '');
        write('liveStatusText', payload && payload.status ? payload.status : '任务仍在执行…');
        this.updateActiveTask({ preview: read('liveAssistantText'), status: '执行中' }, sessionId);
        this.scrollLivePreviewToBottom(sessionId);
        return;
      }
      if (eventName === 'cancelled') {
        const cancelled = new Error(payload && payload.message ? payload.message : '本次请求已终止');
        cancelled.name = 'AdminChatCancelled';
        throw cancelled;
      }
      if (eventName === 'user-message') {
        const tempIndex = messages.findIndex((item) => item.id === tempMessageId);
        if (tempIndex >= 0 && payload) messages.splice(tempIndex, 1, payload);
        return;
      }

      if (eventName === 'token') {
        write('liveAssistantText', read('liveAssistantText') + (payload && payload.text ? payload.text : ''));
        write('liveThinkingText', '');
        write('liveStatusText', '正在生成回答…');
        this.updateActiveTask({ preview: read('liveAssistantText'), status: '生成中' }, sessionId);
        this.scrollLivePreviewToBottom(sessionId);
        return;
      }

      if (eventName === 'usage') {
        this.$set(this.sessionUsage, sessionId, payload || null);
        if (sessionId === this.currentSessionId) this.latestUsage = payload || null;
        return;
      }

      if (eventName === 'assistant-phase') {
        if (payload && payload.trajectoryId) write('liveTrajectoryId', payload.trajectoryId);
        if (payload && payload.trajectoryEvent) {
          const event = payload.trajectoryEvent;
          this.upsertLiveTrajectoryEvent(event, sessionId);
          if (event.eventType === 'TextReasoningContent') {
            write('liveThinkingText', payload.text || event.content || '');
            write('liveStatusText', '正在思考…');
          } else {
            write('liveAssistantText', payload.text || event.content || '');
            write('liveStatusText', '正在生成回答…');
          }
          this.updateActiveTask({
            preview: event.content || payload.text || '',
            status: '生成中'
          }, sessionId);
          this.scrollLivePreviewToBottom(sessionId);
        }
        return;
      }

      if (eventName === 'trajectory-event') {
        if (payload && payload.trajectoryId) write('liveTrajectoryId', payload.trajectoryId);
        if (payload && payload.trajectoryEvent) {
          if (payload.trajectoryEvent.eventType === 'assistant.text') {
            this.upsertLiveTrajectoryEvent(payload.trajectoryEvent, sessionId);
          } else {
            read('liveTrajectoryEvents').push(payload.trajectoryEvent);
          }
          const eventType = payload.trajectoryEvent.eventType || '';
          if (eventType === 'assistant.text') {
            this.updateActiveTask({
              preview: payload.trajectoryEvent.content || payload.text || '',
              status: '生成中'
            }, sessionId);
          }
          write('liveStatusText', {
            'tool.call': '正在调用工具…',
            'tool.result': '正在整理工具结果…',
            'approval.request': '等待你的确认…',
            'assistant.text': '正在组织回答…'
          }[eventType] || '正在处理任务…');
        }
        write('livePendingApprovals', payload && payload.pendingApprovals ? payload.pendingApprovals : []);
        return;
      }

      if (eventName === 'trajectory-complete') {
        write('livePendingApprovals', payload && payload.pendingApprovals ? payload.pendingApprovals : []);
        write('liveStatusText', read('livePendingApprovals').length > 0 ? '等待你的确认…' : '任务已完成，正在保存…');
        return;
      }

      if (eventName === 'assistant-message') {
        if (payload) {
          payload.trajectoryId = payload.trajectoryId || read('liveTrajectoryId');
          payload.trajectoryEvents = read('liveTrajectoryEvents').slice();
          payload.pendingApprovals = read('livePendingApprovals').slice();
          messages.push(payload);
        }
        this.finishActiveTask('已完成', sessionId);
        return;
      }

      if (eventName === 'error') {
        this.finishActiveTask('失败', sessionId);
        throw new Error(payload && payload.message ? payload.message : '对话执行失败');
      }
    },

    scrollLivePreviewToBottom(sessionId = this.runningSessionId) {
      this.$nextTick(() => {
        if (Number(sessionId) !== Number(this.currentSessionId)) return;
        ['liveThinkingPreview', 'liveAnswerPreview'].forEach((refName) => {
          const element = this.$refs[refName];
          if (element) {
            element.scrollTop = element.scrollHeight;
          }
        });
      });
    },

    upsertLiveTrajectoryEvent(event, sessionId = this.runningSessionId) {
      const run = this.sessionRuns[sessionId];
      const events = run ? run.liveTrajectoryEvents : this.liveTrajectoryEvents;
      const key = event.correlationId || `${event.eventType || 'event'}-${event.sequence || 0}`;
      const index = events.findIndex((item) => {
        const itemKey = item.correlationId || `${item.eventType || 'event'}-${item.sequence || 0}`;
        return itemKey === key;
      });
      if (index >= 0) events.splice(index, 1, event);
      else events.push(event);
    },

    async consumeAdminChatStream(requestData, tempMessageId, signal, streamUrl) {
      const attached = !!streamUrl;
      const response = await fetch(streamUrl || '/api/Senparc.Areas.Admin/AdminChatStream/send', {
        method: attached ? 'GET' : 'POST',
        credentials: 'same-origin',
        headers: {
          'Content-Type': 'application/json',
          'X-Requested-With': 'XMLHttpRequest'
        },
        body: attached ? undefined : JSON.stringify(requestData),
        signal
      });
      if (response.status === 204) return;
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

    async stopCurrentRequest() {
      const sessionId = this.currentSessionId;
      const run = this.sessionRuns[sessionId];
      const controller = run ? run.controller : this.activeRequestController;
      if (!controller || !this.isCurrentSessionRunning) return;

      if (run) run.isStopping = true;
      this.isStopping = true;
      if (typeof fetch === 'function') {
        try {
          await fetch(`/api/Senparc.Areas.Admin/AdminChatStream/cancel?sessionId=${sessionId}`, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
          });
          console.debug('AdminChat 已请求停止后台任务:', sessionId);
        } catch (error) {
          console.error('停止后台任务失败:', error);
        }
      }
      controller.abort();
    },

    async resumeAdminChatRun() {
      const sessionId = this.currentSessionId;
      if (!sessionId || this.sessionRuns[sessionId] || typeof fetch !== 'function') return;
      let status;
      try {
        const response = await fetch(`/api/Senparc.Areas.Admin/AdminChatStream/status?sessionId=${sessionId}`, {
          credentials: 'same-origin',
          headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });
        if (!response.ok) return;
        status = await response.json();
      } catch (error) {
        console.error('读取 AdminChat 任务状态失败:', error);
        return;
      }
      if (!status || this.currentSessionId !== sessionId) return;
      if (status.justFinished) {
        console.debug('AdminChat 任务刚在后台完成，刷新消息。', sessionId);
        await this.loadSessionDetail(false);
      }
      if (!status.running || this.currentSessionId !== sessionId || this.sessionRuns[sessionId]) return;
      console.debug('AdminChat 任务仍在后台执行，重新连接。', sessionId, status.status || '');
      const run = this.beginSessionRun(sessionId, this.messageList);
      this.runningSessionId = sessionId;
      this.runningMessages = run.messages;
      this.isSending = true;
      this.isAIResponding = true;
      this.isStopping = false;
      this.resetLiveRun();
      this.liveAssistantText = status.text || '';
      this.liveThinkingText = status.thinking || '';
      this.liveStatusText = status.status || '任务仍在执行…';
      this.bindRunLiveView(run);
      this.createRecentTask(status.text || '继续执行未完成的任务', sessionId);
      const controller = typeof AbortController !== 'undefined' ? new AbortController() : null;
      run.controller = controller;
      this.activeRequestController = controller;
      try {
        await this.consumeAdminChatStream(
          { sessionId },
          null,
          controller ? controller.signal : undefined,
          `/api/Senparc.Areas.Admin/AdminChatStream/attach?sessionId=${sessionId}`);
        if (this.currentSessionId === sessionId) await this.loadSessionDetail(false);
        this.finishActiveTask('已完成', sessionId);
      } catch (error) {
        const wasAborted = (error && error.name === 'AbortError')
          || (controller && controller.signal.aborted);
        const cancelled = error && error.name === 'AdminChatCancelled';
        const stoppedByUser = !!(this.sessionRuns[sessionId] && this.sessionRuns[sessionId].isStopping);
        if (cancelled || (wasAborted && stoppedByUser)) {
          this.finishActiveTask('已终止', sessionId);
          if (this.currentSessionId === sessionId) await this.loadSessionDetail(false);
          this.$message.info('本次请求已终止');
        } else if (wasAborted) {
          console.debug('已离开正在执行的 AdminChat 任务观察。', sessionId);
        } else {
          this.finishActiveTask('失败', sessionId);
          console.error('重新连接 AdminChat 任务失败:', error);
          this.$message.error(error.message || '重新连接任务失败');
        }
      } finally {
        this.releaseSessionRun(sessionId, controller);
      }
    },

    async sendMessage(presetReferences) {
      this.closeWorkspaceMention();
      if (this.isCurrentSessionRunning || this.workspaceUploading || this.memoryBusy || !this.memoryReady) return;

      if ((!this.inputMessage || this.inputMessage.trim().length === 0) && this.pendingAttachments.length === 0) {
        this.$message.warning(ncfT('AdminChat.InputRequired'));
        return;
      }

      if (!this.currentSessionId) {
        this.$message.error(ncfT('AdminChat.SessionRequired'));
        return;
      }

      // 新建会话沿用启动器里已经捕获的引用；此时正文输入框可能尚未挂载。
      const promptReferences = Array.isArray(presetReferences)
        ? presetReferences.map(reference => ({ ...reference }))
        : this.capturePromptReferences('promptMessageInput');
      const messageContent = (promptReferences.length ? this.inputMessage : (this.inputMessage || '').trim())
        || '请先查看本轮附件的目录和解析限制，并询问我希望如何处理。';
      const sendingAttachments = this.pendingAttachments.slice();
      const sendingSessionId = this.currentSessionId;
      const sendingAiModelId = this.currentSessionAiModelId;
      const run = this.beginSessionRun(sendingSessionId, this.messageList);
      this.runningSessionId = sendingSessionId;
      this.runningMessages = run.messages;
      this.pendingAttachments = [];
      const isFirstGeneration = !this.messageList.some((message) => message.roleType === 1);
      this.inputMessage = '';
      if (this.$delete) this.$delete(this.sessionDrafts, sendingSessionId);
      else delete this.sessionDrafts[sendingSessionId];
      this.isSending = true;
      this.isStopping = false;
      this.isAIResponding = true;
      this.resetLiveRun();
      this.bindRunLiveView(run);
      this.createRecentTask(messageContent, sendingSessionId);
      const requestController = typeof AbortController !== 'undefined'
        ? new AbortController()
        : null;
      run.controller = requestController;
      this.activeRequestController = requestController;

      // 乐观渲染：立即显示“我”的消息，避免等待接口返回期间出现空白。
      const tempMessageId = `temp-${Date.now()}`;
      const tempUserMessage = {
        id: tempMessageId,
        roleType: 0,
        content: messageContent,
        addTime: new Date().toISOString(),
        userFeedback: 0,
        attachments: sendingAttachments,
        promptReferences
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
          promptReferences,
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
        const owned = this.sessionRuns[sendingSessionId];
        const wasAborted = (error && error.name === 'AbortError')
          || (requestController && requestController.signal.aborted);
        const cancelled = error && error.name === 'AdminChatCancelled';
        const stoppedByUser = !!(owned && owned.isStopping);
        if (wasAborted && !stoppedByUser && !cancelled) {
          console.debug('页面离开，AdminChat 任务继续在后台执行。', sendingSessionId);
          return;
        }
        const kept = ((owned && owned.messages) || this.runningMessages).filter((item) => item.id !== tempMessageId);
        if (owned) owned.messages = kept;
        if (wasAborted || cancelled) {
          this.finishActiveTask('已终止', sendingSessionId);
          if (this.currentSessionId === sendingSessionId) {
            this.runningMessages = kept;
            this.messageList = kept;
            this.inputMessage = messageContent;
            this.restorePromptReferences('promptMessageInput', promptReferences);
            await this.loadSessionDetail(false);
            if (this.currentSessionId === sendingSessionId) await this.loadTrajectoryList();
          }
          this.$message.info('本次请求已终止');
        } else {
          this.finishActiveTask('失败', sendingSessionId);
          console.error('发送消息异常:', error);
          this.$message.error(error.message || ncfT('AdminChat.SendFailedRetry'));
          if (this.currentSessionId === sendingSessionId) {
            this.runningMessages = kept;
            this.messageList = kept;
            this.inputMessage = messageContent;
            this.restorePromptReferences('promptMessageInput', promptReferences);
          }
        }
      } finally {
        sendingAttachments.filter(attachment => !this.pendingAttachments.includes(attachment)).forEach(attachment => {
          if (attachment.localPreviewUrl) URL.revokeObjectURL(attachment.localPreviewUrl);
        });
        this.markSessionUnread(sendingSessionId);
        this.releaseSessionRun(sendingSessionId, requestController);
      }
    },

    capturePromptReferences(refName) {
      const component = this.$refs[refName];
      const input = component && component.$el.querySelector('textarea');
      return input && window.NcfPromptReferences ? window.NcfPromptReferences.capture(input) : [];
    },

    restorePromptReferences(refName, references) {
      const apply = () => {
        const component = this.$refs[refName];
        const input = component && component.$el && component.$el.querySelector('textarea');
        if (!input || !window.NcfPromptReferences) return false;
        const expected = refName === 'promptMessageInput' ? this.inputMessage : this.chatInputText;
        // Element 可能在本次渲染之后才写回原生值；先对齐模型，避免用空正文校验引用范围。
        if (typeof expected === 'string' && input.value !== expected) input.value = expected;
        window.NcfPromptReferences.restore(input, references);
        return true;
      };
      return new Promise(resolve => {
        const attempt = remaining => {
          try {
            if (apply() || remaining <= 0) {
              resolve();
              return;
            }
          } catch (error) {
            this.$message.error(error.message);
            resolve();
            return;
          }
          this.$nextTick(() => attempt(remaining - 1));
        };
        this.$nextTick(() => attempt(3));
      });
    },

    promptReferenceAiScorer(message) {
      if (this.isCurrentSessionRunning || !Number.isInteger(message.id) || message.id <= 0) return undefined;
      const index = this.messageList.indexOf(message);
      const next = this.messageList[index + 1];
      if (!next || next.roleType !== 1) return undefined;
      return async promptItemId => {
        const response = await service.post(
          '/api/Senparc.Areas.Admin/AdminChatPromptReferenceAppService/Areas.Admin_AdminChatPromptReferenceAppService.ScoreWithAIAsync',
          { messageId: message.id, promptItemId }, { customAlert: true });
        if (!response.data || !response.data.success || !response.data.data) {
          throw new Error(response.data && response.data.errorMessage || ncfT('PromptReference.Unavailable'));
        }
        return response.data.data;
      };
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

    sessionIdFromLocation() {
      try {
        if (typeof window === 'undefined' || !window.location || typeof URLSearchParams === 'undefined') return 0;
        const params = new URLSearchParams(window.location.search || '');
        const queryId = Number(params.get('sessionId'));
        if (Number.isInteger(queryId) && queryId > 0) return queryId;
        const hash = String(window.location.hash || '').match(/^#session[=-](\d+)$/i);
        const hashId = hash ? Number(hash[1]) : 0;
        return Number.isInteger(hashId) && hashId > 0 ? hashId : 0;
      } catch (error) {
        return 0;
      }
    },

    syncAdminChatLocation() {
      if (typeof window === 'undefined' || !window.location || !window.history
        || typeof window.history.replaceState !== 'function' || typeof URL === 'undefined') return;
      let url;
      try { url = new URL(window.location.href); }
      catch (error) { return; }
      const sessionId = Number(this.currentSessionId) || 0;
      if (sessionId > 0) {
        url.searchParams.set('sessionId', String(sessionId));
        url.searchParams.set('mode', this.chatMode === 'harness' ? '1' : '0');
        url.hash = 'session-' + sessionId;
      } else {
        url.searchParams.delete('sessionId');
        url.hash = '';
      }
      const next = url.pathname + url.search + url.hash;
      const current = window.location.pathname + window.location.search + window.location.hash;
      if (next !== current) window.history.replaceState(window.history.state, '', next);
    },

    async switchSession(sessionId) {
      if (this.workspaceUploading) { this.$message.warning('请等待附件上传完成后再切换会话。'); return false; }
      if (this.currentSessionId === sessionId) return true;
      if (!(await this.confirmDiscardWorkspaceEdit())) return false;

      this.rememberSessionDraft();
      this.resetWorkspacePreview();
      this.closeWorkspaceMention();
      await this.deletePendingAttachments();
      this.clearPendingAttachments();
      this.currentSessionId = sessionId;
      this.syncAdminChatLocation();
      this.clearSessionUnread(sessionId);
      this.currentSessionStatus = 0;
      this.resetSessionTrajectoryView();
      this.currentSessionAiModelId = this.getSessionAiModelId(sessionId);
      const attachedRun = this.showSessionRun(sessionId);
      if (!attachedRun) {
        this.hideForeignRunView();
        this.messageList = [];
      }
      this.currentSessionModules = [];
      this.currentSessionWorkflows = [];
      this.currentSessionWorkspaceId = '';
      this.currentSessionWorkspaceName = '';
      this.currentSessionWorkspaceWriteEnabled = false;
      this.currentSessionWorkspaceCliEnabled = false;
      if (!this.hasRunningSession) this.clearRecentTasks();
      this.latestUsage = this.sessionUsage[sessionId] || null;
      this.restoreSessionDraft(sessionId);
      this.clearMessageSelection();
      this.isManageMode = false;
      await this.loadSessionDetail();
      if (this.currentSessionId === sessionId) {
        await this.loadTrajectoryList();
        await this.resumeAdminChatRun();
      }
      return this.currentSessionId === sessionId;
    },

    async createNewSession() {
      if (this.workspaceUploading) { this.$message.warning('请等待附件上传完成后再新建会话。'); return; }
      if (!(await this.confirmDiscardWorkspaceEdit())) return;
      this.rememberSessionDraft();
      this.resetWorkspacePreview();
      this.closeWorkspaceMention();
      await this.deletePendingAttachments();
      this.clearPendingAttachments();
      this.launcherAiModelId = this.currentSessionAiModelId || this.launcherAiModelId || 0;
      this.currentSessionId = 0;
      this.hideForeignRunView();
      this.syncAdminChatLocation();
      this.currentSessionStatus = 0;
      this.resetSessionTrajectoryView();
      this.currentSessionTitle = '';
      this.currentSessionModules = [];
      this.currentSessionWorkflows = [];
      this.currentSessionWorkspaceId = '';
      this.currentSessionWorkspaceName = '';
      this.currentSessionWorkspaceWriteEnabled = false;
      this.currentSessionWorkspaceCliEnabled = false;
      if (!this.hasRunningSession) this.clearRecentTasks();
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
      this.workspacePreviewEditing = false;
      this.workspacePreviewDraft = '';
      this.workspacePreviewSaving = false;
      this.workspacePreviewOverlay = false;
      this.workspaceShareDialogVisible = false;
      this.workspaceShares = [];
      this.workspaceSharePath = '';
      this.workspaceEntries = [];
      this.workspacePreviewPath = '';
      this.workspacePreviewFilePath = '';
      this.workspacePreviewData = null;
      this.workspacePreviewSrcdoc = '';
      this.workspacePreviewJsonTree = false;
      this.workspaceJsonRoot = null;
      this.workspaceJsonRevision = 0;
      this.workspacePreviewLines = [];
      this.workspacePreviewLineOffset = 0;
      this.workspacePreviewVirtual = false;
      this.workspaceLineBlockStates = null;
      this.workspacePreviewCodeHtml = '';
      this.workspacePreviewJumpLine = 1;
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
      if (command.action === 'archive') {
        const session = this.sessionList.find(item => item.id === command.id);
        if (session) await this.archiveSession(session);
        return;
      }
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
              
              this.clearSessionUnread(command.id);
              if (this.currentSessionId === command.id) {
                this.currentSessionId = 0;
                this.syncAdminChatLocation();
                this.currentSessionStatus = 0;
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
              await this.loadArchivedSessionList();
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

    normalizeSessionStatus(status) {
      if (status === 1 || status === '1' || status === 'Archived') return 1;
      if (status === 2 || status === '2' || status === 'Deleted') return 2;
      return 0;
    },

    messagePreviewText(content, limit) {
      const text = String(content || '')
        .replace(/<[^>]+>/g, ' ')
        .replace(/```[\s\S]*?```/g, ' ')
        .replace(/`([^`]*)`/g, '$1')
        .replace(/!\[[^\]]*]\([^)]*\)/g, ' ')
        .replace(/\[([^\]]*)]\([^)]*\)/g, '$1')
        .replace(/^#{1,6}\s+/gm, '')
        .replace(/[*_~>/#-]+/g, ' ')
        .replace(/\s+/g, ' ')
        .trim();
      const max = limit || 42;
      if (!text) return ncfT('AdminChat.Rail.EmptyMessage');
      return text.length > max ? text.slice(0, max) + '…' : text;
    },

    observeMessageRail() {
      if (this.messageRailObserver) {
        this.messageRailObserver.disconnect();
        this.messageRailObserver = null;
      }
      const container = this.$refs.messagesContainer;
      if (!container || typeof ResizeObserver === 'undefined') return;
      this.messageRailObserver = new ResizeObserver(() => this.refreshMessageRail());
      this.messageRailObserver.observe(container);
    },

    refreshMessageRail() {
      const container = this.$refs.messagesContainer;
      const messages = (this.messageList || []).filter(message => message && (message.roleType === 0 || message.roleType === 'User'));
      if (!container || messages.length === 0) {
        this.messageRail = [];
        return;
      }
      const railHeight = Math.max(container.clientHeight - 24, 24);
      const scrollHeight = Math.max(container.scrollHeight, 1);
      const ratios = messages.map((message, index) => {
        const selector = `[data-message-id="${window.CSS && CSS.escape ? CSS.escape(String(message.id)) : String(message.id)}"]`;
        const element = container.querySelector(selector);
        return element
          ? Math.min(1, Math.max(0, element.offsetTop / scrollHeight))
          : (messages.length === 1 ? 0 : index / (messages.length - 1));
      });
      const edges = ratios.map(ratio => ratio * railHeight);
      edges.push(railHeight);
      edges[0] = 0;
      const minHeight = Math.min(8, railHeight / messages.length);
      for (let index = 1; index < edges.length; index++) {
        edges[index] = Math.max(edges[index], edges[index - 1] + minHeight);
      }
      if (edges[edges.length - 1] > railHeight) {
        const scale = railHeight / edges[edges.length - 1];
        for (let index = 1; index < edges.length; index++) edges[index] *= scale;
      }
      edges[edges.length - 1] = railHeight;
      for (let index = 1; index < edges.length; index++) {
        edges[index] = Math.max(edges[index], edges[index - 1]);
      }
      const markers = messages.map((message, index) => {
        const top = Math.round(edges[index]);
        const end = index === messages.length - 1 ? railHeight : Math.round(edges[index + 1]);
        return {
          id: message.id,
          preview: this.messagePreviewText(message.content, 42),
          full: this.messagePreviewText(message.content, 180),
          top: top,
          height: Math.max(2, end - top)
        };
      });
      const signature = markers.map(marker => marker.id + '@' + marker.top + 'x' + marker.height).join('|');
      if (signature !== this.messageRailSignature) {
        this.messageRailSignature = signature;
        console.info('AdminChat message rail:', markers.length, 'segments');
      }
      this.messageRail = markers;
      this.updateActiveUserMarker();
    },

    updateActiveUserMarker() {
      const container = this.$refs.messagesContainer;
      if (!container || !this.messageRail.length) return;
      const line = container.scrollTop + 48;
      let active = this.messageRail[0].id;
      this.messageRail.forEach(marker => {
        const selector = `[data-message-id="${window.CSS && CSS.escape ? CSS.escape(String(marker.id)) : String(marker.id)}"]`;
        const element = container.querySelector(selector);
        if (element && element.offsetTop <= line) active = marker.id;
      });
      if (this.messageRailActiveId === active) return;
      this.messageRailActiveId = active;
      if (this.messageRailOpen && !this.messageRailMenuHover) this.$nextTick(() => this.scrollRailMenuToActive());
    },

    openMessageRail() {
      clearTimeout(this.messageRailCloseTimer);
      this.messageRailOpen = true;
    },

    scheduleCloseMessageRail() {
      clearTimeout(this.messageRailCloseTimer);
      this.messageRailCloseTimer = setTimeout(() => {
        this.messageRailOpen = false;
        this.messageRailMenuHover = false;
      }, 180);
    },

    highlightRailMarker(id) {
      clearTimeout(this.messageRailCloseTimer);
      this.messageRailOpen = true;
      this.messageRailMenuHover = true;
      if (this.messageRailActiveId === id) return;
      this.messageRailActiveId = id;
      console.info('AdminChat rail highlight:', id);
    },

    focusRailMarker(id) {
      this.messageRailMenuHover = false;
      const container = this.$refs.messagesContainer;
      const railHeight = container ? Math.max(container.clientHeight - 24, 24) : 240;
      const estimated = Math.min(360, this.messageRail.length * 36 + 12);
      const marker = this.messageRail.find(item => item.id === id);
      const anchor = marker ? marker.top + marker.height / 2 : 0;
      this.messageRailMenuTop = Math.max(0, Math.min(anchor - 18, Math.max(railHeight - estimated, 0)));
      this.messageRailActiveId = id;
      this.openMessageRail();
      this.$nextTick(() => this.scrollRailMenuToActive());
    },

    scrollRailMenuToActive() {
      const menu = this.$refs.messageRailMenu;
      const active = menu && menu.querySelector('.is-active');
      if (!menu || !active) return;
      menu.scrollTop = Math.max(0, active.offsetTop - menu.clientHeight / 2 + active.clientHeight / 2);
    },

    scheduleHistorySearch() {
      clearTimeout(this.historySearchTimer);
      this.historySearchTimer = setTimeout(() => this.searchHistory(), 250);
    },

    setHistorySearchTab(tab) {
      const next = tab === 'tool' ? 'tool' : 'chat';
      if (this.historySearchTab === next) return;
      this.historySearchTab = next;
      this.historySearchHits = [];
      this.historySearchSubmitted = false;
      console.info('AdminChat search tab:', next);
      if ((this.historySearchKeyword || '').trim()) return this.searchHistory();
    },

    async searchHistory() {
      const keyword = (this.historySearchKeyword || '').trim();
      const scope = this.historySearchTab === 'tool' ? 'tool' : 'chat';
      const sequence = ++this.historySearchSequence;
      if (!keyword) {
        this.historySearchHits = [];
        this.historySearchSubmitted = false;
        this.historySearchLoading = false;
        return;
      }
      this.historySearchLoading = true;
      try {
        const response = await service.get(
          '/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SearchHistoryAsync',
          { params: { keyword, limit: 10, scope }, customAlert: true });
        if (sequence !== this.historySearchSequence) return;
        if (!(response.data && response.data.success && response.data.data)) {
          throw new Error(response.data && response.data.errorMessage || ncfT('AdminChat.Search.Failed'));
        }
        const hits = response.data.data.hits || response.data.data.Hits || [];
        this.historySearchHits = hits.map(hit => Object.assign({}, hit, { query: keyword }));
        this.historySearchSubmitted = true;
        console.info('AdminChat history search:', scope, keyword, 'hits', this.historySearchHits.length);
      } catch (error) {
        if (sequence !== this.historySearchSequence) return;
        this.historySearchHits = [];
        this.historySearchSubmitted = true;
        console.error('AdminChat history search failed:', error);
        this.$message.error(ncfT('AdminChat.Search.Failed'));
      } finally {
        if (sequence === this.historySearchSequence) this.historySearchLoading = false;
      }
    },

    highlightParts(text, keyword) {
      const source = text || '';
      const query = (keyword == null ? this.historySearchKeyword : keyword).trim();
      const index = query ? source.toLowerCase().indexOf(query.toLowerCase()) : -1;
      if (index < 0) return { before: source, match: '', after: '' };
      return {
        before: source.slice(0, index),
        match: source.slice(index, index + query.length),
        after: source.slice(index + query.length)
      };
    },

    isTitleSearchHit(hit) {
      return !!hit && (hit.matchKind === 1 || hit.matchKind === 'Title');
    },

    isToolSearchHit(hit) {
      return !!hit && (hit.matchKind === 2 || hit.matchKind === 'ToolCall');
    },

    async openHistorySearchHit(hit) {
      const sessionId = Number(hit && hit.sessionId) || 0;
      const messageId = Number(hit && hit.messageId) || 0;
      if (!sessionId) return;
      this.historySearchVisible = false;
      console.info('AdminChat search navigate:', sessionId, messageId, hit && hit.matchKind);
      if (Number(this.currentSessionId) === sessionId) {
        if (messageId) this.scrollToSearchedMessage(messageId);
        else this.scrollToBottom();
        return;
      }
      this.pendingHistorySearchMessageId = messageId;
      const opened = await this.switchSession(sessionId);
      if (!opened) this.pendingHistorySearchMessageId = 0;
    },

    scrollToSearchedMessage(id, attempt = 0) {
      if (!id) return;
      const container = this.$refs.messagesContainer;
      const selector = `[data-message-id="${window.CSS && CSS.escape ? CSS.escape(String(id)) : String(id)}"]`;
      const element = container && container.querySelector(selector);
      if (!container || !element) {
        if (attempt < 2) {
          this.$nextTick(() => this.scrollToSearchedMessage(id, attempt + 1));
          return;
        }
        console.info('AdminChat search target missing:', id);
        return;
      }
      this.scrollToUserMessage(id);
      element.classList.remove('is-search-target');
      void element.offsetWidth;
      element.classList.add('is-search-target');
      clearTimeout(this.historySearchFlashTimer);
      this.historySearchFlashTimer = setTimeout(() => element.classList.remove('is-search-target'), 1800);
      console.info('AdminChat search scrolled to message:', id);
    },

    scrollToUserMessage(id) {
      const container = this.$refs.messagesContainer;
      const selector = `[data-message-id="${window.CSS && CSS.escape ? CSS.escape(String(id)) : String(id)}"]`;
      const element = container && container.querySelector(selector);
      if (!container || !element) return;
      this.autoScrollEnabled = false;
      this.messageRailActiveId = id;
      container.scrollTo({ top: Math.max(0, element.offsetTop - 12), behavior: 'smooth' });
      console.info('AdminChat jumped to user message:', id);
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
