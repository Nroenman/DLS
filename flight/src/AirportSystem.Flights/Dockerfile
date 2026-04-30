# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/AirportSystem.Flights/AirportSystem.Flights.csproj", "src/AirportSystem.Flights/"]
RUN dotnet restore "src/AirportSystem.Flights/AirportSystem.Flights.csproj"

COPY . .
RUN dotnet publish "src/AirportSystem.Flights/AirportSystem.Flights.csproj" \
    -c Release -o /app/publish

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AirportSystem.Flights.dll"]
