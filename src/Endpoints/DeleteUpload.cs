public class DeleteUpload : BaseEndpoint
{
    private readonly Db _db;

    public DeleteUpload(Db db) {
        _db = db;
    }

    [HttpGet("/b/{id}/delete")]
    [HttpGet("/p/{id}/delete")]
    public ActionResult Handle(string id, string key = default, bool confirmed = false) {
        if (key.IsNullOrWhiteSpace()) {
            return BadRequest("No key was found");
        }

        var isPaste = Request.Path.StartsWithSegments("/p");
        UploadEntityBase? upload = isPaste
            ? _db.Pastes.FirstOrDefault(c => c.PublicId == id)
            : _db.Files.FirstOrDefault(c => c.PublicId == id);

        if (upload is not {DeletedAt: null}) {
            return NotFound();
        }

        if (upload.DeletionKey != key) {
            return BadRequest("Invalid key");
        }

        if (!confirmed) {
            return Content($"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <link rel="stylesheet" href="/index.css">
    <title>{upload.PublicId} - Confirm deletion - Blobbin</title>
</head>
<body>
    <p>Are you sure you want to delete {upload.Name}?</p>
    <a href="{Request.Path.ToString()}?key={key}&confirmed=true">Yes</a>
    <span> - </span>
    <a href="/">No, cancel</a>
</body>
</html>
""", "text/html");
        }

        upload.DeletedAt = DateTime.UtcNow;
        _db.SaveChanges();

        return Content("""
The upload is marked for deletion and cannot be accessed any more, all traces off it will be gone from our systems within 7 days. 
""", "text/plain");
    }
}