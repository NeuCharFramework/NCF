/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharPivotService.cs
    文件功能描述：NeuCharPivot 声明式布局规范化、存储与查询


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

    修改标识：Senparc - 20260829
    修改描述：v0.7.0 新增 NeuCharPivot 全局浮动调用与工作流分析管理能力

----------------------------------------------------------------*/

using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class NeuCharPivotConfigurationService : BaseClientService<NeuCharPivotConfiguration>
{
    public NeuCharPivotConfigurationService(INeuCharPivotConfigurationRepository repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }
}

public sealed class NeuCharPivotFunctionService : BaseClientService<NeuCharPivotFunction>
{
    public NeuCharPivotFunctionService(INeuCharPivotFunctionRepository repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }
}

public sealed class NeuCharPivotLoopTaskService : BaseClientService<NeuCharPivotLoopTask>
{
    public NeuCharPivotLoopTaskService(INeuCharPivotLoopTaskRepository repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }
}

public sealed class NeuCharExecutionLogService : BaseClientService<NeuCharExecutionLog>
{
    public NeuCharExecutionLogService(INeuCharExecutionLogRepository repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }
}

public sealed class NeuCharPivotLayout
{
    public int Version { get; set; } = 1;
    public string Title { get; set; }
    public string Description { get; set; }
    public int Columns { get; set; } = 2;
    public List<NeuCharPivotPanel> Panels { get; set; } = new();

    // Legacy JSON used sections[] directly. Keep a separate deserialization slot so
    // normalized layouts are persisted in the new panels[] shape only.
    [JsonPropertyName("sections")]
    public List<NeuCharPivotSection> LegacySections { get; set; }

    [JsonIgnore]
    public List<NeuCharPivotSection> Sections { get; set; } = new();
}

public sealed class NeuCharPivotPanel
{
    public string Key { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Type { get; set; } = "shortcuts";
    public int Columns { get; set; } = 2;
    public List<NeuCharPivotSection> Sections { get; set; } = new();
}

public sealed class NeuCharPivotSection
{
    public string Title { get; set; }
    public List<NeuCharPivotLayoutFunction> Functions { get; set; } = new();
}

public sealed class NeuCharPivotLayoutFunction
{
    public string FunctionKey { get; set; }
    public string Title { get; set; }
    public string Summary { get; set; }
    public string Accent { get; set; }
    public List<string> ExposedParameters { get; set; } = new();
}

public sealed class NeuCharPivotParameterSchema
{
    public string Name { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public int ParameterType { get; set; }
    public string SystemType { get; set; }
    public int MaxLength { get; set; }
    public bool Filterable { get; set; }
    public bool AllowCreate { get; set; }
    public object DefaultValue { get; set; }
    public List<NeuCharPivotParameterOption> Options { get; set; } = new();
}

public sealed record NeuCharPivotParameterOption(
    string Value,
    string Text,
    string Note,
    bool DefaultSelected);

public sealed record NeuCharPivotSnapshot(
    NeuCharPivotConfiguration Configuration,
    IReadOnlyList<NeuCharPivotFunction> Functions,
    IReadOnlyDictionary<int, NeuCharPivotLoopTask> LoopTasks,
    IReadOnlyDictionary<int, bool> FunctionAvailability,
    bool ModuleAvailable,
    string ModuleState,
    IReadOnlyDictionary<string, NeuCharFunctionDescriptor> FunctionCatalog = null);

public sealed record NeuCharPivotWorkflowOption(
    int Id,
    string Name,
    string Description,
    IReadOnlyList<string> Parameters);

public sealed class NeuCharPivotService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private static readonly HashSet<string> AllowedAccents = new(StringComparer.OrdinalIgnoreCase)
    {
        "blue", "green", "orange", "purple", "gray"
    };

    private readonly NeuCharPivotConfigurationService _configurationService;
    private readonly NeuCharPivotFunctionService _functionService;
    private readonly NeuCharPivotLoopTaskService _loopTaskService;
    private readonly NeuCharFunctionService _catalogService;

    public NeuCharPivotService(
        NeuCharPivotConfigurationService configurationService,
        NeuCharPivotFunctionService functionService,
        NeuCharPivotLoopTaskService loopTaskService,
        NeuCharFunctionService catalogService)
    {
        _configurationService = configurationService;
        _functionService = functionService;
        _loopTaskService = loopTaskService;
        _catalogService = catalogService;
    }

    public async Task<NeuCharPivotSnapshot> GetSnapshotAsync(
        string moduleUid,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _configurationService.GetObjectAsync(z => z.ModuleUid == moduleUid)
            .ConfigureAwait(false);
        if (configuration == null)
        {
            return null;
        }

        var functions = await _functionService.GetFullListAsync(
            z => z.PivotId == configuration.Id,
            z => z.Sort,
            OrderingType.Ascending).ConfigureAwait(false);
        var functionIds = functions.Select(z => z.Id).ToHashSet();
        var loopTasks = functionIds.Count == 0
            ? new List<NeuCharPivotLoopTask>()
            : await _loopTaskService.GetFullListAsync(
                z => functionIds.Contains(z.FunctionId),
                z => z.Id,
                OrderingType.Ascending).ConfigureAwait(false);

        var catalog = await _catalogService.GetCatalogAsync(moduleUid, true, cancellationToken)
            .ConfigureAwait(false);
        var available = catalog.Any() && catalog.All(z => z.ModuleAvailable);
        var availableFunctionKeys = catalog
            .Where(z => z.ModuleAvailable)
            .Select(z => z.FunctionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var functionAvailability = functions.ToDictionary(
            z => z.Id,
            z => availableFunctionKeys.Contains(z.FunctionKey));
        var state = catalog.Count == 0
            ? "missing"
            : available ? "open" : "disabled";

        return new NeuCharPivotSnapshot(
            configuration,
            functions,
            loopTasks.ToDictionary(z => z.FunctionId),
            functionAvailability,
            available,
            state,
            catalog.ToDictionary(z => z.FunctionKey, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<NeuCharPivotSnapshot>> GetAllSnapshotsAsync(
        CancellationToken cancellationToken = default)
    {
        var configurations = await _configurationService.GetFullListAsync(
            z => true,
            z => z.LastUpdateTime,
            OrderingType.Descending).ConfigureAwait(false);
        var result = new List<NeuCharPivotSnapshot>();
        foreach (var configuration in configurations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = await GetSnapshotAsync(configuration.ModuleUid, cancellationToken).ConfigureAwait(false);
            if (snapshot != null)
            {
                result.Add(snapshot);
            }
        }
        return result;
    }

    public async Task<NeuCharPivotSnapshot> SaveGeneratedLayoutAsync(
        string moduleUid,
        string requirement,
        int aiModelId,
        int adminUserId,
        int? chatSessionId,
        string candidateSchemaJson,
        CancellationToken cancellationToken = default)
    {
        var catalog = await _catalogService.GetCatalogAsync(moduleUid, true, cancellationToken)
            .ConfigureAwait(false);
        if (catalog.Count == 0)
        {
            throw new InvalidOperationException("当前模块没有可用于 NeuCharPivot 的 Function。");
        }

        var normalized = NormalizeLayout(candidateSchemaJson, catalog);
        var schemaJson = JsonSerializer.Serialize(normalized, JsonOptions);
        var configuration = await _configurationService.GetObjectAsync(z => z.ModuleUid == moduleUid)
            .ConfigureAwait(false);
        if (configuration == null)
        {
            configuration = new NeuCharPivotConfiguration(
                moduleUid,
                normalized.Title ?? catalog[0].ModuleName,
                adminUserId);
        }
        configuration.ApplyGeneratedLayout(
            normalized.Title,
            requirement,
            schemaJson,
            aiModelId,
            adminUserId,
            chatSessionId);
        await _configurationService.SaveObjectAsync(configuration).ConfigureAwait(false);

        var existing = await _functionService.GetFullListAsync(
            z => z.PivotId == configuration.Id,
            z => z.Id,
            OrderingType.Ascending).ConfigureAwait(false);
        var existingByKey = existing.ToDictionary(z => z.FunctionKey, StringComparer.OrdinalIgnoreCase);
        var order = 0;
        var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var section in normalized.Panels.SelectMany(z => z.Sections))
        {
            foreach (var layoutFunction in section.Functions)
            {
                var descriptor = catalog.First(z => string.Equals(
                    z.FunctionKey,
                    layoutFunction.FunctionKey,
                    StringComparison.OrdinalIgnoreCase));
                activeKeys.Add(descriptor.FunctionKey);

                if (!existingByKey.TryGetValue(descriptor.FunctionKey, out var entity))
                {
                    entity = new NeuCharPivotFunction(
                        configuration.Id,
                        descriptor.ModuleUid,
                        descriptor.FunctionKey,
                        descriptor.Name,
                        descriptor.Description);
                }

                var parameterSchema = BuildParameterSchema(descriptor, layoutFunction.ExposedParameters);
                var defaultParameters = BuildDefaultParameters(parameterSchema);
                entity.Update(
                    layoutFunction.Title ?? descriptor.Name,
                    layoutFunction.Summary ?? descriptor.Description,
                    JsonSerializer.Serialize(parameterSchema, JsonOptions),
                    JsonSerializer.Serialize(defaultParameters, JsonOptions),
                    descriptor.ModuleVersion,
                    order++,
                    true);
                await _functionService.SaveObjectAsync(entity).ConfigureAwait(false);
            }
        }

        foreach (var stale in existing.Where(z => !activeKeys.Contains(z.FunctionKey)))
        {
            stale.Update(
                stale.FunctionName,
                stale.Description,
                stale.UiSchemaJson,
                stale.DefaultParametersJson,
                stale.ModuleVersion,
                stale.Sort,
                false);
            await _functionService.SaveObjectAsync(stale).ConfigureAwait(false);
        }

        return await GetSnapshotAsync(moduleUid, cancellationToken).ConfigureAwait(false);
    }

    public string BuildFallbackSchemaJson(IReadOnlyList<NeuCharFunctionDescriptor> catalog)
    {
        var layout = NormalizeLayout(null, catalog);
        return JsonSerializer.Serialize(layout, JsonOptions);
    }

    public string GetCurrentParameterSchemaJson(
        NeuCharPivotFunction function,
        IReadOnlyDictionary<string, NeuCharFunctionDescriptor> catalog)
    {
        if (function == null ||
            catalog == null ||
            !catalog.TryGetValue(function.FunctionKey, out var descriptor))
        {
            return function?.UiSchemaJson ?? "[]";
        }

        // A persisted Pivot schema may have been generated by an older AI response
        // that omitted usable optional inputs. The live Function contract is the
        // authoritative source for the executable parameter surface.
        var exposedParameters = descriptor.Parameters
            .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Name))
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currentSchema = BuildParameterSchema(descriptor, exposedParameters);
        return JsonSerializer.Serialize(currentSchema, JsonOptions);
    }

    public NeuCharPivotLayout NormalizeLayout(
        string candidateSchemaJson,
        IReadOnlyList<NeuCharFunctionDescriptor> catalog)
    {
        NeuCharPivotLayout layout = null;
        if (!string.IsNullOrWhiteSpace(candidateSchemaJson))
        {
            try
            {
                var json = ExtractJsonObject(candidateSchemaJson);
                layout = JsonSerializer.Deserialize<NeuCharPivotLayout>(json, JsonOptions);
            }
            catch (JsonException)
            {
                layout = null;
            }
        }

        layout ??= new NeuCharPivotLayout
        {
            Title = catalog.FirstOrDefault()?.ModuleName ?? "NeuCharPivot",
            Description = "由模块 Functions 生成的快捷操作面板",
            Columns = 2,
            Panels = new List<NeuCharPivotPanel>()
        };
        layout.Version = 1;
        layout.Title = CleanText(layout.Title, 120) ?? catalog.FirstOrDefault()?.ModuleName ?? "NeuCharPivot";
        layout.Description = CleanText(layout.Description, 500) ?? string.Empty;
        layout.Columns = Math.Clamp(layout.Columns, 1, 3);
        layout.Panels ??= new List<NeuCharPivotPanel>();
        var sourcePanels = layout.Panels.Count > 0
            ? layout.Panels
            : new List<NeuCharPivotPanel>
            {
                new()
                {
                    Key = "shortcuts",
                    Title = "快捷操作",
                    Description = "常用 Function 的参数化执行面板",
                    Type = "shortcuts",
                    Columns = layout.Columns,
                    Sections = layout.LegacySections ?? new List<NeuCharPivotSection>()
                }
            };

        var catalogByKey = catalog.ToDictionary(z => z.FunctionKey, StringComparer.OrdinalIgnoreCase);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalizedPanels = new List<NeuCharPivotPanel>();
        foreach (var panel in sourcePanels.Take(12))
        {
            var normalizedSections = new List<NeuCharPivotSection>();
            foreach (var section in (panel.Sections ?? new List<NeuCharPivotSection>()).Take(12))
            {
                var normalizedFunctions = new List<NeuCharPivotLayoutFunction>();
                foreach (var item in (section.Functions ?? new List<NeuCharPivotLayoutFunction>()).Take(100))
                {
                    if (string.IsNullOrWhiteSpace(item.FunctionKey) ||
                        !catalogByKey.TryGetValue(item.FunctionKey, out var descriptor) ||
                        !used.Add(descriptor.FunctionKey))
                    {
                        continue;
                    }

                    normalizedFunctions.Add(NormalizeFunction(item, descriptor));
                }

                if (normalizedFunctions.Count > 0)
                {
                    normalizedSections.Add(new NeuCharPivotSection
                    {
                        Title = CleanText(section.Title, 100) ?? "快捷操作",
                        Functions = normalizedFunctions
                    });
                }
            }

            if (normalizedSections.Count > 0)
            {
                normalizedPanels.Add(new NeuCharPivotPanel
                {
                    Key = CleanKey(panel.Key, $"panel-{normalizedPanels.Count + 1}"),
                    Title = CleanText(panel.Title, 100) ?? (normalizedPanels.Count == 0 ? "快捷操作" : $"面板 {normalizedPanels.Count + 1}"),
                    Description = CleanText(panel.Description, 500) ?? string.Empty,
                    Type = NormalizePanelType(panel.Type),
                    Columns = Math.Clamp(panel.Columns <= 0 ? layout.Columns : panel.Columns, 1, 3),
                    Sections = normalizedSections
                });
            }
        }

        var missing = catalog.Where(z => !used.Contains(z.FunctionKey)).ToList();
        if (missing.Count > 0)
        {
            if (normalizedPanels.Count == 0)
            {
                normalizedPanels.Add(new NeuCharPivotPanel
                {
                    Key = "shortcuts",
                    Title = "快捷操作",
                    Description = "常用 Function 的参数化执行面板",
                    Type = "shortcuts",
                    Columns = layout.Columns,
                    Sections = new List<NeuCharPivotSection>()
                });
            }

            normalizedPanels[^1].Sections.Add(new NeuCharPivotSection
            {
                Title = normalizedPanels[^1].Sections.Count == 0 ? "快捷操作" : "更多功能",
                Functions = missing.Select(z => NormalizeFunction(null, z)).ToList()
            });
        }

        layout.Panels = normalizedPanels;
        layout.LegacySections = null;
        layout.Sections = normalizedPanels.FirstOrDefault()?.Sections ?? new List<NeuCharPivotSection>();
        return layout;
    }

    private static NeuCharPivotLayoutFunction NormalizeFunction(
        NeuCharPivotLayoutFunction item,
        NeuCharFunctionDescriptor descriptor)
    {
        var parameterNames = descriptor.Parameters.Select(z => z.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requiredParameterNames = descriptor.Parameters
            .Where(parameter => parameter.IsRequired)
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasOptionalSelection = item?.ExposedParameters?.Any(
            parameter => !requiredParameterNames.Contains(parameter)) == true;
        IEnumerable<string> requestedParameters = item == null ||
            item.ExposedParameters == null ||
            item.ExposedParameters.Count == 0 ||
            !hasOptionalSelection
            ? parameterNames
            : item.ExposedParameters;
        var exposed = requestedParameters
            .Where(parameterNames.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(24)
            .ToList();
        foreach (var required in descriptor.Parameters.Where(z => z.IsRequired))
        {
            if (!exposed.Contains(required.Name, StringComparer.OrdinalIgnoreCase))
            {
                exposed.Add(required.Name);
            }
        }

        return new NeuCharPivotLayoutFunction
        {
            FunctionKey = descriptor.FunctionKey,
            Title = CleanText(item?.Title, 120) ?? descriptor.Name,
            Summary = CleanText(item?.Summary, 500) ?? descriptor.Description,
            Accent = AllowedAccents.Contains(item?.Accent ?? string.Empty) ? item.Accent.ToLowerInvariant() : "blue",
            ExposedParameters = exposed
        };
    }

    private static string NormalizePanelType(string value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "shortcuts" or "summary" or "workflow" => value.Trim().ToLowerInvariant(),
            _ => "shortcuts"
        };

    private static string CleanKey(string value, string fallback)
    {
        var cleaned = new string((value ?? string.Empty)
            .Trim()
            .Where(char.IsLetterOrDigit)
            .ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned[..Math.Min(80, cleaned.Length)];
    }

    public static List<NeuCharPivotParameterSchema> BuildParameterSchema(
        NeuCharFunctionDescriptor descriptor,
        IReadOnlyCollection<string> exposedParameters)
    {
        var exposed = exposedParameters?.ToHashSet(StringComparer.OrdinalIgnoreCase)
                      ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return descriptor.Parameters
            .Where(z => z.IsRequired || exposed.Contains(z.Name))
            .Select(z => new NeuCharPivotParameterSchema
            {
                Name = z.Name,
                Title = z.Title,
                Description = z.Description,
                Required = z.IsRequired,
                ParameterType = (int)z.ParameterType,
                SystemType = z.SystemType,
                MaxLength = z.MaxLength,
                Filterable = z.Filterable,
                AllowCreate = z.AllowCreate,
                DefaultValue = z.ParameterType == ParameterType.Password ? null : z.Value,
                Options = z.SelectionList?.Items?.Select(item => new NeuCharPivotParameterOption(
                    item.Value,
                    item.Text,
                    item.Note,
                    item.DefaultSelected)).ToList() ?? new List<NeuCharPivotParameterOption>()
            }).ToList();
    }

    public static Dictionary<string, object> BuildDefaultParameters(
        IEnumerable<NeuCharPivotParameterSchema> parameters)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters)
        {
            object value = parameter.DefaultValue;
            if (parameter.ParameterType == (int)ParameterType.CheckBoxList)
            {
                value = parameter.Options.Where(z => z.DefaultSelected).Select(z => z.Value).ToArray();
            }
            else if (parameter.ParameterType == (int)ParameterType.DropDownList && value == null)
            {
                value = parameter.Options.FirstOrDefault(z => z.DefaultSelected)?.Value
                        ?? parameter.Options.FirstOrDefault()?.Value;
            }
            else if (parameter.ParameterType == (int)ParameterType.CheckBox && value == null)
            {
                value = false;
            }
            result[parameter.Name] = value;
        }
        return result;
    }

    private static string ExtractJsonObject(string value)
    {
        var start = value.IndexOf('{');
        var end = value.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new JsonException("No JSON object found.");
        }
        return value.Substring(start, end - start + 1);
    }

    private static string CleanText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        var clean = value.Replace("<", string.Empty).Replace(">", string.Empty).Trim();
        return clean.Length <= maxLength ? clean : clean[..maxLength];
    }
}
