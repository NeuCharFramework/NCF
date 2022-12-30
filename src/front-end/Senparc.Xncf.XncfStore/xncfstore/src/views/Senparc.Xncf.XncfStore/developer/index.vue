<template>
  <!-- 开发者中心 -->
  <div class="developer">
    <el-card>
      <nav class="nav">
        <h3 class="shoptit">开发者中心</h3>
      </nav>
    </el-card>
    <div class="main">
      <!-- 个人信息 -->
      <div class="infoArea">
        <div class="infoL">
          <div class="infoL-t">
            <i class="el-icon-lollipop"></i>
            <h4>Hi,Rodear!</h4>
          </div>
          <span class="infoL-b"> 快去创建自己的应用吧！加油哦！ </span>
        </div>
        <div class="infoR">
          <div class="infor-item">
            <h3>0.00</h3>
            <span>积分</span>
          </div>
          <div class="infor-item">
            <h3>1</h3>
            <span>应用总数</span>
          </div>
          <div class="infor-item">
            <h3>0</h3>
            <span>当月订阅(2022-12)</span>
          </div>
          <div class="infor-item">
            <h3>￥0.00</h3>
            <span>当月收入 (2022-12)</span>
          </div>
        </div>
      </div>
      <!-- 标题 -->
      <div class="titles">
        <h2>App 运行监测数据</h2>
      </div>
      <!-- app 运行监测数据 -->
      <el-card>
        <main class="mainin">
          <div class="select">
            <span> 筛选: </span>
            <el-date-picker
              v-model="timerValue"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
            >
            </el-date-picker>
            <span> 按 </span>
            <el-select v-model="value" placeholder="">
              <el-option
                v-for="item in options"
                :key="item.value"
                :label="item.label"
                :value="item.value"
              >
              </el-option>
            </el-select>
            <el-button @click="queryData">查询</el-button>
          </div>
          <!-- 提示区域 -->
          <div class="tips">
            <span class="tipsTit">提示：</span>
            <div class="tipsArea">
              <p>点击图例可显示或隐藏对应数据图表哦</p>
            </div>
          </div>
        </main>
      </el-card>
      <!-- 标题 -->
      <div class="titles">
        <h2>App用户、订阅数据统计</h2>
      </div>
      <!-- 图标部分 -->
      <div class="mainEchar">
        <!-- xs<768px  sm≥768px   md≥992px  lg≥1200px   xl≥1920px-->
        <el-row :gutter="10">
          <el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12">
            <el-card>
              <div class="echaritem" id="echarOne"></div>
            </el-card>
          </el-col>
          <el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12">
            <el-card>
              <div class="echaritem" id="echarTwo"></div>
            </el-card>
          </el-col>
          <el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12">
            <el-card>
              <div class="echaritem" id="echarOne"></div>
            </el-card>
          </el-col>
        </el-row>
      </div>
    </div>
    <footer>
      <span>© 2022 -</span>
      <a href="http://www.senparc.com/">苏州盛派网络科技有限公司</a>
    </footer>
  </div>
</template>

<script>
export default {
  data() {
    return {
      // 下拉选择日或月查询
      options: [
        {
          value: "日",
          label: "日",
        },
        {
          value: "月",
          label: "月",
        },
      ],
      value: "", //按日月查询值
      timerValue: "", //选择日期值
    };
  },
  mounted() {
    this.getLine();
    this.getColumn();
  },
  methods: {
    // 折现图
    getLine() {
      var echarts = require("echarts");

      var chartDom = document.getElementById("echarOne");
      var myChart = echarts.init(chartDom);
      var option;

      const symbolSize = 20;
      const data = [
        [15, 0],
        [-50, 10],
        [-56.5, 20],
        [-46.5, 30],
        [-22.1, 40],
      ];
      option = {
        title: {
          text: "Click to Add Points",
        },
        tooltip: {
          formatter: function (params) {
            var data = params.data || [0, 0];
            return data[0].toFixed(2) + ", " + data[1].toFixed(2);
          },
        },
        grid: {
          left: "3%",
          right: "4%",
          bottom: "3%",
          containLabel: true,
        },
        xAxis: {
          min: -60,
          max: 20,
          type: "value",
          axisLine: { onZero: false },
        },
        yAxis: {
          min: 0,
          max: 40,
          type: "value",
          axisLine: { onZero: false },
        },
        series: [
          {
            id: "a",
            type: "line",
            smooth: true,
            symbolSize: symbolSize,
            data: data,
          },
        ],
      };
      var zr = myChart.getZr();
      zr.on("click", function (params) {
        var pointInPixel = [params.offsetX, params.offsetY];
        var pointInGrid = myChart.convertFromPixel("grid", pointInPixel);
        if (myChart.containPixel("grid", pointInPixel)) {
          data.push(pointInGrid);
          myChart.setOption({
            series: [
              {
                id: "a",
                data: data,
              },
            ],
          });
        }
      });
      zr.on("mousemove", function (params) {
        var pointInPixel = [params.offsetX, params.offsetY];
        zr.setCursorStyle(
          myChart.containPixel("grid", pointInPixel) ? "copy" : "default"
        );
      });

      option && myChart.setOption(option);
    },
    // 柱状图
    getColumn() {
      var echarts = require("echarts");
      var chartDom = document.getElementById("echarTwo");
      var myChart = echarts.init(chartDom);
      var option;

      option = {
        xAxis: {
          type: "category",
          data: ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
        },
        yAxis: {
          type: "value",
        },
        series: [
          {
            data: [120, 200, 150, 80, 70, 110, 130],
            type: "bar",
          },
        ],
      };

      option && myChart.setOption(option);
    },
    // 查询按钮
    queryData() {
      console.log("日/月:", this.value);
      console.log("时间区域:", this.timerValue);
    },
  },
};
</script>

<style scoped lang="scss">
.developer {
  background-color: #f5f7fa;
  // 穿透修改按钮颜色和间距，card间距
  ::v-deep .el-card {
    margin: 10px;
  }
  .nav {
    display: flex;
    align-items: center;

    .shoptit {
      background-color: #ffffff;
      font-size: 24px;
      color: #929292;
      font-weight: normal;
    }
  }
  .main {
    padding: 10px 0;
    margin: 0 10px;
    // 个人信息部分
    .infoArea {
      background-image: url(./img/bg.jpg);
      background-size: cover;
      width: 100%;
      height: 90px;
      color: white;
      display: flex;
      justify-content: space-between;
      align-items: center;

      .infoL {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        justify-content: center;
        margin: 0px 15px;

        .infoL-t {
          display: flex;
          align-items: center;
          font-size: 18px;
          font-weight: bold;
          margin-bottom: 10px;
          h4 {
            display: inline-block;
            margin: 0 5px;
          }
        }
        .infoL-b {
          font-size: 14px;
        }
      }
      .infoR {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        .infor-item {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          margin: 0 10px;

          h3 {
            font-size: 24px;
          }
        }
      }
    }
    // title
    .titles {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 10px;
      color: #656565;
      margin: 15px 0;
      h2 {
        font-size: 18px !important;
      }
    }
    // app运行监测数据
    .mainin {
      .select {
        ::v-deep .el-button {
          color: #37bc9b;
          border: #37bc9b 1px solid;
        }
        ::v-deep .el-select {
          width: 60px;
          margin-right: 10px;
        }

        span {
          font-weight: bold;
          margin: 0 10px;
          font-family: "Source Sans Pro", sans-serif;
          color: #656565;
          font-size: 14px;
        }
      }
      .tips {
        padding: 10px 5px;
        margin-top: 20px;
        .tipsTit {
          color: #37bc9b;
          padding: 0px 5px;
          display: inline-block;
          margin: 10px 0;
          font-weight: bold;
          font-size: 14px;
        }
        .tipsArea {
          display: flex;
          flex-direction: column;
          p {
            padding: 5px;
          }
        }
      }
    }
    // 图标部分
    .mainEchar {
      .echaritem {
        width: 100%;
        height: 500px;
        margin: 10px 0;
        // background-color: gray;
        // border: 1px solid gray;
      }
    }
  }
  footer {
    display: flex;
    align-items: center;
    font-family: "Source Sans Pro", sans-serif;
    font-size: 14px;
    padding: 40px 20px;
    border-top: 1px solid #e4eaec;

    span {
      color: #656565;
    }
  }
}
</style>