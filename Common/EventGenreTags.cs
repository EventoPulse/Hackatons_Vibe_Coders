using EventsApp.Models;

namespace EventsApp.Common
{
    public static class EventGenreTags
    {
        public const int MaxGenresPerEvent = 3;

        public static IReadOnlyList<EventGenre> Normalize(IEnumerable<EventGenre>? selectedGenres, EventGenre fallback)
        {
            var values = (selectedGenres ?? Array.Empty<EventGenre>())
                .Where(Enum.IsDefined)
                .Distinct()
                .Take(MaxGenresPerEvent)
                .ToList();

            if (values.Count == 0 && Enum.IsDefined(fallback))
            {
                values.Add(fallback);
            }

            return values;
        }

        public static string Serialize(IEnumerable<EventGenre>? genres)
        {
            var values = Normalize(genres, EventGenre.Other);
            return values.Count == 0
                ? string.Empty
                : "|" + string.Join('|', values.Select(g => g.ToString())) + "|";
        }

        public static IReadOnlyList<EventGenre> Parse(string? genreTags, EventGenre fallback)
        {
            if (string.IsNullOrWhiteSpace(genreTags))
            {
                return Normalize(Array.Empty<EventGenre>(), fallback);
            }

            var values = genreTags
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => Enum.TryParse<EventGenre>(value, ignoreCase: true, out var genre) ? (EventGenre?)genre : null)
                .Where(genre => genre.HasValue && Enum.IsDefined(genre.Value))
                .Select(genre => genre!.Value);

            return Normalize(values, fallback);
        }

        public static string Token(EventGenre genre)
        {
            return "|" + genre + "|";
        }
    }
}
