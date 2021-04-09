using System;
using System.Collections.Generic;
using Senparc.Core.Utility;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;

namespace Senparc.Mvc
{
    public class EntitiesArrayBinder : IModelBinder
    {
        public string Profix { get; set; }
        public EntitiesArrayBinder()
        {
        }

        //public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            return Task.Factory.StartNew(() => { 
            var bingdingType = bindingContext.ModelType;
            if (!bingdingType.IsGenericType)
            {
                throw new Exception(bingdingType.FullName + "必须为泛型变量！如List<T>");

            }

            var modelType = bingdingType.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(modelType);
            Profix = modelType.Name + ".";//类名作为前缀

            //var obj = bingdingType.Assembly.CreateInstance(bingdingType.FullName);
            var list = Activator.CreateInstance(listType);//获取List<T>实例
            var addMethod = listType.GetMethod("Add");//List<T>.Add方法

            var formCollection = bindingContext.HttpContext.Request.Form;

            var valueCollection = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            var properties = modelType.GetProperties();
            var recordCount = 0;//总共记录条数
            //收集所有可用属性
            foreach (var prop in properties)
            {
                var formKey = Profix + prop.Name;
                var formValues = formCollection[formKey];
                if (formValues.Count > 0)
                {
                    //存在
                    if (recordCount == 0)
                    {
                        //计算记录数量
                        recordCount = formValues.Count;
                        //Senparc.Log.LogUtility.ReportLogger.DebugFormat("recordCount数量：{0},字段：{1}", recordCount, formKey);
                    }
                    if (formValues.Count != recordCount)
                    {
                        throw new Exception(formKey + "数量与其他属性不一致。第一个属性数量为：" + recordCount + "，当前数量为：" + formValues.Count);
                    }
                    valueCollection.Add(prop.Name, formValues);
                }
            }

            for (int i = 0; i < recordCount; i++)
            {
                //这里默认为所有属性条数一致，如果不一致或出错，自然抛出错误
                var entity = Activator.CreateInstance(modelType);
                foreach (var item in valueCollection)
                {
                    ReflectorUtility.SetPropertyValue(entity, modelType, item.Key, item.Value[i]);//设置字段
                }
                addMethod.Invoke((object)list, new object[] { entity });//加入列表
            }

            /*return Task*/;
            });
        }
    }
}
