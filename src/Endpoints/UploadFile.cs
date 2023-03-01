namespace BlobBin.Endpoints;

public static class UploadFile
{
    public static async Task<IResult> Handle(HttpContext context, Db db) {
        if (!context.Request.Form.Files.Any()) {
            return Results.BadRequest("No files was found in request");
        }

        var file = new File {
            IP = context.Request.Headers["X-Forwarded-For"].ToString(),
            Singleton = context.Request.Form["singleton"] == "on",
            AutoDeleteAfter = context.Request.Form["autoDeleteAfter"],
            Length = context.Request.Form.Files[0].Length,
            Name = context.Request.Form.Files[0].FileName,
            MimeType = context.Request.Form.Files[0].ContentType,
            PublicId = Tools.GetUnusedPublicFileId(db),
            DeletionKey = RandomString.Generate(6),
        };

        if (context.Request.Form.ContainsKey("uid")) {
            file.CreatedBy = context.Request.Form["uid"].ToString().AsGuid();
        }

        if (context.Request.Form["password"].ToString().HasValue()) {
            file.PasswordHash = PasswordHelper.HashPassword(context.Request.Form["password"]);
        }

        await using var write = System.IO.File.OpenWrite(
            Path.Combine(Tools.GetFilesDirectoryPath(), file.Id.ToString())
        );
        await context.Request.Form.Files[0].CopyToAsync(write);
        db.Files.Add(file);
        await db.SaveChangesAsync();
        var deletionNote = "The file is only deleted when you request it.";
        if (file.AutoDeleteAfter.HasValue()) {
            var relativeDateTime = file.CreatedAt.Add(Tools.ParseHumanTimeSpan(file.AutoDeleteAfter));
            deletionNote = $"The file will be automatically deleted at {relativeDateTime:u}";
        }

        return Results.Text($"""
Your file is available here: {context.Request.GetRequestHost()}/b/{file.PublicId}

To delete the file, open this url in a browser {context.Request.GetRequestHost()}/b/{file.PublicId}/delete?key={file.DeletionKey}.
{deletionNote}
""");
    }
}