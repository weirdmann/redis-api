using CacheService.Data;
using CacheService.Models;
using StackExchange.Redis;
using System.Text;


var t = new Telegram54()
    .AddField("node", 1, 4)
    .AddField("1234567890", 50, 10)
    .Build();

Console.WriteLine(Encoding.ASCII.GetString(t));

//<1234CMD 3450000000FK132456                          >
//000000000011111111112222222222333333333344444444445555
//012345678901234567890123456789012345678901234567890123



t = new Telegram54()
    .AddField("1234", 1, 4)
    .AddField("CMD", 5, 4)
    .AddField("345", 9, 3)
    .AddField("1234",12, 4).AddField("000F", 16, 4)
    .AddField("K132456123456".PadRight(32, ' '), 20, 32)
    .Build();

Console.WriteLine(Encoding.ASCII.GetString(t));


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IConnectionMultiplexer>(opt => 
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("DockerRedisConnection")));

builder.Services.AddScoped<IPlatformRepo, RedisPlatformRepo>();

builder.Services.AddControllers();
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

app.UseAuthorization();

app.MapControllers();

app.Run();
