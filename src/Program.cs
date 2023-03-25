global using BlobBin;
global using IOL.Helpers;
global using System.Text;
global using BlobBin.Endpoints;
global using Microsoft.AspNetCore.Mvc;
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
builder.Services.AddControllers();
var app = builder.Build();
app.UseFileServer();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();
if (app.Environment.IsProduction()) {
    app.UseForwardedHeaders();
}


app.MapControllers();
Tools.GetFilesDirectoryPath(true);
app.Run();