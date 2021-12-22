/*
* 模块：Senparc.Areas.Admin:SysRoleAppService
* 角色
* */

import request from '@/utils/request'
const baseUrl = "/api/Senparc.Areas.Admin/SysRoleAppService/Areas.Admin_SysRoleAppService"

//创建Permission
export function createPermission(data) {
  return request({
    url: `${baseUrl}.AddPermissionAsync`,
    method: 'put',
    data
  })
}

//创建、更新
export function createOrUpdateRole(data) {
  return request({
    url: `${baseUrl}.CreateOrUpdateAsync`,
    method: 'post',
    data
  })
}

//删除
export function deleteRole(params) {
  return request({
    url: `${baseUrl}.DeleteSingeAsync`,
    method: 'delete',
    params
  })
}

//列表
export function getAllRoles(params) {
  return request({
    url: `${baseUrl}.GetListAsync`,
    method: 'get',
    params
  })
}

//列表
export function getRolePermissions(params) {
  return request({
    url: `${baseUrl}.GetRolePermissionsAsync`,
    method: 'get',
    params
  })
}


//详情
export function GetRoleInfo(params) {
  return request({
    url: `${baseUrl}.GetRolePermissionsAsync`,
    method: 'get',
    params
  })
}



