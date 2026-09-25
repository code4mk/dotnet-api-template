# Production image: the API and the background job worker in one container, run by supervisor
# (docker/supervisor/supervisord.conf). Build from the repository root:
#   docker build -f docker/Dockerfile -t dotnetapitemplate-slug-api .
#
# The same image can also run a single role without supervisor, e.g. separate api and worker containers:
#   docker run ... --entrypoint dotnet -e APP_ROLE=worker dotnetapitemplate-slug-api DotnetApiTemplate.Api.dll

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

# supervisor runs the api and worker processes (it brings python3, also used by the health check).
RUN apt-get update \
    && apt-get install -y --no-install-recommends supervisor \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .
COPY docker/supervisor/supervisord.conf /etc/supervisor/supervisord.conf
COPY docker/supervisor/exit-on-fatal.py /etc/supervisor/exit-on-fatal.py

# Non-root user built into the .NET images (named "app").
USER $APP_UID
EXPOSE 8080

# Healthy only when both processes answer: api on 8080, worker on 8081.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD python3 -c "import urllib.request as u; [u.urlopen(f'http://127.0.0.1:{p}/health', timeout=4) for p in (8080, 8081)]" || exit 1

ENTRYPOINT ["supervisord", "-c", "/etc/supervisor/supervisord.conf"]
