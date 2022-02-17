<template>
  <div class="app-container">
    <div id="nav">
      <template v-for="menu in menus">
        <router-link :key="menu.name" :to="menu.path.length === 0 ? '/': menu.path">{{ menu.name }}</router-link>
        ——
      </template>
    </div>

    <div style="margin-top: 20px;">
      <el-button @click="addModule">加载模块</el-button>
      <el-button @click="refreshRouterMenu">刷新路由菜单</el-button>
<!--      <el-button @click="removeModule">卸载模块</el-button>-->
      <el-button @click="trigger">触发事件</el-button>
<!--      <el-button @click="reset">reset</el-button>-->
<!--      <el-button @click="hideMenu">只显示模块</el-button>-->
    </div>
  </div>
</template>

<script>
import { resetRouter, asyncRoutes } from '@/router'
import store from "@/store";
export default {
  name: 'ModuleHome',
  data() {
    return {
      menus: []
    }
  },
  created() {

    this.refreshRouterMenu()
  },
  methods: {
    hideMenu(){
      const routes = this.$router.getRoutes()
      this.$store.dispatch('permission/setRoutes', routes)
    },
    reset() {
      // resetRouter()
      console.log('asyncRoutes', asyncRoutes)
      const routes = this.$router.getRoutes()
      console.log('routes', routes)

    },
    handleSetLineChartData(type) {
      this.lineChartData = lineChartData[type]
    },
    refreshRouterMenu() {
      const _menus = []
      const routes = this.$router.getRoutes()
      for (const key in routes) {
        const element = routes[key]
        _menus.push(element)
      }
      console.info(_menus)
      this.menus = _menus

      this.reset()
      // this.$store.dispatch('permission/setRoutes', routes)
      console.log('$store77777777777', this.$store.state.permission.routes)
    },
    addModule() {
      this.$moduleLoader({
        d: 'http://localhost:9527/dist/d.umd.js'
      }).then(() => {
        // 加载过程完毕
        console.log('加载过程完毕')
        this.hideMenu()
      })
    },
    removeModule() {
      const routes = this.$router.getRoutes()
      console.log('routes', routes)
    },
    trigger() {
      setTimeout(() => {
        this.$eventBus.emit('visitedAbout', { d: new Date() })
      }, 500)
    }
  }
}
</script>

<style scoped>

</style>
