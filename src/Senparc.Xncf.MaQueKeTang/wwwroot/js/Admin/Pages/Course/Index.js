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
            tableDataCategory: [],
            dialog: {
                title: '新增',
                visible: false,
                data: {
                    AccountId: 0,
                    title: "",
                    slug: "",
                    thumb: "",
                    charge: 0,
                    short_description: "",
                    original_desc: "",
                    render_desc: "",
                    seo_keywords: "",
                    seo_description: "",
                    published_at: "",
                    is_show: "",
                    category_id: [],
                    is_rec: "",
                    user_count: 0,
                    is_free: "",
                    comment_status: "",
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
                checkStrictly: true, // 是否严格的遵守父子节点不互相关联	
                dialogSetData: [],
                dialogSetSelected: [], //多选框
                setId: '',
                setTitle: '',
                isVerTrue: false // 校验是否通过
            },
            options: [{
                value: '0',
                label: '关闭评论'
            }, {
                value: '1',
                label: '所有人均可评论'
            }, {
                value: '2',
                label: '订阅后可评论'
            }]
        };
    },
    created: function () {
        this.getList();
    },
    watch: {
    },
    methods: {
        // 获取课程数据
        getList() {
            //let { key, pageIndex, pageSize } = this.listQuery;
            let data = {
                pageIndex: this.listQuery.pageIndex,
                pageSize: this.listQuery.pageSize,
                key: this.listQuery.key
            };
            service.get("/Admin/Course/Index?handler=List", { params: data }).then(res => {
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
                let { AccountId, title, slug, thumb, charge, short_description, original_desc, render_desc, seo_keywords, seo_description, published_at, is_show, category_id, is_rec, user_count, is_free, comment_status, Flag, AddTime, LastUpdateTime, AdminRemark, Remark, Id, } = row;
                this.dialog.data = {
                    AccountId, title, slug, thumb, charge, short_description, original_desc, render_desc, seo_keywords, seo_description, published_at, is_show, category_id:[category_id], is_rec, user_count, is_free, comment_status, Flag, AddTime, LastUpdateTime, AdminRemark, Remark, Id,
                };
                this.dialog.title = '编辑课程';
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // 新增
                this.dialog.title = '新增课程';
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
                        AccountId: this.dialog.data.AccountId * 1,
                        title: this.dialog.data.title,
                        slug: this.dialog.data.slug,
                        thumb: this.dialog.data.thumb,
                        charge: this.dialog.data.charge * 1,
                        short_description: this.dialog.data.short_description,
                        original_desc: this.dialog.data.original_desc,
                        render_desc: this.dialog.data.render_desc,
                        seo_keywords: this.dialog.data.seo_keywords,
                        seo_description: this.dialog.data.seo_description,
                        published_at: this.dialog.data.published_at,
                        is_show: this.dialog.data.is_show * 1,
                        category_id: this.dialog.data.category_id[this.dialog.data.category_id.length - 1] * 1,
                        is_rec: this.dialog.data.is_rec * 1,
                        user_count: this.dialog.data.user_count * 1,
                        is_free: this.dialog.data.is_free * 1,
                        comment_status: this.dialog.data.comment_status * 1,
                        AdminRemark: this.dialog.data.AdminRemark,
                        Remark: this.dialog.data.Remark,
                        Id: this.dialog.data.Id * 1,

                    };
                    service.post("/Admin/Course/Index?handler=Save", data).then(res => {
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
            service.post("/Admin/Course/Index?handler=Delete", ids).then(res => {
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
        handleAvatarSuccess(res, file) {
            this.imageUrl = URL.createObjectURL(file.raw);
        },
        beforeAvatarUpload(file) {
            const isJPG = file.type === 'image/jpeg';
            const isLt2M = file.size / 1024 / 1024 < 2;

            if (!isJPG) {
                this.$message.error('上传头像图片只能是 JPG 格式!');
            }
            if (!isLt2M) {
                this.$message.error('上传头像图片大小不能超过 2MB!');
            }
            return isJPG && isLt2M;
        },
        // 弹窗打开回调（更新数据）
        initTinymce() {
            this.getCourseCategoryList();
            tinymce.init({
                selector: '#mytextarea',
                plugins: 'advlist autolink link image lists preview', //字符串方式
                toolbar: [
                    'undo | bold italic ',
                    'alignleft styleselect alignright',
                ],
            });
        },
        // 获取课程分类数据
        getCourseCategoryList() {
            let data = {
                pageIndex: this.listQuery.pageIndex,
                pageSize: this.listQuery.pageSize,
                key: this.listQuery.key
            };
            service.get("/Admin/CourseCategory/Index?handler=List", { params: data }).then(res => {
                if (res.data.success) {
                    //res.data.data.columnHeaders.forEach((item) => {
                    //    item.key = item.key.substring(0, 1).toLowerCase() + item.key.substring(1)
                    //})
                    //this.headerList = res.data.data.columnHeaders.filter(u => u.browsable);
                    //this.paginationQuery.total = res.data.data.totalCount;
                    const b = res.data.data.list;
                    let allCategory = [];
                    this.ddd(b, 0, allCategory);
                    this.tableDataCategory = allCategory;
                }
            }).catch(error => {
                console.log(error)
            });
        },
        // 数据处理
        ddd(source, parentId, dest) {
            var array = source.filter(_ => _.parent_id === parentId);
            for (var i in array) {
                var ele = array[i];
                ele.children = [];
                dest.unshift(ele);
                this.ddd(source, ele.id, ele.children);
            }
        },
        // 章节
        handleChapter(index, row) {
            console.log(resizeUrl())
            window.open("/Admin/CourseChapter/Index/?uid=" + resizeUrl().uid);
        },
        // 附件
        handleAttachment(index, row) {
            window.open("/Admin/CourseAttach/Index/?uid=" + resizeUrl().uid);
        },
        // 用户观看
        handleUserRecord(index, row) {
            window.open("/Admin/CourseUserRecord/Index/?uid=" + resizeUrl().uid);
        }
    },
    mounted: {
    }
});
