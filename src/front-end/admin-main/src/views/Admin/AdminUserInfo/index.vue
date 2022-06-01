<template>
  <div class="el-main">
    <div>
      <div class="admin-user-info">
        <div class="filter-container">
          <el-button class="filter-item" icon="el-icon-plus" type="primary" @click="handleEdit">增加</el-button>
        </div>
        <el-table
          :data="tableData"
          border
          style="width: 100%"
        >
          <el-table-column
            align="center"
            label="id"
            width="80"
          >
            <template slot-scope="scope">
              {{ scope.row.id }}
            </template>
          </el-table-column>
          <el-table-column
            align="center"
            label="用户名"
            width="180"
          >
            <template slot-scope="scope">
              {{ scope.row.userName }}
            </template>
          </el-table-column>
          <el-table-column
            align="center"
            label="备注"
          >
            <template slot-scope="scope">
              {{ scope.row.note }}
            </template>
          </el-table-column>
          <el-table-column
            align="center"
            label="添加时间"
          >
            <template slot-scope="scope">
              {{ scope.row.addTime }}
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
                @click="handleSet(scope.$index, scope.row)"
              >设置角色
              </el-button>
              <el-popconfirm placement="top" title="确认删除此角色吗？" @on-confirm="handleDelete(scope.$index, scope.row)">
                <el-button slot="reference" size="mini" type="danger">删除</el-button>
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
        <el-dialog :close-on-click-modal="false" :title="dialog.title" :visible.sync="dialog.visible">
          <el-form
            ref="dataForm"
            :model="dialog.data"
            :rules="dialog.rules"
            label-position="left"
            label-width="100px"
            style="max-width: 400px; margin-left:50px;"
          >
            <p v-if="dialog.title==='编辑管理员'" class="username-tip">不建议修改用户名！</p>
            <el-form-item label="用户名" prop="userName">
              <el-input v-model="dialog.data.userName" clearable placeholder="请输入用户名"/>
            </el-form-item>
            <el-form-item :error="dialog.passwordError" :required="isVerPass" label="密码">
              <el-input v-model="dialog.data.password" clearable placeholder="请输入密码" show-password/>
            </el-form-item>
            <el-form-item :error="dialog.password2Error" :required="isVerPass" label="确认密码">
              <el-input v-model="dialog.data.password2" clearable placeholder="请再次输入密码" show-password/>
            </el-form-item>
            <el-form-item label="真实姓名" prop="realName">
              <el-input v-model="dialog.data.realName" clearable placeholder="请输入真实姓名"/>
            </el-form-item>
            <el-form-item label="手机号" prop="phone">
              <el-input v-model="dialog.data.phone" clearable placeholder="请输入手机号"/>
            </el-form-item>
            <el-form-item label="备注" prop="note">
              <el-input v-model="dialog.data.note" clearable placeholder="请输入备注信息"/>
            </el-form-item>
          </el-form>
          <div slot="footer" class="dialog-footer">
            <el-button @click="dialog.visible = false">取 消</el-button>
            <el-button :loading="dialog.updateLoading" type="primary" @click="updateData">确 认</el-button>
          </div>
        </el-dialog>
        <!--分配角色-->
        <el-dialog :title="'设置角色-'+dialog.setTitle" :visible.sync="dialog.visibleSet">
          <el-checkbox-group v-model="dialog.dialogSetSelected">
            <el-checkbox v-for="item in dialog.dialogSetData" :label="item.value" border>{{ item.text }}</el-checkbox>
          </el-checkbox-group>
          <div slot="footer" class="dialog-footer">
            <el-button @click="dialog.visibleSet = false">取 消</el-button>
            <el-button :loading="dialog.updateLoading" type="primary" @click="updateDataSet">确 认</el-button>
          </div>
        </el-dialog>
      </div>
    </div>
  </div>
</template>

<script>
import { getAdminUserList } from '@/api/admin'

export default {
  name: 'AdminUserInfo',
  data() {
    return {
      // 分页参数
      paginationQuery: {
        total: 5
      },
      // 分页接口传参
      listQuery: {
        pageIndex: 1,
        pageSize: 100,
      },
      tableData: [],
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
          note: ''
        },
        rules: {
          userName: [
            {required: true, message: '用户名为必填项', trigger: 'blur'}
          ]
          // password: [{ required: true, validator: validatePass, trigger: "blur" }],
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
      } else if (this.dialog.title === '编辑管理员' && this.dialog.data.password.length > 0 || this.dialog.title === '编辑管理员' && this.dialog.data.password2.length > 0) {
        return true
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
    'dialog.visible': function (val, old) {
      // 关闭dialog，清空
      if (!val) {
        this.dialog.data = {
          id: 0,
          userName: '',
          password: '',
          password2: '',
          realName: '',
          phone: '',
          note: ''
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
    // 再次校验密码
    checkPass() {
      if (this.isVerPass) {
        if (this.dialog.data.password === '') {
          if (this.dialog.visible) {
            this.dialog.passwordError = '请输入密码'
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
      const {adminUserInfoName, pageIndex, pageSize} = this.listQuery
      let res = await getAdminUserList(this.listQuery)
      if(res.success){
        let data = res.data
        this.tableData = data.list
        this.paginationQuery.total = data.totalCount
      }
    },
    // 编辑
    handleEdit(index, row) {
      this.dialog.visible = true
      if (row) {
        // 编辑
        const {userName, password, realName, phone, note, id} = row
        this.dialog.data = {
          userName, realName, phone, note, id, password: '', password2: ''
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
      this.$refs['dataForm'].validate(valid => {
        // 表单校验
        if (valid) {
          // 需要校验
          if (this.isVerPass) {
            // 校验不通过
            if (!this.dialog.isVerTrue) {
              return false
            }
          }
          this.dialog.updateLoading = true
          const data = {
            Id: this.dialog.data.id,
            UserName: this.dialog.data.userName,
            Password: this.dialog.data.password,
            Note: this.dialog.data.note,
            RealName: this.dialog.data.realName,
            Phone: this.dialog.data.phone
          }
          // service.post('/Admin/AdminUserInfo/Edit?handler=Save', data).then(res => {
          //   if (res.data.success) {
          //     this.getList()
          //     this.$notify({
          //       title: 'Success',
          //       message: '成功',
          //       type: 'success',
          //       duration: 2000
          //     })
          //     this.dialog.visible = false
          //     this.dialog.updateLoading = false
          //   }
          // }).catch(error => {
          //   this.dialog.updateLoading = false
          // })
        }
      })
    },
    // 更新设置角色
    updateDataSet() {
      this.dialog.updateLoadingSet = true
      const data = {RoleIds: this.dialog.dialogSetSelected, AccountId: this.setId}
      // service.post('/Admin/AdminUserInfo/AuthorizationPage', data).then(res => {
      //   if (res.data.success) {
      //     this.getList()
      //     this.$notify({
      //       title: 'Success',
      //       message: '成功',
      //       type: 'success',
      //       duration: 2000
      //     })
      //     this.dialog.visibleSet = false
      //     this.dialog.updateLoadingSet = false
      //   }
      // })
    },
    // 设置角色
    handleSet(index, row) {
      this.dialog.dialogSetSelected = []
      this.dialog.visibleSet = true
      this.setId = row.id
      this.dialog.setTitle = row.userName
      // // 所有角色
      // const a = await service.get('/Admin/Role/edit?Handler=SelectItems')
      // if (a.data.success) {
      //   this.dialog.dialogSetData = a.data.data
      // }
      // // 已有角色
      // const b = await service.get(`/Admin/AdminUserInfo/AuthorizationPage?Handler=Detail&accountId=${this.setId}`)
      // if (b.data.success) {
      //   b.data.data.map(res => {
      //     this.dialog.dialogSetSelected.push(res.roleId)
      //   })
      // }
    },
    // 删除
    handleDelete(index, row) {
      const ids = [row.id]
      // service.post('/Admin/AdminUserInfo/Index?handler=Delete', ids).then(res => {
      //   if (res.data.success) {
      //     this.getList()
      //     this.$notify({
      //       title: 'Success',
      //       message: '删除成功',
      //       type: 'success',
      //       duration: 2000
      //     })
      //   }
      // })
    }
  }
}
</script>

<style lang="scss" scoped>

</style>
