using Microsoft.Extensions.FileProviders;
using server.Core;
using System.IO;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.AddConsole();

var channel = Channel.CreateUnbounded<ImageJob>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
builder.Services.AddSingleton(channel);
builder.Services.AddHostedService<ImageConvertWorker>();

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

new Startup().OnStartup();

app.Run();

public record ImageJob(string worldid, string SourcePath, string DestPath, int Quality);