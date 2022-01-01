/*
* 模块：Senparc.Areas.Admin:SysMenuAppService
* 菜单
* */

import request from '@/utils/request'
const baseUrl = "/api/Senparc.Areas.Admin/SysMenuAppService/Areas.Admin_SysMenuAppService"

//创建、更新
export function createOrUpdateMenu(data) {
  return request({
    url: `${baseUrl}.CreateOrUpdateAsync`,
    method: 'post',
    data
  })
}

//删除
export function deleteMenu(params) {
  return request({
    url: `${baseUrl}.DeleteMenuAsync`,
    method: 'delete',
    params
  })
}

//列表
export function getAllMenus(params) {
  return request({
    url: `${baseUrl}.GetAllMenusTreeAsync`,
    method: 'get',
    params
  })
}

//列表-子项
export function getMenus(params) {
  return request({
    url: `${baseUrl}.GetMenusAsync`,
    method: 'get',
    params
  })
}

//详情
export function GetMenuInfo(params) {
  return request({
    url: `${baseUrl}.GetMenuAsync`,
    method: 'get',
    params
  })
}



