using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using nowClock.Infrastructure.Identity;

namespace nowClock.Infrastructure.Data.Auth
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.Cedula).HasMaxLength(30).IsRequired();
                entity.HasIndex(u => u.Cedula).IsUnique();
            });
        }
    }
}
