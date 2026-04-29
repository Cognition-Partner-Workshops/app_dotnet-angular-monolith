FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY InventoryService.Api/InventoryService.Api.csproj InventoryService.Api/
RUN dotnet restore InventoryService.Api/InventoryService.Api.csproj
COPY . .
RUN dotnet build InventoryService.Api/InventoryService.Api.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish InventoryService.Api/InventoryService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "InventoryService.Api.dll"]
