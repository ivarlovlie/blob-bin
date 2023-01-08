var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseFileServer();
app.UseStatusCodePages();
app.MapPost("/upload", Upload);
app.MapPost("/text", UploadText);
app.MapGet("/b/{id}", GetBlob);
app.Run();

IResult Upload(HttpContext context) {
    var request = new UploadRequest() {
        Singleton = context.Request.Form["singleton"] == "on",
        File = context.Request.Form.Files.FirstOrDefault(),
        Password = context.Request.Form["password"],
        AutoDeleteAfter = context.Request.Form["autoDeleteAfter"]
    };
    return Results.Ok();
}

IResult UploadText(PasteRequest request) {
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