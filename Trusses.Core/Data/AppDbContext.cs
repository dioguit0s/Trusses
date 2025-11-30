using Microsoft.EntityFrameworkCore;
using Trusses.Core.Models;

namespace Trusses.Core.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Truss> Trusses { get; set; }
        public DbSet<Node> Nodes { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Support> Supports { get; set; }
        public DbSet<Load> Loads { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer("Server=LOCALHOST;Database=Trusses;User Id=sa;Password=123456;TrustServerCertificate=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Node>()
                .HasOne(n => n.Support).WithOne(s => s.Node)
                .HasForeignKey<Support>(s => s.NodeId);

            modelBuilder.Entity<Node>()
                .HasOne(n => n.Load).WithOne(l => l.Node)
                .HasForeignKey<Load>(l => l.NodeId);
        }
    }
}