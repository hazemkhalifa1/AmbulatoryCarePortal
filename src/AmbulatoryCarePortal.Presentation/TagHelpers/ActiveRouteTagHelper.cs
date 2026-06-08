using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace AmbulatoryCarePortal.Presentation.TagHelpers;

[HtmlTargetElement("a", Attributes = ActiveRouteAttribute)]
public class ActiveRouteTagHelper : TagHelper
{
    private const string ActiveRouteAttribute = "is-active-route";

    [HtmlAttributeName("asp-area")]
    public string? Area { get; set; }

    [HtmlAttributeName("asp-controller")]
    public string? Controller { get; set; }

    [HtmlAttributeName("asp-action")]
    public string? Action { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var routeData = ViewContext.RouteData.Values;
        var currentArea = routeData["area"]?.ToString();
        var currentController = routeData["controller"]?.ToString();
        var currentAction = routeData["action"]?.ToString();

        var isActive = string.Equals(Area, currentArea, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(Controller, currentController, StringComparison.OrdinalIgnoreCase) &&
                       (string.IsNullOrEmpty(Action) || string.Equals(Action, currentAction, StringComparison.OrdinalIgnoreCase));

        if (isActive)
        {
            var existingClass = output.Attributes.FirstOrDefault(a => a.Name == "class")?.Value.ToString() ?? "";
            output.Attributes.SetAttribute("class", $"{existingClass} active".Trim());
        }
    }
}
