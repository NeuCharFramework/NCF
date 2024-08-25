var app = new Vue({
    el: '#app',
    data() {
        return {
            xncfStat: {},
            xncfOpeningList: {},
            chartData: [],
            todayLogData: [] // 新增用于存储今日日志数据  
        };
    },
    mounted() {
        this.getXncfStat();
        this.getXncfOpening();
        this.fetchChartData();
        this.fetchTodayLogData(); // 新增调用获取今日日志数据的方法  
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
        async fetchTodayLogData() { // 新增获取今日日志数据的方法  
            try {
                let response = await service.get('/api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.GetTodayLog');
                if (response.data && response.data.data && response.data.data.items) {
                    this.todayLogData = response.data.data.items;
                    this.initChart(); // 确保图表在获取到数据后更新  
                } else {
                    console.error('Invalid API response:', response);
                }
            } catch (error) {
                console.error('Error fetching today log data:', error);
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
                        type: 'line',
                        stack: '总量',
                        areaStyle: {},
                        data: this.chartData.map(item => item.normalLogCount),
                        color: '#91c7ae'
                    },
                    {
                        name: '异常日志',
                        type: 'line',
                        stack: '总量',
                        areaStyle: {},
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

            // 准备今日日志数据  
            let todayLogCategories = this.todayLogData.map(item => item.senparcTraceType);
            let todayLogCounts = this.todayLogData.map(item => item.count);

            let chart2 = document.getElementById('secondChart');
            let chartOption2 = {
                title: {
                    text: '今日日志统计',
                    subtext: '动态数据'
                },
                legend: {
                    data: todayLogCategories // 自动输出所有类别  
                },
                xAxis: {
                    type: 'category',
                    data: todayLogCategories
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
                    data: todayLogCounts,
                    name: '日志数量',
                    type: 'bar',
                    color: '#73c0de'
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
