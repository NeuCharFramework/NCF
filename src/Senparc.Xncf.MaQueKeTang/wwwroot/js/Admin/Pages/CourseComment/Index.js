var app = new Vue({
    el: "#app",
    data() {
        return {
            //分页参数
            paginationQuery: {
                total: 5
            },
            //分页接口传参
            listQuery: {
                pageIndex: 1,
                pageSize: 20,
                key: ''
            },
            listLoading: true, // 主列表记录总数
            headerList: [],

            tableData: [],
            dialog: {
                title: '新增',
                visible: false,
                data: {
                    AccountId : 0,
CourseId : 0,
original_content : "",
render_content : "",
deleted_at : "",
Flag : "",
AddTime : "",
LastUpdateTime : "",
AdminRemark : "",
Remark : "",
TenantId : 0,
Id : 0,

                },
                rules: {
                    remark: [
                        { required: true, message: "Remark为必填项", trigger: "blur" }
                    ]
                },
                updateLoading: false,
                updateLoadingSet: false, // 确认loading按钮
                dialogSetData: [],
                dialogSetSelected: [], //多选框
                setId: '',
                setTitle: '',
                isVerTrue: false // 校验是否通过
            }
        };
    },
    created: function () {
        this.getList();
    },
    computed: {

    },
    watch: {
    },
    methods: {
        // 获取数据
        getList() {
            //let { key, pageIndex, pageSize } = this.listQuery;
            let data = {
                pageIndex: this.listQuery.pageIndex,
                pageSize: this.listQuery.pageSize,
                key: this.listQuery.key
            };
            service.get("/Admin/CourseComment/Index?handler=List", { params: data}).then(res => {
                if (res.data.success) {
                    this.tableData = res.data.data.list;
                    res.data.data.columnHeaders.forEach((item) => {
                        item.key = item.key.substring(0, 1).toLowerCase() + item.key.substring(1)
                    })
                    this.headerList = res.data.data.columnHeaders.filter(u => u.browsable);
                    this.paginationQuery.total = res.data.data.totalCount;
                }
            }).catch(error => {
                console.log(error)
            });
        },
        // 编辑
        handleEdit(index, row) {
            this.dialog.visible = true;
            if (row) {
                // 编辑
                let { AccountId,CourseId,original_content,render_content,deleted_at,Flag,AddTime,LastUpdateTime,AdminRemark,Remark,TenantId,Id, } = row;
                this.dialog.data = {
                    AccountId,CourseId,original_content,render_content,deleted_at,Flag,AddTime,LastUpdateTime,AdminRemark,Remark,TenantId,Id,
                };
                this.dialog.title = '编辑课程介绍';
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // 新增
                this.dialog.title = '新增课程介绍';
            }
        },
        // 更新新增编辑
        updateData() {
            this.$refs['dataForm'].validate(valid => {
                // 表单校验
                if (valid) {
                    // 需要校验
                    if (this.isVerPass) {
                        // 校验不通过
                        if (!this.dialog.isVerTrue) {
                            console.log('不通过');
                            return false;
                        }
                    }
                    this.dialog.updateLoading = true;
                    let data = {
                        AccountId : this.dialog.data.AccountId*1,
CourseId : this.dialog.data.CourseId*1,
original_content : this.dialog.data.original_content,
render_content : this.dialog.data.render_content,
deleted_at : this.dialog.data.deleted_at,
Flag : this.dialog.data.Flag,
AddTime : this.dialog.data.AddTime,
LastUpdateTime : this.dialog.data.LastUpdateTime,
AdminRemark : this.dialog.data.AdminRemark,
Remark : this.dialog.data.Remark,
TenantId : this.dialog.data.TenantId*1,
Id : this.dialog.data.Id*1,

                    };
                    service.post("/Admin/CourseComment/Index?handler=Save", data).then(res => {
                        if (res.data.success) {
                            this.getList();
                            this.$notify({
                                title: "Success",
                                message: "成功",
                                type: "success",
                                duration: 2000
                            });
                            this.dialog.visible = false;
                            this.dialog.updateLoading = false;
                        }
                    }).catch(error => {
                        this.dialog.updateLoading = false;
                    });
                }
            });
        },
        // 删除
        handleDelete(index, row) {
            let ids = [row.id];
            service.post("/Admin/CourseComment/Index?handler=Delete", ids).then(res => {
                if (res.data.success) {
                    this.getList();
                    this.$notify({
                        title: "Success",
                        message: "删除成功",
                        type: "success",
                        duration: 2000
                    });
                }
            });
        },
        handleFilter() {
            this.listQuery.pageIndex = 1
            this.getList()
        }
    },
    mounted: {
    }
});
