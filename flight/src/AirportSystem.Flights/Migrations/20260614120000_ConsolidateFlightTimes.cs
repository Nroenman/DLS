using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AirportSystem.Flights.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateFlightTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new single-time columns (nullable during migration)
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" ADD COLUMN ""ScheduledTime"" timestamp with time zone;");
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" ADD COLUMN ""ActualTime""    timestamp with time zone;");

            // Migrate existing data — use departure times as the authoritative single time
            migrationBuilder.Sql(@"UPDATE ""Flights"" SET ""ScheduledTime"" = ""ScheduledDeparture"";");
            migrationBuilder.Sql(@"UPDATE ""Flights"" SET ""ActualTime""    = ""ActualDeparture"";");

            // Make ScheduledTime NOT NULL now that every row has a value
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" ALTER COLUMN ""ScheduledTime"" SET NOT NULL;");

            // Drop the old four columns
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" DROP COLUMN ""ScheduledDeparture"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" DROP COLUMN ""ScheduledArrival"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" DROP COLUMN ""ActualDeparture"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" DROP COLUMN ""ActualArrival"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" ADD COLUMN ""ScheduledDeparture"" timestamp with time zone;");
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" ADD COLUMN ""ScheduledArrival""   timestamp with time zone;");
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" ADD COLUMN ""ActualDeparture""    timestamp with time zone;");
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" ADD COLUMN ""ActualArrival""      timestamp with time zone;");

            migrationBuilder.Sql(@"UPDATE ""Flights"" SET ""ScheduledDeparture"" = ""ScheduledTime"", ""ScheduledArrival"" = ""ScheduledTime"";");
            migrationBuilder.Sql(@"UPDATE ""Flights"" SET ""ActualDeparture"" = ""ActualTime"";");

            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" ALTER COLUMN ""ScheduledDeparture"" SET NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" ALTER COLUMN ""ScheduledArrival""   SET NOT NULL;");

            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" DROP COLUMN ""ScheduledTime"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Flights"" DROP COLUMN ""ActualTime"";");
        }
    }
}
