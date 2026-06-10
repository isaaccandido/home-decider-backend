using HomeDecider.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeDecider.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Decision> Decisions => Set<Decision>();
    public DbSet<Option> Options => Set<Option>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vote>()
            .HasIndex(v => new { v.DecisionId, v.VoterName, v.OptionId })
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Option>()
            .HasOne(o => o.Decision)
            .WithMany(d => d.Options)
            .HasForeignKey(o => o.DecisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vote>()
            .HasOne(v => v.Decision)
            .WithMany(d => d.Votes)
            .HasForeignKey(v => v.DecisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vote>()
            .HasOne(v => v.Option)
            .WithMany(o => o.Votes)
            .HasForeignKey(v => v.OptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
