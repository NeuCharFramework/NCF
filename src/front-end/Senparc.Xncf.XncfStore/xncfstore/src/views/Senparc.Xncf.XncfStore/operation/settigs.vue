<template>
  <!-- 运营设置 -->
  <div class="setting">
    <el-card>
      <div class="title">
        <h3>运营设置</h3>
        <div class="use">
          <i class="el-icon-edit"></i>
          <span>使用指南</span>
        </div>
      </div>
    </el-card>
    <el-card>
      <main class="main">
        <div class="operation">
          <el-button type="success">添加运营者</el-button>
          <el-button @click="editState" v-if="edit">编辑</el-button>
          <el-button @click="preservation" type="success" v-if="!edit"
            >保存编辑</el-button
          >
          <el-button @click="edit = true" type="danger" plain v-if="!edit"
            >取消编辑</el-button
          >
        </div>
        <div class="table">
          <el-table
            :data="tableData"
            style="width: 100%"
            :header-row-style="{ 'background-color': '#f5f5f5' }"
            :header-cell-style="{ 'background-color': 'transparent' }"
          >
            <el-table-column prop="date" label="运营者"> </el-table-column>
            <el-table-column prop="name" label="素材关键字回复">
            </el-table-column>
            <el-table-column prop="province" label="自定义菜单">
            </el-table-column>
            <el-table-column prop="city" label="运营设置"> </el-table-column>
            <el-table-column prop="address" label="应用中枢"> </el-table-column>
          </el-table>
        </div>
      </main>
    </el-card>
  </div>
</template>

<script>
export default {
  data() {
    return {
      tableData: [
        {
          date: "2016-05-03",
          name: "王小虎",
          province: "上海",
          city: "普陀区",
          address: "上海市普陀区金沙江路 1518 弄",
        },
      ],
      // 编辑按钮状态
      edit: true,
    };
  },
  methods: {
    // 点击编辑
    editState() {
      this.edit = false;
    },
    // 保存编辑
    preservation() {
      console.log("保存");
    },
  },
};
</script>

<style scoped lang="scss">
.setting {
  ::v-deep .el-card {
    margin: 10px;
  }

  .title {
    display: flex;
    align-items: center;

    .use {
      color: #37bc9b;
      margin: 0 10px;
    }
  }
  .main {
    .operation {
      // 穿透修改按钮颜色和间距
      ::v-deep .el-button--success {
        color: #fff;
        background-color: #37bc9b;
        border-color: transparent;
      }
      ::v-deep .el-button {
        padding: 7px 12px;
      }
    }
    .table {
      margin: 20px 0;
    }
  }
}
</style>