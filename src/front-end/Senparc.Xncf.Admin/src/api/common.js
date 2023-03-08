import request from '@/utils/request'
/**
 * 获取零时参数
 * @param {*} data 请求参数
 */
export function tencentCosCredential(data) {
    return request({
        url: `/Senparc.Xncf.TestModular/ApiAppService/Xncf.TestModular_ApiAppService.GetCosCredential`,
        method: 'post',
        data: data
    })
}