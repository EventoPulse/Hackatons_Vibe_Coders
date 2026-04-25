using EventsApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventsApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<OrganizerData> OrganizerData { get; set; } = null!;

        public DbSet<Event> Events { get; set; } = null!;

        public DbSet<Post> Posts { get; set; } = null!;

        public DbSet<PostImage> PostImages { get; set; } = null!;

        public DbSet<PostComment> PostComments { get; set; } = null!;

        public DbSet<PostLike> PostLikes { get; set; } = null!;

        public DbSet<EventComment> EventComments { get; set; } = null!;

        public DbSet<EventLike> EventLikes { get; set; } = null!;

        public DbSet<EventImage> EventImages { get; set; } = null!;

        public DbSet<UserPreferences> UserPreferences { get; set; } = null!;

        public DbSet<Ticket> Tickets { get; set; } = null!;

        public DbSet<Transaction> Transactions { get; set; } = null!;

        public DbSet<UserTicket> UserTickets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<OrganizerData>(entity =>
            {
                entity.HasKey(o => o.OrganizerId);

                entity.HasOne(o => o.Organizer)
                      .WithOne(u => u.OrganizerData)
                      .HasForeignKey<OrganizerData>(o => o.OrganizerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserPreferences>(entity =>
            {
                entity.HasOne(p => p.User)
                      .WithOne(u => u.UserPreferences)
                      .HasForeignKey<UserPreferences>(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.UserId).IsUnique();
            });

            builder.Entity<Event>(entity =>
            {
                entity.HasOne(e => e.Organizer)
                      .WithMany(u => u.Events)
                      .HasForeignKey(e => e.OrganizerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Post>(entity =>
            {
                entity.HasOne(p => p.Organizer)
                      .WithMany(u => u.Posts)
                      .HasForeignKey(p => p.OrganizerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Event)
                      .WithMany(e => e.Posts)
                      .HasForeignKey(p => p.EventId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<PostImage>(entity =>
            {
                entity.HasOne(pi => pi.Post)
                      .WithMany(p => p.Images)
                      .HasForeignKey(pi => pi.PostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PostComment>(entity =>
            {
                entity.HasOne(pc => pc.Post)
                      .WithMany(p => p.Comments)
                      .HasForeignKey(pc => pc.PostId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pc => pc.User)
                      .WithMany(u => u.PostComments)
                      .HasForeignKey(pc => pc.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PostLike>(entity =>
            {
                entity.HasOne(pl => pl.Post)
                      .WithMany(p => p.Likes)
                      .HasForeignKey(pl => pl.PostId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pl => pl.User)
                      .WithMany(u => u.PostLikes)
                      .HasForeignKey(pl => pl.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(pl => new { pl.PostId, pl.UserId }).IsUnique();
            });

            builder.Entity<EventComment>(entity =>
            {
                entity.HasOne(ec => ec.Event)
                      .WithMany(e => e.Comments)
                      .HasForeignKey(ec => ec.EventId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ec => ec.User)
                      .WithMany(u => u.EventComments)
                      .HasForeignKey(ec => ec.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<EventLike>(entity =>
            {
                entity.HasOne(el => el.Event)
                      .WithMany(e => e.Likes)
                      .HasForeignKey(el => el.EventId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(el => el.User)
                      .WithMany(u => u.EventLikes)
                      .HasForeignKey(el => el.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(el => new { el.EventId, el.UserId }).IsUnique();
            });

            builder.Entity<EventImage>(entity =>
            {
                entity.HasOne(ei => ei.Event)
                      .WithMany(e => e.Images)
                      .HasForeignKey(ei => ei.EventId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Ticket>(entity =>
            {
                entity.HasOne(t => t.Event)
                      .WithMany(e => e.Tickets)
                      .HasForeignKey(t => t.EventId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Transaction>(entity =>
            {
                entity.HasOne(t => t.User)
                      .WithMany()
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<UserTicket>(entity =>
            {
                entity.HasOne(ut => ut.Ticket)
                      .WithMany(t => t.UserTickets)
                      .HasForeignKey(ut => ut.TicketId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ut => ut.Transaction)
                      .WithMany(t => t.UserTickets)
                      .HasForeignKey(ut => ut.TransactionId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ut => ut.UsedByOrganizer)
                      .WithMany()
                      .HasForeignKey(ut => ut.UsedByOrganizerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(ut => ut.QrCode).IsUnique();
            });
        }
    }
}
