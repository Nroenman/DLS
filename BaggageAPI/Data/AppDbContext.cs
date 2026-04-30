using BaggageAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BaggageAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Baggage> Baggages { get; set; }
}