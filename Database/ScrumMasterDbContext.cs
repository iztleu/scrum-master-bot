using Domain.Models;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Action = Domain.Models.Action;

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
    
    public DbSet<Voting> Votings { get; set; }
    
    public DbSet<Vote> Votes { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Action>(entity => {
            entity.HasIndex(e => e.TelegramUserId).IsUnique();
        });
    }
}