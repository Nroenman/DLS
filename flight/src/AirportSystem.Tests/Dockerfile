FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["flight/src/AirportSystem.Tests/AirportSystem.Tests.csproj", "flight/src/AirportSystem.Tests/"]
COPY ["flight/src/AirportSystem.Flights/AirportSystem.Flights.csproj", "flight/src/AirportSystem.Flights/"]
RUN dotnet restore "flight/src/AirportSystem.Tests/AirportSystem.Tests.csproj"
COPY . .
WORKDIR "/src/flight/src/AirportSystem.Tests"
RUN dotnet build "./AirportSystem.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AirportSystem.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AirportSystem.Tests.dll"]
