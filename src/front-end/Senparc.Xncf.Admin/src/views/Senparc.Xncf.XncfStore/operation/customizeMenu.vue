<template>
  <!-- 自定义菜单 -->
  <div class="customize">
    <el-card>
      <div class="title">
        <div class="leftCard">
          <h3>自定义菜单</h3>
          <div class="use">
            <i class="el-icon-edit"></i>
            <span>使用指南</span>
          </div>
        </div>
        <div class="rightCard">
          <el-button type="warning">拉取菜单</el-button>
          <el-button type="primary">推送菜单</el-button>
        </div>
      </div>
    </el-card>
    <el-card>
      <main class="main">
        <img src="../shopping/imgs/201905191646449997..jpg" alt="" />
      </main>
    </el-card>
  </div>
</template>

<script>
export default {};
</script>
<style scoped lang="scss">
.customize {
  ::v-deep .el-card {
    margin: 10px;
  }
  .title {
    display: flex;
    align-items: center;
    justify-content: space-between;

    .leftCard {
      display: flex;
      align-items: center;

      .use {
        color: #37bc9b;
        margin: 0 10px;
      }
    }

    .rightCard {
      display: flex;
      ::v-deep .el-button--warning {
        background-color: #7266ba;
        border: transparent;
      }
    }
  }
  .main {
    img {
      width: 100px;
    }
  }
}
</style>