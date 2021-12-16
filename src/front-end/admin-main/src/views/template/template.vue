<template>
  <div class="app-container">
    <div class="filter-container">
      <el-input
        v-model="listQuery.title"
        placeholder="标题"
        style="width: 200px"
        class="filter-item"
        @keyup.enter.native="handleFilter"
      />
      <el-button
        v-waves
        class="filter-item"
        type="primary"
        icon="el-icon-search"
        @click="handleFilter"
      >
        搜索
      </el-button>
      <el-button
        class="filter-item"
        style="margin-left: 10px"
        type="primary"
        icon="el-icon-plus"
        @click="handleCreate"
      >
        添加
      </el-button>
    </div>

    <!-- 列表 -->
    <el-table
      :key="tableKey"
      v-loading="listLoading"
      :data="list"
      border
      fit
      highlight-current-row
      style="width: 100%"
    >
      <el-table-column label="#" type="index" width="50"/>
      <el-table-column label="标题">
        <template slot-scope="{ row }">
          <span>{{ row.title }}</span>
        </template>
      </el-table-column>
      <el-table-column label="时间" align="center">
        <template slot-scope="{ row }">
          <span>{{ row.addTime | parseTime("{y}-{m}-{d} {h}:{i}") }}</span>
        </template>
      </el-table-column>
      <el-table-column
        label="操作"
        align="center"
        width="230"
        class-name="small-padding fixed-width"
      >
        <template slot-scope="{ row, $index }">
          <el-button type="primary" size="mini" @click="handleUpdate(row)">
            修改
          </el-button>
          <el-button
            v-if="row.status != 'deleted'"
            size="mini"
            type="danger"
            @click="handleDelete(row, $index)"
          >
            删除
          </el-button>
        </template>
      </el-table-column>
    </el-table>

    <pagination
      v-show="total > 0"
      :total="total"
      :page.sync="listQuery.page"
      :limit.sync="listQuery.limit"
      @pagination="getList"
    />

    <!-- 弹窗 -->
    <el-dialog
      v-el-drag-dialog
      :title="textMap[dialogStatus]"
      :visible.sync="dialogFormVisible"
      top="5vh"
      @dragDialog="handleDrag"
    >
      <el-form
        ref="dataForm"
        :rules="rules"
        :model="temp"
        label-position="left"
        label-width="100px"
        style="width: 400px; margin-left: 50px"
      >
        <el-form-item label="栏目名称">
          <el-input v-model="temp.title" />
        </el-form-item>
        <el-form-item label="样式">
          <el-select
            v-model="temp.StyleName"
            class="filter-item"
            placeholder="请选择"
          >
            <el-option
              v-for="item in statusOptions"
              :key="item"
              :label="item"
              :value="item"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="是否启用">
          <span> <el-switch v-model="temp.Enabled" /></span>
        </el-form-item>
      </el-form>
      <div slot="footer" class="dialog-footer">
        <el-button @click="dialogFormVisible = false"> 取消 </el-button>
        <el-button
          type="primary"
          @click="dialogStatus === 'create' ? createData() : updateData()"
        >
          确认
        </el-button>
      </div>
    </el-dialog>
  </div>
</template>

<script>
import { createTemplate, fetchTemplate, updateTemplate, deleteTemplate } from '@/api/template'
import elDragDialog from '@/directive/el-drag-dialog' // 自定义指令,可拖拽的dialog
import waves from '@/directive/waves'
import { parseTime } from '@/utils'
import Pagination from '@/components/Pagination' // 基于分页的二级包装

export default {
  name: 'Tags',
  components: { Pagination },
  directives: { waves, elDragDialog },
  filters: {
    statusFilter(status) {
      const statusMap = {
        published: 'success',
        draft: 'info',
        deleted: 'danger'
      }
      return statusMap[status]
    },
    typeFilter(type) {
      return '精选内容'
    }
  },
  data() {
    return {
      tableKey: 0,
      list: null,
      total: 0,
      listLoading: true,
      listQuery: {
        pageIndex: 1,
        pageSize: 20,
        keyWord: undefined,
        source: ''
      },
      statusOptions: ['StyleName'],
      temp: {
        id: undefined,
        title: '',
        StyleName: 'StyleName',
        Enabled: true
      },
      dialogFormVisible: false,
      dialogStatus: '',
      textMap: {
        update: '修改',
        create: '添加'
      },
      dialogPvVisible: true,
      pvData: [],
      rules: {
        BannerName: [
          { required: true, message: '请输入栏目名称', trigger: 'change' }
        ]
      },
      downloadLoading: false
    }
  },
  created() {
    this.getList()
  },
  methods: {
    // 查
    getList() {
      this.listLoading = true



      this.listLoading = false
      this.list = [{
        title: 'jam',
        addTime:'2021-12-12'
      }]
      this.total = 0


      // fetchTemplate(this.listQuery).then((response) => {
      //   this.list = response.data.list
      //   this.total = response.data.totalCount
      //   this.listLoading = false
      // })
    },
    handleFilter() {
      this.listQuery.page = 1
      this.getList()
    },
    handleModifyStatus(row, status) {
      this.$message({
        message: '操作Success',
        type: 'success'
      })
      row.status = status
    },
    resetTemp() {
      this.temp = {
        id: undefined,
        BannerName: '',
        StyleName: 'StyleName',
        Enabled: true
      }
    },
    handleCreate() {
      this.resetTemp()
      this.dialogStatus = 'create'
      this.dialogFormVisible = true
      this.$nextTick(() => {
        this.$refs['dataForm'].clearValidate()
      })
    },
    // 增
    createData() {
      this.$refs['dataForm'].validate((valid) => {
        if (valid) {
          console.log(this.temp)
          const _temp = JSON.parse(JSON.stringify(this.temp))
          delete _temp.id


          //模拟
          this.list.push(this.temp)
          this.dialogFormVisible = false


          // createTemplate(_temp).then(() => {
          //   this.getList()
          //   this.dialogFormVisible = false
          //   this.$notify({
          //     title: '成功',
          //     message: '创建成功',
          //     type: 'success',
          //     duration: 2000
          //   })
          // })
        }
      })
    },
    handleUpdate(row) {
      this.temp = Object.assign({}, row) // copy obj
      this.temp.timestamp = new Date(this.temp.timestamp)
      this.dialogStatus = 'update'
      this.dialogFormVisible = true
      this.$nextTick(() => {
        this.$refs['dataForm'].clearValidate()
      })
    },
    // 改
    updateData() {
      this.$refs['dataForm'].validate((valid) => {
        if (valid) {
          const tempData = Object.assign({}, this.temp)
          tempData.timestamp = +new Date(tempData.timestamp) // change Thu Nov 30 2017 16:41:05 GMT+0800 (CST) to 1512031311464
          updateTemplate(tempData).then(() => {
            const index = this.list.findIndex((v) => v.id === this.temp.id)
            this.list.splice(index, 1, this.temp)
            this.dialogFormVisible = false
            this.$notify({
              title: 'Success',
              message: 'Update Successfully',
              type: 'success',
              duration: 2000
            })
          })
        }
      })
    },
    // 删
    handleDelete(row, index) {
      this.$notify({
        title: 'Success',
        message: 'Delete Successfully',
        type: 'success',
        duration: 2000
      })
      this.list.splice(index, 1)
      deleteTemplate()
    },

    formatJson(filterVal) {
      return this.list.map((v) =>
        filterVal.map((j) => {
          if (j === 'timestamp') {
            return parseTime(v[j])
          } else {
            return v[j]
          }
        })
      )
    },
    handleDrag() {
      this.$refs.select.blur()
    }
  }
}
</script>
