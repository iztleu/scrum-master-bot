using Database.Models;
using Microsoft.EntityFrameworkCore;
using Action = Database.Models.Action;

namespace Database;

public class ScrumMasterDbContext: DbContext
{
    public ScrumMasterDbContext(DbContextOptions<ScrumMasterDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<ScrumTeam> ScrumTeams { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Action> Actions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Action>(entity => {
            entity.HasIndex(e => e.UserId).IsUnique();
        });
    }
}