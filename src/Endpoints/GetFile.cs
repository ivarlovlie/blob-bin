namespace BlobBin.Endpoints;

public class GetFile : BaseEndpoint
{
    private readonly Db _db;
    private readonly ILogger<GetFile> _logger;

    public GetFile(Db db, ILogger<GetFile> logger) {
        _db = db;
        _logger = logger;
    }

    [HttpGet("/b/{id}")]
    [HttpPost("/b/{id}")]
    public async Task<ActionResult> Handle(string id) {
        var file = _db.Files.FirstOrDefault(c => c.PublicId == id.Trim());
        if (file is not {DeletedAt: null}) return NotFound();
        if (file.PasswordHash.HasValue()) {
            var password = Request.Method == "POST" ? Request.Form["password"].ToString() : "";
            if (password.IsNullOrWhiteSpace() || !PasswordHelper.Verify(password, file.PasswordHash)) {
                return Content($"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <link rel="stylesheet" href="/index.css">
    <title>{file.PublicId} - Authenticate - Blobbin</title>
</head>
<body>
<form action="/b/{file.PublicId}" method="post">
    <p>Authenticate to access this file:</p>
    <input type="password" name="password" placeholder="Password">
    <button type="submit">Unlock</button>
</form>
</body>
</html>
""", "text/html");
            }
        }

        if (file.Singleton) {
            file.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleting file {fileid}, because it is marked as singleton", file.Id);
        }

        if (Tools.ShouldDeleteUpload(file)) {
            file.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Deleting file {fileid}, because it is time", file.Id);
        }

        var path = Path.Combine(Tools.GetFilesDirectoryPath(), file.Id.ToString());
        if (!System.IO.File.Exists(path)) {
            return NotFound();
        }

        var reader = await System.IO.File.ReadAllBytesAsync(path);
        return File(reader, file.MimeType, file.Name);
    }
}