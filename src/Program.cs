global using BlobBin;
using System.Text;
using System.Text.Json;
using IOL.Helpers;
using Serilog;
using Serilog.Events;
using File = BlobBin.File;

const long MAX_REQUEST_BODY_SIZE = 104_857_600;
var builder = WebApplication.CreateBuilder(args);
var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console();
Log.Logger = logger.CreateLogger();
Log.Information("Starting web host");
builder.Host.UseSerilog(Log.Logger);
builder.Services.AddDbContext<Db>();
builder.Services.AddHostedService<WallE>();
builder.WebHost.UseKestrel(o => { o.Limits.MaxRequestBodySize = MAX_REQUEST_BODY_SIZE; });
var app = builder.Build();
app.UseFileServer();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();
if (app.Environment.IsProduction()) {
    app.UseForwardedHeaders();
}

app.MapGet("/upload-link", GetFileUploadLink);
app.MapPost("/file/{id}", UploadFilePart);
app.MapPost("/file", UploadFile);
app.MapPost("/text", UploadText);
app.MapGet("/b/{id}/delete", DeleteUpload);
app.MapGet("/p/{id}/delete", DeleteUpload);
app.MapPost("/b/{id}", GetFile);
app.MapGet("/b/{id}", GetFile);
app.MapGet("/p/{id}", GetPaste);
app.MapPost("/p/{id}", GetPaste);
Tools.GetFilesDirectoryPath(true);
app.Run();

IResult DeleteUpload(HttpContext context, Db db, string id, string key = default, bool confirmed = false) {
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
    <a href="/{context.Request.Path.ToString()}&confirmed=true">Yes</a>
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

IResult GetFileUploadLink(HttpContext context, Db db) {
    var file = new File {
        CreatedBy = context.Request.Headers["X-Forwarded-For"].ToString()
    };
    db.Files.Add(file);
    db.SaveChanges();
    return Results.Text(
        context.Request.GetRequestHost()
        + "/upload/"
        + file.Id
    );
}

async Task<IResult> UploadFile(HttpContext context, Db db) {
    if (!context.Request.Form.Files.Any()) {
        return Results.BadRequest("No files was found in request");
    }

    var file = new File {
        CreatedBy = context.Request.Headers["X-Forwarded-For"].ToString(),
        Singleton = context.Request.Form["singleton"] == "on",
        AutoDeleteAfter = context.Request.Form["autoDeleteAfter"],
        Length = context.Request.Form.Files[0].Length,
        Name = context.Request.Form.Files[0].FileName,
        MimeType = context.Request.Form.Files[0].ContentType,
        PublicId = GetUnusedPublicFileId(db),
        DeletionKey = RandomString.Generate(6),
    };

    if (context.Request.Form["password"].ToString().HasValue()) {
        file.PasswordHash = PasswordHelper.HashPassword(context.Request.Form["password"]);
    }

    await using var write = System.IO.File.OpenWrite(
        Path.Combine(Tools.GetFilesDirectoryPath(), file.Id.ToString())
    );
    await context.Request.Form.Files[0].CopyToAsync(write);
    db.Files.Add(file);
    db.SaveChanges();
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

IResult UploadFilePart(HttpContext context, Db db, Guid id) {
    return Results.Ok();
}

async Task<IResult> UploadText(HttpContext context, Db db) {
    if (context.Request.Form["content"].ToString().IsNullOrWhiteSpace()) {
        return Results.Text("No content was found in request", default, default, 400);
    }

    var paste = new Paste {
        CreatedBy = context.Request.Headers["X-Forwarded-For"].ToString(),
        Singleton = context.Request.Form["singleton"] == "on",
        AutoDeleteAfter = context.Request.Form["autoDeleteAfter"],
        Length = context.Request.Form["content"].Count,
        Name = context.Request.Form["name"],
        MimeType = context.Request.Form["mime"],
        PublicId = GetUnusedPublicPasteId(db),
        Content = context.Request.Form["content"],
        DeletionKey = RandomString.Generate(6),
    };

    if (paste.MimeType.IsNullOrWhiteSpace()) {
        paste.MimeType = "text/plain";
    }

    if (context.Request.Form["password"].ToString().HasValue()) {
        paste.PasswordHash = PasswordHelper.HashPassword(context.Request.Form["password"]);
    }

    db.Pastes.Add(paste);
    db.SaveChanges();
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

async Task<IResult> GetPaste(HttpContext context, string id, Db db) {
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
        db.SaveChanges();
    }

    if (ShouldDeleteUpload(paste)) {
        paste.DeletedAt = DateTime.UtcNow;
        db.SaveChanges();
    }

    Console.WriteLine(JsonSerializer.Serialize(paste));
    return Results.Content(paste.Content, paste.MimeType, Encoding.UTF8);
}

async Task<IResult> GetFile(HttpContext context, Db db, string id, bool download = false) {
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
        db.SaveChanges();
    }

    if (ShouldDeleteUpload(file)) {
        file.DeletedAt = DateTime.UtcNow;
        db.SaveChanges();
    }

    var path = Path.Combine(Tools.GetFilesDirectoryPath(), file.Id.ToString());
    if (!System.IO.File.Exists(path)) {
        return Results.NotFound();
    }

    var reader = await System.IO.File.ReadAllBytesAsync(path);
    return download ? Results.File(reader, file.MimeType, file.Name) : Results.Bytes(reader, file.MimeType);
}

bool ShouldDeleteUpload(UploadEntityBase entity) {
    if (entity.AutoDeleteAfter.IsNullOrWhiteSpace()) {
        return false;
    }

    var deletedDateTime = entity.CreatedAt.Add(Tools.ParseHumanTimeSpan(entity.AutoDeleteAfter));
    return DateTime.Compare(DateTime.UtcNow, deletedDateTime) > 0;
}

string GetUnusedPublicFileId(Db db) {
    string id() => RandomString.Generate(3);
    var res = id();
    while (db.Files.Any(c => c.PublicId == res)) {
        res = id();
    }

    return res;
}

string GetUnusedPublicPasteId(Db db) {
    string id() => RandomString.Generate(3);
    var res = id();
    while (db.Pastes.Any(c => c.PublicId == res)) {
        res = id();
    }

    return res;
}