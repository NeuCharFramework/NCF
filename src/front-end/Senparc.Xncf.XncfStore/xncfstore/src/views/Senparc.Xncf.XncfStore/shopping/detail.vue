<template>
  <!-- 商品首页 /XncfStore/shopping/Index.vue -->
  <div class="box">
    <el-card>
      <h3 class="shoptit">应用详情-微微嗨-签到</h3>
    </el-card>
    <div class="yellowInfo">
      <i class="el-icon-warning"></i>
      <p>
        此应用尚未上架，其他用户无法访问此页面并订阅。您可以看到这个页面是因为您是这个应用的开发者，可以订阅后进行线上测试。测试完成之后不忘忘了提交正式接入申请哦
      </p>
    </div>
    <!-- xs<768  sm≥768px"  md≥992px  lg≥1200px xl≥1920px -->
    <!-- 详情和右侧推荐 -->
    <el-card>
      <el-row :gutter="20">
        <!-- 左边 -->
        <el-col :span="6" :xs="24" :sm="24" :md="24" :lg="18" :xl="18">
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
                    class="transImg"
                    style=""
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
                    <el-rate v-model="value1"></el-rate>
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
                    <del style="color: #f05050">￥599</del>
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
                    <p style="color: #37bc9b; font-weight: bold">0</p>
                    <i
                      class="el-icon-coin"
                      style="color: #fad732; font-weight: bold"
                    ></i>
                  </div>
                  <div class="infoItem">
                    <span> NeuChar Cell 账号：</span>
                    <!-- <p>微微嗨-签到</p> -->
                    <el-select v-model="value" placeholder="请选择">
                      <el-option
                        v-for="item in options"
                        :key="item.value"
                        :label="item.value"
                        :value="item.value"
                      >
                      </el-option>
                    </el-select>
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
                      <el-divider></el-divider>
                      <div class="span">暂时没有更多评论</div>
                    </div>
                  </el-tab-pane>
                  <el-tab-pane label="开发者信息">
                    <div class="tabsItem">
                      <el-divider></el-divider>
                      <div class="tabsItem-th">
                        <span>开发者名称</span>
                        <span>苏州盛派网络科技有限公司</span>
                      </div>
                      <el-divider></el-divider>
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
                      <el-divider></el-divider>
                      <div class="tabsItem-th">
                        <span>类别</span>
                        <span>公司</span>
                      </div>
                      <el-divider></el-divider>
                      <div class="tabsItem-th">
                        <span>地区</span>
                        <span>江苏省 苏州市 吴中区</span>
                      </div>
                      <el-divider></el-divider>
                      <div class="tabsItem-th">
                        <span>等级</span>
                        <span>0</span>
                      </div>
                      <el-divider></el-divider>
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
        <el-col :span="6" :xs="24" :sm="24" :md="24" :lg="6" :xl="6">
          <div class="grid-content bg-purple">
            <el-row :gutter="20">
              <div style="display: flex; flex-direction: column">
                <el-col :span="6" :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
                  <div class="otherApp">
                    <img src="./imgs/企业微信截图_20221222111152.png" alt="" />
                    <h4>开发者的其他应用</h4>
                  </div>
                </el-col>
                <div class="appArea">
                  <div v-for="it in 4" :key="it">
                    <el-col
                      :span="6"
                      :xs="24"
                      :sm="24"
                      :md="24"
                      :lg="24"
                      :xl="24"
                    >
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
                </div>
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
                  <el-col :span="6" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
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
      // 星级评价
      value1: null,
      colors: ["#99A9BF", "#F7BA2A", "#FF9900"],
      // 详情的名称
      options: [
        {
          value: "NeuChar Group222",
        },
        {
          value: "NeuChar Group35",
        },
        {
          value: "NeuChar Group0",
        },
      ],
      value: "NeuChar Group222",
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
        path: "/Senparc.Xncf.XncfStore/shopping/application",
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
  // background-color: #f5f7fa;
  border: 1px solid transparent;

  ::v-deep .el-card {
    margin: 10px;
  }

  // 滚动条
  ::-webkit-scrollbar {
    width: 11px;
    height: 11px;
  }
  ::-webkit-scrollbar-thumb {
    border-radius: 10px;
    background-color: #37bc9b;
    border: 2px solid transparent;
    background-clip: padding-box;
  }

  .shoptit {
    font-size: 24px;
    background-color: #ffffff;
    color: #929292;
    font-weight: normal;
    // padding: 30px 15px;
    // margin: 20px 0px;
  }
  .yellowInfo {
    width: 98%;
    margin: 10px;
    padding: 15px 20px;
    background-color: white;
    border: rgb(255, 144, 43) solid 1px;
    color: rgb(255, 144, 43);
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    p {
      margin-left: 5px;
      font-size: 14px;
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

      .transImg {
        width: 180px;
        height: 180px;
        border-radius: 10px;
        -webkit-animation-name: move; /*关键帧名称*/
        -webkit-animation-timing-function: ease-in-out; /*动画的速度曲线*/
        -webkit-animation-iteration-count: infinite; /*动画播放的次数*/
        -webkit-animation-duration: 1s; /*动画所花费的时间*/
        animation-iteration-count: 1; /*动画只执行一次*/
        /* Safari and Chrome */
        -webkit-animation: move 3s;
        -webkit-animation-iteration-count: 1;
      }

      @keyframes move {
        0% {
          transform: scale(0.5); /*开始为原始大小*/
        }
        100% {
          transform: scale(1); /*放大1.1倍*/
        }
      }
      img {
        width: 40px;
        height: 40px;
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
            font-size: 14px;
          }
          p {
            color: #656565;
            font-size: 12px;
          }
          del {
            font-size: 12px;
          }
          ::v-deep .el-button {
            padding: 5px 10px;
          }
          ::v-deep .el-select {
            padding: 5px;
          }
          ::v-deep .el-input--medium {
            font-size: 12px;
          }
        }
      }
    }
    .leftBot {
      padding: 30px;
      .tabsItem {
        display: flex;
        flex-direction: column;
        font-size: 14px;

        ::v-deep .el-divider--horizontal {
          margin: 0px 0;
        }
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
          .user {
            font-size: 14px;
          }
          .evaluate {
            color: #37bc9b;
            font-size: 14px;
          }
          .timer {
            font-size: 12px;
          }
        }
        .commentInfo {
          display: flex;
          align-items: center;
          font-size: 14px;
          span {
            padding: 10px;
            color: gray;
          }
        }
        .span {
          padding: 10px;
          color: gray;
          font-size: 12px;
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
  .otherApp {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    padding: 10px 0;
    img {
      width: 50px;
    }
  }
  .appArea {
    height: 900px;
    overflow: hidden;
    overflow-y: auto;
    scrollbar-width: none; /* firefox */
    -ms-overflow-style: none; /* IE 10+ */

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
      flex-direction: column;
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