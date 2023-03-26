using System.Runtime.CompilerServices;

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
            Singleton = Request.Form["singleton"].ToString() == "on",
            IsProbablyEncrypted = Request.Form["encrypt"].ToString() == "on",
            AutoDeleteAfter = Request.Form["autoDeleteAfter"].ToString(),
            Name = Request.Form["name"],
            MimeType = Request.Form["mime"],
            PublicId = Tools.GetUnusedPublicPasteId(_db),
            Content = Request.Form["content"].ToString(),
            DeletionKey = RandomString.Generate(6),
        };

        paste.Length = paste.Content.Length;

        var uid = Request.Form["uid"].ToString();
        if (uid.HasValue()) {
            paste.CreatedBy = uid.AsGuid();
        }

        if (paste.MimeType.IsNullOrWhiteSpace()) {
            paste.MimeType = "text/plain";
        }

        var password = Request.Form["password"].ToString();
        if (password.HasValue()) {
            paste.PasswordHash = PasswordHelper.HashPassword(password);
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