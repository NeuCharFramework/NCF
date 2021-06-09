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
                    name: "",
                    parent_id: [],
                    parent_name: "",
                    is_show: 0,
                    sort: 0,
                    AdminRemark: "",
                    Remark: "",
                    id: 0,

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
            service.get("/Admin/CourseCategory/Index?handler=List", { params: data }).then(res => {
                if (res.data.success) {
                    //this.tableData = res.data.data.list;
                    res.data.data.columnHeaders.forEach((item) => {
                        item.key = item.key.substring(0, 1).toLowerCase() + item.key.substring(1)
                    })
                    this.headerList = res.data.data.columnHeaders.filter(u => u.browsable);
                    this.paginationQuery.total = res.data.data.totalCount;
                    const b = res.data.data.list;
                    let allMenu = [];
                    this.ddd(b, 0, allMenu);
                    this.tableData = allMenu;
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
        // 编辑 // 新增 // 增加下一级
        handleEdit(index, row, flag) {
            this.dialog.visible = true;
            if (row) {
                // 编辑.
                console.log(row);
                let { name, parent_id, parent_name, is_show, sort,  AdminRemark, Remark, id, } = row;
                this.dialog.data = {
                    name, parent_id: [parent_id], parent_name, is_show, sort, AdminRemark, Remark, id,
                };
                this.dialog.title = '编辑课程分类';
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // 新增
                this.dialog.title = '新增课程分类';
            }
            // dialog中父级菜单 做递归显示
            let x = [];
            this.recursionFunc(row, this.tableData, x);
            this.dialog.data.parent_id = x;
            if (flag === 'addNext') {
                this.dialog.data.id = 0;
                this.dialog.title = '增加下一级分类';
                this.dialog.data.name = '';
                this.dialog.data.parent_id.push(row.id);
            }
        },
        // 设置父级菜单默认显示 递归
        recursionFunc(row, source, dest) {
            if (row.parent_id === null) {
                return;
            }
            for (let i in source) {
                let ele = source[i];
                if (row.parent_id === ele.id) {
                    this.recursionFunc(ele, this.tableData, dest);
                    dest.push(ele.id);
                } else {
                    this.recursionFunc(row, ele.children, dest);
                }
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
                        name: this.dialog.data.name,
                        parent_id: this.dialog.data.parent_id[this.dialog.data.parent_id.length - 1] * 1,
                        parent_name: this.dialog.data.parent_name,
                        is_show: this.dialog.data.is_show * 1,
                        sort: this.dialog.data.sort * 1,
                        AdminRemark: this.dialog.data.AdminRemark,
                        Remark: this.dialog.data.Remark,
                        Id: this.dialog.data.id * 1,

                    };
                    service.post("/Admin/CourseCategory/Index?handler=Save", data).then(res => {
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
            service.post("/Admin/CourseCategory/Index?handler=Delete", ids).then(res => {
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
