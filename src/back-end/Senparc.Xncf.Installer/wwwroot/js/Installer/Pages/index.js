var app = new Vue({
    el: '#app',
    data: {
        labelPosition: 'left',
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
                if (res.data.success) {
                    console.log(res.data);
                } else {
                    console.log(res.data);
                }
            });

            return true;
        }
    }
})