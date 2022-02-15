/** When your routing table is too long, you can split it into small modules**/
import Layout from '@/layout'

const moduleRouter = {
  path: '/Module',
  component: Layout,
  redirect: 'noRedirect',
  // 这里的name需要和module模块中module.js的addRoute的name相同
  name: 'Module',
  meta: {
    title: '拓展模块',
    icon: 'el-icon-cpu'
  },
  children: [
    {
      path: '/Admin/XncfModule/Index',
      component: () => import('@/views/module/home'),
      name: 'ModuleHome',
      meta: { title: '拓展模块', noCache: false }
    },
  ]
}

export default moduleRouter
