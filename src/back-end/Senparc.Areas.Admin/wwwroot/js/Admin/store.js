var NCF_ADMIN_NAV_STORAGE_KEYS = {
    openedMenus: 'ncf.admin.navMenu.openedMenus',
    scrollTop: 'ncf.admin.navMenu.scrollTop'
};

function getStoredNavMenuOpenedMenus() {
    try {
        var openedMenus = JSON.parse(window.sessionStorage.getItem(NCF_ADMIN_NAV_STORAGE_KEYS.openedMenus));
        return Array.isArray(openedMenus) ? openedMenus : [];
    } catch (e) {
        return [];
    }
}

// 菜单搜索：按关键字过滤菜单树。命中节点保留其完整子树；否则仅保留命中的分支。
function filterNavMenuTree(list, keyword) {
    if (!list || list.length === 0) {
        return [];
    }
    var kw = String(keyword || '').trim().toLowerCase();
    if (!kw) {
        return list;
    }
    var result = [];
    list.forEach(function (node) {
        var children = node.children || [];
        var selfMatched = String(node.menuName || '').toLowerCase().indexOf(kw) >= 0;
        var keptChildren = selfMatched ? children : filterNavMenuTree(children, kw);
        if (selfMatched || keptChildren.length > 0) {
            result.push(Object.assign({}, node, { children: keptChildren }));
        }
    });
    return result;
}

// 收集菜单树中所有子菜单的 index（搜索时强制展开，保证命中项可见）。
function collectNavMenuSubmenuIndexes(list) {
    var indexes = [];
    (list || []).forEach(function (node) {
        var children = node.children || [];
        if (children.length > 0) {
            indexes.push(node.index !== undefined && node.index !== null ? node.index : node.id);
            indexes = indexes.concat(collectNavMenuSubmenuIndexes(children));
        }
    });
    return indexes;
}

var Store = new Vuex.Store({
    state: {
        pageSrc: '1',
        resourceCodes: [],
        navMenu: { //侧边栏数据
            navMenuList: [],
            searchKeyword: '',
            isCollapse:JSON.parse( window.sessionStorage.getItem('isCollapse'))|| false,
            openedMenus: getStoredNavMenuOpenedMenus(),
            variables: {
                menuBg: '#304156', // 背景色
                menuText: '#bfcbd9', // 文字色
                menuActiveText: '#409EFF' //激活颜色
            },
            // 当前激活菜单的 index
            activeMenu: window.sessionStorage.getItem('activeMenu') || '0'
        }
    },
    getters: {
        // 左侧菜单搜索过滤结果
        filteredNavMenuList: function (state) {
            return filterNavMenuTree(state.navMenu.navMenuList, state.navMenu.searchKeyword);
        },
        // 搜索中：展开过滤结果里的全部子菜单，保证命中项直接可见
        effectiveNavMenuOpeneds: function (state) {
            var keyword = String(state.navMenu.searchKeyword || '').trim();
            if (!keyword) {
                return state.navMenu.openedMenus;
            }
            var searched = collectNavMenuSubmenuIndexes(filterNavMenuTree(state.navMenu.navMenuList, keyword));
            // 与已展开菜单合并，避免切换搜索时丢失用户手动展开的分支
            return state.navMenu.openedMenus.concat(
                searched.filter(function (index) {
                    return state.navMenu.openedMenus.indexOf(index) < 0;
                })
            );
        },
        isNavMenuSearching: function (state) {
            return String(state.navMenu.searchKeyword || '').trim().length > 0;
        },
        // 菜单渲染键：普通模式保持稳定（展开/收起不重建菜单）；
        // 搜索模式下随“应展开集合”变化而重建，保证新出现的命中分支自动展开。
        navMenuRenderKey: function (state) {
            var keyword = String(state.navMenu.searchKeyword || '').trim();
            if (!keyword) {
                return 'nav-menu-normal';
            }
            return 'nav-menu-searching-' + collectNavMenuSubmenuIndexes(
                filterNavMenuTree(state.navMenu.navMenuList, keyword)).join('|');
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
        // 保存已展开的菜单
        changeOpenedMenus(state, data) {
            state.navMenu.openedMenus = data;
        },
        // 保存菜单数据
        savenavMenuList(state, data) {
            state.navMenu.navMenuList = data;
        },
        // 设置菜单搜索关键字
        setNavMenuSearchKeyword(state, data) {
            state.navMenu.searchKeyword = data || '';
        }
    }
});
