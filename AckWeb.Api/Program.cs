var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("game", c =>
{
    var gameUrl = builder.Configuration["ACKTNG_GAME_URL"] ?? "http://localhost:8080";
    c.BaseAddress = new Uri(gameUrl);
    c.Timeout = TimeSpan.FromSeconds(3);
});

// Allow WASM clients to call the API from different origins in dev
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("https://aha.ackmud.com", "https://ackmud.com")
     .AllowAnyMethod()
     .AllowAnyHeader()));

var app = builder.Build();

app.UseCors();

// ── Directory layout ──────────────────────────────────────────────────────
var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var acktngDir = Path.Combine(homeDir, "acktng");
var helpDir  = Path.Combine(acktngDir, "help");
var shelpDir = Path.Combine(acktngDir, "shelp");
var loreDir  = Path.Combine(acktngDir, "lore");

static string? SafeTopicPath(string baseDir, string topic)
{
    var cleaned = topic.Trim().Trim('/');
    if (string.IsNullOrEmpty(cleaned)) return null;
    var candidate = Path.GetFullPath(Path.Combine(baseDir, cleaned));
    if (!File.Exists(candidate)) return null;
    if (!candidate.StartsWith(Path.GetFullPath(baseDir) + Path.DirectorySeparatorChar)) return null;
    return candidate;
}

static string ExtractFirstLoreEntry(string content)
{
    var blocks = content.Split("\n---\n");
    for (int i = 0; i < blocks.Length; i++)
    {
        if (blocks[i].TrimStart().StartsWith("keywords ") && i + 1 < blocks.Length)
            return blocks[i + 1].Trim();
    }
    return content.Trim();
}

static string ResolveRefDir(string type, string helpDir, string shelpDir, string loreDir) =>
    type switch { "shelp" => shelpDir, "lore" => loreDir, _ => helpDir };

// ── GET /api/who ──────────────────────────────────────────────────────────
app.MapGet("/api/who", async (IHttpClientFactory factory) =>
{
    try
    {
        var client = factory.CreateClient("game");
        var html = await client.GetStringAsync("/who");
        return Results.Content(html, "text/html; charset=utf-8");
    }
    catch
    {
        return Results.Content("<h2>Players Online</h2>\n<ul></ul>", "text/html; charset=utf-8");
    }
});

// ── GET /api/gsgp ─────────────────────────────────────────────────────────
app.MapGet("/api/gsgp", async (IHttpClientFactory factory) =>
{
    try
    {
        var client = factory.CreateClient("game");
        var json = await client.GetStringAsync("/gsgp");
        return Results.Content(json, "application/json");
    }
    catch
    {
        return Results.Content(
            "{\"name\":\"ACK!MUD TNG\",\"active_players\":0,\"leaderboards\":[]}",
            "application/json");
    }
});

// ── GET /api/reference/{type}?q= ─────────────────────────────────────────
app.MapGet("/api/reference/{type}", (string type, string? q) =>
{
    var dir = ResolveRefDir(type, helpDir, shelpDir, loreDir);
    if (!Directory.Exists(dir))
        return Results.Json(Array.Empty<string>());

    var query = (q ?? "").Trim().ToLowerInvariant();
    var names = Directory.EnumerateFiles(dir)
        .Select(Path.GetFileName)
        .Where(n => n != null && (query.Length == 0 || n!.ToLowerInvariant().Contains(query)))
        .OrderBy(n => n)
        .ToList();

    return Results.Json(names);
});

// ── GET /api/reference/{type}/{topic} ─────────────────────────────────────
app.MapGet("/api/reference/{type}/{topic}", (string type, string topic) =>
{
    var dir = ResolveRefDir(type, helpDir, shelpDir, loreDir);
    var path = SafeTopicPath(dir, topic);
    if (path is null) return Results.NotFound();

    var content = File.ReadAllText(path);
    if (type == "lore") content = ExtractFirstLoreEntry(content);
    return Results.Content(content, "text/plain; charset=utf-8");
});

app.Run();
