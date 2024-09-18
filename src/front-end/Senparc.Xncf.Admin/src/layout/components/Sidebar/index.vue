<template>
  <div :class="{'has-logo':showLogo}">
    <logo v-if="showLogo" :collapse="isCollapse" />
    <!--    <div v-if="!isCollapse" style="padding: 20px">-->
    <!--      <el-input v-model="searchValue" placeholder="搜索菜单" clearable @input="changeMenu(permission_routes)" />-->
    <!--    </div>-->
    <!--    <div v-if="allMenu.length===0&&searchValue!==''" style="text-align: center">-->
    <!--      <p style="font-size: 14px;color: white;margin-bottom: 5px">没有结果</p>-->
    <!--      <el-button size="mini" type="primary" @click="searchValue='';changeMenu(permission_routes)">点击清空搜索</el-button>-->
    <!--    </div>-->
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
        <sidebar-item v-for="route in allMenu" :key="route.path" :item="route" :base-path="route.path"  :collapse="isCollapse" />
      </el-menu>
    </el-scrollbar>

    <!--    <div>{{ JSON.stringify(permission_routes) }}</div>-->
  </div>
</template>

<script>
import { mapGetters } from 'vuex'
import Logo from './Logo'
import SidebarItem from './SidebarItem'
import variables from '@/styles/variables.scss'

export default {
  components: { SidebarItem, Logo },
  data() {
    return {
      searchValue: '',
      allMenu: []
    }
  },
  computed: {
    ...mapGetters([
      'permission_routes',
      'sidebar'
    ]),
    activeMenu() {
      const route = this.$route
      const { meta, path } = route
      // if set path, the sidebar will highlight the path you set
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
    this.allMenu = this.permission_routes
  },
  methods: {
    changeMenu(menu) {
      if (!this.searchValue || this.searchValue === '') {
        this.allMenu = this.permission_routes
        return;
      }
      const res = []
      this.findMenu(menu, res)

      this.allMenu = res;
    },
    findMenu(menu, arr) {
      menu.forEach(el => {
        if (el.meta && el.meta.title.includes(this.searchValue)) arr.push(el)
        if (el.children) {
          this.findMenu(el.children, arr)
        }
      })
    }
  }
}
</script>
