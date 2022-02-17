<template>
  <div class="el-main">

    <el-container>
      <el-header class="module-header">
        <span class="start-title"><span class="module-header-v">NeuCharFramework 管理员后台</span></span>
      </el-header>
      <el-main />
    </el-container>
    <el-row :gutter="20">
      <el-col :span="6">
        <a href="~/Admin/XncfModule/Index">
          <div class="grid-content xncf-stat-item">
            <span class="count">{{ xncfStat.installedXncfCount || 0 }}</span>
            <div class="icon">
              <i class="fa fa-caret-square-o-right" />
            </div>
            <p class="tit">已安装模块</p>
          </div>
        </a>
      </el-col>
      <el-col :span="6">
        <a href="~/Admin/XncfModule/Index">
          <div class="grid-content bg-purple xncf-stat-item">
            <span class="count">{{ xncfStat.updateVersionXncfCount || 0 }}</span>
            <div class="icon">
              <i class="fa fa-comments-o" />
            </div>
            <p class="tit">待更新模块</p>
          </div>
        </a>
      </el-col>
      <el-col :span="6">
        <a href="~/Admin/XncfModule/Index">
          <div class="grid-content bg-purple xncf-stat-item">
            <span class="count">{{ xncfStat.newXncfCount || 0 }}</span>
            <div class="icon">
              <i class="fa fa-sort-amount-desc" />
            </div>
            <p class="tit">发现新模块</p>
          </div>
        </a>
      </el-col>
      <el-col :span="6">
        <a href="~/Admin/XncfModule/Index">
          <div class="grid-content bg-purple xncf-stat-item">
            <span class="count">{{ xncfStat.missingXncfCount || 0 }}</span>
            <div class="icon">
              <i class="fa fa-check-square-o" />
            </div>
            <p class="tit">模块异常</p>
          </div>
        </a>
      </el-col>
    </el-row>
    <el-row :gutter="10">
      <el-col :xs="20" :sm="20" :md="20" :lg="12" :xl="12">
        <el-card class="box-card">
          <div id="firstChart" style="height:350px;width: 100%" />
        </el-card>
      </el-col>
      <el-col :xs="20" :sm="20" :md="20" :lg="12" :xl="12">
        <el-card class="box-card">
          <div id="secondChart" style="height:350px;width: 100%" />
        </el-card>
      </el-col>
    </el-row>

    <!-- 功能模块 -->
    <el-row>
      <el-card class="box-card">
        <div slot="header" class="clearfix">
          <span>功能模块</span>
        </div>
        <div id="xncf-modules-area">
          <el-row :gutter="20">
            <el-col v-for="item in xncfOpeningList" v-key="item.Id" :span="6" class="xncf-item">
              <el-card class="box-card">
                <div slot="header" class="clearfix">
                  <span>{{ item.menuName }}</span> <small class="version">v{{ item.version }}</small>
                </div>
                <a class="component-item" :href="'/Admin/XncfModule/Start/?uid='+item.uid">
                  <span class=""><i :class="[item.icon,'icon']" /></span>
                  @*<a :href="'/Admin/XncfModule/Start/?uid='+item.uid">{{ item.menuName }}</a>*@
                </a>
              </el-card>
            </el-col>
          </el-row>
        </div>
      </el-card>
    </el-row>
  </div>
</template>

<script>
import echarts from 'echarts'
import { getXncfOpening, getXncfStat } from '@/api/home'
export default {
  name: 'DashboardAdmin',
  data() {
    return {
      chart: null,
      xncfStat: {},
      xncfOpeningList: {}
    }
  },
  created() {
    // this.getXncfStat()
    // this.getXncfOpening()
    // this.initChart();
  },
  methods: {
    initChart() {
      const chart1 = document.getElementById('firstChart')
      const chartOption1 = {
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
      }

      const chartInstance1 = echarts.init(chart1)
      chartInstance1.setOption(chartOption1)

      // let chart2 = document.getElementById('secondChart');
      // let chartOption2 = {
      //   title: {
      //     text: '数量统计',
      //     subtext: '2019年度'
      //   }, legend: {
      //     data: ['自由商品销售额']
      //   },
      //   xAxis: {
      //     type: 'category',
      //     data: ['一月', '二月', '三月', '四月', '五月', '六月', '七月', '八月', '九月', '十月', '十一月', '十二月']
      //   },
      //   yAxis: {
      //     type: 'value',
      //     axisLabel: {
      //       formatter: '{value} 元'
      //     }
      //   }, tooltip: {
      //     formatter: function (data, ticket, cllback) {
      //       //debugger
      //       return data.seriesName + ':' + formatCurrency(data.value) + '元';
      //     }
      //   },
      //   series: [{
      //     data: [2666, 2778, 4926, 5767, 6810, 5670, 4123, 5687, 3654, 4999, 5301, 7358],
      //     name: '自由商品销售额',
      //     type: 'line'
      //   }]
      // };
      //
      // let chartInstance2 = echarts.init(chart2);
      // chartInstance2.setOption(chartOption2);
    },
    // XNCF 统计状态
    async getXncfStat() {
      const xncfStatData = await getXncfStat()
      this.xncfStat = xncfStatData.data.data
    },
    // 开放模块数据
    async getXncfOpening() {
      const xncfOpeningList = await getXncfOpening()
      this.xncfOpeningList = xncfOpeningList.data.data
    }
  }
}
</script>

<style lang="scss" scoped>
.xncf-stat-item {
  position: relative;
  display: block;
  margin-bottom: 12px;
  border: 1px solid #E4E4E4;
  overflow: hidden;
  padding-bottom: 5px;
  border-radius: 5px;
  background-clip: padding-box;
  background: #FFF;
  transition: all 300ms ease-in-out;
  padding-left: 10px;
}

.xncf-stat-item .icon {
  font-size: 60px;
  color: #BAB8B8;
  position: absolute;
  right: 20px;
  top: -5px;
}

.xncf-stat-item .count {
  font-size: 38px;
  font-weight: bold;
  line-height: 1.65857;
}

.xncf-stat-item .tit {
  color: #BAB8B8;
  font-size: 24px;
}

.xncf-stat-item p {
  margin-bottom: 5px;
}

.chart-title {
  font-size: 20px;
}

.chart-li {
  padding-top: 20px;
}

.chart-li li {
  line-height: 2;
}

.chart-li .fa {
  margin-right: 5px;
}

.box-card {
  margin-top:20px
}

#xncf-modules-area{margin-bottom:50px;}
#xncf-modules-area .xncf-item{

}

#xncf-modules-area .xncf-item .version{ float:right;}
#xncf-modules-area .xncf-item .icon{ float:left;}
</style>
