using Microsoft.AspNetCore.Mvc;

namespace BlobBin.Endpoints;

public static class GetPaste
{
    public static async Task<IResult> Handle(
        HttpContext context,
        [FromRoute] string id,
        Db db,
        ILogger logger) {
        var paste = db.Pastes.FirstOrDefault(c => c.PublicId == id.Trim());
        if (paste is not {DeletedAt: null}) return Results.NotFound();
        if (paste.PasswordHash.HasValue()) {
            var password = context.Request.Method == "POST" ? context.Request.Form["password"].ToString() : "";
            if (password.IsNullOrWhiteSpace() || !PasswordHelper.Verify(password, paste.PasswordHash)) {
                return Results.Content($"""
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
            await db.SaveChangesAsync();
        }

        if (Tools.ShouldDeleteUpload(paste)) {
            paste.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        Console.WriteLine(JsonSerializer.Serialize(paste));
        return Results.Content(paste.Content, paste.MimeType, Encoding.UTF8);
    }
}