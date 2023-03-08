/** When your routing table is too long, you can split it into small modules**/
import Layout from '@/layout'

const moduleRouter = {
  path: '/Module',
  component: Layout,
  redirect: '/Module/home',
  // 这里的name需要和module模块中module.js的addRoute的name相同
  name: 'Module',
  meta: {
    title: '加载模块',
    icon: 'el-icon-cpu'
  },
  children: [
    {
      path: '/Module/home',
      component: () => import('@/views/module/home'),
      name: 'ModuleHome',
      meta: { title: '加载模块' }
    },
    // 首页
    {
      path: "/openaiindex",
      component: Layout,
      hidden: true,
      children: [
        {
          path: "/redirect/:path(.*)",
          component: () => import("@/views/OpenAI/index"),
        },
      ],
    },
    // 详情页
    {
      path: "/openaidetail",
      component: Layout,
      hidden: true,
      children: [
        {
          path: "/redirect/:path(.*)",
          component: () => import("@/views/OpenAI/detail"),
        },
      ],
    },
  ]
}

export default moduleRouter
