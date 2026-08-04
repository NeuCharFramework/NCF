(function (window) {
    'use strict';

    function createAdminConsole() {
        const maximumConsoleEntries = 200;
        const consoleEntries = [];
        const consoleListeners = new Set();
        let consoleSequence = 0;

        function formatConsoleValue(value) {
            if (value instanceof Error) {
                return value.stack || value.message;
            }
            if (typeof value === 'string') {
                return value;
            }
            try {
                return JSON.stringify(value);
            } catch (_) {
                return String(value);
            }
        }

        function publishConsoleEntry(level, values) {
            const entry = {
                id: ++consoleSequence,
                level: level,
                time: new Date().toLocaleTimeString(),
                message: Array.from(values).map(formatConsoleValue).join(' ')
            };
            consoleEntries.push(entry);
            if (consoleEntries.length > maximumConsoleEntries) {
                consoleEntries.splice(0, consoleEntries.length - maximumConsoleEntries);
            }
            consoleListeners.forEach(listener => listener(consoleEntries.slice()));
        }

        ['log', 'info', 'warn', 'error'].forEach(level => {
            const original = window.console[level].bind(window.console);
            window.console[level] = function () {
                original.apply(window.console, arguments);
                publishConsoleEntry(level, arguments);
            };
        });

        return {
            subscribe(listener) {
                consoleListeners.add(listener);
                listener(consoleEntries.slice());
                return function () { consoleListeners.delete(listener); };
            },
            clear() {
                consoleEntries.splice(0, consoleEntries.length);
                consoleListeners.forEach(listener => listener([]));
            }
        };
    }

    // 防止开发期脚本重复加载时再次包装 console，造成一条日志被重复采集。
    window.NcfAdminConsole = window.NcfAdminConsole || createAdminConsole();

    const footerRuntime = window.NcfAdminFooterRuntime || { owner: null };
    window.NcfAdminFooterRuntime = footerRuntime;

    function isAdminLayoutRoot(viewModel) {
        const element = viewModel && viewModel.$el;
        return !!element
            && element.id === 'app'
            && window.document.getElementById('app') === element;
    }

    function claimFooterOwnership(viewModel) {
        if (!isAdminLayoutRoot(viewModel)) {
            return false;
        }
        if (footerRuntime.owner
            && footerRuntime.owner !== viewModel
            && !footerRuntime.owner.footerDisposed) {
            return false;
        }
        footerRuntime.owner = viewModel;
        return true;
    }

    function releaseFooterOwnership(viewModel) {
        if (footerRuntime.owner === viewModel) {
            footerRuntime.owner = null;
        }
    }

    const initialState = window.NCF_ADMIN_FOOTER_INITIAL_STATE || {};

    // 每个 Admin 页面都会加载此 mixin，因此必须严格控制请求数和长连接生命周期。
    const footerMixin = {
        data() {
            const parsedServerTime = Date.parse(initialState.serverTime || '');
            return {
                footerAiDialogVisible: false,
                footerAiUrl: '/Admin/AdminChat/Chat?embedded=1',
                consoleDialogVisible: false,
                consoleEntries: [],
                synchroDrawerVisible: false,
                synchroProviders: [],
                synchroAvailable: false,
                footerAvailabilityKnown: false,
                synchroLoading: false,
                serverTimeBaseMs: Number.isFinite(parsedServerTime) ? parsedServerTime : Date.now(),
                serverTimeMeasuredAtMs: Date.now(),
                footerClockTick: 0,
                footerConsoleUnsubscribe: null,
                footerClockTimer: null,
                footerPollTimer: null,
                footerEventSource: null,
                footerStateRequest: null,
                footerStateCancelSource: null,
                footerCommunicationOwner: false,
                footerPaused: false,
                footerDisposed: false,
                footerPageHideHandler: null,
                footerPageShowHandler: null,
                footerVisibilityHandler: null
            };
        },
        computed: {
            serverTimeText() {
                this.footerClockTick;
                const elapsed = Date.now() - this.serverTimeMeasuredAtMs;
                return new Date(this.serverTimeBaseMs + elapsed).toLocaleString('zh-CN', {
                    hour12: false,
                    month: '2-digit',
                    day: '2-digit',
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit'
                });
            },
            synchroTotalCount() {
                return this.synchroProviders
                    .filter(provider => provider.enabled)
                    .reduce((providerTotal, provider) => providerTotal + (provider.items || [])
                        .reduce((itemTotal, item) => itemTotal + Math.max(0, Number(item.count) || 0), 0), 0);
            }
        },
        mounted() {
            // Element UI 会动态创建额外的 Vue 根实例。只有真正挂载到布局 #app 的实例
            // 可以成为 Owner，否则每个临时根实例都会各自创建 state 请求和 SSE。
            if (!claimFooterOwnership(this)) {
                return;
            }
            this.footerCommunicationOwner = true;

            this.footerConsoleUnsubscribe = window.NcfAdminConsole.subscribe(entries => {
                this.consoleEntries = entries;
            });

            if (initialState.embedded) {
                // AdminChat iframe 不再启动一套 Footer 后台通讯，避免嵌套页面重复连接。
                return;
            }

            this.startFooterClock();
            // pagehide/pageshow 需要兼容 bfcache：隐藏时暂停，恢复时重用同一 Vue 实例。
            this.footerPageHideHandler = () => this.pauseFooter();
            this.footerPageShowHandler = () => this.resumeFooter();
            this.footerVisibilityHandler = () => {
                if (document.hidden) {
                    this.pauseFooter();
                } else {
                    this.resumeFooter();
                }
            };
            window.addEventListener('pagehide', this.footerPageHideHandler);
            window.addEventListener('pageshow', this.footerPageShowHandler);
            document.addEventListener('visibilitychange', this.footerVisibilityHandler);
            this.refreshFooterState();
        },
        beforeDestroy() {
            if (!this.footerCommunicationOwner) {
                return;
            }
            this.disposeFooter();
        },
        methods: {
            openFooterAi() {
                this.footerAiUrl = '/Admin/AdminChat/Chat?embedded=1&footer=' + Date.now();
                this.footerAiDialogVisible = true;
            },
            openFullAdminChat() {
                window.location.href = '/Admin/AdminChat/Chat';
            },
            clearFooterConsole() {
                window.NcfAdminConsole.clear();
            },
            synchroPreferenceKey() {
                return 'ncf.admin.synchro.providers.' + (initialState.account || 'admin');
            },
            readSynchroPreferences() {
                try {
                    const value = JSON.parse(window.localStorage.getItem(this.synchroPreferenceKey()) || '{}');
                    return value && typeof value === 'object' ? value : {};
                } catch (_) {
                    return {};
                }
            },
            saveSynchroPreferences() {
                const preferences = {};
                this.synchroProviders.forEach(provider => {
                    preferences[provider.providerId] = provider.enabled !== false;
                });
                window.localStorage.setItem(this.synchroPreferenceKey(), JSON.stringify(preferences));
            },
            applySynchroProviders(providers) {
                const preferences = this.readSynchroPreferences();
                this.synchroProviders = (providers || []).map(provider => Object.assign({}, provider, {
                    enabled: Object.prototype.hasOwnProperty.call(preferences, provider.providerId)
                        ? preferences[provider.providerId]
                        : provider.defaultVisible !== false
                }));
                // 服务端只返回已安装且开放的 Provider；空集合即代表应隐藏功能并停止通讯。
                this.footerAvailabilityKnown = true;
                this.synchroAvailable = this.synchroProviders.length > 0;
                if (this.synchroAvailable && !document.hidden) {
                    this.ensureSynchroRealtime();
                } else {
                    this.synchroDrawerVisible = false;
                    this.stopSynchroRealtime();
                }
            },
            refreshFooterState() {
                if (!this.footerCommunicationOwner || this.footerDisposed || this.footerPaused) {
                    return Promise.resolve();
                }
                if (this.footerStateRequest) {
                    // 多个变更通知同时到达时共用已在进行的请求，不叠加 state 请求。
                    return this.footerStateRequest;
                }

                this.synchroLoading = true;
                const requestSource = axios.CancelToken.source();
                this.footerStateCancelSource = requestSource;
                const request = axios.get('/api/Senparc.Areas.Admin/synchro/state', {
                    timeout: 10000,
                    cancelToken: requestSource.token,
                    headers: { 'Cache-Control': 'no-cache', 'x-requested-with': 'XMLHttpRequest' }
                }).then(response => {
                    const responseBody = response && response.data ? response.data : {};
                    const state = responseBody.data && responseBody.data.serverTime ? responseBody.data : responseBody;
                    const serverTime = Date.parse(state.serverTime || '');
                    if (Number.isFinite(serverTime)) {
                        this.serverTimeBaseMs = serverTime;
                        this.serverTimeMeasuredAtMs = Date.now();
                    }
                    this.applySynchroProviders(state.providers || []);
                }).catch(error => {
                    if (!axios.isCancel(error)) {
                        console.warn('Synchro 状态刷新失败:', error);
                    }
                }).finally(() => {
                    if (this.footerStateRequest === request) {
                        this.footerStateRequest = null;
                    }
                    if (this.footerStateCancelSource === requestSource) {
                        this.footerStateCancelSource = null;
                    }
                    this.synchroLoading = false;
                    if (this.synchroAvailable) {
                        this.ensureSynchroRealtime();
                    } else if (!this.footerAvailabilityKnown && !document.hidden) {
                        // 初始化失败时做低频恢复；已确认无 Provider 后不再轮询。
                        this.scheduleFooterPoll(30000, true);
                    }
                });
                this.footerStateRequest = request;
                return request;
            },
            scheduleFooterPoll(delay, allowWhenUnknown) {
                if (this.footerPollTimer) {
                    window.clearTimeout(this.footerPollTimer);
                    this.footerPollTimer = null;
                }
                if (!this.footerCommunicationOwner
                    || this.footerDisposed
                    || this.footerPaused
                    || document.hidden
                    || (!this.synchroAvailable && !allowWhenUnknown)) {
                    return;
                }
                this.footerPollTimer = window.setTimeout(() => {
                    this.footerPollTimer = null;
                    this.refreshFooterState();
                }, delay);
            },
            ensureSynchroRealtime() {
                if (!this.footerCommunicationOwner
                    || this.footerDisposed
                    || this.footerPaused
                    || document.hidden
                    || !this.synchroAvailable) {
                    return;
                }
                // SSE 负责及时通知，30 秒单次定时器用于断线和不支持 EventSource 时的兼容兜底。
                this.startSynchroEventStream();
                this.scheduleFooterPoll(30000, false);
            },
            stopSynchroRealtime() {
                if (this.footerPollTimer) {
                    window.clearTimeout(this.footerPollTimer);
                    this.footerPollTimer = null;
                }
                if (this.footerEventSource) {
                    this.footerEventSource.close();
                    this.footerEventSource = null;
                }
            },
            startSynchroEventStream() {
                if (!this.footerCommunicationOwner
                    || typeof window.EventSource === 'undefined'
                    || this.footerEventSource
                    || this.footerDisposed
                    || this.footerPaused
                    || document.hidden
                    || !this.synchroAvailable) {
                    return;
                }
                this.footerEventSource = new EventSource('/api/Senparc.Areas.Admin/synchro/events');
                this.footerEventSource.addEventListener('synchro-changed', () => this.refreshFooterState());
            },
            startFooterClock() {
                if (!this.footerClockTimer) {
                    this.footerClockTimer = window.setInterval(() => { this.footerClockTick += 1; }, 1000);
                }
            },
            pauseFooter() {
                // 后台页签不应保留 SSE、轮询或挂起的 HTTP 请求。
                this.footerPaused = true;
                if (this.footerClockTimer) {
                    window.clearInterval(this.footerClockTimer);
                    this.footerClockTimer = null;
                }
                this.stopSynchroRealtime();
                if (this.footerStateCancelSource) {
                    this.footerStateCancelSource.cancel('page paused');
                }
            },
            resumeFooter() {
                if (this.footerDisposed || document.hidden) {
                    return;
                }
                this.footerPaused = false;
                this.startFooterClock();
                const pendingRequest = this.footerStateRequest;
                if (pendingRequest) {
                    pendingRequest.then(() => {
                        if (!this.footerDisposed && !this.footerPaused && !this.footerStateRequest) {
                            this.refreshFooterState();
                        }
                    });
                } else {
                    this.refreshFooterState();
                }
            },
            disposeFooter() {
                if (this.footerDisposed) {
                    return;
                }
                this.pauseFooter();
                this.footerDisposed = true;
                if (this.footerConsoleUnsubscribe) {
                    this.footerConsoleUnsubscribe();
                    this.footerConsoleUnsubscribe = null;
                }
                this.footerStateCancelSource = null;
                if (this.footerPageHideHandler) {
                    window.removeEventListener('pagehide', this.footerPageHideHandler);
                }
                if (this.footerPageShowHandler) {
                    window.removeEventListener('pageshow', this.footerPageShowHandler);
                }
                if (this.footerVisibilityHandler) {
                    document.removeEventListener('visibilitychange', this.footerVisibilityHandler);
                }
                this.footerCommunicationOwner = false;
                releaseFooterOwnership(this);
            }
        }
    };

    window.NcfAdminFooterMixin = footerMixin;
    if (window.Vue && !footerRuntime.mixinRegistered) {
        window.Vue.mixin(footerMixin);
        footerRuntime.mixinRegistered = true;
    }
})(window);
