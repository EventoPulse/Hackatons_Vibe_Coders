using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public class EventImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Event))]
        public int EventId { get; set; }

        public Event Event { get; set; } = null!;

        [Required]
        [MaxLength(GlobalConstants.Event.ImageUrlMaxLength)]
        public string ImageUrl { get; set; } = null!;
    }
}
