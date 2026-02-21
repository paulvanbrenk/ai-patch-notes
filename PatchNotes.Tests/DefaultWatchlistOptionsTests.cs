using Microsoft.Extensions.Configuration;
using PatchNotes.Data;

namespace PatchNotes.Tests;

public class DefaultWatchlistOptionsTests
{
    [Fact]
    public void Bind_GivenValidConfig_BindsAllSixPackageNames()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DefaultWatchlist:Packages:0"] = "dotnet/runtime",
                ["DefaultWatchlist:Packages:1"] = "dotnet/aspnetcore",
                ["DefaultWatchlist:Packages:2"] = "fastapi/fastapi",
                ["DefaultWatchlist:Packages:3"] = "dotnet/efcore",
                ["DefaultWatchlist:Packages:4"] = "steveyegge/beads",
                ["DefaultWatchlist:Packages:5"] = "steveyegge/gastown",
            })
            .Build();

        var options = new DefaultWatchlistOptions();
        config.GetSection(DefaultWatchlistOptions.SectionName).Bind(options);

        Assert.Equal(6, options.Packages.Length);
        Assert.Equal("dotnet/runtime", options.Packages[0]);
        Assert.Equal("dotnet/aspnetcore", options.Packages[1]);
        Assert.Equal("fastapi/fastapi", options.Packages[2]);
        Assert.Equal("dotnet/efcore", options.Packages[3]);
        Assert.Equal("steveyegge/beads", options.Packages[4]);
        Assert.Equal("steveyegge/gastown", options.Packages[5]);
    }

    [Fact]
    public void Bind_GivenEmptyPackagesArray_ReturnsEmptyList()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var options = new DefaultWatchlistOptions();
        config.GetSection(DefaultWatchlistOptions.SectionName).Bind(options);

        Assert.NotNull(options.Packages);
        Assert.Empty(options.Packages);
    }

    [Fact]
    public void Bind_GivenOptions_SectionNameIsDefaultWatchlist()
    {
        Assert.Equal("DefaultWatchlist", DefaultWatchlistOptions.SectionName);
    }
}
