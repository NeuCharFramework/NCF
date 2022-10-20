/*
* 模块：Senparc.Areas.Admin:SysMenuAppService
* 菜单
* */

import request from '@/utils/request'
const baseUrl = '/Senparc.Areas.Admin/SysMenuAppService/Areas.Admin_SysMenuAppService'

// 创建、更新
export function createOrUpdateMenu(data) {
  return request({
    url: `${baseUrl}.CreateOrUpdateAsync`,
    method: 'post',
    data
  })
}

// 获取完整菜单  
export function getFullMenus(params) {
  // ?hasButton=true
  return request({
    url: `${baseUrl}.GetAllMenuListAsync`,
    method: 'get',
    params
  })
}

// 删除
export function deleteMenu(id) {
  return request({
    url: `${baseUrl}.DeleteMenuAsync`,
    method: 'delete',
    params:{
      id
    }
  })
}

// 获取菜单树
export function getTreeMenus(params) {
  return request({
    url: `${baseUrl}.GetAllMenusTreeAsync`,
    method: 'get',
    params
  })
}

// 获取菜单列表
export function getMenus(params) {
  return request({
    url: `${baseUrl}.GetMenusAsync`,
    method: 'get',
    params
  })
}

// 获取菜单详情
export function GetMenuInfo(params) {
  return request({
    url: `${baseUrl}.GetMenuAsync`,
    method: 'get',
    params
  })
}
