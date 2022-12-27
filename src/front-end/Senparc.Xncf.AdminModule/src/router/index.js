import Vue from 'vue'
import VueRouter from 'vue-router'
import Home from '../views/Home.vue'

Vue.use(VueRouter)

let routes = [
  {
    path: '/',
    name: 'home',
    component: Home
  },
  {
    path: '/appstore',
    component: () =>
      import('../views/XncfStore/shopping/Index.vue')
  }
]
const router = new VueRouter({
  routes
})

export default router
