FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/BuildingBlocks/BuildingBlocks.Common/BuildingBlocks.Common.csproj src/BuildingBlocks/BuildingBlocks.Common/
COPY src/BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj src/BuildingBlocks/BuildingBlocks.Messaging/
COPY src/Services/InventoryService/InventoryService.Domain/InventoryService.Domain.csproj src/Services/InventoryService/InventoryService.Domain/
COPY src/Services/InventoryService/InventoryService.Application/InventoryService.Application.csproj src/Services/InventoryService/InventoryService.Application/
COPY src/Services/InventoryService/InventoryService.Infrastructure/InventoryService.Infrastructure.csproj src/Services/InventoryService/InventoryService.Infrastructure/
COPY src/Services/InventoryService/InventoryService.API/InventoryService.API.csproj src/Services/InventoryService/InventoryService.API/
RUN dotnet restore src/Services/InventoryService/InventoryService.API/InventoryService.API.csproj

COPY src/ src/
RUN dotnet publish src/Services/InventoryService/InventoryService.API/InventoryService.API.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=10s --timeout=3s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "InventoryService.API.dll"]
