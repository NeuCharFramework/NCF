<template>
  <div class="imgText">
    <el-card>
      <nav class="nav">
        <h3 class="shoptit">编辑图文消息</h3>
        <div class="discover">
          <span style="cursor: pointer; color: #5d9cec">素材管理</span> /
          <span>编辑图文消息</span>
        </div>
      </nav>
    </el-card>
    <el-card>
      <div class="main">
        <el-form
          :model="ruleForm"
          :rules="rules"
          ref="ruleForm"
          label-width="100px"
          class="demo-ruleForm"
        >
          <el-form-item label="" prop="mailboxTit">
            <el-input v-model="ruleForm.mailboxTit" placeholder="标题">
            </el-input>
          </el-form-item>
          <div class="textareaInfo"></div>
        </el-form>
        <div class="content">
          <div class="setup">
            <span class="setItem">H</span>
            <span class="setItem">H</span>
            <span class="setItem">H</span>
            <span class="setItem">H</span>
            <span class="setItem">H</span>
            <span class="setItem">H</span>
            <span class="setItem">H</span>
            <span class="setItem">H</span>
            <span class="setItem">H</span>
            <span class="setItem">H</span>
          </div>
          <div class="setText">
            <el-input
              type="textarea"
              :rows="12"
              placeholder="请输入内容"
              v-model="textarea"
            >
            </el-input>
          </div>
        </div>
      </div>
    </el-card>
    <el-card>
      <div class="release">
        <h4>发布样式编辑</h4>
        <div class="grayarea">
          <div class="reLeft">
            <span>封面</span>
            <el-input
              v-model="input"
              placeholder="请输入图片地址，以http(s)开头"
            >
            </el-input>
            <span>摘要</span>
            <el-input v-model="input" placeholder="请输入摘要"></el-input>
            <span>原地址</span>
            <el-input v-model="input" placeholder="请输入原地址，以http(s)开头">
            </el-input>
            <div class="graycolor">
              用户点击该图文链接将会跳转到原地址，不填则跳转到此编辑内容。
            </div>
          </div>
          <div class="reRigh">
            <span class="rt">预览</span>
            <div class="imgdetail">
              <div class="imgin"></div>
            </div>
            <div class="imgbtn">
              <el-button type="success" plain>多图文添加</el-button>
            </div>
          </div>
        </div>
      </div>
    </el-card>
    <div class="preserv">
      <el-button type="success" @click="submitForm('ruleForm')">保存</el-button>
    </div>
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
      ruleForm: {
        mailboxTit: "", //标题
      },
      rules: {
        mailboxTit: [
          { required: true, message: "请输入标题名称", trigger: "blur" },
          {
            min: 3,
            max: 5,
            message: "长度在 0 到 200 个字符",
            trigger: "blur",
          },
        ],
      },
      textarea: "",
      input: "",
    };
  },
  methods: {
    // 提交表单
    submitForm(formName) {
      this.$refs[formName].validate((valid) => {
        if (valid) {
          alert("submit!");
        } else {
          console.log("error submit!!");
          return false;
        }
      });
    },
    // 重置表单,暂未调用
    resetForm(formName) {
      this.$refs[formName].resetFields();
    },
  },
};
</script>

<style lang="scss" scoped>
.imgText {
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
    .content {
      padding: 0;
      border-radius: 4px;
      border: 1px solid #dcdfe6;
      color: #606266;
      width: 100%;
      height: 300px;
      .setup {
        display: flex;
        align-items: center;
        border-bottom: #ccc solid 1px;
        margin: 5px 10px;
        .setItem {
          display: inline-block;
          padding: 5px 10px;
        }
      }
      .setText {
        // 滚动条样式
        ::-webkit-scrollbar {
          width: 11px;
          height: 11px;
        }
        ::-webkit-scrollbar-thumb {
          border-radius: 10px;
          background-color: #37bc9b;
          border: 2px solid transparent;
          background-clip: padding-box;
        }

        overflow: hidden;
        overflow-y: auto;
        scrollbar-width: none; /* firefox */
        -ms-overflow-style: none; /* IE 10+ */
      }
    }
  }
  .release {
    h4 {
      font-size: 18px;
      font-weight: bold;
      color: #515253;
      padding: 20px 10px;
    }
    .grayarea {
      background-color: #e4eaec;
      color: #515253 !important;
      height: 100%;
      padding: 15px;
      display: flex;
      justify-content: flex-start;
      align-items: flex-start;
      .reLeft {
        font-size: 14px;
        flex: 1;
        span {
          display: inline-block;
          margin: 20px 10px;
        }
        .graycolor {
          margin: 10px;
        }
      }
      .reRigh {
        margin: 10px;
        font-size: 14px;
        width: 410px;
        height: 100%;
        border-radius: 4px;
        background-color: #fff;
        .rt {
          display: inline-block;
          margin: 15px;
        }
        .imgdetail {
          margin: 5px auto;
          width: 200px;
          border-radius: 4px;
          border: 1px solid #515253;
          padding: 10px 15px;
          display: flex;
          align-items: center;
          justify-content: center;
          .imgin {
            width: 100%;
            height: 108px;
            background-color: #515253;
          }
        }
        .imgbtn {
          background-color: #fafafa;
          padding: 10px 15px;
          display: flex;
          align-items: center;
          justify-content: center;
        }
      }
    }
  }
  .preserv {
    padding: 15px;
    background-color: #fafafa;
    border-top: 1px solid #eee;
    margin: 0 10px;
    border-radius: 4px;
    margin-top: -10px;
  }
}
</style>
<style scoped>
/* 卡片间距 */
.imgText >>> .el-card {
  margin: 10px;
}
/* 标题高度 */
.main >>> .el-input__inner {
  height: 45px;
  font-size: 17px;
}
/* 表单穿透 */
.main >>> .el-form-item__content {
  margin-left: 0 !important;
}
.setText >>> .el-textarea__inner {
  border: 0;
}
.reLeft >>> .el-input {
  margin: 0 10px;
  width: 98%;
}
</style>