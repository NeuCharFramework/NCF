<template>
  <!-- 个人应用-全部 -->
  <div class="myapplication">
    <!-- 标题 -->
    <el-card>
      <h3 class="shoptit">应用形式-微信API-SWAG文档</h3>
    </el-card>
    <!-- 推送素材 -->
    <el-card>
      <div class="notice">
        <span>
          <i class="el-icon-lollipop"></i>
        </span>
        <span>轻</span>
        <p @click="dialogVisible = true">推送素材</p>
        <span>一起用当前应用</span>
      </div>
    </el-card>
    <div class="detail">
      <el-card>
        <div class="appdetail">
          <!-- <div class="appdetail-left">
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
                <span v-if="switchState == true">已开放</span>
                <span v-else>已关闭</span>
                
                <el-button
                  style="margin-left: 5px"
                  type="primary"
                  plain
                  @click="changeSwitch"
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
                <el-button
                  v-if="appCodeState == false"
                  style="margin-left: 5px"
                  type="primary"
                  plain
                  @click="getAppCode"
                  >获取AppCode</el-button
                >
                <span v-else>1522245454</span>
              </el-descriptions-item>
              <el-descriptions-item>
                <template slot="label">
                  <i class="el-icon-tickets"></i>
                  进入会话关键字
                </template>
                enter
              
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
          </div> -->
          <el-table :data="tableData" border style="width: 100%">
            <el-table-column prop="datename" label="NeuChar Cell" width="250">
              <template slot-scope="scope">
                <div class="opera">
                  <div class="clickArea">{{ scope.row.datename }}</div>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="appname" label="应用">
              <template slot-scope="scope">
                <div class="opera" v-if="scope.row.detail">
                  <table>
                    <thead>
                      <tr>
                        <td>图标</td>
                        <td>名称</td>
                        <td>到期时间</td>
                        <td>状态</td>
                        <td>操作</td>
                      </tr>
                    </thead>
                    <tbody>
                      <tr>
                        <td>img</td>
                        <td>微信API Swagger文档</td>
                        <td>2022/2/52</td>
                        <td>关闭</td>
                        <td>
                          <el-button type="primary" plain>查看详情</el-button>
                          <el-button type="success" plain>
                            打开高级设置
                          </el-button>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                <div class="opera" v-else>
                  <span>{{ scope.row.appname.data }}</span>
                  <div class="clickArea">{{ scope.row.appname.operetion }}</div>
                </div>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </el-card>
    </div>
    <!-- 公司信息-页尾 -->
    <footerinfo />
    <!-- 推送素材 -->
    <el-dialog
      title="请选择需要推送的NeuChar Endings"
      :visible.sync="dialogVisible"
      width="60%"
      :before-close="handleClose"
    >
      <el-card>
        <div class="pushState">
          <div class="dialogTit">
            <el-checkbox v-model="checked">
              存入时光机，即可备份当前版本
            </el-checkbox>
          </div>
          <el-divider></el-divider>
          <div class="dialogTit">NeuChar Group2022</div>
          <el-divider></el-divider>
          <el-table
            ref="multipleTable"
            :data="dialogTable"
            tooltip-effect="dark"
            style="width: 100%"
          >
            <!-- <template slot-scope="scope">{{ scope.row.date }}</template> -->
            <el-table-column prop="name" label="NeuChar Endings">
            </el-table-column>
            <el-table-column
              prop="sqstate"
              label="授权状态"
              show-overflow-tooltip
            >
              <template slot-scope="scope">
                <div style="color: #ff902b">{{ scope.row.sqstate }}</div>
              </template>
            </el-table-column>
            <el-table-column prop="state" label="状态" show-overflow-tooltip>
            </el-table-column>
            <el-table-column type="selection"> </el-table-column>
          </el-table>
        </div>
      </el-card>
      <span slot="footer" class="dialog-footer">
        <el-button @click="dialogVisible = false"> 关 闭 </el-button>
        <el-button type="primary" @click="dialogVisible = false">
          推 送
        </el-button>
      </span>
    </el-dialog>
    <!-- 我要反馈 -->
    <feedback />
  </div>
</template>
  
<script>
import feedback from "@/components/Feedback/index";
import footerinfo from "@/components/FooterInfo/index.vue";
export default {
  components: {
    feedback,
    footerinfo,
  },
  data() {
    return {
      tableData: [
        {
          detail: true,
          datename: "NeuChar Group111",
          appname: {
            data: "已订阅应用",
            operetion: "逛商店",
          },
        },
        {
          detail: false,
          datename: "NeuChar Group222",
          appname: {
            data: "尚未订阅应用",
            operetion: "逛商店",
          },
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
      size: "",
      // dialog开关状态
      dialogVisible: false,
      // 是否存入时光机
      checked: true,
      // 弹出层表单
      dialogTable: [
        {
          name: "王小虎",
          sqstate: "2016-05-03",
          state: "上海",
        },
      ],
      // 开关状态
      switchState: true,
      //是否获取appcode
      appCodeState: false,
    };
  },
  methods: {
    resetForm(formName) {
      this.$refs[formName].resetFields();
    },
    // dialog关闭
    handleClose(done) {
      done();
    },
    // 改变开关状态
    changeSwitch() {
      this.switchState = !this.switchState;
    },
    // 获取AppCode
    getAppCode() {
      this.appCodeState = true;
    },
  },
};
</script>
<style lang="scss" scoped>
.myapplication {
  margin: 5px;
  width: 100%;
  height: 100%;
  ::v-deep .el-card {
    margin: 10px 0;
  }
  .shoptit {
    font-size: 24px;
    background-color: #ffffff;
    border-bottom: 1px solid #cfdbe2;
    color: #929292;
    font-weight: normal;
    padding: 15px;
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
      cursor: pointer;
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
      margin: 20px;

      .opera {
        font-size: 14px;
        span {
          color: #656565;
        }
        .clickArea {
          color: #5d9cec;
          cursor: pointer;
        }
        table,
        td {
          border: 1px solid #eee;
          background-color: #f5f7fa;
          border-collapse: collapse;
          padding: 8px 25px;
        }
        ::v-deep .el-button {
          padding: 5px;
        }
      }
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

  .pushState {
    ::v-deep .el-divider--horizontal {
      margin: 0 !important;
    }
    .dialogTit {
      padding: 15px;
      color: #37bc9b;
    }
  }
}
</style>
  <style scoped>
.myapplication >>> .el-card__body {
  padding: 0px;
  /* margin-bottom: 10px; */
}
.appdetail-right >>> .el-button--primary {
  padding: 5px;
}
</style>