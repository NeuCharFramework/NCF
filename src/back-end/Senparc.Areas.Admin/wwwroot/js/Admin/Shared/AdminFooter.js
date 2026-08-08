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
                neuBellDrawerVisible: false,
                neuBellProviders: [],
                neuBellAvailable: false,
                footerAvailabilityKnown: false,
                neuBellLoading: false,
                serverTimeBaseMs: Number.isFinite(parsedServerTime) ? parsedServerTime : Date.now(),
                serverTimeMeasuredAtMs: Date.now(),
                footerClockTick: 0,
                footerConsoleUnsubscribe: null,
                footerClockTimer: null,
                footerPollTimer: null,
                footerEventSource: null,
                footerStateRequest: null,
                footerStateCancelSource: null,
                neuBellNotificationHandles: {},
                neuBellNotifyOnRefresh: false,
                neuBellRefreshQueued: false,
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
            neuBellTotalCount() {
                return this.neuBellProviders
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
            neuBellPreferenceKey() {
                return 'ncf.admin.neubell.providers.' + (initialState.account || 'admin');
            },
            readNeuBellPreferences() {
                try {
                    const value = JSON.parse(window.localStorage.getItem(this.neuBellPreferenceKey()) || '{}');
                    return value && typeof value === 'object' ? value : {};
                } catch (_) {
                    return {};
                }
            },
            saveNeuBellPreferences() {
                const preferences = {};
                this.neuBellProviders.forEach(provider => {
                    preferences[provider.providerId] = provider.enabled !== false;
                });
                window.localStorage.setItem(this.neuBellPreferenceKey(), JSON.stringify(preferences));
                this.syncNeuBellNotifications(this.neuBellProviders, this.neuBellProviders, false);
            },
            neuBellItemKey(providerId, itemId) {
                return String(providerId || '') + ':' + String(itemId || '');
            },
            getNeuBellItemCounts(providers) {
                const counts = {};
                (providers || []).forEach(provider => {
                    (provider.items || []).forEach(item => {
                        counts[this.neuBellItemKey(provider.providerId, item.id)] = Math.max(0, Number(item.count) || 0);
                    });
                });
                return counts;
            },
            closeNeuBellNotification(key) {
                const notification = this.neuBellNotificationHandles[key];
                if (notification && typeof notification.close === 'function') {
                    notification.close();
                }
                delete this.neuBellNotificationHandles[key];
            },
            closeAllNeuBellNotifications() {
                Object.keys(this.neuBellNotificationHandles).forEach(key => this.closeNeuBellNotification(key));
            },
            syncNeuBellNotifications(previousProviders, nextProviders, showNewNotifications) {
                const previousCounts = this.getNeuBellItemCounts(previousProviders);
                const visibleItemKeys = {};

                (nextProviders || []).forEach(provider => {
                    if (provider.enabled === false) {
                        return;
                    }
                    (provider.items || []).forEach(item => {
                        const count = Math.max(0, Number(item.count) || 0);
                        if (count <= 0) {
                            return;
                        }

                        const key = this.neuBellItemKey(provider.providerId, item.id);
                        visibleItemKeys[key] = true;
                        if (!showNewNotifications || count <= (previousCounts[key] || 0)) {
                            return;
                        }

                        this.closeNeuBellNotification(key);
                        if (typeof this.$notify !== 'function') {
                            return;
                        }

                        const supportedTypes = ['success', 'warning', 'info', 'error'];
                        const notificationType = supportedTypes.indexOf(item.severity) >= 0 ? item.severity : 'info';
                        let notification = null;
                        notification = this.$notify({
                            title: item.title || provider.displayName || 'NeuBell',
                            message: item.summary || '',
                            type: notificationType,
                            duration: 0,
                            position: 'bottom-right',
                            onClose: () => {
                                if (this.neuBellNotificationHandles[key] === notification) {
                                    delete this.neuBellNotificationHandles[key];
                                }
                            }
                        });
                        if (notification) {
                            this.neuBellNotificationHandles[key] = notification;
                        }
                    });
                });

                Object.keys(this.neuBellNotificationHandles).forEach(key => {
                    if (!visibleItemKeys[key]) {
                        this.closeNeuBellNotification(key);
                    }
                });
            },
            applyNeuBellProviders(providers, showNewNotifications) {
                const previousProviders = this.neuBellProviders;
                const preferences = this.readNeuBellPreferences();
                const nextProviders = (providers || []).map(provider => Object.assign({}, provider, {
                    enabled: Object.prototype.hasOwnProperty.call(preferences, provider.providerId)
                        ? preferences[provider.providerId]
                        : provider.defaultVisible !== false
                }));
                this.neuBellProviders = nextProviders;
                this.syncNeuBellNotifications(previousProviders, nextProviders, showNewNotifications === true);
                // 服务端只返回已安装且开放的 Provider；空集合即代表应隐藏功能并停止通讯。
                this.footerAvailabilityKnown = true;
                this.neuBellAvailable = this.neuBellProviders.length > 0;
                if (this.neuBellAvailable && !document.hidden) {
                    this.ensureNeuBellRealtime();
                } else {
                    this.neuBellDrawerVisible = false;
                    this.stopNeuBellRealtime();
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

                const showNewNotifications = this.neuBellNotifyOnRefresh;
                this.neuBellNotifyOnRefresh = false;
                this.neuBellLoading = true;
                const requestSource = axios.CancelToken.source();
                this.footerStateCancelSource = requestSource;
                const request = axios.get('/api/Senparc.Areas.Admin/neubell/state', {
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
                    this.applyNeuBellProviders(state.providers || [], showNewNotifications);
                }).catch(error => {
                    if (showNewNotifications) {
                        this.neuBellNotifyOnRefresh = true;
                    }
                    if (!axios.isCancel(error)) {
                        console.warn('纽铃状态刷新失败:', error);
                    }
                }).finally(() => {
                    if (this.footerStateRequest === request) {
                        this.footerStateRequest = null;
                    }
                    if (this.footerStateCancelSource === requestSource) {
                        this.footerStateCancelSource = null;
                    }
                    this.neuBellLoading = false;
                    const refreshQueued = this.neuBellRefreshQueued;
                    this.neuBellRefreshQueued = false;
                    if (refreshQueued && !this.footerDisposed && !this.footerPaused && !document.hidden) {
                        this.refreshFooterState();
                        return;
                    }
                    if (this.neuBellAvailable) {
                        this.ensureNeuBellRealtime();
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
                    || (!this.neuBellAvailable && !allowWhenUnknown)) {
                    return;
                }
                this.footerPollTimer = window.setTimeout(() => {
                    this.footerPollTimer = null;
                    this.refreshFooterState();
                }, delay);
            },
            ensureNeuBellRealtime() {
                if (!this.footerCommunicationOwner
                    || this.footerDisposed
                    || this.footerPaused
                    || document.hidden
                    || !this.neuBellAvailable) {
                    return;
                }
                // SSE 负责及时通知，30 秒单次定时器用于断线和不支持 EventSource 时的兼容兜底。
                this.startNeuBellEventStream();
                this.scheduleFooterPoll(30000, false);
            },
            stopNeuBellRealtime() {
                if (this.footerPollTimer) {
                    window.clearTimeout(this.footerPollTimer);
                    this.footerPollTimer = null;
                }
                if (this.footerEventSource) {
                    this.footerEventSource.close();
                    this.footerEventSource = null;
                }
            },
            startNeuBellEventStream() {
                if (!this.footerCommunicationOwner
                    || typeof window.EventSource === 'undefined'
                    || this.footerEventSource
                    || this.footerDisposed
                    || this.footerPaused
                    || document.hidden
                    || !this.neuBellAvailable) {
                    return;
                }
                this.footerEventSource = new EventSource('/api/Senparc.Areas.Admin/neubell/events');
                this.footerEventSource.addEventListener('neubell-changed', () => this.handleNeuBellChanged());
            },
            handleNeuBellChanged() {
                this.neuBellNotifyOnRefresh = true;
                if (this.footerStateRequest) {
                    this.neuBellRefreshQueued = true;
                    return this.footerStateRequest;
                }
                return this.refreshFooterState();
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
                this.stopNeuBellRealtime();
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
                this.closeAllNeuBellNotifications();
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
