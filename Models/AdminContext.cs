using Microsoft.EntityFrameworkCore;

namespace PodcastSiteBuilder.Models
{
    public class AdminContext : DbContext
    {
        public AdminContext(DbContextOptions<AdminContext> options) : base(options) { }
        public DbSet<Admin> Admins {get;set;}
        public DbSet<Podcast> Podcasts {get;set;}
    }
}