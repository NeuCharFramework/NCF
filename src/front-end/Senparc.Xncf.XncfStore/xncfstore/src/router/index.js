import Vue from 'vue'
import VueRouter from 'vue-router'
import HomeView from '../views/HomeView.vue'

Vue.use(VueRouter)

const routes = [
  {
    path: '/',
    name: 'home',
    component: HomeView
  },
  {
    path: '/about',
    name: 'about',
    // route level code-splitting
    // this generates a separate chunk (about.[hash].js) for this route
    // which is lazy-loaded when the route is visited.
    component: () => import(/* webpackChunkName: "about" */ '../views/AboutView.vue')
  },
  {
    path: '/cells',
    name: 'cells',
    // route level code-splitting
    // this generates a separate chunk (about.[hash].js) for this route
    // which is lazy-loaded when the route is visited.
    component: () => import(/* webpackChunkName: "about" */ '../views/Senparc.Xncf.XncfStore/cells/index.vue')
  },
  // {
  //   path: '/jsplumb',
  //   name: 'jsplumb',
  //   // route level code-splitting
  //   // this generates a separate chunk (about.[hash].js) for this route
  //   // which is lazy-loaded when the route is visited.
  //   component: () => import(/* webpackChunkName: "about" */ '../views/Senparc.Xncf.XncfStore/lightdemo/antvShow.vue')
  // },
  // {
  //   path: '/AntV6X',
  //   name: 'AntV6X',
  //   component: () => import(/* webpackChunkName: "about" */ '../views/Senparc.Xncf.XncfStore/lightdemo/antv.vue')
  // },
  {
    path: '/shopping',
    name: 'shopping',
    // route level code-splitting
    // this generates a separate chunk (about.[hash].js) for this route
    // which is lazy-loaded when the route is visited.
    component: () => import(/* webpackChunkName: "about" */ '../views/Senparc.Xncf.XncfStore/shopping/Index.vue')
  },
  {
    path: '/shoppingdetail',
    name: 'shoppingdetail',
    // route level code-splitting
    // this generates a separate chunk (about.[hash].js) for this route
    // which is lazy-loaded when the route is visited.
    component: () => import(/* webpackChunkName: "about" */ '../views/Senparc.Xncf.XncfStore//shopping/detail.vue')
  },
  {
    path: '/application',
    name: 'application',
    // route level code-splitting
    // this generates a separate chunk (about.[hash].js) for this route
    // which is lazy-loaded when the route is visited.
    component: () => import(/* webpackChunkName: "about" */ '../views/Senparc.Xncf.XncfStore//shopping/application.vue')
  },

  // {
  //   path: '/cells',
  //   name: 'cells',
  //   // route level code-splitting
  //   // this generates a separate chunk (about.[hash].js) for this route
  //   // which is lazy-loaded when the route is visited.
  //   component: () => import(/* webpackChunkName: "about" */ '../views/Senparc.Xncf.XncfStore/cells/index.vue')
  // },
  // {
  //   path: '/cells',
  //   name: 'cells',
  //   // route level code-splitting
  //   // this generates a separate chunk (about.[hash].js) for this route
  //   // which is lazy-loaded when the route is visited.
  //   component: () => import(/* webpackChunkName: "about" */ '../views/Senparc.Xncf.XncfStore/cells/index.vue')
  // },
  // {
  //   path: '/cells',
  //   name: 'cells',
  //   // route level code-splitting
  //   // this generates a separate chunk (about.[hash].js) for this route
  //   // which is lazy-loaded when the route is visited.
  //   component: () => import(/* webpackChunkName: "about" */ '../views/Senparc.Xncf.XncfStore/cells/index.vue')
  // },
  // {
  //   path: '/cells',
  //   name: 'cells',
  //   // route level code-splitting
  //   // this generates a separate chunk (about.[hash].js) for this route
  //   // which is lazy-loaded when the route is visited.
  //   component: () => import(/* webpackChunkName: "about" */ '../views/Senparc.Xncf.XncfStore/cells/index.vue')
  // },
]

const router = new VueRouter({
  routes
})

export default router
