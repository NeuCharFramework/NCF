(function (window) {
    'use strict';
    const trackers = new WeakMap();
    const scores = Vue.observable({});
    const pending = new Map();
    const t = key => window.ncfT('PromptReference.' + key);
    const api = '/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.';

    async function apiRequest(method, data, post) {
        const response = post
            ? await service.post(api + method, data, { customAlert: true })
            : await service.get(api + method, { params: data, customAlert: true });
        if (!response || !response.data || !response.data.success || !response.data.data) {
            throw new Error(response && response.data && response.data.errorMessage || t('Unavailable'));
        }
        return response.data.data;
    }

    async function request(method, data, post) {
        const info = await apiRequest(method, data, post);
        Vue.set(scores, info.promptItemId, info);
        if (post) window.dispatchEvent(new CustomEvent('ncf-prompt-score-changed', { detail: info }));
        return info;
    }

    function load(id) {
        if (!pending.has(id)) {
            pending.set(id, request('GetReferenceInfoAsync', { id }).finally(() => pending.delete(id)));
        }
        return pending.get(id);
    }

    function updateRanges(previous, value, references, edit) {
        if (previous === value && (!edit || edit.start === edit.end)) return references.slice();
        let start = 0;
        while (start < previous.length && start < value.length && previous[start] === value[start]) start++;
        let end = previous.length;
        let newEnd = value.length;
        while (end > start && newEnd > start && previous[end - 1] === value[newEnd - 1]) { end--; newEnd--; }
        // beforeinput identifies the actual edited occurrence when identical text appears more than once.
        if (edit && edit.start <= start && edit.end >= edit.start &&
            previous.slice(0, edit.start) === value.slice(0, edit.start) &&
            previous.slice(edit.end) === value.slice(value.length - (previous.length - edit.end))) {
            start = edit.start;
            end = edit.end;
        }
        const delta = value.length - previous.length;
        return references.flatMap(reference => {
            const referenceEnd = reference.start + reference.length;
            if (end <= reference.start) return [{ ...reference, start: reference.start + delta }];
            if (start >= referenceEnd) return [{ ...reference }];
            return [];
        });
    }

    function publish(state, references) {
        state.references = references;
        state.view.references = references;
        state.input.dispatchEvent(new CustomEvent('prompt-references-change', {
            bubbles: true, detail: references.map(reference => ({ ...reference }))
        }));
    }

    function sync(input) {
        const state = trackers.get(input);
        if (!state || state.value === input.value) return;
        // Programmatic replacement has no edit intent: never infer a reference from matching text.
        state.value = input.value;
        publish(state, []);
    }

    window.NcfPromptReferences = {
        updateRanges,
        scores,
        attach(input, host) {
            if (!input || trackers.has(input)) return;
            const mount = document.createElement('div');
            mount.className = 'ncf-prompt-reference-labels';
            host.appendChild(mount);
            const state = { input, value: input.value, references: [], edit: null };
            state.view = new Vue({
                el: mount, data: { references: [] },
                methods: {
                    remove(index) { publish(state, state.references.filter((_, position) => position !== index)); },
                    locate(reference) { input.focus(); input.setSelectionRange(reference.start, reference.start + reference.length); }
                },
                template: `<div class="ncf-prompt-reference-labels">
                    <prompt-reference-tag v-for="(reference, index) in references"
                        :key="reference.promptItemId + ':' + reference.start" :reference="reference"
                        removable @remove="remove(index)" @locate="locate(reference)"></prompt-reference-tag>
                </div>`
            });
            state.before = function (event) {
                state.edit = { start: input.selectionStart, end: input.selectionEnd, type: event.inputType };
            };
            state.changed = function () {
                if (state.edit && state.edit.start === state.edit.end && input.value.length < state.value.length) {
                    const deleted = state.value.length - input.value.length;
                    if (/Backward$/.test(state.edit.type)) state.edit.start -= deleted;
                    else if (/Forward$/.test(state.edit.type)) state.edit.end += deleted;
                }
                const references = updateRanges(state.value, input.value, state.references, state.edit);
                state.value = input.value;
                state.edit = null;
                publish(state, references);
            };
            input.addEventListener('beforeinput', state.before);
            // Track native edits before Vue's input listener can render and synchronize the directive.
            input.addEventListener('input', state.changed, true);
            trackers.set(input, state);
        },
        sync,
        capture(input) {
            sync(input);
            const state = trackers.get(input);
            return state ? state.references.map(reference => ({ ...reference })) : [];
        },
        restore(input, references) {
            const state = trackers.get(input);
            if (!state) return;
            if (references != null && (!Array.isArray(references) || references.length > 32)) {
                throw new Error(t('InvalidReference'));
            }
            let end = 0;
            for (const reference of references || []) {
                if (!reference || !Number.isInteger(reference.start) || reference.start < end ||
                    !Number.isInteger(reference.length) || reference.length <= 0 ||
                    reference.start + reference.length > input.value.length ||
                    !Number.isInteger(reference.promptItemId) || reference.promptItemId <= 0 ||
                    typeof reference.promptCode !== 'string' || !reference.promptCode.trim()) {
                    throw new Error(t('InvalidReference'));
                }
                end = reference.start + reference.length;
            }
            state.value = input.value;
            publish(state, (references || []).map(reference => ({ ...reference })));
        },
        inserted(input, previous, start, end, item) {
            const state = trackers.get(input);
            if (!state) return;
            const references = updateRanges(previous, input.value, state.references, { start, end });
            references.push({ promptItemId: item.id, promptCode: item.fullVersion, start, length: item.content.length });
            state.value = input.value;
            state.edit = null;
            publish(state, references.sort((a, b) => a.start - b.start));
        },
        detach(input) {
            const state = trackers.get(input);
            if (!state) return;
            input.removeEventListener('beforeinput', state.before);
            input.removeEventListener('input', state.changed, true);
            state.view.$destroy();
            state.view.$el.remove();
            trackers.delete(input);
        }
    };

    Vue.component('prompt-reference-score-chart', {
        props: { reference: { type: Object, required: true }, score: Number },
        data() {
            return {
                items: [], scores, loading: false, error: '', previewId: 0,
                preview: null, previewLoading: false, previewError: '', version: 0, previewVersion: 0
            };
        },
        computed: {
            rangeLabel() {
                const item = this.items[0];
                return item ? [item.rangeName, item.rangeAlias].filter(Boolean).join(' · ') : '';
            },
            axisMax() {
                return this.items.reduce((max, item) => {
                    const value = this.columnScore(item);
                    return Number.isFinite(value) ? Math.max(max, value) : max;
                }, 10);
            },
            previewInfo() { return this.scores[this.previewId]; }
        },
        mounted() { this.reload(); },
        beforeDestroy() { this.version++; this.previewVersion++; },
        methods: {
            t,
            format(value) { return Number.isFinite(value) && value >= 0 ? value.toFixed(1) : t('Unrated'); },
            savedScore(item) {
                return this.scores[item.id] ? this.scores[item.id].currentScore : item.currentScore;
            },
            columnScore(item) {
                return item.id === this.reference.promptItemId ? this.score : this.savedScore(item);
            },
            height(item) { return Math.max(0, this.columnScore(item) || 0) / this.axisMax * 100 + '%'; },
            locateTarget() {
                const target = this.$el.querySelector('.ncf-prompt-score-column.is-target');
                if (target) target.scrollIntoView({ block: 'nearest', inline: 'center' });
            },
            async reload() {
                const version = ++this.version;
                const previousScores = { ...this.scores };
                this.loading = true; this.error = '';
                try {
                    const items = await apiRequest('GetSelectorRangeScoresAsync', { id: this.reference.promptItemId });
                    if (version !== this.version) return;
                    if (!Array.isArray(items) || !items.some(item => item.id === this.reference.promptItemId)) {
                        throw new Error(t('Unavailable'));
                    }
                    this.items = items;
                    for (const item of items) {
                        const info = this.scores[item.id];
                        // Refresh cached scores unless a newer read or save completed during this request.
                        if (info && info === previousScores[item.id]) {
                            Vue.set(this.scores, item.id, { ...info, currentScore: item.currentScore });
                        }
                    }
                    this.$nextTick(this.locateTarget);
                    this.select(this.reference.promptItemId);
                } catch (error) {
                    if (version === this.version) this.error = error.message || t('Unavailable');
                } finally { if (version === this.version) this.loading = false; }
            },
            async select(id) {
                const version = ++this.previewVersion;
                this.previewId = id; this.preview = null; this.previewError = ''; this.previewLoading = true;
                try {
                    const [prompt] = await Promise.all([
                        apiRequest('GetSelectorPromptAsync', { id }), load(id)
                    ]);
                    if (version === this.previewVersion) this.preview = prompt;
                } catch (error) {
                    if (version === this.previewVersion) this.previewError = error.message || t('Unavailable');
                } finally { if (version === this.previewVersion) this.previewLoading = false; }
            }
        },
        template: `<section class="ncf-prompt-score-overview" :aria-label="t('RangeScores')">
            <div class="ncf-prompt-score-heading">
                <strong>{{ t('RangeScores') }} <span>{{ rangeLabel }}</span></strong>
                <el-button size="mini" :disabled="loading || !items.length" @click="locateTarget">{{ t('LocateTarget') }}</el-button>
            </div>
            <p class="ncf-prompt-selector-hint">{{ t('ChartHint') }}</p>
            <p v-if="error" role="alert" class="ncf-prompt-selector-error">{{ error }}
                <el-button size="mini" @click="reload">{{ t('Retry') }}</el-button>
            </p>
            <div v-loading="loading" class="ncf-prompt-score-plot">
                <div class="ncf-prompt-score-axis" aria-hidden="true"><span>{{ format(axisMax) }}</span><span>{{ format(axisMax / 2) }}</span><span>0</span></div>
                <div class="ncf-prompt-score-scroll">
                    <div class="ncf-prompt-score-columns">
                        <button v-for="item in items" :key="item.id" type="button"
                            class="ncf-prompt-score-column" :data-prompt-id="item.id"
                            :class="{'is-target': item.id === reference.promptItemId, 'is-preview': item.id === previewId}"
                            :aria-pressed="item.id === previewId" :aria-label="item.fullVersion + ' · ' + format(columnScore(item))"
                            :title="item.fullVersion + ' · ' + format(columnScore(item))" @click="select(item.id)">
                            <span class="ncf-prompt-score-track"><span class="ncf-prompt-score-bar"
                                :style="{height: height(item)}" :class="{'is-unrated': columnScore(item) < 0}">
                                <span class="ncf-prompt-score-bar-value">{{ format(columnScore(item)) }}</span>
                            </span></span>
                            <span class="ncf-prompt-score-code">{{ item.fullVersion.slice(item.rangeName.length + 1) }}</span>
                        </button>
                    </div>
                </div>
            </div>
            <div v-loading="previewLoading" class="ncf-prompt-score-preview" aria-live="polite">
                <strong>{{ t('PreviewOnly') }}</strong>
                <p v-if="previewError" role="alert" class="ncf-prompt-selector-error">{{ previewError }}
                    <el-button size="mini" @click="select(previewId)">{{ t('Retry') }}</el-button>
                </p>
                <template v-if="preview">
                    <p class="ncf-prompt-score-preview-code">{{ preview.fullVersion }}</p>
                    <p v-if="previewInfo" class="ncf-prompt-score-details">
                        <span>{{ t('CurrentScore') }}: {{ format(previewInfo.currentScore) }}</span>
                        <span>{{ t('HumanScore') }}: {{ format(previewInfo.humanScore) }}</span>
                        <span>{{ t('RobotScore') }}: {{ format(previewInfo.robotScore) }}</span>
                    </p>
                    <pre>{{ preview.content }}</pre>
                </template>
            </div>
        </section>`
    });

    Vue.component('prompt-reference-tag', {
        props: { reference: { type: Object, required: true }, removable: Boolean, aiScore: Function },
        data() { return { scores, error: '', visible: false, busy: false, scoreLoading: false, score: 0 }; },
        computed: {
            info() { return this.scores[this.reference.promptItemId]; },
            label() {
                const score = this.info && this.info.currentScore;
                return this.reference.promptCode + ' · ' +
                    (Number.isFinite(score) && score >= 0 ? score.toFixed(1) : t('Unrated'));
            }
        },
        mounted() { this.refresh(); },
        methods: {
            t,
            async refresh() {
                try { await load(this.reference.promptItemId); this.error = ''; }
                catch (error) { this.error = error.message || t('Unavailable'); }
            },
            async command(command) {
                if (command === 'view') { window.NcfPromptRange.openPrompt(this.reference.promptItemId); return; }
                if (command === 'remove') { this.$emit('remove'); return; }
                if (command === 'locate') { this.$emit('locate'); return; }
                this.visible = true;
                this.scoreLoading = true;
                await this.refresh();
                this.score = this.info && this.info.currentScore >= 0 ? Math.min(10, this.info.currentScore) : 0;
                this.scoreLoading = false;
            },
            async save() {
                if (!Number.isFinite(this.score) || this.score < 0 || this.score > 10 ||
                    Math.round(this.score * 10) !== this.score * 10) {
                    this.error = t('InvalidScore'); return;
                }
                this.busy = true;
                try {
                    await request('ScoreReferenceAsync', { id: this.reference.promptItemId, score: this.score }, true);
                    this.error = ''; this.visible = false;
                } catch (error) { this.error = error.message || t('Unavailable'); }
                finally { this.busy = false; }
            },
            async autoScore() {
                try { await this.$confirm(t('AIConfirm'), t('AIScore'), { type: 'warning' }); }
                catch (action) { if (action === 'cancel' || action === 'close') return; throw action; }
                this.busy = true;
                try {
                    const info = await this.aiScore(this.reference.promptItemId);
                    Vue.set(scores, info.promptItemId, info);
                    window.dispatchEvent(new CustomEvent('ncf-prompt-score-changed', { detail: info }));
                    this.score = info.currentScore; this.error = '';
                } catch (error) { this.error = error.message || t('Unavailable'); }
                finally { this.busy = false; }
            }
        },
        template: `<span class="ncf-prompt-reference-tag">
            <el-dropdown trigger="click" @command="command">
                <button type="button" class="ncf-prompt-reference-pill" :title="error || reference.promptCode">
                    {{ label }} <i class="el-icon-arrow-down"></i><span v-if="error"> !</span>
                </button>
                <el-dropdown-menu slot="dropdown">
                    <el-dropdown-item command="score">{{ t('Score') }}</el-dropdown-item>
                    <el-dropdown-item command="view">{{ t('View') }}</el-dropdown-item>
                    <el-dropdown-item v-if="removable" command="locate">{{ t('Locate') }}</el-dropdown-item>
                    <el-dropdown-item v-if="removable" command="remove">{{ t('Remove') }}</el-dropdown-item>
                </el-dropdown-menu>
            </el-dropdown>
            <el-dialog v-if="visible" :visible.sync="visible" :title="reference.promptCode"
                append-to-body width="min(920px, 96vw)" top="4vh" custom-class="ncf-prompt-score-dialog" :close-on-click-modal="false"
                :show-close="!busy" :close-on-press-escape="!busy">
                <p>{{ t('LastScoreWins') }}</p>
                <p v-if="error" role="alert" class="ncf-prompt-selector-error">{{ error }}</p>
                <div v-loading="scoreLoading" class="ncf-prompt-score-editor">
                    <div class="ncf-prompt-score-heading"><strong>{{ t('ScoringTarget') }}: {{ reference.promptCode }}</strong>
                        <output class="ncf-prompt-score-value">{{ Number.isFinite(score) ? score.toFixed(1) : '—' }} / 10</output>
                    </div>
                    <el-slider v-if="!scoreLoading" v-model="score" :min="0" :max="10" :step="0.1" show-input :show-input-controls="false"
                        :disabled="busy" :format-tooltip="value => value.toFixed(1)" :aria-label="t('Score')"></el-slider>
                    <p class="ncf-prompt-selector-hint">{{ t('DraftHint') }}</p>
                </div>
                <prompt-reference-score-chart v-if="!scoreLoading" :reference="reference" :score="score"></prompt-reference-score-chart>
                <p v-if="!info || !info.hasBenchmark || !aiScore">{{ t('AIUnavailable') }}</p>
                <div slot="footer">
                    <el-button :disabled="busy" @click="visible=false">{{ t('Cancel') }}</el-button>
                    <el-button v-if="aiScore" :disabled="busy || scoreLoading || !info || !info.hasBenchmark"
                        @click="autoScore">{{ t('AIScore') }}</el-button>
                    <el-button type="primary" :loading="busy" :disabled="scoreLoading" @click="save">{{ t('Save') }}</el-button>
                </div>
            </el-dialog>
        </span>`
    });

    Vue.component('prompt-reference-content', {
        props: { content: String, references: Array, aiScore: Function },
        computed: {
            parts() {
                let start = 0;
                const parts = [];
                for (const reference of this.references || []) {
                    if (reference.start > start) parts.push({ text: this.content.slice(start, reference.start) });
                    parts.push({ text: this.content.slice(reference.start, reference.start + reference.length), reference });
                    start = reference.start + reference.length;
                }
                if (start < this.content.length) parts.push({ text: this.content.slice(start) });
                return parts;
            }
        },
        template: `<div class="ncf-prompt-reference-content" data-i18n-skip><template v-for="(part, index) in parts"><span
            v-if="!part.reference" :key="index">{{ part.text }}</span><span v-else :key="index"
            class="ncf-prompt-reference-segment"><prompt-reference-tag :reference="part.reference"
            :ai-score="aiScore"></prompt-reference-tag><span>{{ part.text }}</span></span></template></div>`
    });
})(window);
