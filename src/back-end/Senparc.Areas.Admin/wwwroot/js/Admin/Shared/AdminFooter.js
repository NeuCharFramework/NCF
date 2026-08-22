(function (window) {
    'use strict';

    function createAdminConsole() {
        const maximumConsoleEntries = 200;
        const repeatedConsoleEntryWindowMs = 10000;
        const consoleEntries = [];
        const consoleListeners = new Set();
        let consoleSequence = 0;
        let lastConsoleEntrySignature = '';
        let lastConsoleEntryAt = 0;

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
            const message = Array.from(values).map(formatConsoleValue).join(' ');
            const now = Date.now();
            const signature = `${level}:${message}`;
            // Vue 会把 render 异常写入 console。Footer 若立即把同一异常再写入响应式状态，
            // 就会触发新的根组件渲染并形成自激循环；浏览器原始 Console 仍会完整保留每次错误。
            if (signature === lastConsoleEntrySignature && now - lastConsoleEntryAt < repeatedConsoleEntryWindowMs) {
                return;
            }
            lastConsoleEntrySignature = signature;
            lastConsoleEntryAt = now;
            const entry = {
                id: ++consoleSequence,
                level: level,
                time: new Date().toLocaleTimeString(),
                message: message
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
                lastConsoleEntrySignature = '';
                lastConsoleEntryAt = 0;
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
    const maximumVisibleNeuBellToasts = 2;
    const neuBellToastDurationMs = 30000;
    const footerEasterEggClickCount = 10;
    const footerEasterEggClickWindowMs = 3500;

    function stopFooterFireworks() {
        if (footerRuntime.fireworksCleanup) {
            footerRuntime.fireworksCleanup();
            footerRuntime.fireworksCleanup = null;
        }
    }

    function playFooterFireworks() {
        stopFooterFireworks();
        if (!document.body || typeof document.createElement !== 'function') {
            return;
        }

        const canvas = document.createElement('canvas');
        const context = canvas.getContext && canvas.getContext('2d');
        if (!context) {
            return;
        }

        canvas.setAttribute('aria-hidden', 'true');
        Object.assign(canvas.style, {
            position: 'fixed',
            inset: '0',
            width: '100vw',
            height: '100vh',
            pointerEvents: 'none',
            zIndex: '2147483647'
        });
        document.body.appendChild(canvas);

        const colors = ['#ff1b6b', '#ff6b35', '#ffd166', '#8ac926', '#06d6a0', '#00bbf9', '#4361ee', '#7b2cbf', '#f15bb5'];
        const particles = [];
        const confetti = [];
        const burstCount = 5;
        const confettiWaveCount = 6;
        const burstIntervalMs = 240;
        const confettiWaveIntervalMs = 280;
        let frameId = null;
        let nextBurst = 0;
        let nextConfettiWave = 0;
        let width = 1;
        let height = 1;
        let stopped = false;
        let lastFrameAt = 0;
        let startedAt = null;
        const requestFrame = window.requestAnimationFrame || (callback => window.setTimeout(() => callback(Date.now()), 16));
        const cancelFrame = window.cancelAnimationFrame || window.clearTimeout;

        function resize() {
            const scale = Math.min(2, Math.max(1, window.devicePixelRatio || 1));
            width = Math.max(1, window.innerWidth || document.documentElement.clientWidth || 1);
            height = Math.max(1, window.innerHeight || document.documentElement.clientHeight || 1);
            canvas.width = Math.round(width * scale);
            canvas.height = Math.round(height * scale);
            context.setTransform(scale, 0, 0, scale, 0, 0);
        }

        function launchBurst(index) {
            const x = width * (.12 + Math.random() * .76);
            const y = height * (.14 + Math.random() * .42);
            for (let particleIndex = 0; particleIndex < 42; particleIndex++) {
                const angle = Math.PI * 2 * particleIndex / 42 + Math.random() * .12;
                const speed = 2.8 + Math.random() * 4.8;
                particles.push({
                    x: x,
                    y: y,
                    previousX: x,
                    previousY: y,
                    velocityX: Math.cos(angle) * speed,
                    velocityY: Math.sin(angle) * speed,
                    life: 48 + Math.random() * 34,
                    color: colors[(index + particleIndex + Math.floor(Math.random() * 3)) % colors.length],
                    size: 1.2 + Math.random() * 1.8
                });
            }
        }

        function launchConfetti() {
            for (let confettiIndex = 0; confettiIndex < 34; confettiIndex++) {
                confetti.push({
                    x: Math.random() * width,
                    y: -18 - Math.random() * 150,
                    velocityX: -1.1 + Math.random() * 2.2,
                    velocityY: 1.5 + Math.random() * 2.2,
                    angle: Math.random() * Math.PI * 2,
                    spin: -.15 + Math.random() * .3,
                    wave: Math.random() * Math.PI * 2,
                    life: 125 + Math.random() * 85,
                    width: 5 + Math.random() * 6,
                    height: 11 + Math.random() * 12,
                    color: colors[Math.floor(Math.random() * colors.length)]
                });
            }
        }

        function cleanup() {
            if (stopped) {
                return;
            }
            stopped = true;
            if (frameId !== null) {
                cancelFrame(frameId);
            }
            window.removeEventListener('resize', resize);
            if (canvas.parentNode) {
                canvas.parentNode.removeChild(canvas);
            }
        }

        function draw(frameAt) {
            if (stopped) {
                return;
            }
            const now = Number.isFinite(frameAt) ? frameAt : Date.now();
            const frameScale = Math.min(2, Math.max(.5, (now - (lastFrameAt || now)) / 16.67));
            lastFrameAt = now;
            startedAt = startedAt === null ? now : startedAt;
            const elapsed = now - startedAt;

            while (nextBurst < burstCount && elapsed >= nextBurst * burstIntervalMs) {
                launchBurst(nextBurst++);
            }
            while (nextConfettiWave < confettiWaveCount && elapsed >= nextConfettiWave * confettiWaveIntervalMs) {
                launchConfetti();
                nextConfettiWave++;
            }

            context.clearRect(0, 0, width, height);
            context.globalCompositeOperation = 'lighter';
            const drag = Math.pow(.985, frameScale);
            for (let index = particles.length - 1; index >= 0; index--) {
                const particle = particles[index];
                particle.life -= frameScale;
                if (particle.life <= 0) {
                    particles.splice(index, 1);
                    continue;
                }
                particle.previousX = particle.x;
                particle.previousY = particle.y;
                particle.x += particle.velocityX * frameScale;
                particle.y += particle.velocityY * frameScale;
                particle.velocityX *= drag;
                particle.velocityY = particle.velocityY * drag + .045 * frameScale;
                const alpha = Math.min(1, particle.life / 20);
                context.globalAlpha = alpha;
                context.strokeStyle = particle.color;
                context.lineWidth = particle.size;
                context.beginPath();
                context.moveTo(particle.previousX, particle.previousY);
                context.lineTo(particle.x, particle.y);
                context.stroke();
                context.fillStyle = '#fff';
                context.beginPath();
                context.arc(particle.x, particle.y, particle.size * .72, 0, Math.PI * 2);
                context.fill();
            }
            context.globalAlpha = 1;
            context.globalCompositeOperation = 'source-over';

            for (let index = confetti.length - 1; index >= 0; index--) {
                const ribbon = confetti[index];
                ribbon.life -= frameScale;
                if (ribbon.life <= 0 || ribbon.y > height + ribbon.height) {
                    confetti.splice(index, 1);
                    continue;
                }
                ribbon.x += (ribbon.velocityX + Math.sin(elapsed * .006 + ribbon.wave) * .8) * frameScale;
                ribbon.y += ribbon.velocityY * frameScale;
                ribbon.velocityY += .012 * frameScale;
                ribbon.angle += (ribbon.spin + Math.sin(elapsed * .01 + ribbon.wave) * .03) * frameScale;
                context.save();
                context.globalAlpha = Math.min(1, ribbon.life / 22);
                context.translate(ribbon.x, ribbon.y);
                context.rotate(ribbon.angle);
                context.fillStyle = ribbon.color;
                context.fillRect(-ribbon.width / 2, -ribbon.height / 2, ribbon.width, ribbon.height);
                context.globalAlpha *= .35;
                context.fillStyle = '#fff';
                context.fillRect(-ribbon.width / 2, -ribbon.height / 2, ribbon.width * .28, ribbon.height);
                context.restore();
            }

            if (nextBurst < burstCount || nextConfettiWave < confettiWaveCount || particles.length || confetti.length) {
                frameId = requestFrame(draw);
            } else {
                cleanup();
                footerRuntime.fireworksCleanup = null;
            }
        }

        resize();
        window.addEventListener('resize', resize);
        footerRuntime.fireworksCleanup = cleanup;
        frameId = requestFrame(draw);
    }

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
                neuBellToastEntries: [],
                neuBellToastExpanded: false,
                neuBellToastTimers: {},
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
            },
            neuBellToastVisibleItems() {
                const entries = this.neuBellToastEntries || [];
                return this.neuBellToastExpanded ? entries : entries.slice(0, maximumVisibleNeuBellToasts);
            },
            neuBellToastOverflowCount() {
                return Math.max(0, (this.neuBellToastEntries || []).length - maximumVisibleNeuBellToasts);
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
            handleFooterTimeClick() {
                const now = Date.now();
                if (now - (this._footerTimeLastClickedAt || 0) > footerEasterEggClickWindowMs) {
                    this._footerTimeClickCount = 0;
                }
                this._footerTimeLastClickedAt = now;
                this._footerTimeClickCount = (this._footerTimeClickCount || 0) + 1;
                if (this._footerTimeClickCount >= footerEasterEggClickCount) {
                    this._footerTimeClickCount = 0;
                    playFooterFireworks();
                }
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
            clearNeuBellToastTimer(key) {
                const timer = this.neuBellToastTimers[key];
                if (timer) {
                    window.clearTimeout(timer);
                }
                delete this.neuBellToastTimers[key];
            },
            closeNeuBellToast(key) {
                this.clearNeuBellToastTimer(key);
                const index = (this.neuBellToastEntries || []).findIndex(entry => entry.key === key);
                if (index >= 0) {
                    this.neuBellToastEntries.splice(index, 1);
                }
                if (this.neuBellToastEntries.length === 0) {
                    this.neuBellToastExpanded = false;
                }
            },
            clearAllNeuBellToasts() {
                Object.keys(this.neuBellToastTimers).forEach(key => this.clearNeuBellToastTimer(key));
                this.neuBellToastEntries.splice(0, this.neuBellToastEntries.length);
                this.neuBellToastExpanded = false;
            },
            closeAllNeuBellNotifications() {
                // 保留旧方法名，兼容可能由页面脚本调用的生命周期清理入口。
                this.clearAllNeuBellToasts();
            },
            scheduleNeuBellToastExpiry(key) {
                this.clearNeuBellToastTimer(key);
                // 右下角提示只是视觉提醒；到期不会消费服务端的业务提醒。
                this.neuBellToastTimers[key] = window.setTimeout(() => {
                    this.closeNeuBellToast(key);
                }, neuBellToastDurationMs);
            },
            upsertNeuBellToast(provider, item) {
                const key = this.neuBellItemKey(provider.providerId, item.id);
                const entry = {
                    key: key,
                    providerId: provider.providerId,
                    providerName: provider.displayName,
                    providerIcon: provider.icon,
                    item: Object.assign({}, item)
                };
                const entries = this.neuBellToastEntries;
                const existingIndex = entries.findIndex(existing => existing.key === key);
                if (existingIndex >= 0) {
                    entries.splice(existingIndex, 1);
                }
                entries.unshift(entry);
                this.scheduleNeuBellToastExpiry(key);
            },
            toggleNeuBellToastExpanded() {
                if (this.neuBellToastOverflowCount > 0) {
                    this.neuBellToastExpanded = !this.neuBellToastExpanded;
                }
            },
            openNeuBellToast(entry) {
                this.neuBellDrawerVisible = true;
                if (entry && entry.key) {
                    this.closeNeuBellToast(entry.key);
                }
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

                        this.upsertNeuBellToast(provider, item);
                    });
                });

                (this.neuBellToastEntries || []).slice().forEach(entry => {
                    if (!visibleItemKeys[entry.key]) {
                        this.closeNeuBellToast(entry.key);
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
                // 服务端只返回已安装且开放的 Provider；空集合时保留 Footer 入口，
                // 但不建立实时通讯，抽屉内会明确提示当前没有可用 Provider。
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
            consumeNeuBell(provider, item, consumeAll) {
                const providerId = String(provider && provider.providerId || '').trim();
                const itemId = String(item && item.id || '').trim();
                if (!provider || provider.canConsume !== true || !providerId || (!consumeAll && !itemId)) {
                    return Promise.resolve(0);
                }

                return axios.post('/api/Senparc.Areas.Admin/neubell/consume', {
                    providerId: providerId,
                    itemId: itemId,
                    consumeAll: consumeAll === true
                }, {
                    headers: { 'x-requested-with': 'XMLHttpRequest' }
                }).then(response => {
                    const responseBody = response && response.data ? response.data : {};
                    const body = responseBody.data && typeof responseBody.data === 'object'
                        ? responseBody.data
                        : responseBody;
                    const consumedCount = Math.max(0, Number(body.consumedCount) || 0);
                    return this.handleNeuBellChanged().then(() => consumedCount);
                }).catch(error => {
                    console.warn('纽铃消费失败:', error);
                    if (typeof this.$message === 'function') {
                        this.$message.error('纽铃提醒未能消费，请在业务页面中处理。');
                    }
                    return 0;
                });
            },
            startFooterClock() {
                if (!this.footerClockTimer) {
                    this.footerClockTimer = window.setInterval(() => { this.footerClockTick += 1; }, 1000);
                }
            },
            pauseFooter() {
                // 后台页签不应保留 SSE、轮询或挂起的 HTTP 请求。
                this.footerPaused = true;
                stopFooterFireworks();
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
