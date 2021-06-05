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
        expandRowKeysCache: []
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
            var dataSource = this.tableData.filter(_ => onlyException ? _.isException : true);
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
        }
    }
});