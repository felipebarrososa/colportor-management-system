// src/Colportor.Api/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;

// Aliases para evitar colisão com o namespace raiz "Colportor"
using ColpUser = Colportor.Api.Models.User;
using ColpColportor = Colportor.Api.Models.Colportor;
using ColpVisit = Colportor.Api.Models.Visit;
using ColpRegion = Colportor.Api.Models.Region;
using ColpCountry = Colportor.Api.Models.Country;
using ColpNotificationLog = Colportor.Api.Models.NotificationLog;

namespace Colportor.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

        public DbSet<ColpUser> Users => Set<ColpUser>();
        public DbSet<ColpColportor> Colportors => Set<ColpColportor>();
        public DbSet<ColpVisit> Visits => Set<ColpVisit>();
        public DbSet<ColpRegion> Regions => Set<ColpRegion>();
        public DbSet<ColpCountry> Countries => Set<ColpCountry>();
        public DbSet<ColpNotificationLog> NotificationLogs => Set<ColpNotificationLog>();
        public DbSet<Colportor.Api.Models.PacEnrollment> PacEnrollments => Set<Colportor.Api.Models.PacEnrollment>();
        public DbSet<Colportor.Api.Models.MissionContact> MissionContacts => Set<Colportor.Api.Models.MissionContact>();
        public DbSet<Colportor.Api.Models.ContactObservation> ContactObservations => Set<Colportor.Api.Models.ContactObservation>();
        public DbSet<Colportor.Api.Models.ContactStatusHistory> ContactStatusHistories => Set<Colportor.Api.Models.ContactStatusHistory>();
        public DbSet<Colportor.Api.Models.WhatsAppMessage> WhatsAppMessages => Set<Colportor.Api.Models.WhatsAppMessage>();
        public DbSet<Colportor.Api.Models.WhatsAppTemplate> WhatsAppTemplates => Set<Colportor.Api.Models.WhatsAppTemplate>();
        public DbSet<Colportor.Api.Models.WhatsAppConnection> WhatsAppConnections => Set<Colportor.Api.Models.WhatsAppConnection>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // ===== Users =====
            mb.Entity<ColpUser>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Email).IsRequired();
                e.Property(x => x.PasswordHash).IsRequired();
                e.Property(x => x.Role).IsRequired();
                e.HasIndex(x => x.Email).IsUnique();

                e.HasOne(x => x.Colportor)
                    .WithMany()
                    .HasForeignKey(x => x.ColportorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ===== Colportors =====
            mb.Entity<ColpColportor>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.FullName).IsRequired();
                e.Property(x => x.CPF).IsRequired();
                e.HasIndex(x => x.CPF).IsUnique();
                
                // Relacionamento explícito com Region
                e.HasOne(x => x.Region)
                    .WithMany(r => r.Colportors)
                    .HasForeignKey(x => x.RegionId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Relacionamento explícito com Leader (User)
                e.HasOne(x => x.Leader)
                    .WithMany()
                    .HasForeignKey(x => x.LeaderId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ===== Visits =====
            mb.Entity<ColpVisit>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(v => v.Colportor)
                    .WithMany(c => c.Visits)
                    .HasForeignKey(v => v.ColportorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Regions =====
            mb.Entity<ColpRegion>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired();
                e.HasIndex(x => new { x.CountryId, x.Name }).IsUnique();
                e.HasOne(x => x.Country)
                    .WithMany(c => c.Regions)
                    .HasForeignKey(x => x.CountryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Countries =====
            mb.Entity<ColpCountry>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired();
            });

            // ===== NotificationLogs =====
            mb.Entity<ColpNotificationLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).IsRequired();
                e.Property(x => x.Email).IsRequired();
            });

            // ===== PacEnrollments =====
            mb.Entity<Colportor.Api.Models.PacEnrollment>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Status).IsRequired();
                e.HasOne(x => x.Leader)
                    .WithMany()
                    .HasForeignKey(x => x.LeaderId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Colportor)
                    .WithMany(c => c.PacEnrollments)
                    .HasForeignKey(x => x.ColportorId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.ColportorId, x.StartDate, x.EndDate });
            });

            // ===== MissionContacts =====
            mb.Entity<Colportor.Api.Models.MissionContact>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.FullName).IsRequired();
                e.Property(x => x.Status).HasMaxLength(50).IsRequired();

                e.HasOne(x => x.Region)
                    .WithMany()
                    .HasForeignKey(x => x.RegionId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Leader)
                    .WithMany()
                    .HasForeignKey(x => x.LeaderId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.CreatedByColportor)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByColportorId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => x.RegionId);
                e.HasIndex(x => x.LeaderId);
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.CreatedAt);
            });

            // ===== ContactObservations =====
            mb.Entity<Colportor.Api.Models.ContactObservation>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).IsRequired().HasMaxLength(50);
                e.Property(x => x.Title).IsRequired().HasMaxLength(200);
                e.Property(x => x.Content).IsRequired();
                e.Property(x => x.Author).IsRequired().HasMaxLength(100);
                e.Property(x => x.CreatedAt).IsRequired();

                e.HasOne(x => x.MissionContact)
                    .WithMany()
                    .HasForeignKey(x => x.MissionContactId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.MissionContactId);
                e.HasIndex(x => x.CreatedAt);
            });

            // ===== ContactStatusHistories =====
            mb.Entity<Colportor.Api.Models.ContactStatusHistory>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.FromStatus).IsRequired().HasMaxLength(50);
                e.Property(x => x.ToStatus).IsRequired().HasMaxLength(50);
                e.Property(x => x.ChangedBy).HasMaxLength(100);
                e.Property(x => x.ChangedAt).IsRequired();
                e.Property(x => x.Notes).HasMaxLength(500);

                e.HasOne(x => x.MissionContact)
                    .WithMany()
                    .HasForeignKey(x => x.MissionContactId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.MissionContactId);
                e.HasIndex(x => x.ChangedAt);
            });

            // ===== WhatsAppMessages =====
            mb.Entity<Colportor.Api.Models.WhatsAppMessage>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Content).IsRequired().HasMaxLength(1000);
                e.Property(x => x.Sender).IsRequired().HasMaxLength(50);
                e.Property(x => x.Timestamp).IsRequired();
                e.Property(x => x.Status).IsRequired().HasMaxLength(20);
                e.Property(x => x.MediaUrl).HasMaxLength(500);
                e.Property(x => x.MediaType).HasMaxLength(50);
                e.Property(x => x.WhatsAppMessageId).HasMaxLength(100);

                e.HasOne(x => x.MissionContact)
                    .WithMany()
                    .HasForeignKey(x => x.MissionContactId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.SentByUser)
                    .WithMany()
                    .HasForeignKey(x => x.SentByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => x.MissionContactId);
                e.HasIndex(x => x.Timestamp);
                e.HasIndex(x => x.Status);
            });

            // ===== WhatsAppTemplates =====
            mb.Entity<Colportor.Api.Models.WhatsAppTemplate>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(100);
                e.Property(x => x.Content).IsRequired().HasMaxLength(1000);
                e.Property(x => x.Category).HasMaxLength(50);
                e.Property(x => x.IsActive).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
                e.Property(x => x.AvailableVariables).HasMaxLength(500);

                e.HasOne(x => x.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.IsActive);
                e.HasIndex(x => x.Category);
                e.HasIndex(x => x.CreatedAt);
            });

            // ===== WhatsAppConnections =====
            mb.Entity<Colportor.Api.Models.WhatsAppConnection>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
                e.Property(x => x.Status).IsRequired().HasMaxLength(50);
                e.Property(x => x.QrCode).HasMaxLength(500);
                e.Property(x => x.ErrorMessage).HasMaxLength(1000);
                e.Property(x => x.SessionData).HasMaxLength(100);
                e.Property(x => x.CreatedAt).IsRequired();

                e.HasOne(x => x.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.PhoneNumber);
                e.HasIndex(x => x.CreatedAt);
            });

        }
    }
}