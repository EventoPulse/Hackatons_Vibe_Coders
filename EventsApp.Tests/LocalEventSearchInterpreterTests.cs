using EventsApp.Models;
using EventsApp.Services.AI;

namespace EventsApp.Tests;

public class LocalEventSearchInterpreterTests
{
    [Fact]
    public void Parse_SeaPartyQuery_UsesCoastalCitiesAndPartyGenresWithoutAi()
    {
        var result = LocalEventSearchInterpreter.Parse("искам парти на морето", new DateTime(2026, 5, 10));

        Assert.NotNull(result.Intent);
        Assert.False(result.ShouldAskAi);
        Assert.True(result.HasStrongIntent);
        Assert.Contains("Varna", result.Intent.Cities);
        Assert.Contains("Burgas", result.Intent.Cities);
        Assert.Contains("Dobrich", result.Intent.Cities);
        Assert.Contains(EventGenre.Nightlife, result.Intent.Genres);
        Assert.Contains(EventGenre.Electronic, result.Intent.Genres);
    }

    [Fact]
    public void Parse_TomorrowJazzInSofia_ExtractsCityGenreAndDate()
    {
        var today = new DateTime(2026, 5, 10);
        var result = LocalEventSearchInterpreter.Parse("джаз утре в София", today);

        Assert.NotNull(result.Intent);
        Assert.False(result.ShouldAskAi);
        Assert.Equal("Sofia", result.Intent.City);
        Assert.Contains(EventGenre.Jazz, result.Intent.Genres);
        Assert.Equal(today.AddDays(1), result.Intent.DateFrom);
        Assert.Equal(today.AddDays(1), result.Intent.DateTo);
    }
}
