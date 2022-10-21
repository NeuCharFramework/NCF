/** When your routing table is too long, you can split it into small modules**/
import Layout from '@/layout'

const moduleRouter = {
  path: '/Module',
  component: Layout,
  redirect: '/Module/Index',
  // 这里的name需要和module模块中module.js的addRoute的name相同
  name: 'Module',
  meta: {
    title: '扩展模块',
    icon: 'el-icon-cpu'
  },
  children: [
    {
      path: '/Module/Index',
      component: () => import('@/views/module/index'),
      name: 'ModuleIndex',
      meta: { title: '模块管理'}
    },
    {
      path: '/Module/Start',
      component: () => import('@/views/module/start'),
      name: 'ModuleStart',
      meta: { title: '执行'}
    }
  ]
}

export default moduleRouter
