# Build from the repository root:
#   docker build -f docker/Dockerfile -t dotnetapitemplate-slug-api .

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached until project files change).
COPY Directory.Build.props Directory.Packages.props ./
COPY src/DotnetApiTemplate.Api/DotnetApiTemplate.Api.csproj src/DotnetApiTemplate.Api/
RUN dotnet restore src/DotnetApiTemplate.Api/DotnetApiTemplate.Api.csproj

# Build and publish.
COPY src/ src/
RUN dotnet publish src/DotnetApiTemplate.Api/DotnetApiTemplate.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

# Non-root user built into the .NET images.
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "DotnetApiTemplate.Api.dll"]
