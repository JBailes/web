using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AckWeb.Tests.Api;

public class ApiIntegrationTests : IDisposable
{
    private readonly string _helpDir;
    private readonly string _shelpDir;
    private readonly string _loreDir;
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _helpDir  = Path.Combine(tempRoot, "help");
        _shelpDir = Path.Combine(tempRoot, "shelp");
        _loreDir  = Path.Combine(tempRoot, "lore");
        Directory.CreateDirectory(_helpDir);
        Directory.CreateDirectory(_shelpDir);
        Directory.CreateDirectory(_loreDir);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("HelpDir",  _helpDir);
            builder.UseSetting("ShelpDir", _shelpDir);
            builder.UseSetting("LoreDir",  _loreDir);
            builder.ConfigureServices(services =>
                services.AddHttpClient("game")
                    .ConfigurePrimaryHttpMessageHandler(() => new FakeGameHandler()));
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
        var parent = Directory.GetParent(_helpDir)!.FullName;
        if (Directory.Exists(parent)) Directory.Delete(parent, recursive: true);
    }

    // ── /api/who ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWho_Returns200_WithHtml()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/who");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType ?? "");
    }

    [Fact]
    public async Task GetWho_ReturnsFallback_WhenGameServerFails()
    {
        var factory = _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
                services.AddHttpClient("game")
                    .ConfigurePrimaryHttpMessageHandler(() => new FailingHandler())));

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/who");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<ul></ul>", body);
    }

    // ── /api/gsgp ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGsgp_Returns200_WithJson()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/gsgp");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetGsgp_ReturnsFallbackJson_WhenGameServerFails()
    {
        var factory = _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
                services.AddHttpClient("game")
                    .ConfigurePrimaryHttpMessageHandler(() => new FailingHandler())));

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/gsgp");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("active_players", body);
    }

    // ── /api/reference/{type} ─────────────────────────────────────────────

    [Fact]
    public async Task GetReferenceIndex_Returns200_WithTopicList()
    {
        File.WriteAllText(Path.Combine(_helpDir, "fire"), "fire help");
        File.WriteAllText(Path.Combine(_helpDir, "water"), "water help");

        var client = _factory.CreateClient();
        var names = await client.GetFromJsonAsync<List<string>>("/api/reference/help");

        Assert.NotNull(names);
        Assert.Contains("fire", names);
        Assert.Contains("water", names);
    }

    [Fact]
    public async Task GetReferenceIndex_FiltersTopics_WhenQueryProvided()
    {
        File.WriteAllText(Path.Combine(_helpDir, "fireball"), "");
        File.WriteAllText(Path.Combine(_helpDir, "water"), "");

        var client = _factory.CreateClient();
        var names = await client.GetFromJsonAsync<List<string>>("/api/reference/help?q=fire");

        Assert.NotNull(names);
        Assert.Contains("fireball", names);
        Assert.DoesNotContain("water", names);
    }

    [Fact]
    public async Task GetReferenceIndex_ReturnsEmptyArray_WhenDirIsEmpty()
    {
        var client = _factory.CreateClient();
        var names = await client.GetFromJsonAsync<List<string>>("/api/reference/shelp");

        Assert.NotNull(names);
        Assert.Empty(names);
    }

    // ── /api/reference/{type}/{topic} ─────────────────────────────────────

    [Fact]
    public async Task GetReferenceTopic_Returns200_WithContent()
    {
        File.WriteAllText(Path.Combine(_helpDir, "fire"), "Fire burns things.");

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/reference/help/fire");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Fire burns things.", body);
    }

    [Fact]
    public async Task GetReferenceTopic_Returns404_WhenTopicMissing()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/reference/help/doesnotexist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReferenceTopic_Returns404_OnPathTraversal()
    {
        var client = _factory.CreateClient();
        // URL-encode the path traversal
        var response = await client.GetAsync("/api/reference/help/..%2F..%2Fetc%2Fpasswd");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReferenceTopic_Lore_ReturnsFirstEntryOnly()
    {
        var lore = "keywords dragon\n---\nDragons breathe fire.\n---\nflags city\n---\nCity lore.";
        File.WriteAllText(Path.Combine(_loreDir, "dragon"), lore);

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/reference/lore/dragon");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Dragons breathe fire.", body);
        Assert.DoesNotContain("City lore.", body);
    }

    // ── Fake handlers ─────────────────────────────────────────────────────

    private sealed class FakeGameHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var content = path switch
            {
                "/who"  => "<h2>Players Online</h2><ul><li>Gandalf</li></ul>",
                "/gsgp" => "{\"name\":\"ACK!MUD TNG\",\"active_players\":1,\"leaderboards\":[]}",
                _       => ""
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            });
        }
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated game server failure");
    }
}
