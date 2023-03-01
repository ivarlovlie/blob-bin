global using BlobBin;
global using IOL.Helpers;
global using System.Text;
global using System.Text.Json;
using BlobBin.Endpoints;
using Serilog;
using Serilog.Events;

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
builder.Services.AddHostedService<CleanupService>();
builder.WebHost.UseKestrel(o => { o.Limits.MaxRequestBodySize = MAX_REQUEST_BODY_SIZE; });
builder.Services.AddAuthentication().AddCookie(o => o.Cookie.Name = "session");
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseFileServer();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();
if (app.Environment.IsProduction()) {
    app.UseForwardedHeaders();
}

app.MapGet("/upload-link", GetFileUploadLink.Handle);
app.MapPost("/file/{id}", UploadFilePart.Handle);
app.MapPost("/file", UploadFile.Handle);
app.MapPost("/text", UploadText.Handle);
app.MapGet("/b/{id}/delete", DeleteUpload.Handle);
app.MapGet("/p/{id}/delete", DeleteUpload.Handle);
app.MapPost("/b/{id}", GetFile.Handle);
app.MapGet("/b/{id}", GetFile.Handle);
app.MapGet("/p/{id}", GetPaste.Handle);
app.MapPost("/p/{id}", GetPaste.Handle);
app.MapPost("/uploads", GetUploads.Handle);
app.MapPost("/update-user", UpdateUser.Handle);
app.MapPost("/delete-user", DeleteUser.Handle);
app.MapPost("/create-user", CreateUser.Handle);
Tools.GetFilesDirectoryPath(true);
app.Run();