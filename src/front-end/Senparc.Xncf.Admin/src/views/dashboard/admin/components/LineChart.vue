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
      default: '350px'
    },
    autoResize: {
      type: Boolean,
      default: true
    }
    // chartData: {
    //   type: Object,
    //   required: true
    // }
  },
  data() {
    return {
      chart: null
    }
  },
  watch: {
    // chartData: {
    //   deep: true,
    //   handler(val) {
    //     this.setOptions(val)
    //   }
    // }
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
      this.setOptions()
    },
    setOptions({ expectedData, actualData } = {}) {
      this.chart.setOption({
        //  tooltip: {
        //   trigger: 'axis',
        //   axisPointer: {
        //     type: 'cross'
        //   },
        //   padding: [5, 10]
        // },
        // grid: {
        //   left: 10,
        //   right: 10,
        //   bottom: 20,
        //   top: 30,
        //   containLabel: true
        // },
        // legend: {
        //   data: ['expected', 'actual']
        // },
        tooltip: {
          formatter: function(data, ticket, cllback) {
            // debugger
            return data.seriesName + ':' + data.value + '元'
          }
        },
        title: {
          text: '数量统计',
          subtext: '2019年度'
        },
        legend: {
          data: ['自由商品销售额']
        },
        xAxis: {
          type: 'category',
          data: [
            '一月',
            '二月',
            '三月',
            '四月',
            '五月',
            '六月',
            '七月',
            '八月',
            '九月',
            '十月',
            '十一月',
            '十二月'
          ]
        },
        yAxis: {
          type: 'value',
          axisLabel: {
            formatter: '{value} 元'
          }
        },
        series: [
          {
            data: [
              2666, 2778, 4926, 5767, 6810, 5670, 4123, 5687, 3654, 4999, 5301,
              7358
            ],
            name: '自由商品销售额',
            type: 'line'
          }
        ]
      })
    }
  }
}
</script>
