<template>
  <!-- 项目应用 -->
  <div class="app">
    <!-- 标题 -->
    <el-card>
      <nav class="nav">
        <h3 class="shoptit">运行监测</h3>
        <div class="discover">
          <span style="cursor: pointer; color: #5d9cec">开发者中心</span> /
          <span>项目应用</span>
        </div>
      </nav>
    </el-card>
    <div class="main">
      <!-- 个人信息 -->
      <div class="infoArea">
        <div class="infoL">
          <div class="infoL-t">
            <i class="el-icon-lollipop"></i>
            <h4>Hi,Rodear!</h4>
          </div>
          <span class="infoL-b"> 快去创建自己的应用吧！加油哦！ </span>
        </div>
        <div class="infoR">
          <div class="infor-item">
            <h3>0</h3>
            <span> 30天内出售应用 （收入 ¥0.00）</span>
          </div>
          <div class="infor-item">
            <h3>1</h3>
            <span>收到评论</span>
          </div>
          <div class="infor-item">
            <h3>￥0.00</h3>
            <span>总收入</span>
          </div>
        </div>
      </div>
      <!-- 表单 -->
      <el-card>
        <div class="table">
          <div class="topSelect">
            <el-button type="success" round icon="el-icon-edit-outline">
              审核内部应用
            </el-button>
            <div class="selectItem">
              <h5>项目：</h5>
              <el-select v-model="projectValue" placeholder="请选择">
                <el-option
                  v-for="item in projectOptions"
                  :key="item.value"
                  :label="item.label"
                  :value="item.value"
                >
                </el-option>
              </el-select>
            </div>
            <div class="selectItem">
              <h5>状态：</h5>
              <el-select v-model="stateValue" placeholder="请选择">
                <el-option
                  v-for="item in stateOptions"
                  :key="item.value"
                  :label="item.label"
                  :value="item.value"
                >
                </el-option>
              </el-select>
            </div>
            <div class="selectItem">
              <h5>应用类别：</h5>
              <el-select v-model="appValue" placeholder="请选择">
                <el-option
                  v-for="item in appOptions"
                  :key="item.value"
                  :label="item.label"
                  :value="item.value"
                >
                </el-option>
              </el-select>
              <el-input
                placeholder="请输入内容"
                v-model="appIptValue"
                class="input-with-select"
              >
                <el-button slot="append" icon="el-icon-search"></el-button>
              </el-input>
            </div>
          </div>
          <el-table :data="tableData" border style="width: 100%">
            <el-table-column prop="name" label="名称">
              <template>
                <div class="tableName">
                  <img src="./img/bg.jpg" alt="" />
                  <span>应用商店测试</span>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="project" label="项目"> </el-table-column>
            <el-table-column prop="appCategory" label="应用类别">
            </el-table-column>
            <el-table-column prop="edition" label="版本"> </el-table-column>
            <el-table-column prop="price" label="价格"> </el-table-column>
            <el-table-column prop="subscriptions" label="订阅次数">
            </el-table-column>
            <el-table-column prop="totalMonths" label="合计月数">
            </el-table-column>
            <el-table-column prop="private" label="私有">
              <template>
                <div>
                  <span class="stateTrue">是</span>
                  <span class="stateFalse">否</span>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="state" label="状态"></el-table-column>
            <el-table-column label="操作" width="400">
              <template>
                <div>
                  <el-button
                    type="success"
                    icon="el-icon-edit"
                    plain
                    title="编辑"
                  >
                  </el-button>
                  <el-button
                    type="success"
                    icon="el-icon-s-check"
                    plain
                    title="开发接入"
                  >
                    开发接入/内部审核
                  </el-button>
                  <el-button
                    type="success"
                    icon="el-icon-menu"
                    plain
                    title="订阅记录"
                  >
                    订阅记录
                  </el-button>
                </div>
              </template>
            </el-table-column>
          </el-table>
        </div>
        <!-- 分页 -->
        <div class="pagin">
          <el-pagination
            background
            layout="prev, pager, next"
            :total="allTotal"
            @current-change="getPageNum"
          >
          </el-pagination>
        </div>
      </el-card>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      // 项目选择
      projectOptions: [
        {
          value: "请选择",
          label: "请选择",
        },
        {
          value: "应用商店测试",
          label: "应用商店测试",
        },
      ],
      // 项目选择值
      projectValue: "",
      // 状态选择
      stateOptions: [
        {
          value: "请选择",
          label: "请选择",
        },
        {
          value: "未接入",
          label: "未接入",
        },
        {
          value: "申请接入",
          label: "申请接入",
        },
        {
          value: "已接入",
          label: "已接入",
        },
        {
          value: "已上架",
          label: "已上架",
        },
        {
          value: "已下架",
          label: "已下架",
        },
        {
          value: "违规下架",
          label: "违规下架",
        },
        {
          value: "内部审核",
          label: "内部审核",
        },
      ],
      // 状态选择值
      stateValue: "",
      // 应用选择
      appOptions: [
        {
          value: "请选择",
          label: "请选择",
        },
        {
          value: "基础服务",
          label: "基础服务",
        },
        {
          value: "互动推广",
          label: "互动推广",
        },
        {
          value: "社交应用",
          label: "社交应用",
        },
        {
          value: "游戏娱乐",
          label: "游戏娱乐",
        },
        {
          value: "电子商务",
          label: "电子商务",
        },
        {
          value: "应用工具",
          label: "应用工具",
        },
        {
          value: "办公协同",
          label: "办公协同",
        },
        {
          value: "企业管理",
          label: "企业管理",
        },
        {
          value: "商务服务",
          label: "商务服务",
        },
        {
          value: "投资理财",
          label: "投资理财",
        },
        {
          value: "行业应用",
          label: "行业应用",
        },
        {
          value: "其他",
          label: "其他",
        },
      ],
      // 应用选择值
      appValue: "",
      // 应用ipt值
      appIptValue: "",
      // 表单数据
      tableData: [
        {
          name: "王小虎", //名称
          project: "王小虎", //项目
          appCategory: "王小虎", //应用类别
          edition: "王小虎", //版本
          price: "王小虎", //价格
          subscriptions: "王小虎", //订阅次数
          totalMonths: "王小虎", //合计月数
          private: "王小虎", //私有
          state: "王小虎", //状态
        },
      ],
      // 分页总条目页数,真实总页数需要*10(element组件问题)
      allTotal: 1000,
    };
  },
  methods: {
    // 获取点击分页的数字
    getPageNum(val) {
      console.log("分页页数", val);
    },
  },
};
</script>

<style scoped lang="scss">
.app {
  // 穿透修改按钮颜色和间距，card间距
  ::v-deep .el-card {
    margin: 10px;
  }
  ::v-deep .el-button--success {
    background-color: #37bc9b;
    color: white;
    border: transparent;
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
    padding: 10px 0;
    margin: 0 10px;
    // 个人信息部分
    .infoArea {
      background-image: url(./img/bg.jpg);
      background-size: cover;
      width: 100%;
      height: 90px;
      color: white;
      display: flex;
      justify-content: space-between;
      align-items: center;

      .infoL {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        justify-content: center;
        margin: 0px 15px;

        .infoL-t {
          display: flex;
          align-items: center;
          font-size: 18px;
          font-weight: bold;
          margin-bottom: 10px;
          h4 {
            display: inline-block;
            margin: 0 5px;
          }
        }
        .infoL-b {
          font-size: 14px;
        }
      }
      .infoR {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        margin-right: 10px;
        .infor-item {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          margin: 0 10px;

          h3 {
            font-size: 24px;
          }
        }
      }
    }
    // 表单部分
    .table {
      .topSelect {
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        margin-bottom: 20px;

        .selectItem {
          display: flex;
          align-items: center;
          margin: 0 5px;

          h5 {
            color: #656565;
            font-size: 14px;
            white-space: nowrap;
          }

          ::v-deep .el-select {
            width: 150px;
            font-size: 12px;
          }
          ::v-deep .el-input-group {
            width: 220px;
            font-size: 12px;
            margin-left: 10px;
          }
        }
      }
      .tableName {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        img {
          border-radius: 6px;
          margin-bottom: 5px;
          width: 64px;
          height: 100%;
        }

        span {
          font-size: 12px;
          color: #5d9cec;
        }
      }
      .stateTrue {
        background-color: #27c24c;
        padding: 4px;
        font-weight: bold;
        color: white;
        border-radius: 5px;
        margin: 0 2px;
      }
      .stateFalse {
        background-color: red;
        padding: 4px;
        font-weight: bold;
        color: white;
        border-radius: 5px;
        margin: 0 2px;
      }
    }
    // 分页
    .pagin {
      margin: 20px 0;
    }
  }
}
</style>