(function (global) {
    'use strict';

    if (!global.Vue || !global.document || !global.document.getElementById) {
        return;
    }

    var host = global.document.getElementById('ncf-global-function-host');
    if (!host) {
        return;
    }

    function unwrap(response) {
        if (!response || !response.data) {
            return null;
        }
        return Object.prototype.hasOwnProperty.call(response.data, 'data')
            ? response.data.data
            : response.data;
    }

    function parameterValue(parameter, name) {
        return parameter[name] !== undefined
            ? parameter[name]
            : parameter[name.charAt(0).toUpperCase() + name.slice(1)];
    }

    var functionBus = new global.Vue();
    var globalFunctionApp = new global.Vue({
        el: host,
        data: function () {
            return {
                visible: false,
                loading: false,
                running: false,
                descriptor: null,
                values: {},
                result: {
                    visible: false,
                    title: '',
                    htmlMode: false,
                    html: '',
                    content: ''
                }
            };
        },
        template: `
            <div class="ncf-global-function-root">
                <el-dialog
                    v-if="descriptor"
                    :title="descriptor.name"
                    :visible.sync="visible"
                    width="min(720px, 92vw)"
                    top="7vh"
                    custom-class="ncf-global-function-dialog"
                    :close-on-click-modal="false"
                    append-to-body>
                    <div v-loading="loading || running">
                        <p class="ncf-global-function-description">{{descriptor.description}}</p>
                        <el-form label-position="top" @submit.native.prevent>
                            <el-form-item
                                v-for="parameter in descriptor.parameters"
                                :key="parameterName(parameter)"
                                :label="parameter.title || parameter.name"
                                :required="parameter.isRequired">
                                <el-input
                                    v-if="parameter.parameterType === 0 || parameter.parameterType === 3"
                                    v-model="values[parameterName(parameter)]"
                                    :type="parameter.parameterType === 3 ? 'password' : 'text'"
                                    :maxlength="parameter.maxLength || 500"></el-input>
                                <el-input
                                    v-else-if="parameter.parameterType === 5"
                                    v-model="values[parameterName(parameter)]"
                                    type="textarea"
                                    :rows="8"
                                    :maxlength="parameter.maxLength || 20000"
                                    show-word-limit></el-input>
                                <el-select
                                    v-else-if="parameter.parameterType === 1"
                                    v-model="values[parameterName(parameter)]"
                                    :filterable="parameter.filterable"
                                    :allow-create="parameter.allowCreate"
                                    :default-first-option="parameter.allowCreate">
                                    <el-option
                                        v-for="option in selectionItems(parameter)"
                                        :key="option.value"
                                        :label="option.text"
                                        :value="option.value"></el-option>
                                </el-select>
                                <el-checkbox-group
                                    v-else-if="parameter.parameterType === 2"
                                    v-model="values[parameterName(parameter)]">
                                    <el-checkbox
                                        v-for="option in selectionItems(parameter)"
                                        :key="option.value"
                                        :label="option.value">
                                        {{option.text}}
                                    </el-checkbox>
                                </el-checkbox-group>
                                <el-switch
                                    v-else-if="parameter.parameterType === 4"
                                    v-model="values[parameterName(parameter)]"></el-switch>
                                <small v-if="parameter.description" class="ncf-global-function-help">
                                    {{parameter.description}}
                                </small>
                            </el-form-item>
                        </el-form>
                    </div>
                    <span slot="footer" class="dialog-footer">
                        <el-button @click="close">取消</el-button>
                        <el-button type="primary" :loading="running" :disabled="loading || running" @click="run">
                            {{running ? '执行中' : '执行'}}
                        </el-button>
                    </span>
                </el-dialog>

                <el-dialog
                    :title="result.title"
                    :visible.sync="result.visible"
                    width="min(760px, 92vw)"
                    custom-class="ncf-global-function-result-dialog"
                    append-to-body>
                    <div v-if="result.htmlMode" class="ncf-global-function-result-html" v-html="result.html"></div>
                    <pre v-else class="ncf-global-function-result-text">{{result.content}}</pre>
                    <span slot="footer"><el-button type="primary" @click="result.visible=false">关闭</el-button></span>
                </el-dialog>
            </div>
        `,
        created: function () {
            functionBus.$on('open', this.open);
        },
        beforeDestroy: function () {
            functionBus.$off('open', this.open);
        },
        methods: {
            parameterName: function (parameter) {
                return parameter.name || parameter.Name;
            },
            selectionItems: function (parameter) {
                var selectionList = parameter.selectionList || parameter.SelectionList;
                return selectionList && Array.isArray(selectionList.items || selectionList.Items)
                    ? (selectionList.items || selectionList.Items)
                    : [];
            },
            createValues: function (parameters) {
                var values = {};
                (parameters || []).forEach(function (parameter) {
                    var name = this.parameterName(parameter);
                    var value = parameterValue(parameter, 'value');
                    if (parameter.parameterType === 2) {
                        value = Array.isArray(value)
                            ? value.slice()
                            : (typeof value === 'string'
                                ? value.split(/[;,，；\n\r|]+/).map(function (item) {
                                    return item.trim();
                                }).filter(Boolean)
                                : []);
                    } else if (parameter.parameterType === 4) {
                        value = value === true || value === 'true' || value === 'True';
                    } else if (value === null || value === undefined) {
                        value = '';
                    }

                    var options = this.selectionItems(parameter);
                    if ((parameter.parameterType === 1 || parameter.parameterType === 2) && options.length) {
                        if (parameter.parameterType === 1 && !value) {
                            var defaultOption = options.find(function (option) {
                                return option.defaultSelected || option.DefaultSelected;
                            });
                            value = defaultOption
                                ? (defaultOption.value || defaultOption.Value)
                                : (options[0].value || options[0].Value);
                        }
                        if (parameter.parameterType === 2) {
                            options.forEach(function (option) {
                                var optionValue = option.value || option.Value;
                                if ((option.defaultSelected || option.DefaultSelected) &&
                                    value.indexOf(optionValue) < 0) {
                                    value.push(optionValue);
                                }
                            });
                        }
                    }
                    values[name] = value;
                }, this);
                return values;
            },
            firstMissingRequired: function () {
                var parameters = this.descriptor ? this.descriptor.parameters : [];
                return parameters.find(function (parameter) {
                    if (!parameter.isRequired) {
                        return false;
                    }
                    var value = this.values[this.parameterName(parameter)];
                    return value === null || value === undefined || value === '' ||
                        (Array.isArray(value) && value.length === 0);
                }, this);
            },
            open: async function (options) {
                options = options || {};
                if (!options.moduleUid || !options.functionKey) {
                    this.$message.error('全局 Function 映射缺少模块 UID 或 Function Key。');
                    return;
                }

                this.visible = true;
                this.loading = true;
                this.running = false;
                this.result.visible = false;
                this.descriptor = {
                    name: options.title || options.functionKey,
                    description: '',
                    parameters: []
                };
                try {
                    var response = await global.service.get('/Admin/NeuCharPivot/Function?handler=Describe' +
                        '&moduleUid=' + encodeURIComponent(options.moduleUid) +
                        '&functionKey=' + encodeURIComponent(options.functionKey), {
                            customAlert: true
                        });
                    var descriptor = unwrap(response);
                    this.descriptor = descriptor;
                    this.values = this.createValues(descriptor.parameters || []);
                } catch (error) {
                    this.visible = false;
                    this.$message.error('当前账号无权访问该 Function，或模块已不可用。');
                } finally {
                    this.loading = false;
                }
            },
            close: function () {
                if (!this.running) {
                    this.visible = false;
                }
            },
            async run() {
                var missing = this.firstMissingRequired();
                if (missing) {
                    this.$message.warning('请先填写“' + (missing.title || missing.name) + '”。');
                    return;
                }

                this.running = true;
                try {
                    var response = await global.service.post('/Admin/NeuCharPivot/Function?handler=Run', {
                        moduleUid: this.descriptor.moduleUid,
                        functionKey: this.descriptor.functionKey,
                        parametersJson: JSON.stringify(this.values)
                    }, { customAlert: true });
                    var result = unwrap(response) || {};
                    var value = result.data;
                    this.result.title = this.descriptor.name + ' · ' + (result.success ? '执行成功' : '执行失败');
                    this.result.htmlMode = result.success === true && typeof value === 'string';
                    this.result.html = this.result.htmlMode ? this.sanitizeHtml(value) : '';
                    this.result.content = this.result.htmlMode
                        ? ''
                        : (result.success
                            ? JSON.stringify(value === undefined ? null : value, null, 2)
                            : (result.errorMessage || '执行失败'));
                    this.result.visible = true;
                } catch (error) {
                    this.$message.error('Function 执行请求失败。');
                } finally {
                    this.running = false;
                }
            },
            sanitizeHtml: function (value) {
                if (!global.DOMPurify || typeof global.DOMPurify.sanitize !== 'function') {
                    return String(value == null ? '' : value).replace(/</g, '&lt;').replace(/>/g, '&gt;');
                }
                return global.DOMPurify.sanitize(String(value == null ? '' : value), {
                    ALLOWED_TAGS: [
                        'a', 'b', 'blockquote', 'br', 'code', 'div', 'em',
                        'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'hr', 'i',
                        'li', 'mark', 'ol', 'p', 'pre', 'small', 'span',
                        'strong', 'sub', 'sup', 'table', 'tbody', 'td',
                        'th', 'thead', 'tr', 'u', 'ul'
                    ],
                    ALLOWED_ATTR: ['href', 'title'],
                    ALLOW_ARIA_ATTR: false,
                    ALLOW_DATA_ATTR: false,
                    ALLOWED_URI_REGEXP: /^https?:\/\/[^\s<>"']+$/i
                });
            }
        }
    });

    global.NeuCharPivotFunction = Object.freeze({
        open: function (options) {
            functionBus.$emit('open', options || {});
        }
    });
})(window);
