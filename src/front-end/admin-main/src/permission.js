import router from './router'
import store from './store'
import { Message } from 'element-ui'
import NProgress from 'nprogress' // progress bar
import 'nprogress/nprogress.css' // progress bar style
import { getToken } from '@/utils/auth' // get token from cookie
import getPageTitle from '@/utils/get-page-title'

NProgress.configure({ showSpinner: false }) // NProgress Configuration

const whiteList = ['/login', '/auth-redirect'] // no redirect whitelist

router.beforeEach(async(to, from, next) => {
  // start progress bar
  NProgress.start()

  // set page title
  document.title = getPageTitle(to.meta.title)

  // determine whether the user has logged in
  const hasToken = getToken()

  if (hasToken) {
    if (to.path === '/login') {
      // 如果已登录，则重定向到主页
      next({ path: '/' })
      NProgress.done() // hack: https://github.com/PanJiaChen/vue-element-admin/pull/2939
    } else {
      // 通过getInfo判断用户是否获得了他的权限角色
      const hasRoles = store.getters.menuTree && store.getters.menuTree.length > 0
      console.log('hasRoles',hasRoles)
      if (hasRoles) {
        next()
      } else {
        try {
          // get user info
          // 注意：menuTree、roleCodes 必须是一个数组
          const { roleCodes, menuTree } = await store.dispatch('user/getInfo')

          // 根据后端路由表生成可访问的路由
          const accessRoutes = await store.dispatch('permission/generateRoutes', { roleCodes, menuTree })

          // 动态添加可访问的路由
          console.log('可访问的路由',accessRoutes)

          router.addRoutes(accessRoutes)

          // hack 方法来确保 addRoutes 是完整的
          // 设置 replace: true, 这样导航就不会留下历史记录
          next({ ...to, replace: true })
        } catch (error) {
          // remove token and go to login page to re-login
          await store.dispatch('user/resetToken')
          Message.error(error || 'Has Error')
          next(`/login?redirect=${to.path}`)
          NProgress.done()
        }
      }
    }
  } else {
    /* has no token*/

    if (whiteList.indexOf(to.path) !== -1) {
      // in the free login whitelist, go directly
      next()
    } else {
      // other pages that do not have permission to access are redirected to the login page.
      next(`/login?redirect=${to.path}`)
      NProgress.done()
    }
  }
})

router.afterEach(() => {
  // finish progress bar
  NProgress.done()
})
