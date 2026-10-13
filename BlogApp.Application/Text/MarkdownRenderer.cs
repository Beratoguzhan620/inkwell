using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace BlogApp.Application.Text;

/// <summary>
/// Turns author Markdown into HTML that is safe to render: raw HTML is escaped and links
/// or images with scripting schemes are dropped.
/// </summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseEmphasisExtras()
        .UsePipeTables()
        .UseAutoLinks()
        .UseTaskLists()
        .DisableHtml()
        .Build();

    private const int WordsPerMinute = 200;

    public static string ToHtml(string markdown)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);

        foreach (var link in document.Descendants<LinkInline>())
        {
            if (!IsSafeUrl(link.Url))
                link.Url = "#";
        }

        foreach (var link in document.Descendants<AutolinkInline>())
        {
            if (!IsSafeUrl(link.Url))
                link.Url = "#";
        }

        using var writer = new StringWriter();
        var renderer = new Markdig.Renderers.HtmlRenderer(writer);
        Pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();
        return writer.ToString();
    }

    public static string ToPlainText(string markdown) =>
        Markdown.ToPlainText(markdown ?? string.Empty, Pipeline).Trim();

    /// <summary>First sentences of the post as plain text, cut on a word boundary.</summary>
    public static string Excerpt(string markdown, int maxLength = 220)
    {
        var text = string.Join(' ', ToPlainText(markdown).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (text.Length <= maxLength)
            return text;

        var cut = text.LastIndexOf(' ', maxLength);
        return text[..(cut > 0 ? cut : maxLength)].TrimEnd(',', '.', ';', ':') + "…";
    }

    public static int ReadingMinutes(string markdown)
    {
        var words = ToPlainText(markdown).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));
    }

    private static bool IsSafeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        var trimmed = url.Trim();
        if (trimmed.StartsWith('/') || trimmed.StartsWith('#'))
            return true;

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            ? uri.Scheme is "http" or "https" or "mailto"
            : !trimmed.Contains(':');
    }
}
