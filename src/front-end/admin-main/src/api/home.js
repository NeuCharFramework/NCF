import request from '@/utils/request'

export function getXncfStat() {
  return request({
    url: '/Admin/Index?handler=XncfStat',
    method: 'get'
  })
}

export function getXncfOpening() {
  return request({
    url: '/Admin/Index?handler=XncfOpening',
    method: 'get'
  })
}
