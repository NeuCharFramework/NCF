<template>
  <!-- 关键字回复 -->
  <div class="keyword">
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
        <el-input v-model="input" placeholder="请输入内容"></el-input>
        <el-button type="success" @click="addReply">新增回复</el-button>
      </main>
      <el-card v-if="replyInfo">
        <div class="replyArea">
          <el-form ref="form" :model="form" label-width="80px">
            <el-form-item label="规则名称">
              <el-input v-model="form.name"></el-input>
            </el-form-item>
            <el-form-item label="接收消息">
              <el-select v-model="form.region" placeholder="">
                <el-option label="文字" value="text"></el-option>
                <el-option label="图片" value="img"></el-option>
              </el-select>
            </el-form-item>
            <el-form-item label="接收消息">
              <el-select v-model="form.reply" placeholder="">
                <el-option label="图文消息文字" value="imgText"></el-option>
                <el-option label="文字" value="text"></el-option>
                <el-option label="图片" value="img"></el-option>
                <el-option label="语言" value="img" disabled></el-option>
                <el-option label="视频" value="img" disabled></el-option>
              </el-select>
            </el-form-item>
          </el-form>
          <el-button type="success">保存</el-button>
          <el-button>取消</el-button>
        </div>
      </el-card>
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
      input: "",
      //表单
      form: {
        name: "", //规则名称
        region: "", //接收消息
        reply: "", //回复内容
      },
      //是否显示回复
      replyInfo: false,
    };
  },
  methods: {
    // 新增回复
    addReply() {
      this.replyInfo = true;
    },
  },
};
</script>

<style scoped lang="scss">
.keyword {
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
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 10px 0;

    ::v-deep .el-input__inner {
      width: 200px;
    }
    ::v-deep .el-button {
      padding: 7px 12px;
    }
  }
  .replyArea {
    margin-top: 10px;
  }
}
</style>