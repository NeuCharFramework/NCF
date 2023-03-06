<template>
  <div class="tab">
    <el-card>
      <nav class="nav">
        <h3 class="shoptit">运行监测</h3>
        <div class="discover">
          <span style="cursor: pointer; color: #5d9cec">开发者中心</span> /
          <span>开发者 - Rodear</span>
        </div>
      </nav>
    </el-card>
    <main class="main">
      <!-- tab页切换 -->
      <el-card>
        <div class="maintit">
          <div class="item bluecard">
            <h4>1.</h4>
            <span>创建应用</span>
          </div>
          <div class="item">
            <h4>2.</h4>
            <span>开发接入</span>
          </div>
          <div class="item">
            <h4>3.</h4>
            <span>接入审核</span>
          </div>
          <div class="item">
            <h4>4.</h4>
            <span>上架管理</span>
          </div>
        </div>
      </el-card>
      <el-card>
        <!--主体1创建应用 -->
        <div class="maininfo">
          <div class="mainitem">
            <el-form
              :model="ruleForm"
              :rules="rules"
              ref="ruleForm"
              label-width="100px"
              class="demo-ruleForm"
            >
              <el-form-item label="应用名称" prop="name">
                <el-input v-model="ruleForm.name"></el-input>
              </el-form-item>
              <el-form-item label="应用类型">
                <el-select
                  v-model="ruleForm.region"
                  placeholder="请选择应用类型"
                >
                  <el-option label="基础服务" value="shanghai"></el-option>
                  <el-option label="互动推广" value="beijing"></el-option>
                  <el-option label="社交应用" value="beijing"></el-option>
                  <el-option label="游戏娱乐" value="beijing"></el-option>
                  <el-option label="电子商务" value="beijing"></el-option>
                  <el-option label="应用工具" value="beijing"></el-option>
                  <el-option label="办公协同" value="beijing"></el-option>
                  <el-option label="企业管理" value="beijing"></el-option>
                  <el-option label="商务服务" value="beijing"></el-option>
                  <el-option label="投资理财" value="beijing"></el-option>
                  <el-option label="行业应用" value="beijing"></el-option>
                  <el-option label="其他" value="beijing"></el-option>
                </el-select>
              </el-form-item>
              <el-form-item label="适用账号">
                <el-select v-model="ruleForm.synum" placeholder="适用账号">
                  <el-option label="全部" value="shanghai"></el-option>
                  <el-option label="订阅号" value="beijing"></el-option>
                  <el-option label="服务号" value="beijing"></el-option>
                </el-select>
              </el-form-item>
              <el-form-item label="验证">
                <el-radio-group v-model="ruleForm.resource">
                  <el-radio label="公众号必须通过验证"></el-radio>
                </el-radio-group>
              </el-form-item>
              <el-form-item label="私有">
                <el-radio-group v-model="ruleForm.sy">
                  <el-radio label="私有项目只有当前用户可以订阅"></el-radio>
                </el-radio-group>
              </el-form-item>
              <el-form-item label="Logo图表">
                <el-upload
                  class="upload-demo"
                  action="https://jsonplaceholder.typicode.com/posts/"
                  :on-change="handleChange"
                  :file-list="fileList"
                >
                  <el-button size="small" type="primary">点击上传</el-button>
                  <div slot="tip" class="el-upload__tip">
                    只能上传jpg/png文件，且不超过500kb
                  </div>
                </el-upload>
              </el-form-item>
              <el-form-item label="应用图片（轮播图）">
                <el-upload
                  class="upload-demo"
                  action="https://jsonplaceholder.typicode.com/posts/"
                  :on-preview="handlePreview"
                  :on-remove="handleRemove"
                  :file-list="fileList"
                  list-type="picture"
                >
                  <el-button size="small" type="primary">点击上传</el-button>
                  <div slot="tip" class="el-upload__tip">
                    只能上传jpg/png文件，且不超过500kb
                  </div>
                </el-upload>
              </el-form-item>
              <el-form-item label="版本号">
                <el-input v-model="ruleForm.edition"></el-input>
              </el-form-item>
              <el-form-item label="原价">
                <el-input v-model="ruleForm.Yprice"></el-input>
              </el-form-item>
              <el-form-item label="现价">
                <el-input v-model="ruleForm.Xprice"></el-input>
              </el-form-item>
              <el-form-item label="内容提要" prop="name">
                <el-input type="textarea" v-model="ruleForm.desc"></el-input>
              </el-form-item>
              <el-form-item label="版本新功能">
                <el-input
                  type="textarea"
                  v-model="ruleForm.delivery"
                ></el-input>
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="submitForm('ruleForm')">
                  创建应用
                </el-button>
                <el-button @click="resetForm('ruleForm')">重置</el-button>
                <el-button>取消</el-button>
              </el-form-item>
            </el-form>
          </div>
        </div>
        <!-- 主体2开发接入 -->
        <div class="access">
          <div class="projecttit">
            <h3>项目新增部分</h3>
            <span>(总项目名称)</span>
          </div>
          <el-divider></el-divider>
          <div class="projectmain">
            <h3>应用对接状态</h3>
            <el-table stripe :data="tableData" border style="width: 100%">
              <el-table-column prop="approvalStatus" label="" width="80">
              </el-table-column>
              <el-table-column prop="state" label="状态" width="120">
              </el-table-column>
              <el-table-column prop="explain" label="说明"> </el-table-column>
              <el-table-column prop="operation" label="操作">
                <template>
                  <div>
                    请先完成
                    <a href="">消息接口对接</a>
                    设置
                  </div>
                </template>
              </el-table-column>
            </el-table>
            <h3>App 关联配置</h3>
            <div class="projectdouble">
              <div class="doL">当前版本：0.1.0</div>
              <el-divider direction="vertical"></el-divider>
              <div class="doR">设置</div>
            </div>
            <h3>消息接口对接</h3>
            <div class="projectdouble">
              <div class="doL">当前状态： 未设置</div>
              <el-divider direction="vertical"></el-divider>
              <div class="doR">设置</div>
            </div>
            <h3>oAuth认证</h3>
            <div class="projectdouble">
              <div class="doL">当前状态： 未设置 已关闭</div>
              <el-divider direction="vertical"></el-divider>
              <div class="doR">设置</div>
            </div>
            <h3>应用状态变更记录</h3>
            <el-table border :data="changeTableData" stripe style="width: 100%">
              <el-table-column prop="before" label="变更前状态">
              </el-table-column>
              <el-table-column prop="after" label="变更后状态">
              </el-table-column>
              <el-table-column prop="explain" label="说明"> </el-table-column>
              <el-table-column prop="operation" label="操作时间">
              </el-table-column>
            </el-table>
          </div>
        </div>
        <!-- 主体3接入审核 -->
        <div class="approval"></div>
        <!-- 主体n消息接口对接 -->
        <div class="news"></div>
      </el-card>
    </main>
    <FooterInfo />
  </div>
</template>

<script>
import FooterInfo from "@/components/FooterInfo/index.vue";
export default {
  components: {
    FooterInfo,
  },
  data() {
    return {
      // 主体1创建应用
      // 表单数据
      ruleForm: {
        name: "", //应用名称
        region: "", //应用类型
        synum: "", //适用账号
        resource: "", //验证
        sy: "", //私有
        edition: "", //版本号
        Yprice: "", //原价
        Xprice: "", //现价
        name: "", //内容提要
        delivery: "", //版本新功能
      },
      //   验证
      rules: {
        name: [{ required: true, message: "必填项", trigger: "blur" }],
      },
      //上传文件/图片
      fileList: [
        {
          name: "food.jpeg",
          url: "https://fuss10.elemecdn.com/3/63/4e7f3a15429bfda99bce42a18cdd1jpeg.jpeg?imageMogr2/thumbnail/360x360/format/webp/quality/100",
        },
        {
          name: "food2.jpeg",
          url: "https://fuss10.elemecdn.com/3/63/4e7f3a15429bfda99bce42a18cdd1jpeg.jpeg?imageMogr2/thumbnail/360x360/format/webp/quality/100",
        },
      ],
      //   主体2开发接入
      //   应用对接状态图表数据
      tableData: [
        {
          approvalStatus: "", //审批状态
          state: "未接入", //状态
          explain: "说明文字", //说明
          operation: "", //操作
        },
        {
          approvalStatus: "", //审批状态
          state: "未接入", //状态
          explain: "说明文字", //说明
          operation: "", //操作
        },
        {
          approvalStatus: "", //审批状态
          state: "未接入", //状态
          explain: "说明文字", //说明
          operation: "", //操作
        },
      ],
      //应用状态变更记录图表
      changeTableData: [
        {
          before: "2",
          after: "5",
          explain: "上",
          operation: "",
        },
      ],
    };
  },
  methods: {
    // 主体1创建应用
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
    // 清空表单
    resetForm(formName) {
      this.$refs[formName].resetFields();
    },
    // 上传文件
    handleChange(file, fileList) {
      this.fileList = fileList.slice(-3);
    },
    // 上传轮播图
    handleRemove(file, fileList) {
      console.log(file, fileList);
    },
    handlePreview(file) {
      console.log(file);
    },
    // 主体2开发接入
  },
};
</script>

<style lang="scss" scoped>
.tab {
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
  .main {
    width: 100%;
    height: 100%;

    .maintit {
      display: flex;
      align-items: center;
      justify-content: center;

      .item {
        display: flex;
        align-items: center;
        justify-content: flex-start;
        padding: 15px 20px;
        width: 25%;
        cursor: pointer;

        h4 {
          font-size: 22px;
        }
        span {
          font-size: 15px;
        }
      }
      .bluecard {
        background: #5d9cec;
        color: #fff;
      }
    }
    // 主体2开发接入
    .access {
      .projecttit {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        color: #656565;
        span {
          font-weight: bold;
          font-size: 14px;
          display: inline-block;
          margin: 10px;
        }
      }
      .projectmain {
        color: #656565;
        h3 {
          margin: 20px 0;
        }
        .projectdouble {
          margin: 20px 0;
          border: 1;
          background-color: #f5f7fa;
          border: 1px solid #eee;
          color: #656565;
          font-size: 14px;
          display: flex;
          align-items: center;
          justify-content: flex-start;
          height: 40px;

          .doL {
            padding: 10px;
            width: 220px;
          }
          .doR {
            padding: 10px;
          }
        }
      }
    }
    //主体3接入审核
    .approval {
    }
  }
}
</style>
<style scoped>
.tab >>> .el-card {
  margin: 10px;
}
.projectdouble >>> .el-divider--vertical {
  display: inline-block;
  width: 1px;
  height: 100%;
  margin: 0 8px;
  vertical-align: middle;
  position: relative;
}
.mainitem >>> .el-form-item__label {
  width: 150px !important;
  white-space: nowrap;
}
</style>
