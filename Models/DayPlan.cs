using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsApp.Models
{
    public class DayPlan
    {
        public DayPlan()
        {
            this.CreatedAt = DateTime.UtcNow;
            this.Items = new HashSet<DayPlanItem>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        [Required]
        [MaxLength(80)]
        public string City { get; set; } = null!;

        [Required]
        public DateTime PlannedFor { get; set; }

        [MaxLength(500)]
        public string? UserRequest { get; set; }

        [MaxLength(120)]
        public string? Vibe { get; set; }

        [MaxLength(160)]
        public string? Title { get; set; }

        [MaxLength(800)]
        public string? Intro { get; set; }

        [MaxLength(64)]
        public string? ShareToken { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public ICollection<DayPlanItem> Items { get; set; }
    }

    public enum DayPlanSlotKind
    {
        Before = 0,
        Main = 1,
        After = 2,
    }

    public class DayPlanItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(DayPlan))]
        public int DayPlanId { get; set; }

        public DayPlan DayPlan { get; set; } = null!;

        [Required]
        public DayPlanSlotKind Slot { get; set; }

        [Required]
        public int Order { get; set; }

        [MaxLength(16)]
        public string? StartTime { get; set; }

        [MaxLength(16)]
        public string? EndTime { get; set; }

        [Required]
        [MaxLength(160)]
        public string Title { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [ForeignKey(nameof(Event))]
        public int? EventId { get; set; }

        public Event? Event { get; set; }
    }
}
