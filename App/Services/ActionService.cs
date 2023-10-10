using Database;
using Microsoft.EntityFrameworkCore;
using Action =  Domain.Models.Action;
namespace App.Services;

public class ActionService
{
    private readonly ILogger<ActionService> _logger;
    private readonly ScrumMasterDbContext _dbContext;
    
    public ActionService(ILogger<ActionService> logger, ScrumMasterDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }
    
    public async Task<long> CreateActionAsync(Action action)
    {
        _dbContext.Actions.Add(action);
        await _dbContext.SaveChangesAsync();
        return action.Id;
    }
    
    public async Task<Action[]> GetActionsAsync(long telegramUserId, CancellationToken ct = default)
    {
        return await _dbContext.Actions   
            .Where(a => a.TelegramUserId == telegramUserId)
            .ToArrayAsync(ct);
    }
    
    public async Task<Action?> GetActionAsync(long telegramUserId, long actionId, CancellationToken ct = default)
    {
        return await _dbContext.Actions
            .Where(a => a.TelegramUserId == telegramUserId && a.Id == actionId)
            .FirstOrDefaultAsync(ct);
    }
    
    public async Task UpdateActionAsync(Action action)
    {
        _dbContext.Actions.Update(action);
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task DeleteActionAsync(Action action)
    {
        _dbContext.Actions.Remove(action);
        await _dbContext.SaveChangesAsync();
    }
}