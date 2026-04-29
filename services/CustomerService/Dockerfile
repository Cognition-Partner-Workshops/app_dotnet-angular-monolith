FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CustomerService.Api/CustomerService.Api.csproj CustomerService.Api/
RUN dotnet restore CustomerService.Api/CustomerService.Api.csproj
COPY . .
RUN dotnet build CustomerService.Api/CustomerService.Api.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish CustomerService.Api/CustomerService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "CustomerService.Api.dll"]
