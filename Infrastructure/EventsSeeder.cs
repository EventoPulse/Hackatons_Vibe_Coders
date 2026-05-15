using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Infrastructure
{
    public static class EventsSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var db = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(EventsSeeder));

            var admins = await userManager.GetUsersInRoleAsync(GlobalConstants.Roles.Admin);
            var admin = admins.FirstOrDefault();
            if (admin == null)
            {
                logger.LogWarning("EventsSeeder skipped: no admin user found.");
                return;
            }

            var today = DateTime.UtcNow.Date;
            var seedSpecs = BuildSeedSpecs();

            var seedTitles = seedSpecs.Select(s => s.Title).ToList();
            var existingEvents = await db.Events
                .Include(e => e.Tickets)
                .Where(e => seedTitles.Contains(e.Title))
                .ToListAsync();

            foreach (var existing in existingEvents)
            {
                var spec = seedSpecs.First(s => s.Title == existing.Title);
                if (string.IsNullOrWhiteSpace(existing.ImageUrl))
                {
                    existing.ImageUrl = SeedImage(spec.ImageSeed);
                }

                // Backfill tickets for previously seeded events that pre-date the
                // ticket support — only add if the event has none at all.
                if (existing.Tickets.Count == 0)
                {
                    foreach (var t in spec.Tickets)
                    {
                        existing.Tickets.Add(new Ticket
                        {
                            Name = t.Name,
                            Description = t.Description,
                            Price = t.Price,
                            QuantityTotal = t.QuantityTotal,
                            QuantityRemaining = t.QuantityTotal,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                        });
                    }
                }
            }

            var toInsert = seedSpecs
                .Where(s => existingEvents.All(e => e.Title != s.Title))
                .Select(s => BuildEventFromSpec(admin.Id, today, s))
                .ToList();

            if (toInsert.Count == 0 && !db.ChangeTracker.HasChanges())
            {
                return;
            }

            db.Events.AddRange(toInsert);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} events.", toInsert.Count);
        }

        private static Event BuildEventFromSpec(string organizerId, DateTime today, SeedEventSpec spec)
        {
            var ev = new Event
            {
                OrganizerId = organizerId,
                Title = spec.Title,
                Description = spec.Description,
                StartTime = today.AddDays(spec.DaysFromToday).AddHours(spec.StartHour).AddMinutes(spec.StartMinute),
                EndTime = today.AddDays(spec.DaysFromToday + spec.EndDayOffset).AddHours(spec.EndHour).AddMinutes(spec.EndMinute),
                Genre = spec.Genre,
                Address = spec.Address,
                City = spec.City,
                Latitude = spec.Latitude,
                Longitude = spec.Longitude,
                ImageUrl = SeedImage(spec.ImageSeed),
                IsApproved = true,
                TicketingMode = EventTicketingMode.GeneralAdmission,
                CreatedAt = DateTime.UtcNow,
            };

            foreach (var t in spec.Tickets)
            {
                ev.Tickets.Add(new Ticket
                {
                    Name = t.Name,
                    Description = t.Description,
                    Price = t.Price,
                    QuantityTotal = t.QuantityTotal,
                    QuantityRemaining = t.QuantityTotal,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            return ev;
        }

        private static List<SeedEventSpec> BuildSeedSpecs()
        {
            return new List<SeedEventSpec>
            {
                new SeedEventSpec
                {
                    Title = "Рок вечер в Каракул — Русе",
                    Description = "Жива рок музика на високо ниво в сърцето на Русе. Местни и гостуващи групи.",
                    DaysFromToday = 14,
                    StartHour = 20, StartMinute = 0,
                    EndDayOffset = 0, EndHour = 23, EndMinute = 30,
                    Genre = EventGenre.Rock,
                    Address = "ул. Александровска 87, Русе",
                    City = "Русе",
                    Latitude = 43.8564, Longitude = 25.9658,
                    ImageSeed = "rock-ruse",
                    Tickets = new[]
                    {
                        new SeedTicketSpec { Name = "Стандартен", Description = "Вход на крака", Price = 25m, QuantityTotal = 200 },
                        new SeedTicketSpec { Name = "VIP", Description = "Места близо до сцената, бар в зоната", Price = 60m, QuantityTotal = 40 },
                    },
                },
                new SeedEventSpec
                {
                    Title = "Jazz Night при Дунава — Русе",
                    Description = "Камерна джаз вечер с гледка към реката. Топла атмосфера и качествена музика.",
                    DaysFromToday = 21,
                    StartHour = 19, StartMinute = 30,
                    EndDayOffset = 0, EndHour = 22, EndMinute = 30,
                    Genre = EventGenre.Jazz,
                    Address = "Кей крайбрежна, Русе",
                    City = "Русе",
                    Latitude = 43.8564, Longitude = 25.9658,
                    ImageSeed = "jazz-ruse",
                    Tickets = new[]
                    {
                        new SeedTicketSpec { Name = "Резервация на маса", Description = "Маса за двама + добре дошъл коктейл", Price = 70m, QuantityTotal = 40 },
                        new SeedTicketSpec { Name = "Бар стол", Description = "Свободно сядане на бара", Price = 35m, QuantityTotal = 60 },
                    },
                },
                new SeedEventSpec
                {
                    Title = "Electronic Underground — Русе",
                    Description = "Техно и хаус до сутринта. Местни и международни DJ-и.",
                    DaysFromToday = 28,
                    StartHour = 22, StartMinute = 0,
                    EndDayOffset = 1, EndHour = 4, EndMinute = 0,
                    Genre = EventGenre.Electronic,
                    Address = "Индустриална зона, Русе",
                    City = "Русе",
                    Latitude = 43.8564, Longitude = 25.9658,
                    ImageSeed = "electronic-ruse",
                    Tickets = new[]
                    {
                        new SeedTicketSpec { Name = "Early bird", Description = "Ограничен брой ранни билети", Price = 20m, QuantityTotal = 100 },
                        new SeedTicketSpec { Name = "Регулярен", Description = "Вход за вечерта", Price = 30m, QuantityTotal = 200 },
                    },
                },
                new SeedEventSpec
                {
                    Title = "Поп вечер в София",
                    Description = "Любими хитове и нови парчета на живо. Голяма сцена в центъра на София.",
                    DaysFromToday = 35,
                    StartHour = 20, StartMinute = 0,
                    EndDayOffset = 0, EndHour = 23, EndMinute = 0,
                    Genre = EventGenre.Pop,
                    Address = "пл. Народно събрание 1, София",
                    City = "София",
                    Latitude = 42.6977, Longitude = 23.3219,
                    ImageSeed = "pop-sofia",
                    Tickets = new[]
                    {
                        new SeedTicketSpec { Name = "Стандартен", Description = "Седящи места, втора зона", Price = 40m, QuantityTotal = 400 },
                        new SeedTicketSpec { Name = "Партер", Description = "Места близо до сцената", Price = 85m, QuantityTotal = 120 },
                    },
                },
                new SeedEventSpec
                {
                    Title = "Театър под звездите — Пловдив",
                    Description = "Класика на открито в Античния театър. Незабравима лятна вечер.",
                    DaysFromToday = 42,
                    StartHour = 21, StartMinute = 0,
                    EndDayOffset = 0, EndHour = 23, EndMinute = 30,
                    Genre = EventGenre.Theater,
                    Address = "Античен театър, Пловдив",
                    City = "Пловдив",
                    Latitude = 42.1465, Longitude = 24.7480,
                    ImageSeed = "theater-plovdiv",
                    Tickets = new[]
                    {
                        new SeedTicketSpec { Name = "Партер", Description = "Каменни седалки, първи редове", Price = 50m, QuantityTotal = 200 },
                        new SeedTicketSpec { Name = "Балкон", Description = "Горните редове, общ вход", Price = 30m, QuantityTotal = 150 },
                    },
                },
            };
        }

        private static string SeedImage(string seed) => $"https://picsum.photos/seed/seed-event-{seed}/1200/720";

        private sealed class SeedEventSpec
        {
            public string Title { get; init; } = null!;
            public string Description { get; init; } = null!;
            public int DaysFromToday { get; init; }
            public int StartHour { get; init; }
            public int StartMinute { get; init; }
            public int EndDayOffset { get; init; }
            public int EndHour { get; init; }
            public int EndMinute { get; init; }
            public EventGenre Genre { get; init; }
            public string Address { get; init; } = null!;
            public string City { get; init; } = null!;
            public double Latitude { get; init; }
            public double Longitude { get; init; }
            public string ImageSeed { get; init; } = null!;
            public IReadOnlyList<SeedTicketSpec> Tickets { get; init; } = Array.Empty<SeedTicketSpec>();
        }

        private sealed class SeedTicketSpec
        {
            public string Name { get; init; } = null!;
            public string? Description { get; init; }
            public decimal Price { get; init; }
            public int QuantityTotal { get; init; }
        }
    }
}
