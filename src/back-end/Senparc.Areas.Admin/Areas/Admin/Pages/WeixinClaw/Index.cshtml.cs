/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml.cs implementation and project behavior.


    创建标识：Senparc - 20260928

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.WeixinClawIntegration;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.WeixinManager.Domain.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages.WeixinClaw;

[IgnoreAuth]
public sealed class IndexModel(
    IServiceProvider serviceProvider,
    IOptions<WeixinClawAdminIntegrationOptions> options,
    WeixinClawBindingProfileService bindingProfileService,
    IAdminWorkContextProvider adminWorkContextProvider) : BaseAdminPageModel(serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly WeixinClawAdminIntegrationOptions _options = options.Value;
    private readonly WeixinClawBindingProfileService _bindingProfileService = bindingProfileService;
    private readonly IAdminWorkContextProvider _adminWorkContextProvider = adminWorkContextProvider;

    public WeixinClawIntegrationPageDto PageData { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentAdminUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        var workflowUserId = currentAdminUserId;
        var workflowProvider = _serviceProvider.GetService<IWorkflowFunctionCallingProvider>();
        var bindingProfiles = await _bindingProfileService.GetAllDtosAsync().ConfigureAwait(false);
        var workflows = workflowProvider == null || workflowUserId <= 0
            ? Array.Empty<WorkflowFunctionCallingDescriptor>()
            : await workflowProvider.GetAvailableAsync(workflowUserId, HttpContext.RequestAborted)
                .ConfigureAwait(false);

        PageData = new WeixinClawIntegrationPageDto
        {
            Enabled = _options.Enabled,
            CurrentAdminUserId = currentAdminUserId,
            BindingProfileCount = bindingProfiles.Count(item => item.Enabled),
            RequireCommandPrefix = _options.RequireCommandPrefix,
            EnableNeuBell = _options.EnableNeuBell,
            EnableWorkflow = _options.EnableWorkflow,
            EnableHarness = _options.EnableHarness,
            CommandPrefix = string.IsNullOrWhiteSpace(_options.CommandPrefix) ? "/" : _options.CommandPrefix,
            WorkflowUserId = workflowUserId,
            Workflows = workflows
                .Select(workflow => new WeixinClawWorkflowDto
                {
                    Id = workflow.Id,
                    Name = workflow.Name,
                    Description = workflow.Description
                })
                .ToList()
        };

        return Page();
    }

    public sealed class WeixinClawIntegrationPageDto
    {
        public bool Enabled { get; set; }
        public int CurrentAdminUserId { get; set; }
        public int BindingProfileCount { get; set; }
        public bool RequireCommandPrefix { get; set; }
        public bool EnableNeuBell { get; set; }
        public bool EnableWorkflow { get; set; }
        public bool EnableHarness { get; set; }
        public string CommandPrefix { get; set; }
        public int WorkflowUserId { get; set; }
        public System.Collections.Generic.List<WeixinClawWorkflowDto> Workflows { get; set; } = [];
    }

    public sealed class WeixinClawWorkflowDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
