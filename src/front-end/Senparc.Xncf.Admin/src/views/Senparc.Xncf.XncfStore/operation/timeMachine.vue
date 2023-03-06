<template>
  <!-- NeuChar Cell时光机 -->
  <div class="timer">
    <el-card>
      <div class="title">
        <h3>NeuChar Cell时光机</h3>
        <div class="use">
          <i class="el-icon-edit"></i>
          <span>使用指南</span>
        </div>
      </div>
    </el-card>
    <el-card>
      <el-table :data="tableData" border style="width: 100%">
        <el-table-column prop="edition" label="版本"> </el-table-column>
        <el-table-column prop="automatic" label="自动回复数量">
        </el-table-column>
        <el-table-column prop="source " label="素材数量"> </el-table-column>
        <el-table-column prop="backups" label="备份时间"> </el-table-column>
        <el-table-column label="操作"> </el-table-column>
      </el-table>
    </el-card>
    <!-- 页脚组件 -->
    <footerinfo />
    <!-- 反馈组件 -->
    <feedback />
  </div>
</template>

<script>
import feedback from "@/components/Feedback";
import footerinfo from "@/components/FooterInfo";

export default {
  components: {
    feedback,
    footerinfo,
  },
  data() {
    return {
      tableData: [
        {
          edition: "1",
          automatic: "2",
          source: "3",
          backups: "4",
        },
      ],
    };
  },
};
</script>

<style scoped lang="scss">
.timer {
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
}
</style>