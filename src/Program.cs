global using BlobBin;
using IOL.Helpers;
using Microsoft.AspNetCore.Http.Features;
using File = BlobBin.File;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<DB>();
builder.WebHost.UseKestrel(o => { o.Limits.MaxRequestBodySize = 100_000_000; });
var app = builder.Build();

app.UseFileServer();
app.UseStatusCodePages();
app.MapGet("/upload-link", GetUploadLink);
app.MapPost("/upload/{id}", UploadBig);
app.MapPost("/upload", UploadSimple);
app.MapPost("/text", UploadText);
app.MapGet("/b/{id}", GetBlob);
Util.GetFilesDirectoryPath(true);
app.Run();

IResult GetUploadLink(HttpContext context, DB db) {
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

async Task<IResult> UploadSimple(HttpContext context, DB db) {
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
        PublicId = GetUnusedBlobId(db)
    };

    if (context.Request.Form["password"].ToString().HasValue()) {
        file.PasswordHash = PasswordHelper.HashPassword(context.Request.Form["password"]);
    }

    await using var write = System.IO.File.OpenWrite(
        Path.Combine(Util.GetFilesDirectoryPath(), file.Id.ToString())
    );
    await context.Request.Form.Files[0].CopyToAsync(write);
    db.Files.Add(file);
    db.SaveChanges();
    return Results.Text(
        context.Request.GetRequestHost()
        + "/b/"
        + file.PublicId
    );
}

IResult UploadBig(HttpContext context, DB db) {
    return Results.Ok();
}

IResult UploadText(HttpContext context, DB db) {
    return Results.Ok();
}

async Task<IResult> GetBlob(string id, DB db) {
    var file = db.Files.FirstOrDefault(c => c.PublicId == id.Trim());
    if (file == default) return Results.NotFound();
    var reader = await System.IO.File.ReadAllBytesAsync(
        Path.Combine(
            Util.GetFilesDirectoryPath(), file.Id.ToString()
        )
    );
    return Results.File(reader, file.MimeType, file.Name);
}


string GetUnusedBlobId(DB db) {
    string id() => RandomString.Generate(3);
    var res = id();
    while (db.Files.Any(c => c.PublicId == res)) {
        res = id();
    }

    return res;
}

class BlobBase
{
    public string Password { get; set; }
    public bool Singleton { get; set; }
    public string AutoDeleteAfter { get; set; }
}

class PasteRequest : BlobBase
{
    public string Text { get; set; }
    public string Mime { get; set; }
}

class UploadRequest : BlobBase
{
    public IFormFile? File { get; set; }
}