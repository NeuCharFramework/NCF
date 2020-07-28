using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Core.Conventions
{
    public class AutoValidateAntiForgeryTokenModelConvention : IPageConvention// IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            if (IsConventionApplicable(action))
            {
                action.Filters.Add(new ValidateAntiForgeryTokenAttribute());
            }
        }

        public bool IsConventionApplicable(ActionModel action)
        {
            if (action.Attributes.Any(f => f.GetType() == typeof(HttpPostAttribute)) &&
                !action.Attributes.Any(f => f.GetType() == typeof(ValidateAntiForgeryTokenAttribute)))
            {
                return true;
            }
            return false;
        }
    }
}
