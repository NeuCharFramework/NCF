function permissionJudge(value) {
    let list = JSON.parse(window.sessionStorage.getItem('saveResourceCodes'));
    if (!value.length > 0) {
        return true;
    }
    for (let item of list) {
        if (value.indexOf(item.resourceCode) > -1) {
            return true;
        }
    }
    return false;
}
// 注册一个全局自定义指令 `v-has`
Vue.directive('has', {
    // 当被绑定的元素插入到 DOM 中时触发bind钩子
    inserted: function (el, binding) {
        if (!permissionJudge(binding.value)) {
            el.parentNode.removeChild(el);
        }
    }
});