# NCF 基础规范及约定

1. 所有 `Base` 结尾的文件及类型（如：`EntityBase.cs`、`IEntityBase.cs`），均为底层支持文件，虽然源代码已经开放给大家，建议不要修改，如果项目直接使用 `Senparc.Ncf.*` 的 Nuget 包，可以忽略此问题。所有允许修改的基类，`Base` 会放在开头，如 `BaseEntity`。

2. 如果没有特殊情况，所有数据库实体都标记为可序列化 `[Serializable]`。
