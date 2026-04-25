using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public class Transaction
    {
        public Transaction()
        {
            this.Id = Guid.NewGuid();
            this.CreatedAt = DateTime.UtcNow;
            this.Status = GlobalConstants.TransactionStatuses.Pending;
            this.UserTickets = new HashSet<UserTicket>();
        }

        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        [Required]
        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(GlobalConstants.Ticket.TransactionStatusMaxLength)]
        public string Status { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public ICollection<UserTicket> UserTickets { get; set; }
    }
}
