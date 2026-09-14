using LearnHub.Services;
using LearnHub.Services.Content;

namespace LearnHub.Tests.Unit;

public sealed class VideoEmbedParserTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=GhQdlIFylQ8", "GhQdlIFylQ8")]
    [InlineData("https://youtube.com/watch?v=GhQdlIFylQ8&t=120s", "GhQdlIFylQ8")]
    [InlineData("https://m.youtube.com/watch?v=GhQdlIFylQ8", "GhQdlIFylQ8")]
    [InlineData("https://youtu.be/GhQdlIFylQ8", "GhQdlIFylQ8")]
    [InlineData("https://www.youtube.com/embed/GhQdlIFylQ8", "GhQdlIFylQ8")]
    [InlineData("https://www.youtube.com/shorts/GhQdlIFylQ8", "GhQdlIFylQ8")]
    public void Accepts_youtube_links_and_builds_a_privacy_enhanced_embed(string url, string expectedId)
    {
        Assert.True(VideoEmbedParser.TryParse(url, out var embed));
        Assert.Equal("YouTube", embed.Provider);
        Assert.Equal(expectedId, embed.VideoId);
        Assert.Equal($"https://www.youtube-nocookie.com/embed/{expectedId}?rel=0", embed.EmbedUrl);
    }

    [Theory]
    [InlineData("https://vimeo.com/76979871")]
    [InlineData("https://player.vimeo.com/video/76979871")]
    public void Accepts_vimeo_links(string url)
    {
        Assert.True(VideoEmbedParser.TryParse(url, out var embed));
        Assert.Equal("https://player.vimeo.com/video/76979871?dnt=1", embed.EmbedUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://www.youtube.com/watch?v=GhQdlIFylQ8")]
    [InlineData("https://www.youtube.com.evil.example/watch?v=GhQdlIFylQ8")]
    [InlineData("https://evil.example/youtube.com/watch?v=GhQdlIFylQ8")]
    [InlineData("https://www.youtube.com/watch?v=short")]
    [InlineData("https://www.youtube.com/watch?v=GhQdlIFylQ8\"><script>")]
    [InlineData("https://vimeo.com/not-a-number")]
    [InlineData("www.youtube.com/watch?v=GhQdlIFylQ8")]
    public void Rejects_anything_that_is_not_a_well_formed_https_youtube_or_vimeo_link(string? url)
    {
        Assert.False(VideoEmbedParser.TryParse(url, out _));
    }
}

public sealed class LessonContentRendererTests
{
    [Fact]
    public void Html_in_lesson_text_is_encoded_and_never_executed()
    {
        var html = Render("<script>alert('x')</script>\n\n<img src=x onerror=alert(1)>");

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Formatting_syntax_produces_only_allow_listed_elements()
    {
        var html = Render("## Heading\n\nA **bold** word and `code`.\n\n- one\n- two\n\n1. first\n2. second\n\n> A note");

        Assert.Contains("<h2>Heading</h2>", html);
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<code>code</code>", html);
        Assert.Contains("<ul><li>one</li><li>two</li></ul>", html);
        Assert.Contains("<ol><li>first</li><li>second</li></ol>", html);
        Assert.Contains("<blockquote class=\"lesson-note\"><p>A note</p></blockquote>", html);
    }

    [Fact]
    public void Code_blocks_keep_their_content_encoded_and_unformatted()
    {
        var html = Render("```\nif (a < b && **c**) { }\n```");

        Assert.Contains("<pre class=\"lesson-code\" tabindex=\"0\"><code>", html);
        Assert.Contains("if (a &lt; b &amp;&amp; **c**) { }", html);
        Assert.DoesNotContain("<strong>", html);
    }

    [Fact]
    public void Attack_attempts_through_formatting_markers_stay_as_text()
    {
        var html = Render("**<a href=\"javascript:alert(1)\">x</a>**");

        Assert.DoesNotContain("<a ", html);
        Assert.Contains("&lt;a href=&quot;javascript:alert(1)&quot;&gt;", html);
    }

    [Fact]
    public void Empty_content_renders_nothing()
    {
        Assert.Equal(string.Empty, Render("   "));
    }

    private static string Render(string text)
    {
        using var writer = new StringWriter();
        LessonContentRenderer.Render(text).WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        return writer.ToString();
    }
}

public sealed class SearchPatternTests
{
    [Theory]
    [InlineData("sql", "%sql%")]
    [InlineData("  sql  ", "%sql%")]
    [InlineData("100%", "%100\\%%")]
    [InlineData("first_name", "%first\\_name%")]
    [InlineData("[abc]", "%\\[abc]%")]
    public void Wildcards_typed_by_users_are_escaped(string input, string expected)
    {
        Assert.Equal(expected, SearchPattern.Contains(input));
    }

    [Fact]
    public void Very_long_search_terms_are_truncated()
    {
        var pattern = SearchPattern.Contains(new string('a', 500));
        Assert.Equal(SearchPattern.MaxTermLength + 2, pattern.Length);
    }
}
