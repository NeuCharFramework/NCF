var app = new Vue({
    el: '#app',
    data: {
        isExpanded: false,
        installStarted: false,
        optionsModelList: [],
        installOptions: {
            systemName: "",
            adminUserName: "",
            dbConnectionString: "",
            needModelList:null  //模型名称列表
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
                    console.log(data);
                    this.installOptions.adminUserName = data.adminUserName;
                    this.installOptions.dbConnectionString = data.dbConnectionString;
                    this.optionsModelList = data.needModelList;
                })
                .catch(error => {
                    console.error("获取配置发生错误:", error);
                });
        },
        submit() {
            if (this.installStarted) {
                alert((window.ncfInstallI18n && window.ncfInstallI18n.started) || 'Installation is already running, please wait.');
                return false;
            }

            var confirmText = ((window.ncfInstallI18n && window.ncfInstallI18n.startConfirm) || 'Start installation?') + '\n' +
                ((window.ncfInstallI18n && window.ncfInstallI18n.startConfirmDetail) || 'This page will expire after installation.');
            if (!confirm(confirmText)) {
                return false;
            }

            this.installStarted = true;

            document.getElementById('btnInstall').setAttribute('disabled', true);
            document.getElementById('btnInstall').innerHTML = (window.ncfInstallI18n && window.ncfInstallI18n.startedWaiting) || 'Installation started, please wait...';

            axios.post("/Install/Index", this.installOptions, {
            }).then(res => {
                document.getElementById('app').innerHTML = res.data;
            }).catch(error => {
                document.getElementById('app').innerHTML = ((window.ncfInstallI18n && window.ncfInstallI18n.failedPrefix) || 'Install failed:') + error;
            });
            return true;
        }
    },
    mounted() {
        this.initOptions();
    }
})