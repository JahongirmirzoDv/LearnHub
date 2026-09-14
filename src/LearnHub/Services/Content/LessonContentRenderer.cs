using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace LearnHub.Services.Content;

/// <summary>
/// Renders article lessons written with a tiny, safe formatting syntax:
/// <list type="bullet">
/// <item><c>## Heading</c> / <c>### Sub-heading</c></item>
/// <item><c>- item</c> bullet lists and <c>1. item</c> numbered lists</item>
/// <item><c>&gt; note</c> call-outs, <c>**bold**</c> and <c>`inline code`</c></item>
/// <item>fenced code blocks between lines containing <c>```</c></item>
/// </list>
/// Security: every piece of text is HTML-encoded <em>before</em> a fixed set of tags without attributes is
/// added, so author-supplied text can never create scripts, links or event handlers.
/// </summary>
public static partial class LessonContentRenderer
{
    private enum Block
    {
        None,
        Paragraph,
        BulletList,
        NumberedList,
        Quote
    }

    public static IHtmlContent Render(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return HtmlString.Empty;
        }

        var html = new StringBuilder();
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var block = Block.None;
        var inCode = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                if (inCode)
                {
                    html.Append("</code></pre>");
                    inCode = false;
                }
                else
                {
                    CloseBlock(html, ref block);
                    // Focusable, so keyboard users can scroll code lines that are wider than the screen.
                    html.Append("<pre class=\"lesson-code\" tabindex=\"0\"><code>");
                    inCode = true;
                }

                continue;
            }

            if (inCode)
            {
                html.Append(WebUtility.HtmlEncode(rawLine)).Append('\n');
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                CloseBlock(html, ref block);
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                CloseBlock(html, ref block);
                html.Append("<h3>").Append(Inline(line[4..])).Append("</h3>");
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal) || line.StartsWith("# ", StringComparison.Ordinal))
            {
                CloseBlock(html, ref block);
                html.Append("<h2>").Append(Inline(line[(line.IndexOf(' ') + 1)..])).Append("</h2>");
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                OpenBlock(html, ref block, Block.BulletList);
                html.Append("<li>").Append(Inline(line[2..])).Append("</li>");
            }
            else if (NumberedItem().Match(line) is { Success: true } numbered)
            {
                OpenBlock(html, ref block, Block.NumberedList);
                html.Append("<li>").Append(Inline(numbered.Groups["text"].Value)).Append("</li>");
            }
            else if (line.StartsWith('>'))
            {
                OpenBlock(html, ref block, Block.Quote);
                html.Append("<p>").Append(Inline(line[1..].TrimStart())).Append("</p>");
            }
            else
            {
                if (block == Block.Paragraph)
                {
                    html.Append("<br>");
                }
                else
                {
                    OpenBlock(html, ref block, Block.Paragraph);
                }

                html.Append(Inline(line.Trim()));
            }
        }

        if (inCode)
        {
            html.Append("</code></pre>");
        }

        CloseBlock(html, ref block);
        return new HtmlString(html.ToString());
    }

    /// <summary>Encodes text, then applies inline code and bold. Code spans are never formatted further.</summary>
    private static string Inline(string text)
    {
        var result = new StringBuilder();
        var parts = text.Split('`');
        for (var i = 0; i < parts.Length; i++)
        {
            var encoded = WebUtility.HtmlEncode(parts[i]);
            var isCode = i % 2 == 1 && i < parts.Length - 1;
            if (isCode)
            {
                result.Append("<code>").Append(encoded).Append("</code>");
            }
            else
            {
                if (i % 2 == 1)
                {
                    result.Append('`'); // unmatched trailing backtick
                }

                result.Append(Bold().Replace(encoded, "<strong>$1</strong>"));
            }
        }

        return result.ToString();
    }

    private static void OpenBlock(StringBuilder html, ref Block current, Block next)
    {
        if (current == next)
        {
            return;
        }

        CloseBlock(html, ref current);
        html.Append(next switch
        {
            Block.Paragraph => "<p>",
            Block.BulletList => "<ul>",
            Block.NumberedList => "<ol>",
            Block.Quote => "<blockquote class=\"lesson-note\">",
            _ => string.Empty
        });
        current = next;
    }

    private static void CloseBlock(StringBuilder html, ref Block current)
    {
        html.Append(current switch
        {
            Block.Paragraph => "</p>",
            Block.BulletList => "</ul>",
            Block.NumberedList => "</ol>",
            Block.Quote => "</blockquote>",
            _ => string.Empty
        });
        current = Block.None;
    }

    [GeneratedRegex(@"^\d{1,3}\.\s+(?<text>.+)$")]
    private static partial Regex NumberedItem();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex Bold();
}
