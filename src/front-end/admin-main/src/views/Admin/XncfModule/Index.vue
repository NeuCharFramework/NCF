<template>
  <div class="moduleIndex">
    <div class="filter-container" style="text-align: right">
      <el-popconfirm placement="top" :title="handlerTips" @confirm="handleSwitch">
        <el-button size="mini" type="danger" slot="reference">{{
          handlerText
        }}</el-button>
      </el-popconfirm>
    </div>
    <el-container v-if="newTableData.length > 0">
      <el-header class="module-header">
        <span class="start-title">发现 {{ newTableData.length }} 个新模块！</span>
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
          <el-table-column align="center" label="唯一编码（全局唯一）" width="350">
            <template slot-scope="scope">
              <i class="fa fa-shield"></i>
              {{ scope.row.uid }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="120" align="center">
            <template slot-scope="scope">
              <el-button size="mini" type="primary" icon="el-icon-circle-plus" @click="handleInstall(scope.$index, scope.row)">安装</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-main>
    </el-container>
    <el-container v-if="oldTableData.length > 0">
      <el-header class="module-header">
        <span class="start-title">已安装模块</span>
      </el-header>
      <el-main>
        <el-table :data="oldTableData" v-if="oldTableData.length > 0" style="width: 100%; margin-bottom: 20px">
          <el-table-column align="left" label="菜单名称/版本号">
            <template slot-scope="scope">
              <div>
                <i :class="
                    scope.row.icon ? scope.row.icon : 'fa fa-chain-broken'
                  "></i>
                {{ scope.row.menuName }}
              </div>
              <div>
                <i class="fa fa-code-fork"></i>
                v{{ scope.row.version }}
              </div>
            </template>
          </el-table-column>
          <el-table-column align="center" label="模块名称/唯一编码（全局唯一）" width="350">
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
              <el-button size="mini" icon="el-icon-s-tools" type="primary" v-if="scope.row" @click="handleHandle(scope.$index, scope.row)">操作</el-button>
              <el-button size="mini" v-if="scope.row.homeUrl" icon="el-icon-s-home" type="primary" @click="handleIndex(scope.$index, scope.row)">主页</el-button>
              <span v-if="!scope.row"> 此模块已被删除~ </span>
            </template>
          </el-table-column>
        </el-table>
      </el-main>
    </el-container>
  </div>
</template>

<script>
import { getModuleList, getUnModuleList, moduleInstallXncf } from '@/api/module'
import { openHideManager, getSystemConfig } from '@/api/system'
export default {
  data() {
    return {
      newTableData: [], // 新模块数据
      oldTableData: [], // 已安装模块
      isExtend: false, //是否切换状态
      handlerText: '',
      handlerTips: '',
      newData: {}
    }
  },
  watch: {
    isExtend: {
      handler: function (val, oldVal) {
        this.handlerText = val
          ? '开启【扩展模块】管理模式'
          : '切换至发布状态，隐藏【扩展模块】管理单元'
        this.handlerTips = val
          ? '打开【扩展模块】管理功能后，所有扩展模块将显示在【扩展模块】二级目录中。确定要打开吗？'
          : '隐藏【扩展模块】管理功能后，所有扩展模块将并列显示在一级目录中。如需重新打开，请直接浏览器内访问此页面【/Admin/XncfModule】。确定要隐藏吗？'
      },
      immediate: true
    }
  },
  filters: {
    oldDataState(value) {
      let obj = {
        0: '关闭',
        1: '开放',
        2: '新增待审核',
        3: '更新待审核'
      }
      return obj[value] || ''
    }
  },
  created: function () {
    this.getList()
  },
  methods: {
    // 获取
    async getList() {
      // 模块状态 是否切换状态

      const extend = await getSystemConfig()
      this.isExtend = extend.data[0].hideModuleManager
      // console.log('isExtend',this.isExtend);
      // 已安装模块
      const oldTableData = await getModuleList()
      // console.log("oldTableData", oldTableData);
      this.oldTableData = oldTableData.data || []

      // 未安装模块
      const newTableData = await getUnModuleList()
      // console.log("newTableData", newTableData);
      this.newTableData = newTableData.data || []
    },

    // 切换状态
    async handleSwitch() {
      // console.log('dasdd')
      await openHideManager({ id: 0, hide: !this.isExtend }).then((res) => {
        // console.log('切换状态', res.data)
        if (res) {
          this.isExtend = res.data.hideModuleManager
          this.getList()
          location.reload()
        }
      })
    },

    // 安装
    async handleInstall(index, row) {
      await moduleInstallXncf(row.uid)
        .then((res) => {
          console.log('安装 ok', row.uid)
          if (res.success) {
            location.reload()
            setTimeout(() => {
              // 跳转到模块详情
              this.$router.push('/Admin/XncfModule/Start/uid=' + row.uid)
            }, 1000)
          }
        })
        .catch(() => {
          this.$message.error('安装失败')
        })
      // window.sessionStorage.setItem("setNavMenuActive", row.menuName);
      // 修改侧边栏
    },

    // 操作
    handleHandle(index, row) {
      this.$router.push('/Admin/XncfModule/Start/uid=' + row.uid)
      // window.location.href = "/Admin/XncfModule/Start/?uid=" + row.uid;
    },

    // 主页
    handleIndex(index, row) {
      console.log('主页', row)
      window.location.href = row.homeUrl
      // this.$router.push('/');
    }
  }
}
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
    content: '';
    position: absolute;
    left: 0;
    width: 17.67767px;
    background-color: #ff6a00;
    height: 17.67767px;
    transform: rotate(45deg);
    transform-origin: top left;
  }

  .box-card .box-card-action2::before {
    content: '';
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

  .box-card-tip {
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
