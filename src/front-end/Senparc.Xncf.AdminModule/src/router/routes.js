export default [
  {
    path: 'b/about1',
    name: 'b-about1',
    component: () =>
      import(/* webpackChunkName: "about" */ '../views/About.vue')
  },
  {
    path: 'b/home1',
    name: 'b-home1',
    component: () =>
      import(/* webpackChunkName: "about" */ '../views/Home.vue')
  },
  {
    path: '/appstore',
    name: 'b-appstore',
    component: () =>
      import('../views/XncfStore/shopping/Index.vue')
  }
]
