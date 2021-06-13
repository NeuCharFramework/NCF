<template>
	<div class="filter-items">
    <template v-for="btn of buttons">
      <json-excel
        :key="btn.Id"
        v-if="btn.domId === 'btnExport'"
        :fetch="fetchData"
        :before-finish = "finishDownload"
        class = "export-excel-wrapper"
        :fields = "excelInfo.json_fields"
        :name = "excelInfo.excelName">
        <el-button :type="btn.class || 'primary'" size="mini" style="margin-left: 10px;" :loading="excelLoading">导出EXCEL</el-button>
      </json-excel>
      <el-button v-else :type="btn.class" :size="size" v-bind:key="btn.Id" class="filter-item" @click="$emit('btn-event',btn.domId)"><i :class="`iconfont icon-${btn.icon}`"></i>{{btn.name}}</el-button>
    </template>
	</div>
</template>

<script>
import waves from '@/directive/waves' // 水波纹指令
import JsonExcel from 'vue-json-excel'
export default {
  name: 'permission-btn',
  components: {
    JsonExcel
  },
  props: {
    moduleName: {
      type: String,
      required: true
    },
    size: {
      type: String,
      default: ''
    }
  },
  data() {
    // todo:兼容layui的样式、图标
    return {
      excelInfo: {
        json_fields: {},
        excelName: '',
        data: []
      },
      excelLoading: false,
      buttons: []
    }
  },
  computed: {},
  directives: {
    waves
  },
  mounted() {
    var route = this.$route
    var elements = route.meta.elements
    this.buttons = elements.sort((a, b) => {
      return a.sort - b.sort
    })
  },
  methods: {
    fetchData() {
      if(this.excelLoading) return
      this.excelLoading = true
      this.$emit('btn-event','btnExport', (val) => {
        this.excelInfo = { ...val }
      })
      return this.excelInfo.data
    },
    finishDownload(){
      this.excelLoading = false
    }
  }
}
</script>

<style lang="scss" scoped>
</style>
