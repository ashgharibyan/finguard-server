using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using finguard_server.Models;

namespace finguard_server.Data
{
      public class AppDbContext : IdentityDbContext<ApplicationUser>
      {
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public DbSet<Expense> Expenses { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                  base.OnModelCreating(modelBuilder); // This is important for Identity tables!

                  // Configuring the Expense entity
                  modelBuilder.Entity<Expense>(entity =>
                  {
                        entity.HasKey(e => e.Id);

                        entity.Property(e => e.Description)
                        .IsRequired()
                        .HasMaxLength(200);

                        entity.Property(e => e.Amount)
                        .IsRequired()
                        .HasColumnType("decimal(18,2)");

                        entity.Property(e => e.Date)
                        .IsRequired();

                        // New properties for tracking who created the expense
                        entity.Property(e => e.CreatedById)
                        .IsRequired()
                        .HasMaxLength(450); // This matches Identity's default key length

                        entity.Property(e => e.CreatedByEmail)
                        .IsRequired()
                        .HasMaxLength(256); // This matches Identity's default email length

                        entity.Property(e => e.CreatedAt)
                        .IsRequired();
                  });
            }
      }
}