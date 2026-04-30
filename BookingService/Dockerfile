FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .

WORKDIR /app
ENTRYPOINT ["dotnet", "BookingService.dll"]
