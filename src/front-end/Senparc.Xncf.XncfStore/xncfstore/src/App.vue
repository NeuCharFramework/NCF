<template>
  <div id="app">
    <nav>
      <!-- <router-link to="/">Home</router-link> |
      <router-link to="/about">About</router-link> |
      <router-link to="/cells">cells</router-link> | -->
      <router-link to="/shopping">商城首页</router-link> |
      <router-link to="/shoppingdetail">商城详情页</router-link> |
      <router-link to="/applicationall">个人应用总页</router-link> |
      <router-link to="/application">个人应用详情</router-link> |
    </nav>

    <router-view />
  </div>
</template>

<style lang="scss">
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
}

nav {
  padding: 30px;

  a {
    font-weight: bold;
    color: #2c3e50;

    &.router-link-exact-active {
      color: #42b983;
    }
  }
}
</style>
