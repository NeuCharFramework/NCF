<template>
  <!-- 应用中枢 -->
  <div class="application">
    <!-- 标题 -->
    <nav class="nav">
      <h3 class="shoptit">NeuChar Group222应用中枢</h3>
      <div class="discover">
        <i class="el-icon-discover"></i>
        <span>使用指南</span>
      </div>
    </nav>
    <main class="main">
      <div class="mainTitle">
        <el-button type="primary">新增</el-button>
        <el-button type="danger">删除</el-button>
        <el-input
          placeholder="请输入内容"
          v-model="input3"
          class="input-with-select"
        >
          <el-button
            slot="append"
            icon="el-icon-search"
            @click="search"
          ></el-button>
        </el-input>
      </div>
      <div class="mainCenter">
        <el-table
          ref="multipleTable"
          :data="tableData"
          tooltip-effect="dark"
          style="width: 100%"
          @selection-change="handleSelectionChange"
          border
        >
          <el-table-column type="selection"> </el-table-column>
          <el-table-column label="名称" >
            <template slot-scope="scope">{{ scope.row.date }}</template>
          </el-table-column>
          <el-table-column prop="name" label="是否允许重复">
          </el-table-column>
          <el-table-column prop="address" label="添加事件" show-overflow-tooltip>
          </el-table-column>
          <el-table-column  label="操作">
          </el-table-column>
        </el-table>
        <div style="margin-top: 20px">
          <el-button @click="toggleSelection([tableData[1], tableData[2]])">切换第二、第三行的选中状态</el-button>
          <el-button @click="toggleSelection()">取消选择</el-button>
        </div>
      </div>
    </main>
  </div>
</template>

<script>
export default {
  data() {
    return {
      input3: "", //搜索的输入框
      //   表单数据
      tableData: [
        {
          date: "2016-05-03",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-02",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-04",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-01",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-08",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-06",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
        {
          date: "2016-05-07",
          name: "王小虎",
          address: "上海市普陀区金沙江路 1518 弄",
        },
      ],
      multipleSelection: [],
    };
  },

  methods: {
    // 搜索
    search() {
      console.log("iptvalue", this.input3);
    },
    toggleSelection(rows) {
      if (rows) {
        rows.forEach((row) => {
          this.$refs.multipleTable.toggleRowSelection(row);
        });
      } else {
        this.$refs.multipleTable.clearSelection();
      }
    },
    handleSelectionChange(val) {
      this.multipleSelection = val;
    },
  },
};
</script>

<style scoped lang="scss">
.application {
  .nav {
    border-bottom: 1px solid #cfdbe2;
    display: flex;
    align-items: center;
    justify-content: center;

    .shoptit {
      font-size: 24px;
      background-color: #ffffff;
      color: #929292;
      font-weight: normal;
      padding: 15px;
    }
    .discover {
      color: #37bc9b;
    }
  }
  .main {
    .mainTitle {
      display: flex;
      align-items: center;
      justify-content: flex-start;
      padding: 10px;
    }
    .mainCenter {
      padding: 10px;
    }
  }
}
</style>
<style scoped>
.main >>> .el-input {
  margin: 0px 10px;
  width: 300px;
}
</style>