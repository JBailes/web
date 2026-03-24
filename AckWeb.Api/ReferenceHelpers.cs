namespace AckWeb.Api;

internal static class ReferenceHelpers
{
    /// <summary>
    /// Resolves a topic name to a file path under baseDir, or returns null if the
    /// topic is empty, the file does not exist, or the resolved path escapes baseDir.
    /// </summary>
    public static string? SafeTopicPath(string baseDir, string topic)
    {
        var cleaned = topic.Trim().Trim('/');
        if (string.IsNullOrEmpty(cleaned)) return null;

        var resolvedBase = Path.GetFullPath(baseDir);
        var candidate = Path.GetFullPath(Path.Combine(resolvedBase, cleaned));

        if (!File.Exists(candidate)) return null;
        if (!candidate.StartsWith(resolvedBase + Path.DirectorySeparatorChar)) return null;

        return candidate;
    }

    /// <summary>
    /// Extracts only the first (unflagged) entry from a lore file.
    /// Lore files are structured as:
    ///   keywords ...
    ///   ---
    ///   [first entry]
    ///   flags ...
    ///   ---
    ///   [subsequent entries]
    /// Returns the full content trimmed if no keywords header is found.
    /// </summary>
    public static string ExtractFirstLoreEntry(string content)
    {
        var blocks = content.Split("\n---\n");
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i].TrimStart().StartsWith("keywords ") && i + 1 < blocks.Length)
                return blocks[i + 1].Trim();
        }
        return content.Trim();
    }

    public static string ResolveRefDir(string type, string helpDir, string shelpDir, string loreDir) =>
        type switch { "shelp" => shelpDir, "lore" => loreDir, _ => helpDir };
}
