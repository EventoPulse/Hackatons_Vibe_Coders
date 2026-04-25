using System.ComponentModel.DataAnnotations;
using EventsApp.Common;

namespace EventsApp.ViewModels.Tickets
{
    public class TicketValidationViewModel
    {
        [Required]
        [StringLength(GlobalConstants.Ticket.QrCodeMaxLength)]
        [Display(Name = "QR code")]
        public string QrCode { get; set; } = null!;
    }

    public class TicketValidationResultViewModel
    {
        public bool Valid { get; set; }
        public bool AlreadyUsed { get; set; }
        public bool NotFound { get; set; }
        public bool NotAllowed { get; set; }
        public string? Message { get; set; }
        public UserTicketDetailsViewModel? Ticket { get; set; }
    }
}
