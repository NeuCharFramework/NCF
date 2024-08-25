var app = new Vue({
    el: '#app',
    data() {
        return {
            xncfStat: {},
            xncfOpeningList: {},
            chartData: []
        };
    },
    mounted() {
        this.getXncfStat();
        this.getXncfOpening();
        this.fetchChartData();
    },
    methods: {
        async fetchChartData() {
            try {
                let response = await service.get('/api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.GetLogs');
                if (response.data && response.data.data && response.data.data.logs) {
                    this.chartData = response.data.data.logs;
                    this.initChart();
                } else {
                    console.error('Invalid API response:', response);
                }
            } catch (error) {
                console.error('Error fetching chart data:', error);
            }
        },
        initChart() {
            let chart1 = document.getElementById('firstChart');
            let chartOption1 = {
                title: {
                    text: '日志统计',
                    subtext: '近 14 天'
                },
                xAxis: {
                    type: 'category',
                    data: this.chartData.map(item => item.date)
                },
                yAxis: {
                    type: 'value',
                    axisLabel: {
                        formatter: '{value} 件'
                    }
                },
                tooltip: {
                    trigger: 'axis',
                    axisPointer: {
                        type: 'shadow'
                    }
                },
                legend: {
                    data: ['常规日志', '异常日志']
                },
                series: [
                    {
                        name: '常规日志',
                        type: 'bar',
                        stack: '总量',
                        data: this.chartData.map(item => item.normalLogCount),
                        color: '#91c7ae'
                    },
                    {
                        name: '异常日志',
                        type: 'bar',
                        stack: '总量',
                        data: this.chartData.map(item => item.exceptionLogCount),
                        color: '#d48265'
                    }
                ]
            };
            let chartInstance1 = echarts.init(chart1);
            chartInstance1.setOption(chartOption1);

            // 添加点击事件监听器  
            chartInstance1.on('click', params => {
                if (params.componentType === 'series') {
                    let date = params.name;
                    window.location.href = `/Admin/SenparcTrace/DateLog?date=${date}`;
                }
            });

            let chart2 = document.getElementById('secondChart');
            let chartOption2 = {
                title: {
                    text: '数量统计',
                    subtext: '动态数据'
                },
                legend: {
                    data: ['日志数量']
                },
                xAxis: {
                    type: 'category',
                    data: this.chartData.map(item => item.date)
                },
                yAxis: {
                    type: 'value',
                    axisLabel: {
                        formatter: '{value} 件'
                    }
                },
                tooltip: {
                    formatter: function (data, ticket, callback) {
                        return data.seriesName + ':' + data.value + '件';
                    }
                },
                series: [{
                    data: this.chartData.map(item => item.totalLogCount),
                    name: '日志数量',
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
