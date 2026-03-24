using Bunit;
using AckWeb.Client.Aha.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace AckWeb.Tests.Components;

public class ReferenceTests : TestContext
{
    public ReferenceTests()
    {
        // Register a fake HttpClient that returns empty topic lists
        Services.AddSingleton(new HttpClient(new EmptyHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        });
    }

    [Fact]
    public void Reference_DefaultsToHelpTab_WhenNoTabParam()
    {
        var cut = RenderComponent<Reference>();

        var activeLink = cut.Find(".sub-nav a.active");
        Assert.Equal("Help", activeLink.TextContent.Trim());
    }

    [Fact]
    public void Reference_ActivatesShelpTab_WhenTabParamIsShelp()
    {
        var cut = RenderComponent<Reference>(p => p.Add(c => c.Tab, "shelp"));

        var activeLink = cut.Find(".sub-nav a.active");
        Assert.Equal("Spell Help", activeLink.TextContent.Trim());
    }

    [Fact]
    public void Reference_ActivatesLoreTab_WhenTabParamIsLore()
    {
        var cut = RenderComponent<Reference>(p => p.Add(c => c.Tab, "lore"));

        var activeLink = cut.Find(".sub-nav a.active");
        Assert.Equal("Lore", activeLink.TextContent.Trim());
    }

    [Fact]
    public void Reference_RendersSubNavWithThreeTabs()
    {
        var cut = RenderComponent<Reference>();

        var links = cut.FindAll(".sub-nav a");
        Assert.Equal(3, links.Count);
    }

    [Fact]
    public void Reference_ShowsSearchForm_WhenNoTopicParam()
    {
        var cut = RenderComponent<Reference>();

        Assert.NotNull(cut.Find("form"));
    }

    // ── Fake handler ──────────────────────────────────────────────────────

    private sealed class EmptyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
    }
}
