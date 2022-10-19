import request from '@/utils/request'

// 获取列表
export function getTenantInfoList(params = {}) {
    return request({
        url: '/Admin/TenantInfo/index',
        params,
        method: 'get'
    })
}

// 获取当前租户信息
export function getTenantInfo(params = {}) {
    return request({
        method: 'get',
        url: '/Admin/TenantInfo/Index?handler=RequestTenantInfo',
        params
    })
}
// 修改信息
export function setTenantInfo(data = {}) {
    return request({
        url: '/Admin/TenantInfo/Index?handler=Save',
        method: 'post',
        data
    })
}

// 删除数据
export function deleteTenantInfo(data = {}) {
    return request({
        method: 'post',
        url: '/Admin/TenantInfo/Index?handler=Delete',
        data
    })
}
