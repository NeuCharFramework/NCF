<template>
  <!-- 运行监测 -->
  <div class="functions">
    <el-card>
      <nav class="nav">
        <h3 class="shoptit">运行监测</h3>
        <div class="discover">
          <i class="el-icon-discover"></i>
          <span>使用指南</span>
        </div>
      </nav>
    </el-card>
    <el-card>
      <main class="main">
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
          <el-button>查询</el-button>
        </div>
        <!-- 提示区域 -->
        <div class="tips">
          <span class="tipsTit">提示：</span>
          <div class="tipsArea">
            <p>1、您当前使用的NeuChar是【免费版】，可查看7天内的数据监测</p>
            <p>2、点击图例可现实或隐藏对应数据图标哦</p>
          </div>
        </div>
      </main>
    </el-card>
    <!-- 页脚组件 -->
    <footerinfo />
    <!-- 反馈组件 -->
    <feedback />
  </div>
</template>

<script>
import footerinfo from "@/components/FooterInfo";
import feedback from "@/components/Feedback";
export default {
  components: {
    footerinfo,
    feedback,
  },
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
};
</script>

<style scoped lang="scss">
.functions {
  // 穿透修改按钮颜色和间距，card间距
  ::v-deep .el-card {
    margin: 10px;
  }
  ::v-deep .el-select {
    width: 60px;
    margin-right: 10px;
  }
  .nav {
    display: flex;
    align-items: center;

    .shoptit {
      background-color: #ffffff;
      //   padding: 15px;
    }
    .discover {
      color: #37bc9b;
      margin: 0 10px;
    }
  }
  .main {
    .select {
      ::v-deep .el-button {
        color: #37bc9b;
        border: #37bc9b 1px solid;
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
}
</style>