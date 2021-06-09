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
                    id: 0,
                    courseId: 0,
                    ParentId: 0,
                    title: "",
                    sort: 0,
                    adminRemark: "",
                    remark: "",
                    tenantId: 0,
                },
                rules: {
                    title: [{ required: true, message: "标题为必填项", trigger: "blur" }],
                },
                checkStrictly: true, // 是否严格的遵守父子节点不互相关联	
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
            let data = {
                pageIndex: this.listQuery.pageIndex,
                pageSize: this.listQuery.pageSize,
                key: this.listQuery.key
            };
            service.get("/Admin/CourseChapter/Index?handler=List", { params: data }).then(res => {
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
                let { id, courseId, title, sort, flag, addTime, addUserId, addUserName, lastUpdateTime, updateUserId, updateUserName, adminRemark, remark,  } = row;
                this.dialog.data = {
                    id, courseId, title, sort, flag, addTime, addUserId, addUserName, lastUpdateTime, updateUserId, updateUserName, adminRemark, remark,
                };
                this.dialog.title = '编辑课程章节';
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // 新增
                this.dialog.title = '新增课程章节';
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
                        id: this.dialog.data.id * 1,
                        courseId: this.dialog.data.courseId * 1,
                        title: this.dialog.data.title,
                        sort: this.dialog.data.sort * 1,
                        flag: this.dialog.data.flag,
                        addTime: this.dialog.data.addTime,
                        addUserId: this.dialog.data.addUserId * 1,
                        addUserName: this.dialog.data.addUserName,
                        lastUpdateTime: this.dialog.data.lastUpdateTime,
                        updateUserId: this.dialog.data.updateUserId * 1,
                        updateUserName: this.dialog.data.updateUserName,
                        adminRemark: this.dialog.data.adminRemark,
                        remark: this.dialog.data.remark,
                        tenantId: this.dialog.data.tenantId * 1,
                        deleted_at: this.dialog.data.deleted_at,

                    };
                    service.post("/Admin/CourseChapter/Index?handler=Save", data).then(res => {
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
            service.post("/Admin/CourseChapter/Index?handler=Delete", ids).then(res => {
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
