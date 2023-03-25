namespace BlobBin.Endpoints;

public class UploadText : BaseEndpoint
{
    private readonly Db _db;

    public UploadText(Db db) {
        _db = db;
    }

    [HttpPost("/text")]
    public async Task<ActionResult> Handle() {
        if (Request.Form["content"].ToString().IsNullOrWhiteSpace()) {
            return BadRequest("No content was found in request");
        }

        var paste = new Paste {
            IP = Request.Headers["X-Forwarded-For"].ToString(),
            Singleton = Request.Form["singleton"] == "on",
            AutoDeleteAfter = Request.Form["autoDeleteAfter"],
            Length = Request.Form["content"].Count,
            Name = Request.Form["name"],
            MimeType = Request.Form["mime"],
            PublicId = Tools.GetUnusedPublicPasteId(_db),
            Content = Request.Form["content"],
            DeletionKey = RandomString.Generate(6),
        };
        if (Request.Form.ContainsKey("uid")) {
            paste.CreatedBy = Request.Form["uid"].ToString().AsGuid();
        }

        if (paste.MimeType.IsNullOrWhiteSpace()) {
            paste.MimeType = "text/plain";
        }

        if (Request.Form["password"].ToString().HasValue()) {
            paste.PasswordHash = PasswordHelper.HashPassword(Request.Form["password"]);
        }

        _db.Pastes.Add(paste);
        await _db.SaveChangesAsync();
        var deletionNote = "The paste is only deleted when you request it.";
        if (paste.AutoDeleteAfter.HasValue()) {
            var relativeDateTime = paste.CreatedAt.Add(Tools.ParseHumanTimeSpan(paste.AutoDeleteAfter));
            deletionNote = $"The paste will be automatically deleted at {relativeDateTime:u}";
        }

        return Content($"""
Your paste is available here: {Request.GetRequestHost()}/p/{paste.PublicId}

To delete the paste, open this url in a browser {Request.GetRequestHost()}/p/{paste.PublicId}/delete?key={paste.DeletionKey}.
{deletionNote}
""", "text/plain");
    }
}