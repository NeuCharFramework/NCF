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
                    id : 0,
userId : 0,
courseId : 0,
videoId : 0,
watchSeconds : 0,
watchedAt : "",
flag : "",
addTime : "",
addUserId : 0,
addUserName : "",
lastUpdateTime : "",
updateUserId : 0,
updateUserName : "",
adminRemark : "",
remark : "",
tenantId : 0,
deleted_at : "",

                },
                rules: {
                    id:[{required:true, message:"ID为必填项",trigger:"blur"}],
userId:[{required:true, message:"用户ID为必填项",trigger:"blur"}],
courseId:[{required:true, message:"课程ID为必填项",trigger:"blur"}],
videoId:[{required:true, message:"视频ID为必填项",trigger:"blur"}],
watchSeconds:[{required:true, message:"已观看秒数为必填项",trigger:"blur"}],
flag:[{required:true, message:"删除状态为必填项",trigger:"blur"}],
addTime:[{required:true, message:"添加时间为必填项",trigger:"blur"}],
lastUpdateTime:[{required:true, message:"最后修改时间为必填项",trigger:"blur"}],
tenantId:[{required:true, message:"租户Id为必填项",trigger:"blur"}],

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
            service.get("/Admin/VideoUserWatchRecord/Index?handler=List", { params: data}).then(res => {
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
                let { id,userId,courseId,videoId,watchSeconds,watchedAt,flag,addTime,addUserId,addUserName,lastUpdateTime,updateUserId,updateUserName,adminRemark,remark,tenantId,deleted_at, } = row;
                this.dialog.data = {
                    id,userId,courseId,videoId,watchSeconds,watchedAt,flag,addTime,addUserId,addUserName,lastUpdateTime,updateUserId,updateUserName,adminRemark,remark,tenantId,deleted_at,
                };
                this.dialog.title = '编辑视频观看记录';
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // 新增
                this.dialog.title = '新增视频观看记录';
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
                        id : this.dialog.data.id*1,
userId : this.dialog.data.userId*1,
courseId : this.dialog.data.courseId*1,
videoId : this.dialog.data.videoId*1,
watchSeconds : this.dialog.data.watchSeconds*1,
watchedAt : this.dialog.data.watchedAt,
flag : this.dialog.data.flag,
addTime : this.dialog.data.addTime,
addUserId : this.dialog.data.addUserId*1,
addUserName : this.dialog.data.addUserName,
lastUpdateTime : this.dialog.data.lastUpdateTime,
updateUserId : this.dialog.data.updateUserId*1,
updateUserName : this.dialog.data.updateUserName,
adminRemark : this.dialog.data.adminRemark,
remark : this.dialog.data.remark,
tenantId : this.dialog.data.tenantId*1,
deleted_at : this.dialog.data.deleted_at,

                    };
                    service.post("/Admin/VideoUserWatchRecord/Index?handler=Save", data).then(res => {
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
            service.post("/Admin/VideoUserWatchRecord/Index?handler=Delete", ids).then(res => {
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
