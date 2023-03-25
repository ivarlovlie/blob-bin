namespace BlobBin.Endpoints;

public class UploadFile : BaseEndpoint
{
    private readonly Db _db;

    public UploadFile(Db db) {
        _db = db;
    }

    [HttpPost("/file")]
    public async Task<ActionResult> Handle() {
        if (!Request.Form.Files.Any()) {
            return BadRequest("No files was found in request");
        }

        var file = new File {
            IP = Request.Headers["X-Forwarded-For"].ToString(),
            Singleton = Request.Form["singleton"] == "on",
            AutoDeleteAfter = Request.Form["autoDeleteAfter"],
            Length = Request.Form.Files[0].Length,
            Name = Request.Form.Files[0].FileName,
            MimeType = Request.Form.Files[0].ContentType,
            PublicId = Tools.GetUnusedPublicFileId(_db),
            DeletionKey = RandomString.Generate(6),
        };

        if (Request.Form.ContainsKey("uid")) {
            file.CreatedBy = Request.Form["uid"].ToString().AsGuid();
        }

        if (Request.Form["password"].ToString().HasValue()) {
            file.PasswordHash = PasswordHelper.HashPassword(Request.Form["password"]);
        }

        await using var write = System.IO.File.OpenWrite(
            Path.Combine(Tools.GetFilesDirectoryPath(), file.Id.ToString())
        );
        await Request.Form.Files[0].CopyToAsync(write);
        _db.Files.Add(file);
        await _db.SaveChangesAsync();
        var deletionNote = "The file is only deleted when you request it.";
        if (file.AutoDeleteAfter.HasValue()) {
            var relativeDateTime = file.CreatedAt.Add(Tools.ParseHumanTimeSpan(file.AutoDeleteAfter));
            deletionNote = $"The file will be automatically deleted at {relativeDateTime:u}";
        }

        return Content($"""
Your file is available here: {Request.GetRequestHost()}/b/{file.PublicId}

To delete the file, open this url in a browser {Request.GetRequestHost()}/b/{file.PublicId}/delete?key={file.DeletionKey}.
{deletionNote}
"""
            , "text/plain");
    }
}