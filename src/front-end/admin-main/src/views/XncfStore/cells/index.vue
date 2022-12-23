<template>
  <div class="cells">
    <nav class="nav">
      <h3 class="shoptit">平台账号配置</h3>
      <div class="discover">
        <i class="el-icon-discover"></i>
        <span>使用指南</span>
      </div>
    </nav>
    <!-- 提示 -->
    <div class="tip">
      <i class="el-icon-warning" style="color: #ff902b"></i>
      <span class="tips">
        提示：使用 NeuChar
        进行同步素材前，请先确保您的服务器上已经使用了最新版本的
        <a href="https://github.com/JeffreySu/WeiXinMPSDK"
          >Senparc.Weixin SDK</a
        >
        。
        如果对应公众号后台已经启用IP白名单，请添加本服务器的IP至白名单本机IP：
        <a href="https://github.com/JeffreySu/WeiXinMPSDK">211.149.130.74</a>
        。
      </span>
    </div>
    <!-- 主体 -->
    <main class="main">
      <div class="mainLeft">
        <el-card>
          <div class="item">
            <div>选择账号：{{ accountNum }}</div>
            <div v-for="(item, i) in this.account" :key="i" class="tr">
              <el-input v-model="item.acc" :disabled="modify"></el-input>

              <i class="el-icon-delete" style="margin: 0 10px; color: red"></i>
              <el-button type="success" @click="choice(item, i)"
                >选择</el-button
              >
            </div>
          </div>
          <el-button type="primary" @click="trueM" v-if="modify"
            >修改</el-button
          >
          <el-button type="primary" @click="falseM" v-else>确定</el-button>
          <el-button @click="addAccount">添加</el-button>
        </el-card>
      </div>
    </main>
  </div>
</template>

<script>
export default {
  data() {
    return {
      modify: true, //是否可修改
      accountNum: null, //选择账号
      //所有账号
      account: [
        {
          acc: "NeuChar Group222",
        },
      ],
    };
  },
  methods: {
    // 修改账号值
    trueM() {
      this.modify = false;
    },
    falseM() {
      this.modify = true;
      console.log(this.account);
    },
    // 添加账号
    addAccount() {
      if (this.account.length == 3) {
        this.$message.error("免费版最多支持三个Cell，请升级当前套餐在重试");
      } else {
        this.account.push({
          acc: "NeuChar Group333",
        });
      }
    },
    // 选择当前帐号
    choice(item, i) {
      console.log(item, i);
      this.accountNum = item.acc;
    },
  },
};
</script>

<style lang="scss" scoped>
.cells {
  padding: 10px;
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
    .discover {
      color: #37bc9b;
    }
  }
  .tip {
    white-space: nowrap;
    padding: 10px;
    .tips {
      color: #ff902b;

      a {
        color: #5d9cec;
        text-decoration: none;
      }
    }
  }
  .main {
    padding: 10px;

    // 穿透ipt隐藏边框
    ::v-deep.el-input__inner {
      color: rgb(0, 0, 0);
      border: none;
      outline: none;
      padding: 0;
      line-height: normal;
      background-color: transparent;
    }

    .mainLeft {
      width: 500px;
      display: flex;
      flex-direction: column;

      .item {
        .tr {
          display: flex;
          align-items: center;
          justify-content: center;
          padding: 10px;
        }
      }
    }
  }
}
</style>
<style scoped>
.tr >>> .el-button {
  padding: 5px 10px;
}
</style>