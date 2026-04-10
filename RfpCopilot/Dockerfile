# Stage 1: Build Angular frontend
FROM node:20-alpine AS frontend-build
WORKDIR /app/client-app
COPY client-app/package*.json ./
RUN npm ci
COPY client-app/ ./
RUN npx ng build --configuration production

# Stage 2: Build .NET backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /app
COPY src/RfpCopilot.Api/RfpCopilot.Api.csproj src/RfpCopilot.Api/
RUN dotnet restore src/RfpCopilot.Api/RfpCopilot.Api.csproj
COPY src/ src/
COPY --from=frontend-build /app/src/RfpCopilot.Api/wwwroot src/RfpCopilot.Api/wwwroot/
RUN dotnet publish src/RfpCopilot.Api/RfpCopilot.Api.csproj -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=backend-build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "RfpCopilot.Api.dll"]
