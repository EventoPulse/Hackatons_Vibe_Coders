namespace EventsApp.ViewModels.Tickets
{
    public class AdminTicketsViewModel
    {
        public int TicketTypesCount { get; set; }
        public int TicketsSoldCount { get; set; }
        public int UsedTicketsCount { get; set; }
        public int UnusedTicketsCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public IReadOnlyList<AdminTicketRowViewModel> Recent { get; set; } = Array.Empty<AdminTicketRowViewModel>();
    }

    public class AdminTicketRowViewModel
    {
        public Guid Id { get; set; }
        public string TicketName { get; set; } = null!;
        public string EventTitle { get; set; } = null!;
        public int EventId { get; set; }
        public string OwnerUserName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsUsed { get; set; }
        public decimal Price { get; set; }
    }

    public class AdminTransactionsViewModel
    {
        public IReadOnlyList<AdminTransactionRowViewModel> Transactions { get; set; } = Array.Empty<AdminTransactionRowViewModel>();
        public decimal TotalRevenue { get; set; }
    }

    public class AdminTransactionRowViewModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int TicketCount { get; set; }
    }
}
