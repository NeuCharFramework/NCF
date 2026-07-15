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

(function () {
    var started = false;
    var promptVisible = false;
    var checkTimer = null;
    var remindTimer = null;
    var currentExpiresAtMs = 0;

    function getConfig() {
        var cfg = window.NCF_SESSION_CONFIG || {};
        return {
            webLoginExpireMinutes: Number(cfg.webLoginExpireMinutes) > 0 ? Number(cfg.webLoginExpireMinutes) : 120,
            jwtExpireMinutes: Number(cfg.jwtExpireMinutes) > 0 ? Number(cfg.jwtExpireMinutes) : 9000,
            remindBeforeMinutes: Number(cfg.remindBeforeMinutes) > 0 ? Number(cfg.remindBeforeMinutes) : 5
        };
    }

    function getI18n() {
        return window.NCF_SESSION_I18N || {
            title: '登录即将过期',
            message: '检测到当前登录会话即将过期，您可以保持登录或立即退出。',
            keepLogin: '保持登录',
            logoutNow: '立即退出登录',
            keepLoginSuccess: '已保持登录，会话已续期。'
        };
    }

    function parseUtcToMs(utcValue) {
        if (!utcValue) {
            return 0;
        }
        var ms = new Date(utcValue).getTime();
        return Number.isNaN(ms) ? 0 : ms;
    }

    function doLogout() {
        if (typeof Vue !== 'undefined' && Vue.prototype && typeof Vue.prototype.loginout === 'function') {
            Vue.prototype.loginout();
        } else {
            window.location.href = '/Admin/Login?url=' + escape(window.location.pathname + window.location.search);
        }
    }

    function scheduleReminder() {
        if (remindTimer) {
            clearTimeout(remindTimer);
            remindTimer = null;
        }

        if (!currentExpiresAtMs) {
            return;
        }

        var remindMs = getConfig().remindBeforeMinutes * 60 * 1000;
        var delayMs = currentExpiresAtMs - Date.now() - remindMs;
        if (delayMs <= 0) {
            showReminderDialog();
            return;
        }

        remindTimer = setTimeout(function () {
            showReminderDialog();
        }, delayMs);
    }

    function applySessionStatus(statusData) {
        if (!statusData) {
            return;
        }

        if (statusData.token) {
            window.ncfJwtToken = statusData.token;
        }

        currentExpiresAtMs = parseUtcToMs(statusData.expiresUtc);
        scheduleReminder();
    }

    function fetchSessionStatus() {
        if (!window.service) {
            return Promise.resolve();
        }

        return service.get('/Admin/Session?handler=Status', { customAlert: true })
            .then(function (res) {
                if (res && res.data && res.data.success) {
                    applySessionStatus(res.data.data);
                    return;
                }

                currentExpiresAtMs = 0;
            });
    }

    function keepAliveSession() {
        if (!window.service) {
            return Promise.reject(new Error('service not ready'));
        }

        return service.get('/Admin/Session?handler=KeepAlive', { customAlert: true })
            .then(function (res) {
                if (!(res && res.data && res.data.success)) {
                    return Promise.reject(new Error('keep alive failed'));
                }

                applySessionStatus(res.data.data);
                var i18n = getI18n();
                if (typeof app !== 'undefined' && app.$message) {
                    app.$message({
                        message: i18n.keepLoginSuccess || '已保持登录，会话已续期。',
                        type: 'success',
                        duration: 1500
                    });
                }
            });
    }

    function showReminderDialog() {
        if (promptVisible) {
            return;
        }

        if (!currentExpiresAtMs || currentExpiresAtMs <= Date.now()) {
            return;
        }

        if (typeof app === 'undefined' || !app.$confirm) {
            return;
        }

        promptVisible = true;
        var i18n = getI18n();

        app.$confirm(
            i18n.message || '检测到当前登录会话即将过期，您可以保持登录或立即退出。',
            i18n.title || '登录即将过期',
            {
                confirmButtonText: i18n.keepLogin || '保持登录',
                cancelButtonText: i18n.logoutNow || '立即退出登录',
                type: 'warning',
                closeOnClickModal: false,
                showClose: false,
                distinguishCancelAndClose: true
            })
            .then(function () {
                return keepAliveSession();
            })
            .catch(function () {
                doLogout();
            })
            .finally(function () {
                promptVisible = false;
            });
    }

    function startMonitorWhenReady() {
        if (started) {
            return;
        }

        if (window.location.pathname.toLowerCase().indexOf('/admin/login') === 0) {
            return;
        }

        var waitAndStart = function () {
            if (!window.service || typeof app === 'undefined') {
                setTimeout(waitAndStart, 800);
                return;
            }

            started = true;
            fetchSessionStatus().catch(function () { });
            checkTimer = setInterval(function () {
                fetchSessionStatus().catch(function () { });
            }, 60 * 1000);
        };

        waitAndStart();
    }

    window.ncfStartSessionMonitor = startMonitorWhenReady;
})();
