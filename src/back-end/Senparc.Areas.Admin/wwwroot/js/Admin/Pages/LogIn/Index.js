var validatePass = (rule, value, callback) => {
    if (value === '') {
        callback(new Error('请输入密码'));
    } else {
        callback();
    }
};
var validateUser = (rule, value, callback) => {
    if (value === '') {
        callback(new Error('请输入用户名'));
    } else {
        callback();
    }
};
var validateTenant = (rule, value, callback) => {
    // 租户名称不是必填的，直接通过验证
    callback();
};
var app = new Vue({
    el: '#app',
    data: {
        ruleForm: {
            user: '',
            pass: '',
            tenant: ''
        },
        enableMultiTenant: false,
        rules: {
            user: [
                { validator: validateUser, trigger: 'blur' }
            ],
            pass: [
                { validator: validatePass, trigger: 'blur' }
            ],
            tenant: [
                { validator: validateTenant, trigger: 'blur' }
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
                    Tenant: this.ruleForm.tenant
                };
                if (valid) {
                    service.post(url, data).then(res => {
                        if (res.data.success) {
                            const url = this.resizeUrl().ReturnUrl;
                            window.location.href = url ? unescape(url) : '/Admin/index';
                        } else {
                            this.$message.error(res.data.message || '登录失败');
                            this.loading = false;
                        }
                    }).catch(error => {
                        this.$message.error('登录失败，请重试');
                        this.loading = false;
                    });
                } else {
                    this.loading = false;
                    console.log('error submit!!');
                    return false;
                }
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