namespace App.Services;

public interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}