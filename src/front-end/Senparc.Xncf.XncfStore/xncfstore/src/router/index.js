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
    // 平台账号配置
    path: '/cells',
    component: () => import('../views/Senparc.Xncf.XncfStore/cells/index.vue')
  },
  {
    // 商城首页
    path: '/shopping',
    component: () => import('../views/Senparc.Xncf.XncfStore/shopping/Index.vue')
  },
  {
    // 商城详情页
    path: '/shoppingdetail',
    component: () => import('../views/Senparc.Xncf.XncfStore/shopping/detail.vue')
  },
  {
    // 个人应用首页
    path: '/applicationall',
    component: () => import('../views/Senparc.Xncf.XncfStore/shopping/applicationall.vue')
  },
  {
    // 个人应用详情
    path: '/application',
    component: () => import('../views/Senparc.Xncf.XncfStore/shopping/application.vue')
  },
  {
    // 运营与配置-应用中枢
    path: '/operationapplication',
    component: () => import('../views/Senparc.Xncf.XncfStore/operation/application.vue')
  },
  {
    // 运营与配置-自定义菜单
    path: '/customizeMenu',
    component: () => import('../views/Senparc.Xncf.XncfStore/operation/customizeMenu.vue')
  },
  {
    // 运营与配置-运行监测
    path: '/functions',
    component: () => import('../views/Senparc.Xncf.XncfStore/operation/function.vue')
  },
  {
    // 运营与配置-关键字回复
    path: '/keywordReply',
    component: () => import('../views/Senparc.Xncf.XncfStore/operation/keywordReply.vue')
  },
  {
    // 运营与配置-运营设置
    path: '/settigs',
    component: () => import('../views/Senparc.Xncf.XncfStore/operation/settigs.vue')
  },
  {
    // 运营与配置-素材管理
    path: '/sourceMaterial',
    component: () => import('../views/Senparc.Xncf.XncfStore/operation/sourceMaterial.vue')
  },
  {
    // 运营与配置-已订阅内容
    path: '/subscribed',
    component: () => import('../views/Senparc.Xncf.XncfStore/operation/subscribed.vue')
  },
  {
    // 运营与配置-NeuChar Cell时光机
    path: '/timeMachine',
    component: () => import('../views/Senparc.Xncf.XncfStore/operation/timeMachine.vue')
  },
  // {
  //   // demo1
  //   path: '/antv',
  //   component: () => import('../views/Senparc.Xncf.XncfStore/lightdemo/antv.vue')
  // },
  // {
  //   // demo2
  //   path: '/antvShow',
  //   component: () => import('../views/Senparc.Xncf.XncfStore/lightdemo/antvShow.vue')
  // },
  // {
  //   // 开发者中心
  //   path: '/antvShow',
  //   component: () => import('../views/Senparc.Xncf.XncfStore/developer/index.vue')
  // },
  {
    // 开发者信息
    path: '/developInfo',
    component: () => import('../views/Senparc.Xncf.XncfStore/developer/developInfo.vue')
  },
  {
    // 项目管理
    path: '/admin',
    component: () => import('../views/Senparc.Xncf.XncfStore/developer/administration/index.vue')
  },
  {
    // 项目应用
    path: '/appration',
    component: () => import('../views/Senparc.Xncf.XncfStore/developer/administration/appration.vue')
  },
  {
    // 订阅记录
    path: '/recordindex',
    component: () => import('../views/Senparc.Xncf.XncfStore/developer/record/index.vue')
  },
  {
    // 积分记录
    path: '/recordintegral',
    component: () => import('../views/Senparc.Xncf.XncfStore/developer/record/integral.vue')
  },


]

const router = new VueRouter({
  routes
})

export default router
