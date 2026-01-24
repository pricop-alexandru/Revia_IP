using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Revia.Models;

namespace Revia.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }
        public DbSet<Owner> Owners { get; set; }
        public DbSet<LocalGuide> LocalGuides { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<OwnerRequest> OwnerRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<UserCoupon> UserCoupons { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Ex: One-to-One pentru Owner, LocalGuide, Admin cu ApplicationUser
            builder.Entity<Owner>()
            .HasOne(o => o.User)
            .WithOne(u => u.Owner)
            .HasForeignKey<Owner>(o => o.ApplicationUserId);
            builder.Entity<LocalGuide>()
            .HasOne(lg => lg.User)
            .WithOne(u => u.LocalGuide)
            .HasForeignKey<LocalGuide>(lg => lg.ApplicationUserId);
            builder.Entity<Admin>()
            .HasOne(a => a.User)
            .WithOne(u => u.Admin)
            .HasForeignKey<Admin>(a => a.ApplicationUserId);
            // OwnerRequest cu User
            builder.Entity<OwnerRequest>()
            .HasOne(or => or.User)
            .WithMany() // Un user poate avea multiple request-uri, dar doar unul activ
            .HasForeignKey(or => or.ApplicationUserId);
        }
    }
}