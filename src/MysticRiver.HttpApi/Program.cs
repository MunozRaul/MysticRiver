using MysticRiver.Application.Battles;
using MysticRiver.Application.Data;
using MysticRiver.HttpApi.Battles;

using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Register PostgreSQL DbContext
var connectionString = builder.Configuration.GetConnectionString("MysticRiverDb")
    ?? throw new InvalidOperationException("Connection string 'MysticRiverDb' not configured.");

builder.Services.AddDbContext<MysticRiverDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IBattleSessionStore, InMemoryBattleSessionStore>();
builder.Services.AddScoped<BattleSessionPersistenceService>();
builder.Services.AddSingleton<IBattleService, BattleService>();
builder.Services.AddSingleton<IConnectionMapping, ConnectionMappingService>();
builder.Services.AddHostedService<TokenSweeperService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply database migrations on startup (skipped for InMemory databases used in testing)
using (var scope = app.Services.CreateScope()) {
    var dbContext = scope.ServiceProvider.GetRequiredService<MysticRiverDbContext>();
    if (!dbContext.Database.IsInMemory()) {
        dbContext.Database.Migrate();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<BattleHub>("/hubs/battle");

app.Run();
