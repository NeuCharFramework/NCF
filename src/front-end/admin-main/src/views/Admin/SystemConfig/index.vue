<template>
  <div class="el-main">
    <div class="admin-systemconfig-info">
      <h2 class="system-info">基础信息</h2>
      <!--  -->
      <el-table :data="tableData" border style="width: 100%">
        <el-table-column align="center" label="系统名称">
          <template slot-scope="scope">
            {{ scope.row.systemName }}
          </template>
        </el-table-column>
        <el-table-column align="center" label="模块发布模式">
          <template slot-scope="scope">
            {{ scope.row.hideModuleManager }}
          </template>
        </el-table-column>
        <el-table-column label="操作" align="center">
          <template slot-scope="scope">
            <el-button
              size="mini"
              type="primary"
              @click="handleEdit(scope.$index, scope.row)"
              >编辑</el-button
            >
          </template>
        </el-table-column>
      </el-table>

      <!--编辑、新增 -->
      <el-dialog
        :title="dialog.title"
        :visible.sync="dialog.visible"
        :close-on-click-modal="false"
      >
        <el-form
          ref="dataForm"
          :rules="dialog.rules"
          :model="dialog.data"
          label-position="left"
          label-width="100px"
          style="max-width: 400px; margin-left: 50px"
        >
          <el-form-item label="系统名称" prop="systemName">
            <el-input
              v-model="dialog.data.systemName"
              clearable
              placeholder="请输入系统名称"
            />
          </el-form-item>
        </el-form>
        <div slot="footer" class="dialog-footer">
          <el-button @click="dialog.visible = false">取 消</el-button>
          <el-button :loading="updateLoading" type="primary" @click="updateData"
            >确 认</el-button
          >
        </div>
      </el-dialog>
    </div>
  </div>
</template>

<script>
import { getSystemConfig, setSystemConfig } from "@/api/system";
import { isHaveToken } from "@/utils/auth";
export default {
  name: "SystemConfigIndex",
  data() {
    return {
      // 分页接口传参（只会有一个）
      listQuery: {
        handler: "List",
        pageIndex: 1,
        pageSize: 20,
      },
      tableData: [],
      dialog: {
        title: "编辑系统信息",
        visible: false,
        data: {
          id: 0,
          systemName: "",
        },
        rules: {
          systemName: [
            { required: true, message: "用户名为必填项", trigger: "blur" },
          ],
        },
      },
      updateLoading: false, // 确认loading按钮
    };
  },
  computed: {},
  watch: {
    "dialog.visible"(val, old) {
      // 关闭dialog，清空
      if (!val) {
        this.dialog.data = {
          id: 0,
          systemName: "",
        };
        this.updateLoading = false;
        this.$refs["dataForm"].resetFields();
      }
    },
  },
  created() {
    this.getList();
    isHaveToken();
  },
  methods: {
    // 获取数据
    async getList() {
      const res = await getSystemConfig(this.listQuery);
      if (res.data) {
        this.tableData = res.data || [];
      } else {
        this.$message.error("获取数据失败");
      }
    },
    // 编辑
    handleEdit(index, row) {
      this.dialog.visible = true;
      if (row) {
        // 编辑
        const { systemName, id } = row;
        this.dialog.data = {
          systemName,
          id,
        };
        this.dialog = Object.assign({}, this.dialog);
      }
    },
    // 更新新增编辑
    updateData() {
      this.$refs["dataForm"].validate((valid) => {
        // 表单校验
        if (valid) {
          this.updateLoading = true;
          const data = {
            id: this.dialog.data.id,
            SystemName: this.dialog.data.systemName,
          };
          setSystemConfig(data)
            .then((res) => {
              if (res.success) {
                this.getList();
                this.$notify({
                  title: "Success",
                  message: "更新成功！",
                  type: "success",
                  duration: 2000,
                });
                this.dialog.visible = false;
              } else {
                this.$notify({
                  title: "Faild",
                  message: "更新失败：" + res.data.msg,
                  type: "success",
                  duration: 2000,
                });
              }
              this.updateLoading = false;
            })
            .catch(() => {
              this.updateLoading = false;
            });
        }
      });
    },
  },
};
</script>

<style lang="scss" scoped>
.admin-systemconfig-info h2 {
  margin-bottom: 22px;
}

h2.system-info {
  margin-top: 10px;
}
</style>
