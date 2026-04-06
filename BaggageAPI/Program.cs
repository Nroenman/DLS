using BaggageAPI.Data;
using BaggageAPI.Interfaces;
using BaggageAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

//
// 🔧 SERVICES
//

// ✅ Controllers
builder.Services.AddControllers();

// ✅ Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Baggage API",
        Version = "v1",
        Description = "Microservice for handling baggage tracking, check-in, and logistics"
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Dependency Injection
builder.Services.AddScoped<IBaggageService, BaggageService>();

// 🔜 Future: RabbitMQ, Auth etc.
// builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

var app = builder.Build();

//
// 🚀 MIDDLEWARE
//

// Auto-apply migrations (optional but useful in Docker)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); // creates DB automatically
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 🔐 (when you add JWT later)
// app.UseAuthentication();
// app.UseAuthorization();

//
// 🌐 ENDPOINTS
//

app.MapControllers();

app.Run();