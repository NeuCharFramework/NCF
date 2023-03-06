<template>
  <!-- 我要反馈 -->
  <div class="feedback">
    <div class="feedTable" @click="changeClick">
      <div class="feedcen">
        <i class="el-icon-message"></i>
        <span>我要反馈</span>
      </div>
    </div>

    <div class="main" v-if="clickState">
      <el-card>
        <div class="feedbackin">
          <span class="title">用户反馈</span>
          <el-divider></el-divider>
          <div class="feedItemO">
            <h5>分类:</h5>
            <el-select v-model="feedbackValue" placeholder="请选择">
              <el-option
                v-for="item in feedback"
                :key="item.value"
                :label="item.value"
                :value="item.value"
              >
              </el-option>
            </el-select>
          </div>
          <div class="feedItemT">
            <h5>反馈内容:</h5>
            <el-input
              type="textarea"
              :rows="5"
              placeholder="请输入反馈内容"
              v-model="feedtextarea"
            >
            </el-input>
            <div class="icontext">
              <i class="el-icon-warning" style="color: #32ac8e"></i>
              <span>反馈内容无回复,更多问题解答请前往</span>
              <a href="https://weixin.senparc.com/QA?catalog=2">问题交流</a>
            </div>
          </div>
          <div class="feedItemO">
            <el-button type="success">提交</el-button>
            <el-button>取消</el-button>
          </div>
        </div>
      </el-card>
    </div>
  </div>
</template>

<script>
export default {
  name: "feedback",

  data() {
    return {
      // 反馈下拉选项
      feedback: [
        {
          value: "平台账号配置",
        },
        {
          value: "关键字回复",
        },
        {
          value: "关键字回复",
        },
        {
          value: "自定义菜单",
        },
        {
          value: "素材管理",
        },
        {
          value: "账号管理",
        },
        {
          value: "UI",
        },
        {
          value: "用户体验",
        },
        {
          value: "其他",
        },
      ],
      // 反馈下拉选项选中值
      feedbackValue: "",
      // 反馈内容
      feedtextarea: "",
      //   是否显示输入内容
      clickState: false,
    };
  },
  methods: {
    changeClick() {
      this.clickState = !this.clickState;
    },
  },
};
</script>

<style scoped lang="scss">
// 我要反馈
.feedback {
  width: 400px;
  height: 350px;
  font-size: 14px;

  ::v-deep .el-divider {
    margin: 10px 0;
  }
  ::v-deep .el-card {
    margin: 10px 0;
    padding: 20px;
  }
  ::v-deep .el-button {
    padding: 7px 12px;
  }

  .feedTable {
    width: 120px;
    height: 40px;
    font-size: 14px;
    color: #32ac8e;
    background-color: white;
    border: 1px solid #e6ebf5;
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
    cursor: pointer;
    position: fixed;
    bottom: 10px;
    right: 10px;

    .feedcen {
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;

      span {
        display: inline-block;
        margin-left: 5px;
      }
    }
  }
  .feedTable :hover {
    background-color: #32ac8e;
    color: white;
    transform: scale(1.1);
    // font-size: 15px;
    -webkit-transition: all 0.6s ease;
  }

  .main {
    position: fixed;
    bottom: 70px;
    right: 10px;
    .feedbackin {
      .feedItemO {
        display: flex;
        align-items: center;
        justify-content: flex-start;
        h5 {
          margin-right: 10px;
        }
      }
      .feedItemT {
        display: flex;
        flex-direction: column;
        justify-content: flex-start;
        h5 {
          margin: 10px 0;
        }
        .icontext {
          margin: 10px 0;
          a {
            color: #1b72e2;
            padding: 0 10px;
          }
        }
      }
    }
  }
}
</style>