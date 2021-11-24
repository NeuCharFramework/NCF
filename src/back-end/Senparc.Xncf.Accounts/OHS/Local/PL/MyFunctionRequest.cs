using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.Accounts.OHS.Local.PL
{
    public class MyFunction_CaculateRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(50)]
        [Description("名称||双竖线之前为参数名称，双竖线之后为参数注释")]
        public string Name { get; set; }

        [Required]
        [Description("数字||数字1")]
        public int Number1 { get; set; }


        [Required]
        [Description("数字||数字2")]
        public int Number2 { get; set; }

        [Description("运算符||")]//下拉列表
        public SelectionList Operator { get; set; } = new SelectionList(SelectionType.DropDownList, new[] {
                 new SelectionItem("+","加法","数字1 + 数字2",false),
                 new SelectionItem("-","减法","数字1 - 数字2",true),
                 new SelectionItem("×","乘法","数字1 × 数字2",false),
                 new SelectionItem("÷","除法","数字1 ÷ 数字2",false)
            });

        [Description("计算平方||")]//多选框
        public SelectionList Power { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("2","平方","计算上述结果之后再计算平方",false),
                 new SelectionItem("3","三次方","计算上述结果之后再计算三次方",false)
            });
    }
}
