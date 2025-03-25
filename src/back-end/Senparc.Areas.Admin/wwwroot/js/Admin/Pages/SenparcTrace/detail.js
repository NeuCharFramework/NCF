var app = new Vue({
    el: '#app',
    data: {
        dateTitle: '',
        tableData: [],
        displayData: [],
        searchData: {
            pageIndex: 1,
            pageSize: 10,
            total: 0
        },
        weixinTraceTypeDesc: {
            0: 'Normal',
            2: 'API',
            4: 'PostRequest',
            6: 'GetRequest',
            8: 'Exception'
        },
        toogleException: false,
        expandRowKeys: [],
        expandRowKeysCache: [],
        filterConditions: {
            keyword: '',
            timeRange: [],
            threadId: '',
            traceType: '',
            exceptionStatus: '', // '': 全部, true: 是, false: 否
            availableThreads: [], // 用于存储所有可用的线程ID
            availableTypes: []    // 用于存储所有可用的类型
        },
        pickerOptions: {
            shortcuts: [{
                text: '今天',
                onClick(picker) {
                    const end = new Date();
                    const start = new Date();
                    picker.$emit('pick', [start, end]);
                }
            }, {
                text: '最近一小时',
                onClick(picker) {
                    const end = new Date();
                    const start = new Date();
                    start.setTime(start.getTime() - 3600 * 1000);
                    picker.$emit('pick', [start, end]);
                }
            }]
        }
    },
    mounted() {
    },
    created() {
        const date = resizeUrl().date;
        this.dateTitle = date;
        service.get(`/Admin/SenparcTrace/DateLog?handler=Detail&date=${date}`).then(res => {
            var responseData = res.data.data;
            var actualData = [];
            var expandRowKeys = [];
            var index = 1;
            responseData.map(ele => {
                ele.no = index++;
                ele.result.url_empty = true;
                ele.result.postData_empty = true;
                ele.result.result_empty = true;
                var showMessage = false;

                if (ele.result.url && ele.result.url.length > 0) {
                    ele.result.url_empty = false;
                    showMessage = true;
                }
                if (ele.result.result && ele.result.result.length > 0) {
                    ele.result.result_empty = false;
                    showMessage = true;
                }
                if (ele.result.postData && ele.result.postData.length > 0) {
                    ele.result.postData_empty = false;
                    showMessage = true;
                }
                if (ele.isException) {
                    showMessage = true;
                }
                ele.colSpan = 1;
                ele.showMessage = showMessage;
                actualData.push(ele);
                if (showMessage) {
                    expandRowKeys.push(ele.no);
                }
            });
            this.expandRowKeysCache = expandRowKeys;
            this.searchData.total = actualData.length;
            this.tableData = actualData;
            
            // 初始化可用的筛选选项
            const threads = new Set();
            const types = new Set();
            actualData.forEach(item => {
                threads.add(item.threadId);
                types.add(item.weixinTraceType);
            });
            this.filterConditions.availableThreads = Array.from(threads);
            this.filterConditions.availableTypes = Array.from(types);
            
            this.fetchData();
        });
    },
    methods: {
        arraySpanMethod({ row, column, rowIndex, columnIndex }) {
            //if (row.colSpan > 1) {
            //    return {
            //        rowspan: 1,
            //        colspan: 7
            //    };
            //}
            return {
                rowspan: 1,
                colspan: 1
            };
        },
        fetchData: function () {
            var onlyException = this.toogleException;
            var skipCount = (this.searchData.pageIndex - 1) * this.searchData.pageSize;
            
            // 应用所有筛选条件
            var dataSource = this.tableData.filter(item => {
                // 异常筛选
                if (onlyException && !item.isException) return false;
                
                // 关键字筛选
                if (this.filterConditions.keyword) {
                    const keyword = this.filterConditions.keyword.toLowerCase();
                    const matchContent = (item.title + ' ' + item.resultStr).toLowerCase();
                    if (!matchContent.includes(keyword)) return false;
                }
                
                // 时间区间筛选
                if (this.filterConditions.timeRange && this.filterConditions.timeRange.length === 2) {
                    const itemDate = new Date(item.dateTime);
                    if (itemDate < this.filterConditions.timeRange[0] || 
                        itemDate > this.filterConditions.timeRange[1]) return false;
                }
                
                // 线程筛选
                if (this.filterConditions.threadId && 
                    item.threadId !== this.filterConditions.threadId) return false;
                
                // 类型筛选
                if (this.filterConditions.traceType !== '' && 
                    item.weixinTraceType !== this.filterConditions.traceType) return false;
                
                // 异常状态筛选
                if (this.filterConditions.exceptionStatus !== '') {
                    if (this.filterConditions.exceptionStatus === 'true' && !item.isException) return false;
                    if (this.filterConditions.exceptionStatus === 'false' && item.isException) return false;
                }
                
                return true;
            });
            
            this.searchData.total = dataSource.length;
            this.displayData = dataSource.slice(skipCount, skipCount + this.searchData.pageSize);
        },
        showException: function () {
            this.toogleException = !this.toogleException;
            if (this.toogleException) {
                this.searchData.pageIndex = 1;
            }
            this.fetchData(this.toogleException);
        },
        showAll: function () {
            if (this.expandRowKeys.length > 0) {
                this.expandRowKeys = [];
                return;
            }
            var expandRowKeys = [];
            this.displayData.forEach(ele => {
                if (this.expandRowKeysCache.lastIndexOf(ele.no) >= 0) {
                    expandRowKeys.push(ele.no);
                }
            });
            this.expandRowKeys = expandRowKeys;
        },
        rowKey(row) {
            return row.no;
        },
        // 高亮显示关键字
        highlightKeyword: function(content) {
            if (!this.filterConditions.keyword) return content;
            const keyword = this.filterConditions.keyword;
            return content.replace(new RegExp(keyword, 'gi'), 
                match => `<span class="highlight-keyword">${match}</span>`);
        }
    }
});