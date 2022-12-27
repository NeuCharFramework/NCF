<template>
  <!-- 商品首页 /XncfStore/shopping/Index.vue -->
  <div class="box">
    <nav class="nav">
      <h3 class="shoptit">应用详情-微微嗨-签到</h3>
    </nav>
    <!-- xs<768  sm≥768px"  md≥992px  lg≥1200px xl≥1920px -->
    <el-card>
      <el-row :gutter="20">
        <!-- 左边 -->
        <el-col :span="6" :xs="16" :sm="16" :md="16" :lg="16" :xl="18">
          <div class="grid-content bg-purple">
            <div class="mainLeft">
              <!-- 标题 -->
              <div class="lefttit">
                <img src="./imgs/企业微信截图_20221222111152.png" alt="" />
                <div class="titInfo">
                  <div class="infoTop">
                    <span>已认证</span>
                    <p>开发者：苏州盛派网络服务有限公司</p>
                  </div>
                  <div class="infoBot">成为开发者日期：2022年4月6日</div>
                </div>
              </div>
              <!-- 详情 -->
              <div class="leftDet">
                <div style="padding: 15px">
                  <img
                    style="width: 250px; height: 250px"
                    src="./imgs/201905191646449997..jpg"
                    alt=""
                  />
                </div>
                <div class="DetIt">
                  <div class="infoItem">
                    <span>应 用 名 称：</span>
                    <p>微微嗨-签到</p>
                  </div>
                  <div class="infoItem">
                    <span>应 用 类 别：</span>
                    <p>基础服务</p>
                  </div>
                  <div class="infoItem">
                    <span>应 用 等 级：</span>
                    <p>微微嗨-签到</p>
                  </div>
                  <div class="infoItem">
                    <span>应 用 订 阅：</span>
                    <p>已被订阅</p>
                  </div>
                  <div class="infoItem">
                    <span>应 用 现 价：</span>
                    <p>¥0.00</p>
                  </div>
                  <div class="infoItem">
                    <span>应 用 原 价：</span>
                    <p>微微嗨-签到</p>
                  </div>
                  <div class="infoItem">
                    <span>发 布 时 间：</span>
                    <p>微微嗨-签到</p>
                  </div>
                </div>
                <div class="DetIt">
                  <div class="infoItem">
                    <span>应 用 单 价：</span>
                    <p>微微嗨-签到</p>
                  </div>
                  <div class="infoItem">
                    <span>购 买 月 数：</span>
                    <p>微微嗨-签到</p>
                  </div>
                  <div class="infoItem">
                    <span>扣 除 积 分：</span>
                    <p>微微嗨-签到</p>
                  </div>
                  <div class="infoItem">
                    <span> NeuChar Cell 账号：</span>
                    <p>微微嗨-签到</p>
                  </div>
                  <div class="infoItem">
                    <el-button type="primary" @click="dialogVisible = true"
                      >立即订阅</el-button
                    >
                  </div>
                </div>
              </div>
              <!-- 底 -->
              <div class="leftBot">
                <el-tabs type="border-card">
                  <el-tab-pane label="应用详情">
                    <div class="tabsItem">
                      <div class="tabsItem-th">
                        <span>试用账号</span>
                        <span>全部</span>
                      </div>
                      <div class="tabsItem-th">
                        <span>语言</span>
                        <span>简体中文</span>
                      </div>
                      <div class="tabsItem-th">
                        <span>是否需要验证</span>
                        <span>否</span>
                      </div>
                      <div class="tabsItem-th">
                        <span>发布日期</span>
                        <span>2022-12-21</span>
                      </div>
                      <div class="tabsItem-th">
                        <span>人气指数</span>
                        <span>已被订阅155次，总计286个月</span>
                      </div>
                      <div class="tabsItem-th">
                        <span>开发者</span>
                        <div
                          style="
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            color: #37bc9b;
                          "
                        >
                          <i class="el-icon-edit"></i>
                          <span>已认证</span>
                        </div>
                      </div>
                      <div class="tabsItem-th">
                        <span>内容提示</span>
                        <span>微微嗨签到</span>
                      </div>
                    </div>
                  </el-tab-pane>
                  <el-tab-pane label="应用参数">
                    <el-table :data="tableData" style="width: 100%">
                      <el-table-column prop="date" label="参数" width="380">
                      </el-table-column>
                      <el-table-column prop="dateinfo" label="" width="180">
                      </el-table-column>
                      <el-table-column
                        prop="explain"
                        label="参数说明"
                        width="380"
                      >
                      </el-table-column>
                    </el-table>
                  </el-tab-pane>
                  <el-tab-pane label="使用攻略">
                    <div class="tabsItem">
                      <div class="explain">
                        <span>一 、选择一个New Char账号进行注册</span>
                        <span>一 、选择一个New Char账号进行注册</span>
                      </div>
                    </div>
                  </el-tab-pane>
                  <el-tab-pane label="用户评论">
                    <el-empty :image-size="200" description="暂时没有更多评论">
                    </el-empty>
                    <div class="tabsItem">
                      <div class="comment">
                        <span class="user">张三：</span>
                        <div class="evaluate">
                          <i class="el-icon-share"></i>
                          <span class="">非常好</span>
                        </div>
                        <div class="timer">
                          <i class="el-icon-share"></i>
                          <span class="timer">2022-10-5 10:00</span>
                        </div>
                      </div>
                      <div class="commentInfo">
                        <span>评论：</span>
                        <p>棒棒棒！非常实用！还可以自由拼接流程！</p>
                      </div>
                      <span class="span">没有更多评论</span>
                    </div>
                  </el-tab-pane>
                  <el-tab-pane label="开发者信息">
                    <div class="tabsItem">
                      <div class="tabsItem-th">
                        <span>开发者名称</span>
                        <span>苏州盛派网络科技有限公司</span>
                      </div>
                      <div class="tabsItem-th">
                        <span>是否认证</span>
                        <div
                          style="
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            color: #37bc9b;
                          "
                        >
                          <i class="el-icon-edit"></i>
                          <span>已认证</span>
                        </div>
                      </div>
                      <div class="tabsItem-th">
                        <span>类别</span>
                        <span>公司</span>
                      </div>
                      <div class="tabsItem-th">
                        <span>地区</span>
                        <span>江苏省 苏州市 吴中区</span>
                      </div>
                      <div class="tabsItem-th">
                        <span>等级</span>
                        <span>0</span>
                      </div>
                      <div class="tabsItem-th">
                        <span>成为开发者时间</span>
                        <span>2018-10-23</span>
                      </div>
                    </div>
                  </el-tab-pane>
                </el-tabs>
              </div>
            </div>
          </div>
        </el-col>
        <!-- 右边 -->
        <el-col :span="6" :xs="6" :sm="6" :md="6" :lg="6" :xl="6">
          <div class="grid-content bg-purple">
            <el-row :gutter="20">
              <div v-for="it in 3" :key="it">
                <el-col :span="6" :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
                  <el-card style="margin: 5px 0">
                    <div class="forItem">
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
        </el-col>
      </el-row>
    </el-card>
    <!-- 推荐应用 -->
    <div class="recommend">
      <el-card>
        <h4>推荐应用</h4>
        <el-row :gutter="20">
          <el-col :span="24" :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
            <div class="grid-content bg-purple">
              <el-row :gutter="20">
                <div v-for="it in 3" :key="it">
                  <el-col :span="6" :xs="6" :sm="6" :md="6" :lg="6" :xl="6">
                    <el-card style="margin: 5px 0">
                      <div class="forItem">
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
          </el-col>
        </el-row>
      </el-card>
    </div>
    <!-- 弹出框 -->
    <el-dialog
      :visible.sync="dialogVisible"
      width="30%"
      :before-close="handleClose"
      center="center"
    >
      <div class="dialogmain">
        <div class="logo">
          <i class="el-icon-lollipop"></i>
        </div>
        <h3>订阅信息</h3>
        <div class="number">
          <span>绑定NeuChart账号</span>
          <p class="greencolor">SenparcZhang</p>
        </div>
        <div class="number">
          <span>订阅时长</span>
          <p class="greencolor">1</p>
          <span>个月</span>
        </div>
        <div class="number">
          <span>将会扣除</span>
          <p class="greencolor">0</p>
          <span>积分</span>
        </div>
      </div>
      <span slot="footer" class="dialog-footer">
        <el-button @click="dialogVisible = false">取 消</el-button>
        <el-button type="primary" @click="gomyapp">确 定</el-button>
      </span>
    </el-dialog>
  </div>
</template>
  
  <script>
export default {
  data() {
    return {
      // 显示的标签页
      activeName: "first",
      //应用参数表格
      tableData: [
        {
          date: "进入会话关键词",
          dateinfo: "enter",
          explain:
            "输入该关键词后，进入应用状态，在此状态下，用户发送的消息都会直接发送到此应用。",
        },
        {
          date: "进入会话关键词",
          dateinfo: "enter",
          explain:
            "输入该关键词后，进入应用状态，在此状态下，用户发送的消息都会直接发送到此应用。",
        },
        {
          date: "进入会话关键词",
          dateinfo: "enter",
          explain:
            "输入该关键词后，进入应用状态，在此状态下，用户发送的消息都会直接发送到此应用。",
        },
        {
          date: "进入会话关键词",
          dateinfo: "enter",
          explain:
            "输入该关键词后，进入应用状态，在此状态下，用户发送的消息都会直接发送到此应用。",
        },
        {
          date: "进入会话关键词",
          dateinfo: "enter",
          explain:
            "输入该关键词后，进入应用状态，在此状态下，用户发送的消息都会直接发送到此应用。",
        },
      ],
      //弹出框状态
      dialogVisible: false,
    };
  },
  methods: {
    // 标签页事件
    handleClick(tab, event) {
      console.log(tab, event);
    },
    // 弹出框事件
    handleClose(done) {
      done();
    },
    //去到个人应用
    gomyapp() {
      this.$router.push({
        path: "/XncfStore/shopping/application",
      });
    },
  },
};
</script>
<style lang="scss" scoped>
.box {
  margin: 5px;
  width: 100%;
  height: 100%;
  background-color: #f5f7fa;
  .nav {
    .shoptit {
      font-size: 24px;
      background-color: #ffffff;
      border-bottom: 1px solid #cfdbe2;
      color: #929292;
      font-weight: normal;
      padding: 15px;
    }
  }
  .mainLeft {
    background-color: #fff;
    .lefttit {
      display: flex;
      align-items: center;
      justify-content: flex-start;
      border-bottom: 1px solid #e4eaec;

      img {
        width: 130px;
      }
      .titInfo {
        display: flex;
        flex-direction: column;
        font-size: 14px;

        .infoTop {
          display: flex;
          align-items: center;
          justify-content: center;
          font-weight: bold;
          height: 100%;
          line-height: 30px;

          span {
            color: #ff902b;
          }
        }
        .infoBot {
          line-height: 30px;
          color: #909fa7;
        }
      }
    }
    .leftDet {
      display: flex;
      align-items: flex-start;
      justify-content: space-around;
      img {
        width: 50px;
        height: 50px;
      }
      .DetIt {
        display: flex;
        flex-direction: column;

        .infoItem {
          display: flex;
          align-items: center;
          justify-content: start;
          line-height: 38px;
          span {
            font-weight: bold;
          }
          p {
            color: #656565;
          }
        }
      }
    }
    .leftBot {
      padding: 30px;
      .tabsItem {
        display: flex;
        flex-direction: column;
        .tabsItem-th {
          display: flex;
          align-items: center;
          justify-content: flex-start;

          span {
            line-height: 20px;
            padding: 10px;
            width: 200px;
            height: 100%;
            white-space: nowrap;
          }
        }
        .explain {
          display: flex;
          flex-direction: column;
          span {
            padding: 10px 0;
          }
        }
        .comment {
          display: flex;
          align-items: center;
          justify-content: flex-start;
          font-size: 15px;

          span {
            padding: 10px;
          }

          .evaluate {
            color: #37bc9b;
          }
        }
        .commentInfo {
          display: flex;
          align-items: center;
          font-size: 15px;
          span {
            padding: 10px;
          }
        }
        .span {
          padding: 10px;
          color: gray;
          font-size: 15px;
        }
      }
    }
  }
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
      img {
        width: 80px;
      }
      span {
        color: #37bc9b;
      }
    }
  }
  .forItem {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 20px 25px;

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

  //   弹出框
  .dialogmain {
    display: flex;
    flex-direction: column;
    align-content: center;
    justify-content: center;

    .logo {
      font-size: 82px;
      text-align: center;
    }
    h3 {
      font-size: 30px;
      text-align: center;
    }
    .number {
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 16px;
      color: #797979;
      line-height: 40px;

      .greencolor {
        color: #37bc9b;
      }
    }
  }
  // 热门推荐
  .recommend {
    h4 {
      padding: 10px;
      background-color: #fff;
    }
    .forItem {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 20px 25px;

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
</style>