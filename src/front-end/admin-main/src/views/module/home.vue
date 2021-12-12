<template>
  <div class="app-container">
    <div id="nav">
      <template v-for="menu in menus">
        <router-link :key="menu.name" :to="menu.path.length === 0 ? '/': menu.path">{{ menu.name }}</router-link>
        ——
      </template>
    </div>
    <div style="margin-top: 20px;">
      <button @click="addModule">加载模块</button>
      <button @click="refreshRouterMenu">刷新路由菜单</button>
      <button @click="removeModule">卸载模块</button>
      <button @click="trigger">触发事件</button>
      <button @click="reset">reset</button>
    </div>
  </div>
</template>

<script>
import { resetRouter, asyncRoutes } from '@/router'
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
    reset() {
      // resetRouter()
      console.log('asyncRoutes', asyncRoutes)
      const routes = this.$router.getRoutes()
      console.log('routes', routes)
      this.$store.dispatch('permission/setRoutes', routes)
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
    },
    addModule() {
      this.$moduleLoader({
        d: 'http://localhost:9527/dist/d.umd.js'
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
