var app = new Vue({
    el: '#app',
    data: {
        isExpanded: false,
        installStarted: false,
        installOptions: {
            systemName: "",
            adminUserName: "",
            dbConnectionString: ""
        }
    },
    methods: {
        toggleInfo() {
            this.isExpanded = !this.isExpanded;
        },
        submit() {
            if (this.installStarted) {
                alert('安装已经在进行中，请等待');
                return false;
            }

            if (!confirm('确定要开始安装吗？安装完成后此页面将失效！')) {
                return false;
            }

            this.installStarted = true;

            //document.getElementById('install_form').submit();
            document.getElementById('btnInstall').setAttribute('disabled', true);
            document.getElementById('btnInstall').innerHTML = '安装已启动，请稍后……';


            axios.post("/Install/Index", this.installOptions, {
            }).then(res => {
                document.getElementById('app').innerHTML = res.data;
            }).catch(error => {
                document.getElementById('app').innerHTML = '安装失败:' + error;
            });
            return true;
        }
    }
})