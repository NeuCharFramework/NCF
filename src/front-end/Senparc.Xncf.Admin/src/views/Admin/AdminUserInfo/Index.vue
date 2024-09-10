<template>
  <div class="el-main">
    <div>
      <div class="admin-user-info">
        <div class="filter-container">
          <el-button
            class="filter-item"
            icon="el-icon-plus"
            type="primary"
            @click="handleEdit"
          >增加
          </el-button>
        </div>
        <el-table v-loading="loadingData" :data="tableData" border style="width: 100%">
          <el-table-column align="center" label="id" width="80">
            <template slot-scope="scope">
              {{ scope.row.id }}
            </template>
          </el-table-column>
          <el-table-column align="center" label="账号" width="180">
            <template slot-scope="scope">
              {{ scope.row.userName }}
            </template>
          </el-table-column>
          <el-table-column align="center" label="备注">
            <template slot-scope="scope">
              {{ scope.row.note }}
            </template>
          </el-table-column>
          <el-table-column align="center" label="添加时间">
            <template slot-scope="scope">
              {{ scope.row.addTime | dateFormat }}
            </template>
          </el-table-column>
          <el-table-column align="center" label="操作">
            <template slot-scope="scope">
              <el-button
                size="mini"
                type="primary"
                @click="handleEdit(scope.$index, scope.row)"
              >编辑
              </el-button>
              <el-button
                size="mini"
                type="primary"
                style="margin-right: 10px"
                @click="handleSet(scope.$index, scope.row)"
              >设置角色
              </el-button>
              <el-popconfirm
                placement="top"
                title="确认删除此角色吗？"
                @confirm="handleDelete(scope.$index, scope.row)"
              >
                <el-button
                  slot="reference"
                  size="mini"
                  type="danger"
                >删除
                </el-button>
              </el-popconfirm>
            </template>
          </el-table-column>
        </el-table>
        <!--        <pagination-->
        <!--          :total="paginationQuery.total"-->
        <!--          :page.sync="listQuery.pageIndex"-->
        <!--          :limit.sync="listQuery.pageSize"-->
        <!--          @pagination="getList"-->
        <!--        />-->
        <!--编辑、新增 -->
        <el-dialog
          :close-on-click-modal="false"
          :title="dialog.title"
          :visible.sync="dialog.visible"
        >
          <el-form
            ref="dataForm"
            :model="dialog.data"
            :rules="dialog.rules"
            label-position="left"
            label-width="100px"
            style="max-width: 400px; margin-left: 50px"
          >
            <p v-if="dialog.title === '编辑管理员'" class="username-tip">
              不建议修改账号！
            </p>
            <el-form-item label="账号" prop="userName">
              <el-input
                v-model.trim="dialog.data.userName"
                clearable
                placeholder="请输入账号"
              />
            </el-form-item>
            <el-form-item
              :error="dialog.passwordError"
              :required="isVerPass"
              label="密码"
            >
              <el-input
                v-model.trim="dialog.data.password"
                clearable
                placeholder="请输入密码"
                show-password
              />
            </el-form-item>
            <el-form-item
              :error="dialog.password2Error"
              :required="isVerPass"
              label="确认密码"
            >
              <el-input
                v-model.trim="dialog.data.password2"
                clearable
                placeholder="请再次输入密码"
                show-password
              />
            </el-form-item>
            <el-form-item label="真实姓名" prop="realName">
              <el-input
                v-model.trim="dialog.data.realName"
                clearable
                placeholder="请输入真实姓名"
              />
            </el-form-item>
            <el-form-item label="手机号" prop="phone">
              <el-input
                v-model.trim="dialog.data.phone"
                clearable
                placeholder="请输入手机号"
              />
            </el-form-item>
            <el-form-item label="备注" prop="note">
              <el-input
                v-model.trim="dialog.data.note"
                clearable
                placeholder="请输入备注信息"
              />
            </el-form-item>
          </el-form>
          <div slot="footer" class="dialog-footer">
            <el-button @click="dialog.visible = false">取 消</el-button>
            <el-button
              :loading="dialog.updateLoadingSet"
              type="primary"
              @click="updateData"
            >
              确 认
            </el-button>
          </div>
        </el-dialog>
        <!--分配角色-->
        <el-dialog
          :title="'设置角色-' + dialog.setTitle"
          :visible.sync="dialog.visibleSet"
        >
          <el-checkbox-group v-model="dialog.dialogSetSelected">
            <el-checkbox
              v-for="item in dialog.dialogSetData"
              :key="item.id"
              :label="item.id"
              border
            >{{ item.roleName }}
            </el-checkbox>
          </el-checkbox-group>
          <div slot="footer" class="dialog-footer">
            <el-button @click="dialog.visibleSet = false">取 消</el-button>
            <el-button
              :loading="dialog.updateLoading"
              type="primary"
              @click="updateDataSet"
            >
              确 认
            </el-button>
          </div>
        </el-dialog>
      </div>
    </div>
    <br><br>
    <el-pagination
      :page-size="listQuery.pageSize"
      :total="paginationQuery.total"
      :page-sizes="[10, 20, 30, 40]"
      layout="sizes,prev, pager, next"
      @size-change="changePageSize"
      @current-change="changePage"
    />
  </div>
</template>

<script>
import {
  getAdminUserList,
  createAdminUser,
  updateAdminUser,
  createRole,
  getRoles,
  deleteRole
} from '@/api/admin'
// 获取角色列表
import { getAllRoles } from '@/api/roles'
import { isHaveToken } from '@/utils/auth'


export default {
  name: 'AdminUserInfo',
  data() {
    return {
      baseApi: process.env.VUE_APP_BASE_DOMINNAME,
      // 分页参数
      paginationQuery: {
        total: 0
      },
      // 分页接口传参
      listQuery: {
        pageIndex: 1,
        pageSize: 10
      },
      tableData: [],
      checkingOrg: false,
      loadingData: true,
      dialog: {
        title: '新增管理员',
        visible: false,
        data: {
          id: 0,
          userName: '',
          password: '',
          password2: '',
          realName: '',
          phone: '',
          note: '',
          alias: '',
          weiXinId: ''
        },
        rules: {
          userName: [
            { required: true, message: '账号为必填项', trigger: 'blur' },
            { min: 5, message: '长度大于等于5个字符', trigger: 'blur' }
          ],
          password: [
            { required: true, message: '密码为必填项', trigger: 'blur' },
            { min: 6, message: '密码必须六位数以上', trigger: 'blur' }
          ]
          // password2: [{ required: true, validator: validatePass2, trigger: "blur" }]
        },
        updateLoading: false,
        visibleSet: false, // 设置角色dialog
        updateLoadingSet: false, // 确认loading按钮
        dialogSetData: [],
        dialogSetSelected: [], // 多选框
        setId: '',
        setTitle: '',
        passwordError: '',
        password2Error: '',
        isVerTrue: false // 密码校验是否通过
      }
    }
  },
  computed: {
    // 密码需要校验
    isVerPass() {
      if (this.dialog.title === '新增管理员') {
        return true
      } else if (
        (this.dialog.title === '编辑管理员' &&
          this.dialog.data.password.length > 0) ||
        (this.dialog.title === '编辑管理员' &&
          this.dialog.data.password2.length > 0)
      ) {
        return false
      } else {
        return false
      }
    }
  },
  watch: {
    'dialog.data.password': function myfunction(val) {
      this.checkPass()
    },
    'dialog.data.password2': function myfunction(val) {
      this.checkPass()
    },
    'dialog.visible': function myfunction(val, old) {
      // 关闭dialog，清空
      if (!val) {
        this.dialog.data = {
          id: 0,
          userName: '',
          password: '',
          password2: '',
          realName: '',
          phone: '',
          note: '',
          alias: '',
          weiXinId: ''
        }
        this.dialog.password2Error = ''
        this.dialog.passwordError = ''
        this.dialog.updateLoading = false
        this.$refs['dataForm'].resetFields()
      }
    }
  },
  created() {
    this.getList()
  },
  methods: {
    changePage(e) {
      this.listQuery.pageIndex = e
      this.getList()
    },
    changePageSize(e) {
      this.listQuery.pageSize = e
      this.getList()
    },
    // 再次校验密码
    checkPass() {
      if (this.isVerPass) {
        if (this.dialog.data.password === '') {
          if (this.dialog.visible) {
            this.dialog.passwordError = '请输入密码'
            this.dialog.isVerTrue = false
            return false
          }
        } else if (this.dialog.data.password.length < 6) {
          // 密码必须为6位数以上
          if (this.dialog.visible) {
            this.dialog.passwordError = '密码必须六位数以上'
            this.dialog.isVerTrue = false
            return false
          }
        } else {
          this.dialog.passwordError = ''
          if (this.dialog.data.password2 === '') {
            this.dialog.password2Error = '请再次输入密码'
            this.dialog.isVerTrue = false
            return false
          } else if (this.dialog.data.password !== this.dialog.data.password2) {
            this.dialog.password2Error = '请输入相同的密码'
            this.dialog.isVerTrue = false
            return false
          } else {
            this.dialog.password2Error = ''
            this.dialog.passwordError = ''
            this.dialog.isVerTrue = true
            return true
          }
        }
      }
    },
    // 获取数据
    async getList() {
      await isHaveToken() // 判断是否过期
      this.loadingData = true
      const res = await getAdminUserList(this.listQuery)
      if (res.data) {
        this.loadingData = false
        const data = res.data
        // console.log(JSON.parse(JSON.stringify(data)));
        this.tableData = data.list
        this.paginationQuery.total = data.totalCount
      }
    },

    // 编辑
    handleEdit(index, row) {
      this.dialog.visible = true
      if (row) {
        // 编辑
        const { userName, realName, phone, note, id, alias, weiXinId } = row
        this.dialog.data = {
          userName,
          realName,
          phone,
          note,
          id,
          password: '',
          password2: '',
          alias,
          weiXinId

        }
        this.dialog.title = '编辑管理员'
        this.dialog = Object.assign({}, this.dialog)
      } else {
        // 新增
        this.dialog.title = '新增管理员'
      }
    },
    // 更新新增编辑
    updateData() {
      this.checkPass()
      this.$refs['dataForm'].validate((valid) => {
        // 表单校验
        if (valid) {
          // 需要校验
          if (this.isVerPass) {
            // 校验不通过
            if (!this.dialog.isVerTrue) {
              return false
            }
          }
          this.dialog.updateLoadingSet = true
          const data = {
            id: 0,
            userName: this.dialog.data.userName,
            password: this.dialog.data.password,
            realName: this.dialog.data.realName,
            phone: this.dialog.data.phone,
            note: this.dialog.data.note,
            alias: this.dialog.data.alias,
            weiXinId: this.dialog.data.weiXinId
          }
          if (this.dialog.title === '新增管理员') {
            // 新增管理员
            createAdminUser(data)
              .then((res) => {
                this.$notify({
                  title: 'Success',
                  message: '成功',
                  type: 'success',
                  duration: 2000
                })
                this.dialog.updateLoadingSet = false
                this.dialog.visible = false
                this.getList()
              })
              .catch(() => {
                this.$message.error('失败')
                this.dialog.updateLoadingSet = false
              })
          } else {
            data.id = this.dialog.data.id
            if (data.password === '') {
              delete data.password
            }
            // 编辑管理员信息
            updateAdminUser(data)
              .then((res) => {
                this.$notify({
                  title: 'Success',
                  message: '成功',
                  type: 'success',
                  duration: 2000
                })
                this.dialog.updateLoadingSet = false
                this.dialog.visible = false
                this.getList()
              })
              .catch(() => {
                this.$message.error('失败')
                this.dialog.updateLoadingSet = false
              })
          }
        }
      })
    },
    // 更新设置角色
    async updateDataSet() {
      const data = {
        roleId: this.dialog.dialogSetSelected,
        accountId: this.setId
      }
      // console.log("updateDataSet", data);
      // 修改角色
      this.dialog.updateLoading = true
      const res = await createRole(data)
      if (res.success) {
        this.$notify({
          title: 'Success',
          message: '成功',
          type: 'success',
          duration: 2000
        })
        this.dialog.updateLoading = false
        this.dialog.visibleSet = false
        this.getList()
      } else {
        this.$message.error(res.errorMessage)
        this.dialog.updateLoading = false
      }
    },
    // 设置角色
    async handleSet(index, row) {
      this.dialog.dialogSetSelected = []
      this.dialog.visibleSet = true
      this.setId = row.id
      this.dialog.setTitle = row.userName
      const data = {
        roleId: this.dialog.dialogSetSelected,
        accountId: this.setId
      }
      // console.log(JSON.parse(JSON.stringify(data)));
      // 获取所有角色
      const rolesList = await getAllRoles()
      // console.log('rolesList', rolesList)
      if (rolesList) {
        // 获取当前账号 下 角色
        const selfRoles = await getRoles(data)
        // console.log("selfRoles", selfRoles);
        if (selfRoles) {
          this.dialog.dialogSetData = rolesList.data.list || []
          this.dialog.dialogSetSelected = selfRoles.data.roleIds || []
          return
        }
        this.$message.error('拉取角色信息失败')
        return
      }
      this.$message.error('拉取角色信息失败')
    },
    // 删除
    async handleDelete(index, row) {
      const res = await deleteRole(row.id)
      if (res.success) {
        this.$notify({
          title: 'Success',
          message: '删除成功',
          type: 'success',
          duration: 2000
        })
        this.getList()
      } else {
        // console.log(JSON.parse(JSON.stringify(res)));
        this.$notify({
          title: 'ERROR',
          message: res.errorMessage,
          type: 'error',
          duration: 2000
        })
      }
    }
  }
}
</script>

<style lang="scss" scoped>
.custom-tree-node {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 14px;
  padding-right: 8px;
}

.el-checkbox.is-bordered {
  margin: 20px 0 20px 10px;
}
</style>
