FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY EndToEnd.csproj .
RUN dotnet restore
COPY . .
RUN dotnet build -c Release

ENTRYPOINT ["dotnet", "test", "-c", "Release", "--logger", "console;verbosity=detailed", "--no-build"]
