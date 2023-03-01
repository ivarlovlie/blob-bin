namespace BlobBin.Endpoints;

public static class UploadText
{
    public static async Task<IResult> Handle(HttpContext context, Db db) {
        if (context.Request.Form["content"].ToString().IsNullOrWhiteSpace()) {
            return Results.Text("No content was found in request", default, default, 400);
        }

        var paste = new Paste {
            IP = context.Request.Headers["X-Forwarded-For"].ToString(),
            Singleton = context.Request.Form["singleton"] == "on",
            AutoDeleteAfter = context.Request.Form["autoDeleteAfter"],
            Length = context.Request.Form["content"].Count,
            Name = context.Request.Form["name"],
            MimeType = context.Request.Form["mime"],
            PublicId = Tools.GetUnusedPublicPasteId(db),
            Content = context.Request.Form["content"],
            DeletionKey = RandomString.Generate(6),
        };
        if (context.Request.Form.ContainsKey("uid")) {
            paste.CreatedBy = context.Request.Form["uid"].ToString().AsGuid();
        }

        if (paste.MimeType.IsNullOrWhiteSpace()) {
            paste.MimeType = "text/plain";
        }

        if (context.Request.Form["password"].ToString().HasValue()) {
            paste.PasswordHash = PasswordHelper.HashPassword(context.Request.Form["password"]);
        }

        db.Pastes.Add(paste);
        await db.SaveChangesAsync();
        var deletionNote = "The paste is only deleted when you request it.";
        if (paste.AutoDeleteAfter.HasValue()) {
            var relativeDateTime = paste.CreatedAt.Add(Tools.ParseHumanTimeSpan(paste.AutoDeleteAfter));
            deletionNote = $"The paste will be automatically deleted at {relativeDateTime:u}";
        }

        return Results.Text($"""
Your paste is available here: {context.Request.GetRequestHost()}/p/{paste.PublicId}

To delete the paste, open this url in a browser {context.Request.GetRequestHost()}/p/{paste.PublicId}/delete?key={paste.DeletionKey}.
{deletionNote}
""");
    }
}