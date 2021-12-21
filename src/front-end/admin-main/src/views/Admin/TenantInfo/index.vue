<template>
  <div class="el-main">
    <div class="admin-tenant-info">
      <div>
        <h2 class="tenant-list">当前租户信息</h2>
        <el-row v-show="!enableMultiTenant" :gutter="20">
          <el-col :span="6"><div class="grid-content bg-purple">当前系统已关闭多租户</div></el-col>
        </el-row>
        <el-row v-show="enableMultiTenant" :gutter="20">
          <el-col :span="6"><div class="grid-content bg-purple">当前系统匹配规则：<span>{{ tenantRule }}</span></div></el-col>
          <el-col :span="6"><div class="grid-content bg-purple">名称：<span>{{ requestTenantInfo.name }} <span class="float-right">(ID:{{ requestTenantInfo.id }})</span></span></div></el-col>
          <el-col :span="6"><div class="grid-content bg-purple">匹配关键字：<span>{{ requestTenantInfo.tenantKey }}</span></div></el-col>
          <el-col :span="6"><div class="grid-content bg-purple">匹配时间：<span>{{ requestTenantInfo.beginTime }}</span></div></el-col>
        </el-row>
      </div>

      <h2 class="current-tenant-info">多租户信息</h2>

      <el-table
        :data="tableData"
        border
        style="width: 100%"
      >
        <el-table-column
          align="center"
          label="id(TentanId)"
          width="120"
        >
          <template slot-scope="scope">
            {{ scope.row.id }}
          </template>
        </el-table-column>
        <el-table-column
          align="center"
          label="租户名称"
        >
          <template slot-scope="scope">
            {{ scope.row.name }}
          </template>
        </el-table-column>
        <el-table-column
          align="center"
          label="匹配规则"
        >
          <template slot-scope="scope">
            {{ scope.row.tenantKey }}
          </template>
        </el-table-column>
        <el-table-column
          align="center"
          label="添加时间"
        >
          <template slot-scope="scope">
            {{ formaTableTime(scope.row.addTime) }}
          </template>
        </el-table-column>
        <el-table-column
          align="center"
          label="管理员备注"
        >
          <template slot-scope="scope">
            {{ scope.row.adminRemark }}
          </template>
        </el-table-column>
        <el-table-column
          align="center"
          label="启用"
          width="60"
        >
          <template slot-scope="scope">
            {{ scope.row.enable }}
          </template>
        </el-table-column>
        <el-table-column label="操作" align="center">
          <template slot-scope="scope">
            <el-button
              size="mini"
              type="primary"
              @click="handleEdit(scope.$index, scope.row)"
            >编辑</el-button>
          </template>
          <template>
            <el-popconfirm placement="top" title="确认删除此租户吗？删除后所有租户信息将处于游离状态，请谨慎操作！" @on-confirm="handleDelete(scope.$index, scope.row)">
              <el-button slot="reference" size="mini" type="danger">删除</el-button>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>

      <!--编辑、新增 -->
      <el-dialog :title="dialog.title" :visible.sync="dialog.visible" :close-on-click-modal="false">
        <el-form
          ref="dataForm"
          :rules="dialog.rules"
          :model="dialog.data"
          label-position="left"
          label-width="100px"
          style="max-width: 400px; margin-left:50px;"
        >

          <el-form-item label="租户名称" :error="dialog.nameError" prop="name">
            <el-input v-model="dialog.data.name" clearable placeholder="请输入租户名称" />
          </el-form-item>
          <el-form-item label="租户匹配关键字" :error="dialog.tenantKeyError" prop="tenantKey">
            <el-input v-model="dialog.data.tenantKey" clearable placeholder="请输入租户匹配关键字" />
          </el-form-item>
          <el-form-item label="管理员备注" prop="adminRemark">
            <el-input v-model="dialog.data.adminRemark" clearable placeholder="请输入管理员备注" />
          </el-form-item>
          <el-form-item label="是否启用">
            <el-switch v-model="dialog.data.enable" />
          </el-form-item>
        </el-form>
        <div slot="footer" class="dialog-footer">
          <el-button @click="dialog.visible = false">取 消</el-button>
          <el-button :loading="dialog.updateLoading" type="primary" @click="updateData">确 认</el-button>
        </div>
      </el-dialog>

    </div>
  </div>
</template>

<script>
export default {
  name: 'Index',
  data() {
    return {
      // 分页参数
      paginationQuery: {
        total: 5
      },
      // 分页接口传参
      listQuery: {
        pageIndex: 1,
        pageSize: 20,
        adminUserInfoName: ''
      },
      tableData: [],
      requestTenantInfo: {},
      tenantRule: '',
      enableMultiTenant: true,
      dialog: {
        title: '新增租户信息',
        visible: false,
        data: {
          id: 0,
          name: '',
          tenantKey: '',
          adminRemark: '',
          enable: true
        },
        rules: {
          name: [
            { required: true, message: '租户名称为必填项', trigger: 'blur' }
          ],
          tenantKey: [
            { required: true, message: '租户匹配规则为必填项', trigger: 'blur' }
          ]
        },
        updateLoading: false,
        updateLoadingSet: false, // 确认loading按钮
        nameError: '',
        tenantKeyError: ''
      }
    }
  },
  computed: {

  },
  watch: {
    'dialog.visible': function(val, old) {
      // 关闭dialog，清空
      if (!val) {
        this.dialog.data = {
          id: 0,
          name: '',
          tenantKey: '',
          adminRemark: '',
          enable: true
        }
        this.dialog.nameError = ''
        this.dialog.tenantKeyError = ''
        this.dialog.updateLoading = false
        this.$refs['dataForm'].resetFields()
      }
    }
  },
  created: function() {
    this.getList()
    this.getRequestTenantInfo()
  },
  methods: {
    // 获取数据
    getList() {
      const { pageIndex, pageSize } = this.listQuery
      service.get(`/Admin/TenantInfo/index?handler=List&pageIndex=${pageIndex}&pageSize=${pageSize}`).then(res => {
        this.tableData = res.data.data.list
        this.paginationQuery.total = res.data.data.totalCount
      })
    },
    // 编辑
    handleEdit(index, row) {
      this.dialog.visible = true
      if (row) {
        // 编辑
        const { id, name, tenantKey, adminRemark, enable } = row
        this.dialog.data = {
          id, name, tenantKey, adminRemark, enable
        }
        this.dialog.title = '编辑租户信息'
        this.dialog = Object.assign({}, this.dialog)
      } else {
        // 新增
        this.dialog.title = '新增租户信息'
      }
    },
    // 更新新增编辑
    updateData() {
      this.$refs['dataForm'].validate(valid => {
        // 表单校验
        if (valid) {
          this.dialog.updateLoading = true
          const data = {
            Id: this.dialog.data.id,
            Name: this.dialog.data.name,
            TenantKey: this.dialog.data.tenantKey,
            AdminRemark: this.dialog.data.adminRemark,
            Enable: this.dialog.data.enable
          }
          service.post('/Admin/TenantInfo/Index?handler=Save', data).then(res => {
            if (res.data.success) {
              this.getList()
              this.$notify({
                title: 'Success',
                message: res.data.msg,
                type: 'success',
                duration: 2000
              })
              this.dialog.visible = false
              this.dialog.updateLoading = false
            }
          }).catch(error => {
            this.dialog.updateLoading = false
          })
        }
      })
    },
    // 删除
    handleDelete(index, row) {
      const ids = [row.id]
      service.post('/Admin/TenantInfo/Index?handler=Delete', ids).then(res => {
        if (res.data.success) {
          this.getList()
          this.$notify({
            title: 'Success',
            message: '删除成功',
            type: 'success',
            duration: 2000
          })
        }
      })
    },
    getRequestTenantInfo() {
      service.get('/Admin/TenantInfo/Index?handler=RequestTenantInfo').then(res => {
        if (res.data.success) {
          this.requestTenantInfo = res.data.data.requestTenantInfo
          this.tenantRule = res.data.data.tenantRule
          this.enableMultiTenant = res.data.data.enableMultiTenant
        }
      })
    }
  }
}
</script>

<style lang="scss" scoped>
.admin-tenant-info h2 {
  margin-bottom: 22px;
}

h2.current-tenant-info {
  margin-top: 10px;
}

h2.tenant-list {
  margin-top: 30px;
}

.grid-content {
  background: #654ba7;
  color: #ffffff;
  border-radius: 4px;
  padding: 10px;
}

.admin-tenant-info .float-right {
  float:right;
}
</style>
