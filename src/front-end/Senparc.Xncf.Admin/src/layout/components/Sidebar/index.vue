<template>
<<<<<<< HEAD
  <div :class="{'has-logo':showLogo}">
    <logo v-if="showLogo" :collapse="isCollapse" />
    <el-scrollbar wrap-class="scrollbar-wrapper">
      <el-menu
        :default-active="activeMenu"
        :collapse="isCollapse"
        :background-color="variables.menuBg"
        :text-color="variables.menuText"
        :unique-opened="false"
        :active-text-color="variables.menuActiveText"
        :collapse-transition="false"
        mode="vertical"
      >
        <sidebar-item v-for="route in permission_routes" :key="route.path" :item="route" :base-path="route.path" />
      </el-menu>
    </el-scrollbar>
<!--    <div>{{ JSON.stringify(permission_routes) }}</div>-->
=======
  <div :class="{ 'has-logo': showLogo }">
    <logo v-if="showLogo" :collapse="isCollapse" />
    <!-- 菜单搜索-结果展示 -->
    <div style="padding: 20px">
      <el-input placeholder="搜索菜单" v-if="!isCollapse" v-model="searchValue" @input="changeMenu(permission_routes)"
                clearable />
    </div>
    <div v-if="allMenu.length === 0 && searchValue !== ''" style="text-align: center">
      <p style="font-size: 14px;color: white;margin-bottom: 5px">没有结果</p>
      <el-button size="mini" type="primary" @click="searchValue = ''; changeMenu(permission_routes)">点击清空搜索</el-button>
    </div>
    <!-- 菜单列表 + 筛选 -->
    <el-scrollbar wrap-class="scrollbar-wrapper">
      <el-menu :default-active="activeMenu" :collapse="isCollapse" :background-color="variables.menuBg"
               :text-color="variables.menuText" :unique-opened="false" :active-text-color="variables.menuActiveText"
               :collapse-transition="false" mode="vertical">
        <sidebar-item v-for="route in allMenu" :key="route.path" :item="route" :base-path="route.path"
                      :collapse="isCollapse" />
      </el-menu>
    </el-scrollbar>
>>>>>>> a24c880 (更新Senparc.Xncf.Admin模块)
  </div>
</template>

<script>
import { mapGetters } from 'vuex'
import Logo from './Logo'
import SidebarItem from './SidebarItem'
import variables from '@/styles/variables.scss'

export default {
  components: { SidebarItem, Logo },
<<<<<<< HEAD
=======
  data() {
    return {
      searchValue: '',
      allMenu: []
    }
  },
  methods: {
    changeMenu(menu) {
      // console.log(menu);
      if (!this.searchValue || this.searchValue === '') {
        this.allMenu = this.permission_routes
        return;
      }
      let res = []
      this.findMenu(menu, res)
      this.allMenu = res;
      // console.log('menu', this.allMenu);
    },
    findMenu(menu, arr) {
      menu.forEach(el => {
        if (el.meta && el.meta.title.includes(this.searchValue)) arr.push(el)
        if (el.children) {
          this.findMenu(el.children, arr)
        }
      })
    }
  },
>>>>>>> a24c880 (更新Senparc.Xncf.Admin模块)
  computed: {
    ...mapGetters([
      'permission_routes',
      'sidebar'
    ]),
    activeMenu() {
      const route = this.$route
      const { meta, path } = route
<<<<<<< HEAD
      // if set path, the sidebar will highlight the path you set
=======
      // 如果设置路径，侧边栏将突出显示您设置的路径
>>>>>>> a24c880 (更新Senparc.Xncf.Admin模块)
      if (meta.activeMenu) {
        return meta.activeMenu
      }
      return path
    },
    showLogo() {
      return this.$store.state.settings.sidebarLogo
    },
    variables() {
      return variables
    },
    isCollapse() {
      return !this.sidebar.opened
    }
  },
  mounted() {
<<<<<<< HEAD

=======
    this.allMenu = this.permission_routes// 初始化展示路由配置信息
    // console.log(this.permission_routes);
>>>>>>> a24c880 (更新Senparc.Xncf.Admin模块)
  }
}
</script>
