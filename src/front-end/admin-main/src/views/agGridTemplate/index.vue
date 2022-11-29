<template>
  <ag-grid-vue style="width: 500px; height: 200px" class="ag-theme-alpine" :column-defs="columnDefs" :row-data="rowData" />
</template>

<script>
import { AgGridVue } from 'ag-grid-vue'

export default {
  name: 'AdGrid',
  components: {
    AgGridVue
  },
  data() {
    return {
      // 列数据
      columnDefs: null,
      // 行数据
      rowData: null
    }
  },
  beforeMount() {
    this.columnDefs = [
      { field: 'make' },
      { field: 'model' },
      { field: 'price' }
    ]

    this.rowData = [
      { make: 'Toyota', model: 'Celica', price: 35000 },
      { make: 'Ford', model: 'Mondeo', price: 32000 },
      { make: 'Porsche', model: 'Boxster', price: 72000 }
    ]
  }
}
</script>
