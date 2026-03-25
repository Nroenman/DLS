# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/AirportSystem.Gateway/AirportSystem.Gateway.csproj", "src/AirportSystem.Gateway/"]
RUN dotnet restore "src/AirportSystem.Gateway/AirportSystem.Gateway.csproj"

COPY . .
RUN dotnet publish "src/AirportSystem.Gateway/AirportSystem.Gateway.csproj" \
    -c Release -o /app/publish

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AirportSystem.Gateway.dll"]
