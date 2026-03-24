using Bunit;
using AckWeb.Client.Wol.Shared;

namespace AckWeb.Tests.Components;

public class StoryCardTests : TestContext
{
    [Fact]
    public void StoryCard_RendersCollapsed_ByDefault()
    {
        var cut = RenderComponent<StoryCard>(p => p
            .Add(c => c.Era, "Age of Monuments")
            .Add(c => c.Title, "The Name-Eater")
            .Add(c => c.Teaser, "A dark god devours names."));

        // Container should not have "open" class when collapsed
        var div = cut.Find(".story-card");
        Assert.DoesNotContain("open", div.ClassName ?? "");
    }

    [Fact]
    public void StoryCard_OpensOnHeaderClick()
    {
        var cut = RenderComponent<StoryCard>(p => p
            .Add(c => c.Era, "Age of Monuments")
            .Add(c => c.Title, "The Name-Eater")
            .Add(c => c.Teaser, "A dark god devours names."));

        cut.Find(".story-header").Click();

        var div = cut.Find(".story-card");
        Assert.Contains("open", div.ClassName ?? "");
    }

    [Fact]
    public void StoryCard_ClosesOnSecondClick()
    {
        var cut = RenderComponent<StoryCard>(p => p
            .Add(c => c.Era, "Age of Monuments")
            .Add(c => c.Title, "The Name-Eater")
            .Add(c => c.Teaser, "A dark god devours names."));

        cut.Find(".story-header").Click();
        cut.Find(".story-header").Click();

        var div = cut.Find(".story-card");
        Assert.DoesNotContain("open", div.ClassName ?? "");
    }

    [Fact]
    public void StoryCard_RendersEraAndTitle()
    {
        var cut = RenderComponent<StoryCard>(p => p
            .Add(c => c.Era, "Age of Monuments")
            .Add(c => c.Title, "The Name-Eater")
            .Add(c => c.Teaser, "A dark god devours names."));

        Assert.Equal("Age of Monuments", cut.Find(".story-era").TextContent);
        Assert.Equal("The Name-Eater", cut.Find(".story-title").TextContent);
        Assert.Equal("A dark god devours names.", cut.Find(".story-teaser").TextContent);
    }

    [Fact]
    public void StoryCard_RendersChildContent()
    {
        var cut = RenderComponent<StoryCard>(p => p
            .Add(c => c.Era, "")
            .Add(c => c.Title, "")
            .Add(c => c.Teaser, "")
            .AddChildContent("<p>Story body text.</p>"));

        Assert.Contains("Story body text.", cut.Find(".story-text").InnerHtml);
    }

    [Fact]
    public void StoryCard_DefaultsToEmptyStrings_WhenNoParamsSet()
    {
        var cut = RenderComponent<StoryCard>();

        Assert.Equal("", cut.Find(".story-era").TextContent);
        Assert.Equal("", cut.Find(".story-title").TextContent);
    }
}
