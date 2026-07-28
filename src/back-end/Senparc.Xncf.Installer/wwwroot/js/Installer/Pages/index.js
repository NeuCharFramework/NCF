var app = new Vue({
    el: '#app',
    data: {
        isExpanded: false,
        installStarted: false,
        installConfirmationVisible: false,
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
                    this.optionsModelList = data.needModelList || [];
                    this.installOptions.needModelList = this.optionsModelList
                        .filter(item => item.selectedByDefault)
                        .map(item => item.uid);
                })
                .catch(error => {
                    console.error((window.ncfInstallI18n && window.ncfInstallI18n.loadOptionsFailed) || 'Failed to load installation options:', error);
                });
        },
        submit() {
            if (this.installStarted) {
                this.$message({
                    message: (window.ncfInstallI18n && window.ncfInstallI18n.started) || 'Installation is already running, please wait.',
                    type: 'info',
                    showClose: true
                });
                return false;
            }

            if (this.installConfirmationVisible) {
                return false;
            }

            var i18n = window.ncfInstallI18n || {};
            this.installConfirmationVisible = true;

            this.$confirm(
                i18n.startConfirmDetail || 'This page will expire after installation.',
                i18n.startConfirm || 'Start installation?',
                {
                    confirmButtonText: i18n.confirmButtonText || 'Install now',
                    cancelButtonText: i18n.cancelButtonText || 'Cancel',
                    type: 'warning',
                    closeOnClickModal: false,
                    distinguishCancelAndClose: true
                })
                .then(() => {
                    this.installConfirmationVisible = false;
                    this.startInstallation();
                })
                .catch(action => {
                    this.installConfirmationVisible = false;
                    if (action !== 'cancel' && action !== 'close') {
                        console.error('Failed to display the installation confirmation:', action);
                    }
                });

            return false;
        },
        startInstallation() {
            if (this.installStarted) {
                return false;
            }

            this.installStarted = true;

            var installButton = document.getElementById('btnInstall');
            installButton.setAttribute('disabled', 'disabled');
            installButton.setAttribute('aria-disabled', 'true');
            installButton.innerHTML = (window.ncfInstallI18n && window.ncfInstallI18n.startedWaiting) || 'Installation started, please wait...';

            axios.post("/Install/Index", this.installOptions, {
                headers: {
                    RequestVerificationToken: document.querySelector('input[name="__RequestVerificationToken"]').value
                },
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
