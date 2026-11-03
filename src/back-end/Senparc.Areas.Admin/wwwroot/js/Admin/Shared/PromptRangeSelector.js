(function (window) {
    'use strict';

    const api = '/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.';
    const bindings = new WeakMap();
    let dialog;
    let target;
    let searchVersion = 0;
    let detailVersion = 0;
    const t = key => window.ncfT('PromptSelector.' + key);
    const inputOf = element => element.querySelector('textarea, input[type="text"], input:not([type])');
    const editable = input => input && input.isConnected && !input.disabled && !input.readOnly;

    async function request(method, params) {
        const response = await service.get(api + method, { params: params, customAlert: true });
        const body = response && response.data;
        if (!body || !body.success || body.data == null) {
            throw new Error(body && (body.errorMessage || body.msg) || t('LoadFailed'));
        }
        return body.data;
    }

    function errorMessage(error) {
        const status = error.response && error.response.status;
        if (status === 401 || status === 403) return t('AccessDenied');
        if (status === 404) return t('Unavailable');
        return t('LoadFailed') + (error.message ? ' ' + error.message : '');
    }

    function getDialog() {
        if (dialog) return dialog;
        const host = document.createElement('div');
        document.body.appendChild(host);
        dialog = new Vue({
            el: host,
            data: function () {
                return {
                    visible: false, loading: false, detailLoading: false, error: '',
                    options: [], rangeId: '', tactic: '', keyword: '', sortBy: 'score',
                    items: [], total: 0, page: 1, pageSize: 10, selected: null
                };
            },
            computed: {
                ranges: function () {
                    return this.options.filter((item, index, items) =>
                        items.findIndex(other => other.rangeId === item.rangeId) === index);
                },
                tactics: function () {
                    return this.options.filter(item => item.rangeId === this.rangeId);
                }
            },
            methods: {
                t: t,
                formatScore: function (score) {
                    return Number.isFinite(score) && score >= 0 ? score.toFixed(1) : t('Unrated');
                },
                reload: async function () {
                    const version = ++searchVersion;
                    ++detailVersion;
                    this.loading = true;
                    this.error = '';
                    this.selected = null;
                    this.items = [];
                    this.total = 0;
                    try {
                        const options = await request('GetSelectorOptionsAsync');
                        if (version !== searchVersion || !this.visible) return;
                        this.options = options;
                        await this.search(1);
                    } catch (error) {
                        if (version === searchVersion && this.visible) this.error = errorMessage(error);
                    } finally {
                        if (version === searchVersion) this.loading = false;
                    }
                },
                search: async function (page) {
                    const version = ++searchVersion;
                    ++detailVersion;
                    this.page = page;
                    this.loading = true;
                    this.detailLoading = false;
                    this.error = '';
                    this.selected = null;
                    this.items = [];
                    this.total = 0;
                    try {
                        const result = await request('SearchSelectorAsync', {
                            rangeId: this.rangeId || 0, tactic: this.tactic,
                            keyword: this.keyword.trim(), pageIndex: page, pageSize: this.pageSize,
                            sortBy: this.sortBy
                        });
                        if (version !== searchVersion || !this.visible) return;
                        this.items = result.items;
                        this.total = result.totalCount;
                    } catch (error) {
                        if (version === searchVersion && this.visible) this.error = errorMessage(error);
                    } finally {
                        if (version === searchVersion) this.loading = false;
                    }
                },
                changeRange: function () {
                    this.tactic = '';
                    this.search(1);
                },
                select: async function (item) {
                    const version = ++detailVersion;
                    this.selected = null;
                    this.detailLoading = true;
                    this.error = '';
                    try {
                        const result = await request('GetSelectorPromptAsync', { id: item.id });
                        if (version === detailVersion && this.visible) this.selected = result;
                    } catch (error) {
                        if (version === detailVersion && this.visible) this.error = errorMessage(error);
                    } finally {
                        if (version === detailVersion) this.detailLoading = false;
                    }
                },
                insert: function () {
                    if (!this.selected || !this.selected.content || this.loading || this.detailLoading) return;
                    const input = target && target.input;
                    // A session switch or an external edit must not redirect/overwrite an open picker's target.
                    if (!editable(input) || input.value !== target.value || !target.enabled()) {
                        this.error = t('TargetChanged');
                        return;
                    }
                    const content = this.selected.content;
                    const value = input.value.slice(0, target.start) + content + input.value.slice(target.end);
                    if (input.maxLength >= 0 && value.length > input.maxLength) {
                        this.error = t('TooLong');
                        return;
                    }
                    input.value = value;
                    input.dispatchEvent(new Event('input', { bubbles: true }));
                    const caret = target.start + content.length;
                    this.visible = false;
                    this.$nextTick(function () {
                        if (editable(input)) {
                            input.focus();
                            input.setSelectionRange(caret, caret);
                        }
                    });
                },
                close: function () {
                    ++searchVersion;
                    ++detailVersion;
                    const input = target && target.input;
                    target = null;
                    this.loading = false;
                    this.detailLoading = false;
                    this.selected = null;
                    this.items = [];
                    this.$nextTick(function () {
                        if (editable(input)) input.focus();
                    });
                }
            },
            template: `
                <el-dialog :title="t('Title')" :visible.sync="visible"
                    append-to-body width="min(960px, 94vw)" top="6vh"
                    custom-class="ncf-prompt-selector" :close-on-click-modal="false" @close="close">
                    <form class="ncf-prompt-selector-filters" @submit.prevent="search(1)">
                        <el-select v-model="rangeId" :placeholder="t('Range')" :aria-label="t('Range')"
                            filterable clearable :disabled="loading" @change="changeRange">
                            <el-option v-for="range in ranges" :key="range.rangeId" :value="range.rangeId"
                                :label="(range.rangeAlias || range.rangeName) + ' (' + range.rangeName + ')'"></el-option>
                        </el-select>
                        <el-select v-model="tactic" :placeholder="t('Tactic')" :aria-label="t('Tactic')"
                            filterable clearable :disabled="!rangeId || loading" @change="search(1)">
                            <el-option v-for="lane in tactics" :key="lane.tactic" :value="lane.tactic"
                                :label="'T' + lane.tactic"></el-option>
                        </el-select>
                        <el-input v-model="keyword" :placeholder="t('Keyword')" :aria-label="t('Keyword')"
                            maxlength="200" clearable @clear="search(1)"></el-input>
                        <el-button type="primary" native-type="submit" :loading="loading">{{ t('Search') }}</el-button>
                    </form>
                    <div class="ncf-prompt-selector-sort">
                        <span>{{ t('Sort') }}</span>
                        <el-select v-model="sortBy" :aria-label="t('Sort')" size="small"
                            :disabled="loading" @change="search(1)">
                            <el-option value="score" :label="t('SortScore')"></el-option>
                            <el-option value="promptCode" :label="t('SortPromptCode')"></el-option>
                        </el-select>
                    </div>
                    <div v-if="error" class="ncf-prompt-selector-error" role="alert">
                        <span>{{ error }}</span>
                        <el-button type="text" @click="reload">{{ t('Retry') }}</el-button>
                    </div>
                    <div class="ncf-prompt-selector-body">
                        <div class="ncf-prompt-selector-results" v-loading="loading">
                            <p v-if="!loading && !items.length">{{ t('Empty') }}</p>
                            <button v-for="item in items" :key="item.id" type="button"
                                :class="['ncf-prompt-selector-item', { 'is-selected': selected && selected.id === item.id }]"
                                :aria-pressed="!!selected && selected.id === item.id" @click="select(item)">
                                <strong>{{ item.nickName || item.fullVersion }}</strong>
                                <span>{{ item.rangeAlias || item.rangeName }} · {{ item.fullVersion }}</span>
                                <span v-if="item.isDraft">{{ t('Draft') }}</span>
                                <span class="ncf-prompt-selector-scores">
                                    {{ t('AverageScore') }}: {{ formatScore(item.evalAvgScore) }}
                                    · {{ t('MaxScore') }}: {{ formatScore(item.evalMaxScore) }}
                                </span>
                                <small data-i18n-skip>{{ item.content }}</small>
                            </button>
                        </div>
                        <div class="ncf-prompt-selector-preview" v-loading="detailLoading">
                            <h4>{{ t('Preview') }}</h4>
                            <template v-if="selected">
                                <strong>{{ selected.nickName || selected.fullVersion }}</strong>
                                <p class="ncf-prompt-selector-scores">
                                    {{ t('AverageScore') }}: {{ formatScore(selected.evalAvgScore) }}
                                    · {{ t('MaxScore') }}: {{ formatScore(selected.evalMaxScore) }}
                                </p>
                                <pre data-i18n-skip>{{ selected.content }}</pre>
                            </template>
                            <p v-else>{{ t('SelectHint') }}</p>
                        </div>
                    </div>
                    <el-pagination v-if="total" layout="prev, pager, next, total"
                        :current-page="page" :page-size="pageSize" :total="total" :disabled="loading"
                        @current-change="search"></el-pagination>
                    <div slot="footer">
                        <span class="ncf-prompt-selector-hint">{{ t('InsertHint') }}</span>
                        <el-button @click="visible = false">{{ t('Cancel') }}</el-button>
                        <el-button type="primary" :disabled="!selected || !selected.content || loading || detailLoading"
                            @click="insert">{{ t('Insert') }}</el-button>
                    </div>
                </el-dialog>`
        });
        return dialog;
    }

    Vue.directive('prompt-range', {
        inserted: function (element, binding) {
            const button = document.createElement('button');
            const state = { button: button, enabled: binding.value !== false };
            bindings.set(element, state);
            button.type = 'button';
            button.className = 'ncf-prompt-selector-trigger';
            button.textContent = 'PromptRange';
            button.title = t('Title');
            button.setAttribute('aria-label', t('Title'));
            button.setAttribute('aria-haspopup', 'dialog');
            button.disabled = !state.enabled || !editable(inputOf(element));
            button.addEventListener('mousedown', event => event.preventDefault());
            button.addEventListener('keydown', event => event.stopPropagation());
            button.addEventListener('click', function (event) {
                event.stopPropagation();
                const input = inputOf(element);
                if (!state.enabled || !editable(input)) return;
                const picker = getDialog();
                target = {
                    input: input, value: input.value,
                    start: input.selectionStart, end: input.selectionEnd,
                    enabled: () => state.enabled
                };
                picker.rangeId = '';
                picker.tactic = '';
                picker.keyword = '';
                picker.sortBy = 'score';
                picker.options = [];
                picker.visible = true;
                picker.reload();
            });
            element.classList.add('ncf-prompt-selector-input');
            element.appendChild(button);
        },
        componentUpdated: function (element, binding) {
            const state = bindings.get(element);
            if (!state) return;
            state.enabled = binding.value !== false;
            // Element UI applies disabled/readonly to its native input in its own render tick.
            Vue.nextTick(function () {
                if (bindings.get(element) === state) {
                    state.button.disabled = !state.enabled || !editable(inputOf(element));
                }
            });
        },
        unbind: function (element) {
            const state = bindings.get(element);
            if (!state) return;
            if (target && element.contains(target.input)) {
                dialog.visible = false;
                dialog.close();
            }
            state.button.remove();
            element.classList.remove('ncf-prompt-selector-input');
            bindings.delete(element);
        }
    });
})(window);
