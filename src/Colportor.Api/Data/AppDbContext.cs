// src/Colportor.Api/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;

// Aliases para evitar colis�o com o namespace raiz "Colportor"
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

                // L�DER: v�nculo opcional com Region
                e.HasOne(x => x.Region)
                    .WithMany() // n�o depende de a Region ter cole��o
                    .HasForeignKey(x => x.RegionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ===== Colportors =====
            mb.Entity<ColpColportor>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.FullName).IsRequired();
                e.Property(x => x.CPF).IsRequired();
                e.HasIndex(x => x.CPF).IsUnique();
                // Se voc� j� adicionou RegionId em Colportor, pode mapear aqui.
                // (Deixei sem FK para n�o quebrar quem ainda usa string Region.)
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
                    .WithMany()      // n�o exigir cole��o em Country
                    .HasForeignKey(x => x.CountryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Countries =====
            mb.Entity<ColpCountry>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired();
                // Se existir Code/Iso2 no seu modelo, crie �ndice �nico. Caso n�o exista, remova.
                // e.HasIndex(x => x.Iso2).IsUnique();
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
                e.HasOne(x => x.Leader).WithMany().HasForeignKey(x => x.LeaderId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Colportor).WithMany(c => c.PacEnrollments).HasForeignKey(x => x.ColportorId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.ColportorId, x.StartDate, x.EndDate });
            });
        }
    }
}
