// /** When your routing table is too long, you can split it into small modules**/
// import Layout from '@/layout'

// const moduleRouter = {
//   path: '/XncfModule',
//   component: Layout,
//   redirect: '/Admin/XncfModule/Index',
//   // 这里的name需要和module模块中module.js的addRoute的name相同
//   name: 'XncfModule',
//   meta: {
//     title: '扩展模块',
//     icon: 'el-icon-cpu'
//   },
//   children: [
//     {
//       path: '/Admin/XncfModule/Index',
//       component: () => import('@/views/module/index'),
//       name: 'XncfModuleIndex',
//       meta: { title: '模块管理'}
//     },
//     {
//       path: '/Admin/XncfModule/Start',
//       component: () => import('@/views/module/start'),
//       name: 'XncfModuleStart',
//       meta: { title: '执行'}
//     },
//     // {
//     //   path: '/Admin/XncfModule/Start',
//     //   component: () => import('@/views/module/start'),
//     //   name: 'XncfModuleStart',
//     //   meta: { title: '执行'}
//     // }
//   ]
// }

// export default moduleRouter
