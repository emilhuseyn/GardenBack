using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace App.Business.Helpers
{
    public class PluralizedRouteConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            var pluralizedName = PluralizeControllerName(controller.ControllerName);

            foreach (var selector in controller.Selectors)
            {
                if (selector.AttributeRouteModel != null)
                {
                    selector.AttributeRouteModel.Template = $"api/{pluralizedName}";
                }
            }
        }

        public static string PluralizeControllerName(string controllerName)
        {
            if (string.IsNullOrWhiteSpace(controllerName))
                throw new ArgumentException("Controller name cannot be null or empty.", nameof(controllerName));

            var changedControllerName = string.Empty;

            if (controllerName.EndsWith("y"))
            {
                changedControllerName = Regex.Replace(controllerName, "y$", "ies");
            }
            else if (controllerName.EndsWith("s"))
            {
                changedControllerName = controllerName + "es";
            }
            else
            {
                changedControllerName = controllerName + "s";
            }

            return changedControllerName.ToLower();
        }
    }
}
