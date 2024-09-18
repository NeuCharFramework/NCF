<template>
  <div class="el-main">
    <div class="admin-role">
      <div class="filter-container">
        <el-button
          class="filter-item"
          type="primary"
          icon="el-icon-plus"
          @click="handleEdit('', '', 'add')"
          >增加菜单
        </el-button>
      </div>
      <el-table
        :data="tableData"
        style="width: 100%; margin-bottom: 20px"
        row-key="id"
        border
        default-expand-all
        v-loading="tableLoading"
        :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
      >
        <el-table-column prop="menuName" align="left" label="菜单名称" />
        <el-table-column prop="sort" align="center" label="排序" sortable />
        <el-table-column prop="adminRemark" align="center" label="备注" />
        <el-table-column prop="remark" align="center" label="说明" />
        <el-table-column align="center" label="添加时间">
          <template slot-scope="scope">
            {{ scope.row.addTime | dateFormat }}
          </template>
        </el-table-column>
        <el-table-column label="操作" align="center">
          <template slot-scope="scope">
            <el-button
              size="mini"
              type="primary"
              @click="handleEdit(scope.$index, scope.row, 'edit')"
              >编辑
            </el-button>
            <el-button
              v-if="scope.row.children"
              size="mini"
              type="primary"
              @click="handleEdit(scope.$index, scope.row, 'addNext')"
              >增加下一级
            </el-button>
            <el-popconfirm
              placement="top"
              title="确认删除此菜单吗？"
              @confirm="handleDelete(scope.$index, scope.row)"
            >
              <el-button slot="reference" size="mini" type="danger"
                >删除</el-button
              >
            </el-popconfirm>
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
          :disabled="dialog.disabled"
          label-position="left"
          label-width="100px"
          style="max-width: 400px; margin-left: 50px"
        >
          <el-form-item label="菜单名称" prop="menuName">
            <el-input v-model="dialog.data.menuName" clearable placeholder="" />
          </el-form-item>
          <el-form-item label="父级菜单">
            <el-cascader
              v-model="dialog.data.parentId"
              :options="tableData"
              clearable
              :props="{
                checkStrictly: dialog.checkStrictly,
                label: 'menuName',
                value: 'id',
              }"
            />
          </el-form-item>
          <el-form-item label="排序">
            <el-input
              v-model="dialog.data.sort"
              type="number"
              clearable
              placeholder=""
            />
          </el-form-item>
          <el-form-item label="类型" prop="menuType">
            <el-radio-group v-model="dialog.data.menuType">
              <el-radio :label="1">菜单</el-radio>
              <el-radio :label="2">页面</el-radio>
              <el-radio :label="3">按钮</el-radio>
            </el-radio-group>
          </el-form-item>
          <el-form-item label="路径">
            <el-input v-model="dialog.data.url" clearable placeholder="" />
          </el-form-item>
          <el-form-item label="操作代码" prop="resourceCode">
            <el-input
              v-model="dialog.data.resourceCode"
              clearable
              placeholder=""
            />
          </el-form-item>
          <el-form-item label="是否锁定">
            <el-switch v-model="dialog.data.isLocked" />
          </el-form-item>
          <el-form-item label="是否显示">
            <el-switch v-model="dialog.data.visible" />
          </el-form-item>
          <el-form-item label="图标">
            <i v-if="dialog.data.icon" :class="'fa ' + dialog.data.icon" />
            <el-button
              plain
              style="margin-left: 15px"
              @click="openIconsDialog"
              >选择图标</el-button
            >
          </el-form-item>
        </el-form>
        <div slot="footer" class="dialog-footer">
          <el-button @click="dialog.visible = false">取 消</el-button>
          <el-button
            :loading="dialog.updateLoading"
            :disabled="dialog.disabled"
            type="primary"
            @click="updateData"
            >确 认
          </el-button>
        </div>
      </el-dialog>
      <!--选择图标-->
      <el-dialog title="图标列表" :visible.sync="dialogIcon.visible">
        <el-input v-model="dialogIcon.searchValue" clearable placeholder="搜索图标名..." @input="filterElIcons" style="margin-bottom: 5px;" />
        <div class="menu-icons-grid">
          <div
            v-for="(item, index) in dialogIcon.elementIcons"
            :key="`${item}-${index}`"
            class="menu-icon-item"
            @click="pickIcon(item)"
          >
            <i :class="'fa ' + item" />
            <span>{{ item }}</span>
          </div>
        </div>
      </el-dialog>
    </div>
  </div>
</template>

<script>
import {
  createOrUpdateMenu,
  getMenus,
  deleteMenu,
  getAllMenus,
  getFullMenus,
} from "@/api/menu";
import { isHaveToken } from "@/utils/auth";
import icons from "@/views/Admin/Menu/icons";

export default {
  name: "Index",
  data() {
    var validateCode = (rule, value, callback) => {
      if (this.dialog.data.menuType === 3) {
        if (!value) {
          callback(new Error("当类型是按钮类型时此项必填"));
        } else {
          callback();
        }
      } else {
        callback();
      }
    };
    return {
      // 表格数据
      tableData: [],
      tableLoading: true,
      dialog: {
        title: "新增菜单",
        visible: false,
        data: {
          id: "",
          menuName: "",
          parentId: [],
          url: "",
          icon: "",
          sort: "",
          visible: true,
          resourceCode: "",
          isLocked: false,
          menuType: "",
        },
        rules: {
          menuName: [
            { required: true, message: "菜单名称为必填项", trigger: "blur" },
          ],
          menuType: [
            { required: true, message: "类型为必选项", trigger: "blur" },
          ],
          resourceCode: [{ validator: validateCode, trigger: "blur" }],
        },
        updateLoading: false,
        disabled: false,
        checkStrictly: true, // 是否严格的遵守父子节点不互相关联
      },
      dialogIcon: {
        visible: false,
        searchValue: '',
        elementIcons: icons,
      },
    };
  },
  watch: {
    "dialog.visible"(val, old) {
      // 关闭dialog，清空
      if (!val) {
        this.dialog.data = {
          id: "",
          menuName: "",
          parentId: [],
          url: "",
          icon: "",
          sort: "",
          visible: false,
          resourceCode: "",
          isLocked: false,
          menuType: "",
        };
        this.dialog.updateLoading = false;
        this.dialog.disabled = false;
      }
    },
  },
  created() {
    this.getList();
    isHaveToken();
  },
  methods: {
    // 选取图标
    pickIcon(item) {
      this.dialogIcon.visible = false;
      this.dialog.data.icon = item;
    },
    // 获取所有菜单
    async getList() {
      this.tableLoading = true;
      const a = await getFullMenus();
      this.tableLoading = false;
      if (a.success) {
        const b = a.data || [];
        const allMenu = [];
        this.menuFormat(b, null, allMenu);
        this.tableData = allMenu;
        return;
      }
      this.$message.error("获取菜单信息失败");

      // const menusList = await getAllMenus({ hasButton: true })
      // const allMenu = menusList.data.items
      // this.menuDeWeight(allMenu)
      // console.log('menuDeWeight', allMenu)
      // await this.getMenuInfo(allMenu)
      // console.log('getMenuInfo', allMenu)
      // this.$nextTick(() => {
      //   console.log('1111111', allMenu)
      //   this.tableData = allMenu
      // })
      // this.ddd(b, null, allMenu)
      // this.tableData = allMenu
    },
    // 数据处理
    menuFormat(source, parentId, dest) {
      const array = source.filter((_) => _.parentId === parentId);
      array.sort((a, b) => a.sort - b.sort);
      for (let i in array) {
        const ele = array[i];
        ele.children = [];
        dest.unshift(ele);
        this.menuFormat(source, ele.id, ele.children);
      }
    },
    // 编辑 // 新增菜单 // 增加下一级
    handleEdit(index, row, flag) {
      this.dialog.visible = true;
      if (flag === "add") {
        // 新增
        this.dialog.title = "新增菜单";
        return;
      }
      // 编辑
      const {
        id,
        menuName,
        parentId,
        url,
        icon,
        sort,
        visible,
        resourceCode,
        isLocked,
        menuType,
      } = row;
      this.dialog.data = {
        id,
        menuName,
        parentId: [parentId],
        url,
        icon,
        sort,
        visible,
        resourceCode,
        isLocked,
        menuType,
      };
      // dialog中父级菜单 做递归显示
      const x = [];
      this.recursionFunc(row, this.tableData, x);
      this.dialog.data.parentId = x;
      // ////////////////////////////

      if (flag === "edit") {
        this.dialog.title = "编辑菜单";
        if (row.isLocked) {
          this.dialog.disabled = true;
        }
      } else if (flag === "addNext") {
        this.dialog.data.id = "";
        this.dialog.title = "增加下一级菜单";
        this.dialog.data.menuName = "";
        this.dialog.data.parentId.push(row.id);
      }
    },
    // 设置父级菜单默认显示 递归
    recursionFunc(row, source, dest) {
      if (row.parentId === null) {
        return;
      }
      for (const i in source) {
        const ele = source[i];
        if (row.parentId === ele.id) {
          this.recursionFunc(ele, this.tableData, dest);
          dest.push(ele.id);
        } else {
          this.recursionFunc(row, ele.children, dest);
        }
      }
    },
    // 更新新增、编辑
    updateData() {
      this.$refs["dataForm"].validate((valid) => {
        if (!this.dialog.data.icon) {
          this.$alert('请选择菜单图标', '', {
            confirmButtonText: '确定',
            callback: action => {
              this.openIconsDialog()
            }
          });
          return;
        }
        // 表单校验
        if (valid) {
          this.dialog.updateLoading = true;
          const data = {
            Id: this.dialog.data.id,
            MenuName: this.dialog.data.menuName,
            ParentId:
              this.dialog.data.parentId[this.dialog.data.parentId.length - 1],
            Url: this.dialog.data.url,
            Icon: this.dialog.data.icon.includes("fa ") ? this.dialog.data.icon : "fa " + this.dialog.data.icon,
            Sort: this.dialog.data.sort * 1,
            Visible: this.dialog.data.visible,
            ResourceCode: this.dialog.data.resourceCode,
            IsLocked: this.dialog.data.isLocked,
            MenuType: this.dialog.data.menuType,
          };
          createOrUpdateMenu(data)
            .then((res) => {
              this.$notify({
                title: "Success",
                message: "成功",
                type: "success",
                duration: 2000,
              });
              this.getList();
              this.dialog.visible = false;
            })
            .catch(() => {
              this.dialog.updateLoading = false;
            });
        }
      });
    },
    // 删除
    handleDelete(index, row) {
      if (!row.id) return;
      deleteMenu(row.id).then((res) => {
          this.$notify({
            title: "Success",
            message: "删除成功",
            type: "success",
            duration: 2000,
          });
          this.getList();
      });
    },
    openIconsDialog(){
      this.dialogIcon.visible = true
      this.dialogIcon.searchValue = '';
      this.dialogIcon.elementIcons = icons
    },
    //搜索图标
    filterElIcons(){
        this.dialogIcon.elementIcons=!this.dialogIcon.searchValue?icons:icons.filter(el=>el.includes(this.dialogIcon.searchValue))
    }
  },
};
</script>

<style lang="scss" scoped>
.menu-icons-grid{
  position:relative;
  display:grid;
  grid-template-columns:repeat(auto-fill,minmax(120px,1fr));
  grid-auto-rows:100px 100px;
  grid-gap: 20px;
  height:60vh;
  overflow-y:scroll;
  align-items: center;
  justify-items:center;
}
.menu-icon-item {
  height: 85px;
  text-align: center;
  width: 100px;
  font-size: 30px;
  color: rgb(36, 41, 46);
  cursor: pointer;
}
.menu-icon-item:hover {
  color: #409EFF
}
.menu-icon-item span {
  display: block;
  font-size: 16px;
  margin-top: 10px;
}
</style>
