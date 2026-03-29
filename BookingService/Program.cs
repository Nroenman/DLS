using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBookingReadRepository, BookingReadRepository>();
builder.Services.AddScoped<IBookingWriteRepository, BookingWriteRepository>();
builder.Services.AddScoped<IPricingRepository, PricingRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();