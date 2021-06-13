
var app = new Vue({
    el: '#app',
    data() {
        return {
            xncfStat: {},
            xncfOpeningList: {}
        };
    },
    mounted() {
        this.getXncfStat();
        this.getXncfOpening();
        this.initChart();
    },
    methods: {
        initChart() {
            let chart1 = document.getElementById('firstChart');
            let chartOption1 = {
                title: {
                    text: '数量统计',
                    subtext: '2019年11月1日 - 2019年11月5日'
                },
                xAxis: {
                    type: 'category',
                    data: ['商品', '数据', '订单', '消息', '异常', '审批', '退单', '新订单']
                },
                yAxis: {
                    type: 'value',
                    axisLabel: {
                        formatter: '{value} 件'
                    }
                }, tooltip: {

                },
                series: [{
                    data: [5, 20, 36, 10, 15, 40, 33, 17],
                    type: 'bar',
                    color: '#91c7ae'
                }]
            };

            let chartInstance1 = echarts.init(chart1);
            chartInstance1.setOption(chartOption1);


            let chart2 = document.getElementById('secondChart');
            let chartOption2 = {
                title: {
                    text: '数量统计',
                    subtext: '2019年度'
                }, legend: {
                    data: ['自由商品销售额']
                },
                xAxis: {
                    type: 'category',
                    data: ['一月', '二月', '三月', '四月', '五月', '六月', '七月', '八月', '九月', '十月', '十一月', '十二月']
                },
                yAxis: {
                    type: 'value',
                    axisLabel: {
                        formatter: '{value} 元'
                    }
                }, tooltip: {
                    formatter: function (data, ticket, cllback) {
                        //debugger
                        return data.seriesName + ':' + formatCurrency(data.value) + '元';
                    }
                },
                series: [{
                    data: [2666, 2778, 4926, 5767, 6810, 5670, 4123, 5687, 3654, 4999, 5301, 7358],
                    name: '自由商品销售额',
                    type: 'line'
                }]
            };

            let chartInstance2 = echarts.init(chart2);
            chartInstance2.setOption(chartOption2);
        },
        //XNCF 统计状态
        async getXncfStat() {
            let xncfStatData = await service.get('/Admin/Index?handler=XncfStat');
            this.xncfStat = xncfStatData.data.data;
        },
        //开放模块数据
        async getXncfOpening() {
            let xncfOpeningList = await service.get('/Admin/Index?handler=XncfOpening');
            this.xncfOpeningList = xncfOpeningList.data.data;
        }
    }
});
