<template>
  <!-- 商品首页 /XncfStore/shopping/Index.vue -->
  <div class="box">
    <nav class="nav">
      <h3 class="shoptit">应用商店</h3>
    </nav>
    <!-- xs<768  sm≥768px"  md≥992px  lg≥1200px xl≥1920px -->
    <el-card>
      <el-row :gutter="20">
        <el-col :span="6" :xs="16" :sm="16" :md="16" :lg="16" :xl="18">
          <div class="grid-content bg-purple">
            <div class="mainLeft">
              <div class="topSelect">
                <span>筛选条件：</span>
                <span>状态：</span>
                <el-select v-model="stateValue" placeholder="请选择">
                  <el-option
                    v-for="item in stateOptions"
                    :key="item.value"
                    :label="item.label"
                    :value="item.value"
                  >
                  </el-option>
                </el-select>
                <el-input
                  placeholder="请输入内容"
                  v-model="inputInfo"
                  class="input-with-select"
                >
                  <el-button slot="append" icon="el-icon-search"></el-button>
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
                    <img src="./imgs/企业微信截图_20221222111152.png" alt="" />
                    <span>成为开发者</span>
                  </div>
                  <span class="application">暂无相关应用</span>
                </div>
              </el-tab-pane>
              <el-tab-pane label="私有应用" name="second">配置管理</el-tab-pane>
            </el-tabs>
          </div>
        </el-col>
      </el-row>
    </el-card>
  </div>
</template>

<script>
export default {
  data() {
    return {
      //下拉选择数据
      stateOptions: [
        {
          value: "1",
          label: "所有",
        },
        {
          value: "2",
          label: "基础服务",
        },
        {
          value: "3",
          label: "互动推广",
        },
        {
          value: "4",
          label: "社交应用",
        },
        {
          value: "5",
          label: "游戏娱乐",
        },
        {
          value: "6",
          label: "电子商务",
        },
        {
          value: "7",
          label: "应用工具",
        },
        {
          value: "8",
          label: "办公协同",
        },
        {
          value: "9",
          label: "企业管理",
        },
        {
          value: "10",
          label: "商务服务",
        },
        {
          value: "11",
          label: "投资理财",
        },
        {
          value: "12",
          label: "行业应用",
        },
        {
          value: "13",
          label: "其他",
        },
      ],
      //下拉选中数据
      stateValue: "1",
      //输入框内容
      inputInfo: "",
      // 显示的标签页
      activeName: "first",
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
    },
    // 点击成为开发者
    become() {
      console.log("成为开发者");
    },
  },
};
</script>
<style  scoped>
.box {
  margin: 5px;
  width: 100%;
  height: 100%;
  background-color: #f5f7fa;
}
.shoptit {
  font-size: 24px;
  background-color: #ffffff;
  border-bottom: 1px solid #cfdbe2;
  color: #929292;
  font-weight: normal;
  padding: 15px;
}
.mainLeft {
  background-color: #fff;
}
.topSelect {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  white-space: nowrap;
  font-weight: bold;
}
.mainTit {
  font-size: 18px;
  margin: 10.5px 0;
  padding: 15px 0;
}
.forItem {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 20px 25px;
  cursor: pointer;
}
.forItem .logo {
  width: 64px;
  height: 64px;
  border-radius: 6px;
}

.forItem h4 {
  margin-top: 10px;
  height: 40px;
}
.forItem div {
  margin-bottom: 10px;
  height: 20px;
}

.forItem .price {
  display: flex;
  align-items: center;
  justify-content: center;
  white-space: nowrap;
}
.become {
  background-color: #fff;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}
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
}
.developer img {
  width: 80px;
}
.developer span {
  color: #37bc9b;
}
</style>
<style scoped>
.topSelect >>> .el-input-group {
  margin-left: 10px;
  width: 400px;
}
</style>