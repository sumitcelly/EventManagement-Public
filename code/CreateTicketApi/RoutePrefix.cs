

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

public class RoutePrefixConvention : IControllerModelConvention
{
    private readonly string _prefix;
    public RoutePrefixConvention(string prefix) => _prefix = prefix;

    public void Apply(ControllerModel controller)
    {
        foreach (var selector in controller.Selectors)
        {
            if (selector.AttributeRouteModel != null)
            {
                selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(
                    new AttributeRouteModel(new RouteAttribute(_prefix)),
                    selector.AttributeRouteModel);
            }
        }
    }
}