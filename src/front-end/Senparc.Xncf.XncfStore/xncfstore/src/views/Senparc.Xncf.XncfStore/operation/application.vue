<template>
  <!-- 应用中枢,剩余流程未动 -->
  <div class="application">
    <!-- 标题 -->
    <el-card v-if="!showAddTable">
      <nav class="nav">
        <h3 class="shoptit">NeuChar Group222应用中枢</h3>
        <div class="discover">
          <i class="el-icon-discover"></i>
          <span>使用指南</span>
        </div>
      </nav>
    </el-card>
    <el-card>
      <main class="main" v-if="!showAddTable">
        <div class="mainTitle">
          <el-button type="danger" @click="addTable">新增</el-button>
          <el-button type="primary">删除</el-button>
          <el-input
            placeholder="请输入内容"
            v-model="input3"
            class="input-with-select"
          >
            <el-button
              slot="append"
              icon="el-icon-search"
              @click="search"
            ></el-button>
          </el-input>
        </div>
        <div class="mainCenter">
          <el-table
            ref="multipleTable"
            :data="tableData"
            tooltip-effect="dark"
            style="width: 100%"
            @selection-change="handleSelectionChange"
            border
          >
            <el-table-column type="selection"> </el-table-column>
            <el-table-column label="名称">
              <template slot-scope="scope">{{ scope.row.date }}</template>
            </el-table-column>
            <el-table-column prop="name" label="是否允许重复">
            </el-table-column>
            <el-table-column
              prop="address"
              label="添加事件"
              show-overflow-tooltip
            >
            </el-table-column>
            <el-table-column label="操作"> </el-table-column>
          </el-table>
          <div style="margin-top: 20px">
            <el-button @click="toggleSelection([tableData[1], tableData[2]])"
              >切换第二、第三行的选中状态</el-button
            >
            <el-button @click="toggleSelection()">取消选择</el-button>
          </div>
        </div>
      </main>
      <main class="main" v-if="showAddTable">
        <div class="preservation">
          <el-button type="primary">保存</el-button>
        </div>
        <el-collapse v-model="activeNames" @change="handleChange">
          <el-collapse-item title="应用中枢基础设置" name="1">
            <div class="predetail">
              <el-tabs
                v-model="activeName"
                type="card"
                @tab-click="handleClick"
              >
                <el-tab-pane label="基础信息" name="first">
                  <div class="iptArea">
                    <span>应用中枢名称</span>
                    <el-input
                      v-model="appNameIpt"
                      placeholder="应用中枢名称"
                    ></el-input>
                  </div>
                  <div class="iptArea">
                    <span>应用中枢简介</span>
                    <el-input
                      v-model="appNameIpt"
                      type="textarea"
                      :rows="2"
                      placeholder="应用中枢简介"
                    ></el-input>
                  </div>
                  <div class="repeatArea">
                    <span>重复流程:</span>
                    <el-radio v-model="radio" label="1">允许</el-radio>
                  </div>
                  <div class="graycolor">
                    说明：最后一个流程完成后，扫码重新回到第一个流程中
                  </div>
                  <div class="repeatArea">
                    <span>流程进入码:</span>
                    <el-button type="success" plain>查看</el-button>
                  </div>
                  <div class="graycolor">说明：扫码进入应用中枢。</div>
                </el-tab-pane>
                <el-tab-pane label="全局变量设置" name="second">
                  <div class="graycolor">
                    说明：最后一个流程完成后，扫码重新回到第一个流程中
                  </div>
                  <el-button type="success" plain>添加</el-button>
                </el-tab-pane>
              </el-tabs>
            </div>
          </el-collapse-item>
        </el-collapse>
        <el-collapse v-model="activeNames" @change="handleChange">
          <el-collapse-item title="搜索App名称" name="2">
            <div class="prepri">
              <div class="addArea" @click="showAddProcess">
                <span>+</span>
              </div>
              <div class="process" v-if="showProcess">
                <h4>添加流程</h4>
                <span>选择App</span>
                <el-select v-model="arvalue" placeholder="请选择">
                  <el-option
                    v-for="item in options"
                    :key="item.value"
                    :label="item.label"
                    :value="item.value"
                  >
                  </el-option>
                </el-select>
              </div>
            </div>
          </el-collapse-item>
        </el-collapse>
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
import KeywordReply from "./keywordReply.vue";
export default {
  components: {
    footerinfo,
    feedback,
  },
  data() {
    return {
      input3: "", //搜索的输入框
      //   表单数据
      tableData: [
        {
          date: "2016-05-03",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-02",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-04",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-01",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-08",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-06",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-07",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
      ],
      multipleSelection: [],
      // 切换新增详情首页状态
      showAddTable: false,
      // 折叠面板的展示状态
      activeNames: ["1"],
      //
      activeName: "first",
      // 应用中枢名称ipt
      radio: "",
      appNameIpt: "",
      // 应用中枢配置-添加流程显示状态
      showProcess: false,
      // 显示添加流程
      options: [
        {
          value: "选项",
          label: "选项",
        },
      ],
    };
  },

  methods: {
    // 搜索
    search() {
      console.log("iptvalue", this.input3);
    },
    toggleSelection(rows) {
      if (rows) {
        rows.forEach((row) => {
          this.$refs.multipleTable.toggleRowSelection(row);
        });
      } else {
        this.$refs.multipleTable.clearSelection();
      }
    },
    handleSelectionChange(val) {
      this.multipleSelection = val;
    },
    // 添加表单
    addTable() {
      this.showAddTable = !this.showAddTable;
    },
    // 折叠面板事件
    handleChange(val) {
      console.log(val);
    },
    // 折叠面板-标签框
    handleClick(tab, event) {
      console.log(tab, event);
    },
    //显示应用中枢配置添加流程
    showAddProcess() {
      this.showProcess = !this.showProcess;
    },
  },
};
</script>

<style scoped lang="scss">
.application {
  // 穿透修改按钮颜色和间距，card间距
  ::v-deep .el-card {
    margin: 10px;
  }
  ::v-deep .el-button--danger {
    background-color: #7266ba;
    border: transparent;
  }
  .nav {
    // border-bottom: 1px solid #cfdbe2;
    display: flex;
    align-items: center;
    justify-content: center;

    .shoptit {
      // font-size: 24px;
      background-color: #ffffff;
      color: #929292;
      font-weight: normal;
      // padding: 15px;
    }
    .discover {
      color: #37bc9b;
    }
  }
  .main {
    .preservation {
      background-color: #f5f7fa;
      padding: 10px 15px;
      display: flex;
      align-items: center;
      justify-content: flex-end;
    }
    .predetail {
      margin-top: 10px;
    }
    .mainTitle {
      display: flex;
      align-items: center;
      justify-content: flex-start;
      padding: 10px;
    }
    .mainCenter {
      padding: 10px;
    }
    // 详情部分
    .iptArea {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      justify-content: flex-end;
      margin-bottom: 10px;
      display: flex;
      flex-direction: column;

      span {
        font-weight: bold;
        font-family: "Source Sans Pro";
        color: #656565;
        display: inline-block;
        margin-bottom: 10px;
      }
    }
    .repeatArea {
      margin-bottom: 10px;

      span {
        font-weight: bold;
        font-family: "Source Sans Pro";
        color: #656565;
        display: inline-block;
      }
    }
    .graycolor {
      margin-top: 5px;
      margin-bottom: 10px;
      color: #909293;
      font-size: 12px;
    }
    .prepri {
      .addArea {
        width: 185px;
        height: 107px;
        border: 1px solid rgba(0, 0, 0, 0.12);
        border-radius: 4px;
        margin: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;

        span {
          font-size: 16px;
          font-weight: bold;
        }
      }
      .addArea:hover {
        background-color: #37bc9b;
        color: white;
      }
    }
  }
}
</style>
<style scoped>
.main >>> .el-input {
  margin: 0px 10px;
  width: 300px;
}
.main >>> .el-textarea {
  margin: 0px 10px;
  width: 98%;
}
/* 折叠面板 */
.main >>> .el-collapse-item__header {
  /* background-color: ; */
  background-image: linear-gradient(to right, #37bc9b 0%, #58ceb1 100%);
  color: white;
  padding-left: 15px;
  font-weight: bold;
}
/* 单选框 */
.main >>> .el-radio__label {
  font-weight: bold;
  font-family: "Source Sans Pro";
  color: #656565;
  display: inline-block;
}
.main >>> .el-radio__input {
  margin-left: 10px;
}
/* 按钮 */
.repeatArea >>> .el-button--success {
  border-color: #37bc9b;
  color: #37bc9b;
  margin-left: 10px;
}
.main >>> .el-tabs__content {
  margin-left: 20px;
}
</style>