import Logo from './components/Logo.vue'
import routes from './router/routes'
import storeModule from './store/store-module'

export default function (Vue) {
    Vue.config.productionTip = true
    console.info('this is7: ', this);
    console.info('Vue is7: ', Vue);
    console.log('routes', routes);
    console.log('$layout', this.$root.$layout)
    routes.map(item => {
        this.$root.$layout.push(item)
    })
    console.log('$layout2', this.$root.$layout)
    this.$router.addRoutes(routes)
    this.$store.registerModule('main', storeModule)
    this.$eventBus.on('visitedAbout', () => {
        alert('用户访问了about页面。')
    })
    this.$dynamicComponent.create(Logo)
}
