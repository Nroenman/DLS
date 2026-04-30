FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AirportSystem.Gateway.csproj", "."]
RUN dotnet restore "AirportSystem.Gateway.csproj"

COPY . .
RUN dotnet publish "AirportSystem.Gateway.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AirportSystem.Gateway.dll"]
