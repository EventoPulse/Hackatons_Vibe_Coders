using EventsApp.Common;
using EventsApp.Data;
using EventsApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Infrastructure
{
    public static class DemoDataSeeder
    {
        private const string DemoPassword = "Demo123";
        // Sentinels used to detect whether the demo set has already been seeded.
        private const string SentinelEmail = "ivan.dimitrov@demo.bg";
        private const string ExtendedSentinelEmail = "rock.republic@demo.bg";

        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            var rnd = new Random(20260425);
            var now = DateTime.UtcNow;

            if (!await db.Users.AnyAsync(u => u.Email == SentinelEmail))
            {
                await SeedBaseAsync(db, userManager, rnd, now);
            }

            if (!await db.Users.AnyAsync(u => u.Email == ExtendedSentinelEmail))
            {
                await SeedExtendedAsync(db, userManager, rnd, now);
            }
        }

        private static async Task SeedBaseAsync(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            Random rnd,
            DateTime now)
        {

            var organizers = new[]
            {
                new
                {
                    Email = "sofia.sound@demo.bg", Username = "sofia.sound", First = "Александър", Last = "Петров",
                    OrgName = "Sofia Sound Collective", Phone = "+359 88 555 0101", Web = "https://sofiasound.bg",
                    Company = "BG203456789",
                    Bio = "Колектив от диджеи и промоутъри за тъмни техно нощи в столицата.",
                    Avatar = "https://images.unsplash.com/photo-1571266028243-d220bc6e3e3e?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "beat.rooms@demo.bg", Username = "beat.rooms", First = "Мира", Last = "Стоянова",
                    OrgName = "Beat Rooms Plovdiv", Phone = "+359 89 412 7733", Web = "https://beatrooms.bg",
                    Company = "BG204998112",
                    Bio = "Камерни джаз и lounge вечери в сърцето на Капана.",
                    Avatar = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "sea.wave@demo.bg", Username = "sea.wave", First = "Никола", Last = "Кирилов",
                    OrgName = "Sea Wave Events", Phone = "+359 87 200 4480", Web = "https://seawave.bg",
                    Company = "BG205667291",
                    Bio = "Летни фестивали и beach paries по Черноморието.",
                    Avatar = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=400&q=80",
                },
            };

            var orgUsers = new List<ApplicationUser>();
            foreach (var o in organizers)
            {
                var u = new ApplicationUser
                {
                    UserName = o.Username,
                    Email = o.Email,
                    EmailConfirmed = true,
                    FirstName = o.First,
                    LastName = o.Last,
                    Bio = o.Bio,
                    ProfileImageUrl = o.Avatar,
                    CreatedAt = now.AddMonths(-6),
                };
                var res = await userManager.CreateAsync(u, DemoPassword);
                if (!res.Succeeded) continue;

                await userManager.AddToRoleAsync(u, GlobalConstants.Roles.Organizer);
                db.OrganizerData.Add(new OrganizerData
                {
                    OrganizerId = u.Id,
                    OrganizationName = o.OrgName,
                    Description = o.Bio,
                    PhoneNumber = o.Phone,
                    Website = o.Web,
                    CompanyNumber = o.Company,
                    Approved = true,
                    CreatedAt = now.AddMonths(-6),
                });
                orgUsers.Add(u);
            }

            var users = new[]
            {
                new { Email = "ivan.dimitrov@demo.bg", Username = "ivan.dimitrov", First = "Иван", Last = "Димитров",
                    Bio = "Меломан, DJ от време на време.",
                    Avatar = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=400&q=80" },
                new { Email = "petya.k@demo.bg", Username = "petya.k", First = "Петя", Last = "Колева",
                    Bio = "Обичам джаз и виновни вечери в Капана.",
                    Avatar = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?auto=format&fit=crop&w=400&q=80" },
                new { Email = "georgi.t@demo.bg", Username = "georgi.t", First = "Георги", Last = "Тодоров",
                    Bio = "Живея за летните фестивали по морето.",
                    Avatar = "https://images.unsplash.com/photo-1607746882042-944635dfe10e?auto=format&fit=crop&w=400&q=80" },
                new { Email = "maria.v@demo.bg", Username = "maria.v", First = "Мария", Last = "Василева",
                    Bio = "Хроникьор на нощния живот в София.",
                    Avatar = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=400&q=80" },
                new { Email = "stefan.r@demo.bg", Username = "stefan.r", First = "Стефан", Last = "Райков",
                    Bio = "Пътувам за концерти. Бира + рок > всичко.",
                    Avatar = "https://images.unsplash.com/photo-1628157588553-5eeea00af15c?auto=format&fit=crop&w=400&q=80" },
            };

            var regularUsers = new List<ApplicationUser>();
            foreach (var u in users)
            {
                var au = new ApplicationUser
                {
                    UserName = u.Username,
                    Email = u.Email,
                    EmailConfirmed = true,
                    FirstName = u.First,
                    LastName = u.Last,
                    Bio = u.Bio,
                    ProfileImageUrl = u.Avatar,
                    CreatedAt = now.AddMonths(-3),
                };
                var res = await userManager.CreateAsync(au, DemoPassword);
                if (!res.Succeeded) continue;
                await userManager.AddToRoleAsync(au, GlobalConstants.Roles.User);
                regularUsers.Add(au);
            }

            await db.SaveChangesAsync();

            // Events: id maps to whether tickets should be generated.
            var eventsSpec = new[]
            {
                new
                {
                    Org = orgUsers[0],
                    Title = "Underground Pulse Vol. 12",
                    Description = "Дванадесетата нощ от Underground Pulse поредицата събира два международни хедлайнера и трима локални любимци за безкомпромисно техно сет от 22:00 до 06:00. Очаквайте мощна звукова система, дим, лазери и плътна публика.",
                    City = "София", Address = "Mixtape 5, ул. Шейново 7",
                    Lat = 42.6957, Lng = 23.3338,
                    Genre = EventGenre.Electronic,
                    Image = "https://images.unsplash.com/photo-1571266028243-d220bc6e3e3e?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 14, DurationHours = 8, HasTickets = true,
                },
                new
                {
                    Org = orgUsers[0],
                    Title = "House Therapy: Sunday Edition",
                    Description = "Дневен сет с deep и soulful house в градината на НДК. Без билети, безплатен вход — само вибрация и слънце.",
                    City = "София", Address = "НДК, пл. България 1",
                    Lat = 42.6868, Lng = 23.3192,
                    Genre = EventGenre.Pop,
                    Image = "https://images.unsplash.com/photo-1429962714451-bb934ecdc4ec?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 21, DurationHours = 6, HasTickets = false,
                },
                new
                {
                    Org = orgUsers[1],
                    Title = "Jazz Sessions Vol. 7",
                    Description = "Седмо издание на джаз вечерите в Капана. Българско трио + специален китарист от Гърция в интимна камерна обстановка.",
                    City = "Пловдив", Address = "Капана, ул. Абаджийска 8",
                    Lat = 42.1505, Lng = 24.7505,
                    Genre = EventGenre.Jazz,
                    Image = "https://images.unsplash.com/photo-1516280440614-37939bbacd81?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 9, DurationHours = 4, HasTickets = true,
                },
                new
                {
                    Org = orgUsers[1],
                    Title = "Акустично & Живо",
                    Description = "Камерна вечер с трима акустични изпълнители и swap китари. Подходящо за идеална първа среща.",
                    City = "Пловдив", Address = "Кафе Petnoto, ул. 4-ти януари 25",
                    Lat = 42.1450, Lng = 24.7460,
                    Genre = EventGenre.Folk,
                    Image = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = -10, DurationHours = 3, HasTickets = false,
                },
                new
                {
                    Org = orgUsers[2],
                    Title = "Sunwaves x Burgas Beach",
                    Description = "Двудневен open-air фестивал на плажа с 12 диджеи на две сцени. Парти от залез до изгрев.",
                    City = "Бургас", Address = "Морска градина, северен плаж",
                    Lat = 42.4912, Lng = 27.4805,
                    Genre = EventGenre.Festival,
                    Image = "https://images.unsplash.com/photo-1459749411175-04bf5292ceea?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 35, DurationHours = 36, HasTickets = true,
                },
                new
                {
                    Org = orgUsers[2],
                    Title = "Varna Beach Sessions",
                    Description = "Седмични петъчни сетове на плажа на Варна. Свободен достъп, donation jar за артистите.",
                    City = "Варна", Address = "Морско казино, плаж Север",
                    Lat = 43.2099, Lng = 27.9251,
                    Genre = EventGenre.Electronic,
                    Image = "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 4, DurationHours = 5, HasTickets = false,
                },
                new
                {
                    Org = orgUsers[0],
                    Title = "Vinyl Only: 90s Hip Hop Night",
                    Description = "Само от винил. Класиките на 90-те с двама диджеи, които си носят кутиите. Ограничен капацитет — 120 души.",
                    City = "София", Address = "Bar 100 grams, ул. Парчевич 38",
                    Lat = 42.6929, Lng = 23.3158,
                    Genre = EventGenre.HipHop,
                    Image = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 7, DurationHours = 5, HasTickets = true,
                },
                new
                {
                    Org = orgUsers[1],
                    Title = "Класика на свещи",
                    Description = "Камерен квартет изпълнява Вивалди и Бах в зала, осветена от 200 свещи. Без приказки, без телефони.",
                    City = "Пловдив", Address = "Античен театър, Стария град",
                    Lat = 42.1466, Lng = 24.7510,
                    Genre = EventGenre.Classical,
                    Image = "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 28, DurationHours = 2, HasTickets = true,
                },
            };

            var createdEvents = new List<Event>();
            foreach (var e in eventsSpec)
            {
                var start = now.AddDays(e.StartOffsetDays);
                var ev = new Event
                {
                    OrganizerId = e.Org.Id,
                    Title = e.Title,
                    Description = e.Description,
                    City = e.City,
                    Address = e.Address,
                    Latitude = e.Lat,
                    Longitude = e.Lng,
                    Genre = e.Genre,
                    ImageUrl = e.Image,
                    StartTime = start,
                    EndTime = start.AddHours(e.DurationHours),
                    IsApproved = true,
                    CreatedAt = now.AddDays(-Math.Abs(e.StartOffsetDays) - 5),
                };
                db.Events.Add(ev);
                createdEvents.Add(ev);
            }
            await db.SaveChangesAsync();

            // Tickets — only for events flagged HasTickets
            var ticketsByEvent = new Dictionary<int, List<Ticket>>();
            for (int i = 0; i < eventsSpec.Length; i++)
            {
                if (!eventsSpec[i].HasTickets) continue;
                var ev = createdEvents[i];

                var ticketSet = ev.Genre switch
                {
                    EventGenre.Festival => new[]
                    {
                        ("Earlybird Pass", "Двудневен пропуск, ограничено количество.", 89m, 100, 42),
                        ("Standard Pass", "Двудневен пропуск.", 129m, 400, 215),
                        ("VIP Pass", "VIP зона, бар, паркинг.", 249m, 60, 18),
                    },
                    EventGenre.Classical => new[]
                    {
                        ("Места партер", "Седящи места, партер.", 45m, 80, 36),
                        ("Балкон", "Седящи места, балкон.", 30m, 60, 21),
                    },
                    EventGenre.Jazz => new[]
                    {
                        ("Стандартен билет", "Свободни места.", 25m, 80, 47),
                    },
                    EventGenre.HipHop => new[]
                    {
                        ("Ранна предпродажба", "Ограничено до 50 бр.", 20m, 50, 31),
                        ("Стандартен билет", "Стандартен вход.", 30m, 70, 12),
                    },
                    _ => new[]
                    {
                        ("Standard", "Целогодишен достъп до залата.", 35m, 250, 124),
                        ("VIP", "Зона с бар и сядане.", 70m, 50, 18),
                    },
                };

                var list = new List<Ticket>();
                foreach (var (name, desc, price, total, sold) in ticketSet)
                {
                    var tk = new Ticket
                    {
                        EventId = ev.Id,
                        Name = name,
                        Description = desc,
                        Price = price,
                        QuantityTotal = total,
                        QuantityRemaining = total - sold,
                        IsActive = true,
                        CreatedAt = now.AddDays(-30),
                    };
                    db.Tickets.Add(tk);
                    list.Add(tk);
                }
                ticketsByEvent[ev.Id] = list;
            }
            await db.SaveChangesAsync();

            // Generate user purchases (Paid transactions) so the dashboard stats are populated.
            foreach (var (evId, ticketList) in ticketsByEvent)
            {
                foreach (var tk in ticketList)
                {
                    var soldCount = tk.QuantityTotal - tk.QuantityRemaining;
                    soldCount = Math.Min(soldCount, 30); // cap at 30 user-tickets per type for seed
                    if (soldCount <= 0) continue;

                    for (int s = 0; s < soldCount; s++)
                    {
                        var buyer = regularUsers[rnd.Next(regularUsers.Count)];
                        var tx = new Transaction
                        {
                            UserId = buyer.Id,
                            TotalAmount = tk.Price,
                            Status = GlobalConstants.TransactionStatuses.Paid,
                            CreatedAt = now.AddDays(-rnd.Next(1, 28)).AddHours(-rnd.Next(0, 23)),
                        };
                        db.Transactions.Add(tx);

                        var ut = new UserTicket
                        {
                            TicketId = tk.Id,
                            Transaction = tx,
                            QrCode = Guid.NewGuid().ToString("N"),
                            IsUsed = rnd.NextDouble() < 0.18,
                            CreatedAt = tx.CreatedAt,
                        };
                        if (ut.IsUsed)
                        {
                            ut.UsedAt = tx.CreatedAt.AddHours(rnd.Next(1, 48));
                            ut.UsedByOrganizerId = ((Event)createdEvents.First(e => e.Id == evId)).OrganizerId;
                        }
                        db.UserTickets.Add(ut);
                    }
                }
            }
            await db.SaveChangesAsync();

            // Posts (some referencing events, some standalone)
            var postSpecs = new (ApplicationUser Org, string Content, int? EventIdx, string[] Images)[]
            {
                (orgUsers[0], "Билетите за Underground Pulse Vol. 12 излязоха. Ranged предпродажба до петък — после се вдига цената.", 0,
                    new[] { "https://images.unsplash.com/photo-1571266028243-d220bc6e3e3e?auto=format&fit=crop&w=1000&q=80" }),
                (orgUsers[0], "Снимки от последното House Therapy. Благодарим на всички! 🌞", 1,
                    new[] {
                        "https://images.unsplash.com/photo-1429962714451-bb934ecdc4ec?auto=format&fit=crop&w=1000&q=80",
                        "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=1000&q=80"
                    }),
                (orgUsers[1], "Beat Rooms се мести в нова зала! Очаквайте Jazz Sessions Vol. 7 на новия адрес.", 2,
                    new[] { "https://images.unsplash.com/photo-1516280440614-37939bbacd81?auto=format&fit=crop&w=1000&q=80" }),
                (orgUsers[1], "Понякога не ни трябват пиано и контрабас — само 6 струни и една свещ.", 3,
                    Array.Empty<string>()),
                (orgUsers[2], "Sunwaves x Burgas: разкриваме третия headliner следващия вторник. Подсказка: Берлин 🇩🇪", 4,
                    new[] { "https://images.unsplash.com/photo-1459749411175-04bf5292ceea?auto=format&fit=crop&w=1000&q=80" }),
                (orgUsers[2], "Beach Session #4 беше пълнен. Видео от вечерта — линк в bio.", 5,
                    new[] { "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?auto=format&fit=crop&w=1000&q=80" }),
                (orgUsers[0], "Търсим резидентни диджеи за есенната ни поредица. DM-нете ни до 30-ти.", null,
                    Array.Empty<string>()),
                (orgUsers[1], "Какво бихте искали да чуете на следващия Jazz Sessions? Коментирайте — топ 3 ще влязат в плейлистата на вечерта.", null,
                    Array.Empty<string>()),
            };

            var createdPosts = new List<Post>();
            foreach (var p in postSpecs)
            {
                var post = new Post
                {
                    OrganizerId = p.Org.Id,
                    EventId = p.EventIdx.HasValue ? createdEvents[p.EventIdx.Value].Id : null,
                    Content = p.Content,
                    CreatedAt = now.AddDays(-rnd.Next(1, 25)).AddHours(-rnd.Next(0, 23)),
                };
                db.Posts.Add(post);
                createdPosts.Add(post);
            }
            await db.SaveChangesAsync();

            for (int i = 0; i < postSpecs.Length; i++)
            {
                foreach (var img in postSpecs[i].Images)
                {
                    db.PostImages.Add(new PostImage
                    {
                        PostId = createdPosts[i].Id,
                        ImageUrl = img,
                        MediaType = PostMediaType.Image,
                    });
                }
            }
            await db.SaveChangesAsync();

            // Likes & comments on events
            var eventComments = new[]
            {
                "Идвам! Кой иска да се събере преди?",
                "Last edition was insane 🔥",
                "Има ли паркинг наблизо?",
                "Това е точно това, което ми трябваше за уикенда.",
                "Препоръчвам! Бях преди и звукът беше топ.",
                "Ще има ли late check-in?",
                "Можем ли да ползваме картата за плащане на бара?",
                "Колко време продължава първият сет?",
            };

            foreach (var ev in createdEvents)
            {
                var likers = regularUsers.OrderBy(_ => rnd.Next()).Take(rnd.Next(2, regularUsers.Count + 1)).ToList();
                foreach (var u in likers)
                {
                    db.EventLikes.Add(new EventLike
                    {
                        EventId = ev.Id,
                        UserId = u.Id,
                        CreatedAt = now.AddDays(-rnd.Next(1, 30)),
                    });
                }

                var commenters = regularUsers.OrderBy(_ => rnd.Next()).Take(rnd.Next(1, 4)).ToList();
                foreach (var u in commenters)
                {
                    db.EventComments.Add(new EventComment
                    {
                        EventId = ev.Id,
                        UserId = u.Id,
                        Content = eventComments[rnd.Next(eventComments.Length)],
                        CreatedAt = now.AddDays(-rnd.Next(1, 25)).AddHours(-rnd.Next(0, 23)),
                    });
                }
            }

            // Likes & comments on posts
            var postComments = new[]
            {
                "❤️",
                "Идваме с приятели!",
                "Звучи страхотно, чакаме повече инфо.",
                "Кой ще е headliner-ът?",
                "Бях на миналата вечер — топ беше.",
                "Можем ли да си купим билет на входа?",
                "Кога има отстъпка за студенти?",
                "Любим организатор 🙌",
            };

            foreach (var post in createdPosts)
            {
                var likers = regularUsers.OrderBy(_ => rnd.Next()).Take(rnd.Next(1, regularUsers.Count + 1)).ToList();
                foreach (var u in likers)
                {
                    db.PostLikes.Add(new PostLike
                    {
                        PostId = post.Id,
                        UserId = u.Id,
                        CreatedAt = now.AddDays(-rnd.Next(1, 20)),
                    });
                }

                var commenters = regularUsers.OrderBy(_ => rnd.Next()).Take(rnd.Next(0, 3)).ToList();
                foreach (var u in commenters)
                {
                    db.PostComments.Add(new PostComment
                    {
                        PostId = post.Id,
                        UserId = u.Id,
                        Content = postComments[rnd.Next(postComments.Length)],
                        CreatedAt = post.CreatedAt.AddHours(rnd.Next(1, 60)),
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task SeedExtendedAsync(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            Random rnd,
            DateTime now)
        {
            // 15 additional organizers spanning rock, metal, theatre, comedy, folk,
            // classical, cinema, conferences, yoga, winter, vinyl, art, sports.
            var organizers = new[]
            {
                new
                {
                    Email = "rock.republic@demo.bg", Username = "rock.republic", First = "Васил", Last = "Маринов",
                    OrgName = "Rock Republic Sofia", Phone = "+359 88 770 2210", Web = "https://rockrepublic.bg",
                    Company = "BG206771122",
                    Bio = "Промоутъри на български и балкански рок групи в столицата.",
                    Avatar = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "metal.heart@demo.bg", Username = "metal.heart", First = "Илиян", Last = "Колев",
                    OrgName = "Metal Heart Productions", Phone = "+359 89 113 4477", Web = "https://metalheart.bg",
                    Company = "BG206891003",
                    Bio = "Метъл и хардрок концерти и фестивали из цялата страна.",
                    Avatar = "https://images.unsplash.com/photo-1493676304819-0d7a8d026dcf?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "plovdiv.drama@demo.bg", Username = "plovdiv.drama", First = "Боряна", Last = "Иванова",
                    OrgName = "Plovdiv Drama Stage", Phone = "+359 88 339 4421", Web = "https://plovdivdrama.bg",
                    Company = "BG207009987",
                    Bio = "Независима театрална трупа с класически и съвременни постановки.",
                    Avatar = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "comedy.lab@demo.bg", Username = "comedy.lab", First = "Христо", Last = "Михайлов",
                    OrgName = "Comedy Lab BG", Phone = "+359 87 902 3318", Web = "https://comedylab.bg",
                    Company = "BG207118842",
                    Bio = "Стендъп вечери и open mic сцени за нови комици.",
                    Avatar = "https://images.unsplash.com/photo-1607746882042-944635dfe10e?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "pirin.folk@demo.bg", Username = "pirin.folk", First = "Снежана", Last = "Георгиева",
                    OrgName = "Pirin Folk Society", Phone = "+359 88 565 1102", Web = "https://pirinfolk.bg",
                    Company = "BG207220018",
                    Bio = "Пазители на пиринския фолклор — гайди, тамбури и хора.",
                    Avatar = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "tarnovo.sym@demo.bg", Username = "tarnovo.sym", First = "Любомир", Last = "Петров",
                    OrgName = "Tarnovo Symphonia", Phone = "+359 89 770 4423", Web = "https://tarnovosym.bg",
                    Company = "BG207331259",
                    Bio = "Камерен оркестър и солисти от Велико Търново.",
                    Avatar = "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "open.air.cinema@demo.bg", Username = "open.air.cinema", First = "Даниела", Last = "Стоева",
                    OrgName = "Open Air Pictures", Phone = "+359 88 220 9911", Web = "https://openairpictures.bg",
                    Company = "BG207448820",
                    Bio = "Кино под звездите — летни прожекции в паркове и площади.",
                    Avatar = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "devconf.sofia@demo.bg", Username = "devconf.sofia", First = "Калоян", Last = "Атанасов",
                    OrgName = "DevConf Sofia", Phone = "+359 88 660 7711", Web = "https://devconf.bg",
                    Company = "BG207559113",
                    Bio = "Технически конференции и митъпи за разработчици.",
                    Avatar = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "yoga.soul@demo.bg", Username = "yoga.soul", First = "Анелия", Last = "Илиева",
                    OrgName = "Yoga & Soul Black Sea", Phone = "+359 87 778 2233", Web = "https://yogasoul.bg",
                    Company = "BG207662229",
                    Bio = "Йога ретрийти и работилници по Черноморското крайбрежие.",
                    Avatar = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "bansko.snow@demo.bg", Username = "bansko.snow", First = "Мартин", Last = "Владимиров",
                    OrgName = "Bansko Snow Sessions", Phone = "+359 88 990 1102", Web = "https://banskosnow.bg",
                    Company = "BG207771045",
                    Bio = "Зимни апре-ски партита и open-air концерти в Банско.",
                    Avatar = "https://images.unsplash.com/photo-1628157588553-5eeea00af15c?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "vinyl.society@demo.bg", Username = "vinyl.society", First = "Теодора", Last = "Христова",
                    OrgName = "Vinyl Society", Phone = "+359 89 401 8821", Web = "https://vinylsociety.bg",
                    Company = "BG207884416",
                    Bio = "Хип-хоп вечери от винил, listening sessions и записи на живо.",
                    Avatar = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "rhodope.folk@demo.bg", Username = "rhodope.folk", First = "Боян", Last = "Асенов",
                    OrgName = "Rhodopes Folk Heritage", Phone = "+359 88 117 5544", Web = "https://rhodopefolk.bg",
                    Company = "BG207991177",
                    Bio = "Каба гайди и родопски песни — концерти и работилници.",
                    Avatar = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "standup.hub@demo.bg", Username = "standup.hub", First = "Ралица", Last = "Дамянова",
                    OrgName = "Sofia Stand-up Hub", Phone = "+359 87 332 4498", Web = "https://standuphub.bg",
                    Company = "BG208104451",
                    Bio = "Резидентска сцена за стендъп комедия в София.",
                    Avatar = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "pleven.sport@demo.bg", Username = "pleven.sport", First = "Веселин", Last = "Йотов",
                    OrgName = "Stadion Pleven Sports", Phone = "+359 88 663 7720", Web = "https://stadionpleven.bg",
                    Company = "BG208217734",
                    Bio = "Любителски спортни събития, маратони и турнири.",
                    Avatar = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=400&q=80",
                },
                new
                {
                    Email = "artspace.varna@demo.bg", Username = "artspace.varna", First = "Калина", Last = "Русева",
                    OrgName = "ArtSpace Varna", Phone = "+359 89 552 6611", Web = "https://artspacevarna.bg",
                    Company = "BG208326690",
                    Bio = "Галерийно пространство със съвременно българско изкуство.",
                    Avatar = "https://images.unsplash.com/photo-1607746882042-944635dfe10e?auto=format&fit=crop&w=400&q=80",
                },
            };

            var orgUsers = new List<ApplicationUser>();
            foreach (var o in organizers)
            {
                var u = new ApplicationUser
                {
                    UserName = o.Username,
                    Email = o.Email,
                    EmailConfirmed = true,
                    FirstName = o.First,
                    LastName = o.Last,
                    Bio = o.Bio,
                    ProfileImageUrl = o.Avatar,
                    CreatedAt = now.AddMonths(-5),
                };
                var res = await userManager.CreateAsync(u, DemoPassword);
                if (!res.Succeeded) continue;

                await userManager.AddToRoleAsync(u, GlobalConstants.Roles.Organizer);
                db.OrganizerData.Add(new OrganizerData
                {
                    OrganizerId = u.Id,
                    OrganizationName = o.OrgName,
                    Description = o.Bio,
                    PhoneNumber = o.Phone,
                    Website = o.Web,
                    CompanyNumber = o.Company,
                    Approved = true,
                    CreatedAt = now.AddMonths(-5),
                });
                orgUsers.Add(u);
            }

            // 5 extra regular users so engagement feels denser
            var extraUsers = new[]
            {
                new { Email = "elena.k@demo.bg", Username = "elena.k", First = "Елена", Last = "Кирилова",
                    Bio = "Театър, кино, дълги вечери в Капана.",
                    Avatar = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&w=400&q=80" },
                new { Email = "dimitar.p@demo.bg", Username = "dimitar.p", First = "Димитър", Last = "Павлов",
                    Bio = "Метълхед от Пловдив. Винаги готов за пит.",
                    Avatar = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=400&q=80" },
                new { Email = "boryana.s@demo.bg", Username = "boryana.s", First = "Боряна", Last = "Симеонова",
                    Bio = "Йога, изгреви, тиха музика.",
                    Avatar = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?auto=format&fit=crop&w=400&q=80" },
                new { Email = "kaloyan.m@demo.bg", Username = "kaloyan.m", First = "Калоян", Last = "Манолов",
                    Bio = ".NET разработчик, любител на конференции и крафтова бира.",
                    Avatar = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=400&q=80" },
                new { Email = "rosi.a@demo.bg", Username = "rosi.a", First = "Росица", Last = "Ангелова",
                    Bio = "Снимам концерти и фотографирам улицата.",
                    Avatar = "https://images.unsplash.com/photo-1607746882042-944635dfe10e?auto=format&fit=crop&w=400&q=80" },
            };

            foreach (var u in extraUsers)
            {
                var au = new ApplicationUser
                {
                    UserName = u.Username,
                    Email = u.Email,
                    EmailConfirmed = true,
                    FirstName = u.First,
                    LastName = u.Last,
                    Bio = u.Bio,
                    ProfileImageUrl = u.Avatar,
                    CreatedAt = now.AddMonths(-2),
                };
                var res = await userManager.CreateAsync(au, DemoPassword);
                if (!res.Succeeded) continue;
                await userManager.AddToRoleAsync(au, GlobalConstants.Roles.User);
            }

            await db.SaveChangesAsync();

            // All regular users (existing base + new) for likes/comments
            var allRegularUsers = (await userManager.GetUsersInRoleAsync(GlobalConstants.Roles.User)).ToList();

            // Helper to look up an organizer by org slug (Username)
            ApplicationUser Org(string username) => orgUsers.First(o => o.UserName == username);

            // 24 events spread across genres, cities, and time
            var eventsSpec = new[]
            {
                new
                {
                    Org = Org("rock.republic"),
                    Title = "Rock the Capital 2026",
                    Description = "Цяла вечер с три български рок групи на сцената на Mixtape 5. Гайдата на Сашо Дончев в края — изненадата на вечерта.",
                    City = "София", Address = "Mixtape 5, ул. Шейново 7",
                    Lat = 42.6957, Lng = 23.3338,
                    Genre = EventGenre.Rock,
                    Image = "https://images.unsplash.com/photo-1493676304819-0d7a8d026dcf?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 18, DurationHours = 5, HasTickets = true,
                },
                new
                {
                    Org = Org("rock.republic"),
                    Title = "Студентски рок маратон",
                    Description = "Шест локални групи, безплатен вход, отворена сцена за дебютанти. Вход само със студентска карта.",
                    City = "София", Address = "Студентски град, бл. 8",
                    Lat = 42.6500, Lng = 23.3450,
                    Genre = EventGenre.Rock,
                    Image = "https://images.unsplash.com/photo-1459749411175-04bf5292ceea?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 32, DurationHours = 6, HasTickets = false,
                },
                new
                {
                    Org = Org("metal.heart"),
                    Title = "Bulgaria Metal Fest",
                    Description = "Двудневен метъл фестивал — 12 групи, 2 сцени, мош яма, food trucks и метъл базар. Хедлайнер от Финландия.",
                    City = "Пловдив", Address = "Лятно кино Капана",
                    Lat = 42.1495, Lng = 24.7530,
                    Genre = EventGenre.Metal,
                    Image = "https://images.unsplash.com/photo-1571266028243-d220bc6e3e3e?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 48, DurationHours = 30, HasTickets = true,
                },
                new
                {
                    Org = Org("metal.heart"),
                    Title = "Doom Night Vol. 3",
                    Description = "Бавен, тежък, мрачен doom метъл от три балкански групи. Свещи, пушек и един усилвател повече, отколкото е законно.",
                    City = "София", Address = "Club Live & Loud, ул. Михай Еминеску 25",
                    Lat = 42.6886, Lng = 23.3372,
                    Genre = EventGenre.Metal,
                    Image = "https://images.unsplash.com/photo-1571266028243-d220bc6e3e3e?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 11, DurationHours = 5, HasTickets = true,
                },
                new
                {
                    Org = Org("plovdiv.drama"),
                    Title = "„Хъшове“ — премиера",
                    Description = "Нова постановка по текст на Иван Вазов в режисьорска интерпретация на Боряна Иванова. Премиерата е с официална вечеря след спектакъла.",
                    City = "Пловдив", Address = "ДТ „Н. О. Масалитинов“, ул. Княз Александър I 38",
                    Lat = 42.1458, Lng = 24.7497,
                    Genre = EventGenre.Theater,
                    Image = "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 25, DurationHours = 3, HasTickets = true,
                },
                new
                {
                    Org = Org("plovdiv.drama"),
                    Title = "„Чичовци“ на открито",
                    Description = "Класиката на Вазов в открита постановка на Античния театър. Лимитиран брой места — взимайте билети рано.",
                    City = "Пловдив", Address = "Античен театър, Стария град",
                    Lat = 42.1466, Lng = 24.7510,
                    Genre = EventGenre.Theater,
                    Image = "https://images.unsplash.com/photo-1429962714451-bb934ecdc4ec?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 60, DurationHours = 3, HasTickets = true,
                },
                new
                {
                    Org = Org("comedy.lab"),
                    Title = "Open Mic: Четвъртък вечер",
                    Description = "Седмична open mic сцена. Записването на сцената е от 19:30, започваме точно в 20:00. Вход с консумация.",
                    City = "София", Address = "Comedy Club, ул. Гладстон 31",
                    Lat = 42.6950, Lng = 23.3210,
                    Genre = EventGenre.Standup,
                    Image = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 3, DurationHours = 2, HasTickets = false,
                },
                new
                {
                    Org = Org("comedy.lab"),
                    Title = "Standup Showcase: Best of 2025",
                    Description = "Шест от най-силните български комици за тази година — една вечер, една сцена, без снимане.",
                    City = "Пловдив", Address = "Кино „Космос“, бул. Шести септември 132",
                    Lat = 42.1420, Lng = 24.7480,
                    Genre = EventGenre.Standup,
                    Image = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 22, DurationHours = 3, HasTickets = true,
                },
                new
                {
                    Org = Org("pirin.folk"),
                    Title = "Пирин Фолк Гала",
                    Description = "Гала концерт с гайдари, певци и хора от Пиринския край. Отворен фолклорен пазар преди концерта.",
                    City = "Сандански", Address = "Античен форум, гр. Сандански",
                    Lat = 41.5667, Lng = 23.2833,
                    Genre = EventGenre.Folk,
                    Image = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 40, DurationHours = 6, HasTickets = true,
                },
                new
                {
                    Org = Org("tarnovo.sym"),
                    Title = "Brahms & Vivaldi Night",
                    Description = "Камерен оркестър на свещи в крепостта Царевец. Програмата включва „Четирите годишни времена“ и Брамс op. 25.",
                    City = "Велико Търново", Address = "Крепост Царевец",
                    Lat = 43.0830, Lng = 25.6470,
                    Genre = EventGenre.Classical,
                    Image = "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 19, DurationHours = 2, HasTickets = true,
                },
                new
                {
                    Org = Org("open.air.cinema"),
                    Title = "Open Air Cinema: Cult Classics",
                    Description = "Прожекция на „Pulp Fiction“ под звездите. Носите одеяла, ние осигуряваме пуканки и горещ чай.",
                    City = "Пловдив", Address = "Парк „Цар Симеон“",
                    Lat = 42.1430, Lng = 24.7415,
                    Genre = EventGenre.Other,
                    Image = "https://images.unsplash.com/photo-1429962714451-bb934ecdc4ec?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 26, DurationHours = 3, HasTickets = true,
                },
                new
                {
                    Org = Org("open.air.cinema"),
                    Title = "Филмова вечер в парка",
                    Description = "Безплатна прожекция на български късометражен филм + Q&A с режисьора.",
                    City = "София", Address = "Южен парк, поляната до езерото",
                    Lat = 42.6735, Lng = 23.3120,
                    Genre = EventGenre.Other,
                    Image = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = -7, DurationHours = 3, HasTickets = false,
                },
                new
                {
                    Org = Org("devconf.sofia"),
                    Title = "DevConf Sofia 2026",
                    Description = "Двудневна конференция за разработчици — 30 лектори, 4 трака, открит хакатон в неделя. Кафенето в lobby е отворено цяло време.",
                    City = "София", Address = "Sofia Tech Park, бул. Цариградско шосе 111",
                    Lat = 42.6620, Lng = 23.3760,
                    Genre = EventGenre.Conference,
                    Image = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 55, DurationHours = 30, HasTickets = true,
                },
                new
                {
                    Org = Org("devconf.sofia"),
                    Title = "AI Builders Meetup #4",
                    Description = "Lightning talks по приложен AI — RAG, агенти, наблюдаемост. Свободни места, регистрация задължителна.",
                    City = "София", Address = "Puzl CowOrKing, ул. Сребърна 16",
                    Lat = 42.6680, Lng = 23.3151,
                    Genre = EventGenre.Conference,
                    Image = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 8, DurationHours = 3, HasTickets = false,
                },
                new
                {
                    Org = Org("yoga.soul"),
                    Title = "Sunrise Yoga Retreat",
                    Description = "Тридневен ретрийт на брега край Созопол. Йога, медитация, плуване и здравословно меню.",
                    City = "Созопол", Address = "къмпинг „Веселие“, северен бряг",
                    Lat = 42.4180, Lng = 27.6960,
                    Genre = EventGenre.Workshop,
                    Image = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 70, DurationHours = 60, HasTickets = true,
                },
                new
                {
                    Org = Org("bansko.snow"),
                    Title = "Apres-Ski Festival",
                    Description = "Шест бара, четири DJ-я, един стенд на сцена в подножието на пистата. Започва веднага след затварянето на лифтовете.",
                    City = "Банско", Address = "Долна станция Гондола",
                    Lat = 41.8313, Lng = 23.4880,
                    Genre = EventGenre.Festival,
                    Image = "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 90, DurationHours = 8, HasTickets = true,
                },
                new
                {
                    Org = Org("vinyl.society"),
                    Title = "Boom Bap Sundays",
                    Description = "Неделна вечер с класически hip-hop само от винил. Двама диджеи, ниско осветление, правило: без телефони на дансинга.",
                    City = "София", Address = "Bar 100 grams, ул. Парчевич 38",
                    Lat = 42.6929, Lng = 23.3158,
                    Genre = EventGenre.HipHop,
                    Image = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 5, DurationHours = 5, HasTickets = false,
                },
                new
                {
                    Org = Org("rhodope.folk"),
                    Title = "Каба гайди — концерт",
                    Description = "Десет гайдари от Родопите изпълняват традиционни песни в природен амфитеатър над Смолян.",
                    City = "Смолян", Address = "Природен амфитеатър „Невястата“",
                    Lat = 41.5774, Lng = 24.7027,
                    Genre = EventGenre.Folk,
                    Image = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 44, DurationHours = 3, HasTickets = true,
                },
                new
                {
                    Org = Org("standup.hub"),
                    Title = "Stand-up Hub: Best of 2025",
                    Description = "Финален шоукейс с топ комиците на Sofia Stand-up Hub за изминалата година. Записва се видео.",
                    City = "София", Address = "Joy Station, ул. Tsar Шишман 31",
                    Lat = 42.6912, Lng = 23.3293,
                    Genre = EventGenre.Standup,
                    Image = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 14, DurationHours = 3, HasTickets = true,
                },
                new
                {
                    Org = Org("pleven.sport"),
                    Title = "Pleven City Marathon",
                    Description = "Целогодишно очакван маратон с три дистанции — 5K, 10K и 21K. Стартова такса включва тениска и медал.",
                    City = "Плевен", Address = "Площад „Възраждане“",
                    Lat = 43.4170, Lng = 24.6170,
                    Genre = EventGenre.Sports,
                    Image = "https://images.unsplash.com/photo-1429962714451-bb934ecdc4ec?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 38, DurationHours = 6, HasTickets = true,
                },
                new
                {
                    Org = Org("artspace.varna"),
                    Title = "Modern Bulgarian Art Expo",
                    Description = "Колективна изложба на 14 съвременни български художници. Откриване с вино и куратор обиколка.",
                    City = "Варна", Address = "Морско казино, галерия 2",
                    Lat = 43.2070, Lng = 27.9220,
                    Genre = EventGenre.Exhibition,
                    Image = "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 6, DurationHours = 5, HasTickets = false,
                },
                new
                {
                    Org = Org("artspace.varna"),
                    Title = "Photographers Collective Vol. 2",
                    Description = "Изложба и аукцион на работите на 8 български фотографа. Приходите подкрепят младежки фото-обмени.",
                    City = "Варна", Address = "ArtSpace, ул. Преслав 13",
                    Lat = 43.2055, Lng = 27.9170,
                    Genre = EventGenre.Exhibition,
                    Image = "https://images.unsplash.com/photo-1516280440614-37939bbacd81?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = -14, DurationHours = 4, HasTickets = false,
                },
                new
                {
                    Org = Org("rock.republic"),
                    Title = "Балкан Алтернатива",
                    Description = "Три алтернативни рок групи от България, Сърбия и Северна Македония. Поне една ще ви стане любима до края на вечерта.",
                    City = "София", Address = "Toba & Co, бул. Васил Левски 100",
                    Lat = 42.6985, Lng = 23.3375,
                    Genre = EventGenre.Rock,
                    Image = "https://images.unsplash.com/photo-1571266028243-d220bc6e3e3e?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = 17, DurationHours = 5, HasTickets = true,
                },
                new
                {
                    Org = Org("comedy.lab"),
                    Title = "Шегата е сериозна работа",
                    Description = "Дебатна вечер: четирима стендъп комици срещу четирима философи. Темата се избира от публиката.",
                    City = "София", Address = "Sofia Live Club, НДК",
                    Lat = 42.6850, Lng = 23.3185,
                    Genre = EventGenre.Standup,
                    Image = "https://images.unsplash.com/photo-1607746882042-944635dfe10e?auto=format&fit=crop&w=1200&q=80",
                    StartOffsetDays = -3, DurationHours = 3, HasTickets = false,
                },
            };

            var createdEvents = new List<Event>();
            foreach (var e in eventsSpec)
            {
                var start = now.AddDays(e.StartOffsetDays);
                var ev = new Event
                {
                    OrganizerId = e.Org.Id,
                    Title = e.Title,
                    Description = e.Description,
                    City = e.City,
                    Address = e.Address,
                    Latitude = e.Lat,
                    Longitude = e.Lng,
                    Genre = e.Genre,
                    ImageUrl = e.Image,
                    StartTime = start,
                    EndTime = start.AddHours(e.DurationHours),
                    IsApproved = true,
                    CreatedAt = now.AddDays(-Math.Abs(e.StartOffsetDays) - 5),
                };
                db.Events.Add(ev);
                createdEvents.Add(ev);
            }
            await db.SaveChangesAsync();

            // Add 1-3 gallery images per event so the detail page has more visuals
            var galleryPool = new[]
            {
                "https://images.unsplash.com/photo-1571266028243-d220bc6e3e3e?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1429962714451-bb934ecdc4ec?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1459749411175-04bf5292ceea?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1516280440614-37939bbacd81?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?auto=format&fit=crop&w=1200&q=80",
                "https://images.unsplash.com/photo-1493676304819-0d7a8d026dcf?auto=format&fit=crop&w=1200&q=80",
            };

            foreach (var ev in createdEvents)
            {
                var imgs = galleryPool.OrderBy(_ => rnd.Next()).Take(rnd.Next(1, 4)).ToList();
                foreach (var img in imgs)
                {
                    db.EventImages.Add(new EventImage { EventId = ev.Id, ImageUrl = img });
                }
            }
            await db.SaveChangesAsync();

            // Tickets — selected events get tiered pricing
            var ticketsByEvent = new Dictionary<int, List<Ticket>>();
            for (int i = 0; i < eventsSpec.Length; i++)
            {
                if (!eventsSpec[i].HasTickets) continue;
                var ev = createdEvents[i];

                var ticketSet = ev.Genre switch
                {
                    EventGenre.Festival => new[]
                    {
                        ("Earlybird Pass", "Ранни птици — лимит 80 бр.", 79m, 80, 38),
                        ("Standard Pass", "Пълен достъп до фестивала.", 119m, 350, 184),
                        ("VIP Pass", "VIP зона + обозначен паркинг.", 219m, 50, 12),
                    },
                    EventGenre.Conference => new[]
                    {
                        ("Standard", "Достъп до всички сесии и кафе пауза.", 149m, 300, 162),
                        ("Speaker Lounge", "Standard + достъп до speaker lounge.", 249m, 80, 22),
                        ("Studio Pass", "Включва workshop в неделя.", 349m, 40, 9),
                    },
                    EventGenre.Theater => new[]
                    {
                        ("Партер", "Седящи места партер.", 35m, 120, 64),
                        ("Балкон", "Седящи места балкон.", 22m, 80, 25),
                    },
                    EventGenre.Classical => new[]
                    {
                        ("Партер", "Седящи места партер.", 50m, 120, 51),
                        ("Балкон", "Седящи места балкон.", 30m, 80, 22),
                    },
                    EventGenre.Standup => new[]
                    {
                        ("Стандартен билет", "Свободни места.", 25m, 120, 71),
                        ("Първи редове", "Гарантирано в първите три реда.", 45m, 30, 11),
                    },
                    EventGenre.Sports => new[]
                    {
                        ("5K", "Стартова такса 5K.", 18m, 400, 224),
                        ("10K", "Стартова такса 10K.", 28m, 350, 168),
                        ("Half-Marathon", "Стартова такса 21K.", 42m, 250, 96),
                    },
                    EventGenre.Workshop => new[]
                    {
                        ("Single", "Едно легло, общи занимания.", 320m, 30, 18),
                        ("Двойна стая", "Двойна стая за двама.", 540m, 20, 11),
                    },
                    EventGenre.Folk => new[]
                    {
                        ("Стандартен билет", "Седящи места.", 22m, 200, 86),
                    },
                    EventGenre.Rock or EventGenre.Metal => new[]
                    {
                        ("Ранна предпродажба", "Ограничено количество.", 30m, 80, 51),
                        ("Стандартен билет", "Стандартен вход.", 40m, 200, 84),
                    },
                    EventGenre.HipHop => new[]
                    {
                        ("Standard", "Свободни места.", 25m, 100, 53),
                    },
                    _ => new[]
                    {
                        ("Standard", "Стандартен вход.", 25m, 150, 64),
                    },
                };

                var list = new List<Ticket>();
                foreach (var (name, desc, price, total, sold) in ticketSet)
                {
                    var tk = new Ticket
                    {
                        EventId = ev.Id,
                        Name = name,
                        Description = desc,
                        Price = price,
                        QuantityTotal = total,
                        QuantityRemaining = total - sold,
                        IsActive = true,
                        CreatedAt = now.AddDays(-30),
                    };
                    db.Tickets.Add(tk);
                    list.Add(tk);
                }
                ticketsByEvent[ev.Id] = list;
            }
            await db.SaveChangesAsync();

            // Generate purchases (Paid transactions)
            if (allRegularUsers.Count > 0)
            {
                foreach (var (evId, ticketList) in ticketsByEvent)
                {
                    foreach (var tk in ticketList)
                    {
                        var soldCount = tk.QuantityTotal - tk.QuantityRemaining;
                        soldCount = Math.Min(soldCount, 25);
                        if (soldCount <= 0) continue;

                        for (int s = 0; s < soldCount; s++)
                        {
                            var buyer = allRegularUsers[rnd.Next(allRegularUsers.Count)];
                            var tx = new Transaction
                            {
                                UserId = buyer.Id,
                                TotalAmount = tk.Price,
                                Status = GlobalConstants.TransactionStatuses.Paid,
                                CreatedAt = now.AddDays(-rnd.Next(1, 28)).AddHours(-rnd.Next(0, 23)),
                            };
                            db.Transactions.Add(tx);

                            var ut = new UserTicket
                            {
                                TicketId = tk.Id,
                                Transaction = tx,
                                QrCode = Guid.NewGuid().ToString("N"),
                                IsUsed = rnd.NextDouble() < 0.18,
                                CreatedAt = tx.CreatedAt,
                            };
                            if (ut.IsUsed)
                            {
                                ut.UsedAt = tx.CreatedAt.AddHours(rnd.Next(1, 48));
                                ut.UsedByOrganizerId = createdEvents.First(e => e.Id == evId).OrganizerId;
                            }
                            db.UserTickets.Add(ut);
                        }
                    }
                }
                await db.SaveChangesAsync();
            }

            // Posts referencing some events plus standalone announcements
            var postSpecs = new (ApplicationUser Org, string Content, int? EventIdx, string[] Images)[]
            {
                (Org("rock.republic"), "Lineup-ът за Rock the Capital е почти финализиран. Очаквайте обявата следващата сряда 🤘", 0,
                    new[] { "https://images.unsplash.com/photo-1493676304819-0d7a8d026dcf?auto=format&fit=crop&w=1000&q=80" }),
                (Org("metal.heart"), "Bulgaria Metal Fest — продаваме под 60% от билетите. Ако чакахте — спирайте да чакате.", 2,
                    new[] {
                        "https://images.unsplash.com/photo-1571266028243-d220bc6e3e3e?auto=format&fit=crop&w=1000&q=80",
                        "https://images.unsplash.com/photo-1459749411175-04bf5292ceea?auto=format&fit=crop&w=1000&q=80",
                    }),
                (Org("plovdiv.drama"), "Премиерата на „Хъшове“ е след 25 дни. Премиерният костюм е готов — снимка скоро.", 4,
                    new[] { "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?auto=format&fit=crop&w=1000&q=80" }),
                (Org("comedy.lab"), "Open Mic тази седмица има 14 записани комици — ще е дълга вечер. Бирата е студена, нервите — горещи.", 6,
                    Array.Empty<string>()),
                (Org("pirin.folk"), "Снимки от подготовката на гайдарите за Пирин Фолк Гала.", 8,
                    new[] { "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=1000&q=80" }),
                (Org("tarnovo.sym"), "Brahms на Царевец — една вечер, ограничени 200 места. Билети в линка.", 9,
                    new[] { "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?auto=format&fit=crop&w=1000&q=80" }),
                (Org("open.air.cinema"), "Тази година избрахме 'Pulp Fiction'. Не питайте защо. 🍿", 10,
                    new[] { "https://images.unsplash.com/photo-1429962714451-bb934ecdc4ec?auto=format&fit=crop&w=1000&q=80" }),
                (Org("devconf.sofia"), "Лекторите за DevConf 2026 са обявени. 30 имена, 4 трака — програмата е в сайта.", 12,
                    new[] { "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=1000&q=80" }),
                (Org("yoga.soul"), "Sunrise Retreat в Созопол — остават 6 места. Ела с приятелка и отстъпката е 15%.", 14,
                    new[] { "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=1000&q=80" }),
                (Org("bansko.snow"), "Apres-Ski Festival lineup teaser — следващата седмица го пускаме целия.", 15,
                    new[] { "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?auto=format&fit=crop&w=1000&q=80" }),
                (Org("vinyl.society"), "Boom Bap Sundays се връщат. Носим '93-'97 кутиите.", 16,
                    new[] { "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?auto=format&fit=crop&w=1000&q=80" }),
                (Org("rhodope.folk"), "Каба гайдите се събират. Десет души, десет инструмента, една родопска вечер.", 17,
                    new[] { "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=1000&q=80" }),
                (Org("standup.hub"), "Best of 2025 — гласувайте кой да открие вечерта. Анкета в Stories.", 18,
                    Array.Empty<string>()),
                (Org("pleven.sport"), "Регистрацията за Pleven Marathon е отворена. Тениските са готови, пътят — затворен за деня.", 19,
                    new[] { "https://images.unsplash.com/photo-1429962714451-bb934ecdc4ec?auto=format&fit=crop&w=1000&q=80" }),
                (Org("artspace.varna"), "Откриването на Modern Bulgarian Art Expo е след 6 дни. 14 художници, една вечер. Caterer — Tres Bistro.", 20,
                    new[] { "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?auto=format&fit=crop&w=1000&q=80" }),
                (Org("rock.republic"), "Търсим още един саундмен за есенния сезон. DM с CV и ретенция за GIG-овете през септември.", null,
                    Array.Empty<string>()),
                (Org("comedy.lab"), "Кой би искал спецификален Late Night шоу формат от 23:00? Реагирайте, ако да.", null,
                    Array.Empty<string>()),
            };

            var createdPosts = new List<Post>();
            foreach (var p in postSpecs)
            {
                var post = new Post
                {
                    OrganizerId = p.Org.Id,
                    EventId = p.EventIdx.HasValue ? createdEvents[p.EventIdx.Value].Id : null,
                    Content = p.Content,
                    CreatedAt = now.AddDays(-rnd.Next(1, 25)).AddHours(-rnd.Next(0, 23)),
                };
                db.Posts.Add(post);
                createdPosts.Add(post);
            }
            await db.SaveChangesAsync();

            for (int i = 0; i < postSpecs.Length; i++)
            {
                foreach (var img in postSpecs[i].Images)
                {
                    db.PostImages.Add(new PostImage
                    {
                        PostId = createdPosts[i].Id,
                        ImageUrl = img,
                        MediaType = PostMediaType.Image,
                    });
                }
            }
            await db.SaveChangesAsync();

            if (allRegularUsers.Count == 0) return;

            var eventComments = new[]
            {
                "Идвам! Кой иска да се събере преди?",
                "Last edition was insane 🔥",
                "Има ли паркинг наблизо?",
                "Точно това ми трябваше за уикенда.",
                "Препоръчвам! Бях преди и звукът беше топ.",
                "Ще има ли late check-in?",
                "Колко време продължава първият сет?",
                "Кога излиза програмата?",
                "Студентска отстъпка има ли?",
                "Долетявам от Варна заради това.",
            };

            foreach (var ev in createdEvents)
            {
                var likers = allRegularUsers.OrderBy(_ => rnd.Next())
                    .Take(rnd.Next(2, Math.Min(allRegularUsers.Count, 8) + 1)).ToList();
                foreach (var u in likers)
                {
                    db.EventLikes.Add(new EventLike
                    {
                        EventId = ev.Id,
                        UserId = u.Id,
                        CreatedAt = now.AddDays(-rnd.Next(1, 30)),
                    });
                }

                var commenters = allRegularUsers.OrderBy(_ => rnd.Next()).Take(rnd.Next(1, 4)).ToList();
                foreach (var u in commenters)
                {
                    db.EventComments.Add(new EventComment
                    {
                        EventId = ev.Id,
                        UserId = u.Id,
                        Content = eventComments[rnd.Next(eventComments.Length)],
                        CreatedAt = now.AddDays(-rnd.Next(1, 25)).AddHours(-rnd.Next(0, 23)),
                    });
                }
            }

            var postComments = new[]
            {
                "❤️",
                "Идваме с приятели!",
                "Звучи страхотно, чакаме повече инфо.",
                "Кой ще е headliner-ът?",
                "Бях на миналата вечер — топ беше.",
                "Можем ли да си купим билет на входа?",
                "Кога има отстъпка за студенти?",
                "Любим организатор 🙌",
                "Първи път идвам — ще ме харесате 😄",
            };

            foreach (var post in createdPosts)
            {
                var likers = allRegularUsers.OrderBy(_ => rnd.Next())
                    .Take(rnd.Next(1, Math.Min(allRegularUsers.Count, 6) + 1)).ToList();
                foreach (var u in likers)
                {
                    db.PostLikes.Add(new PostLike
                    {
                        PostId = post.Id,
                        UserId = u.Id,
                        CreatedAt = now.AddDays(-rnd.Next(1, 20)),
                    });
                }

                var commenters = allRegularUsers.OrderBy(_ => rnd.Next()).Take(rnd.Next(0, 3)).ToList();
                foreach (var u in commenters)
                {
                    db.PostComments.Add(new PostComment
                    {
                        PostId = post.Id,
                        UserId = u.Id,
                        Content = postComments[rnd.Next(postComments.Length)],
                        CreatedAt = post.CreatedAt.AddHours(rnd.Next(1, 60)),
                    });
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
