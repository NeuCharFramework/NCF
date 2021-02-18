using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.Student.Functions
{
       public class MyFunction : FunctionBase
    {
        public MyFunction(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public class Parameters : IFunctionParameter
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


        public override string Name => "我的函数";

        public override string Description => "我的函数的注释";

        public override Type FunctionParameterType => typeof(Parameters);

        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<Parameters>(param, (typeParam, sb, result) =>
            {
                /* 页面上点击“执行”后，将调用这里的方法
                 *
                 * 参数说明：
                 * param：IFunctionParameter 类型对象
                 * typeParam：Senparc.Xncf.Student.MyFunction.Parameters 类型对象
                 * sb：日志
                 * result：返回结果
                 */

                double calcResult = typeParam.Number1;
                var theOperator = typeParam.Operator.SelectedValues.FirstOrDefault();
                switch (theOperator)
                {
                    case "+":
                        calcResult = calcResult + typeParam.Number2;
                        break;
                    case "-":
                        calcResult = calcResult - typeParam.Number2;
                        break;
                    case "×":
                        calcResult = calcResult * typeParam.Number2;
                        break;
                    case "÷":
                        if (typeParam.Number2 == 0)
                        {
                            result.Success = false;
                            result.Message = "被除数不能为0！";
                            return;
                        }
                        calcResult = calcResult / typeParam.Number2;
                        break;
                    default:
                        result.Success = false;
                        result.Message = $"未知的运算符：{theOperator}";
                        return;
                }

                sb.AppendLine($"进行运算：{typeParam.Number1} {theOperator} {typeParam.Number2} = {calcResult}");

                Action<int> raisePower = power =>{
                    if (typeParam.Power.SelectedValues.Contains(power.ToString()))
                    {
                        var oldValue = calcResult;
                        calcResult =  Math.Pow(calcResult, power);
                        sb.AppendLine($"进行{power}次方运算：{oldValue}{(power == 2 ? "²" : "³")} = {calcResult}");
                    }
                };

                raisePower(2);
                raisePower(3);

                result.Message = $"计算结果：{calcResult}。计算过程请看日志";
            });
        }
    }
}