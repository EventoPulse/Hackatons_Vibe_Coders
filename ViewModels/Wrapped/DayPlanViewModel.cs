using EventsApp.Models;

namespace EventsApp.ViewModels.Wrapped
{
    public class DayPlanViewModel
    {
        public IReadOnlyList<DayPlan> MyPlans { get; set; } = Array.Empty<DayPlan>();
    }
}
