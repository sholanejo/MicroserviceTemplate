FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore
COPY src/BuildingBlocks/BuildingBlocks.Common/BuildingBlocks.Common.csproj src/BuildingBlocks/BuildingBlocks.Common/
COPY src/BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj src/BuildingBlocks/BuildingBlocks.Messaging/
COPY src/Services/OrderService/OrderService.Domain/OrderService.Domain.csproj src/Services/OrderService/OrderService.Domain/
COPY src/Services/OrderService/OrderService.Application/OrderService.Application.csproj src/Services/OrderService/OrderService.Application/
COPY src/Services/OrderService/OrderService.Infrastructure/OrderService.Infrastructure.csproj src/Services/OrderService/OrderService.Infrastructure/
COPY src/Services/OrderService/OrderService.API/OrderService.API.csproj src/Services/OrderService/OrderService.API/
RUN dotnet restore src/Services/OrderService/OrderService.API/OrderService.API.csproj

# Copy everything and build
COPY src/ src/
RUN dotnet publish src/Services/OrderService/OrderService.API/OrderService.API.csproj \
    -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=10s --timeout=3s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "OrderService.API.dll"]
