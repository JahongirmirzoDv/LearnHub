using System.Globalization;
using LearnHub.ViewModels.Shared;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LearnHub.Infrastructure;

/// <summary>
/// The home page "interchange" map (SVG viewBox 0 0 600 440): up to eight category lines drawn with 45°
/// transit-map bends, all meeting at the LearnHub station in the centre. Each line links to its category in
/// the catalogue. Built with <see cref="TagBuilder"/> so every value is HTML-encoded.
/// </summary>
public static class InterchangeMap
{
    public sealed record Slot(
        string PathData,
        int TerminusX,
        int TerminusY,
        int StopX,
        int StopY,
        int LabelX,
        int LabelY,
        string LabelAnchor);

    public static readonly IReadOnlyList<Slot> Slots =
    [
        new("M 36 96 H 170 L 300 220", 36, 96, 170, 96, 36, 76, "start"),
        new("M 564 96 H 430 L 300 220", 564, 96, 430, 96, 564, 76, "end"),
        new("M 36 220 H 300", 36, 220, 160, 220, 36, 200, "start"),
        new("M 564 220 H 300", 564, 220, 440, 220, 564, 200, "end"),
        new("M 36 344 H 170 L 300 220", 36, 344, 170, 344, 36, 324, "start"),
        new("M 564 344 H 430 L 300 220", 564, 344, 430, 344, 564, 324, "end"),
        new("M 300 30 V 220", 300, 30, 300, 112, 318, 36, "start"),
        new("M 300 410 V 220", 300, 410, 300, 330, 318, 424, "start")
    ];

    /// <param name="categories">Categories to draw; the busiest lines get the most prominent slots.</param>
    /// <param name="urlForCategory">Builds the catalogue URL for a category id.</param>
    public static IHtmlContent Render(IReadOnlyList<CategoryOption> categories, Func<int, string?> urlForCategory)
    {
        var svg = new TagBuilder("svg");
        svg.AddCssClass("interchange-map");
        svg.Attributes["viewBox"] = "0 0 600 440";
        svg.Attributes["role"] = "group";
        svg.Attributes["aria-label"] = "Course categories";

        var ordered = categories
            .OrderByDescending(category => category.CourseCount)
            .ThenBy(category => category.Name)
            .Take(Slots.Count)
            .ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            svg.InnerHtml.AppendHtml(RenderLine(ordered[index], Slots[index], index + 1, urlForCategory(ordered[index].Id)));
        }

        var hub = new TagBuilder("g");
        hub.Attributes["aria-hidden"] = "true";
        hub.InnerHtml.AppendHtml(Element("rect", ("class", "map-hub-ring"), ("x", "232"), ("y", "192"), ("width", "136"), ("height", "56"), ("rx", "28")));
        var label = Element("text", ("class", "map-hub-label"), ("x", "300"), ("y", "226"), ("text-anchor", "middle"));
        label.InnerHtml.Append("LearnHub");
        hub.InnerHtml.AppendHtml(label);
        svg.InnerHtml.AppendHtml(hub);

        return svg;
    }

    private static TagBuilder RenderLine(CategoryOption category, Slot slot, int slotNumber, string? url)
    {
        var link = new TagBuilder("a");
        link.AddCssClass("map-line");
        link.AddCssClass($"map-slot-{slotNumber}");
        link.AddCssClass(DisplayFormat.LineClass(category.Id));
        link.Attributes["href"] = url ?? "#";
        link.Attributes["aria-label"] = $"{category.Name}: {DisplayFormat.Plural(category.CourseCount, "course")}";

        link.InnerHtml.AppendHtml(Element("path", ("d", slot.PathData), ("pathLength", "1")));
        link.InnerHtml.AppendHtml(Element("circle", ("class", "map-stop"), ("cx", Number(slot.StopX)), ("cy", Number(slot.StopY)), ("r", "9")));
        link.InnerHtml.AppendHtml(Element("circle", ("class", "map-terminus"), ("cx", Number(slot.TerminusX)), ("cy", Number(slot.TerminusY)), ("r", "13")));

        var text = Element("text", ("x", Number(slot.LabelX)), ("y", Number(slot.LabelY)), ("text-anchor", slot.LabelAnchor), ("aria-hidden", "true"));
        text.InnerHtml.Append(category.Name + " ");
        var count = Element("tspan", ("class", "map-count"));
        count.InnerHtml.Append(DisplayFormat.Plural(category.CourseCount, "course"));
        text.InnerHtml.AppendHtml(count);
        link.InnerHtml.AppendHtml(text);

        return link;
    }

    private static TagBuilder Element(string name, params (string Name, string Value)[] attributes)
    {
        var element = new TagBuilder(name);
        foreach (var (attributeName, value) in attributes)
        {
            element.Attributes[attributeName] = value;
        }

        return element;
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
