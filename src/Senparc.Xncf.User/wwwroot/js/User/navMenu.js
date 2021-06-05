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
};

// 菜单栏数据
function getNavMenu() {
    service.get("/Admin/index?handler=MenuResource").then(res => {
        if (res.data.success) {
            var temp = res.data.data.menuList;
            myfunctionMain(temp);
            // 数据存起来
            Store.commit('savenavMenuList', temp);
            // 按钮权限存起来  使用：直接在dom上v-has=" ['admin-add']"
            window.sessionStorage.setItem('saveResourceCodes', JSON.stringify(res.data.data.resourceCodes));
        }
    });
}

getNavMenu();

// 菜单栏数据递归
function myfunctionMain(list) {
    if (!list && list.length === 0) {
        return;
    }
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