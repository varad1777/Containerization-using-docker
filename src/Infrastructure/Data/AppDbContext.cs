
// Data/AppDbContext.cs
// the above package is required for the Identity Db context

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Entities;

namespace MyApp.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    // instead of DB context we use the IndentityDbContext
    // it uses the DbContext + Identity logic also 
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Signal> Signals { get; set; }
        public DbSet<RefreshTokens> RefreshTokens { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // making relations by taking the assets as 
            modelBuilder.Entity<Asset>()
                .HasMany(a => a.Signals) // assets has many signals 
                .WithOne(s => s.Asset) // with one assets -> means one to many relationship 
                .HasForeignKey(s => s.AssetId) // forign key is the AssetId
                .OnDelete(DeleteBehavior.Cascade); // on delete cascade 

            modelBuilder.Entity<Asset>()
                  .HasOne(a => a.User) // asset has one user
                  .WithMany() // user can have the many assets
                  .HasForeignKey(a => a.UserId);

            base.OnModelCreating(modelBuilder);
        }
    }
}

// db context has the virtual methods named onModelCreating 
// allow you to configure model 
//used for 
// table relationships 
// constrints
// cascade deletes 
//indexes 

// so basically here 
// hasMany withOne links them together 

