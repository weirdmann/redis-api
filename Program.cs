using CacheService.Data;
using CacheService.Models;
using StackExchange.Redis;
using System.Text;




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


Strategist.Do(new Parse(new Telegram54().Node("1").Build()));
Strategist.WaitAndThen(
    Strategist.WaitAndThen(
        Strategist.Fork(new Parse(new Telegram54().Node("2").Build())),
        new Parse(new Telegram54().Node("3").Build())
        ),
    new Parse(new Telegram54().Node("4").Build()));




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
