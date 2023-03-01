namespace BlobBin;

public class CleanupService : BackgroundService
{
    private IServiceProvider Services { get; }
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(IServiceProvider services,
        ILogger<CleanupService> logger) {
        Services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("WallE is running.");
        await DoWork(stoppingToken);
    }

    private async Task DoWork(CancellationToken stoppingToken) {
        _logger.LogInformation("WallE is working.");
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Db>();
        var pastes = db.Pastes.Where(c => c.DeletedAt != default || c.AutoDeleteAfter != default)
            .Select(c => new Paste() {
                Id = c.Id,
                DeletedAt = c.DeletedAt,
                AutoDeleteAfter = c.AutoDeleteAfter,
                CreatedAt = c.CreatedAt
            })
            .ToList();
        var files = db.Files
            .Where(c => c.DeletedAt != default || c.AutoDeleteAfter != default)
            .Select(c => new File() {
                Id = c.Id,
                DeletedAt = c.DeletedAt,
                AutoDeleteAfter = c.AutoDeleteAfter,
                CreatedAt = c.CreatedAt
            })
            .ToList();
        var now = DateTime.UtcNow;
        foreach (var file in files) {
            var path = Path.Combine(Tools.GetFilesDirectoryPath(), file.Id.ToString());
            if (DateTime.Compare(now, file.DeletedAt?.AddDays(7) ?? DateTime.MinValue) > 0) {
                System.IO.File.Delete(path);
                db.Files.Remove(file);
                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Deleted file {fileId} completely", file.Id);
            } else if (file.AutoDeleteAfter.HasValue()) {
                var autoDeleteDateTime = file.CreatedAt.Add(Tools.ParseHumanTimeSpan(file.AutoDeleteAfter));
                if (DateTime.Compare(now, autoDeleteDateTime) > 0) {
                    System.IO.File.Delete(path);
                    db.Files.Remove(file);
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Deleted file {fileId} completely", file.Id);
                }
            }
        }

        // If the file has lived more than +7 days from when it should be automatically deleted, delete the file completely.
        foreach (var paste in pastes) {
            if (DateTime.Compare(now, paste.DeletedAt?.AddDays(7) ?? DateTime.MinValue) > 0) {
                db.Pastes.Remove(paste);
                _logger.LogInformation("Deleted paste {pasteId} completely", paste.Id);
                await db.SaveChangesAsync(stoppingToken);
            } else if (paste.AutoDeleteAfter.HasValue()) {
                var autoDeleteDateTime = paste.CreatedAt.Add(Tools.ParseHumanTimeSpan(paste.AutoDeleteAfter));
                if (DateTime.Compare(now, autoDeleteDateTime) > 0) {
                    db.Pastes.Remove(paste);
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Deleted paste {pasteId} completely", paste.Id);
                }
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("WallE is stopping.");
        await base.StopAsync(stoppingToken);
    }
}