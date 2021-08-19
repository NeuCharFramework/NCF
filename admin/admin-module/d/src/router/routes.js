export default [
    {
        path: 'about',
        name: 'about',
        meta: {
            title: 'about1',
            roles: ['administrator'] // or you can only set roles in sub nav
        },
        component: () =>
            import(/* webpackChunkName: "about" */ '../views/About.vue')
    },
    {
        path: 'home1',
        name: 'home1',
        meta: {
            title: 'home1',
            roles: ['administrator'] // or you can only set roles in sub nav
        },
        component: () =>
            import(/* webpackChunkName: "about" */ '../views/Home.vue'),
    }
]
