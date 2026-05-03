using System.ComponentModel.DataAnnotations;

namespace EventsApp.Models
{
    public enum ProfileStatusVisibility
    {
        [Display(Name = "Public")]
        Public = 0,

        [Display(Name = "Followers only")]
        FollowersOnly = 1,

        [Display(Name = "Private")]
        Private = 2,
    }
}
