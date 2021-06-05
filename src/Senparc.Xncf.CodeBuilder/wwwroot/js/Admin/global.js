// 退出
Vue.prototype.loginout = function () {
    window.location.href = '/Admin/Login?handler=Logout&ReturnUrl=' + escape(window.location.pathname + window.location.search);
};

// 格式化添加时间等 2020 - 06 - 19T09: 41: 51.1905692
function formaTableTime(value) {
    return value ? value.replace('T', '  ').substr(0, 17) : '暂无时间';
}

/**
 * 复制内容
 * @param {any} value 值 
 * @param {any} toastText 提示内容
 */
function copyToClipboard(value, toastText) {
    $('#clipboardContainer').val(value);
    $('#clipboardContainer').select();
    toastText = toastText || '复制成功！请使用 Ctrl+V 组合键进行粘贴操作。';
    try {
        document.execCommand("copy"); // 执行浏览器复制命令
        base.swal.toast(toastText);
    } finally {
        //Execute Finally
    }
}

// 处理剪切url id
function resizeUrl() {
    let url = window.location.href;
    let obj = {};
    let reg = /[?&][^?&]+=[^?&]+/g;
    let arr = url.match(reg); // return ["?id=123456","&a=b"]
    if (arr) {
        arr.forEach((item) => {
            let tempArr = item.substring(1).split('=');
            let key = tempArr[0];
            let val = tempArr[1];
            obj[key] = decodeURIComponent(val);
        });
    }
    return obj;
}

/**
 * 将数值四舍五入(保留2位小数)后格式化成金额形式
 * @param {any} num 数值(Number或者String)
 * @returns {any} 金额格式的字符串,如'1,234,567.45'
 */
function formatCurrency(num) {
    num = num.toString().replace(/\$|\,/g, '');
    if (isNaN(num))
        num = "0";
    sign = num === (num = Math.abs(num));
    num = Math.floor(num * 100 + 0.50000000001);
    cents = num % 100;
    num = Math.floor(num / 100).toString();
    if (cents < 10)
        cents = "0" + cents;
    for (var i = 0; i < Math.floor((num.length - (1 + i)) / 3); i++)
        num = num.substring(0, num.length - (4 * i + 3)) + ',' + num.substring(num.length - (4 * i + 3));
    return ((sign ? '' : '-') + num + '.' + cents);
}