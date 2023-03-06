import request from "@/utils/request"
const baseUrl = ""//待定共用

// 发送消息
export function postNews(data) {
    return request({
        url: "/Senparc.Xncf.OpenAI/GPT3AppService/Xncf.OpenAI_GPT3AppService.CreateCompletionStreamAsync",
        data,
        method: 'post'
    })
}

export function getOpen(data = {}) {
    console.log(data);
    return request({
        url: "",
        data,
        method: 'get'
    })
}


export function postOpen(data = {}) {
    console.log(data);
    return request({
        url: "",
        data,
        method: 'post'
    })
}

export function putOpen(data = {}) {
    console.log(data);
    return request({
        url: "",
        data,
        method: 'put'
    })
}

export function deleteOpen(data = {}) {
    console.log(data);
    return request({
        url: "",
        data,
        method: 'delete'
    })
}