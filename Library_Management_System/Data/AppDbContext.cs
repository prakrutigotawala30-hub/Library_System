using Library_Management_System.Models;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Library_Management_System.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Event> Events { get; set; }

        // Tables owned by Admin app but read by user-side features
        // (Catalog browsing, Member dashboard). Both apps point at the
        // same database; these DbSets expose existing tables for queries.
        public DbSet<Member> Members { get; set; }
        public DbSet<BorrowRecord> BorrowRecords { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Match admin app's column types for the shared decimal fields.
            // Without these, EF logs warnings on startup and the schema it
            // expects diverges from the schema admin's migrations created.
            modelBuilder.Entity<BorrowRecord>()
                .Property(b => b.FineAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<BorrowRecord>()
                .Property(b => b.FinePerDay)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Membership>()
                .Property(m => m.Fee)
                .HasColumnType("decimal(18,2)");
        }
    }
}