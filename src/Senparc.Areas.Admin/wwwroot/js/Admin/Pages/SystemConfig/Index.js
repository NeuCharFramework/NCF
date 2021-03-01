var app = new Vue({
    el: "#app",
    data() {
        return {
            //分页参数
            paginationQuery: {
                total: 5
            },
            //分页接口传参（只会有一个）
            listQuery: {
                pageIndex: 1,
                pageSize: 20,
            },
            tableData: [],
            tenantData: {},
            dialog: {
                title: '编辑系统信息',
                visible: false,
                data: {
                    id: 0,
                    systemName: '',
                },
                rules: {
                    systemName: [
                        { required: true, message: "用户名为必填项", trigger: "blur" }
                    ]
                }
            },
            updateLoading: false,
            updateLoadingSet: false, // 确认loading按钮
        };
    },
    created: function () {
        this.getList();
    },
    computed: {
    },
    watch: {
        'dialog.visible': function (val, old) {
            // 关闭dialog，清空
            if (!val) {
                this.dialog.data = {
                    id: 0,
                    systemName: ''
                };
                this.dialog.updateLoading = false;
                this.$refs['dataForm'].resetFields();
            }
        }
    },
    methods: {
        // 获取数据
        getList() {
            let { pageIndex, pageSize } = this.listQuery;
            service.get(`/Admin/SystemConfig/index?handler=List&&pageIndex=${pageIndex}&pageSize=${pageSize}`).then(res => {
                this.tableData = res.data.data.list;
                this.paginationQuery.total = res.data.data.totalCount;
            });
        },
        // 编辑
        handleEdit(index, row) {
            this.dialog.visible = true;
            if (row) {
                // 编辑
                let { systemName, id } = row;
                this.dialog.data = {
                    systemName, id
                };
                this.dialog = Object.assign({}, this.dialog);
            }
        },
        // 更新新增编辑
        updateData() {
            this.$refs['dataForm'].validate(valid => {
                // 表单校验
                if (valid) {
                    this.dialog.updateLoading = true;
                    let data = {
                        Id: this.dialog.data.id,
                        SystemName: this.dialog.data.systemName,
                    };
                    service.post("/Admin/SystemConfig/Edit?handler=Save", data).then(res => {
                        if (res.data.success) {
                            this.getList();
                            this.$notify({
                                title: "Success",
                                message: "更新成功！",
                                type: "success",
                                duration: 2000
                            });
                            this.dialog.visible = false;
                            this.dialog.updateLoading = false;
                        } else {
                            this.$notify({
                                title: "Faild",
                                message: "更新失败：" + res.data.msg,
                                type: "success",
                                duration: 2000
                            });
                        }
                    }).catch(error => {
                        this.dialog.updateLoading = false;
                    });
                }
            });
        },
    }
});
