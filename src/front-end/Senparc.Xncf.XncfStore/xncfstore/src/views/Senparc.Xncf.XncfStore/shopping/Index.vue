<template>
  <!-- 商品首页 /XncfStore/shopping/Index.vue -->
  <div class="box">
    <el-card>
      <h3 class="shoptit">应用商店</h3>
    </el-card>
    <!-- xs<768  sm≥768px"  md≥992px  lg≥1200px xl≥1920px -->
    <el-card>
      <el-row :gutter="20">
        <el-col :span="6" :xs="18" :sm="18" :md="18" :lg="18" :xl="18">
          <div class="grid-content bg-purple">
            <div class="mainLeft">
              <div class="topSelect">
                <span>筛选条件：</span>
                <span>状态：</span>
                <el-select v-model="stateValue" placeholder="请选择">
                  <el-option
                    v-for="item in stateOptions"
                    :key="item.value"
                    :label="item.value"
                    :value="item.value"
                  >
                  </el-option>
                </el-select>
                <el-input
                  placeholder="请输入内容"
                  v-model="inputInfo"
                  class="input-with-select"
                >
                  <el-button
                    slot="append"
                    icon="el-icon-search"
                    @click="searchInfo"
                  ></el-button>
                </el-input>
              </div>
              <div class="mainItem">
                <h4 class="mainTit">热门应用</h4>
                <div class="mainCen">
                  <el-row :gutter="20">
                    <div v-for="item in 10" :key="item">
                      <el-col
                        :span="6"
                        :xs="24"
                        :sm="12"
                        :md="12"
                        :lg="8"
                        :xl="6"
                      >
                        <el-card style="margin: 5px 0">
                          <div
                            class="forItem"
                            @click="godetail(item)"
                            title="查看详情"
                          >
                            <img
                              class="logo"
                              src="./imgs/201905191646449997..jpg"
                              alt=""
                            />
                            <h4>微信API Swagger文档</h4>
                            <div>类别：应用工具</div>
                            <div class="price">
                              <span>现价：￥9.9</span>
                              <p>原价：￥99.00</p>
                            </div>
                          </div>
                        </el-card>
                      </el-col>
                    </div>
                  </el-row>
                </div>
              </div>
            </div>
          </div>
        </el-col>
        <el-col :span="6" :xs="6" :sm="6" :md="6" :lg="6" :xl="6">
          <div class="grid-content bg-purple">
            <el-tabs v-model="activeName" type="card" @tab-click="handleClick">
              <el-tab-pane label="我创建的应用" name="first">
                <div class="become">
                  <div class="developer" title="成为开发者" @click="become">
                    <img
                      src="./imgs/企业微信截图_20221222111152.png"
                      alt="
                    加载未成功"
                    />
                    <span>成为开发者</span>
                  </div>
                  <span class="application">暂无相关应用</span>
                </div>
              </el-tab-pane>
              <el-tab-pane label="私有应用" name="second">
                <div class="appAll">
                  <el-row>
                    <el-col :span="12">
                      <div class="appItem">
                        <div class="rotateTick" v-if="this.private == true">
                          私有
                        </div>
                        <img
                          class="itemLogo"
                          src="./imgs/201905191646449997..jpg"
                        />
                        <span class="itemTit">应用商店测试</span>
                        <el-button type="primary" size="mini">未接入</el-button>
                      </div>
                    </el-col>
                    <el-col :span="12">
                      <div class="appItem">
                        <div class="rotateTick" v-if="this.private == true">
                          私有
                        </div>
                        <img
                          class="itemLogo"
                          src="./imgs/201905191646449997..jpg"
                        />
                        <span class="itemTit">应用商店测试</span>
                        <el-button type="primary" size="mini">未接入</el-button>
                      </div>
                    </el-col>
                  </el-row>
                </div>
              </el-tab-pane>
            </el-tabs>
          </div>
        </el-col>
      </el-row>
    </el-card>
    <!-- 反馈部分 -->
    <feedback />
  </div>
</template>
<script>
import feedback from "@/components/Feedback/index";
export default {
  components: {
    feedback,
  },
  data() {
    return {
      //下拉选择数据
      stateOptions: [
        {
          value: "所有",
        },
        {
          value: "基础服务",
        },
        {
          value: "互动推广",
        },
        {
          value: "社交应用",
        },
        {
          value: "游戏娱乐",
        },
        {
          value: "电子商务",
        },
        {
          value: "应用工具",
        },
        {
          value: "办公协同",
        },
        {
          value: "企业管理",
        },
        {
          value: "商务服务",
        },
        {
          value: "投资理财",
        },
        {
          value: "行业应用",
        },
        {
          value: "其他",
        },
      ],
      //下拉选中数据
      stateValue: "",
      //输入框内容
      inputInfo: "",
      // 显示的标签页
      activeName: "first",
      // 是否私有应用
      private: true,
    };
  },
  methods: {
    // 标签页事件
    handleClick(tab, event) {
      console.log(tab, event);
    },
    // 去详情
    godetail(item) {
      console.log(item);
      this.$router.push({
        path: "/Senparc.Xncf.XncfStore/shopping/detail",
      });
    },
    // 点击成为开发者
    become() {
      console.log("成为开发者");
    },
    // 搜索
    searchInfo() {
      // console.log("iptValue", this.inputInfo, "selectValue", this.stateValue);
      if (this.inputInfo == null || this.inputInfo == "") {
        if (this.stateValue == "" || this.stateValue == null) {
          console.log("未选下拉框，未输入内容");
        } else if (this.stateValue != "" || this.stateValue != null) {
          console.log("只选下拉框,未输入内容", this.stateValue);
        }
      } else if (this.inputInfo != null || this.inputInfo != "") {
        if (this.stateValue == "" || this.stateValue == null) {
          console.log("未选下拉框，只输入内容", this.inputInfo);
        }
      }
    },
  },
};
</script>
<style lang="scss" scoped>
.box {
  margin: 5px;
  width: 100%;
  height: 100%;
  ::v-deep .el-card {
    margin: 10px;
  }
  .shoptit {
    font-size: 24px;
    background-color: #ffffff;
    // border-bottom: 1px solid #cfdbe2;
    color: #929292;
    font-weight: normal;
    // padding: 15px;
  }
  .mainLeft {
    background-color: #fff;
    .topSelect {
      display: flex;
      align-items: center;
      justify-content: flex-start;
      white-space: nowrap;
      font-weight: bold;
    }
    .mainItem {
      .mainTit {
        font-size: 18px;
        margin: 10.5px 0;
        padding: 15px 0;
      }
      .mainCen {
        .forItem {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          padding: 20px 25px;
          cursor: pointer;

          .logo {
            width: 64px;
            height: 64px;
            border-radius: 6px;
          }
          h4 {
            margin-top: 10px;
            height: 40px;
          }
          div {
            margin-bottom: 10px;
            height: 20px;
          }

          .price {
            display: flex;
            align-items: center;
            justify-content: center;
            white-space: nowrap;
          }
        }
      }
    }
  }
  // 成为开发者
  .become {
    background-color: #fff;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    .developer {
      margin: 10px 30px;
      padding: 20px;
      border: 1px solid rgba(0, 0, 0, 0.12);
      width: 100%;
      font-size: 16px;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      cursor: pointer;

      img {
        width: 80px;
      }
      span {
        color: #37bc9b;
      }
    }
  }
  // 已有的内容
  .appAll {
    .appItem {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      border: 1px solid rgba(0, 0, 0, 0.12);
      margin: 5px;
      padding: 15px;
      position: relative;
      overflow: hidden;

      .rotateTick {
        position: absolute;
        left: -20px;
        top: 23px;
        border-radius: 0;
        transform: rotate(-45deg);
        transform-origin: 40px;
        width: 100px;
        background-image: linear-gradient(to bottom, #37bc9b 0%, #58ceb1 100%);
        font-size: 0.05em;
        color: #fff;
        text-align: center;
      }

      ::v-deep .el-button--mini {
        font-weight: bold;
        background-color: #23b7e5;
        border: transparent;
        padding: 4px 8px;
      }

      .itemLogo {
        width: 40px;
        border-radius: 3px;
      }
      .itemTit {
        font-size: 14px;
        line-height: 47px;
      }
    }
  }
}
</style>
<style scoped>
.topSelect >>> .el-input-group {
  margin-left: 10px;
  width: 400px;
}
</style>