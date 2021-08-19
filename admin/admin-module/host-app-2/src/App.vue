<template>
  <div id="app">
    <!-- 加载动态组件 -->
    <component :is="$store.state.dynamicComponent.GLOBAL.logo"></component>

    <div id="nav">
      <template v-for="menu in menus">
        <router-link  :to="menu.path.length === 0 ? '/': menu.path" v-bind:key="menu.name">{{menu.name}}</router-link>
       |
      </template>
    </div>
    <button @click="refreshRouterMenu">刷新路由菜单</button>
    <button @click="addModule">加载模块</button>
    <button @click="removeModule">卸载模块</button>
    <button @click="trigger">触发事件</button>
    <router-view />
  </div>
</template>
<script>
export default {
  data() {
    return {
      menus: []
    }
  },
  created() {
    this.refreshRouterMenu();
    console.log(this.$router)
    console.log(this.$router.getRoutes)
  },
  methods: {
    refreshRouterMenu() {
      const _menus = [];
      const routes = this.$router.getRoutes();
      for (const key in routes) {
        const element = routes[key];
        _menus.push(element);
      }
      console.info(_menus);
      this.menus = _menus;
    },
    addModule() {
      this.$moduleLoader({
          d: 'http://localhost:8080/dist/d.umd.js'
      });
    },
    removeModule(){
      const routes = this.$router.getRoutes();
      console.log('routes',routes)
    },
    trigger() {
      setTimeout(() => {
        this.$eventBus.emit('visitedAbout', { d: new Date() })
      }, 500)
    }
  }
}
</script>
<style>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
}

#nav {
  padding: 30px;
}

#nav a {
  font-weight: bold;
  color: #2c3e50;
}

#nav a.router-link-exact-active {
  color: #42b983;
}
</style>
