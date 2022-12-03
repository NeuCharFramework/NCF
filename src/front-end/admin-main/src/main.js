import Vue from 'vue'

import Cookies from 'js-cookie'

import 'default-passive-events' // Passive Event Listeners是Chrome提出的一个新的浏览器特性
import 'normalize.css/normalize.css' // a modern alternative to CSS resets
import 'font-awesome/css/font-awesome.min.css'// font-awesome 导入font-awesome字体库 https://fontawesome.dashgame.com/
// 引入 ag-grid 表格插件 样式文件
// import 'ag-grid-community/dist/styles/ag-grid.css'
// import 'ag-grid-community/dist/styles/ag-theme-balham.css'

import Element from 'element-ui'
import './styles/element-variables.scss'
// import enLang from 'element-ui/lib/locale/lang/en'// 如果使用中文语言包请默认支持，无需额外引入，请删除该依赖

import '@/styles/index.scss' // global css

import App from './App'
import store from './store'
import router from './router'

import './icons' // icon
import './permission' // permission control
import './utils/error-log' // error log

import * as filters from './filters' // global filters

import vueModuleLoader from 'vue-module-loader'

import dayjs from 'dayjs' //日期、时间处理插件 https://dayjs.fenxianglu.cn/
Vue.filter('dateFormat', (originVal,format = "YYYY-MM-DD HH:mm:ss") => {
  // 直接调用dayjs()得到的是当前时间哟
  const dtStr = dayjs(originVal).format('YYYY-MM-DD HH:mm:ss')
  return dtStr
})
/**
 * If you don't want to use mock-server
 * you want to use MockJs for mock api
 * you can execute: mockXHR()
 *
 * Currently MockJs will be used in the production environment,
 * please remove it before going online ! ! !
 */
// if (process.env.NODE_ENV === 'production') {
//   const { mockXHR } = require('../mock')
//   mockXHR()
// }

Vue.use(Element, {
  size: Cookies.get('size') || 'medium' // set element-ui default size
  // locale: enLang // 如果使用中文，无需设置，请删除
})
Vue.use(vueModuleLoader, { store })
// register global utility filters
Object.keys(filters).forEach(key => {
  Vue.filter(key, filters[key])
})

Vue.config.productionTip = false

new Vue({
  el: '#app',
  router,
  store,
  render: h => h(App)
})
