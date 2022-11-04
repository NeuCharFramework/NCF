import axios from "axios";
import { MessageBox, Message } from "element-ui";
import store from "@/store";
import { getToken, isHaveToken } from "@/utils/auth";



// create an axios instance
const service = axios.create({
  baseURL: process.env.VUE_APP_BASE_API, // url = base url + request url
  timeout: 5000, // request timeout
});

// request interceptor  请求拦截
service.interceptors.request.use((config) => {
  // console.log('token', store.getters.token);
  if (store.getters.token) {
    // 让每个请求携带令牌 ['X-Token'] 是自定义标题键
    const _token = getToken();
    config.headers["Authorization"] = `Bearer ${_token}`;
  }
  return config;
},
  (error) => {
    console.log(error); // for debug
    return Promise.reject(error);
  }
);

// response interceptor
service.interceptors.response.use(
  (response) => {
    // 正常执行
    const res = response.data;
    return response.status === 200 ? Promise.resolve(res) : Promise.reject(res);
  },
  (error) => {
    const { response } = error;
    if (response && response.status === 401) {
      Message({
        message: "登录超时请重新登录",
        type: "error",
        duration: 5 * 1000,
      });
      window.location.replace = `/login?redirect=${router.history.current.fullPath}`;
      return;
    }
    Message({
      // message: response.errorMessage || response.data,
      message: response.errorMessage || '请求失败',
      type: "error",
      duration: 5 * 1000,
    });
    return Promise.reject(error);
  }
);

export default service;
