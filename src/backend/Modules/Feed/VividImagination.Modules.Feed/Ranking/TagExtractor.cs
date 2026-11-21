using System.Text.RegularExpressions;

namespace VividImagination.Modules.Feed.Ranking;

public static class TagExtractor
{
    private static readonly Regex HashtagRegex = new(@"#(\w+)", RegexOptions.Compiled);

    public static IReadOnlyList<string> ExtractTags(string? caption)
    {
        if (string.IsNullOrWhiteSpace(caption))
            return ["vivid", "community"];

        var matches = HashtagRegex.Matches(caption);
        if (matches.Count == 0)
            return ["vivid", "moments"];

        return matches.Select(m => m.Groups[1].Value.ToLowerInvariant()).Distinct().ToList();
    }
}
