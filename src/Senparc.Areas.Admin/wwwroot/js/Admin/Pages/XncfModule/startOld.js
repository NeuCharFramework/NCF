var app = new Vue({
    el: "#app",
    data() {
        return {
            data: [], // 数据
            tooltip: {
                "IAreaRegister": '网页',
                "IXncfDatabase": '数据库',
                "IXncfMiddleware": '中间件',
                "IXncfRazorRuntimeCompilation": '线程'
            },
            state: {
                'String': '文本',
                'Int32': '数字',
                'Int64': '数字',
                'DateTime': '日期',
                'String[]': '选项'
            },
            xNcfModules_State: {
                0: '关闭',
                1: '开放',
                2: '新增待审核',
                3: '更新待审核'
            },
            // 执行弹窗
            run: {
                data: {},
                visible: false
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
                    title: '提示',
                    message: '请开启后执行',
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
                    title: '提示',
                    message: '请开启后执行',
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
                    this.runData[res.name].value = [];
                    this.runData[res.name].item = res;
                    res.selectionList.items.map(ele => {
                        if (ele.defaultSelected) {
                            this.runData[res.name].value.push(ele.value);
                        }
                    });
                }
                // 下拉框value
                if (res.parameterType === 1 && res.selectionList.items) {
                    this.runData[res.name] = {};
                    this.runData[res.name].value = '';
                    this.runData[res.name].item = res;
                    res.selectionList.items.map(ele => {
                        if (ele.defaultSelected) {
                            this.runData[res.name].value = ele.value;
                        }
                    });
                    // 如果没有默认给第一个
                    if (this.runData[res.name].value.length === 0) {
                        this.runData[res.name].value = res.selectionList.items[0].value;
                    }
                }
                // 输入框
                if (res.parameterType === 0) {
                    this.runData[res.name] = {};
                    this.runData[res.name].item = res;
                    this.runData[res.name].value = res.value || '';
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
                    title: '警告',
                    message: '请填写源码物理路径',
                    type: 'warning'
                });
                return;
            }
            // 关闭执行弹窗
            // this.run.visible = false;
            let xncfFunctionParams = {};
            for (var i in this.runData) {
                // 多选
                if (this.runData[i].item.parameterType === 2) {
                    if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                        this.$notify({
                            title: '提示',
                            message: this.runData[i].item.title + '  为必选项',
                            type: 'warning'
                        });
                        return;
                    } else {
                        xncfFunctionParams[i] = {};
                        xncfFunctionParams[i].SelectedValues = [];
                        xncfFunctionParams[i].SelectedValues = this.runData[i].value;

                    }
                }
                // 下拉框value为字符串，但接口要数组
                if (this.runData[i].item.parameterType === 1) {
                    if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                        this.$notify({
                            title: '提示',
                            message: this.runData[i].item.title + '  为必填项',
                            type: 'warning'
                        });
                        return;
                    } else {
                        xncfFunctionParams[i] = {};
                        xncfFunctionParams[i].SelectedValues = [];
                        xncfFunctionParams[i].SelectedValues[0] = this.runData[i].value;
                    }
                }
                // 输入框
                if (this.runData[i].item.parameterType === 0) {
                    if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                        this.$notify({
                            title: '提示',
                            message: this.runData[i].item.title + '  为必填项',
                            type: 'warning'
                        });
                        return;
                    } else {
                        xncfFunctionParams[i] = this.runData[i].value;
                    }
                }
            }
            const data = {
                xncfUid: this.data.xncfModule.uid, xncfFunctionName: this.run.data.key.name, xncfFunctionParams: JSON.stringify(xncfFunctionParams)
            };
            const res = await service.post(`/Admin/XncfModule/Start?handler=RunFunction`, data);
            this.runResult.tempId = res.data.tempId;
            if ((res.data.log || '').length > 0 && (res.data.tempId || '').length > 0) {
                this.runResult.hasLog = true;
            }
            if (!res.data.success) {
                this.runResult.tit = '遇到错误';
                this.runResult.tip = '错误信息';
                this.runResult.msg = res.data.msg;
                return;
            }
            if (res.data.msg && (res.data.msg.indexOf('http://') !== -1 || res.data.msg.indexOf('https://') !== -1)) {
                this.runResult.tit = '执行成功';
                this.runResult.tip = '收到网址，点击下方打开<br />（此链接由第三方提供，请注意安全）：';
                this.runResult.msg = '<i class="fa fa-external-link"></i> <a href="' + res.data.msg + '" target="_blank">' + res.data.msg + '</a>';
            }
            else {
                this.runResult.tit = '执行成功';
                this.runResult.tip = '返回信息';
                this.runResult.msg = res.data.msg;
            }
            // 打开执行结果弹窗
            this.runResult.visible = true;
            this.getList();
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
            window.sessionStorage.setItem('setNavMenuActive', '模块管理');
            getNavMenu();
            setTimeout(function () {
                window.location.href = '/Admin/XncfModule/Index';
            }, 100);
        }
    }
});
