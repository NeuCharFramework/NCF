import request from '@/utils/request'

const baseUrl  = '/Senparc.Areas.Admin/ModuleAppService/Areas.Admin_ModuleAppService'

// XNCF 统计状态数据
export function getXncfStat() {
  return request({
    url: `${baseUrl}.StatAsync`,
    method: 'get'
  })
}

// 开放模块数据
export function getXncfOpening() {
  return request({
    url: '/Admin/Index?handler=XncfOpening',
    method: 'get'
  })
}
