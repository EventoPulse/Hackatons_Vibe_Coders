using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Services
{
    public sealed record EventDeletionResult(bool Deleted, string? SkippedReason = null);

    public interface IEventDeletionService
    {
        Task<EventDeletionResult> DeleteEventAsync(
            int eventId,
            bool preservePaidTickets,
            CancellationToken cancellationToken = default);
    }

    public sealed class EventDeletionService : IEventDeletionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IRemoteMediaService _remoteMedia;
        private readonly ILogger<EventDeletionService> _logger;

        public EventDeletionService(
            ApplicationDbContext db,
            IRemoteMediaService remoteMedia,
            ILogger<EventDeletionService> logger)
        {
            _db = db;
            _remoteMedia = remoteMedia;
            _logger = logger;
        }

        public async Task<EventDeletionResult> DeleteEventAsync(
            int eventId,
            bool preservePaidTickets,
            CancellationToken cancellationToken = default)
        {
            var ev = await _db.Events
                .Include(e => e.EventSeries)
                    .ThenInclude(s => s!.Occurrences)
                .Include(e => e.Images)
                .Include(e => e.Tickets)
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (ev == null)
            {
                return new EventDeletionResult(false, "not_found");
            }

            var hasPaidTickets = await HasPaidTicketsAsync(eventId, cancellationToken);
            if (preservePaidTickets && hasPaidTickets)
            {
                return new EventDeletionResult(false, "paid_tickets");
            }

            var mediaUrls = CollectMediaUrls(ev)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var occurrenceIds = ev.EventSeries?.Occurrences.Select(o => o.Id).ToList() ?? new List<int>();
            var ticketIds = ev.Tickets.Select(t => t.Id).ToList();

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var inventoriesQuery = _db.EventSeatInventories.Where(i => i.EventId == ev.Id);
            if (occurrenceIds.Count > 0)
            {
                inventoriesQuery = inventoriesQuery
                    .Union(_db.EventSeatInventories.Where(i => i.EventOccurrenceId.HasValue && occurrenceIds.Contains(i.EventOccurrenceId.Value)));
            }

            var inventories = await inventoriesQuery.ToListAsync(cancellationToken);
            _db.EventSeatInventories.RemoveRange(inventories);

            var userTicketsQuery = _db.UserTickets.Where(ut => ticketIds.Contains(ut.TicketId));
            if (occurrenceIds.Count > 0)
            {
                userTicketsQuery = userTicketsQuery
                    .Union(_db.UserTickets.Where(ut => ut.EventOccurrenceId.HasValue && occurrenceIds.Contains(ut.EventOccurrenceId.Value)));
            }

            var userTickets = await userTicketsQuery.ToListAsync(cancellationToken);
            _db.UserTickets.RemoveRange(userTickets);

            await _db.Messages
                .Where(m => m.SharedEventId == ev.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.SharedEventId, (int?)null), cancellationToken);

            await _db.Posts
                .Where(p => p.EventId == ev.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.EventId, (int?)null), cancellationToken);

            await _db.DayPlanItems
                .Where(i => i.EventId == ev.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.EventId, (int?)null), cancellationToken);

            await _db.Users
                .Where(u => u.PinnedEventId == ev.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.PinnedEventId, (int?)null), cancellationToken);

            _db.Events.Remove(ev);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            foreach (var mediaUrl in mediaUrls)
            {
                try
                {
                    await _remoteMedia.DeleteAsync(mediaUrl, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Event {EventId} was deleted, but media {MediaUrl} could not be deleted.", eventId, mediaUrl);
                }
            }

            _logger.LogInformation("Deleted event {EventId} and {MediaCount} media object(s).", eventId, mediaUrls.Count);
            return new EventDeletionResult(true);
        }

        private Task<bool> HasPaidTicketsAsync(int eventId, CancellationToken cancellationToken)
        {
            return _db.Tickets
                .Where(t => t.EventId == eventId)
                .SelectMany(t => t.UserTickets)
                .AnyAsync(ut => ut.Transaction.Status == GlobalConstants.TransactionStatuses.Paid, cancellationToken);
        }

        private static IEnumerable<string> CollectMediaUrls(Event ev)
        {
            if (!string.IsNullOrWhiteSpace(ev.ImageUrl))
            {
                yield return ev.ImageUrl;
            }

            if (!string.IsNullOrWhiteSpace(ev.EventSeries?.ImageUrl))
            {
                yield return ev.EventSeries.ImageUrl;
            }

            foreach (var image in ev.Images)
            {
                if (!string.IsNullOrWhiteSpace(image.ImageUrl))
                {
                    yield return image.ImageUrl;
                }
            }

            foreach (var ticket in ev.Tickets)
            {
                if (!string.IsNullOrWhiteSpace(ticket.ImageUrl))
                {
                    yield return ticket.ImageUrl;
                }
            }
        }
    }
}
