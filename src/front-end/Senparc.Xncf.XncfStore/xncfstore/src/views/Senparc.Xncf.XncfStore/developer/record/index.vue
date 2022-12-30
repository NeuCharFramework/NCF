<template>
  <!-- 订阅记录 -->
  <div class="app">
    <!-- 标题 -->
    <el-card>
      <nav class="nav">
        <h3 class="shoptit">运行监测</h3>
        <div class="discover">
          <span style="cursor: pointer; color: #5d9cec">开发者中心</span> /
          <span>订阅记录</span>
        </div>
      </nav>
    </el-card>
    <div class="main">
      <!-- 表单 -->
      <el-card>
        <div class="table">
          <div class="topSelect">
            <div class="selectItem">
              <h5>筛选条件：</h5>
              <span>全部应用</span>
            </div>
            <div class="selectItem">
              <h5>状态：</h5>
              <el-select v-model="stateValue" placeholder="请选择">
                <el-option
                  v-for="item in stateOptions"
                  :key="item.value"
                  :label="item.label"
                  :value="item.value"
                >
                </el-option>
              </el-select>
            </div>
            <div class="selectItem">
              <h5>关键词：</h5>
              <el-input
                placeholder="请输入内容"
                v-model="appIptValue"
                class="input-with-select"
              >
                <el-button slot="append" icon="el-icon-search"></el-button>
              </el-input>
            </div>
          </div>
          <el-table :data="tableData" border style="width: 100%">
            <el-table-column prop="application" label="应用"> </el-table-column>
            <el-table-column
              prop="pelonging"
              label="所属NeuChar Cell账号"
            ></el-table-column>
            <el-table-column
              prop="extend"
              label="延长服务时间段"
            ></el-table-column>
            <el-table-column prop="state" label="状态"></el-table-column>
            <el-table-column prop="total" label="总金额"> </el-table-column>
            <el-table-column prop="opration" label="操作时间">
            </el-table-column>
            <el-table-column prop="comment" label="评论"></el-table-column>
          </el-table>
        </div>
      </el-card>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      // 状态选择
      stateOptions: [
        {
          value: "全部",
          label: "全部",
        },
        {
          value: "未开始",
          label: "未开始",
        },
        {
          value: "执行中",
          label: "执行中",
        },
        {
          value: "已结束",
          label: "已结束",
        },
      ],
      // 状态选择值
      stateValue: "",
      // ipt值
      appIptValue: "",
      // 表单数据
      tableData: [
        {
          application: "", //应用
          pelonging: "", //所属NeuChar Cell账号
          extend: "", //延长服务时间段
          state: "", //状态
          total: "", //总金额
          opration: "", //操作时间
          comment: "", //评论
        },
      ],
    };
  },
};
</script>

<style scoped lang="scss">
.app {
  // 穿透修改按钮颜色和间距，card间距
  ::v-deep .el-card {
    margin: 10px;
  }
  // ::v-deep .el-button--danger {
  //   background-color: #7266ba;
  //   border: transparent;
  // }
  ::v-deep .el-button--success {
    background-color: #37bc9b;
    color: white;
    border: transparent;
  }

  .nav {
    display: flex;
    flex-direction: column;

    .shoptit {
      background-color: #ffffff;
      font-size: 24px;
      color: #929292;
      font-weight: normal;
    }
    .discover {
      margin: 10px 0;
      color: #929292;
    }
  }
  .main {
    padding: 10px 0;
    margin: 0 10px;

    // 表单部分
    .table {
      .topSelect {
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        margin-bottom: 20px;

        .selectItem {
          display: flex;
          align-items: center;
          margin: 0 5px;

          h5 {
            color: #656565;
            font-size: 14px;
            white-space: nowrap;
          }
          span {
            color: #37bc9b;
            font-size: 14px;
          }

          ::v-deep .el-select {
            width: 110px;
            font-size: 12px;
          }
          ::v-deep .el-input-group {
            width: 220px;
            font-size: 12px;
            margin-left: 10px;
          }
        }
      }
    }
  }
}
</style>