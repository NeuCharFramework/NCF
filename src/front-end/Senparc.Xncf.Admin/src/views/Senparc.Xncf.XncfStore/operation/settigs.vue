<template>
  <!-- 运营设置 -->
  <div class="setting">
    <el-card>
      <div class="title">
        <h3>运营设置</h3>
        <div class="use">
          <i class="el-icon-edit"></i>
          <span>使用指南</span>
        </div>
      </div>
    </el-card>
    <el-card>
      <main class="main" v-if="changState">
        <div class="operation">
          <el-button type="success" @click="changMain">添加运营者</el-button>
          <el-button @click="editState" v-if="edit">编辑</el-button>
          <el-button @click="preservation" type="success" v-if="!edit"
            >保存编辑</el-button
          >
          <el-button @click="edit = true" type="danger" plain v-if="!edit"
            >取消编辑</el-button
          >
        </div>
        <div class="table">
          <el-table
            :data="tableData"
            style="width: 100%"
            :header-row-style="{ 'background-color': '#f5f5f5' }"
            :header-cell-style="{ 'background-color': 'transparent' }"
          >
            <el-table-column prop="date" label="运营者"> </el-table-column>
            <el-table-column prop="name" label="素材关键字回复">
            </el-table-column>
            <el-table-column prop="province" label="自定义菜单">
            </el-table-column>
            <el-table-column prop="city" label="运营设置"> </el-table-column>
            <el-table-column prop="address" label="应用中枢"> </el-table-column>
          </el-table>
        </div>
      </main>
      <main class="main" v-else>
        <div class="searchArea">
          <div class="areaL">
            <h3>搜索</h3>
            <el-input
              placeholder="请输入内容"
              v-model="userSearch"
              class="input-with-select"
            >
              <el-button slot="append" icon="el-icon-search"></el-button>
            </el-input>
            <span>输入用户名搜索,可绑定多个用户名</span>
          </div>
          <el-divider direction="vertical"></el-divider>
          <div class="areaR">
            <h4>权限设置</h4>
            <div class="selectArea">
              <div class="areatit">权限允许情况</div>
              <el-divider></el-divider>
              <el-checkbox-group v-model="checkList">
                <el-checkbox label="素材关键字回复">
                  <template>
                    <div class="checkboxItem">
                      <div class="spantit">素材关键字回复</div>
                      <span
                        >对素材中的文字、图文消息、图片、语音、视频等素材进行设置，包含增删改查同步素材等所有操作；对关键字回复接收消息、回复内容进行设置，包含增删改查同步等所有操作。</span
                      >
                    </div>
                  </template>
                </el-checkbox>

                <el-checkbox label="自定义菜单">
                  <template>
                    <div class="checkboxItem">
                      <div class="spantit">自定义菜单</div>
                      <span>
                        对自定义菜单内容进行管理设置，包含增删改查同步等所有操作。
                      </span>
                    </div>
                  </template>
                </el-checkbox>
                <el-checkbox label="运营设置">
                  <template>
                    <div class="checkboxItem">
                      <div class="spantit">运营设置</div>
                      <span>
                        对 NeuChar Cell
                        的运营者进行设置，包含增删改查等所有操作。
                      </span>
                    </div>
                  </template>
                </el-checkbox>
                <el-checkbox label="">
                  <template>
                    <div class="checkboxItem">
                      <div class="spantit">应用中枢</div>
                      <span>
                        对 NeuChar Cell
                        的应用中枢进行设置，包含增删改查等所有操作。
                      </span>
                    </div>
                  </template>
                </el-checkbox>
              </el-checkbox-group>
            </div>
          </div>
        </div>
        <div class="centerbtn">
          <el-button type="success">保存</el-button>
          <el-button @click="changMain">取消</el-button>
        </div>
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
  components: {
    footerinfo,
    feedback,
  },
  data() {
    return {
      tableData: [
        {
          date: "2016-05-03",
          name: "王小虎",
          province: "上海",
          city: "普陀区",
          address: "上海市普陀区金沙江路 1518 弄",
        },
      ],
      // 编辑按钮状态
      edit: true,
      // 运营者页面切换使用状态
      changState: true,
      //搜索运营者ipt
      userSearch: "",
      //多选框
      checkList: ["选中且禁用", "复选框 A"],
    };
  },
  methods: {
    // 点击编辑
    editState() {
      this.edit = false;
    },
    // 保存编辑
    preservation() {
      console.log("保存");
    },
    // 切换添加运营者页面
    changMain() {
      this.changState = !this.changState;
    },
  },
};
</script>

<style scoped lang="scss">
.setting {
  ::v-deep .el-card {
    margin: 10px;
  }

  .title {
    display: flex;
    align-items: center;

    .use {
      color: #37bc9b;
      margin: 0 10px;
    }
  }
  .main {
    .operation {
      // 穿透修改按钮颜色和间距
      ::v-deep .el-button--success {
        color: #fff;
        background-color: #37bc9b;
        border-color: transparent;
      }
      ::v-deep .el-button {
        padding: 7px 12px;
      }
    }
    .table {
      margin: 20px 0;
    }
    // 搜索用户名页面
    .searchArea {
      display: flex;
      align-items: flex-start;
      justify-content: center;
      height: 500px;
      width: 100%;
      // justify-content: flex-end;
      .areaL {
        padding: 25px;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: flex-start;
        flex: 1;

        h3 {
          margin: 10px 0;
          margin-bottom: 30px;
          font-weight: 600;
        }
        span {
          display: inline-block;
          margin: 10px 0;
          color: #909fa7;
          font-size: 14px;
        }
      }
      .areaR {
        width: 500px;
        padding: 25px;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: flex-start;
        h4 {
          margin: 40px 0;
        }
        .selectArea {
          .checkboxItem {
            display: flex;
            flex-direction: column;
            .spantit {
              color: black !important;
              font-size: 15px !important;
              margin: 5px 0;
            }
          }
        }
      }
    }
    // 按钮
    .centerbtn {
      padding: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  }
}
</style>
<style scoped>
.areaL >>> .el-input {
  width: 450px;
}
.areaL >>> .el-button {
  color: #37bc9b;
  font-weight: bold;
  background-color: #fff;
  width: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.areaL >>> .el-icon-search {
  font-weight: bold;
}
.searchArea >>> .el-divider--vertical {
  display: inline-block;
  width: 1px;
  height: 100%;
  margin: 0 20px;
  vertical-align: middle;
  position: relative;
}
.areaR >>> .el-checkbox {
  margin: 10px 0;
  display: flex;
  justify-content: flex-start;
  /* align-items: flex-start; */
  align-items: center;
}
.areaR >>> .el-checkbox-group {
  display: flex !important;
  flex-direction: column !important;
  flex-wrap: wrap;
}
.areaR >>> .el-checkbox__input {
  margin: 10px 0;
}
.areaR >>> .el-checkbox__label {
  word-break: break-all;
  white-space: pre-wrap;
  color: gray;
}
.selectArea >>> .el-divider--horizontal {
  margin: 10px 0;
}
</style>