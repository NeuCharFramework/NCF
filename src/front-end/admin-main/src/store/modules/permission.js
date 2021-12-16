import { asyncRoutes, constantRoutes } from '@/router'
import Layout from '@/layout'
import moduleRouter from '@/router/modules/module'

/**
 * Use meta.role to determine if the current user has permission
 * @param roles
 * @param route
 */
function hasPermission(roles, route) {
  if (route.meta && route.meta.roles) {
    return roles.some(role => route.meta.roles.includes(role))
  } else {
    return true
  }
}

/**
 * Filter asynchronous routing tables by recursion
 * @param routes asyncRoutes
 * @param roles
 */
export function filterAsyncRoutes(routes, roles) {
  const res = []

  routes.forEach(route => {
    const tmp = { ...route }
    if (hasPermission(roles, tmp)) {
      if (tmp.children) {
        tmp.children = filterAsyncRoutes(tmp.children, roles)
      }
      res.push(tmp)
    }
  })

  return res
}

const state = {
  routes: [],
  addRoutes: [],
  accessedRoutes:[],
  sidebarRouters: []
}

const mutations = {
  SET_ROUTES: (state, routes) => {
    state.addRoutes = routes
    console.log('routes', routes)
    state.routes = constantRoutes.concat(routes)
    console.log('state.routes',state.routes)
    console.log('constantRoutes',constantRoutes)
    // state.routes = constantRoutes.concat([])
  },
  SET_SIDEBAR_ROUTERS: (state, routes) => {
    console.log(777888)
    state.sidebarRouters = routes
  }
}

const actions = {
  generateRoutes({ commit }, roles) {
    return new Promise(resolve => {
      let accessedRoutes
      if (roles.includes('admin')) {
        accessedRoutes = asyncRoutes || []
      } else {
        accessedRoutes = filterAsyncRoutes(asyncRoutes, roles)
      }
      console.log('accessedRoutes', accessedRoutes)
      state.accessedRoutes = accessedRoutes
      commit('SET_ROUTES', accessedRoutes)
      resolve(accessedRoutes)
    })
  },
  setRoutes({ commit }, routes) {
    let list = { ...moduleRouter, ...{}}
    console.log(123, list)
    console.log(123, list.children)
    console.log(3333, state.routes)

    routes.forEach(item => {
      if (item.name === 'b-home1') {
        item.meta = {
          title: '拓展模块b-home1'
        }
        list.children.push(item)
      }
      if (item.name === 'b-about1') {
        item.meta = {
          title: '拓展模块b-about1'
        }
        list.children = [...list.children, ...[item]]
      }
    })
    // list = [...state.accessedRoutes, ...list]
    console.log(456, routes)
    console.log('之后的菜单', list)
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
