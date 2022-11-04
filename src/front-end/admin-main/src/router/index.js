import Vue from "vue";
import Router from "vue-router";
/* Layout */
import Layout from "@/layout";

/* Router Modules */
// import moduleRouter from "@/router/modules/module";

Vue.use(Router);


/**
 * Note: sub-menu only appear when route children.length >= 1
 * Detail see: https://panjiachen.github.io/vue-element-admin-site/guide/essentials/router-and-nav.html
 *
 * hidden: true                   如果设置为true，项目将不会显示在侧边栏中(默认为false)
 * alwaysShow: true               如果设置为true，将始终显示根菜单
 *                                如果没有设置alwaysShow，当item有多个子路由时，
 *                                它将变成XncfModule模式，否则不会显示根菜单
 * redirect: noRedirect           如果设置了noRedirect，则breadcrumb中不会有重定向
 * name:'router-name'             该名称由使用(必须设置!!)
 * meta : {
    roles: ['admin','editor']    控制页面角色(可以设置多个角色)
    title: 'title'               在侧边栏和面包屑中显示的名称(推荐设置)
    icon: 'svg-name'/'el-icon-x' 图标显示在侧边栏中
    noCache: true                如果设置为true，页面将不会被缓存(默认为false)
    affix: true                  如果设置为true，标签将被添加到标签视图中
    breadcrumb: false            如果设置为false，项目将隐藏在breadcrumb中(默认为true)
    activeMenu: '/example/list'  如果设置了path，侧边栏将突出显示您设置的路径
  }
 */

/**
 * constantRoutes
 * a base page that does not have permission requirements
 * all roles can be accessed
 */
export const constantRoutes = [
  {
    path: "/redirect",
    component: Layout,
    hidden: true,
    children: [
      {
        path: "/redirect/:path(.*)",
        component: () => import("@/views/redirect/index"),
      },
    ],
  },
  {
    path: "/login",
    component: () => import("@/views/login/index"),
    hidden: true,
  },
  {
    path: "/auth-redirect",
    component: () => import("@/views/login/auth-redirect"),
    hidden: true,
  },
  {
    path: "/404",
    component: () => import("@/views/error-page/404"),
    hidden: true,
  },
  {
    path: "/401",
    component: () => import("@/views/error-page/401"),
    hidden: true,
  },
  {
    path: "/",
    component: Layout,
    redirect: "/dashboard",
    children: [
      {
        path: "dashboard",
        component: () => import("@/views/dashboard/index"),
        name: "Dashboard",
        meta: { title: "首页", icon: "fa fa-home", affix: true },
      },
    ],
  },
  
  // {
  //   path: '/AgGridTemplate',
  //   component: Layout,
  //   name: 'AgGridTemplate',
  //   meta: {
  //     title: 'aGgrid模板页',
  //     icon: 'excel'
  //   },
  //   children: [
  //     {
  //       path: '/AgGridTemplate/index',
  //       component: () => import('@/views/agGridTemplate/index'),
  //       name: 'AgGridTemplateIndex',
  //       meta: { title: 'aGgrid模板页模板页' }
  //     }
  //   ]
  // }
];

/**
 * asyncRoutes
 * the routes that need to be dynamically loaded based on user roles
 */
export const asyncRoutes = [
  // {
  //   path: "/Admin",
  //   component: Layout,
  //   redirect: "/Admin/AdminUserInfo/Index",
  //   name: "Admin",
  //   meta: {
  //     title: "系统管理",
  //     icon: "excel",
  //   },
  //   children: [
  //     {
  //       path: "/Admin/AdminUserInfo/Index",
  //       component: () => import("@/views/Admin/AdminUserInfo/Index"),
  //       name: "管理员管理",
  //       meta: { title: "管理员管理" },
  //     },
  //     {
  //       path: "/Admin/Role/Index",
  //       component: () => import("@/views/Admin/Role/Index"),
  //       name: "角色管理",
  //       meta: { title: "角色管理" },
  //     },
  //     {
  //       path: "/Admin/Menu/Index",
  //       component: () => import("@/views/Admin/Menu/Index"),
  //       name: "菜单管理",
  //       meta: { title: "菜单管理" },
  //     },
  //     {
  //       path: "/Admin/SystemConfig/Index",
  //       component: () => import("@/views/Admin/SystemConfig/Index"),
  //       name: "系统信息",
  //       meta: { title: "系统信息" },
  //     },
  //     {
  //       path: "/Admin/TenantInfo/Index",
  //       component: () => import("@/views/Admin/TenantInfo/Index"),
  //       name: "多租户信息",
  //       meta: { title: "多租户信息" },
  //     },
  //   ],
  // },

  // 拓展模块
  // moduleRouter,
  // 404 page must be placed at the end !!!
  { path: "*", redirect: "/404", hidden: true },
];

const createRouter = () =>
  new Router({
    // mode: 'history', // require service support
    scrollBehavior: () => ({ y: 0 }),
    routes: constantRoutes,
  });

const router = createRouter();

// Detail see: https://github.com/vuejs/vue-router/issues/1234#issuecomment-357941465
export function resetRouter() {
  const newRouter = createRouter();
  router.matcher = newRouter.matcher; // reset router
}

export default router;
