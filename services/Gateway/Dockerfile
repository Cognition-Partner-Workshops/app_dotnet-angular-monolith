FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM node:18-alpine AS angular-build
WORKDIR /client
COPY ../../client-app/package*.json ./
RUN npm ci
COPY ../../client-app/ .
RUN npx ng build --output-path dist

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Gateway.Api/Gateway.Api.csproj Gateway.Api/
RUN dotnet restore Gateway.Api/Gateway.Api.csproj
COPY . .
RUN dotnet build Gateway.Api/Gateway.Api.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish Gateway.Api/Gateway.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=angular-build /client/dist/browser wwwroot/
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
