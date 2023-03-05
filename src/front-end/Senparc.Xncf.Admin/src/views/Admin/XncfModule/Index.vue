<template>
  <div class="moduleIndex">
    <div class="filter-container" style="text-align: left">
      <el-popconfirm
        placement="top"
        :title="handlerTips"
        @confirm="handleSwitch"
      >
        <el-button
          icon="el-icon-edit"
          size="mini"
          type="primary"
          slot="reference"
          >{{ handlerText }}</el-button
        >
      </el-popconfirm>
    </div>
    <!-- 未安装模块 -->
    <!-- <el-container v-if="newTableData.length > 0">
      <el-header class="module-header">
        <span class="start-title"
          >发现 {{ newTableData.length }} 个新模块！</span
        >
      </el-header>
      <el-main>
        <el-table :data="newTableData" style="width: 100%; margin-bottom: 20px">
          <el-table-column align="left" label="模块名称">
            <template slot-scope="scope">
              <i class="fa fa-cubes"></i>
              {{ scope.row.name }}
            </template>
          </el-table-column>
          <el-table-column align="center" label="菜单名称（显示名称）">
            <template slot-scope="scope">
              <i :class="scope.row.icon"></i>
              {{ scope.row.menuName }}
            </template>
          </el-table-column>
          <el-table-column align="center" label="版本号">
            <template slot-scope="scope">
              <i class="fa fa-code-fork"></i>
              {{ scope.row.version }}
            </template>
          </el-table-column>
          <el-table-column
            align="center"
            label="唯一编码（全局唯一）"
            width="350"
          >
            <template slot-scope="scope">
              <i class="fa fa-shield"></i>
              {{ scope.row.uid }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="120" align="center">
            <template slot-scope="scope">
              <el-button
                size="mini"
                type="primary"
                icon="el-icon-circle-plus"
                @click="handleInstall(scope.$index, scope.row)"
                >安装</el-button
              >
            </template>
          </el-table-column>
        </el-table>
      </el-main>
    </el-container> -->
    <!-- 已安装模块 -->
    <!-- <el-container v-if="oldTableData.length > 0">
      <el-header class="module-header">
        <span class="start-title">已安装模块</span>
      </el-header>
      <el-main>
        <el-table
          :data="oldTableData"
          v-if="oldTableData.length > 0"
          style="width: 100%; margin-bottom: 20px"
        >
          <el-table-column align="left" label="菜单名称/版本号">
            <template slot-scope="scope">
              <div>
                <i
                  :class="
                    scope.row.icon ? scope.row.icon : 'fa fa-chain-broken'
                  "
                ></i>
                {{ scope.row.menuName }}
              </div>
              <div>
                <i class="fa fa-code-fork"></i>
                v{{ scope.row.version }}
              </div>
            </template>
          </el-table-column>
          <el-table-column
            align="center"
            label="模块名称/唯一编码（全局唯一）"
            width="350"
          >
            <template slot-scope="scope">
              <div>
                <i class="fa fa-cubes"></i>
                {{ scope.row.name }}
              </div>
              <div>
                <i class="fa fa-shield"></i>
                {{ scope.row.uid }}
              </div>
            </template>
          </el-table-column>
          <el-table-column align="center" label="状态">
            <template slot-scope="scope">
              {{ scope.row.state | oldDataState }}
            </template>
          </el-table-column>
          <el-table-column align="center" label="添加时间">
            <template slot-scope="scope">
              {{ scope.row.addTime | dateFormat }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="250" align="center">
            <template slot-scope="scope">
              <el-button
                size="mini"
                icon="el-icon-s-tools"
                type="primary"
                v-if="scope.row"
                @click="handleHandle(scope.$index, scope.row)"
                >操作</el-button
              >
              <el-button
                size="mini"
                v-if="scope.row.homeUrl"
                icon="el-icon-s-home"
                type="primary"
                @click="handleIndex(scope.$index, scope.row)"
                >主页</el-button
              >
              <span v-if="!scope.row"> 此模块已被删除~ </span>
            </template>
          </el-table-column>
        </el-table>
      </el-main>
    </el-container> -->

    <!-- 修改的地方 -->
    <el-tabs v-model="activeName" type="card" @tab-click="handleClick">
      <!-- 应用市场 -->
      <el-tab-pane label="应用市场" name="first">
        <!-- 搜索模块 -->
        <el-input
          placeholder="请输入内容"
          v-model="searchIpt.marketIpt"
          class="input-with-select"
        >
          <el-button
            slot="append"
            icon="el-icon-search"
            @click="search('marketIpt')"
          ></el-button>
        </el-input>
        <el-empty
          :image-size="200"
          description="尚未发布，敬请等待！"
        ></el-empty
      ></el-tab-pane>
      <!-- 发现？个新模块 -->
      <el-tab-pane :label="tabItem.findModule" name="findModule"
        ><el-container>
          <!-- v-if="newTableData.length > 0"原判断条件 -->
          <!-- <el-header class="module-header">
            <span class="start-title"
              >发现 {{ newTableData.length }} 个新模块！</span
            >
          </el-header> -->
          <el-main>
            <!-- 搜索模块 -->
            <el-input
              placeholder="请输入内容"
              v-model="searchIpt.newDiscoveryIpt"
              class="input-with-select"
            >
              <el-button
                slot="append"
                icon="el-icon-search"
                @click="search('newDiscoveryIpt')"
              ></el-button>
            </el-input>
            <el-table
              :data="newTableData"
              style="width: 100%; margin-bottom: 20px"
            >
              <el-table-column align="left" label="模块名称">
                <template slot-scope="scope">
                  <i class="fa fa-cubes"></i>
                  {{ scope.row.name }}
                </template>
              </el-table-column>
              <el-table-column align="center" label="菜单名称（显示名称）">
                <template slot-scope="scope">
                  <i :class="scope.row.icon"></i>
                  {{ scope.row.menuName }}
                </template>
              </el-table-column>
              <el-table-column align="center" label="版本号">
                <template slot-scope="scope">
                  <i class="fa fa-code-fork"></i>
                  {{ scope.row.version }}
                </template>
              </el-table-column>
              <el-table-column
                align="center"
                label="唯一编码（全局唯一）"
                width="350"
              >
                <template slot-scope="scope">
                  <i class="fa fa-shield"></i>
                  {{ scope.row.uid }}
                </template>
              </el-table-column>
              <el-table-column label="操作" width="120" align="center">
                <template slot-scope="scope">
                  <el-button
                    size="mini"
                    type="primary"
                    icon="el-icon-circle-plus"
                    @click="handleInstall(scope.$index, scope.row)"
                    >安装</el-button
                  >
                </template>
              </el-table-column>
            </el-table>
          </el-main>
        </el-container></el-tab-pane
      >
      <!-- 已安装模块 -->
      <el-tab-pane :label="tabItem.installModule" name="installModule"
        ><el-main>
          <!-- 搜索模块 -->
          <el-input
            placeholder="请输入内容"
            v-model="searchIpt.alreadyIpt"
            class="input-with-select"
          >
            <el-button
              slot="append"
              icon="el-icon-search"
              @click="search('alreadyIpt')"
            ></el-button>
          </el-input>
          <el-table
            :data="oldTableData"
            v-if="oldTableData.length > 0"
            style="width: 100%; margin-bottom: 20px"
          >
            <el-table-column align="left" label="菜单名称/版本号">
              <template slot-scope="scope">
                <div>
                  <i
                    :class="
                      scope.row.icon ? scope.row.icon : 'fa fa-chain-broken'
                    "
                  ></i>
                  {{ scope.row.menuName }}
                </div>
                <div>
                  <i class="fa fa-code-fork"></i>
                  v{{ scope.row.version }}
                </div>
              </template>
            </el-table-column>
            <el-table-column
              align="center"
              label="模块名称/唯一编码（全局唯一）"
              width="350"
            >
              <template slot-scope="scope">
                <div>
                  <i class="fa fa-cubes"></i>
                  {{ scope.row.name }}
                </div>
                <div>
                  <i class="fa fa-shield"></i>
                  {{ scope.row.uid }}
                </div>
              </template>
            </el-table-column>
            <el-table-column align="center" label="状态">
              <template slot-scope="scope">
                {{ scope.row.state | oldDataState }}
              </template>
            </el-table-column>
            <el-table-column align="center" label="添加时间">
              <template slot-scope="scope">
                {{ scope.row.addTime | dateFormat }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="250" align="center">
              <template slot-scope="scope">
                <el-button
                  size="mini"
                  icon="el-icon-s-tools"
                  type="primary"
                  v-if="scope.row"
                  @click="handleHandle(scope.$index, scope.row)"
                  >操作</el-button
                >
                <el-button
                  size="mini"
                  v-if="scope.row.homeUrl"
                  icon="el-icon-s-home"
                  type="primary"
                  @click="handleIndex(scope.$index, scope.row)"
                  >主页</el-button
                >
                <span v-if="!scope.row"> 此模块已被删除~ </span>
              </template>
            </el-table-column>
          </el-table>
        </el-main></el-tab-pane
      >
      <!-- 发现？个模块更新 -->
      <el-tab-pane :label="tabItem.updateModule" name="updateModule">
        <!-- 判断条件暂时忘了 -->
        <el-main>
          <!-- 搜索模块 -->
          <el-input
            placeholder="请输入内容"
            v-model="searchIpt.updateIpt"
            class="input-with-select"
          >
            <el-button
              slot="append"
              icon="el-icon-search"
              @click="search('updateIpt')"
            ></el-button>
          </el-input>
          <el-table
            :data="OldUpdate"
            v-if="OldUpdate.length > 0"
            style="width: 100%; margin-bottom: 20px"
          >
            <el-table-column align="left" label="菜单名称/版本号">
              <template slot-scope="scope">
                <div>
                  <i
                    :class="
                      scope.row.icon ? scope.row.icon : 'fa fa-chain-broken'
                    "
                  ></i>
                  {{ scope.row.menuName }}
                </div>
                <div>
                  <i class="fa fa-code-fork"></i>
                  v{{ scope.row.version }}
                </div>
              </template>
            </el-table-column>
            <el-table-column
              align="center"
              label="模块名称/唯一编码（全局唯一）"
              width="350"
            >
              <template slot-scope="scope">
                <div>
                  <i class="fa fa-cubes"></i>
                  {{ scope.row.name }}
                </div>
                <div>
                  <i class="fa fa-shield"></i>
                  {{ scope.row.uid }}
                </div>
              </template>
            </el-table-column>
            <el-table-column align="center" label="状态">
              <template slot-scope="scope">
                {{ scope.row.state | oldDataState }}
              </template>
            </el-table-column>
            <el-table-column align="center" label="添加时间">
              <template slot-scope="scope">
                {{ scope.row.addTime | dateFormat }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="250" align="center">
              <template slot-scope="scope">
                <el-button
                  size="mini"
                  icon="el-icon-s-tools"
                  type="primary"
                  v-if="scope.row"
                  @click="handleHandle(scope.$index, scope.row)"
                  >操作</el-button
                >
                <el-button
                  size="mini"
                  v-if="scope.row.homeUrl"
                  icon="el-icon-s-home"
                  type="primary"
                  @click="handleIndex(scope.$index, scope.row)"
                  >主页</el-button
                >
                <span v-if="!scope.row"> 此模块已被删除~ </span>
              </template>
            </el-table-column>
          </el-table>
        </el-main>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script>
import {
  getModuleList,
  getUnModuleList,
  moduleInstallXncf,
} from "@/api/module";
import { openHideManager, getSystemConfig } from "@/api/system";
import { isHaveToken } from "@/utils/auth";
export default {
  data() {
    return {
      newTableData: [], // 新模块数据
      oldTableData: [], // 已安装模块
      isExtend: false, //是否切换状态
      handlerText: "",
      handlerTips: "",
      newData: {},
      // 默认展示已安装模块
      activeName: "updateModule",
      tabItem: {
        findModule: "发现??个模块", //发现多少模块
        installModule: "已安装模块", //已安装多少模块
        updateModule: "发现??个模块更新", //更新发现多少模块
      },
      // 需要更新的模块
      OldUpdate: [],
      // 模块搜索的值
      searchIpt: {
        marketIpt: "", //应用市场搜索
        newDiscoveryIpt: "", //发现新模块
        alreadyIpt: "", //已安装模块
        updateIpt: "", //发现更新模块
      },
      // 模糊搜索
      branchStoreList: [],
    };
  },
  watch: {
    isExtend: {
      handler: function (val, oldVal) {
        this.handlerText = val
          ? "开启【扩展模块】管理模式"
          : "切换至发布状态，隐藏【扩展模块】管理单元";
        this.handlerTips = val
          ? "打开【扩展模块】管理功能后，所有扩展模块将显示在【扩展模块】二级目录中。确定要打开吗？"
          : "隐藏【扩展模块】管理功能后，所有扩展模块将并列显示在一级目录中。如需重新打开，请直接浏览器内访问此页面【/Admin/XncfModule】。确定要隐藏吗？";
      },
      immediate: true,
    },
  },
  filters: {
    oldDataState(value) {
      let obj = {
        0: "关闭",
        1: "开放",
        2: "新增待审核",
        3: "更新待审核",
      };
      return obj[value] || "";
    },
  },
  created: function () {
    isHaveToken();
    this.getList();
  },
  methods: {
    // 获取
    async getList() {
      // 模块状态 是否切换状态
      const extend = await getSystemConfig();
      this.isExtend = extend.data[0].hideModuleManager;
      // console.log('isExtend',this.isExtend);
      // 已安装模块
      const oldTableData = await getModuleList();
      // console.log("oldTableData", oldTableData);
      this.oldTableData = oldTableData.data || [];

      // 发现的需要更新模块---------判断条件暂时未知
      this.oldTableData.forEach((item) => {
        if (item.uid.substr(0, 1) == 0) {
          this.OldUpdate.push(item);
        }
      });
      this.tabItem.updateModule = `发现 ${this.OldUpdate.length} 个模块更新`;
      // console.log("已安装模块", this.oldTableData);

      // 未安装模块
      const newTableData = await getUnModuleList();
      this.newTableData = newTableData.data || [];

      // tab页展示数据
      if (this.newTableData.length > 0) {
        this.tabItem.findModule = `发现 ${this.newTableData.length} 个可安装模块`;
      }
    },
    // 切换状态
    async handleSwitch() {
      // console.log('dasdd')
      await openHideManager({ id: 0, hide: !this.isExtend }).then((res) => {
        // console.log('切换状态', res.data)
        if (res) {
          this.isExtend = res.data.hideModuleManager;
          this.getList();
          location.reload();
        }
      });
    },
    // 安装
    async handleInstall(index, row) {
      await moduleInstallXncf(row.uid)
        .then((res) => {
          console.log("安装 ok", row.uid);
          if (res.success) {
            location.reload();
            setTimeout(() => {
              // 跳转到模块详情
              this.$router.push("/Admin/XncfModule/Start/uid=" + row.uid);
            }, 1000);
          }
        })
        .catch(() => {
          this.$message.error("安装失败");
        });
      // window.sessionStorage.setItem("setNavMenuActive", row.menuName);
      // 修改侧边栏
    },
    // 操作
    handleHandle(index, row) {
      this.$router.push("/Admin/XncfModule/Start/uid=" + row.uid);
      // window.location.href = "/Admin/XncfModule/Start/?uid=" + row.uid;
    },
    // 主页
    handleIndex(index, row) {
      console.log("主页", row);
      window.location.href = row.homeUrl;
      // this.$router.push('/');
    },
    //tab页切换
    handleClick(tab, event) {
      // console.log(tab, event);
    },
    //四种搜索框查询搜索
    search(item) {
      //应用市场搜索
      if (item == "marketIpt") {
        console.log(this.searchIpt.marketIpt);
      }
      //发现的新模块
      if (item == "newDiscoveryIpt") {
        console.log("iptvalue", this.searchIpt.newDiscoveryIpt);
        console.log("请求到的数据", this.newTableData);
        if (this.searchIpt.newDiscoveryIpt) {
          let _value = this.searchIpt.newDiscoveryIpt.toUpperCase();
          let datas = this.newTableData.filter((item) => {
            let _UpperCase = item.description.toUpperCase();
            return _UpperCase.includes(_value);
          });
          this.newTableData = [...datas];
          console.log("处理后数据", this.newTableData);
        } else {
          console.log("刷新请求");
          this.getList();
        }
      }
      // 已安装模块--模糊搜索
      if (item == "alreadyIpt") {
        console.log("iptvalue", this.searchIpt.alreadyIpt);
        console.log("请求到的数据", this.oldTableData);
        if (this.searchIpt.alreadyIpt) {
          let _value = this.searchIpt.alreadyIpt.toUpperCase();
          let datas = this.oldTableData.filter((item) => {
            let _UpperCase = item.description.toUpperCase();
            return _UpperCase.includes(_value);
          });
          this.oldTableData = [...datas];
          console.log("处理后数据", this.oldTableData);
        } else {
          console.log("刷新请求");
          this.getList();
        }
      }
      // 发现的模块更新--模糊搜索
      if (item == "updateIpt") {
        // console.log("iptvalue", this.searchIpt.updateIpt);
        // console.log("模块更新请求数据", this.OldUpdate);
        if (this.searchIpt.updateIpt) {
          let _value = this.searchIpt.updateIpt.toUpperCase();
          let datas = this.OldUpdate.filter((item) => {
            let _UpperCase = item.description.toUpperCase();
            return _UpperCase.includes(_value);
          });
          this.OldUpdate = [...datas];
          console.log("处理后数据", this.OldUpdate);
        } else {
          console.log("刷新请求");
          this.getList();
        }
      }
    },
  },
};
</script>

<style lang="scss">
.moduleIndex {
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
  width: 59px;
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
  width: 59px;
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
  margin-top: 15px;
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
<style scoped>
.moduleIndex >>> .el-input {
  width: 300px;
}
.el-main {
  padding: 0;
}
</style>