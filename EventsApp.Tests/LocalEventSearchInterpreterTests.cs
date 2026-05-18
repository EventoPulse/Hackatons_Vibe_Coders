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

    [Fact]
    public void Parse_RuseAndNearby_ExtractsCityAndRadius()
    {
        // The two queries the user explicitly named in the spec must
        // parse correctly from the local heuristics alone — so the
        // structured DB query has filters even when the AI is offline.
        var result = LocalEventSearchInterpreter.Parse("Събития в Русе и околието", new DateTime(2026, 5, 10));

        Assert.NotNull(result.Intent);
        Assert.Equal("Ruse", result.Intent.City);
        Assert.Contains("Ruse", result.Intent.Cities);
        Assert.Equal(30, result.Intent.RadiusKm);
        Assert.NotNull(result.Intent.Latitude);
        Assert.NotNull(result.Intent.Longitude);
    }

    [Fact]
    public void Parse_TomorrowSofiaTimeWindow_ExtractsCityDateAndTimeRange()
    {
        var today = new DateTime(2026, 5, 10);
        var result = LocalEventSearchInterpreter.Parse(
            "Утре ще съм в София от 13:00 до 18:00 може ли да ми препоръчаш събития в този часови диапазон",
            today);

        Assert.NotNull(result.Intent);
        Assert.Equal("Sofia", result.Intent.City);
        Assert.Equal(today.AddDays(1), result.Intent.DateFrom);
        Assert.Equal(today.AddDays(1), result.Intent.DateTo);
        Assert.Equal(new TimeSpan(13, 0, 0), result.Intent.StartTimeOfDay);
        Assert.Equal(new TimeSpan(18, 0, 0), result.Intent.EndTimeOfDay);
    }

    [Fact]
    public void Parse_DayAfterTomorrow_DoesNotMisreadAsTomorrow()
    {
        var today = new DateTime(2026, 5, 10);
        var result = LocalEventSearchInterpreter.Parse("вдругиден в Пловдив", today);

        Assert.NotNull(result.Intent);
        Assert.Equal("Plovdiv", result.Intent.City);
        Assert.Equal(today.AddDays(2), result.Intent.DateFrom);
        Assert.Equal(today.AddDays(2), result.Intent.DateTo);
    }

    [Fact]
    public void Parse_NearMe_TriggersRadiusWithoutCity()
    {
        var result = LocalEventSearchInterpreter.Parse("събития около мен", new DateTime(2026, 5, 10));

        Assert.NotNull(result.Intent);
        Assert.True(result.Intent.NearMe);
        Assert.Equal(30, result.Intent.RadiusKm);
        Assert.Null(result.Intent.City);
    }

    [Fact]
    public void Parse_InvalidTimeRange_DropsTimeFields()
    {
        // 18-13 is nonsensical (end before start) — must NOT populate
        // the time-of-day fields so the DB query doesn't filter on it.
        var today = new DateTime(2026, 5, 10);
        var result = LocalEventSearchInterpreter.Parse("утре в София от 18 до 13", today);

        Assert.NotNull(result.Intent);
        Assert.Null(result.Intent.StartTimeOfDay);
        Assert.Null(result.Intent.EndTimeOfDay);
    }
}
