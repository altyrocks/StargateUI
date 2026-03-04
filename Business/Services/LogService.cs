namespace StargateAPI.Business.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using StargateAPI.Business.Data;

    public class LogService : ILogService
    {
        private readonly StargateContext _context;

        public LogService(StargateContext context)
        {
            _context = context;
        }

        public Task InfoAsync(
            string source,
            string message,
            string? details = null,
            CancellationToken cancellationToken = default)
        {
            return SaveAsync("INFO", source, message, details, cancellationToken);
        }

        public Task ErrorAsync(
            string source,
            string message,
            string? details = null,
            CancellationToken cancellationToken = default)
        {
            return SaveAsync("ERROR", source, message, details, cancellationToken);
        }

        private async Task SaveAsync(
            string level,
            string source,
            string message,
            string? details,
            CancellationToken cancellationToken)
        {
            var log = new ProcessLog
            {
                TimestampUtc = DateTime.UtcNow,
                Level = level,
                Source = source,
                Message = message,
                Details = details
            };

            _context.ProcessLogs.Add(log);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}