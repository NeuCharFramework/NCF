<template>
  <!-- 素材管理 -->
  <div class="source">
    <el-card>
      <div class="title">
        <div class="leftCard">
          <h3>关键字回复</h3>
          <div class="use">
            <i class="el-icon-edit"></i>
            <span>使用指南</span>
          </div>
        </div>
        <div class="rightCard">
          <el-button type="warning">拉取</el-button>
          <el-button type="primary">推送</el-button>
        </div>
      </div>
    </el-card>
    <el-card>
      <main class="main">
        <el-tabs v-model="activeName" type="card" @tab-click="handleClick">
          <el-tab-pane label="文字" name="first">
            <el-card>
              <main class="mainin">
                <el-input v-model="input" placeholder="请输入内容"></el-input>
                <el-button type="success" @click="addReply">添加文字内容</el-button>
              </main>
              <el-card v-if="replyInfo">
                <div class="replyArea">
                  <el-form ref="form" :model="form" label-width="80px">
                    <el-form-item label="内容">
                      <el-input type="textarea" v-model="form.textarea"></el-input>
                    </el-form-item>
                  </el-form>
                  <el-button type="success">保存</el-button>
                  <el-button @click="replyInfo = false">取消</el-button>
                </div>
              </el-card>
            </el-card>
          </el-tab-pane>
          <el-tab-pane label="图文消息" name="second">
            <el-card>
              <div class="mainin">
                <span>图文消息(共0条)</span>
                <div class="option">
                  <el-input v-model="imgTextIpt" placeholder="标题"></el-input>
                  <el-button type="success" @click="goImgText">新建图文素材</el-button>
                  <el-button type="success" disabled icon="el-icon-arrow-down">
                  </el-button>
                </div>
              </div>
            </el-card>
          </el-tab-pane>
          <el-tab-pane label="图片" name="third">角色管理</el-tab-pane>
          <el-tab-pane label="语音" name="fourth" disabled>语音</el-tab-pane>
          <el-tab-pane label="视频" name="five" disabled>视频</el-tab-pane>
        </el-tabs>
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
  name: "sourceMaterial",
  components: {
    footerinfo,
    feedback,
  },
  data() {
    return {
      // tabs切换显示
      activeName: "second",
      input: "", //文字输入框
      imgTextIpt: "", //图文消息输入框
      replyInfo: false, //是否显示文字输入详情
      //表单
      form: {
        textarea: "",
      },
      //图文选中值
      twValue: null,
    };
  },
  methods: {
    // tabs切换显示
    handleClick(tab, event) {
      console.log(tab, event);
    },
    // 添加文字内容
    addReply() {
      this.replyInfo = true;
    },
    //新建图文素材
    goImgText() {
      // this.$router.push({ path: "/Senparc.Xncf.XncfStore/operation/imgText" });
      this.$router.push({ path: "/imgText" });
      console.log('路由biaoada', this.$router,);
    },
  },
};
</script>

<style scoped lang="scss">
.source {

  // 穿透修改按钮颜色和间距，card间距
  ::v-deep .el-card {
    margin: 10px;
  }

  ::v-deep .el-button--success {
    color: #fff;
    background-color: #37bc9b;
    border-color: transparent;
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
    .mainin {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 10px 0;

      ::v-deep .el-input__inner {
        width: 200px;
      }

      ::v-deep .el-button {
        padding: 10px;
      }

      .option {
        display: flex;
        align-items: center;
        justify-content: space-between;

        ::v-deep .el-button {
          margin: 0 5px;
          margin-right: 0;
        }
      }
    }

    .replyArea {
      margin-top: 10px;
    }
  }
}
</style>