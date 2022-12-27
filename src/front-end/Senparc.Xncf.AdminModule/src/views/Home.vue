<template>
  <div class="app-container home">
    <h1>{{ title }}1</h1>
    <!-- 加载动态组件 -->
    <!--    <component :is="$store.state.dynamicComponent.GLOBAL.logo"></component>-->
    <HelloWorld msg="我是远程加载的!" />

    <el-button type="primary" @click="goshopping">去商城</el-button>
  </div>
</template>

<script>
// @ is an alias to /src
import HelloWorld from "@/components/HelloWorld.vue";

export default {
  name: "home",
  components: {
    HelloWorld,
  },
  data() {
    return {
      title: process.env.VUE_APP_TITLE,
    };
  },
  methods: {
    goshopping() {
      this.$router.push({
        path: "/appstore",
      });
    },
  },
};
</script>
