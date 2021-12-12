Vue.component('sidebar-item', {
    name: 'sidebar-item',
    template: `         <!--最里面一层-->
                      <template v-if="item.children.length<1">
                            <el-menu-item :index="item.index" @click="link(item)">
                                  <template slot='title'>
                                        <i v-if="item.icon" :class="''+item.icon"></i>
                                        <i v-if="!item.icon" class="el-icon-s-help"></i>
                                        <span>{{item.menuName}}</span>
                                  </template>
                            </el-menu-item>
                        </template>
                        <!--递归-->
                        <el-submenu v-else :index="item.index">
                            <template slot='title'>
                                <i v-if="item.icon" :class="''+item.icon"></i>
                                <i v-if="!item.icon" class="el-icon-s-help"></i>
                                <span >{{item.menuName}}</span>
                            </template>
                                <sidebar-item
                                    v-for="child in item.children"
                                    :key="child.id"
                                    :is-nest="true"
                                    :item="child"
                                    class="nest-menu"
                                  />
                        </el-submenu>
`,
    data() {
        return {
        };
    },
    props: {
        item: {
            type: Object,
            required: true
        }
    },
    methods: {
        link(item) {
            // 显示在首页ifram
            if (!item.url) return;
            window.location.href = item.url;
        }
    }
});
//基于 Element 的 el - pagination进行了二次封装，并拓展了自动滚动的功能。
//https://panjiachen.github.io/vue-element-admin-site/zh/feature/component/pagination.html#%E4%BD%BF%E7%94%A8%E6%96%B9%E5%BC%8F
Vue.component('pagination', {
    template: `
  <!-- 分页组件 -->
  <div :class="{'hidden':hidden}" class="pagination-container">
    <el-pagination
      :background="background"
      :current-page.sync="currentPage"
      :page-size.sync="pageSize"
      :layout="layout"
      :page-sizes="pageSizes"
      :total="total"
      v-bind="$attrs"
      @size-change="handleSizeChange"
      @current-change="handleCurrentChange"
    />
  </div>`,
    name: 'pagination',
    props: {
        total: {
            required: true,
            type: Number
        },
        page: {
            type: Number,
            default: 1
        },
        limit: {
            type: Number,
            default: 20
        },
        pageSizes: {
            type: Array,
            default() {
                return [10, 20, 30, 50];
            }
        },
        layout: {
            type: String,
            default: "total, sizes, prev, pager, next, jumper"
        },
        background: {
            type: Boolean,
            default: true
        },
        autoScroll: {
            type: Boolean,
            default: true
        },
        hidden: {
            type: Boolean,
            default: false
        }
    },
    computed: {
        //监听传进来的当前页和大小，有变化时更新 :page.sync="listQuery.page"和:limit.sync="listQuery.limit"
        currentPage: {
            get() {
                return this.page;
            },
            set(val) {
                this.$emit("update:page", val);
            }
        },
        pageSize: {
            get() {
                return this.limit;
            },
            set(val) {
                this.$emit("update:limit", val);
            }
        }
    },
    methods: {
        handleSizeChange(val) {
            this.$emit("pagination", { page: this.currentPage, limit: val });
            if (this.autoScroll) {
                scrollTo(0, 800);
            }
        },
        handleCurrentChange(val) {
            this.$emit("pagination", { page: val, limit: this.pageSize });
            if (this.autoScroll) {
                scrollTo(0, 800);
            }
        }
    }
});