<template>
  <div :class="className" :style="{height:height,width:width}" />
</template>

<script>
import echarts from 'echarts'
// require('echarts/theme/macarons') // echarts theme
import resize from './mixins/resize'

export default {
  mixins: [resize],
  props: {
    className: {
      type: String,
      default: 'chart'
    },
    width: {
      type: String,
      default: '100%'
    },
    height: {
      type: String,
      default: '300px'
    }
  },
  data() {
    return {
      chart: null
    }
  },
  mounted() {
    this.$nextTick(() => {
      this.initChart()
    })
  },
  beforeDestroy() {
    if (!this.chart) {
      return
    }
    this.chart.dispose()
    this.chart = null
  },
  methods: {
    initChart() {
      // , 'macarons'
      this.chart = echarts.init(this.$el)

      this.chart.setOption({
        title: {
          text: '数量统计',
          subtext: '2019年11月1日 - 2019年11月5日'
        },
        tooltip: {
          trigger: 'axis',
          axisPointer: {
            // 坐标轴指示器，坐标轴触发有效
            type: 'shadow' // 默认为直线，可选为：'line' | 'shadow'
          }
        },
        // grid: {
        //   top: 10,
        //   left: '2%',
        //   right: '2%',
        //   bottom: '3%',
        //   containLabel: true
        // },
        xAxis: {
          type: 'category',
          data: [
            '商品',
            '数据',
            '订单',
            '消息',
            '异常',
            '审批',
            '退单',
            '新订单'
          ]
        },
        yAxis: {
          type: 'value',
          axisLabel: {
            formatter: '{value} 件'
          }
        },
        series: [
          {
            data: [5, 20, 36, 10, 15, 40, 33, 17],
            type: 'bar',
            color: '#91c7ae'
          }
        ]
      })
    }
  }
}
</script>
