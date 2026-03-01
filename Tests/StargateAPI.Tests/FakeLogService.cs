using StargateAPI.Business.Services;

public class FakeLogService : ILogService
{
    public Task InfoAsync(string source, string message, string? details = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ErrorAsync(string source, string message, string? details = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}