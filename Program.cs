using Microsoft.Extensions.Options;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connectionMultiplexer = ConnectionMultiplexer
        .Connect(builder.Configuration["redisConnectionString"] ?? "localhost");

        return connectionMultiplexer;
    
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed_window", config =>
    {
        config.Window = TimeSpan.FromSeconds(10);
        config.PermitLimit = 5;
        config.QueueLimit = 0;
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseRateLimiter();

app.MapControllers().RequireRateLimiting("fixed_window");

app.Run();


