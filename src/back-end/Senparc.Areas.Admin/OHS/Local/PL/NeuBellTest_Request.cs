/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellTest_Request.cs
    文件功能描述：纽铃可见提醒示例 Function 请求参数

    创建标识：Senparc - 20260805

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Areas.Admin.OHS.PL;

public sealed class NeuBellTest_Request : FunctionAppRequestBase
{
    public const string SendAction = "send";
    public const string ConsumeAction = "consume";

    [Required]
    [Description("操作")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(ActionOptions))]
    public string Action { get; set; } = SendAction;

    [JsonIgnore]
    public SelectionList ActionOptions { get; set; } = new(
        SelectionType.DropDownList,
        [
            new SelectionItem(SendAction, "发送提醒", "新增一条可在 Footer 弹窗和徽标中看到的测试提醒。", true),
            new SelectionItem(ConsumeAction, "消费提醒", "清除所有由此 Function 发送的测试提醒。")
        ]);
}
