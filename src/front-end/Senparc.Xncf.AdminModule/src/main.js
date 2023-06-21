import Vue from 'vue'
import App from './App.vue'
import router from './router'
import store from './store'
// 完整引入element-ui
import ElementUI from 'element-ui';
import 'element-ui/lib/theme-chalk/index.css';

Vue.use(ElementUI);
const app = new Vue({
  router,
  store,
  render: h => h(App)
});
app.$mount('#app')
