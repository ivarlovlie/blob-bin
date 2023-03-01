namespace BlobBin.Endpoints;

public static class GetFile
{
    public static async Task<IResult> Handle(
        HttpContext context, 
        Db db, 
        string id, 
        ILogger logger,
        bool download = false) {
        var file = db.Files.FirstOrDefault(c => c.PublicId == id.Trim());
        if (file is not {DeletedAt: null}) return Results.NotFound();
        if (file.PasswordHash.HasValue()) {
            var password = context.Request.Method == "POST" ? context.Request.Form["password"].ToString() : "";
            if (password.IsNullOrWhiteSpace() || !PasswordHelper.Verify(password, file.PasswordHash)) {
                return Results.Content($"""
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
            await db.SaveChangesAsync();
            logger.LogInformation("Deleting file {fileid}, because it is marked as singleton",file.Id);
        }

        if (Tools.ShouldDeleteUpload(file)) {
            file.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            logger.LogInformation("Deleting file {fileid}, because it is time",file.Id);
        }

        var path = Path.Combine(Tools.GetFilesDirectoryPath(), file.Id.ToString());
        if (!System.IO.File.Exists(path)) {
            return Results.NotFound();
        }

        var reader = await System.IO.File.ReadAllBytesAsync(path);
        return download ? Results.File(reader, file.MimeType, file.Name) : Results.Bytes(reader, file.MimeType, file.Name);
    }
}