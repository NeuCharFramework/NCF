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
                    CourseId: 0,
                    name: "",
                    path: "",
                    disk: "",
                    size: 0,
                    extension: "",
                    only_buyer: "",
                    AdminRemark: "",
                    Remark: "",
                    Id: 0,

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
            service.get("/Admin/CourseAttach/Index?handler=List", { params: data }).then(res => {
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
                let { CourseId, name, path, disk, size, extension, only_buyer, download_times, AdminRemark, Remark, Id, } = row;
                this.dialog.data = {
                    CourseId, name, path, disk, size, extension, only_buyer, download_times, AdminRemark, Remark, Id,
                };
                this.dialog.title = '编辑课程附件';
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // 新增
                this.dialog.title = '新增课程附件';
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
                        CourseId: this.dialog.data.CourseId * 1,
                        name: this.dialog.data.name,
                        path: this.dialog.data.path,
                        AdminRemark: this.dialog.data.AdminRemark,
                        Remark: this.dialog.data.Remark,
                        Id: this.dialog.data.Id * 1,

                    };
                    service.post("/Admin/CourseAttach/Index?handler=Save", data).then(res => {
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
            service.post("/Admin/CourseAttach/Index?handler=Delete", ids).then(res => {
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
        },
        beforeBannerUpload(files) {
            let param = new FormData();
            param.append('files', files);
            //param.append('title', this.bannerData.title);
            //param.append('showState', this.bannerData.showState);
            this.bannerUpload(param);
        },
        async bannerUpload(param) {
            try {
                await this.bannerUploadAction(param);
            } catch (err) {
                this.$refs.upload.clearFiles();
                this.tipMessage = "上传图片失败";
            }
        },
        bannerUploadAction(param) {
            service.post("/Admin/UploadFile/Index?handler=Upload", param).then(res => {
                if (res.data.success) {
                    this.$refs.upload.clearFiles();
                }
            });
        },
        uploadImg() {
            this.$refs.upload.submit();
        },
    },
    mounted: {
    }
});
