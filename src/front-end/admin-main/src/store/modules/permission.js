import {asyncRoutes, constantRoutes} from '@/router'
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

function hasPermissionMenuTree(menuTree,route) {

  //menuTree.some(role => route.path ===role.path)
  if (route.path) {
    return  menuTree.filter(item=>item.url===route.path).length>0
  } else {
    return true
  }
}

/**
 * Filter asynchronous routing tables by recursion
 * @param routes asyncRoutes
 * @param roles
 * @param menuTree
 */
export function filterAsyncRoutes(routes, menuTree) {
  const res = []
  menuTree = menuTree.flat()
    routes.forEach(route => {
      console.log('route1234',route)

      const tmp = {...route}
      if (hasPermissionMenuTree(menuTree, tmp)) {
        if (tmp.children) {
          let list = menuTree.filter(item=>item.url===tmp.path)[0] || {}
          tmp.children = filterAsyncRoutes(tmp.children, list.children)
        }
        res.push(tmp)
      }
    })

  console.log('res8', res)

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
    // console.log('routes', routes)
    state.routes = constantRoutes.concat(routes)
    // console.log('state.routes',state.routes)
    // console.log('constantRoutes',constantRoutes)
    // state.routes = constantRoutes.concat([])
  },
  SET_SIDEBAR_ROUTERS: (state, routes) => {
    // console.log(777888)
    state.sidebarRouters = routes
  }
}

const actions = {
  generateRoutes({commit}, {roleCodes, menuTree}) {
    return new Promise(resolve => {
      console.log('asyncRoutes7', asyncRoutes)
      console.log('roles7', roleCodes)
      console.log('menuTree7', menuTree)

      let accessedRoutes
      if (roleCodes.includes('administrator8')) {
        accessedRoutes = asyncRoutes || []
      } else {
        accessedRoutes = filterAsyncRoutes(asyncRoutes, menuTree)
      }
      console.log('accessedRoutes7', accessedRoutes)

      state.accessedRoutes = accessedRoutes
      commit('SET_ROUTES', accessedRoutes)
      resolve(accessedRoutes)
    })
  },
  setRoutes({commit}, routes) {
    let list = {...moduleRouter, ...{}}
    // console.log(123, list)
    // console.log(123, list.children)
    // console.log(3333, state.routes)

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
    // console.log(456, routes)
    // console.log('之后的菜单', list)
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
