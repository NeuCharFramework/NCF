<template>
  <!-- 已订阅内容 -->
  <div class="subscribed">
    <!-- 标题 -->
    <el-card>
      <nav class="nav">
        <h3 class="shoptit">我的应用-NeuChar 2022</h3>
      </nav>
    </el-card>
    <!-- 主体 -->
    <el-card>
      <main class="main">
        <div class="mainTit">
          <h3>我的应用-全部</h3>
        </div>
        <!-- <el-card> -->
        <el-table :data="tableData" style="width: 100%">
          <el-table-column prop="date" label="NellChar"> </el-table-column>
          <el-table-column label="应用">
            <el-table-column prop="name" label="图标">
              <template slot="">
                <img
                  style="width: 100px"
                  src="../../Senparc.Xncf.XncfStore/shopping/imgs/201905191646449997..jpg"
                  alt=""
                />
              </template>
            </el-table-column>
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
          </el-table-column>
        </el-table>
        <!-- </el-card> -->
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
  // 穿透修改按钮颜色和间距，card间距
  ::v-deep .el-card {
    margin: 10px;
  }
  .nav {
    // border-bottom: 1px solid #cfdbe2;
    display: flex;
    align-items: center;
    // justify-content: center;

    .shoptit {
      // font-size: 24px;
      background-color: #ffffff;
      color: #929292;
      font-weight: normal;
      // padding: 15px;
    }
    // .discover {
    //   color: #37bc9b;
    // }
  }
  .main {
    .mainTit {
      h3 {
        padding: 10px;
        font-size: 16px;
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