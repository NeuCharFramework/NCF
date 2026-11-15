var validatePass = (rule, value, callback) => {
    if (value === '') {
        callback(new Error((window.ncfLoginI18n && window.ncfLoginI18n.passwordRequired) || 'Password is required'));
    } else {
        callback();
    }
};
var validateUser = (rule, value, callback) => {
    if (value === '') {
        callback(new Error((window.ncfLoginI18n && window.ncfLoginI18n.usernameRequired) || 'Username is required'));
    } else {
        callback();
    }
};
var validateTenant = (rule, value, callback) => {
    // 租户名称不是必填的，直接通过验证
    callback();
};
var validateCaptcha = (rule, value, callback) => {
    // 仅当服务端标记需要验证码时才必填
    var required = typeof app !== 'undefined' && app && app.captchaRequired;
    if (required && (!value || value.trim() === '')) {
        callback(new Error((window.ncfLoginI18n && window.ncfLoginI18n.captchaRequired) || 'Captcha is required'));
    } else {
        callback();
    }
};
var app = new Vue({
    el: '#app',
    data: {
        ruleForm: {
            user: '',
            pass: '',
            tenant: '',
            captcha: ''
        },
        enableMultiTenant: false,
        captchaRequired: false,
        captchaToken: '',
        captchaImage: '',
        rules: {
            user: [
                { validator: validateUser, trigger: 'blur' }
            ],
            pass: [
                { validator: validatePass, trigger: 'blur' }
            ],
            tenant: [
                { validator: validateTenant, trigger: 'blur' }
            ],
            captcha: [
                { validator: validateCaptcha, trigger: 'blur' }
            ]
        }, loading: false
    },
    mounted() {
        // 检查是否启用多租户
        service.get('/Admin/Login?handler=CheckMultiTenant').then(res => {
            this.enableMultiTenant = res.data.data;
            // 如果不是多租户模式，清空租户输入
            if (!this.enableMultiTenant) {
                this.ruleForm.tenant = '';
            }
        }).catch(error => {
            console.error('检查多租户状态失败:', error);
            this.enableMultiTenant = false;
            this.ruleForm.tenant = '';
        });
    },
    methods: {
        submitForm(formName) {
            this.$refs[formName].validate((valid) => {
                this.loading = true;
                var url = "/Admin/Login?handler=Login";
                let data = {
                    Name: this.ruleForm.user+'',
                    Password: this.ruleForm.pass,
                    Tenant: this.ruleForm.tenant,
                    CaptchaToken: this.captchaRequired ? this.captchaToken : '',
                    CaptchaCode: this.captchaRequired ? (this.ruleForm.captcha || '') : ''
                };
                if (valid) {
                    service.post(url, data, { customAlert: true }).then(res => {
                        if (res.data.success) {
                            const url = this.resizeUrl().ReturnUrl;
                            window.location.href = url ? unescape(url) : '/Admin/index';
                        } else {
                            this.$message.error(res.data.msg || (window.ncfLoginI18n && window.ncfLoginI18n.loginFailed) || 'Login failed');
                            if (res.data.captchaRequired) {
                                this.captchaRequired = true;
                                if (res.data.captcha && res.data.captcha.token) {
                                    this.captchaToken = res.data.captcha.token;
                                    this.captchaImage = res.data.captcha.image;
                                } else {
                                    this.refreshCaptcha();
                                }
                            }
                            this.ruleForm.captcha = '';
                            this.loading = false;
                        }
                    }).catch(error => {
                        this.$message.error((window.ncfLoginI18n && window.ncfLoginI18n.loginRetry) || 'Login failed, please try again');
                        this.loading = false;
                    });
                } else {
                    this.loading = false;
                    console.log('error submit!!');
                    return false;
                }
            });
        },
        refreshCaptcha() {
            var self = this;
            service.get('/Admin/Login?handler=Captcha', { customAlert: true }).then(res => {
                if (res.data && res.data.success) {
                    self.captchaToken = res.data.data.token;
                    self.captchaImage = res.data.data.image;
                    self.ruleForm.captcha = '';
                }
            }).catch(error => {
                console.error('获取验证码失败:', error);
                self.captchaImage = '';
                self.captchaToken = '';
            });
        },
        checkCaptchaRequired() {
            var name = this.ruleForm.user + '';
            var self = this;
            if (!name) {
                self.captchaRequired = false;
                self.captchaImage = '';
                self.captchaToken = '';
                return;
            }
            service.get('/Admin/Login?handler=CaptchaRequired&name=' + encodeURIComponent(name), { customAlert: true }).then(res => {
                if (res.data && res.data.success) {
                    if (res.data.data && !self.captchaRequired) {
                        self.captchaRequired = true;
                        self.refreshCaptcha();
                    } else if (!res.data.data) {
                        self.captchaRequired = false;
                        self.captchaImage = '';
                        self.captchaToken = '';
                    }
                }
            }).catch(error => {
                console.error('检查验证码状态失败:', error);
            });
        },
        resizeUrl() {//处理剪切url id
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
    }
});