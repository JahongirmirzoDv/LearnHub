using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace LearnHub.Infrastructure;

/// <summary>
/// Marks a navigation link as the current page: <c>&lt;a lh-nav="Courses" …&gt;</c>. Entries are comma
/// separated controller names, optionally prefixed with an area (<c>Admin/Courses</c>).
/// </summary>
[HtmlTargetElement("a", Attributes = "lh-nav")]
public sealed class NavLinkTagHelper : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    [HtmlAttributeName("lh-nav")]
    public string Controllers { get; set; } = string.Empty;

    /// <summary>Optional action name, or comma-separated names, one of which must also match (e.g. "Dashboard,MyCourses").</summary>
    [HtmlAttributeName("lh-nav-action")]
    public string? Action { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var values = ViewContext.RouteData.Values;
        var currentArea = values["area"] as string ?? string.Empty;
        var currentController = values["controller"] as string;
        var currentAction = values["action"] as string;

        var controllerMatches = Controllers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(entry =>
            {
                var parts = entry.Split('/');
                var area = parts.Length == 2 ? parts[0] : string.Empty;
                var controller = parts[^1];
                return string.Equals(area, currentArea, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(controller, currentController, StringComparison.OrdinalIgnoreCase);
            });

        var actionMatches = Action is null || Action
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(action => string.Equals(action, currentAction, StringComparison.OrdinalIgnoreCase));
        if (controllerMatches && actionMatches)
        {
            output.AddClass("active", HtmlEncoder.Default);
            output.Attributes.SetAttribute("aria-current", "page");
        }
    }
}

/// <summary>
/// Renders a course cover image, or a category-coloured icon panel when the course has no image:
/// <c>&lt;course-cover path="@course.ThumbnailPath" icon="@course.CategoryIcon" /&gt;</c>.
/// The image is decorative (empty alt) because the course title is always shown next to it.
/// </summary>
[HtmlTargetElement("course-cover", TagStructure = TagStructure.WithoutEndTag)]
public sealed class CourseCoverTagHelper : TagHelper
{
    public string? Path { get; set; }

    public string Icon { get; set; } = Models.CategoryIcons.Default;

    /// <summary>Load immediately (above-the-fold covers) instead of lazily.</summary>
    public bool Eager { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("course-cover", HtmlEncoder.Default);

        if (!string.IsNullOrWhiteSpace(Path))
        {
            var image = new TagBuilder("img");
            image.TagRenderMode = TagRenderMode.SelfClosing;
            image.Attributes["src"] = Path;
            image.Attributes["alt"] = string.Empty;
            image.Attributes["width"] = "640";
            image.Attributes["height"] = "360";
            image.Attributes["loading"] = Eager ? "eager" : "lazy";
            image.Attributes["decoding"] = "async";
            output.Content.SetHtmlContent(image);
            return;
        }

        var iconName = Models.CategoryIcons.IsAllowed(Icon) ? Icon : Models.CategoryIcons.Default;
        var fallback = new TagBuilder("div");
        fallback.AddCssClass("course-cover-fallback");
        fallback.Attributes["aria-hidden"] = "true";
        fallback.InnerHtml.AppendHtml($"<i class=\"bi bi-{iconName}\"></i>");
        output.Content.SetHtmlContent(fallback);
    }
}
