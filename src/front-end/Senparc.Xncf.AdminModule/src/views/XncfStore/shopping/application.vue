<template>
  <!-- 我的应用 -->
  <div class="myapplication">
    <nav class="nav">
      <h3 class="shoptit">应用形式-微信API-SWAG文档</h3>
    </nav>
    <el-card>
      <div class="notice">
        <span>
          <i class="el-icon-lollipop"></i>
        </span>
        <span>轻</span>
        <p>推送素材</p>
        <span>一起用当前应用</span>
      </div>
    </el-card>
    <div class="detail">
      <el-card>
        <div class="appdetail">
          <div class="appdetail-left">
            <h4>应用</h4>
            <div class="logo">
              <img src="./imgs/企业微信截图_20221222111152.png" alt="" />
            </div>
          </div>
          <div class="appdetail-right">
            <el-descriptions
              class="margin-top"
              title="应用详情"
              :column="3"
              :size="size"
              border
            >
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-user"></i>
                  所属NeuChar
                </template>
                kooriookami
              </el-descriptions-item>
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-mobile-phone"></i>
                  名称
                </template>
                答题得分
              </el-descriptions-item>
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-location-outline"></i>
                  评分
                </template>
                3星
              </el-descriptions-item>
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-tickets"></i>
                  当前状态
                </template>
                已开放
                <!-- <el-tag size="small"></el-tag> -->
                <el-button style="margin-left: 5px" type="primary" plain
                  >切换</el-button
                >
              </el-descriptions-item>
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-office-building"></i>
                  高级设置
                </template>
                <el-button style="margin-left: 5px" type="primary" plain
                  >打开高级设置</el-button
                >
              </el-descriptions-item>
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-office-building"></i>
                  AppCode
                </template>
                <el-button style="margin-left: 5px" type="primary" plain
                  >获取AppCode</el-button
                >
              </el-descriptions-item>
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-tickets"></i>
                  进入会话关键字
                </template>
                enter
                <!-- <el-tag size="small"></el-tag> -->
                <!-- <el-button style="margin-left: 5px" type="primary" plain
                  >切换</el-button
                > -->
              </el-descriptions-item>
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-office-building"></i>
                  退出会话关键字
                </template>
                exit
              </el-descriptions-item>
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-office-building"></i>
                  会话自动停留时间
                </template>
                0分钟
              </el-descriptions-item>
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-office-building"></i>
                  关键词
                </template>
                [无]
              </el-descriptions-item>
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-office-building"></i>
                  到期时间
                </template>
                2023/1/23 9：25：57
              </el-descriptions-item>
            </el-descriptions>
          </div>
        </div>
      </el-card>
    </div>
    <!-- 订阅 -->
    <div class="bottom">
      <h3>订阅记录</h3>
      <el-table :data="tableData" style="width: 100%">
        <el-table-column prop="startTime" label="订阅时间"> </el-table-column>
        <el-table-column prop="endTime" label="到期时间"> </el-table-column>
        <el-table-column prop="price" label="价格"> </el-table-column>
        <el-table-column prop="score" label="评分"> </el-table-column>
      </el-table>
    </div>
    <!-- 添加评论 -->
    <div class="comment">
      <h4>评分：微信API Swagger 文档</h4>
      <div>
        <el-form
          :model="ruleForm"
          :rules="rules"
          ref="ruleForm"
          label-width="100px"
          class="demo-ruleForm"
        >
          <el-form-item label="评分" prop="resource">
            <el-radio-group v-model="ruleForm.resource">
              <el-radio label="非常好"></el-radio>
              <el-radio label="很好"></el-radio>
              <el-radio label="一般"></el-radio>
              <el-radio label="差"></el-radio>
              <el-radio label="很差"></el-radio>
            </el-radio-group>
          </el-form-item>
          <el-form-item label="评论" prop="desc">
            <el-input
              type="textarea"
              :rows="4"
              v-model="ruleForm.desc"
            ></el-input>
          </el-form-item>
          <span style="color: #ff902b; font-size: 15px"
            >故意恶评或者刷分将被永久封号</span
          >
          <div class="operation">
            <el-button type="primary" @click="submitForm('ruleForm')"
              >提交</el-button
            >
            <el-button @click="resetForm('ruleForm')">重置</el-button>
          </div>
        </el-form>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      tableData: [
        {
          startTime: "2016-05-02",
          endTime: "2016-05-02",
          price: "price",
          score: "score",
        },
      ],
      ruleForm: {
        desc: "",
        resource: "",
      },
      //   验证
      rules: {
        desc: [
          { required: true, message: "请输入活动名称", trigger: "blur" },
          { min: 3, max: 5, message: "长度在 3 到 5 个字符", trigger: "blur" },
        ],
        resource: [
          {
            required: true,
            message: "必选项不可为空",
            trigger: "change",
          },
        ],
      },
    };
  },
  methods: {
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
    resetForm(formName) {
      this.$refs[formName].resetFields();
    },
  },
};
</script>

<!-- <style lang="scss" scoped>
.myapplication {
  margin: 5px;
  width: 100%;
  height: 100%;
  background-color: #f5f7fa;
  padding: 20px;
  .nav {
    margin-bottom: 15px;
    .shoptit {
      font-size: 24px;
      background-color: #ffffff;
      border-bottom: 1px solid #cfdbe2;
      color: #929292;
      font-weight: normal;
      padding: 15px;
    }
  }
  .notice {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    padding: 15px 10px;
    font-size: 15px;

    span {
      color: #ff902b;
      padding: 0 5px;
    }
    p {
      color: #5d9cec;
    }
  }
  .detail {
    h3 {
      line-height: 40px;
      padding: 0px 20px;
      margin-bottom: 5px;
      border-bottom: rgba(0, 0, 0, 0.1) solid 1px;
    }
    .appdetail {
      display: flex;
      align-items: flex-start;
      margin-top: 10px;
      .appdetail-left {
        width: 300px;
        height: 100%;
        padding: 10px;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: flex-start;
        .logo {
          img {
            width: 200px;
            margin-top: 20px;
            border: rgba(0, 0, 0, 0.1) solid 1px;
          }
        }
      }
      .appdetail-right {
        height: 100%;
        width: 100%;
        padding: 10px;
        .detailInfo {
          .infoItem {
            width: 250px;
            height: 100%;
            line-height: 50px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            span {
              color: #5d9cec;
            }
          }
        }
      }
    }
  }
  .bottom {
    margin-top: 15px;
    padding: 10px;
    background-color: #fff;
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
    h3 {
      padding: 10px;
    }
  }
  .comment {
    padding: 10px;
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
    background-color: white;
    margin-top: 10px;
    display: flex;
    flex-direction: column;
    h4 {
      padding: 10px;
    }
    .operation {
      margin: 10px;
    }
  }
}
</style> -->
<style scoped>
.myapplication >>> .el-card__body {
  padding: 0px;
  /* margin-bottom: 10px; */
}
.appdetail-right >>> .el-button--primary {
  padding: 5px;
}
</style>