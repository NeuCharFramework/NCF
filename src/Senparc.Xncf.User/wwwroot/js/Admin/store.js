var Store = new Vuex.Store({
    state: {
        pageSrc: '1',
        resourceCodes: [],
        navMenu: { //侧边栏数据
            navMenuList: [],
            isCollapse:JSON.parse( window.sessionStorage.getItem('isCollapse'))|| false,
            variables: {
                menuBg: '#304156', // 背景色
                menuText: '#bfcbd9', // 文字色
                menuActiveText: '#409EFF' //激活颜色
            },
            // 当前激活菜单的 index
            activeMenu: window.sessionStorage.getItem('activeMenu') || '0'
        }
    },
    mutations: {
        changePageSrc(state, data) {
            state.pageSrc = data;
        },
        saveResourceCodes(state, data) {
            state.resourceCodes = data;
        },
        // 切换菜单栏状态
        changeIsCollapse(state,data) {
            state.navMenu.isCollapse = data;
        },
        // 保存菜单数据
        savenavMenuList(state, data) {
            state.navMenu.navMenuList = data;
        }
    }
});
