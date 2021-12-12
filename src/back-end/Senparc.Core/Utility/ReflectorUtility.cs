using System;

namespace Senparc.Core.Utility
{
    public static class ReflectorUtility
    {
        public static void SetPropertyValue(object obj, Type objType, string propertyName, string value)
        {
            var prop = objType.GetProperty(propertyName);
            switch (prop.PropertyType.Name)
            {
                case "DateTime":
                    prop.SetValue(obj, DateTime.Parse(value), null);
                    break;
                case "DateTimeOffset":
                    prop.SetValue(obj, DateTimeOffset.Parse(value), null);
                    break;
                case "Int32":
                    prop.SetValue(obj, int.Parse(value), null);
                    break;
                case "Int64":
                    prop.SetValue(obj, long.Parse(value), null);
                    break;
                case "Single":
                case "float":
                    prop.SetValue(obj, float.Parse(value), null);
                    break;
                case "Double":
                    prop.SetValue(obj, double.Parse(value), null);
                    break;
                case "Boolean":
                    prop.SetValue(obj, bool.Parse(value), null);
                    break;
                default:
                    prop.SetValue(obj, value, null);
                    break;
            }
        }
    }
}