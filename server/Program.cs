using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using server.Core;
using server.Service;
using server.Util;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

string path = Path.Combine(builder.Environment.ContentRootPath, "app.db");
string conn = $"Data Source={path};Cache=Shared;Foreign Keys=True;Journal Mode=WAL;Synchronous=Normal;busy_timeout=3000;Temp Store=Memory;";
builder.Services.AddDbContext<DB>(options => options.UseSqlite(conn));

builder.Services.AddOptions<AppPathsOptions>()
    .Bind(builder.Configuration.GetSection("AppPaths"))
    .PostConfigure(opts =>
    {
        static string Abs(string root, string p)
            => Path.IsPathRooted(p) ? Path.GetFullPath(p)
                                    : Path.GetFullPath(Path.Combine(root, p));

        var root = env.ContentRootPath; // 콘텐츠 루트
        if (!string.IsNullOrWhiteSpace(opts.BaseDir))
            opts.BaseDir            = Abs(root, opts.BaseDir);
        opts.ThumbImageDir          = Abs(root, opts.ThumbImageDir);
        opts.ViewImageDir           = Abs(root, opts.ViewImageDir);
        opts.DatabaseJsonPath       = Abs(root, opts.DatabaseJsonPath);
        opts.DatabaseJsonTempPath   = Abs(root, opts.DatabaseJsonTempPath);
        opts.ScanFolderPath         = Abs(root, opts.ScanFolderPath);
    });

builder.Services.AddOptions<ImageOptions>()
    .Bind(builder.Configuration.GetSection("Image"))
    .Validate(o => o.ThumbQuality is >= 1 and <= 100 && o.ViewQuality is >= 1 and <= 100,
              "Image quality must be 1~100");

builder.Services.AddOptions<CacheOptions>()
    .Bind(builder.Configuration.GetSection("Cache"));

builder.Services.AddSingleton<IPathUtil, PathUtil>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.AddConsole();

var channel = Channel.CreateUnbounded<Channels.ImageJob>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
builder.Services.AddSingleton(channel);
builder.Services.AddHostedService<ImageConvertWorker>();

builder.Services.AddScoped<Database>();
builder.Services.AddSingleton<VRCClient>();
builder.Services.AddSingleton<WorldPreprocessor>();

builder.Services.AddHostedService<StartupOrchestratorService>();


var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "static")),
    RequestPath = "/static"
});

app.UseAuthorization();
app.MapControllers();

app.Run();

