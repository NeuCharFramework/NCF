var app = new Vue({
    el: '#app',
    data: {
        searchData: {
            pageIndex: 1,
            pageSize: 10,
            total: 0
        },
        tableData: []
    },
    mounted() {
    },
    created() {
        this.fetchData();
    },
    methods: {
        fetchData: function () {
            service.get(`/Admin/SenparcTrace/Index?handler=List&pageIndex=${this.searchData.pageIndex}&pageSize=${this.searchData.pageSize}`).then(res => {
                var responseData = res.data.data;
                const actualData = [];
                var startIndex = (this.searchData.pageIndex - 1) * this.searchData.pageSize;
                responseData.list.forEach(ele => {
                    startIndex++;
                    actualData.push({ no: startIndex, text: ele });
                });
                this.tableData = actualData;
                this.searchData.total = responseData.count;
                console.info(actualData);
            });
        }
    }
});