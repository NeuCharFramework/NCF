/*
* 模块：Senparc.Areas.Admin:AdminUserInfoAppService
*  管理员相关
* */
import request from '@/utils/request'
const baseUrl = '/Senparc.Areas.Admin/AdminUserInfoAppService/Areas.Admin_AdminUserInfoAppService'

// 登录
export function login(data) {
  return request({
    url: `${baseUrl}.LoginAsync`,
    method: 'post',
    data
  })
}

// 管理员-列表
export function getAdminUserList(params) {
  return request({
    url: `${baseUrl}.GetList`,
    method: 'get',
    params
  })
}

// 管理员-创建
export function createAdminUser(data) {
  return request({
    url: `${baseUrl}.Create`,
    method: 'post',
    data
  })
}

// 管理员-更新
export function updateAdminUser(data) {
  return request({
    url: `${baseUrl}.Update`,
    method: 'put',
    data
  })
}

// 创建角色
export function createRole(data) {
  return request({
    url: `${baseUrl}.AddRoleAsync`,
    method: 'put',
    data
  })
}

// 删除角色
export function deleteRole(id) {
  return request({
    url: `${baseUrl}.DeleteAsync`,
    method: 'delete',
    params:{
      id
    }
  })
}

// 获取角色列表
export function getRoles(data) {
  return request({
    url: `${baseUrl}.GetRolesAsync`,
    method: 'put',
    data
  })
}

// 获取账号信息
export function getInfo() {
  return request({
    url: `${baseUrl}.GetAdminUserInfoAsync`,
    method: 'get'
  })
}

// 退出登录
export function logout() {
  return request({
    url: `${baseUrl}.logout`,
    method: 'post'
  })
}