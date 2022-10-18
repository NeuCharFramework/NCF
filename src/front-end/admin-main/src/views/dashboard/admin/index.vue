<template>
  <div class="el-main">
    <el-container>
      <el-header class="module-header">
        <span class="start-title">
          <span class="module-header-v">NeuCharFramework 管理员后台</span>
        </span>
      </el-header>
      <el-main />
    </el-container>
    <!-- 图表 -->
    <el-row :gutter="10">
      <el-col :xs="20" :sm="20" :md="20" :lg="12" :xl="12">
        <el-card class="box-card">
          <div
            id="firstChart"
            ref="firstChart"
            style="height: 350px; width: 100%"
          />
        </el-card>
      </el-col>
      <el-col :xs="20" :sm="20" :md="20" :lg="12" :xl="12">
        <el-card class="box-card">
          <div
            id="secondChart"
            ref="secondChart"
            style="height: 350px; width: 100%"
          />
        </el-card>
      </el-col>
      <!-- 其他组件展示 -->
      <el-col :xs="20" :sm="20" :md="20" :lg="12" :xl="12">
        <component :is="'BoxCard'" />
      </el-col>
      <el-col :xs="20" :sm="20" :md="20" :lg="12" :xl="12">
        <el-card class="box-card">
          <component :is="'BarChart'" />
        </el-card>
      </el-col>
      <el-col :xs="20" :sm="20" :md="20" :lg="12" :xl="12">
        <el-card class="box-card">
          <component :is="'PanelGroup'" />
        </el-card>
      </el-col>
      <el-col :xs="20" :sm="20" :md="20" :lg="12" :xl="12">
        <el-card class="box-card">
          <component :is="'TodoList'" />
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script>
import echarts from 'echarts'
import BarChart from './components/BarChart.vue'
import BoxCard from './components/BoxCard.vue'
import PanelGroup from './components/PanelGroup.vue'
import TodoList from './components/TodoList/index.vue'
export default {
  name: 'DashboardAdmin',
  components: { BarChart, BoxCard, PanelGroup, TodoList },
  data() {
    return {
      chart: null,
      xncfStat: {},
      xncfOpeningList: {}
    }
  },
  created() {},
  mounted() {
    this.initChart()
  },
  methods: {
    initChart() {
      const chart1 = document.getElementById("firstChart");
      const chartOption1 = {
        title: {
          text: "数量统计",
          subtext: "2019年11月1日 - 2019年11月5日",
        },
        xAxis: {
          type: "category",
          data: [
            "商品",
            "数据",
            "订单",
            "消息",
            "异常",
            "审批",
            "退单",
            "新订单",
          ],
        },
        yAxis: {
          type: "value",
          axisLabel: {
            formatter: "{value} 件",
          },
        },
        tooltip: {},
        series: [
          {
            data: [5, 20, 36, 10, 15, 40, 33, 17],
            type: "bar",
            color: "#91c7ae",
          },
        ],
      };

      const chartInstance1 = echarts.init(chart1);
      chartInstance1.setOption(chartOption1);

      const chart2 = document.getElementById("secondChart");
      const chartOption2 = {
        title: {
          text: "数量统计",
          subtext: "2019年度",
        },
        legend: {
          data: ["自由商品销售额"],
        },
        xAxis: {
          type: "category",
          data: [
            "一月",
            "二月",
            "三月",
            "四月",
            "五月",
            "六月",
            "七月",
            "八月",
            "九月",
            "十月",
            "十一月",
            "十二月",
          ],
        },
        yAxis: {
          type: "value",
          axisLabel: {
            formatter: "{value} 元",
          },
        },
        tooltip: {
          formatter: function (data, ticket, cllback) {
            // debugger
            // eslint-disable-next-line no-undef
            return data.seriesName + ":" + formatCurrency(data.value) + "元";
          },
        },
        series: [
          {
            data: [
              2666, 2778, 4926, 5767, 6810, 5670, 4123, 5687, 3654, 4999, 5301,
              7358,
            ],
            name: "自由商品销售额",
            type: "line",
          },
        ],
      };
      const chartInstance2 = echarts.init(chart2);
      chartInstance2.setOption(chartOption2);
    }
  }
}
</script>

<style lang="scss" scoped>
.box-card {
  margin-top: 20px;
}
</style>
