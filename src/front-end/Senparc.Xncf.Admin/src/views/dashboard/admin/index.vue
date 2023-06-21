<template>
  <div class="el-main">
    <el-container>
      <el-header class="module-header">
        <span class="start-title"><span class="module-header-v">
            NeuCharFramework 管理员后台
          </span></span>
      </el-header>
      <el-main />
    </el-container>
    <el-row :gutter="20">
      <el-col :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
        <router-link to="/Admin/XncfModule/Index">
          <div class="grid-content xncf-stat-item">
            <span class="count">{{ xncfStat.installedXncfCount || 0 }}</span>
            <div class="icon">
              <i class="fa fa-caret-square-o-right" />
            </div>
            <p class="tit">已安装模块</p>
          </div>
        </router-link>
      </el-col>
      <el-col :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
        <router-link to="/Admin/XncfModule/Index">
          <div class="grid-content bg-purple xncf-stat-item">
            <span class="count">
              {{ xncfStat.updateVersionXncfCount || 0 }}
            </span>
            <div class="icon">
              <i class="fa fa-comments-o" />
            </div>
            <p class="tit">待更新模块</p>
          </div>
        </router-link>
      </el-col>
      <el-col :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
        <router-link to="/Admin/XncfModule/Index">
          <div class="grid-content bg-purple xncf-stat-item">
            <span class="count">{{ xncfStat.newXncfCount || 0 }}</span>
            <div class="icon">
              <i class="fa fa-sort-amount-desc" />
            </div>
            <p class="tit">发现新模块</p>
          </div>
        </router-link>
      </el-col>
      <el-col :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
        <router-link to="/Admin/XncfModule/Index">
          <div class="grid-content bg-purple xncf-stat-item">
            <span class="count">{{ xncfStat.missingXncfCount || 0 }}</span>
            <div class="icon">
              <i class="fa fa-check-square-o" />
            </div>
            <p class="tit">模块异常</p>
          </div>
        </router-link>
      </el-col>
    </el-row>
    <el-row :gutter="10">
      <el-col :xs="24" :sm="24" :md="24" :lg="12" :xl="12">
        <el-card class="box-card">
          <BarChart height="350px" />
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="24" :md="24" :lg="12" :xl="12">
        <el-card class="box-card">
          <LineChart height="350px" />
        </el-card>
      </el-col>
    </el-row>

    <!-- 功能模块 -->
    <el-row>
      <el-card class="box-card">
        <div slot="header" class="clearfix">
          <span style="font-size: 20px; color: black">功能模块</span>
        </div>
        <!-- 搜索 -->
        <el-input class="box-ipt" v-model="modelValue" placeholder="输入内容按下Enter" clearable @keyup.enter.native="putXncfOpening" @input="inputXncfOpening"></el-input>

        <!-- 所有的展示数据 -->
        <div id="xncf-modules-area">
          <el-row :gutter="20">
            <el-col v-for="item in xncfOpeningList" :key="item.uid" :span="6" class="xncf-item">
              <el-card class="box-card">
                <div slot="header" class="xncf-item-top svgimg greencolor">
                  <span class="moudelName">{{ item.menuName }}</span>
                  <small class="version">v{{ item.version }}</small>
                </div>
                <router-link :to="'/Admin/XncfModule/Start/uid=' + item.uid">
                  <i style="font-size: 22px" :class="[item.icon, 'icon']" />
                </router-link>
                <!-- <a
                  class="component-item"
                  :href="'/Admin/XncfModule/Start/?uid=' + item.uid"
                >
                  <i :class="[item.icon, 'icon']" />
                </a> -->
              </el-card>
            </el-col>
          </el-row>
        </div>
      </el-card>
    </el-row>
  </div>
</template>
<script>
// 图表
import BarChart from './components/BarChart.vue'
import LineChart from './components/LineChart.vue'
// 接口
import { getXncfStat, getModuleList } from '@/api/module'
import { isHaveToken } from '@/utils/auth'
export default {
  name: 'DashboardAdmin',
  components: { BarChart, LineChart },
  data() {
    return {
      chart: null,
      // XNCF 统计状态数据
      xncfStat: {},
      // 开放模块数据列表
      xncfOpeningList: [],
      // input模糊搜索使用
      modelValue: ''
    }
  },
  created() {
    this.getXncfStat()
    this.getXncfOpening()
    // this.getCharts();
    isHaveToken()
  },

  methods: {
    // XNCF 统计状态
    async getXncfStat() {
      const xncfStatData = await getXncfStat()
      this.xncfStat = xncfStatData.data || {}
    },
    // 开放模块数据
    async getXncfOpening() {
      const xncfOpeningList = await getModuleList()
      this.xncfOpeningList = xncfOpeningList.data || []
      // console.log(JSON.parse(JSON.stringify(xncfOpeningList)))
    },
    // 图表数据
    // async getCharts() {
    //   const xncfOpeningList = await getXncfOpening();
    //   this.xncfOpeningList = xncfOpeningList.data.data || [];
    // },
    // 搜索模块
    putXncfOpening() {
      // console.log("输入值", this.modelValue);
      if (this.modelValue) {
        let _value = this.modelValue.toUpperCase()
        let datas = this.xncfOpeningList.filter((item) => {
          let _UpperCase = item.menuName.toUpperCase()
          return _UpperCase.includes(_value)
        })
        this.xncfOpeningList = [...datas]
      } else {
        this.getXncfOpening()
      }
    },
    // 搜索框没值时展示全部数据
    inputXncfOpening() {
      if (!this.modelValue) {
        this.getXncfOpening()
      }
    }
  }
}
</script>
<style lang="scss" scoped>
  .xncf-stat-item {
    position: relative;
    display: block;
    margin-bottom: 12px;
    border: 1px solid #e4e4e4;
    overflow: hidden;
    padding: 5px;
    border-radius: 5px;
    background-clip: padding-box;
    background: #fff;
    transition: all 300ms ease-in-out;
    padding-left: 10px;
  }

  .xncf-stat-item .icon {
    position: absolute;
    right: 20px;
    top: 50%;
    transform: translateY(-50%);
    font-size: 60px;
    color: #bab8b8;
  }

  .xncf-stat-item .count {
    font-size: 38px;
    font-weight: bold;
    line-height: 1.65857;
  }

  .xncf-stat-item .tit {
    color: #bab8b8;
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
    margin-top: 20px;

    .clearfix {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    ::v-deep .el-input {
      width: 310px;
    }
    ::v-deep .el-input__inner {
      border: 1px solid rgba(0, 0, 0, 0.3);
    }

    // .svgimg {
    //   flex: 1;
    //   background: url(./img/join.svg);
    //   background-size: 100% 100%;
    //   background-repeat: no-repeat;
    // }
    // .svgimgt {
    //   flex: 1;
    //   background: url(./img/enlarge.svg);
    //   background-size: 100% 100%;
    //   background-repeat: no-repeat;
    // }
    .greencolor {
      // background-color: #00a971;
      // color: #fff;
      font-weight: bold;
      // padding: 10px;
    }
  }

  #xncf-modules-area {
    margin-bottom: 50px;
  }
  ::v-deep .el-card__header {
    padding: 16px;
  }
  ::v-deep .el-card__body {
    padding: 16px;
  }
  #xncf-modules-area .xncf-item .xncf-item-top {
    display: flex;
    flex-direction: row;
    flex-wrap: nowrap;
    gap: 10px;
  }
  #xncf-modules-area .xncf-item .xncf-item-top .moudelName {
    flex: 1;
    word-break: break-all;
  }
  #xncf-modules-area .xncf-item .xncf-item-top .version {
    white-space: nowrap;
  }

  #xncf-modules-area .xncf-item .icon {
    // float: left;
    position: relative;
  }
</style>

