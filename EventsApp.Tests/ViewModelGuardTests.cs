using EventsApp.Models;
using EventsApp.ViewModels.Events;
using EventsApp.ViewModels.Social;

namespace EventsApp.Tests;

public class ViewModelGuardTests
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void EventCardFreeStateTracksPaidActiveTickets(bool hasActiveTickets, bool hasPaidTickets, bool expected)
    {
        var card = new EventCardViewModel
        {
            HasActiveTickets = hasActiveTickets,
            HasPaidTickets = hasPaidTickets,
        };

        Assert.Equal(expected, card.IsFreeEvent);
    }

    [Fact]
    public void ConversationRequestFlagsSeparateIncomingAndOutgoingRequests()
    {
        var incoming = new ConversationListItemViewModel
        {
            Status = ConversationStatus.Pending,
            IsRequestedByCurrentUser = false,
        };
        var outgoing = new ConversationListItemViewModel
        {
            Status = ConversationStatus.Pending,
            IsRequestedByCurrentUser = true,
        };

        Assert.True(incoming.IsIncomingRequest);
        Assert.False(incoming.IsOutgoingRequest);
        Assert.True(outgoing.IsOutgoingRequest);
        Assert.False(outgoing.IsIncomingRequest);
    }

    [Fact]
    public void PageConversationFlagDependsOnOrganizerProfileId()
    {
        var personal = new ConversationListItemViewModel();
        var page = new ConversationListItemViewModel
        {
            OrganizerProfileId = 42,
            PageName = "Showroom",
        };

        Assert.False(personal.IsPageConversation);
        Assert.True(page.IsPageConversation);
    }
}
