using AckWeb.Api;

namespace AckWeb.Tests.Api;

public class ReferenceHelpersTests : IDisposable
{
    private readonly string _tempDir;

    public ReferenceHelpersTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // ── SafeTopicPath ─────────────────────────────────────────────────────

    [Fact]
    public void SafeTopicPath_ReturnsNull_WhenTopicIsEmpty()
    {
        Assert.Null(ReferenceHelpers.SafeTopicPath(_tempDir, ""));
    }

    [Fact]
    public void SafeTopicPath_ReturnsNull_WhenTopicIsWhitespace()
    {
        Assert.Null(ReferenceHelpers.SafeTopicPath(_tempDir, "   "));
    }

    [Fact]
    public void SafeTopicPath_ReturnsNull_WhenFileDoesNotExist()
    {
        Assert.Null(ReferenceHelpers.SafeTopicPath(_tempDir, "nonexistent"));
    }

    [Fact]
    public void SafeTopicPath_ReturnsNull_WhenPathTraversesOutside()
    {
        Assert.Null(ReferenceHelpers.SafeTopicPath(_tempDir, "../../etc/passwd"));
    }

    [Fact]
    public void SafeTopicPath_ReturnsNull_WhenPathTraversesOutside_WithLeadingSlash()
    {
        Assert.Null(ReferenceHelpers.SafeTopicPath(_tempDir, "/etc/passwd"));
    }

    [Fact]
    public void SafeTopicPath_ReturnsResolvedPath_WhenValid()
    {
        var file = Path.Combine(_tempDir, "fire");
        File.WriteAllText(file, "content");

        var result = ReferenceHelpers.SafeTopicPath(_tempDir, "fire");

        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(file), result);
    }

    [Fact]
    public void SafeTopicPath_StripsLeadingAndTrailingSlashes()
    {
        var file = Path.Combine(_tempDir, "fire");
        File.WriteAllText(file, "content");

        var result = ReferenceHelpers.SafeTopicPath(_tempDir, "/fire/");

        Assert.NotNull(result);
    }

    // ── ExtractFirstLoreEntry ─────────────────────────────────────────────

    [Fact]
    public void ExtractFirstLoreEntry_ReturnsFullContent_WhenNoKeywordsHeader()
    {
        var content = "Just some lore text\nwithout a header.";
        Assert.Equal(content.Trim(), ReferenceHelpers.ExtractFirstLoreEntry(content));
    }

    [Fact]
    public void ExtractFirstLoreEntry_ReturnsFirstEntry_WhenKeywordsHeaderPresent()
    {
        var content = "keywords fire\n---\nThis is the first entry.\n---\nflags city\n---\nCity-specific entry.";
        var result = ReferenceHelpers.ExtractFirstLoreEntry(content);
        Assert.Equal("This is the first entry.", result);
    }

    [Fact]
    public void ExtractFirstLoreEntry_ReturnsEmpty_WhenKeywordsHeaderIsLast()
    {
        var content = "keywords only\n---\n";
        // The block after the separator is empty
        var result = ReferenceHelpers.ExtractFirstLoreEntry(content);
        Assert.Equal("", result);
    }

    [Fact]
    public void ExtractFirstLoreEntry_TrimsWhitespace()
    {
        var content = "keywords test\n---\n\n  Trimmed entry.  \n\n---\nflags x";
        var result = ReferenceHelpers.ExtractFirstLoreEntry(content);
        Assert.Equal("Trimmed entry.", result);
    }

    // ── ResolveRefDir ─────────────────────────────────────────────────────

    [Fact]
    public void ResolveRefDir_ReturnsHelpDir_ForUnknownType()
    {
        Assert.Equal("H", ReferenceHelpers.ResolveRefDir("unknown", "H", "S", "L"));
        Assert.Equal("H", ReferenceHelpers.ResolveRefDir("help", "H", "S", "L"));
    }

    [Fact]
    public void ResolveRefDir_ReturnsShelpDir_ForShelp()
    {
        Assert.Equal("S", ReferenceHelpers.ResolveRefDir("shelp", "H", "S", "L"));
    }

    [Fact]
    public void ResolveRefDir_ReturnsLoreDir_ForLore()
    {
        Assert.Equal("L", ReferenceHelpers.ResolveRefDir("lore", "H", "S", "L"));
    }
}
