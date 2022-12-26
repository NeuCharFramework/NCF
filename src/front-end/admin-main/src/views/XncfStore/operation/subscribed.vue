<template>
  <!-- 已订阅内容 -->
  <div class="subscribed">
    <!-- 标题 -->
    <nav class="nav">
      <h3 class="shoptit">我的应用</h3>
    </nav>
    <!-- 主体 -->
    <main class="main">
      <div class="mainTit">
        <el-card>
          <h3>我的应用-NeuChar 2022</h3>
        </el-card>
      </div>
      <el-card>
        <el-table :data="tableData" style="width: 100%">
          <el-table-column prop="date" label="NellChar"> </el-table-column>
          <el-table-column label="应用">
            <el-table-column prop="name" label="图标">
              <template slot="">
                <img
                  style="width: 100px"
                  src="../../XncfStore/shopping/imgs/201905191646449997..jpg"
                  alt=""
                />
              </template>
            </el-table-column>
            <!-- <el-table-column label="地址"> -->
            <el-table-column prop="province" label="名称"> </el-table-column>
            <el-table-column prop="city" label="到期时间"> </el-table-column>
            <el-table-column prop="address" label="状态"> </el-table-column>
            <el-table-column prop="zip" label="操作">
              <template>
                <div style="display: flex; align-items: center">
                  <el-button>查看详情</el-button>
                  <el-button type="primary">高级设置</el-button>
                </div>
              </template>
            </el-table-column>
            <!-- </el-table-column> -->
          </el-table-column>
        </el-table>
      </el-card>
    </main>
  </div>
</template>

<script>
export default {
  data() {
    return {
      tableData: [
        {
          date: "2016-05-03",
          name: "王小虎",
          province: "上海",
          city: "普陀区",
          address: "上海市普陀区金沙江路 1518 弄",
        },
      ],
    };
  },
};
</script>

<style scoped lang="scss">
.subscribed {
  .nav {
    border-bottom: 1px solid #cfdbe2;
    display: flex;
    align-items: center;
    justify-content: center;

    .shoptit {
      font-size: 24px;
      background-color: #ffffff;
      color: #929292;
      font-weight: normal;
      padding: 15px;
    }
    // .discover {
    //   color: #37bc9b;
    // }
  }
  .main {
    .mainTit {
      h3 {
        padding: 0px 10px;
      }
    }
  }
}
</style>
<style scoped>
.main >>> .el-card__body {
  margin: 15px;
}
</style>