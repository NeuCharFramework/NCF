var app = new Vue({
    el: "#app",
    data() {
        return {
            data: [], // 数据
            tooltip: {
                "IAreaRegister": ncfT('Xncf.AreaFeature'),
                "IXncfDatabase": ncfT('Xncf.DatabaseFeature'),
                "IXncfMiddleware": ncfT('Xncf.MiddlewareFeature'),
                "IXncfRazorRuntimeCompilation": ncfT('Xncf.ThreadFeature')
            },
            state: {
                'String': ncfT('Xncf.TypeText'),
                'Int32': ncfT('Xncf.TypeNumber'),
                'Int64': ncfT('Xncf.TypeNumber'),
                'DateTime': ncfT('Xncf.TypeDate'),
                'String[]': ncfT('Xncf.TypeOption'),
                'Boolean': ncfT('Xncf.TypeBoolean')
            },
            xNcfModules_State: {
                0: ncfT('Xncf.Close'),
                1: ncfT('Xncf.Open'),
                2: ncfT('Xncf.NewModules'),
                3: ncfT('Xncf.PendingUpdates')
            },
            // 执行弹窗
            run: {
                data: {},
                visible: false,
                loading: false
            },
            runData: {
                // 绑定数据
            },
            runResult: {
                visible: false,
                tit: '',
                tip: '',
                msg: '',
                tempId: '',
                hasLog: false
            },
            //查看线程 
            thread: {
                visible: false
            }
        };
    },
    created() {
        this.getList();
    },
    methods: {
        safeHtml(value) {
            return typeof DOMPurify === 'undefined' ? '' : DOMPurify.sanitize(String(value || ''));
        },
        normalizeMultiValue(value) {
            if (Array.isArray(value)) {
                return value;
            }

            if (typeof value !== 'string' || value.length === 0) {
                return [];
            }

            return value
                .split(/[;,，；\n\r|]+/)
                .map(item => item.trim())
                .filter(item => item.length > 0);
        },
        async  getList() {
            const uid = resizeUrl().uid;
            const res = await service.get(`/Admin/XncfModule/Start?handler=Detail&uid=${uid}`);
            this.data = res.data.data;
            this.data.xncfRegister.interfaces = this.data.xncfRegister.interfaces.splice(1);
            window.document.title = this.data.xncfModule.menuName;
        },


        // 打开首页
        openUrl(url, flag) {
            // 关闭状态返回
            flag = flag + '';
            if (flag !== '1') {
                this.$notify({
                    title: ncfT('Xncf.Prompt'),
                    message: ncfT('Xncf.EnableFirst'),
                    type: 'warning'
                });
                return;
            }
            window.location.href = url;
        },
        // 打开执行
        openRun(item, flag) {
            // 关闭状态返回
            flag = flag + '';
            if (flag !== '1') {
                this.$notify({
                    title: ncfT('Xncf.Prompt'),
                    message: ncfT('Xncf.EnableFirst'),
                    type: 'warning'
                });
                return;
            }
            this.run.data = item;
            this.runData = {};
            this.run.data.value.map(res => {
                // 动态model绑定生成
                // 默认选择赋值
                // 多选
                if (res.parameterType === 2 && res.selectionList.items) {
                    this.runData[res.name] = {};
                    this.runData[res.name].value = this.normalizeMultiValue(res.value);
                    this.runData[res.name].item = res;
                    res.selectionList.items.map(ele => {
                        if (ele.defaultSelected && this.runData[res.name].value.indexOf(ele.value) < 0) {
                            this.runData[res.name].value.push(ele.value);
                        }
                    });
                }
                // 下拉框value
                if (res.parameterType === 1 && res.selectionList.items) {
                    this.runData[res.name] = {};
                    this.runData[res.name].value = res.value || '';
                    this.runData[res.name].item = res;
                    res.selectionList.items.map(ele => {
                        if (!this.runData[res.name].value && ele.defaultSelected) {
                            this.runData[res.name].value = ele.value;
                        }
                    });
                    // 如果没有默认给第一个
                    if (this.runData[res.name].value.length === 0) {
                        this.runData[res.name].value = res.selectionList.items[0].value;
                    }
                }
                // 输入框
                if (res.parameterType === 0 || res.parameterType === 3) {
                    this.runData[res.name] = {};
                    this.runData[res.name].item = res;
                    this.runData[res.name].value = res.value || '';
                }
                // 布尔（单个复选框）
                if (res.parameterType === 4) {
                    this.runData[res.name] = {};
                    this.runData[res.name].item = res;
                    this.runData[res.name].value = res.value === true || res.value === 'true' || res.value === 'True';
                }
            });
            this.runData = Object.assign({}, this.runData);
            //  this.runData数组结构
            //在接口传输时，将下拉单选转成数组
            //{
            //   // parameterType === 2 多选
            //    Modules: {
            //        item: {},
            //        value: []
            //    },
            //   // parameterType === 1 下拉单选
            //    ReferenceType: {
            //        item: {},
            //        value: []
            //    },
            //   // parameterType === 0 input
            //    SourcePath: {
            //        item: {},
            //        value: ''
            //    }
            //};
            this.run.visible = true;
        },
        // 执行
        async handleRun() {
            // 物理路径校验
            if (this.runData.hasOwnProperty('SourcePath') && this.runData.SourcePath.length < 1) {
                this.$notify({
                    title: ncfT('Xncf.Prompt'),
                    message: ncfT('Xncf.SourcePathRequired'),
                    type: 'warning'
                });
                return;
            }

            // 设置 loading 状态
            this.run.loading = true;

            try {
                let xncfFunctionParams = {};
                for (var i in this.runData) {
                    // 多选
                    if (this.runData[i].item.parameterType === 2) {
                        if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                            this.$notify({
                                title: ncfT('Xncf.Prompt'),
                                message: ncfT('Xncf.RequiredOption', this.runData[i].item.title),
                                type: 'warning'
                            });
                            return;
                        } else {
                            xncfFunctionParams[i] = this.runData[i].value;
                        }
                    }
                    // 下拉框
                    if (this.runData[i].item.parameterType === 1) {
                        if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                            this.$notify({
                                title: ncfT('Xncf.Prompt'),
                                message: ncfT('Xncf.RequiredField', this.runData[i].item.title),
                                type: 'warning'
                            });
                            return;
                        } else {
                            xncfFunctionParams[i] = this.runData[i].value;
                        }
                    }
                    // 输入框
                    if (this.runData[i].item.parameterType === 0 || this.runData[i].item.parameterType === 3) {
                        if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                            this.$notify({
                                title: ncfT('Xncf.Prompt'),
                                message: ncfT('Xncf.RequiredField', this.runData[i].item.title),
                                type: 'warning'
                            });
                            return;
                        } else {
                            xncfFunctionParams[i] = this.runData[i].value;
                        }
                    }
                    // 布尔
                    if (this.runData[i].item.parameterType === 4) {
                        xncfFunctionParams[i] = !!this.runData[i].value;
                    }
                }
                const data = {
                    xncfUid: this.data.xncfModule.uid,
                    xncfFunctionName: this.run.data.key.name,
                    xncfFunctionParams: JSON.stringify(xncfFunctionParams)
                };

                const res = await service.post(`/Admin/XncfModule/Start?handler=RunFunction`, data, { customAlert: true });
                
                this.runResult.tempId = res.data.tempId;
                if ((res.data.log || '').length > 0 && (res.data.tempId || '').length > 0) {
                    this.runResult.hasLog = true;
                }

                const msg = DOMPurify.sanitize(res.data.msg);

                if (!res.data.success) {
                    this.runResult.tit = ncfT('Xncf.RunError');
                    this.runResult.tip = ncfT('Xncf.ErrorInfo');
                    this.runResult.msg = (msg || DOMPurify.sanitize(res.data.exception)).replace(/&lt;br \/&gt;/g, '<br />').replace('\r\n', '<br />').replace('\n', '<br />').replace('\r', '<br />');
                    this.runResult.visible = true;
                    return;
                }
                if (msg && (msg.indexOf('http://') === 0 || msg.indexOf('https://') === 0)) {
                    this.runResult.tit = ncfT('Xncf.RunSuccess');
                    this.runResult.tip = ncfT('Xncf.UrlResultTip');
                    try {
                        const safeUrl = new URL(msg, window.location.origin);
                        if (safeUrl.protocol !== 'http:' && safeUrl.protocol !== 'https:') {
                            throw new Error('unsupported URL scheme');
                        }
                        const anchor = document.createElement('a');
                        anchor.href = safeUrl.href;
                        anchor.target = '_blank';
                        anchor.rel = 'noopener noreferrer';
                        anchor.textContent = msg;
                        this.runResult.msg = this.safeHtml('<i class="fa fa-external-link"></i> ') + anchor.outerHTML;
                    } catch {
                        this.runResult.msg = this.safeHtml(msg);
                    }
                }
                else {
                    this.runResult.tit = ncfT('Xncf.RunSuccess');
                    this.runResult.tip = ncfT('Xncf.ReturnInfo');
                    this.runResult.msg = msg.replace(/&lt;br \/&gt;/g, '<br />').replace('\r\n', '<br />').replace('\n', '<br />').replace('\r','<br />');
                }
                // 打开执行结果弹窗
                this.runResult.visible = true;
                this.getList();
            } catch (error) {
                console.error('执行出错:', error);
                this.$notify({
                    title: ncfT('Xncf.RunError'),
                    message: ncfT('Xncf.ExecutionError'),
                    type: 'error'
                });
            } finally {
                // 无论成功失败都取消 loading 状态
                this.run.loading = false;
            }
        },
        // 关闭和开启
        async updataState(state) {
            const id = this.data.xncfModule.id;
            const res = await service.get(`/Admin/XncfModule/Start?handler=ChangeState&id=${id}&tostate=${state}`);
            window.location.reload();
        },
        // 更新版本
        async  updataVersion() {
            const uid = resizeUrl().uid;
            await service.get(`/Admin/XncfModule/Index?handler=ScanAjax&uid=${uid}`);
            window.location.reload();
        },
        // 删除
        async handleDelete() {
            const id = this.data.xncfModule.id;
            const res = await service.post(`/Admin/XncfModule/Start?handler=Delete&id=${id}`);
            window.sessionStorage.setItem('setNavMenuActive', ncfT('Xncf.Title'));
            getNavMenu();
            setTimeout(function () {
                window.location.href = '/Admin/XncfModule/Index';
            }, 100);
        }
    }
});
