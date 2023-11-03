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
        initOptions() {
            axios.get("/Install/Index?handler=DefaultOptions")
                .then(response => {
                    var data = response.data.result.data;
                    this.installOptions.systemName = data.systemName;
                    console.log(data)
                    this.installOptions.adminUserName = data.adminUserName;
                    this.installOptions.dbConnectionString = data.dbConnectionString;
                })
                .catch(error => {
                    console.error("获取配置发生错误:", error);
                });
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
    },
    mounted() {
        this.initOptions();
    }
})