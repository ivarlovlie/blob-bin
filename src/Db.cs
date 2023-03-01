using Microsoft.EntityFrameworkCore;

namespace BlobBin;

public sealed class Db : DbContext
{
    private readonly bool migrated;

    public Db(DbContextOptions<Db> options) : base(options) {
        if (!migrated) {
            Database.Migrate();
            migrated = true;
        }
    }

    public DbSet<File> Files { get; set; }
    public DbSet<Paste> Pastes { get; set; }
    public DbSet<User> Users { get; set; }

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
    public Guid? CreatedBy { get; set; }
    public string IP { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? PasswordHash { get; set; }
    public bool Singleton { get; set; }
    public string? AutoDeleteAfter { get; set; }
    public string? MimeType { get; set; }
    public string DeletionKey { get; set; }
    public string? Name { get; set; }
    public long Length { get; set; }
}

public class Paste : UploadEntityBase
{
    public string? Content { get; set; }
}

public class File : UploadEntityBase
{ }

public class User
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string IP { get; set; }
    public string Uid { get; set; }
    public string PasswordHash { get; set; }
}