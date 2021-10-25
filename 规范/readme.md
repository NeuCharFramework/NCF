# NCF 基础规范及约定

1. 所有 `Base` 结尾的文件及类型（如：`EntityBase.cs`、`IEntityBase.cs`），均为底层支持文件，虽然源代码已经开放给大家，建议不要修改，如果项目直接使用 `Senparc.Ncf.*` 的 Nuget 包，可以忽略此问题。所有允许修改的基类，`Base` 会放在开头，如 `BaseEntity`。

2. 如果没有特殊情况，所有数据库实体都标记为可序列化 `[Serializable]`。

3.	Xncf 命名规则为：<br>
> `[公司/项目名].Xncf.[模块名]`<br>
> 如：`Senparc.Xncf.XncfBuilder`

当遇到完全依赖此模块的下级模块，可以继续用 . 分割，如：`Senparc.Xncf.XncfBuilder.Backup`。
4.	所有 OHS 层对接的服务都以 `AppService` 结尾，如：`TicketAppService`。
5.	所有 AppService 的请求参数超过 1 个时，需要进行封装，类名格式：
> `[AppService名称]_[方法（动作）名称]Request`<br>
> 如：`Ticket_BuyRequest`。
6.	所有 AppService 的响应消息有两种情况：<br>
a.	常规情况直接使用 `AppResponseBase<T>`，其中 `T` 可以为任意类型；<br>
b.	自定义更复杂的类型，需要创建专用的类，并继承 `AppResponseBase<T>`，类名格式：<br>
> `[AppService名称]_[方法（动作）名称]Response`<br>
> 如：`Ticket_BuyResponse`。
7.	所有属于 Domain 范畴的扩展服务，以 Service 结尾，暂时不限制输入和输出参数数量和名称。
