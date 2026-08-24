/**
 * axios封装
 * 请求拦截、响应拦截、错误统一处理
 */
// 创建一个axios实例
var service = axios.create({
    timeout: 1000000 // request timeout
});

function showErrorMessage(options) {
    if (typeof app !== 'undefined' && app && typeof app.$message === 'function') {
        app.$message(options);
        return;
    }
    if (typeof ELEMENT !== 'undefined' && ELEMENT && typeof ELEMENT.Message === 'function') {
        ELEMENT.Message(options);
        return;
    }
    console.error(options.message);
}

// 请求拦截
service.interceptors.request.use(
    config => {
        if ((config.method || '').toUpperCase() === 'POST') {
            const tokenInput = window.document.getElementsByName('__RequestVerificationToken')[0];
            if (tokenInput && tokenInput.value) {
                config.headers['RequestVerificationToken'] = tokenInput.value;
            }
        }
        if (window.ncfJwtToken) {
            config.headers['Authorization'] = 'Bearer ' + window.ncfJwtToken;
        }
        config.headers['x-requested-with'] = 'XMLHttpRequest';
        return config;
    },
    error => {
        console.log(error); // for debug
        return Promise.reject(error);
    }
);
// 响应拦截器
service.interceptors.response.use(
    response => {
        if (response.status === 200) {
            if (response.data.success) {
                return Promise.resolve(response);
            } else {
                // 请求已发出，其他状态
                // 切换隐藏时不给错误提示，直接刷新
                if (response.config.url.includes('HideManager') || response.config.url.includes('ChangeState')) {
                    return;
                }
                if (!response.config.customAlert){
                    showErrorMessage({
                        message: response.data.msg||response.data.exception|| 'Error',
                        type: 'error',
                        duration: 5 * 1000
                    });
                }
                return Promise.resolve(response);
            }
        } else {
            showErrorMessage({
                message: response.msg || 'Error',
                type: 'error',
                duration: 5 * 1000
            });
            return Promise.reject(response);
        }
    },
    error => {
        console.log('err' + error);
        // Workflow uses customAlert to render a 400 validation failure in its
        // own node-aware panel. Do not call the legacy global `app` here: this
        // page deliberately has no globally named Vue instance.
        if (error && error.config && error.config.customAlert) {
            return Promise.reject(error);
        }
        if (error.message.includes('401')) {
            showErrorMessage({
                message: ncfT('Admin.Session.ExpiredRedirect'),
                type: 'error',
                duration: 3 * 1000,
                onClose: function () {
                    window.location.href = '/Admin/Login?url=' + escape(window.location.pathname + window.location.search);
                }
            });
            return Promise.reject(error);
        } if (error.message.includes('403')) {
            showErrorMessage({
                message: ncfT('Admin.Session.AccessDenied'),
                type: 'error',
                duration: 3 * 1000
            });
            return Promise.reject(error);
        } else if (error.message.includes('302')) {
            return Promise.reject(error);
        }
        showErrorMessage({
            message: error.message,
            type: 'error',
            duration: 5 * 1000
        });
        return Promise.reject(error);
    }
);
