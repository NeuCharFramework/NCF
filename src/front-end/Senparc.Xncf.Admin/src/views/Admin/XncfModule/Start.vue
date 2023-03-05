<template>
  <!-- 扩展 -->
  <div class="extend">
    <!-- 顶部 -->
    <el-container v-if="data.xncfModule">
      <el-header class="module-header">
        <span class="start-title">
          <i :class="data.xncfRegister.icon"></i>{{ data.xncfModule.menuName }}
          <span class="module-header-v"
            >模块 {{ data.xncfModule.version }}</span
          ></span
        >
        <span class="start-delete">
          <el-popconfirm
            placement="top"
            title="确认删除此模块吗？"
            @confirm="handleDelete()"
          >
            <el-button
              size="mini"
              type="danger"
              slot="reference"
              v-if="data.xncfModule.state === 1"
              >删除</el-button
            >
          </el-popconfirm>
        </span>
      </el-header>
      <el-container>
        <!-- 发现 -->
        <el-aside class="module-aside" style="margin-top: 10px">
          <div class="start-des">
            <ul>
              <!-- 模块信息 xncfModule -->
              <li>
                <span class="start-green"
                  ><i class="fa fa-map-marker fa-cubes"></i
                  >{{ data.xncfModule.name }}</span
                >
              </li>
              <li>
                <i class="fa fa-map-marker fa-code-fork"></i>v{{
                  data.xncfModule.version
                }}
              </li>
              <li v-if="data.xncfModule.version !== data.xncfRegister.version">
                发现新版本:v{{ data.xncfRegister.version }}
                <el-button size="mini" @click="updataVersion">
                  <em class="fa fa-gift"></em>立即更新
                </el-button>
              </li>
              <li>
                <el-tooltip
                  class="item"
                  effect="dark"
                  :content="data.xncfModule.uid"
                  placement="top-start"
                >
                  <span>
                    <i class="fa fa-map-marker fa-shield"></i>
                    唯一编号
                  </span>
                </el-tooltip>
              </li>
              <li>
                <i class="fa fa-map-marker puzzle-piece"></i>特性：
                <el-tooltip
                  v-for="item in data.xncfRegister.interfaces"
                  :key="item"
                  class="item"
                  effect="dark"
                  placement="top"
                >
                  <div slot="content">
                    {{ tooltip[item] }}
                    <span
                      v-if="item === 'IXncfRazorRuntimeCompilation'"
                      @click="thread.visible = true"
                    >
                      <el-button size="mini"> 查看 </el-button>
                    </span>
                  </div>
                  <!-- 网页 -->
                  <i
                    v-if="item === 'IAreaRegister'"
                    class="fa fa-map-marker fa-external-link"
                  ></i>
                  <!-- 数据库 -->
                  <i
                    v-if="item === 'IXncfDatabase'"
                    class="fa fa-map-marker fa-database"
                  ></i>
                  <!-- 中间件 -->
                  <i
                    v-if="item === 'IXncfMiddleware'"
                    class="fa fa-map-marker fa-plug"
                  ></i>
                  <!-- 线程 -->
                  <i
                    v-if="item === 'IXncfRazorRuntimeCompilation'"
                    class="fa fa-map-marker fa-retweet"
                  ></i>
                </el-tooltip>
                <template v-if="data.xncfRegister.functionCount > 0">
                  <el-tooltip
                    class="item"
                    effect="dark"
                    content="执行方法"
                    placement="top-start"
                  >
                    <i class="fa fa-map-marker fa-rocket"
                      >({{ data.xncfRegister.functionCount }})</i
                    >
                  </el-tooltip>
                </template>
              </li>
              <li>
                <i class="fa fa-map-marker fa-sliders"></i
                ><span
                  :class="[
                    data.xncfModule.state === 1 ? 'start-green' : 'start-gray',
                  ]"
                  style="margin-right: 10px"
                  >{{ xNcfModules_State[data.xncfModule.state] }}</span
                >
                <template
                  v-if="
                    data.xncfModule.state === 3 ||
                    data.xncfModule.state === 0 ||
                    data.xncfModule.state === 2
                  "
                >
                  <el-button
                    size="mini"
                    :type="data.xncfModule.state === 0 ? 'success' : 'warning'"
                    @click="updataState('1')"
                  >
                    <em class="glyphicon glyphicon-flash"></em> 开启
                  </el-button>
                  <p class="red">
                    <i class="fa fa-exclamation-triangle"></i
                    >未开启的模块无法执行功能
                  </p>
                </template>
                <template v-if="data.xncfModule.state === 1">
                  <el-button
                    size="mini"
                    type="danger"
                    @click="updataState('0')"
                  >
                    <em class="glyphicon glyphicon-off"></em> 关闭
                  </el-button>
                </template>
              </li>
              <li>
                <i class="fa fa-map-marker fa-newspaper-o"></i>介绍：
                <p>{{ data.xncfModule.description }}</p>
              </li>
            </ul>
          </div>
          <!-- 更新记录-刚开始的样式 -->
          <!-- <div class="start-log">
            <el-collapse>
              <el-collapse-item title="更新记录" name="1">
                <el-table
                  :data="data.xncfModule.updateLog"
                  stripe
                  style="width: 100%"
                >
                  <el-table-column label="#" width="30">
                    <template slot-scope="scope">
                      {{ scope.$index + 1 }}
                    </template>
                  </el-table-column>
                  <el-table-column label="更新记录">
                    <template slot-scope="scope">
                      {{ scope.row }}
                    </template>
                  </el-table-column>
                </el-table>
              </el-collapse-item>
            </el-collapse>
          </div> -->
        </el-aside>
        <!-- <el-main>
          <template v-if="!data.mustUpdate">
            <template
              v-if="
                data.xncfRegister.interfaces.includes('IAreaRegister') &&
                data.xncfRegister.areaHomeUrl !== null
              "
            >
              <el-card class="box-card">
                <div slot="header" class="clearfix">
                  <span>进入 {{ data.xncfRegister.menuName }} 的首页</span>
                  <a
                    @click="
                      openUrl(
                        data.xncfRegister.areaHomeUrl,
                        data.xncfModule.state
                      )
                    "
                    :class="[
                      data.xncfModule.state === 1
                        ? 'box-card-action'
                        : 'box-card-action2',
                      'box-card-action',
                    ]"
                    >进入首页</a
                  >
                  <p class="box-card-des">
                    地址：<a :href="data.xncfRegister.areaHomeUrl">{{
                      data.xncfRegister.areaHomeUrl
                    }}</a>
                  </p>
                </div>
                <template>
                  <div class="item-container">
                    <el-collapse>
                      <el-collapse-item title="页面透视" name="2">
                        <el-menu
                          default-active="2"
                          class="el-menu-vertical-demo"
                          background-color="#304156"
                          text-color="#bfcbd9"
                          width="300px"
                          active-text-color="#409eff"
                        >
                          <el-menu-item
                            v-for="(item, index) in data.xncfRegister
                              .areaPageMenuItems"
                            :key="index"
                          >
                            <router-link :to="item.url">
                              <div>
                                <i class="el-icon-location"></i>
                                <span slot="title">
                                  {{ item.name }}
                                </span>
                              </div>
                            </router-link>
                          </el-menu-item>
                        </el-menu>
                      </el-collapse-item>
                    </el-collapse>
                  </div>
                  <p class="box-card-tip">
                    说明：当前模块配备了可互动操作的UI界面，因此您可以看到这个入口。
                  </p>
                </template>
              </el-card>
            </template>
          </template>
          <el-card
            class="box-card"
            v-for="item in data.functionParameterInfoCollection"
            :key="item.key.name"
          >
            <div slot="header" class="clearfix">
              <span><i class="fa fa-terminal"></i> {{ item.key.name }}</span>
              <span
                type="text"
                :class="[
                  data.xncfModule.state === 1
                    ? 'box-card-action'
                    : 'box-card-action2',
                ]"
                @click="openRun(item, data.xncfModule.state)"
                >执行</span
              >
              <p class="box-card-des">{{ item.key.description }}</p>
            </div>
            <template>
              <div class="item-container">
                <el-collapse>
                  <el-collapse-item title="参数透视" name="2">
                    <el-table
                      v-if="item.value.length > 0"
                      :data="item.value"
                      style="width: 100%"
                    >
                      <el-table-column label="#" width="30">
                        <template slot-scope="scope">
                          {{ scope.$index + 1 }}
                        </template>
                      </el-table-column>
                      <el-table-column label="名称" width="200">
                        <template slot-scope="scope">
                          {{ scope.row.title }}
                        </template>
                      </el-table-column>
                      <el-table-column label="说明">
                        <template slot-scope="scope">
                          {{ scope.row.description }}
                        </template>
                      </el-table-column>
                      <el-table-column label="类型" width="150">
                        <template slot-scope="scope">
                          {{
                            state[scope.row.systemType] || scope.row.systemType
                          }}
                        </template>
                      </el-table-column>
                      <el-table-column label="必须" width="150">
                        <template slot-scope="scope">
                          {{ scope.row.isRequired ? "是" : "否" }}
                        </template>
                      </el-table-column>
                    </el-table>
                    <div v-else class="no-content">无需填写数据</div>
                  </el-collapse-item>
                </el-collapse>
              </div>
            </template>
          </el-card>
        </el-main> -->
        <!-- 更新记录,错误方案，放置最右侧 -->
        <div style="flex: 1; margin: 10px">
          <el-tabs v-model="activeName" type="card" @tab-click="handleClick">
            <el-tab-pane label="执行方法" name="second">
              <el-main>
                <template v-if="!data.mustUpdate">
                  <template
                    v-if="
                      data.xncfRegister.interfaces.includes('IAreaRegister') &&
                      data.xncfRegister.areaHomeUrl !== null
                    "
                  >
                    <el-card class="box-card">
                      <div slot="header" class="clearfix">
                        <span
                          >进入 {{ data.xncfRegister.menuName }} 的首页</span
                        >
                        <a
                          @click="
                            openUrl(
                              data.xncfRegister.areaHomeUrl,
                              data.xncfModule.state
                            )
                          "
                          :class="[
                            data.xncfModule.state === 1
                              ? 'box-card-action'
                              : 'box-card-action2',
                            'box-card-action',
                          ]"
                          >进入首页</a
                        >
                        <p class="box-card-des">
                          地址：<a :href="data.xncfRegister.areaHomeUrl">{{
                            data.xncfRegister.areaHomeUrl
                          }}</a>
                        </p>
                      </div>
                      <template>
                        <div class="item-container">
                          <el-collapse>
                            <el-collapse-item title="页面透视" name="2">
                              <el-menu
                                default-active="2"
                                class="el-menu-vertical-demo"
                                background-color="#304156"
                                text-color="#bfcbd9"
                                width="300px"
                                active-text-color="#409eff"
                              >
                                <el-menu-item
                                  v-for="(item, index) in data.xncfRegister
                                    .areaPageMenuItems"
                                  :key="index"
                                >
                                  <router-link :to="item.url">
                                    <div>
                                      <i class="el-icon-location"></i>
                                      <span slot="title">
                                        {{ item.name }}
                                      </span>
                                    </div>
                                  </router-link>
                                </el-menu-item>
                              </el-menu>
                            </el-collapse-item>
                          </el-collapse>
                        </div>
                        <p class="box-card-tip">
                          说明：当前模块配备了可互动操作的UI界面，因此您可以看到这个入口。
                        </p>
                      </template>
                    </el-card>
                  </template>
                </template>
                <el-card
                  class="box-card"
                  v-for="item in data.functionParameterInfoCollection"
                  :key="item.key.name"
                >
                  <div slot="header" class="clearfix">
                    <span
                      ><i class="fa fa-terminal"></i> {{ item.key.name }}</span
                    >
                    <span
                      type="text"
                      :class="[
                        data.xncfModule.state === 1
                          ? 'box-card-action'
                          : 'box-card-action2',
                      ]"
                      @click="openRun(item, data.xncfModule.state)"
                      >执行</span
                    >
                    <p class="box-card-des">{{ item.key.description }}</p>
                  </div>
                  <template>
                    <div class="item-container">
                      <el-collapse>
                        <el-collapse-item title="参数透视" name="2">
                          <el-table
                            v-if="item.value.length > 0"
                            :data="item.value"
                            style="width: 100%"
                          >
                            <el-table-column label="#" width="30">
                              <template slot-scope="scope">
                                {{ scope.$index + 1 }}
                              </template>
                            </el-table-column>
                            <el-table-column label="名称" width="200">
                              <template slot-scope="scope">
                                {{ scope.row.title }}
                              </template>
                            </el-table-column>
                            <el-table-column label="说明">
                              <template slot-scope="scope">
                                {{ scope.row.description }}
                              </template>
                            </el-table-column>
                            <el-table-column label="类型" width="150">
                              <template slot-scope="scope">
                                {{
                                  state[scope.row.systemType] ||
                                  scope.row.systemType
                                }}
                              </template>
                            </el-table-column>
                            <el-table-column label="必须" width="150">
                              <template slot-scope="scope">
                                {{ scope.row.isRequired ? "是" : "否" }}
                              </template>
                            </el-table-column>
                          </el-table>
                          <div v-else class="no-content">无需填写数据</div>
                        </el-collapse-item>
                      </el-collapse>
                    </div>
                  </template>
                </el-card>
              </el-main>
            </el-tab-pane>
            <el-tab-pane label="更新记录" name="first">
              <div class="start-log">
                <el-collapse>
                  <el-collapse-item title="更新记录" name="1">
                    <el-table
                      :data="data.xncfModule.updateLog"
                      stripe
                      style="width: 100%"
                    >
                      <el-table-column label="#" width="30">
                        <template slot-scope="scope">
                          {{ scope.$index + 1 }}
                          <!-- {{ scope.$index }} -->
                        </template>
                      </el-table-column>
                      <el-table-column label="更新记录">
                        <template slot-scope="scope">
                          {{ scope.row }}
                        </template>
                      </el-table-column>
                    </el-table>
                  </el-collapse-item>
                </el-collapse>
              </div>
            </el-tab-pane>
          </el-tabs>
        </div>
      </el-container>
    </el-container>
    <!-- 执行弹窗 -->
    <el-dialog
      width="60%"
      v-if="run.data.key"
      :title="run.data.key.name"
      :visible.sync="run.visible"
      class="run-dialog"
      @close="runVisibleClose"
      :close-on-click-modal="false"
    >
      <h4><i class="fa fa-newspaper-o"></i>{{ run.data.key.description }}</h4>
      <el-form :label-position="'left'">
        <el-form-item
          v-for="item in run.data.value"
          :key="item.name"
          :label="item.title || item.name"
          label-width="130px"
          :required="item.isRequired"
        >
          <el-input
            v-if="item.parameterType === 0"
            v-model="runData[item.name].value"
            maxlength="500"
          ></el-input>
          <el-select
            v-if="item.parameterType === 1"
            v-model="runData[item.name].value"
          >
            <template v-if="item.selectionList.items">
              <el-option
                v-for="selectionItem in item.selectionList.items"
                :key="selectionItem.text"
                :label="selectionItem.text"
                :value="selectionItem.value"
              ></el-option>
            </template>
          </el-select>
          <template v-if="item.parameterType === 2 && item.selectionList.items">
            <el-checkbox-group v-model="runData[item.name].value">
              <template v-for="selectionItem in item.selectionList.items">
                <el-checkbox
                  :key="selectionItem.value"
                  :label="selectionItem.value"
                >
                  {{ selectionItem.text }}
                  <template v-if="selectionItem.note !== null">
                    <span class="icon-note">
                      <i class="fa fa-info-circle"></i>
                      {{ selectionItem.note }}</span
                    >
                  </template>
                </el-checkbox>
              </template>
            </el-checkbox-group>
          </template>
          <template v-if="item.description !== null">
            <p><i class="fa fa-info-circle"></i> {{ item.description }}</p>
          </template>
        </el-form-item>
      </el-form>
      <div slot="footer" class="dialog-footer">
        <el-button @click="cancelRun">取 消</el-button>
        <el-button type="primary" @click="handleRun" :loading="isExecut"
          >执 行</el-button
        >
      </div>
    </el-dialog>
    <!-- 运行结果 -->
    <el-dialog
      :title="runResult.tit"
      :visible.sync="runResult.visible"
      :close-on-click-modal="false"
      width="30%"
    >
      <p v-html="runResult.tip"></p>
      <p v-html="runResult.msg"></p>
      <span slot="footer" class="dialog-footer">
        <a
          :href="
            'https://localhost:44311/Admin/XncfModule/Start/?handler=Log&tempId=' +
            runResult.tempId
          "
          v-if="runResult.hasLog"
          style="display: block"
          ><i class="fa fa-cloud-download"></i> 下载日志（5分钟内1次有效）</a
        >
        <el-button type="primary" @click="closeDialog">
          <!-- <el-button type="primary" @click="runResult.visible = false"> -->
          关 闭
        </el-button>
      </span>
    </el-dialog>
    <!-- 查看线程 -->
    <el-dialog title="查看线程" :visible.sync="thread.visible" width="60%">
      <div class="thread-contenet">
        <el-table :data="data.registeredThreadInfo" style="width: 100%">
          <el-table-column label="名称" width="180">
            <template slot-scope="scope">
              {{ scope.row.key.name }}
            </template>
          </el-table-column>
          <el-table-column label="活动状态/后台" width="180">
            <template slot-scope="scope">
              {{ scope.row.value.isAlive }}/
              {{ scope.row.value.isAlive ? value.isBackground : false }}
            </template>
          </el-table-column>
          <el-table-column label="线程状态" width="180">
            <template slot-scope="scope">
              {{
                scope.row.value.isAlive
                  ? formaTableTime(scope.row.value.threadState)
                  : "-"
              }}
            </template>
          </el-table-column>
          <el-table-column label="最近故事">
            <template slot-scope="scope">
              <span v-html="scope.row.key.storyHtml"></span>
            </template>
          </el-table-column>
        </el-table>
      </div>
      <span slot="footer" class="dialog-footer">
        <el-button type="primary" @click="thread.visible = false"
          >关 闭</el-button
        >
      </span>
    </el-dialog>
    <!-- 取消提示 -->
    <el-dialog
      title="提示"
      :show-close="false"
      :visible.sync="dialogVisible"
      width="30%"
      :close-on-click-modal="false"
    >
      <span
        >您执行的操作，后台正在运行，现在退出后台的运行并不会停止。确定退出吗？</span
      >
      <span slot="footer" class="dialog-footer">
        <el-button @click="dialogVisible = false">取 消</el-button>
        <el-button type="primary" @click="closeRunHit">确 定</el-button>
      </span>
    </el-dialog>
  </div>
</template>

<script>
import {
  getItemModule,
  deleteModule,
  changeModuleState,
  moduleRunFunction,
  moduleInstallXncf,
} from "@/api/module";
import { isHaveToken } from "@/utils/auth";
export default {
  data() {
    return {
      tabPosition: "left", //tab页切换的位置
      data: {}, // 数据
      tooltip: {
        IAreaRegister: "网页",
        IXncfDatabase: "数据库",
        IXncfMiddleware: "中间件",
        IXncfRazorRuntimeCompilation: "线程",
      },
      state: {
        String: "文本",
        Int32: "数字",
        Int64: "数字",
        DateTime: "日期",
        "String[]": "选项",
      },
      xNcfModules_State: {
        0: "关闭",
        1: "开放",
        2: "新增待审核",
        3: "更新待审核",
      },
      // 执行弹窗
      run: {
        data: {},
        visible: false,
      },
      runData: {
        // 绑定数据
      },
      runResult: {
        visible: false,
        tit: "",
        tip: "",
        msg: "",
        tempId: "",
        hasLog: false,
      },
      //查看线程
      thread: {
        visible: false,
      },
      uid: null,
      isExecut: false, // 是否正在执行
      dialogVisible: false, // 正在执行取消提示
      activeName: "second", //切换更新记录和执行方法
    };
  },
  created() {
    isHaveToken();
    // this.getList();
  },
  mounted() {
    const uid = this.$route.path.split("=")[1];
    if (!uid) return;
    this.uid = uid;
    // console.log(uid);
    this.getList();
  },
  methods: {
    async getList() {
      // 获取uid
      // const uid = "00000000-0000-0001-0001-000000000001";
      // 获取详情
      const res = await getItemModule(this.uid);
      // console.log("获取详情", res.data.xncfModule.updateLog);
      // console.log(JSON.parse(JSON.stringify(res)))
      if (res.data) {
        this.data = res.data || {};
        this.data.xncfRegister.interfaces =
          this.data.xncfRegister.interfaces.splice(1);
        this.data.xncfModule.updateLog =
          this.data.xncfModule.updateLog.split("\r");
        // console.log("查看内容", this.data.xncfModule.updateLog);
        this.data.xncfModule.updateLog.pop();
        // \r or \n
        // window.document.title = this.data.xncfModule.menuName
        return;
      }
      // 获取数据失败
      this.$message.error("获取详情失败");
    },
    // 更新版本
    async updataVersion() {
      // window.sessionStorage.setItem('setNavMenuActive', '模块管理')
      await moduleInstallXncf(this.uid)
        .then((res) => {
          // console.log("ok", res);
          if (res.success) {
            // 安装成功重新获取详情
            this.getList();
          }
        })
        .catch(() => {
          this.$message.error("更新版本失败");
        });
    },

    // 打开首页
    openUrl(url, flag) {
      // 关闭状态返回
      flag = flag + "";
      if (flag !== "1") {
        this.$notify({
          title: "提示",
          message: "请开启后执行",
          type: "warning",
        });
        return;
      }
      // console.log('进入首页',url);
      window.location.href = url;
    },
    closeDialog() {
      this.runResult.visible = false; //关闭结果
      this.run.visible = false; //关闭一层弹框
    },
    // 执行弹框关闭事件
    runVisibleClose() {
      this.runResult.visible = false; //关闭结果
      this.run.visible = false; //关闭一层弹框
      this.isExecut = false;
    },
    // 打开执行
    openRun(item, flag) {
      // console.log('click', item, flag)
      // 关闭状态返回
      flag = flag + "";
      if (flag !== "1") {
        this.$notify({
          title: "提示",
          message: "请开启后执行",
          type: "warning",
        });
        return;
      }
      this.run.data = item;
      this.runData = {};
      this.run.data.value.map((res) => {
        // 动态model绑定生成
        // 默认选择赋值
        // 多选
        if (res.parameterType === 2 && res.selectionList.items) {
          this.runData[res.name] = {};
          this.runData[res.name].value = [];
          this.runData[res.name].item = res;
          res.selectionList.items.map((ele) => {
            if (ele.defaultSelected) {
              this.runData[res.name].value.push(ele.value);
            }
          });
        }
        // 下拉框value
        if (res.parameterType === 1 && res.selectionList.items) {
          this.runData[res.name] = {};
          this.runData[res.name].value = "";
          this.runData[res.name].item = res;
          res.selectionList.items.map((ele) => {
            if (ele.defaultSelected) {
              this.runData[res.name].value = ele.value;
            }
          });
          // 如果没有默认给第一个
          if (this.runData[res.name].value.length === 0) {
            this.runData[res.name].value = res.selectionList.items[0].value;
          }
        }
        // 输入框
        if (res.parameterType === 0) {
          this.runData[res.name] = {};
          this.runData[res.name].item = res;
          this.runData[res.name].value = res.value || "";
        }
      });
      this.runData = Object.assign({}, this.runData);
      //  this.runData数组结构
      //在接口传输时，将下拉单选转成数组
      //{
      //   // parameterType === 2 多选
      //    Modules: {
      //        item: {},
      //        value: []
      //    },
      //   // parameterType === 1 下拉单选
      //    ReferenceType: {
      //        item: {},
      //        value: []
      //    },
      //   // parameterType === 0 input
      //    SourcePath: {
      //        item: {},
      //        value: ''
      //    }
      //};
      this.run.visible = true;
    },
    // 取消执行
    cancelRun() {
      if (this.isExecut) {
        this.dialogVisible = true;
      } else {
        this.runResult.visible = false; //关闭结果
        this.run.visible = false; //关闭一层弹框
      }
    },
    // 确定取消执行吗
    closeRunHit() {
      this.dialogVisible = false;
      this.runResult.visible = false; //关闭结果
      this.run.visible = false; //关闭一层弹框
    },
    // 执行
    async handleRun() {
      if (this.isExecut) return;
      this.isExecut = true;
      // 物理路径校验
      if (
        this.runData.hasOwnProperty("SourcePath") &&
        this.runData.SourcePath.length < 1
      ) {
        this.$notify({
          title: "警告",
          message: "请填写源码物理路径",
          type: "warning",
        });
        this.isExecut = false;
        return;
      }
      // 关闭执行弹窗
      // this.run.visible = false;
      let xncfFunctionParams = {};
      // console.log('执行',this.runData);
      for (var i in this.runData) {
        // 多选
        if (this.runData[i].item.parameterType === 2) {
          if (
            this.runData[i].item.isRequired &&
            this.runData[i].value.length === 0
          ) {
            this.$notify({
              title: "提示",
              message:
                this.runData[i].item.title ||
                this.runData[i].item.name + "  为必选项",
              type: "warning",
            });
            this.isExecut = false;
            return;
          } else {
            xncfFunctionParams[i] = {};
            xncfFunctionParams[i].SelectedValues = [];
            xncfFunctionParams[i].SelectedValues = this.runData[i].value;
          }
        }
        // 下拉框value为字符串，但接口要数组
        if (this.runData[i].item.parameterType === 1) {
          if (
            this.runData[i].item.isRequired &&
            this.runData[i].value.length === 0
          ) {
            this.$notify({
              title: "提示",
              message:
                this.runData[i].item.title ||
                this.runData[i].item.name + "  为必填项",
              type: "warning",
            });
            this.isExecut = false;
            return;
          } else {
            xncfFunctionParams[i] = {};
            xncfFunctionParams[i].SelectedValues = [];
            xncfFunctionParams[i].SelectedValues[0] = this.runData[i].value;
          }
        }
        // 输入框
        if (this.runData[i].item.parameterType === 0) {
          if (
            this.runData[i].item.isRequired &&
            this.runData[i].value.length === 0
          ) {
            this.$notify({
              title: "提示",
              message:
                this.runData[i].item.title ||
                this.runData[i].item.name + "  为必填项",
              type: "warning",
            });
            this.isExecut = false;
            return;
          } else {
            xncfFunctionParams[i] = this.runData[i].value;
          }
        }
      }
      const data = {
        xncfUid: this.data.xncfModule.uid,
        xncfFunctionName: this.run.data.key.name || "",
        xncfFunctionParams: JSON.stringify(xncfFunctionParams),
      };
      const res = await moduleRunFunction(data);
      console.log("执行 结果", res);

      this.runResult.tempId = res.data.tempId;
      console.log("执行结果2", this.runResult);
      // console.log('res.data.success',res.data.success);
      // console.log('res.data.msg',res.data.msg);//提示信息
      // console.log(
      //   'res.data.msg.indexOf("http://")111',
      //   res.data.msg.indexOf('http://')
      // )
      // console.log(
      //   'res.data.msg.indexOf("https://") !== -1 111',
      //   res.data.msg.indexOf('https://') !== -1
      // )
      if (
        (res.data.log || "").length > 0 &&
        (res.data.tempId || "").length > 0
      ) {
        this.runResult.hasLog = true;
      }
      // 错误
      if (!res.success) {
        // console.log(res.data);
        this.runResult.hasLog = false; //不显示下载
        this.runResult.tit = "遇到错误";
        this.runResult.tip = "错误信息";
        // this.runResult.msg = res.data.msg;
        this.runResult.msg = "res.data.msg错误信息"; //错误信息
        this.runResult.visible = true; //显示
        return;
      }
      // 成功
      if (
        res.data.msg &&
        (res.data.msg.indexOf("http://") !== -1 ||
          res.data.msg.indexOf("https://") !== -1)
      ) {
        this.runResult.visible = true;
        this.runResult.tit = "执行成功";
        this.runResult.tip =
          "收到网址，点击下方打开<br />（此链接由第三方提供，请注意安全）：";
        this.runResult.msg =
          '<i class="fa fa-external-link"></i> <a href="' +
          res.data.msg +
          '" target="_blank">' +
          res.data.msg +
          "</a>";
      } else {
        this.runResult.tit = "执行成功";
        this.runResult.tip = "返回信息";
        this.runResult.msg = res.data.msg || res.data.exception;
      }
      // 打开执行结果弹窗
      this.runResult.visible = true;
      this.isExecut = false;
      this.getList();
    },
    // 关闭和开启
    async updataState(state) {
      const id = this.data.xncfModule.id;
      const res = await changeModuleState({ id, toState: state });
      // console.log('关闭和开启', res)
      if (res.data === "OK") {
        this.$notify({
          title: "Success",
          message: "成功",
          type: "success",
          duration: 2000,
        });
        // 重新获取数据
        this.getList();
        return;
      }
      this.$message.error("修改模块状态失败");
      // window.location.reload()
    },
    // 删除
    async handleDelete() {
      const id = this.data.xncfModule.id;
      const res = await deleteModule(id);
      // console.log("删除", res);
      if (res.data === "OK") {
        this.$notify({
          title: "Success",
          message: "删除模块成功",
          type: "success",
          duration: 2000,
        });
        // 删除成功 返回首页
        this.$router.push("/Admin/XncfModule/Index");
        location.reload();
        return;
      }
      this.$message.error("删除失败");
    },
    //element，tab切换执行和日志
    handleClick(tab, event) {
      // console.log(tab, event);
    },
  },
};
</script>

<style lang="scss">
.extend {
  padding: 20px;
}

.filter-container .float-right {
  float: right;
}

.start-delete {
  float: right;
}

.el-collapse-item__header {
  background-color: #ebeef5;
  padding-left: 15px;
}

.el-main {
  padding: 0 20px;
  flex: 1;
  // flex: 0.4; //昨日的样式
}

.el-main .el-card {
  margin: 10px 0;
  max-width: 1000px;
}

.box-card .box-card-action {
  cursor: pointer;
  background-color: #ff6a00;
  float: right;
  position: relative;
  white-space: nowrap;
  line-height: 25px;
  height: 25px;
  color: #fff;
  margin-left: 12.5px;
  margin-right: 10px;
  padding-left: 12.5px;
  padding-right: 9.33333px;
  // width: 59px;
  width: fit-content;
  font-size: 14px;
  text-align: right;
}

.box-card .box-card-action2 {
  cursor: pointer;
  background-color: #909399;
  float: right;
  position: relative;
  white-space: nowrap;
  line-height: 25px;
  height: 25px;
  color: #fff;
  margin-left: 12.5px;
  margin-right: 10px;
  padding-left: 12.5px;
  padding-right: 9.33333px;
  // width: 59px;
  width: fit-content;
  font-size: 14px;
  text-align: right;
}

.box-card .box-card-action::before {
  content: "";
  position: absolute;
  left: 0;
  width: 17.67767px;
  background-color: #ff6a00;
  height: 17.67767px;
  transform: rotate(45deg);
  transform-origin: top left;
}

.box-card .box-card-action2::before {
  content: "";
  position: absolute;
  left: 0;
  width: 17.67767px;
  background-color: #909399;
  height: 17.67767px;
  transform: rotate(45deg);
  transform-origin: top left;
}

.box-card .el-collapse-item__wrap {
  background-color: #ebeef5;
}

.box-card .el-table th,
.box-card .el-table tr {
  background-color: #ebeef5;
}

.box-card .no-content {
  text-align: center;
}

.start-green {
  color: #67c23a;
}

.start-gray {
  color: #909399;
}

.start-warning {
  color: #e6a23c;
}

.start-danger {
  color: #f56c6c;
}

.start-des {
  font-size: 13px;
}

.start-des .fa {
  width: 22px;
}

.start-des li {
  margin-bottom: 10px;
}

.item-container .el-menu {
  width: 300px;
}

.item-container .el-menu a {
  color: #bfcbd9;
  text-decoration: none;
  display: block;
}

.run-dialog .el-checkbox {
  display: block;
  white-space: normal;
}

.run-dialog .icon-note {
  color: #606266;
}

.module-header {
  line-height: 60px;
}

.module-aside {
  padding: 20px;
}

.module-aside .red {
  color: red;
}

.collapse-transition {
  -webkit-transition: 0s height, 0s padding-top, 0s padding-bottom;
  transition: 0s height, 0s padding-top, 0s padding-bottom;
}

.horizontal-collapse-transition {
  -webkit-transition: 0s width, 0s padding-left, 0s padding-right;
  transition: 0s width, 0s padding-left, 0s padding-right;
}

.horizontal-collapse-transition .el-submenu__title .el-submenu__icon-arrow {
  -webkit-transition: 0s;
  transition: 0s;
  opacity: 0;
}

.start-log {
  margin-top: 10px;
  // flex: 0.4;
  flex: 1;
}

.el-card__header a {
  text-decoration: none;
}

.box-card-des {
  padding: 0.25em 0;
  font-style: italic;
  font-size: 12px;
  color: #aab6aa;
}

.thread-contenet {
  margin-top: 10px;
}

.start-title {
  font-weight: 400;
  font-size: 22px;
  color: #1f2d3d;
}
</style>
