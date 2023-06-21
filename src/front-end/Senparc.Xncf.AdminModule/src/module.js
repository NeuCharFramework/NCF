import Logo from './components/Logo.vue'
import routes from './router/routes'
import storeModule from './store/store-module'
export default function(Vue) {
  Vue.config.productionTip = true
  console.info('编译完成');
  // this.$router.addRoutes(routes)
  routes.forEach(ele => {
    this.$router.addRoute('Module', ele)
  })
  console.info('routes', routes)
  this.$store.registerModule('main', storeModule)
  this.$eventBus.on('visitedAbout', (e) => {
    console.info(e);
    alert('visitedAbout被触发了！')
  })
}
