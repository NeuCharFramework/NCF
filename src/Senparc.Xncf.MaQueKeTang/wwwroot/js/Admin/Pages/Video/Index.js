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
            checkStrictly: true, // 是否严格的遵守父子节点不互相关联	
            courseChapterOptions: [],
            courseOptions: [],
            videoType: "aliyun",
            tableData: [],
            dialog: {
                title: '新增',
                visible: false,
                data: {
                    id: 0,
                    userId: 0,
                    courseId: 0,
                    title: "",
                    slug: "",
                    url: "",
                    charge: 0,
                    viewNum: 0,
                    shortDescription: "",
                    originalDesc: "",
                    renderDesc: "",
                    seoKeywords: "",
                    seoDescription: "",
                    publishedAt: "",
                    isShow: 0,
                    aliyunVideoId: "",
                    chapterId: 0,
                    duration: 0,
                    tencentVideoId: "",
                    isBanSell: 0,
                    commentStatus: 0,
                    freeSeconds: 0,
                    playerPC: "",
                    playerH5: "",
                    banDrag: 0,
                    adminRemark: "",
                    remark: "",
                },
                rules: {
                    id: [{ required: true, message: "ID为必填项", trigger: "blur" }],
                    userId: [{ required: true, message: "用户ID为必填项", trigger: "blur" }],
                    courseId: [{ required: true, message: "课程ID为必填项", trigger: "blur" }],
                    title: [{ required: true, message: "标题为必填项", trigger: "blur" }],
                    slug: [{ required: true, message: "slug为必填项", trigger: "blur" }],
                    url: [{ required: true, message: "播放地址为必填项", trigger: "blur" }],
                    charge: [{ required: true, message: "价格为必填项", trigger: "blur" }],
                    viewNum: [{ required: true, message: "观看次数为必填项", trigger: "blur" }],
                    shortDescription: [{ required: true, message: "简短介绍为必填项", trigger: "blur" }],
                    originalDesc: [{ required: true, message: "简介原始内容为必填项", trigger: "blur" }],
                    renderDesc: [{ required: true, message: "简介渲染后的内容为必填项", trigger: "blur" }],
                    seoKeywords: [{ required: true, message: "SEO关键字为必填项", trigger: "blur" }],
                    seoDescription: [{ required: true, message: "SEO描述为必填项", trigger: "blur" }],
                    isShow: [{ required: true, message: "1显示,-1隐藏为必填项", trigger: "blur" }],
                    chapterId: [{ required: true, message: "章节ID为必填项", trigger: "blur" }],
                    duration: [{ required: true, message: "时长，单位：秒为必填项", trigger: "blur" }],
                    isBanSell: [{ required: true, message: "禁止售卖,0否,1是为必填项", trigger: "blur" }],
                    commentStatus: [{ required: true, message: "0禁止评论,1所有人,2仅购买为必填项", trigger: "blur" }],
                    freeSeconds: [{ required: true, message: "试看秒数为必填项", trigger: "blur" }],
                    playerPC: [{ required: true, message: "pc播放器为必填项", trigger: "blur" }],
                    playerH5: [{ required: true, message: "h5播放器为必填项", trigger: "blur" }],
                    banDrag: [{ required: true, message: "禁止拖动,0否,1是为必填项", trigger: "blur" }],
                    flag: [{ required: true, message: "删除状态为必填项", trigger: "blur" }],
                    addTime: [{ required: true, message: "添加时间为必填项", trigger: "blur" }],
                    lastUpdateTime: [{ required: true, message: "最后修改时间为必填项", trigger: "blur" }],
                    tenantId: [{ required: true, message: "租户Id为必填项", trigger: "blur" }],

                },
                updateLoading: false,
                updateLoadingSet: false, // 确认loading按钮
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
            }],
            players: [{
                value: '0',
                label: '默认'
            }, {
                value: '1',
                label: '阿里云'
            }, {
                value: '2',
                label: '腾讯云'
            }],
        };
    },
    created: function () {
        this.getList();
        this.getCourseOptions();
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
            service.get("/Admin/Video/Index?handler=List", { params: data }).then(res => {
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
        //获取课程
        getCourseOptions() {
            let data = {
                pageIndex: this.listQuery.pageIndex,
                pageSize: this.listQuery.pageSize,
                key: this.listQuery.key
            };
            service.get("/Admin/Course/Index?handler=List", { params: data }).then(res => {
                if (res.data.success) {
                    this.courseOptions = res.data.data.list;
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
                let { id, userId, courseId, title, slug, url, charge, viewNum, shortDescription, originalDesc, renderDesc, seoKeywords, seoDescription, publishedAt, isShow, aliyunVideoId, chapterId, duration, tencentVideoId, isBanSell, commentStatus, freeSeconds, playerPC, playerH5, banDrag, adminRemark, remark, tenantId, } = row;
                this.dialog.data = {
                    id, userId, courseId, title, slug, url, charge, viewNum, shortDescription, originalDesc, renderDesc, seoKeywords, seoDescription, publishedAt, isShow, aliyunVideoId, chapterId, duration, tencentVideoId, isBanSell, commentStatus, freeSeconds, playerPC, playerH5, banDrag, adminRemark, remark, tenantId,
                };
                this.dialog.title = '编辑视频';
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // 新增
                this.dialog.title = '新增视频';
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
                        userId: this.dialog.data.userId * 1,
                        courseId: this.dialog.data.courseId * 1,
                        title: this.dialog.data.title,
                        slug: this.dialog.data.slug,
                        url: this.dialog.data.url,
                        charge: this.dialog.data.charge * 1,
                        viewNum: this.dialog.data.viewNum * 1,
                        shortDescription: this.dialog.data.shortDescription,
                        originalDesc: this.dialog.data.originalDesc,
                        renderDesc: this.dialog.data.renderDesc,
                        seoKeywords: this.dialog.data.seoKeywords,
                        seoDescription: this.dialog.data.seoDescription,
                        publishedAt: this.dialog.data.publishedAt,
                        isShow: this.dialog.data.isShow * 1,
                        aliyunVideoId: this.dialog.data.aliyunVideoId,
                        chapterId: this.dialog.data.chapterId * 1,
                        duration: this.dialog.data.duration * 1,
                        tencentVideoId: this.dialog.data.tencentVideoId,
                        isBanSell: this.dialog.data.isBanSell * 1,
                        commentStatus: this.dialog.data.commentStatus * 1,
                        freeSeconds: this.dialog.data.freeSeconds * 1,
                        playerPC: this.dialog.data.playerPC,
                        playerH5: this.dialog.data.playerH5,
                        banDrag: this.dialog.data.banDrag * 1,
                        adminRemark: this.dialog.data.adminRemark,
                        remark: this.dialog.data.remark,
                    };
                    service.post("/Admin/Video/Index?handler=Save", data).then(res => {
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
            service.post("/Admin/Video/Index?handler=Delete", ids).then(res => {
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
        courseOptionChange() {
            let data = {
                pageIndex: this.listQuery.pageIndex,
                pageSize: this.listQuery.pageSize,
                key: this.listQuery.key
            };
            service.get("/Admin/CourseChapter/Index?handler=List", { params: data }).then(res => {
                if (res.data.success) {
                    this.courseChapterOptions = res.data.data.list;
                }
            }).catch(error => {
                console.log(error)
            });
            // dialog中父级菜单 做递归显示
            let x = [];
            this.recursionFunc(row, this.courseChapterOptions, x);
            this.dialog.data.chapterId = x;
        },
        // 设置父级菜单默认显示 递归
        recursionFunc(row, source, dest) {
            if (row.parent_id === null) {
                return;
            }
            for (let i in source) {
                let ele = source[i];
                if (row.parent_id === ele.id) {
                    this.recursionFunc(ele, this.courseChapterOptions, dest);
                    dest.push(ele.id);
                } else {
                    this.recursionFunc(row, ele.children, dest);
                }
            }
        },
    },
    mounted: {
    }
});
