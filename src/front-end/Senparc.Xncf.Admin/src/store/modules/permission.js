import { asyncRoutes, constantRoutes } from "@/router";
import Layout from "@/layout";
import moduleRouter from "@/router/modules/module";

/**
 * 使用 后端传来的路由表判断当前用户是否有权限
 * @param menuTree
 * @param route
 */

function hasPermission(menuTree, route) {
  // menuTree.some(role => route.path ===role.path)
  if (route.path) {
    return (
      menuTree.filter(
        (item) => item.url === route.path || item.url.startsWith(route.path)
      ).length > 0
    );
  } else {
    return false;
  }
}

/**
 * 通过递归过滤异步路由表
 * @param routes asyncRoutes
 * @param menuTree
 * @param pageNotFind
 */
export function filterAsyncRoutes(routes, menuTree, pageNotFind = true) {
  const res = [];
  const page404 = routes.filter((item) => item.path === "*")[0];
  routes.forEach((route) => {
    const tmp = { ...route };
    // 路由权限
    if (hasPermission(menuTree, tmp)) {
      if (tmp.children) {
        // 子路由权限 (比对远程路由的url和定义的动态路由的path)，如果没有值检查远程路由的结构或者url是否为空
        const list = menuTree.filter((item) => item.url === tmp.path)[0] || {};
        tmp.children = filterAsyncRoutes(tmp.children, list.children, false);
      }
      res.push(tmp);
    }
  });
  // 把404页面加载到路由最后面
  pageNotFind && page404 && res.push(page404);
  return res;
}

const state = {
  routes: [],
  addRoutes: [],
  accessedRoutes: [],
  sidebarRouters: [],
};

const mutations = {
  SET_ROUTES: (state, routes) => {
    state.addRoutes = routes;
    state.routes = constantRoutes.concat(routes);
  },
  SET_SIDEBAR_ROUTERS: (state, routes) => {
    state.sidebarRouters = routes;
  },
};

const actions = {
  generateRoutes({ commit }, { roleCodes, menuTree }) {
    return new Promise((resolve) => {
      let accessedRoutes;
      // 超级管理员（administrator）有全部异步路由的权限
      // if (roleCodes.includes("administrator")) {
      //   accessedRoutes = asyncRoutes || [];
      // } else {
      //   accessedRoutes = filterAsyncRoutes(asyncRoutes, menuTree);
      // }
      // 根据后端返回生成动态路由
      accessedRoutes = generateRoutesList(asyncRoutes, menuTree);
      state.accessedRoutes = accessedRoutes;
      commit("SET_ROUTES", accessedRoutes);
      resolve(accessedRoutes);
    });
  },
  setRoutes({ commit }, routes) {
    // console.log(routes)
    // console.log('moduleRouter', moduleRouter)
    const list = { ...moduleRouter, ...{} };

    // 是否是远程加载的路由
    routes.forEach((item) => {
      if (item.name === "b-home1") {
        console.log("item", item.name);
        item.meta = {
          title: "拓展模块b-home1",
        };
        list.children.push(item);
      }
      if (item.name === "b-about1") {
        item.meta = {
          title: "拓展模块b-about1",
        };
        list.children = [...list.children, ...[item]];
      }
    });
    // console.log("list", list);
    state.accessedRoutes.forEach((item, index) => {
      if (item.name === "Module") {
        // console.log(index, item.name);
        state.accessedRoutes[index] = list;
        // state.accessedRoutes[index].children = [
        //   ...state.accessedRoutes[index].children,
        //   ...
        //   [{
        //   "path": "/Admin/AdminUserInfo/Index",
        //   "name": "管理员管理",
        //   "meta": {
        //     "title": "管理员管理"
        //   }
        // },
        //   {
        //     "path": "/Admin/Role/Index",
        //     "name": "角色管理",
        //     "meta": {
        //       "title": "角色管理"
        //     }
        //   }]
        // ]
      }
    });

    // list = [...state.accessedRoutes, ...list]
    // console.log("路由state.accessedRoutes", state.accessedRoutes);
    commit("SET_ROUTES", state.accessedRoutes);
  },
};

// 遍历后台传来的路由字符串，转换为组件对象
function filterAsyncRouter(asyncRouterMap) {
  return asyncRouterMap.filter((route) => {
    if (route.component) {
      // Layout组件特殊处理
      if (route.component === "Layout") {
        route.component = Layout;
      } else {
        route.component = loadView(route.component);
      }
    }
    if (route.children != null && route.children && route.children.length) {
      route.children = filterAsyncRouter(route.children);
    }
    return true;
  });
}

/**
 * 通过递归 生成路由表
 * @param routes asyncRoutes
 * @param menuTree
 * @param pageNotFind
 */
export function generateRoutesList(routes, menuTree, pageNotFind = true) {
  const res = [];
  const page404 = routes.filter((item) => item.path === "*")[0];
  let urlPathList = []
  menuTree.forEach((route) => {
    const tmp = { ...route };
    // 判断是否有Url 没有随机上成一个   
    if (!tmp.url) {
      let urlPath = getUrlPath(urlPathList)
      urlPathList.push(urlPath)
      tmp.url = `${urlPath}`
      tmp.breadcrumb = true
    }
    // 查询是否相同路由
    const isIdentical = res.findIndex((item) => item.url === tmp.url);
    // 路由权限
    if (tmp.url && isIdentical === -1) {
      // 有子项递归
      if (tmp.children && tmp.children.length > 0) {
        tmp.children = generateRoutesList([], tmp.children, false);
      }
      // console.log('tmp项', tmp);
      // 拆取path 路径
      let componentName = tmp.url.includes("?") ? tmp.url.split("?")[0] : tmp.url;
      // 如果已 Start/ 结尾 就是执行方法页面使用前端路径
      if (componentName.endsWith("Start/")) {
        componentName = "/Admin/XncfModule/Start";
        tmp.url = "/Admin/XncfModule/Start/" + tmp.url.split("?")[1]
      }
      // 如果 urlPathList 列表中包含则是二级及以上无url的包裹页面
      if (urlPathList.includes(tmp.url)) {
        componentName = "/Admin/XncfModule/menu/index";
      }

      let routerObj = {
        path: tmp.url,
        name: tmp.menuName + tmp.url,
        component: pageNotFind ? Layout : (resolve) => require(["@/views" + componentName + ".vue"], resolve),
        meta: {
          title: tmp.menuName,
          icon: tmp.icon,
          breadcrumb: tmp.breadcrumb ? false : true
        },
        children: tmp.children,
      };

      res.push(routerObj);
    }
  });
  // console.log('路由',res);
  // 把404页面加载到路由最后面
  pageNotFind && page404 && res.push(page404);
  return res;
}

/**
 * 随机生成字符串
 * @param 存储数组
 */
function getUrlPath(urlPathList) {
  // 生成随机字符串
  let urlPath = getRandomString(9);
  if (!urlPathList.includes(urlPath)) {
    return urlPath
  } else {
    getUrlPath(urlPathList)
  }
}

/**
 * 随机生成字符串
 * @param len 指定生成字符串长度
 */
function getRandomString(len) {
  let _charStr = 'abacdefghjklmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ',
    min = 0,
    max = _charStr.length - 1,
    _str = '';                    //定义随机字符串 变量
  //判断是否指定长度，否则默认长度为15
  len = len || 9;
  //循环生成字符串
  for (var i = 0, index; i < len; i++) {
    index = (function (randomIndexFunc, i) {
      return randomIndexFunc(min, max, i, randomIndexFunc);
    })(function (min, max, i, _self) {
      let indexTemp = Math.floor(Math.random() * (max - min + 1) + min),
        numStart = _charStr.length - 10;
      if (i == 0 && indexTemp >= numStart) {
        indexTemp = _self(min, max, i, _self);
      }
      return indexTemp;
    }, i);
    _str += _charStr[index];
  }
  return _str;
}



export default {
  namespaced: true,
  state,
  mutations,
  actions,
};
