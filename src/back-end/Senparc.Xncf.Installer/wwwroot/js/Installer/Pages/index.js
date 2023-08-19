var app = new Vue({
    el: '#app',
    data() {
        return {
            labelPosition: 'left',
            isExpanded: false,
            installStarted: false,
            installOptions: {
                systemName: "Default",
                adminUserName: "admin",
                dbConnectionString: "123"
            }
        }
    },
    methods: {
        toggleInfo() {
            this.isExpanded = !this.isExpanded;
        },
        saveOptions() {
        },
        submit() {
            if (installStarted) {
                alert('安装已经在进行中，请等待');
                return false;
            }

            if (!confirm('确定要开始安装吗？安装完成后此页面将失效！')) {
                return false;
            }

            installStarted = true;

            document.getElementById('install_form').submit();
            document.getElementById('btnInstall').setAttribute('disabled', true);
            document.getElementById('btnInstall').innerHTML = '安装已启动，请稍后……';

            return true;
        }
    }
})