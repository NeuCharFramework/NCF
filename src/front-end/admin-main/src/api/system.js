import request from '@/utils/request'

// 获取列表
export function getSystemConfig(params = {}) {
  return request({
    url: '/Admin/SystemConfig/index',
    params,
    method: 'get'
  })
}

// 修改信息
export function setSystemConfig(data = {}) {
  return request({
    url: '/Admin/SystemConfig/Edit?handler=Save',
    method: 'post',
    data
  })
}
