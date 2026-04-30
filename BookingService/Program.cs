using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.Repositories;
using BookingService.Services;
using BookingService.Validators;
using BookingService.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBookingReadRepository, BookingReadRepository>();
builder.Services.AddScoped<IBookingWriteRepository, BookingWriteRepository>();
builder.Services.AddScoped<IPricingRepository, PricingRepository>();
builder.Services.AddScoped<IBookingValidator, BookingValidator>();
builder.Services.AddScoped<IBookingService, BookingService.Services.BookingService>();
var publisher = new BookingEventPublisher();
await publisher.InitializeAsync(builder.Configuration);
builder.Services.AddSingleton<IBookingEventPublisher>(publisher);

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();