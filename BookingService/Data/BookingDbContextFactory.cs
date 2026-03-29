using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookingService.Data;

public class BookingDbContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BookingDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=BookingDb;Username=localdb;Password=localpassword");

        return new BookingDbContext(optionsBuilder.Options);
    }
}