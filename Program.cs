global using Serilog;
using CacheService.Data;
using CacheService.Communications;
using CacheService.Models;
using StackExchange.Redis;
using CacheService.Hubs;
using System.Text;
using Microsoft.AspNetCore.SignalR;

using var log = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss:fff} {Level:u4}] {Message:lj}{NewLine}{Exception}",
    theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
    .CreateLogger();

Log.Logger = log;
Log.Information("The global logger has been configured");

//<1234CMD 3450000000FK132456                          >
//000000000011111111112222222222333333333344444444445555
//012345678901234567890123456789012345678901234567890123



//var t1 = new Telegram54()
//    .Node("1234")
//    .Type("CMD")
//    .SequenceNo(12)
//    .Addr1("1234")
//    .Addr2("000F")
//    .Barcode("K132456123456")
//    .Build();

//Console.WriteLine(t1.GetString());

//var t2 = new Telegram54()
//    .Node("1")
//    .Type("CMD")
//    .SequenceNo(13)
//    .Addr1("1234")
//    .Addr2("000F")
//    .Barcode("K13256")
//    .Build();

//Console.WriteLine(t2.GetString());


//Strategist.Do(new Parse(new Telegram54().Node("1").Build()));
//Strategist.WaitAndThen(
//    Strategist.WaitAndThen(
//        Strategist.Fork(new Parse(new Telegram54().Node("2").Build())),
//        new Parse(new Telegram54().Node("3").Build())
//        ),
//    new Parse(new Telegram54().Node("4").Build()));

//var server = new AsynchronousSocketListener("127.0.0.1", 11000);
//server.StartListeningThread();

//var e = new Echo(server);



WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IConnectionMultiplexer>(opt =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("DockerRedisConnection")));

builder.Services.AddScoped<IPlatformRepo, RedisPlatformRepo>();

//builder.Services.AddScoped<IndexHub>();
builder.Services.AddSingleton<Main>(new Main((IHubContext<IndexHub>)builder.Services.BuildServiceProvider().GetService<IHubClients<IndexHub>>()));

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.MapHub<IndexHub>("/indexHub");


app.Run();
Log.CloseAndFlush();
