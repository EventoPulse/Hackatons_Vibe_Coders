using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventsApp.Common;

namespace EventsApp.Models
{
    public class Venue
    {
        public Venue()
        {
            this.CreatedAt = DateTime.UtcNow;
            this.Events = new HashSet<Event>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Organizer))]
        public string OrganizerId { get; set; } = null!;

        public ApplicationUser Organizer { get; set; } = null!;

        [Required]
        [MinLength(GlobalConstants.Venue.NameMinLength)]
        [MaxLength(GlobalConstants.Venue.NameMaxLength)]
        public string Name { get; set; } = null!;

        [MaxLength(GlobalConstants.Venue.DescriptionMaxLength)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(GlobalConstants.Venue.AddressMaxLength)]
        public string Address { get; set; } = null!;

        [Required]
        [MaxLength(GlobalConstants.Venue.CityMaxLength)]
        public string City { get; set; } = null!;

        [MaxLength(GlobalConstants.Venue.ImageUrlMaxLength)]
        public string? ImageUrl { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public ICollection<Event> Events { get; set; }
    }
}
