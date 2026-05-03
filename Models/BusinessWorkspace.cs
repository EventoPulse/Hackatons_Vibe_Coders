using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public enum BusinessWorkspaceStatus
    {
        Draft = 0,
        Active = 1,
        Suspended = 2,
        Archived = 3,
    }

    public enum PaymentProvider
    {
        None = 0,
        Stripe = 1,
    }

    public enum StripeOnboardingStatus
    {
        NotStarted = 0,
        Pending = 1,
        Complete = 2,
        Restricted = 3,
    }

    public class BusinessWorkspace
    {
        public BusinessWorkspace()
        {
            this.Status = BusinessWorkspaceStatus.Active;
            this.PaymentProvider = PaymentProvider.None;
            this.StripeOnboardingStatus = StripeOnboardingStatus.NotStarted;
            this.CreatedAt = DateTime.UtcNow;
            this.UpdatedAt = this.CreatedAt;
            this.OrganizerProfiles = new HashSet<OrganizerProfile>();
            this.Events = new HashSet<Event>();
            this.Posts = new HashSet<Post>();
            this.Stories = new HashSet<Story>();
            this.Transactions = new HashSet<Transaction>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Owner))]
        public string OwnerId { get; set; } = null!;

        public ApplicationUser Owner { get; set; } = null!;

        [Required]
        [MaxLength(GlobalConstants.Organizer.OrganizationNameMaxLength)]
        public string DisplayName { get; set; } = null!;

        [Required]
        [MaxLength(GlobalConstants.Organizer.OrganizationNameMaxLength)]
        public string LegalName { get; set; } = null!;

        [MaxLength(GlobalConstants.Organizer.CompanyNumberMaxLength)]
        public string? CompanyNumber { get; set; }

        [MaxLength(GlobalConstants.Organizer.ContactEmailMaxLength)]
        public string? BillingEmail { get; set; }

        [MaxLength(GlobalConstants.Organizer.PhoneNumberMaxLength)]
        public string? PhoneNumber { get; set; }

        [MaxLength(GlobalConstants.Event.AddressMaxLength)]
        public string? Address { get; set; }

        [MaxLength(GlobalConstants.Organizer.CityMaxLength)]
        public string? City { get; set; }

        [MaxLength(80)]
        public string? Country { get; set; }

        public BusinessWorkspaceStatus Status { get; set; }

        public bool IsDefault { get; set; }

        [MaxLength(160)]
        public string? StripeConnectedAccountId { get; set; }

        public StripeOnboardingStatus StripeOnboardingStatus { get; set; }

        public bool PayoutsEnabled { get; set; }

        public bool ChargesEnabled { get; set; }

        public PaymentProvider PaymentProvider { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        public ICollection<OrganizerProfile> OrganizerProfiles { get; set; }

        public ICollection<Event> Events { get; set; }

        public ICollection<Post> Posts { get; set; }

        public ICollection<Story> Stories { get; set; }

        public ICollection<Transaction> Transactions { get; set; }
    }
}
