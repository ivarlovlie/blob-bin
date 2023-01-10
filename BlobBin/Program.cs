global using BlobBin;
using Microsoft.EntityFrameworkCore;
using File = BlobBin.File;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<DB>(opt => opt.UseSqlite("data source=main.db"));
var app = builder.Build();

app.UseFileServer();
app.UseStatusCodePages();
app.MapGet("/upload-link", GetUploadLink);
app.MapPost("/upload/{id}", GetUploadLink);
app.MapPost("/text", UploadText);
app.MapGet("/b/{id}", GetBlob);
app.Run();

IResult GetUploadLink(HttpContext context, DB db) {
    var file = new File {
        CreatedBy = context.Request.Headers["X-Forwarded-For"].ToString()
    };
    db.Files.Add(file);
    db.SaveChanges();
    return Results.Ok(context.Request.Host.Value + "/upload/" + file.Id);
}

IResult Upload(HttpContext context, DB db) {
    return Results.Ok();
}

IResult UploadText(HttpContext context, DB db) {
    return Results.Ok();
}

IResult GetBlob(string id) {
    return Results.Ok();
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