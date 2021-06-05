//import request from '@/utils/request'
// 加载列表
export function getList(params) {
  return request({
    url: '/BuilderTables/Load',
    method: 'get',
    params
  })
}
// 加载明细列表
export function getDetailList(params) {
  return request({
    url: '/BuilderTableColumns/Load',
    method: 'get',
    params
  })
}

// 创建一个代码生成的模版<br>会自动创建字段明细信息，添加成功后使用BuilderTableColumnsController.Load加载字段明细<returns>返回添加的模版ID</returns>
export function add(data) {
  return request({
    url: '/BuilderTables/Add',
    method: 'post',
    data
  })
}

// 只修改表信息，不会更新明细
export function update(data) {
  return request({
    url: '/BuilderTables/Update',
    method: 'post',
    data
  })
}

// 更新明细
export function updateDetail(data) {
  return request({
    url: '/BuilderTableColumns/Update',
    method: 'post',
    data
  })
}

// 批量删除代码生成模版和对应的字段信息
export function del(data) {
  return request({
    url: '/BuilderTables/Delete',
    method: 'post',
    data
  })
}
// 创建实体
export function CreateEntity(data) {
  return request({
    url: '/BuilderTables/CreateEntity',
    method: 'post',
    data
  })
}

// 创建业务逻辑
export function CreateBusiness(data) {
  return request({
    url: '/BuilderTables/CreateBusiness',
    method: 'post',
    data
  })
}

// 创建VUE界面
export function CreateVue(data) {
  return request({
    url: '/BuilderTables/CreateVue',
    method: 'post',
    data
  })
}

// 删除明细
export function delDetail(data) {
  return request({
    url: '/BuilderTableColumns/Delete',
    method: 'post',
    data
  })
}
