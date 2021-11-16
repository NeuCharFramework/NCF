import request from '@/utils/request'
const url = '/api/template/index'

// 查
export function fetchTemplate(params) {
  return request({
    url: `${url}`,
    method: 'get',
    params
  })
}

// 增
export function createTemplate(data) {
  return request({
    url: `${url}/create`,
    method: 'post',
    data
  })
}

// 改
export function updateTemplate(data) {
  return request({
    url: `${url}/update`,
    method: 'post',
    data
  })
}

// 删
export function deleteTemplate(data) {
  return request({
    url: `${url}/delete`,
    method: 'post',
    data
  })
}
