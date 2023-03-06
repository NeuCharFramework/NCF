<template>
  <!-- 积分记录 -->
  <div class="app">
    <!-- 标题 -->
    <el-card>
      <nav class="nav">
        <h3 class="shoptit">积分记录</h3>
        <div class="discover">
          <span style="cursor: pointer; color: #5d9cec">开发者中心</span> /
          <span>积分记录</span>
        </div>
      </nav>
    </el-card>
    <div class="main">
      <!-- 表单 -->
      <el-card>
        <div class="table">
          <el-table :data="tableData" border style="width: 100%">
            <el-table-column prop="timer" label="时间"> </el-table-column>
            <el-table-column prop="integral" label="积分变动"></el-table-column>
            <el-table-column prop="explain" label="说明"></el-table-column>

            <el-table-column
              prop="subscriptions"
              label="评论"
            ></el-table-column>
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
      // 表单数据
      tableData: [
        {
          timer: "", //时间
          integral: "", //积分变动
          explain: "", //说明
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
    }
  }
}
</style>