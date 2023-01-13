using Microsoft.EntityFrameworkCore;

namespace BlobBin;

public sealed class DB : DbContext
{
    public DB(DbContextOptions<DB> options) : base(options) {
        Database.Migrate();
    }

    public DbSet<File> Files { get; set; }
    public DbSet<Paste> Pastes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        optionsBuilder.UseSqlite("data source = AppData/main.db");
        base.OnConfiguring(optionsBuilder);
    }
}

public class UploadEntityBase
{
    public UploadEntityBase() {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }
    public string PublicId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? PasswordHash { get; set; }
    public bool Singleton { get; set; }
    public string? AutoDeleteAfter { get; set; }
    public string? MimeType { get; set; }
}

public class File : UploadEntityBase
{
    public string? Name { get; set; }
    public long Length { get; set; }
}

public class Paste : UploadEntityBase
{
    public string? Name { get; set; }
    public string? Content { get; set; }
    public long Length { get; set; }
}