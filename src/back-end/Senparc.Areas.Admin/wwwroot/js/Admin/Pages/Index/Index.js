var app = new Vue({
  el: '#app',
  data() {
    return {
      isExpandAll: true,
      loading: false,
      refreshTable: true,
      xncfStat: {},
      xncfOpeningList: {},
      chartData: [],
      todayLogData: [],
      hostMetrics: {
        warnings: []
      },
      hostMetricsHistory: [],
      hostMetricsLoading: false,
      hostMetricsError: '',
      hostMetricsTimer: null,
      hostMetricsChart: null,
      hostMetricsResizeHandler: null,
      agentsOverview: {
        available: false
      },
      agentsOverviewLoading: false,
      agentsOverviewUnavailable: false,
      agentsOverviewTimer: null,
      agentsOverviewUpdatedAt: null,
      // 添加动画控制变量
      shakeAllModules: false,
      glowUpgradeableModules: false
    };
  },
  mounted() {
    this.getXncfStat();
    this.getXncfOpening();
    this.fetchChartData();
    this.fetchTodayLogData();
    this.fetchHostMetrics();
    this.startHostMetricsPolling();
    this.fetchAgentsOverview();
    this.startAgentsOverviewPolling();
    this.hostMetricsResizeHandler = () => {
      if (this.hostMetricsChart &&
        (typeof this.hostMetricsChart.isDisposed !== 'function' || !this.hostMetricsChart.isDisposed())) {
        this.hostMetricsChart.resize();
      }
    };
    window.addEventListener('resize', this.hostMetricsResizeHandler);
    // 添加鼠标事件监听
    this.initializeHoverEffects();
  },
  beforeDestroy() {
    if (this.hostMetricsTimer) {
      window.clearInterval(this.hostMetricsTimer);
      this.hostMetricsTimer = null;
    }
    if (this.agentsOverviewTimer) {
      window.clearInterval(this.agentsOverviewTimer);
      this.agentsOverviewTimer = null;
    }
    if (this.hostMetricsResizeHandler) {
      window.removeEventListener('resize', this.hostMetricsResizeHandler);
      this.hostMetricsResizeHandler = null;
    }
    if (this.hostMetricsChart &&
      (typeof this.hostMetricsChart.isDisposed !== 'function' || !this.hostMetricsChart.isDisposed())) {
      this.hostMetricsChart.dispose();
    }
    this.hostMetricsChart = null;
  },
  methods: {
    startAgentsOverviewPolling() {
      if (this.agentsOverviewTimer) {
        window.clearInterval(this.agentsOverviewTimer);
      }
      this.agentsOverviewTimer = window.setInterval(() => this.fetchAgentsOverview(), 5000);
    },
    async fetchAgentsOverview() {
      if (this.agentsOverviewLoading || this.agentsOverviewUnavailable || document.hidden) {
        return;
      }

      this.agentsOverviewLoading = true;
      try {
        const headers = { 'x-requested-with': 'XMLHttpRequest' };
        if (window.ncfJwtToken) {
          headers.Authorization = 'Bearer ' + window.ncfJwtToken;
        }
        const response = await axios.get(
          '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetDashboardOverview',
          { headers: headers });
        const payload = response && response.data;
        if (!payload || !payload.success || !payload.data) {
          throw new Error('AgentsManager overview is unavailable.');
        }

        this.agentsOverview = Object.assign({ available: true }, payload.data);
        this.agentsOverviewUpdatedAt = new Date();
      } catch (_) {
        // AgentsManager 是可选模块。端点不可用通常表示未安装、未启用或当前账号无权访问，首页保持无卡片状态。
        this.agentsOverviewUnavailable = true;
        this.agentsOverview = { available: false };
      } finally {
        this.agentsOverviewLoading = false;
      }
    },
    disabledCount(total, enabled) {
      return Math.max(0, (Number(total) || 0) - (Number(enabled) || 0));
    },
    agentsTotal() {
      return (Number(this.agentsOverview.localAgentCount) || 0) + (Number(this.agentsOverview.remoteA2AAgentCount) || 0);
    },
    agentsEnabledTotal() {
      return (Number(this.agentsOverview.localAgentEnabledCount) || 0) + (Number(this.agentsOverview.remoteA2AAgentEnabledCount) || 0);
    },
    agentsDisabledTotal() {
      return this.disabledCount(this.agentsTotal(), this.agentsEnabledTotal());
    },
    agentsActiveTotal() {
      return (Number(this.agentsOverview.activeLocalAgentCount) || 0) + (Number(this.agentsOverview.activeRemoteA2AAgentCount) || 0);
    },
    agentsChattingTotal() {
      return (Number(this.agentsOverview.chattingLocalAgentCount) || 0) + (Number(this.agentsOverview.chattingRemoteA2AAgentCount) || 0);
    },
    agentsOverviewUpdatedText() {
      if (!this.agentsOverviewUpdatedAt) {
        return '--';
      }
      return this.agentsOverviewUpdatedAt.toLocaleTimeString();
    },
    navigateToAgentsManager() {
      window.location.href = '/Admin/XncfModule/Start/?uid=D858D7FA-775A-4690-9023-CFB0B3B84994';
    },
    startHostMetricsPolling() {
      if (this.hostMetricsTimer) {
        window.clearInterval(this.hostMetricsTimer);
      }
      this.hostMetricsTimer = window.setInterval(() => this.fetchHostMetrics(), 2000);
    },
    async fetchHostMetrics() {
      if (this.hostMetricsLoading || document.hidden) {
        return;
      }

      this.hostMetricsLoading = true;
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.GetHostMetrics');
        const metrics = response && response.data && response.data.data;
        if (!metrics || !metrics.sampledAt) {
          throw new Error(ncfT('Admin.Home.HostMetricsInvalidResponse'));
        }

        this.hostMetrics = metrics;
        this.hostMetricsError = '';
        this.appendHostMetricsHistory(metrics);
        this.$nextTick(() => this.updateHostMetricsChart());
      } catch (error) {
        this.hostMetricsError = error && error.message
          ? error.message
          : ncfT('Admin.Home.HostMetricsUnavailable');
        console.warn('Host metrics refresh failed:', error);
      } finally {
        this.hostMetricsLoading = false;
      }
    },
    appendHostMetricsHistory(metrics) {
      this.hostMetricsHistory.push({
        sampledAt: metrics.sampledAt,
        cpu: this.toNullableNumber(metrics.cpuUsagePercent),
        processCpu: this.toNullableNumber(metrics.processCpuUsagePercent),
        memory: this.toNullableNumber(metrics.memoryUsagePercent),
        receiveMbps: this.bytesPerSecondToMbps(metrics.networkReceiveBytesPerSecond),
        sendMbps: this.bytesPerSecondToMbps(metrics.networkSendBytesPerSecond)
      });
      if (this.hostMetricsHistory.length > 30) {
        this.hostMetricsHistory.splice(0, this.hostMetricsHistory.length - 30);
      }
    },
    updateHostMetricsChart() {
      const chartElement = document.getElementById('hostMetricsChart');
      if (!chartElement) {
        return;
      }
      if (this.hostMetricsChart &&
        typeof this.hostMetricsChart.isDisposed === 'function' &&
        this.hostMetricsChart.isDisposed()) {
        this.hostMetricsChart = null;
      }
      if (!this.hostMetricsChart) {
        const existingChart = typeof echarts.getInstanceByDom === 'function'
          ? echarts.getInstanceByDom(chartElement)
          : null;
        this.hostMetricsChart = existingChart &&
          (typeof existingChart.isDisposed !== 'function' || !existingChart.isDisposed())
          ? existingChart
          : echarts.init(chartElement);
      }

      // 旧版 ECharts 在第一次 setOption() 前没有内部 model，此时调用 getOption() 会抛错。
      // 仅在已有 model 时读取图例状态，保证首屏能完成第一次绘制。
      const chartModel = typeof this.hostMetricsChart.getModel === 'function'
        ? this.hostMetricsChart.getModel()
        : true;
      const currentOption = chartModel && typeof this.hostMetricsChart.getOption === 'function'
        ? this.hostMetricsChart.getOption()
        : null;
      const selectedSeries = currentOption && currentOption.legend && currentOption.legend[0]
        ? Object.assign({}, currentOption.legend[0].selected || {})
        : {};
      const labels = this.hostMetricsHistory.map(item => this.formatSampleTime(item.sampledAt));
      this.hostMetricsChart.setOption({
        animationDurationUpdate: 300,
        title: {
          text: ncfT('Admin.Home.HostMetricsChart'),
          textStyle: { fontSize: 15, fontWeight: 500 }
        },
        tooltip: { trigger: 'axis' },
        legend: {
          top: 2,
          right: 8,
          selected: selectedSeries,
          data: [
            ncfT('Admin.Home.HostCpu'),
            ncfT('Admin.Home.HostProcessCpu'),
            ncfT('Admin.Home.HostMemory'),
            ncfT('Admin.Home.HostNetworkReceive'),
            ncfT('Admin.Home.HostNetworkSend')
          ]
        },
        grid: { left: 52, right: 58, top: 52, bottom: 38 },
        xAxis: {
          type: 'category',
          boundaryGap: false,
          data: labels
        },
        yAxis: [{
          type: 'value',
          name: '%',
          min: 0,
          max: 100
        }, {
          type: 'value',
          name: 'Mbps',
          min: 0,
          splitLine: { show: false }
        }],
        series: [{
          name: ncfT('Admin.Home.HostCpu'),
          type: 'line',
          smooth: true,
          showSymbol: false,
          data: this.hostMetricsHistory.map(item => item.cpu),
          lineStyle: { color: '#8c52ff' },
          itemStyle: { color: '#8c52ff' }
        }, {
          name: ncfT('Admin.Home.HostProcessCpu'),
          type: 'line',
          smooth: true,
          showSymbol: false,
          data: this.hostMetricsHistory.map(item => item.processCpu),
          lineStyle: { color: '#00a6a6', type: 'dashed' },
          itemStyle: { color: '#00a6a6' }
        }, {
          name: ncfT('Admin.Home.HostMemory'),
          type: 'line',
          smooth: true,
          showSymbol: false,
          data: this.hostMetricsHistory.map(item => item.memory),
          lineStyle: { color: '#67c23a' },
          itemStyle: { color: '#67c23a' }
        }, {
          name: ncfT('Admin.Home.HostNetworkReceive'),
          type: 'line',
          yAxisIndex: 1,
          smooth: true,
          showSymbol: false,
          data: this.hostMetricsHistory.map(item => item.receiveMbps),
          lineStyle: { color: '#409eff' },
          itemStyle: { color: '#409eff' }
        }, {
          name: ncfT('Admin.Home.HostNetworkSend'),
          type: 'line',
          yAxisIndex: 1,
          smooth: true,
          showSymbol: false,
          data: this.hostMetricsHistory.map(item => item.sendMbps),
          lineStyle: { color: '#e6a23c' },
          itemStyle: { color: '#e6a23c' }
        }]
      }, true);
    },
    toNullableNumber(value) {
      if (value === null || value === undefined || value === '') {
        return null;
      }
      const number = Number(value);
      return Number.isFinite(number) ? number : null;
    },
    normalizePercent(value) {
      const number = this.toNullableNumber(value);
      return number === null ? 0 : Math.max(0, Math.min(100, number));
    },
    barWidth(value) {
      return this.normalizePercent(value).toFixed(3) + '%';
    },
    boundedSubsetPercent(value, maximum) {
      const subset = this.normalizePercent(value);
      const maximumNumber = this.toNullableNumber(maximum);
      if (maximumNumber === null || maximumNumber <= 0) {
        return subset;
      }
      return Math.min(subset, this.normalizePercent(maximumNumber));
    },
    boundedSubsetWidth(value, maximum) {
      return this.boundedSubsetPercent(value, maximum).toFixed(3) + '%';
    },
    processMemoryPercent() {
      const processBytes = this.toNullableNumber(this.hostMetrics.processWorkingSetBytes);
      const totalBytes = this.toNullableNumber(this.hostMetrics.memoryTotalBytes);
      if (processBytes === null || totalBytes === null || totalBytes <= 0) {
        return 0;
      }
      return this.normalizePercent(processBytes / totalBytes * 100);
    },
    formatPercent(value) {
      const number = this.toNullableNumber(value);
      return number === null ? '--' : number.toFixed(1) + '%';
    },
    formatBytes(value) {
      const number = this.toNullableNumber(value);
      if (number === null) {
        return '--';
      }
      if (number <= 0) {
        return '0 B';
      }
      const units = ['B', 'KB', 'MB', 'GB', 'TB', 'PB'];
      const unitIndex = Math.min(Math.floor(Math.log(number) / Math.log(1024)), units.length - 1);
      const scaled = number / Math.pow(1024, unitIndex);
      const digits = scaled >= 100 ? 0 : (scaled >= 10 ? 1 : 2);
      return scaled.toFixed(digits) + ' ' + units[unitIndex];
    },
    formatRate(value) {
      const number = this.toNullableNumber(value);
      return number === null ? '--' : this.formatBytes(number) + '/s';
    },
    bytesPerSecondToMbps(value) {
      const number = this.toNullableNumber(value);
      return number === null ? null : Number((number * 8 / 1000000).toFixed(3));
    },
    formatDuration(value) {
      const totalSeconds = Math.max(0, Math.floor(Number(value) || 0));
      const days = Math.floor(totalSeconds / 86400);
      const hours = Math.floor((totalSeconds % 86400) / 3600);
      const minutes = Math.floor((totalSeconds % 3600) / 60);
      return (days > 0 ? days + 'd ' : '') +
        (hours > 0 || days > 0 ? hours + 'h ' : '') +
        minutes + 'm';
    },
    formatSampleTime(value) {
      const date = new Date(value);
      return Number.isNaN(date.getTime()) ? '--' : date.toLocaleTimeString();
    },
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
          this.todayDate = response.data.data.date;
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
          text: ncfT('Admin.Home.LogStats'),
          subtext: ncfT('Admin.Home.Last14Days')
        },
        xAxis: {
          type: 'category',
          data: this.chartData.map(item => item.date)
        },
        yAxis: {
          type: 'value',
          axisLabel: {
            formatter: '{value} ' + ncfT('Admin.Home.LogCountUnit')
          }
        },
        tooltip: {
          trigger: 'axis',
          axisPointer: {
            type: 'shadow'
          }
        },
          legend: {
          data: [ncfT('Admin.Home.NormalLogs'), ncfT('Admin.Home.ErrorLogs')]
        },
        series: [
          {
            name: ncfT('Admin.Home.NormalLogs'),
            type: 'line',
            stack: '总量',
            areaStyle: { color: '#91c7ae' }, // 添加区域填充颜色  
            data: this.chartData.map(item => item.normalLogCount),
            color: '#91c7ae'
          },
          {
            name: ncfT('Admin.Home.ErrorLogs'),
            type: 'line',
            stack: '总量',
            areaStyle: { color: '#d48265' }, // 添加区域填充颜色  
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
      let todayLogData = this.todayLogData.map(item => ({
        name: item.senparcTraceType,
        value: item.count
      }));

      let chart2 = document.getElementById('secondChart');
      let chartOption2 = {
        title: {
          text: ncfT('Admin.Home.TodayLogStats'),
          subtext: ncfT('Admin.Home.DynamicData'),
          left: 'center'
        },
        tooltip: {
          trigger: 'item',
          formatter: '{a} <br/>{b}: {c} ({d}%)'
        },
        legend: {
          orient: 'vertical',
          left: 'left',
          data: this.todayLogData.map(item => item.senparcTraceType) // 自动输出所有类别  
        },
        series: [{
          name: ncfT('Admin.Home.LogType'),
          type: 'pie',
          radius: '50%',
          data: todayLogData,
          emphasis: {
            itemStyle: {
              shadowBlur: 10,
              shadowOffsetX: 0,
              shadowColor: 'rgba(0, 0, 0, 0.5)'
            }
          }
        }]
      };
      let chartInstance2 = echarts.init(chart2);
      chartInstance2.setOption(chartOption2);
      // 添加点击事件监听器    
      chartInstance2.on('click', params => {
        if (params.componentType === 'series') {
          window.location.href = `/Admin/SenparcTrace/DateLog?date=${this.todayDate}`;
        }
      });

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
    },
    //点击打开模块
    navigateTo(uid) {
      window.location.href = '/Admin/XncfModule/Start/?uid=' + uid;
    },
    getOpenDetail(rowIndex, menus) {
      console.log(`rowIndex --- ${JSON.stringify(rowIndex)}`)
      console.log(`menus --- ${JSON.stringify(menus)}`)
      var menuInfo = menus[rowIndex]
      window.location.href = menuInfo.url
    },
    // 添加新方法处理悬停效果
    initializeHoverEffects() {
      // 获取统计项元素 - 修正选择器
      const installedModulesStat = document.querySelector('.xncf-stat-item');
      const updateModulesStat = document.querySelectorAll('.xncf-stat-item')[1];

      // 已安装模块统计项的鼠标事件
      if (installedModulesStat) {
        installedModulesStat.addEventListener('mouseenter', () => {
          this.triggerShakeAnimation();
        });
      }

      // 待更新模块统计项的鼠标事件
      if (updateModulesStat) {
        updateModulesStat.addEventListener('mouseenter', () => {
          this.triggerGlowAnimation();
        });
      }
    },

    // 触发抖动动画
    triggerShakeAnimation() {
      const moduleCards = document.querySelectorAll('#xncf-modules-area .box-card');
      moduleCards.forEach(card => {
        // 添加随机延迟
        const delay = Math.random() * 200; // 0-200ms的随机延迟
        setTimeout(() => {
          card.classList.add('shake-animation');
          // 动画结束后移除类
          setTimeout(() => {
            card.classList.remove('shake-animation');
          }, 800); // 与动画持续时间匹配
        }, delay);
      });
    },

    // 触发发光/淡化动画
    triggerGlowAnimation() {
      const allCards = document.querySelectorAll('#xncf-modules-area .box-card');
      const upgradeableVersions = document.querySelectorAll('#xncf-modules-area .version-upgradeable');

      // 为所有可更新的模块添加发光效果
      upgradeableVersions.forEach(version => {
        const card = version.closest('.box-card');
        if (card) {
          // 添加随机延迟
          const delay = Math.random() * 200;
          setTimeout(() => {
            card.classList.add('glow-animation');
            setTimeout(() => {
              card.classList.remove('glow-animation');
            }, 1200); // 与动画持续时间匹配
          }, delay);
        }
      });

      // 为不可更新的模块添加淡化效果
      allCards.forEach(card => {
        if (!card.querySelector('.version-upgradeable')) {
          // 添加随机延迟
          const delay = Math.random() * 200;
          setTimeout(() => {
            card.classList.add('fade-animation');
            setTimeout(() => {
              card.classList.remove('fade-animation');
            }, 1200); // 与动画持续时间匹配
          }, delay);
        }
      });
    }
  }
});  
