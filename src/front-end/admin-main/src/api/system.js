import request from '@/utils/request'

const baseUrl  = '/Senparc.Areas.Admin/SystemInfoAppService/Areas.Admin_SystemInfoAppService'

// 获取列表
export function getSystemConfig(params = {}) {
  return request({
    url: `${baseUrl}.GetListAsync`,
    params,
    method: 'get'
  })
}

// 创建or修改信息
export function setSystemConfig(data = {}) {
  return request({
    url: `${baseUrl}.CreateOrUpdateAsync`,
    method: 'post',
    data
  })
}

// 打开or关闭 模块管理.
export function openHideManager(params) {
  return request({
    url: `${baseUrl}.HideManagerAsync`,
    method: 'post',
    params
  })
}