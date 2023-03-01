public static class DeleteUpload
{
    public static IResult Handle(HttpContext context, Db db, string id, string key = default, bool confirmed = false) {
        if (key.IsNullOrWhiteSpace()) {
            return Results.Text("No key was found", default, default, 400);
        }

        var isPaste = context.Request.Path.StartsWithSegments("/p");
        UploadEntityBase? upload = isPaste
            ? db.Pastes.FirstOrDefault(c => c.PublicId == id)
            : db.Files.FirstOrDefault(c => c.PublicId == id);

        if (upload is not {DeletedAt: null}) {
            return Results.NotFound();
        }

        if (upload.DeletionKey != key) {
            return Results.Text("Invalid key", default, default, 400);
        }

        if (!confirmed) {
            return Results.Content($"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <link rel="stylesheet" href="/index.css">
    <title>{upload.PublicId} - Confirm deletion - Blobbin</title>
</head>
<body>
    <p>Are you sure you want to delete {upload.Name}?</p>
    <a href="{context.Request.Path.ToString()}&confirmed=true">Yes</a>
    <span>  </span>
    <a href="/">No, cancel</a>
</body>
</html>
""", "text/html");
        }

        upload.DeletedAt = DateTime.UtcNow;
        db.SaveChanges();

        return Results.Text("""
The upload is marked for deletion and cannot be accessed any more, all traces off it will be gone from our systems within 7 days. 
""");
    }
}