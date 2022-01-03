import {asyncRoutes, constantRoutes} from '@/router'
import Layout from '@/layout'
import moduleRouter from '@/router/modules/module'

/**
 * 使用 后端传来的路由表判断当前用户是否有权限
 * @param menuTree
 * @param route
 */

function hasPermission(menuTree, route) {

  //menuTree.some(role => route.path ===role.path)
  if (route.path) {
    return menuTree.filter(item => item.url === route.path).length > 0
  } else {
    return true
  }
}

/**
 * 通过递归过滤异步路由表
 * @param routes asyncRoutes
 * @param menuTree
 * @param pageNotFind
 */
export function filterAsyncRoutes(routes, menuTree, pageNotFind=true) {
  const res = []
  let page404 = routes.filter(item=>item.path==="*")[0]
  routes.forEach(route => {
    const tmp = {...route}
    //路由权限
    if (hasPermission(menuTree, tmp)) {
      if (tmp.children) {
        //子路由权限
        let list = menuTree.filter(item => item.url === tmp.path)[0] || {}
        tmp.children = filterAsyncRoutes(tmp.children, list.children, false)
      }
      res.push(tmp)
    }
  })
  //把404页面加载到路由最后面
  pageNotFind && page404 && res.push(page404)
  return res
}

const state = {
  routes: [],
  addRoutes: [],
  accessedRoutes: [],
  sidebarRouters: []
}

const mutations = {
  SET_ROUTES: (state, routes) => {
    state.addRoutes = routes
    state.routes = constantRoutes.concat(routes)
  },
  SET_SIDEBAR_ROUTERS: (state, routes) => {
    state.sidebarRouters = routes
  }
}

const actions = {
  generateRoutes({commit}, {roleCodes, menuTree}) {
    return new Promise(resolve => {
      let accessedRoutes
      //超级管理员（）有全部异步路由的权限
      if (roleCodes.includes('administrator8')) {
        accessedRoutes = asyncRoutes || []
      } else {
        accessedRoutes = filterAsyncRoutes(asyncRoutes, menuTree)
      }
      state.accessedRoutes = accessedRoutes
      commit('SET_ROUTES', accessedRoutes)
      resolve(accessedRoutes)
    })
  },
  setRoutes({commit}, routes) {
    let list = {...moduleRouter, ...{}}

    // routes.forEach(item => {
    //   if (item.name === 'b-home1') {
    //     item.meta = {
    //       title: '拓展模块b-home1'
    //     }
    //     list.children.push(item)
    //   }
    //   if (item.name === 'b-about1') {
    //     item.meta = {
    //       title: '拓展模块b-about1'
    //     }
    //     list.children = [...list.children, ...[item]]
    //   }
    // })

    list = [...state.accessedRoutes, ...list]

    commit('SET_ROUTES', list)
  }
}

// 遍历后台传来的路由字符串，转换为组件对象
function filterAsyncRouter(asyncRouterMap) {
  return asyncRouterMap.filter(route => {
    if (route.component) {
      // Layout组件特殊处理
      if (route.component === 'Layout') {
        route.component = Layout
      } else {
        route.component = loadView(route.component)
      }
    }
    if (route.children != null && route.children && route.children.length) {
      route.children = filterAsyncRouter(route.children)
    }
    return true
  })
}

export default {
  namespaced: true,
  state,
  mutations,
  actions
}
