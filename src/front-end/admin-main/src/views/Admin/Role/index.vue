<template>
  <div class="el-main">
    <div class="admin-role">
      <div class="filter-container">
        <el-button class="filter-item" type="primary" icon="el-icon-plus" @click="handleEdit">增加</el-button>
      </div>
      <el-table :data="tableData" border style="width: 100%">
        <el-table-column align="center" label="角色名称">
          <template slot-scope="scope">
            {{ scope.row.roleName }}
          </template>
        </el-table-column>
        <el-table-column align="center" label="角色代码">
          <template slot-scope="scope">
            {{ scope.row.roleCode }}
          </template>
        </el-table-column>
        <el-table-column align="center" label="备注">
          <template slot-scope="scope">
            {{ scope.row.adminRemark }}
          </template>
        </el-table-column>
        <el-table-column align="center" label="说明">
          <template slot-scope="scope">
            {{ scope.row.remark }}
          </template>
        </el-table-column>
        <el-table-column align="center" label="添加时间">
          <template slot-scope="scope">
            {{ scope.row.addTime | formaTime }}
          </template>
        </el-table-column>
        <el-table-column label="操作" align="center">
          <template slot-scope="scope">
            <el-button size="mini" type="primary" @click="handleEdit(scope.$index, scope.row)">编辑</el-button>
            <el-button size="mini" type="primary" @click="handleRole(scope.$index, scope.row)">权限</el-button>
            <el-popconfirm placement="top" title="确认删除此角色吗？" @confirm="handleDelete(scope.$index, scope.row)">
              <el-button slot="reference" size="mini" type="danger">删除</el-button>
            </el-popconfirm>
          </template>
        </el-table-column>
      </el-table>
      <!-- <pagination :total="total" :page.sync="listQuery.pageIndex" :limit.sync="listQuery.pageSize" @pagination="getList" /> -->
      <!--编辑、新增 -->
      <el-dialog :title="dialog.title" :visible.sync="dialog.visible" :close-on-click-modal="false">
        <el-form ref="dataForm" :rules="dialog.rules" :model="dialog.data" label-position="left" label-width="100px" style="max-width: 400px; margin-left: 50px">
          <el-form-item label="角色名称" prop="roleName">
            <el-input v-model="dialog.data.roleName" clearable placeholder="请输入角色名称" />
          </el-form-item>
          <el-form-item label="角色代码" prop="roleCode">
            <el-input v-model="dialog.data.roleCode" clearable placeholder="请输入角色代码" />
          </el-form-item>
          <el-form-item label="是否启用">
            <el-switch v-model="dialog.data.enabled" />
          </el-form-item>
          <el-form-item label="说明">
            <el-input v-model="dialog.data.remark" clearable placeholder="请输入说明" />
          </el-form-item>
          <el-form-item label="备注">
            <el-input v-model="dialog.data.adminRemark" clearable placeholder="请输入备注信息" />
          </el-form-item>
        </el-form>
        <div slot="footer" class="dialog-footer">
          <el-button @click="dialog.visible = false">取 消</el-button>
          <el-button :loading="dialog.updateLoading" type="primary" @click="updateData">确 认</el-button>
        </div>
      </el-dialog>
      <!--授权 -->
      <el-dialog :title="'为  ' + au.title + '  角色授权'" :visible.sync="au.visible" :close-on-click-modal="false">
        <el-tree ref="tree" :data="allMenu" show-checkbox node-key="id" :props="defaultProps" :default-expanded-keys="defaultExpandedKeys" :default-checked-keys="defaultCheckedKeys" />
        <div slot="footer" class="dialog-footer">
          <el-button @click="au.visible = false">取 消</el-button>
          <el-button :loading="au.updateLoading" type="primary" @click="auUpdateData">确 认</el-button>
        </div>
      </el-dialog>
    </div>
  </div>
</template>

<script>
import {
  getAllRoles,
  createPermission,
  createOrUpdateRole,
  deleteRole,
  getRolePermissions
} from '@/api/roles'
import { getAllMenus } from '@/api/menu'
export default {
  name: 'Index',
  data() {
    return {
      // 数据总数
      total: 5,
      // 分页接口传参
      listQuery: {
        pageIndex: 1,
        pageSize: 100,
        roleName: '',
        orderField: ''
      },
      tableData: [],
      dialog: {
        title: '新增角色',
        visible: false,
        data: {
          roleName: '',
          roleCode: '',
          adminRemark: '',
          remark: '',
          addTime: '',
          id: '',
          enabled: false
        },
        rules: {
          roleName: [
            { required: true, message: '角色名称为必填项', trigger: 'blur' }
          ],
          roleCode: [
            { required: true, message: '角色代码为必填项', trigger: 'blur' }
          ]
        },
        updateLoading: false
      },
      // 树结构字段
      defaultProps: {
        children: 'children',
        label: 'menuName'
      },
      // 授权
      au: {
        title: '',
        visible: false,
        updateLoading: false,
        temp: {}
      },
      allMenu: [], // 所有权限
      currMenu: [], // 当前权限
      defaultExpandedKeys: [], // 默认展开
      defaultCheckedKeys: [], // 默认选中
      parentArr: [] // 父节点集合
    }
  },
  watch: {
    'dialog.visible'(val, old) {
      // 关闭dialog，清空
      if (!val) {
        this.dialog.data = {
          roleName: '',
          roleCode: '',
          adminRemark: '',
          remark: '',
          addTime: '',
          id: ''
        }
        this.dialog.updateLoading = false
      }
    },
    'au.visible'(val, old) {
      // 关闭dialog，清空
      if (!val) {
        this.currMenu = []
        this.defaultCheckedKeys = []
        this.defaultExpandedKeys = []
        this.au.updateLoading = false
      }
    }
  },
  created() {
    this.getList()
  },
  methods: {
    // 权限
    async handleRole(index, row) {
      // 打开dialog
      this.au = {
        title: row.roleName,
        visible: true,
        temp: row
      }
      // 当前已有权限
      const c = await getRolePermissions({ roleId: row.id })
      // console.log('handleRole c', c.data.permissionId)
      // 默认选中
      this.defaultCheckedKeys = c.data.permissionId
      // 菜单列表
      const a = await getAllMenus({ hasButton: true })
      // console.log('handleRole a', a.data.items)
      // 去重菜单树
      const menuTree = a.data.items
      this.menuDeWeight(menuTree)
      // console.log('menuTree', menuTree)
      // 所有权限 菜单列表
      this.allMenu = menuTree
    },
    // 获取所有权限|菜单树去重
    menuDeWeight(source) {
      const arr = []
      for (var i = source.length - 1; i >= 0; i--) {
        const ele = source[i]
        if (arr.indexOf(ele.id) === -1) {
          arr.push(ele.id)
        } else {
          if (ele.children.length > 0) {
            const arrIndex = source.findIndex((el) => ele.id === el.id)
            // console.log('arrIndex', arrIndex)
            source.splice(arrIndex, 1)
            if (arrIndex < i) i++
          } else {
            source.splice(i, 1)
          }
          // console.log('source splice', item.id, source)
        }
        if (ele.children.length > 0) {
          this.menuDeWeight(ele.children)
        }
      }
    },
    // 更新授权
    auUpdateData() {
      this.au.updateLoading = true
      const checkNodes = this.$refs.tree.getCheckedNodes(false, true)
      const array = []
      checkNodes.map((ele) => {
        array.push({
          permissionId: ele.id,
          roleId: this.au.temp.id,
          isMenu: ele.isMenu,
          roleCode: this.au.temp.roleCode
          // resourceCode: ''
        })
      })
      console.log(this.au.temp, array)
      createPermission({ requestDtos: array })
        .then((res) => {
          this.$notify({
            title: 'Success',
            message: '成功',
            type: 'success',
            duration: 2000
          })
          this.au.updateLoading = false
          this.au.visible = false
          this.getList()
          // window.location.reload()
        })
        .catch(() => {
          this.$message.error('失败')
          this.au.updateLoading = false
        })
    },

    // 编辑
    handleEdit(index, row) {
      this.dialog.visible = true
      if (row) {
        // 编辑
        const {
          roleName,
          roleCode,
          adminRemark,
          remark,
          addTime,
          id,
          enabled
        } = row
        this.dialog.data = {
          roleName,
          roleCode,
          adminRemark,
          remark,
          addTime,
          id,
          enabled
        }
        this.dialog.title = '编辑角色'
      } else {
        // 新增
        this.dialog.title = '新增角色'
      }
    },
    // 更新新增、编辑
    updateData() {
      // 没有操作权限
      this.$refs['dataForm'].validate((valid) => {
        // 表单校验
        if (valid) {
          this.dialog.updateLoading = true
          const { id, roleName, roleCode, adminRemark, remark, enabled } =
            this.dialog.data
          const data = { id, roleName, roleCode, adminRemark, remark, enabled }
          createOrUpdateRole(data)
            .then((res) => {
              this.$notify({
                title: 'Success',
                message: '成功',
                type: 'success',
                duration: 2000
              })
              this.dialog.updateLoading = false
              this.dialog.visible = false
              this.getList()
            })
            .catch(() => {
              this.$message.error('失败')
              this.dialog.updateLoading = false
            })
        }
      })
    },
    // 删除
    handleDelete(index, row) {
      const id = {
        id: row.id
      }
      deleteRole(id)
        .then((res) => {
          this.$notify({
            title: 'Success',
            message: '删除成功',
            type: 'success',
            duration: 2000
          })
          this.getList()
        })
        .catch(() => {
          this.$message.error('失败')
        })
    },
    // 初始化获取数据
    getList() {
      getAllRoles(this.listQuery).then((res) => {
        // console.log('getAllRoles', res)
        if (res.data) {
          const data = res.data
          this.tableData = data.list
          this.total = data.totalCount
        }
      })
    }
  }
}
</script>

<style lang="scss" scoped>
</style>
