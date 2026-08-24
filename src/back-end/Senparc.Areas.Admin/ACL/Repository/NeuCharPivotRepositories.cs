/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharPivotRepositories.cs
    文件功能描述：NeuCharPivot 系统实体仓储


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL;

public interface INeuCharPivotConfigurationRepository : IClientRepositoryBase<NeuCharPivotConfiguration> { }
public interface INeuCharPivotFunctionRepository : IClientRepositoryBase<NeuCharPivotFunction> { }
public interface INeuCharPivotLoopTaskRepository : IClientRepositoryBase<NeuCharPivotLoopTask> { }
public interface INeuCharExecutionLogRepository : IClientRepositoryBase<NeuCharExecutionLog> { }

public sealed class NeuCharPivotConfigurationRepository : ClientRepositoryBase<NeuCharPivotConfiguration>, INeuCharPivotConfigurationRepository
{
    private NeuCharPivotConfigurationRepository() : base(null) { }
    public NeuCharPivotConfigurationRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharPivotFunctionRepository : ClientRepositoryBase<NeuCharPivotFunction>, INeuCharPivotFunctionRepository
{
    private NeuCharPivotFunctionRepository() : base(null) { }
    public NeuCharPivotFunctionRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharPivotLoopTaskRepository : ClientRepositoryBase<NeuCharPivotLoopTask>, INeuCharPivotLoopTaskRepository
{
    private NeuCharPivotLoopTaskRepository() : base(null) { }
    public NeuCharPivotLoopTaskRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharExecutionLogRepository : ClientRepositoryBase<NeuCharExecutionLog>, INeuCharExecutionLogRepository
{
    private NeuCharExecutionLogRepository() : base(null) { }
    public NeuCharExecutionLogRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}
