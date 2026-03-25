using AirportSystem.Flights.Data;
using AirportSystem.Flights.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddGraphQLConfiguration();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
        await DevDataSeeder.SeedAsync(db);
}

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseForwardedHeaders();
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.MapGraphQL();

app.Run();

public partial class Program { }
