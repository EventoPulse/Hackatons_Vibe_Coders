using EventsApp.Models;

namespace EventsApp.ViewModels.Posts
{
    public class PostMediaItemViewModel
    {
        public string Url { get; set; } = null!;
        public PostMediaType MediaType { get; set; }
    }
}
