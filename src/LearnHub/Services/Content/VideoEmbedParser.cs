using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using LearnHub.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace LearnHub.Services.Content;

public sealed record VideoEmbed(string Provider, string VideoId, string EmbedUrl, string WatchUrl);

/// <summary>
/// Turns a YouTube or Vimeo page link into a safe embed URL. Only an allow-listed host and a strictly
/// formatted video id are accepted, and the embed URL is rebuilt from the id, so an administrator can
/// never inject an arbitrary iframe source.
/// </summary>
public static partial class VideoEmbedParser
{
    private static readonly HashSet<string> YouTubeHosts =
        ["youtube.com", "www.youtube.com", "m.youtube.com", "youtube-nocookie.com", "www.youtube-nocookie.com"];

    private static readonly HashSet<string> VimeoHosts = ["vimeo.com", "www.vimeo.com", "player.vimeo.com"];

    public static bool TryParse(string? url, [NotNullWhen(true)] out VideoEmbed? embed)
    {
        embed = null;
        if (string.IsNullOrWhiteSpace(url) || url.Length > FieldLengths.Url)
        {
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = uri.IdnHost.ToLowerInvariant();
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (host == "youtu.be")
        {
            return TryCreateYouTube(segments.FirstOrDefault(), out embed);
        }

        if (YouTubeHosts.Contains(host))
        {
            if (segments is ["watch"])
            {
                var query = QueryHelpers.ParseQuery(uri.Query);
                return TryCreateYouTube(query.TryGetValue("v", out var id) ? id.ToString() : null, out embed);
            }

            if (segments is ["embed" or "shorts" or "live", var pathId, ..])
            {
                return TryCreateYouTube(pathId, out embed);
            }

            return false;
        }

        if (VimeoHosts.Contains(host))
        {
            var candidate = host == "player.vimeo.com"
                ? segments is ["video", var playerId, ..] ? playerId : null
                : segments.FirstOrDefault();
            return TryCreateVimeo(candidate, out embed);
        }

        return false;
    }

    private static bool TryCreateYouTube(string? id, [NotNullWhen(true)] out VideoEmbed? embed)
    {
        embed = id is not null && YouTubeId().IsMatch(id)
            ? new VideoEmbed(
                "YouTube",
                id,
                $"https://www.youtube-nocookie.com/embed/{id}?rel=0",
                $"https://www.youtube.com/watch?v={id}")
            : null;
        return embed is not null;
    }

    private static bool TryCreateVimeo(string? id, [NotNullWhen(true)] out VideoEmbed? embed)
    {
        embed = id is not null && VimeoId().IsMatch(id)
            ? new VideoEmbed(
                "Vimeo",
                id,
                $"https://player.vimeo.com/video/{id}?dnt=1",
                $"https://vimeo.com/{id}")
            : null;
        return embed is not null;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex YouTubeId();

    [GeneratedRegex("^[0-9]{6,12}$")]
    private static partial Regex VimeoId();
}
