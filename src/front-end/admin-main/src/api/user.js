import request from '@/utils/request'

export function login(data) {
  return request({
    url: '/Admin/Login?handler=Login',
    // url: '/api/Senparc.Areas.Admin/AdminUserInfoAppService/Areas.Admin_AdminUserInfoAppService.LoginAsync',
    method: 'post',
    data
  })
}

//管理员管理
export function getAdminUserList(params) {
  return request({
    // url: '/Admin/AdminUserInfo/index',
    url: '/api/Senparc.Areas.Admin/AdminUserInfoAppService/Areas.Admin_AdminUserInfoAppService.GetList',
    method: 'get',
    params
  })
}


export function getInfo(token) {
  return request({
    url: '/vue-element-admin/user/info',
    method: 'get',
    params: { token }
  })
}

export function logout() {
  return request({
    url: '/vue-element-admin/user/logout',
    method: 'post'
  })
}
