using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options): base(options)
        {
            
        }

        public DbSet<Value> Values{ get; set; }
        public DbSet<User> Users{ get; set; }
        public DbSet<Photo> Photos{ get; set; }
        public DbSet<Like> Likes { get; set; }

        // configure the relation between Like and User
        // override the OnModelCreating
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // tells entity about our primary key
            builder.Entity<Like>()
                .HasKey(k => new {k.LikerId, k.LikeeId});
            
            // tells also about the relationship
            // each likers can have many likees 
            builder.Entity<Like>()
                .HasOne(u => u.Liker)
                .WithMany(u => u.Likee)
                .HasForeignKey(u => u.LikeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // and each likee can have many likers
            builder.Entity<Like>()
                .HasOne(u => u.Likee)
                .WithMany(u => u.Liker)
                .HasForeignKey(u => u.LikerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}