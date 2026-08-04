var app = new Vue({
  el: "#app",
  data() {
    return {
      newTableData: [], // 新模块数据
      oldTableData: [], // 已安装模块
      updatedTableData: [], // 待更新模块
      isExtend: false, //是否切换状态
      handlerText: "",
      handlerTips: "",
      newData: {},
      oldData: {
        state: {
          0: ncfT('Xncf.Close'),
          1: ncfT('Xncf.Open'),
          2: ncfT('Xncf.NewModules'),
          3: ncfT('Xncf.PendingUpdates')
        }
      },
      newTableSearch: '',
      oldTableSearch: ''
    };
  },
  watch: {
    'isExtend': {
      handler: function (val, oldVal) {
        this.handlerText = val ? ncfT('Xncf.EnableManager') : ncfT('Xncf.HideManager');
        this.handlerTips = val ? ncfT('Xncf.EnableManagerConfirm') : ncfT('Xncf.HideManagerConfirm');
      },
      immediate: true
    }
  },
  created: function () {
    this.getList();
  },
  methods: {
    // 获取
    async getList() {
      const oldTableData = await service.get('/Admin/XncfModule/Index?handler=Mofules');
      this.oldTableData = oldTableData.data.data.result;
      // 是否切换状态
      this.isExtend = oldTableData.data.data.hideModuleManager;
      const newTableData = await service.get('/Admin/XncfModule/Index?handler=UnMofules');
      this.newTableData = newTableData.data.data;

      const updatedTableData = await service.get('/Admin/XncfModule/Index?handler=UpdatedMofules');
      this.updatedTableData = updatedTableData.data.data;
    },
    // 切换状态
    async handleSwitch() {
      await service.post('/Admin/XncfModule/Index?handler=HideManager');
      this.isExtend = !this.isExtend;
      window.location.href = "/Admin/Index";
    },
    // 安装
    async handleInstall(index, row) {
      await service.get(`/Admin/XncfModule/Index?handler=ScanAjax&uid=${row.uid}`);
      window.sessionStorage.setItem('setNavMenuActive', row.menuName);
      getNavMenu();
      // 跳转到模块详情
      setTimeout(function () {
        window.location.href = `/Admin/XncfModule/Start/?uid=${row.uid}`;
      }, 100);
    },
    // 操作
    handleHandle(index, row) {
      window.location.href = "/Admin/XncfModule/Start/?uid=" + row.xncfRegister.uid;
    },
    // 主页
    handleIndex(index, row) {
      window.location.href = row.xncfRegister.homeUrl;
    }
  }
});
