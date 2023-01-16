using IOL.Helpers;

namespace BlobBin;

public class WallE : BackgroundService
{
    public IServiceProvider Services { get; }
    private readonly ILogger<WallE> _logger;

    public WallE(IServiceProvider services,
        ILogger<WallE> logger) {
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
        var eva = scope.ServiceProvider.GetRequiredService<Db>();
        var pastes = eva.Pastes.Where(c => c.DeletedAt != default || c.AutoDeleteAfter != default)
            .Select(c => new Paste() {
                Id = c.Id,
                DeletedAt = c.DeletedAt,
                AutoDeleteAfter = c.AutoDeleteAfter,
                CreatedAt = c.CreatedAt
            })
            .ToList();
        var files = eva.Files
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
                eva.Files.Remove(file);
            } else if (file.AutoDeleteAfter.HasValue()) {
                var autoDeleteDateTime = file.CreatedAt.Add(Tools.ParseHumanTimeSpan(file.AutoDeleteAfter));
                if (DateTime.Compare(now, autoDeleteDateTime) > 0) {
                    System.IO.File.Delete(path);
                    eva.Files.Remove(file);
                }
            }
        }

        // If the file has lived more than +7 days from when it should be automatically deleted, delete the file completely.
        foreach (var paste in pastes) {
            if (DateTime.Compare(now, paste.DeletedAt?.AddDays(7) ?? DateTime.MinValue) > 0) {
                eva.Pastes.Remove(paste);
            } else if (paste.AutoDeleteAfter.HasValue()) {
                var autoDeleteDateTime = paste.CreatedAt.Add(Tools.ParseHumanTimeSpan(paste.AutoDeleteAfter));
                if (DateTime.Compare(now, autoDeleteDateTime) > 0) {
                    eva.Pastes.Remove(paste);
                }
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("WallE is stopping.");
        await base.StopAsync(stoppingToken);
    }
}