import request from '@/utils/request'

export function login(data) {
  return request({
    url: '/Admin/Login?handler=Login',
    method: 'post',
    data
  })
}

export function getAdminUserList(params) {
  return request({
    // url: '/Admin/AdminUserInfo/index',
    url: '/api/Senparc.Areas.Admin/AdminUserInfoService/Areas.Admin_AdminUserInfoService.GetList',
    method: 'get',
    params
  })
}

// export function getInfo(token) {
//   return request({
//     url: '/vue-element-admin/user/info',
//     method: 'get',
//     params: { token }
//   })
// }
//
// export function logout() {
//   return request({
//     url: '/vue-element-admin/user/logout',
//     method: 'post'
//   })
// }
