// 切换菜单栏展开
Vue.prototype.toggleSideBar = function () {
    Store.commit('changeIsCollapse', !Store.state.navMenu.isCollapse);
    let isCollapse = JSON.parse(window.sessionStorage.getItem('isCollapse')) || false;
    // 解决刷新菜单状态复原问题
    window.sessionStorage.setItem('isCollapse', !isCollapse);
};

// 点击菜单，高亮
Vue.prototype.menuSelect = function (key, keypath) {
    window.sessionStorage.setItem('activeMenu', key);
    // 页面跳转前同步保存当前菜单的所有父级，确保新页面仍然展开到当前位置。
    saveOpenedMenus((keypath || []).slice(0, -1), true);
};

// 展开菜单
Vue.prototype.menuOpen = function (key, keypath) {
    saveOpenedMenus(keypath || [key], true);
};

// 收起菜单
Vue.prototype.menuClose = function (key) {
    saveOpenedMenus([key], false);
};

function saveOpenedMenus(menuIndexes, isOpened) {
    var openedMenus = Store.state.navMenu.openedMenus.slice();

    (menuIndexes || []).forEach(function (menuIndex) {
        var position = openedMenus.indexOf(menuIndex);
        if (isOpened && position < 0) {
            openedMenus.push(menuIndex);
        } else if (!isOpened && position >= 0) {
            openedMenus.splice(position, 1);
        }
    });

    Store.commit('changeOpenedMenus', openedMenus);
    try {
        window.sessionStorage.setItem(NCF_ADMIN_NAV_STORAGE_KEYS.openedMenus, JSON.stringify(openedMenus));
    } catch (e) {
        // 浏览器禁用会话存储时仍允许菜单正常使用。
    }
}

function restoreNavMenuScroll() {
    var scrollWrap = document.querySelector('.el-aside-index .scrollbar-wrapper');
    if (!scrollWrap) {
        return;
    }

    var storedScrollTop = 0;
    try {
        storedScrollTop = Number(window.sessionStorage.getItem(NCF_ADMIN_NAV_STORAGE_KEYS.scrollTop));
    } catch (e) {
        storedScrollTop = 0;
    }

    if (Number.isFinite(storedScrollTop) && storedScrollTop > 0) {
        scrollWrap.scrollTop = storedScrollTop;
    }

    if (scrollWrap._ncfNavScrollPersistenceBound) {
        return;
    }

    var saveScrollTop = function () {
        try {
            window.sessionStorage.setItem(NCF_ADMIN_NAV_STORAGE_KEYS.scrollTop, String(scrollWrap.scrollTop));
        } catch (e) {
            // 浏览器禁用会话存储时仍允许菜单正常滚动。
        }
    };

    scrollWrap.addEventListener('scroll', saveScrollTop, { passive: true });
    window.addEventListener('pagehide', saveScrollTop);
    scrollWrap._ncfNavScrollPersistenceBound = true;
}

// 菜单栏数据
function getNavMenu() {
    service.get("/Admin/index?handler=MenuResource").then(res => {
        if (res.data.success) {
            var temp = res.data.data.menuList;
            myfunctionMain(temp);
            // 数据存起来
            Store.commit('savenavMenuList', temp);
            // 菜单异步渲染并恢复展开状态后，再还原它自己的滚动位置。
            Vue.nextTick(function () {
                if (window.requestAnimationFrame) {
                    window.requestAnimationFrame(restoreNavMenuScroll);
                } else {
                    window.setTimeout(restoreNavMenuScroll, 0);
                }
            });
            // 按钮权限存起来  使用：直接在dom上v-has=" ['admin-add']"
            window.sessionStorage.setItem('saveResourceCodes', JSON.stringify(res.data.data.resourceCodes));
        }
    });
}

getNavMenu();

// 菜单栏数据递归
function myfunctionMain(list) {
    if (!list || list.length === 0) {
        return;
    }
    //if (!list && list.length === 0) {
    //    return;
    //}
    for (var i in list) {
        let setNavMenuActive = window.sessionStorage.getItem('setNavMenuActive');
        // 如果有需要单独设置的导航 （例如安装、卸载后激活特定的导航）
        if (setNavMenuActive && setNavMenuActive !== "null" && list[i].menuName === setNavMenuActive) {
            window.sessionStorage.setItem('setNavMenuActive', null);
            if (list[i].children.length > 0) {
                window.sessionStorage.setItem('activeMenu', list[i].children[0].id);
            } else {
                window.sessionStorage.setItem('activeMenu', list[i].id);
            }
        }
        list[i].index = list[i].id;
        myfunctionMain(list[i].children);
    }
}
