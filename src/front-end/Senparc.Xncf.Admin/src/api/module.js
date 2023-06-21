import request from '@/utils/request'

const baseUrl = '/Senparc.Areas.Admin/ModuleAppService/Areas.Admin_ModuleAppService'

// XNCF 统计状态数据
export function getXncfStat() {
    return request({
        url: `${baseUrl}.StatAsync`,
        method: 'get'
    })
}

// 已安装模块数据
export function getModuleList() {
    return request({
        url: `${baseUrl}.GetInstalledListAsync`,
        method: 'get'
    })
}

// 未安装模块数据
export function getUnModuleList() {
    return request({
        url: `${baseUrl}.GetUnInstalledListAsync`,
        method: 'get'
    })
}


// 删除模块
export function deleteModule(id) {
    return request({
        url: `${baseUrl}.DeleteAsync`,
        method: 'delete',
        params: {
            id
        }
    })
}

// 获取单个模块 详情
export function getItemModule(uid) {
    return request({
        url: `${baseUrl}.GetItemAsync`,
        method: 'get',
        params: {
            uid
        }
    })
}

// 修改单个模块 状态
export function changeModuleState(params) {
    return request({
        url: `${baseUrl}.ChangeStateAsync`,
        method: 'put',
        params
    })
}

// 提交信息、执行方法
export function moduleRunFunction(data) {
    return request({
        url: `${baseUrl}.RunFunctionAsync`,
        method: 'post',
        data
    })
}
// 安装
export function moduleInstallXncf(uid) {
    return request({
        url: `${baseUrl}.InstallXncfModuleAsync?uid=${uid}`,
        method: 'get',
    })
}
