(function (window) {
    'use strict';

    const baseUrl = 'https://doc.ncf.pub/';
    const catalog = window.NcfDocHelpCatalog;
    const t = key => window.ncfT('DocHelp.' + key);
    let dialog;
    let loadTimer;
    let opener;

    function pageTopic(path) {
        const normalized = (path || '').split(/[?#]/)[0].replace(/\/+$/, '').toLowerCase().replace(/\/index$/, '');
        return Object.prototype.hasOwnProperty.call(catalog.pages, normalized) ? catalog.pages[normalized] : null;
    }

    function resolve(topic, culture) {
        if (!Object.prototype.hasOwnProperty.call(catalog.topics, topic)) {
            throw new Error('Unknown NCF documentation topic: ' + topic);
        }
        const entry = catalog.topics[topic];
        const language = /^zh(?:-|$)/i.test(culture || '') ? 'zh' : 'en';
        // Keep all destinations inside the documentation origin and locale tree.
        if (!/^(?:[a-z0-9_-]+\/)*[a-z0-9_.-]+\.html$/i.test(entry.path)
            || entry.path.split('/').some(segment => segment === '.' || segment === '..')
            || entry.path.startsWith('zh/')) {
            throw new Error('Invalid NCF documentation path: ' + topic);
        }
        const anchor = entry.anchors && entry.anchors[language];
        if (entry.anchors && (typeof anchor !== 'string' || !anchor)) {
            throw new Error('Missing NCF documentation anchor: ' + topic + '/' + language);
        }
        return baseUrl + (language === 'zh' ? 'zh/' : '') + entry.path
            + (anchor ? '#' + encodeURIComponent(anchor) : '');
    }

    function getDialog() {
        if (dialog) return dialog;
        const host = document.createElement('div');
        document.body.appendChild(host);
        dialog = new Vue({
            el: host,
            data: function () {
                return { visible: false, url: '', loading: false, failed: false, frameVersion: 0 };
            },
            methods: {
                t: t,
                startLoading: function () {
                    clearTimeout(loadTimer);
                    this.loading = true;
                    this.failed = false;
                    this.frameVersion++;
                    loadTimer = setTimeout(() => {
                        this.loading = false;
                        this.failed = true;
                    }, 15000);
                },
                frameLoaded: function (version) {
                    if (version !== this.frameVersion || !this.visible) return;
                    clearTimeout(loadTimer);
                    this.loading = false;
                    // Cross-origin load does not prove HTTP success or that framing was allowed.
                },
                frameFailed: function (version) {
                    if (version !== this.frameVersion || !this.visible) return;
                    clearTimeout(loadTimer);
                    this.loading = false;
                    this.failed = true;
                },
                cleanup: function () {
                    clearTimeout(loadTimer);
                    this.loading = false;
                    this.url = '';
                    if (opener && opener.isConnected) opener.focus();
                    opener = null;
                }
            },
            template: `
                <el-dialog :title="t('Title')" :visible.sync="visible"
                           width="min(1200px, 96vw)" top="4vh" append-to-body
                           custom-class="ncf-doc-help-dialog" :close-on-click-modal="false"
                           @closed="cleanup">
                    <div class="ncf-doc-help-toolbar">
                        <span role="status">{{ loading ? t('Loading') : t('Notice') }}</span>
                        <a :href="url" target="_blank" rel="noopener noreferrer"
                           class="ncf-doc-help-external">{{ t('OpenWindow') }}</a>
                    </div>
                    <div v-if="failed" class="ncf-doc-help-error" role="alert">
                        {{ t('LoadFailed') }}
                        <el-button type="text" @click="startLoading">{{ t('Retry') }}</el-button>
                    </div>
                    <iframe v-for="version in (visible ? [frameVersion] : [])" :key="version"
                            :src="url" :title="t('Title')" class="ncf-doc-help-frame"
                            referrerpolicy="no-referrer"
                            sandbox="allow-scripts allow-same-origin allow-popups allow-popups-to-escape-sandbox"
                            @load="frameLoaded(version)" @error="frameFailed(version)"></iframe>
                    <span slot="footer">
                        <el-button @click="visible = false">{{ t('Close') }}</el-button>
                    </span>
                </el-dialog>`
        });
        return dialog;
    }

    function open(topic, trigger, culture) {
        let url;
        try {
            url = resolve(topic, culture || document.documentElement.lang);
        } catch (error) {
            console.error('[NcfDocHelp]', error);
            Vue.prototype.$message.error(t('InvalidTopic'));
            return;
        }
        const instance = getDialog();
        opener = trigger || document.activeElement;
        instance.url = url;
        instance.startLoading();
        instance.visible = true;
    }

    window.NcfDocHelp = Object.freeze({ resolve: resolve, pageTopic: pageTopic, open: open });
    Vue.component('ncf-doc-help', {
        props: {
            topic: { type: String, default: '' },
            pagePath: { type: String, default: '' },
            culture: { type: String, default: '' }
        },
        computed: {
            resolvedTopic: function () { return this.topic || pageTopic(this.pagePath); }
        },
        methods: {
            t: t,
            show: function (event) { open(this.resolvedTopic, event.currentTarget, this.culture); }
        },
        template: `
            <button v-if="resolvedTopic" type="button" class="ncf-doc-help-button"
                    :title="t('Help')" :aria-label="t('Help')" aria-haspopup="dialog"
                    @click.stop.prevent="show">
                <i class="el-icon-question" aria-hidden="true"></i>
            </button>`
    });
})(window);
