import { getInfo, login } from "@/api/admin";
import {
  getToken,
  removeRole,
  removeToken,
  setRole,
  setToken,
} from "@/utils/auth";
import router, { resetRouter } from "@/router";
import { Message } from "element-ui";
import store from "@/store";

const state = {
  token: getToken(),
  name: "",
  avatar: "",
  introduction: "",
  roles: [],
  menuTree: [],
  permissionCodes: [],
};

const mutations = {
  SET_TOKEN: (state, token) => {
    state.token = token;
  },
  SET_INTRODUCTION: (state, introduction) => {
    state.introduction = introduction;
  },
  SET_NAME: (state, name) => {
    state.name = name;
  },
  SET_AVATAR: (state, avatar) => {
    state.avatar = avatar;
  },
  SET_ROLES: (state, roles) => {
    state.roles = roles;
  },
  SET_MENUTREE: (state, menuTree) => {
    state.menuTree = menuTree;
  },
  SET_PERMISSIONCODES: (state, permissionCodes) => {
    state.permissionCodes = permissionCodes;
  },
};

const actions = {
  // user login    登录
  login({ commit }, userInfo) {
    const { username, password } = userInfo;
    return new Promise((resolve, reject) => {
      // 模拟登录
      // commit('SET_TOKEN', '123456789QWER')
      // setToken('123456789QWER')
      // resolve('123456789QWER')
      // 登录请求
      login({ username: username.trim(), password: password })
        .then((response) => {
          if (response.success) {
            const { data } = response;
            const { token } = data;
            commit("SET_TOKEN", token);
            setToken(token);
            resolve(data);
          } else {
            Message({
              message: response.errorMessage || "登录失败！",
              type: "error",
              duration: 5 * 1000,
            });
            reject(response);
          }
        })
        .catch((error) => {
          reject(error);
        });
    });
  },

  // get user info
  getInfo({ commit, state }) {
    return new Promise((resolve, reject) => {
      // console.log('获取信息')
      // 本地信息
      // const data = {
      //   introduction: 'i am admin',
      //   avatar: '',
      //   userName: 'admin',
      //   roleCodes: ['administrator'],
      //   menuTree: [],
      //   permissioncodes: []
      // }
      // const { roleCodes, avatar, userName, menuTree, permissioncodes, introduction } = data
      // commit('SET_ROLES', roleCodes)
      // setRole(roleCodes)
      // commit('SET_NAME', userName)
      // commit('SET_AVATAR', avatar)
      // commit('SET_INTRODUCTION', introduction)
      // commit('SET_MENUTREE', menuTree)
      // commit('SET_PERMISSIONCODES', permissioncodes)
      // resolve(data)

      // 获取账号信息
      getInfo(state.token)
        .then((response) => {
          // console.log('response', response);
          const { data } = response;
          if (!data) {
            reject("验证失败，请重新登录。");
          }
          // console.log("getInfo", JSON.parse(JSON.stringify(data)));
          const { roleCodes, userName, menuTree, permissionCodes } = data;
          // roles must be a non-empty array
          if (!roleCodes || roleCodes.length <= 0) {
            reject("getInfo: roles must be a non-null array!");
          }
          
          menuTree.forEach((item) => {
            if (item.menuName === "系统管理") {
              item.url = "/Admin";
              item.breadcrumb = true 
            }
            if (item.menuName === "扩展模块") {
              item.url = "/XncfModule";
              item.breadcrumb = true 
            }
          });

          const avatar = "";
          const introduction = "i am admin";

          commit("SET_ROLES", roleCodes);
          setRole(roleCodes);
          commit("SET_NAME", userName);
          commit("SET_AVATAR", avatar);
          commit("SET_INTRODUCTION", introduction);
          commit("SET_MENUTREE", menuTree);
          commit("SET_PERMISSIONCODES", permissionCodes);
          resolve(data);
        })
        .catch((error) => {
          reject(error);
        });
    });
  },

  // user logout
  logout({ commit, state, dispatch }) {
    return new Promise((resolve, reject) => {
      console.log("退出登录--必须移除token和roles");
      commit("SET_TOKEN", "");
      commit("SET_ROLES", []);
      removeToken(); // must remove  token  first
      removeRole();
      resetRouter();

      dispatch("tagsView/delAllViews", null, { root: true });
      // 刷新页面
      // location.reload()
      resolve();
      // 框架原本的逻辑
      // logout(state.token).then(() => {
      //   commit('SET_TOKEN', '')
      //   commit('SET_ROLES', [])
      //   removeToken()
      //   resetRouter()
      //
      //   // reset visited views and cached views
      //   // to fixed https://github.com/PanJiaChen/vue-element-admin/issues/2485
      //   dispatch('tagsView/delAllViews', null, { root: true })
      //
      //   resolve()
      // }).catch(error => {
      //   reject(error)
      // })
    });
  },

  // remove token
  resetToken({ commit }) {
    return new Promise((resolve) => {
      commit("SET_TOKEN", "");
      commit("SET_ROLES", []);
      removeToken();
      resolve();
    });
  },

  // dynamically modify permissions
  async changeRoles({ commit, dispatch }, role) {
    const token = role + "-token";

    commit("SET_TOKEN", token);
    setToken(token);

    const { roleCodes, menuTree } = await dispatch("getInfo");

    resetRouter();

    // 根据后端路由表生成可访问的路由
    const accessRoutes = await store.dispatch("permission/generateRoutes", {
      roleCodes,
      menuTree,
    });

    // 动态添加可访问的路由
    console.log("动态添加可访问的路由", accessRoutes);
    router.addRoutes(accessRoutes);

    // reset visited views and cached views
    dispatch("tagsView/delAllViews", null, { root: true });
  },
};

export default {
  namespaced: true,
  state,
  mutations,
  actions,
};
