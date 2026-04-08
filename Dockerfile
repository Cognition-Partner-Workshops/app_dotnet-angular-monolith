# Stage 1: Build Angular frontend
FROM node:20-alpine AS frontend-build
WORKDIR /app/client-app
COPY client-app/package.json ./
RUN npm install
COPY client-app/ ./
RUN npx ng build --output-path /app/frontend-dist

# Stage 2: Build .NET API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /app
COPY OrderManager.sln ./
COPY src/OrderManager.Api/OrderManager.Api.csproj src/OrderManager.Api/
COPY tests/OrderManager.Api.Tests/OrderManager.Api.Tests.csproj tests/OrderManager.Api.Tests/
RUN dotnet restore
COPY src/ src/
COPY tests/ tests/
COPY --from=frontend-build /app/frontend-dist/browser src/OrderManager.Api/wwwroot
RUN dotnet publish src/OrderManager.Api/OrderManager.Api.csproj -c Release -o /app/publish --no-restore

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=backend-build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderManager.Api.dll"]
