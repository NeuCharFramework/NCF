import Vue from 'vue'
import Router from 'vue-router'

Vue.use(Router)

/* Layout */
import Layout from '@/layout'

/* Router Modules */
import moduleRouter from '@/router/modules/module'
import chartsRouter from '@/router/modules/charts'

/**
 * Note: sub-menu only appear when route children.length >= 1
 * Detail see: https://panjiachen.github.io/vue-element-admin-site/guide/essentials/router-and-nav.html
 *
 * hidden: true                   if set true, item will not show in the sidebar(default is false)
 * alwaysShow: true               if set true, will always show the root menu
 *                                if not set alwaysShow, when item has more than one children route,
 *                                it will becomes XncfModule mode, otherwise not show the root menu
 * redirect: noRedirect           if set noRedirect will no redirect in the breadcrumb
 * name:'router-name'             the name is used by <keep-alive> (must set!!!)
 * meta : {
    roles: ['admin','editor']    control the page roles (you can set multiple roles)
    title: 'title'               the name show in sidebar and breadcrumb (recommend set)
    icon: 'svg-name'/'el-icon-x' the icon show in the sidebar
    noCache: true                if set true, the page will no be cached(default is false)
    affix: true                  if set true, the tag will affix in the tags-view
    breadcrumb: false            if set false, the item will hidden in breadcrumb(default is true)
    activeMenu: '/example/list'  if set path, the sidebar will highlight the path you set
  }
 */

/**
 * constantRoutes
 * a base page that does not have permission requirements
 * all roles can be accessed
 */
export const constantRoutes = [
  {
    path: '/redirect',
    component: Layout,
    hidden: true,
    children: [
      {
        path: '/redirect/:path(.*)',
        component: () => import('@/views/redirect/index')
      }
    ]
  },
  {
    path: '/login',
    component: () => import('@/views/login/index'),
    hidden: true
  },
  {
    path: '/auth-redirect',
    component: () => import('@/views/login/auth-redirect'),
    hidden: true
  },
  {
    path: '/404',
    component: () => import('@/views/error-page/404'),
    hidden: true
  },
  {
    path: '/401',
    component: () => import('@/views/error-page/401'),
    hidden: true
  },
  {
    path: '/',
    component: Layout,
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        component: () => import('@/views/dashboard/index'),
        name: 'Dashboard',
        meta: { title: 'Dashboard', icon: 'dashboard', affix: true }
      }
    ]
  },
  {
    path: '/Admin',
    component: Layout,
    name: 'Admin',
    meta: {
      title: '系统管理',
      icon: 'excel'
    },
    children: [
      {
        path: 'AdminUserInfo',
        component: () => import('@/views/Admin/AdminUserInfo/index'),
        name: '管理员管理',
        meta: { title: '管理员管理' }
      },
      {
        path: 'Role',
        component: () => import('@/views/Admin/Role/index'),
        name: '角色管理',
        meta: { title: '角色管理' }
      },
      {
        path: 'Menu',
        component: () => import('@/views/Admin/Menu/index'),
        name: '菜单管理',
        meta: { title: '菜单管理' }
      },
      {
        path: 'SystemConfig',
        component: () => import('@/views/Admin/SystemConfig/index'),
        name: '系统信息',
        meta: { title: '系统信息' }
      },
      {
        path: 'TenantInfo',
        component: () => import('@/views/Admin/TenantInfo/index'),
        name: '多租户信息',
        meta: { title: '多租户信息' }
      }
    ]
  },
  {
    path: '/template',
    component: Layout,
    name: 'Template',
    meta: {
      title: '模板页',
      icon: 'excel'
    },
    children: [
      {
        path: 'template',
        component: () => import('@/views/template/template'),
        name: '模板页',
        meta: { title: '模板页' }
      }
    ]
  }
]

/**
 * asyncRoutes
 * the routes that need to be dynamically loaded based on user roles
 */
export const asyncRoutes = [
  // {
  //   path: '/XncfModule',
  //   component: Layout,
  //   redirect: '/XncfModule/menu1/menu1-1',
  //   name: '拓展模块',
  //   meta: {
  //     title: '拓展模块',
  //     icon: 'excel'
  //   },
  //   children: [
  //     {
  //       path: 'index',
  //       name: '模块管理',
  //       component: () => import('@/views/Admin/XncfModule/menu2/index'),
  //       meta: { title: '模块管理' }
  //     },
  //     {
  //       path: 'menu1',
  //       component: () => import('@/views/Admin/XncfModule/menu1/index'), // Parent router-view
  //       name: 'NCF 系统后台',
  //       meta: { title: 'NCF 系统后台' },
  //       redirect: '/XncfModule/menu1/menu1-1',
  //       children: [
  //         {
  //           path: 'menu1-1',
  //           component: () => import('@/views/Admin/XncfModule/menu1/menu1-1'),
  //           name: '设置/执行',
  //           meta: { title: '设置/执行' }
  //         },
  //         {
  //           path: 'menu1-3',
  //           component: () => import('@/views/Admin/XncfModule/menu1/menu1-3'),
  //           name: '菜单管理',
  //           meta: { title: '菜单管理' }
  //         },
  //         {
  //           path: 'SenparcTrace',
  //           component: () => import('@/views/Admin/XncfModule/menu1/menu1-3'),
  //           name: 'SenparcTrace 日志',
  //           meta: { title: 'SenparcTrace 日志' }
  //         }
  //       ]
  //     }
  //   ]
  // },

  // chartsRouter,
  moduleRouter,
  // 404 page must be placed at the end !!!
  { path: '*', redirect: '/404', hidden: true }
]

const createRouter = () => new Router({
  // mode: 'history', // require service support
  scrollBehavior: () => ({ y: 0 }),
  routes: constantRoutes
})

const router = createRouter()

// Detail see: https://github.com/vuejs/vue-router/issues/1234#issuecomment-357941465
export function resetRouter() {
  const newRouter = createRouter()
  router.matcher = newRouter.matcher // reset router
}

export default router
