using System.ComponentModel.DataAnnotations;

namespace EventsApp.ViewModels.Organizer
{
    public class OrganizerValidatorsViewModel
    {
        public int MaxValidators { get; set; } = 3;
        public int ActiveValidatorsCount { get; set; }
        public OrganizerValidatorCreateViewModel NewValidator { get; set; } = new();
        public IReadOnlyList<OrganizerValidatorRowViewModel> Validators { get; set; } = Array.Empty<OrganizerValidatorRowViewModel>();
        public IReadOnlyList<OrganizerValidatorPageOptionViewModel> Pages { get; set; } = Array.Empty<OrganizerValidatorPageOptionViewModel>();
    }

    public class OrganizerValidatorCreateViewModel
    {
        [Required]
        [Display(Name = "Имейл, потребителско име или телефон")]
        public string UserLookup { get; set; } = null!;

        [Required]
        [Display(Name = "Публична страница")]
        public int OrganizerProfileId { get; set; }
    }

    public class OrganizerValidatorRowViewModel
    {
        public int Id { get; set; }
        public string ValidatorUserId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? OrganizerProfileId { get; set; }
        public string? OrganizerProfileName { get; set; }
    }

    public class OrganizerValidatorPageOptionViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public bool IsDefault { get; set; }
    }
}
