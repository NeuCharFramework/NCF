<template>
  <div class="home">
    <h1>{{title}}1</h1>
    <div>777jam</div>
    <!-- 加载动态组件 -->
    <component :is="$store.state.dynamicComponent.GLOBAL.logo"></component>
    <HelloWorld msg="Vue + vue-module-loader = Micro Front-end 测试修改后的文件" />
  </div>
</template>

<script>
// @ is an alias to /src
import HelloWorld from '@/components/HelloWorld.vue'

export default {
  name: 'home',
  components: {
    HelloWorld
  },
  data() {
    return {
      title: process.env.VUE_APP_TITLE
    }
  }
}
</script>
