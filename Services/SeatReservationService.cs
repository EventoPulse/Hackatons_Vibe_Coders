using EventsApp.Data;
using EventsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Services
{
    public interface ISeatReservationService
    {
        Task ReleaseExpiredReservationsAsync();
        Task<EventSeatInventory?> ReserveSeatAsync(int eventId, int? occurrenceId, int seatId, string userId, TimeSpan holdFor);
        Task<bool> ReleaseReservationAsync(int inventoryId, string userId, bool isAdmin);
        Task<bool> MarkSeatSoldAsync(int eventId, int? occurrenceId, int seatId, Guid userTicketId, string userId);
    }

    public class SeatReservationService : ISeatReservationService
    {
        private readonly ApplicationDbContext _db;

        public SeatReservationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task ReleaseExpiredReservationsAsync()
        {
            var now = DateTime.UtcNow;
            await _db.EventSeatInventories
                .Where(i => i.Status == EventSeatInventoryStatus.Reserved
                            && i.ReservedUntil.HasValue
                            && i.ReservedUntil <= now
                            && i.TicketId == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Status, EventSeatInventoryStatus.Available)
                    .SetProperty(i => i.ReservedUntil, (DateTime?)null)
                    .SetProperty(i => i.ReservedByUserId, (string?)null));
        }

        public async Task<EventSeatInventory?> ReserveSeatAsync(int eventId, int? occurrenceId, int seatId, string userId, TimeSpan holdFor)
        {
            await ReleaseExpiredReservationsAsync();

            var now = DateTime.UtcNow;
            var reservedUntil = now.Add(holdFor);
            var affected = await FindInventoryQuery(eventId, occurrenceId, seatId)
                .Where(i => i.Status == EventSeatInventoryStatus.Available
                            || (i.Status == EventSeatInventoryStatus.Reserved
                                && i.ReservedByUserId == userId
                                && i.ReservedUntil > now))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Status, EventSeatInventoryStatus.Reserved)
                    .SetProperty(i => i.ReservedByUserId, userId)
                    .SetProperty(i => i.ReservedUntil, reservedUntil));

            return affected == 0
                ? null
                : await FindInventoryAsync(eventId, occurrenceId, seatId);
        }

        public async Task<bool> ReleaseReservationAsync(int inventoryId, string userId, bool isAdmin)
        {
            var query = _db.EventSeatInventories
                .Where(i => i.Id == inventoryId && i.Status == EventSeatInventoryStatus.Reserved);

            if (!isAdmin)
            {
                query = query.Where(i => i.ReservedByUserId == userId);
            }

            var affected = await query.ExecuteUpdateAsync(s => s
                .SetProperty(i => i.Status, EventSeatInventoryStatus.Available)
                .SetProperty(i => i.ReservedUntil, (DateTime?)null)
                .SetProperty(i => i.ReservedByUserId, (string?)null));

            return affected > 0;
        }

        public async Task<bool> MarkSeatSoldAsync(int eventId, int? occurrenceId, int seatId, Guid userTicketId, string userId)
        {
            var now = DateTime.UtcNow;
            var affected = await FindInventoryQuery(eventId, occurrenceId, seatId)
                .Where(i => i.Status == EventSeatInventoryStatus.Available
                            || (i.Status == EventSeatInventoryStatus.Reserved
                                && i.ReservedByUserId == userId
                                && i.ReservedUntil > now))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Status, EventSeatInventoryStatus.Sold)
                    .SetProperty(i => i.TicketId, (Guid?)userTicketId)
                    .SetProperty(i => i.ReservedUntil, (DateTime?)null)
                    .SetProperty(i => i.ReservedByUserId, (string?)null));

            return affected > 0;
        }

        private Task<EventSeatInventory?> FindInventoryAsync(int eventId, int? occurrenceId, int seatId)
        {
            return FindInventoryQuery(eventId, occurrenceId, seatId).FirstOrDefaultAsync();
        }

        private IQueryable<EventSeatInventory> FindInventoryQuery(int eventId, int? occurrenceId, int seatId)
        {
            return occurrenceId.HasValue
                ? _db.EventSeatInventories.Where(i => i.EventOccurrenceId == occurrenceId.Value && i.SeatId == seatId)
                : _db.EventSeatInventories.Where(i => i.EventId == eventId && i.EventOccurrenceId == null && i.SeatId == seatId);
        }
    }
}
