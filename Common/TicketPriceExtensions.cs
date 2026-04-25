namespace EventsApp.Common
{
    public static class TicketPriceExtensions
    {
        public static bool IsFreeTicket(this decimal price) => price <= 0m;

        public static string ToTicketPriceDisplay(this decimal price, string freeLabel = "Free", string currencySuffix = "лв.")
        {
            return price.IsFreeTicket()
                ? freeLabel
                : $"{price:0.00} {currencySuffix}";
        }
    }
}
