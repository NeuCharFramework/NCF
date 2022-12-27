<template>
  <!-- 开发者信息 -->
  <div class="develop">
    <el-card>
      <nav class="nav">
        <h3 class="shoptit">运行监测</h3>
        <div class="discover">
          <span style="cursor: pointer; color: #5d9cec">开发者中心</span> /
          <span>开发者 - Rodear</span>
        </div>
      </nav>
    </el-card>
    <el-card>
      <h4 class="infotit">基本信息</h4>
      <main class="mian">
        <el-divider />
        <div class="main-item">
          <span>开发者名称</span>
          <p>Rodear</p>
        </div>
        <el-divider />
        <div class="main-item">
          <span>手机号</span>
          <p>18530878273</p>
        </div>
        <el-divider />
        <div class="main-item">
          <span>地区</span>
          <p>江苏省 苏州市 吴中区</p>
        </div>
        <el-divider />
        <div class="main-item">
          <span>等级</span>
          <p>100</p>
        </div>
        <el-divider />
        <div class="main-item">
          <span>积分</span>
          <p>0.00</p>
        </div>
        <el-divider />
        <div class="main-item">
          <span>成为开发者时间</span>
          <p>2022-12-26</p>
        </div>
        <el-divider />
        <div class="main-item">
          <span>说明</span>
          <p>一个靓仔</p>
        </div>
        <el-divider />
        <div class="main-item">
          <span>认证类型</span>
          <p>个人认证</p>
        </div>
        <el-divider />
        <div class="main-item">
          <span>开发者类型</span>
          <p>已通过验证</p>
          <p>查看认证信息</p>
        </div>
        <el-divider />

        <h4 class="infotit">开发者接口凭证</h4>
        <el-divider />
        <div class="main-item">
          <span>AppKey</span>
          <p>a1bb0e3445c644a5959b986949c6af40</p>
        </div>
        <el-divider />
        <div class="main-item">
          <span>Secret</span>
          <p>327867****1010f5</p>
          <p>显示全部</p>
        </div>
      </main>
    </el-card>
    <footer>
      <span>© 2022 -</span>
      <a href="http://www.senparc.com/">苏州盛派网络科技有限公司</a>
    </footer>
  </div>
</template>
<script>
export default {};
</script>

<style scoped lang="scss">
.develop {
  // 穿透修改按钮颜色和间距，card间距
  ::v-deep .el-card {
    margin: 10px;
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
  .infotit {
    padding: 20px 0;
  }
  .mian {
    ::v-deep .el-divider--horizontal {
      margin: 14px 0;
      background-color: #eee;
    }

    .main-item {
      display: flex;
      color: #656565;
      font-size: 14px;

      span {
        font-weight: bold;
        display: inline-block;
        width: 280px;
      }
      p {
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