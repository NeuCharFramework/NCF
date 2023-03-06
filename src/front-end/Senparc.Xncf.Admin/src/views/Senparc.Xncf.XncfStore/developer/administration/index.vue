<template>
  <!-- 项目管理 -->
  <div class="admin">
    <!-- 标题 -->
    <el-card>
      <nav class="nav">
        <h3 class="shoptit">项目管理</h3>
      </nav>
    </el-card>
    <el-card v-if="showInfo">
      <div class="addSearch">
        <el-button type="success" @click="addProject">新建项目</el-button>
        <el-input
          placeholder="项目名称"
          v-model="appName"
          class="input-with-select"
        >
          <el-button slot="append" icon="el-icon-search"></el-button>
        </el-input>
      </div>
      <main class="main">
        <el-row :gutter="10">
          <!-- xs<768px  xl≥1920px -->
          <el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6">
            <div class="appItem">
              <div class="blueArea">
                <h4>应用商店测试</h4>
                <span>建立于2022-12-26</span>
              </div>
              <div class="whiteArea">
                <span class="title">应用商店测试</span>
                <div class="operation">
                  <i
                    class="el-icon-plus"
                    title="创建APP"
                    @click="createApp"
                  ></i>
                  <i class="el-icon-view" title="查看APP"></i>
                  <i class="el-icon-s-custom" title="添加成员"></i>
                  <i class="el-icon-menu" title="管理"></i>
                </div>
              </div>
            </div>
          </el-col>
          <el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6">
            <div class="appItem">
              <div class="blueArea">
                <h4>应用商店测试</h4>
                <span>建立于2022-12-26</span>
              </div>
              <div class="whiteArea">
                <span class="title">应用商店测试</span>
                <div class="operation">
                  <i class="el-icon-plus" title="创建APP"></i>
                  <i class="el-icon-view" title="查看APP"></i>
                  <i class="el-icon-s-custom" title="添加成员"></i>
                  <i class="el-icon-menu" title="管理"></i>
                </div>
              </div>
            </div>
          </el-col>
          <el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6">
            <div class="appItem">
              <div class="blueArea">
                <h4>应用商店测试</h4>
                <span>建立于2022-12-26</span>
              </div>
              <div class="whiteArea">
                <span class="title">应用商店测试</span>
                <div class="operation">
                  <i class="el-icon-plus" title="创建APP"></i>
                  <i class="el-icon-view" title="查看APP"></i>
                  <i class="el-icon-s-custom" title="添加成员"></i>
                  <i class="el-icon-menu" title="管理"></i>
                </div>
              </div>
            </div>
          </el-col>
          <el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6">
            <div class="appItem">
              <div class="blueArea">
                <h4>应用商店测试</h4>
                <span>建立于2022-12-26</span>
              </div>
              <div class="whiteArea">
                <span class="title">应用商店测试</span>
                <div class="operation">
                  <i class="el-icon-plus" title="创建APP"></i>
                  <i class="el-icon-view" title="查看APP"></i>
                  <i class="el-icon-s-custom" title="添加成员"></i>
                  <i class="el-icon-menu" title="管理"></i>
                </div>
              </div>
            </div>
          </el-col>
          <el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6">
            <div class="appItem">
              <div class="blueArea">
                <h4>应用商店测试</h4>
                <span>建立于2022-12-26</span>
              </div>
              <div class="whiteArea">
                <span class="title">应用商店测试</span>
                <div class="operation">
                  <i class="el-icon-plus" title="创建APP"></i>
                  <i class="el-icon-view" title="查看APP"></i>
                  <i class="el-icon-s-custom" title="添加成员"></i>
                  <i class="el-icon-menu" title="管理"></i>
                </div>
              </div>
            </div>
          </el-col>
          <el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6">
            <div class="appItem">
              <div class="blueArea">
                <h4>应用商店测试</h4>
                <span>建立于2022-12-26</span>
              </div>
              <div class="whiteArea">
                <span class="title">应用商店测试</span>
                <div class="operation">
                  <i class="el-icon-plus" title="创建APP"></i>
                  <i class="el-icon-view" title="查看APP"></i>
                  <i class="el-icon-s-custom" title="添加成员"></i>
                  <i class="el-icon-menu" title="管理"></i>
                </div>
              </div>
            </div>
          </el-col>
          <el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6">
            <div class="appItem">
              <div class="blueArea">
                <h4>应用商店测试</h4>
                <span>建立于2022-12-26</span>
              </div>
              <div class="whiteArea">
                <span class="title">应用商店测试</span>
                <div class="operation">
                  <i class="el-icon-plus" title="创建APP"></i>
                  <i class="el-icon-view" title="查看APP"></i>
                  <i class="el-icon-s-custom" title="添加成员"></i>
                  <i class="el-icon-menu" title="管理"></i>
                </div>
              </div>
            </div>
          </el-col>
          <el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6">
            <div class="appItem">
              <div class="blueArea">
                <h4>应用商店测试</h4>
                <span>建立于2022-12-26</span>
              </div>
              <div class="whiteArea">
                <span class="title">应用商店测试</span>
                <div class="operation">
                  <i class="el-icon-plus" title="创建APP"></i>
                  <i class="el-icon-view" title="查看APP"></i>
                  <i class="el-icon-s-custom" title="添加成员"></i>
                  <i class="el-icon-menu" title="管理"></i>
                </div>
              </div>
            </div>
          </el-col>
        </el-row>
      </main>
    </el-card>
    <!-- 新建项目 -->
    <el-card v-else>
      <el-form
        :model="ruleForm"
        :rules="rules"
        ref="ruleForm"
        label-width="100px"
        class="demo-ruleForm"
      >
        <el-form-item label="项目名称" prop="name">
          <el-input v-model="ruleForm.name"></el-input>
        </el-form-item>
        <el-form-item label="项目简介" prop="info">
          <el-input
            type="textarea"
            :rows="4"
            v-model="ruleForm.info"
          ></el-input>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="submitForm('ruleForm')"
            >立即创建</el-button
          >
          <el-button @click="resetForm('ruleForm')">取消</el-button>
        </el-form-item>
      </el-form>
    </el-card>
    <!-- 创建表单 -->
    
  </div>
</template>

<script>
export default {
  data() {
    return {
      // 搜索ipt内容--项目名称
      appName: "",
      showInfo: true,
      ruleForm: {
        name: "",
        info: "",
      },
      rules: {
        name: [{ required: true, message: "请输入项目名称", trigger: "blur" }],
        info: [{ required: true, message: "请输入项目简介", trigger: "blur" }],
      },
    };
  },
  methods: {
    // 进入退出详情页面
    addProject() {
      this.showInfo = !this.showInfo;
    },
    // 表单提交
    submitForm(formName) {
      this.$refs[formName].validate((valid) => {
        if (valid) {
          console.log(this.ruleForm.name, this.ruleForm.info);
        } else {
          return false;
        }
      });
    },
    // 表单重置,退出详情
    resetForm(formName) {
      this.$refs[formName].resetFields();
      this.addProject();
    },
  },
};
</script>

<style lang="scss" scoped>
.admin {
  // 穿透修改按钮颜色和间距，card间距
  ::v-deep .el-card {
    margin: 10px;
  }
  ::v-deep .el-button--danger {
    background-color: #7266ba;
    border: transparent;
  }
  ::v-deep .el-button--success {
    background-color: #37bc9b;
    border: transparent;
  }

  .nav {
    display: flex;
    align-items: center;

    .shoptit {
      background-color: #ffffff;
      color: #929292;
      font-weight: normal;
    }
  }

  .addSearch {
    display: flex;
    align-items: center;
    justify-content: space-between;

    ::v-deep .el-input {
      width: 350px;
    }
  }
  .main {
    margin: 25px;

    .appItem {
      display: flex;
      flex-direction: column;
      color: white;
      width: 100%;
      height: 100%;
      border-radius: 4px;
      margin: 10px;
      .blueArea {
        background-color: #5d9cec;
        padding: 10px 15px;
        border-top-right-radius: 3px;
        border-top-left-radius: 3px;
        width: 100%;

        h4 {
          font-size: 16.5px;
          line-height: 20px;
          height: 40px;
        }
        span {
          font-size: 14px;
        }
      }
      .whiteArea {
        color: gray;
        border-bottom-right-radius: 3px;
        border-bottom-left-radius: 3px;
        padding: 10px 15px;
        border: 1px solid #cfdbe2;

        .title {
          display: inline-block;
          padding: 15px 0;
          height: 70px;
        }
        .operation {
          display: flex;
          align-items: center;
          justify-content: flex-end;

          i {
            cursor: pointer;
            margin: 0 5px;
          }
        }
      }
    }
  }
}
</style>