namespace BlobBin.Endpoints;

public class GetPaste : BaseEndpoint
{
    private readonly Db _db;

    public GetPaste(Db db) {
        _db = db;
    }

    [HttpGet("/p/{id}")]
    [HttpPost("/p/{id}")]
    public async Task<ActionResult> Handle(string id) {
        var paste = _db.Pastes.FirstOrDefault(c => c.PublicId == id.Trim());
        if (paste is not {DeletedAt: null}) return NotFound();
        if (paste.PasswordHash.HasValue()) {
            var password = Request.Method == "POST" ? Request.Form["password"].ToString() : "";
            if (password.IsNullOrWhiteSpace() || !PasswordHelper.Verify(password, paste.PasswordHash)) {
                return Content($"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <link rel="stylesheet" href="/index.css">
    <title>{paste.PublicId} - Authenticate - Blobbin</title>
</head>
<body>
<form action="/p/{paste.PublicId}" method="post">
    <p>Authenticate to access this paste:</p>
    <input type="password" name="password" placeholder="Password">
    <button type="submit">Unlock</button>
</form>
</body>
</html>
""", "text/html");
            }
        }

        if (paste.Singleton) {
            paste.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        if (Tools.ShouldDeleteUpload(paste)) {
            paste.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Content(paste.Content, paste.MimeType, Encoding.UTF8);
    }
}