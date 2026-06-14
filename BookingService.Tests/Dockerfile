FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BookingService.Tests/BookingService.Tests.csproj", "BookingService.Tests/"]
COPY ["BookingService/BookingService.csproj", "BookingService/"]
RUN dotnet restore "BookingService.Tests/BookingService.Tests.csproj"
COPY . .
WORKDIR "/src/BookingService.Tests"
RUN dotnet build "./BookingService.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BookingService.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingService.Tests.dll"]
