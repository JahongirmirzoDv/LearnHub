# syntax=docker/dockerfile:1
# LearnHub – ASP.NET Core MVC image for Railway (and any other container host).
#
# Railway terminates TLS at its edge proxy and forwards plain HTTP on the port named by PORT, so the application
# reads that variable at start-up (see UsePlatformPort) and listens on 0.0.0.0. Persistent state (the SQLite file,
# the Data Protection key ring and admin uploads) is written under /data, which is where the Railway volume is
# mounted.

# ---------------------------------------------------------------- build ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# The solution-level build files are copied first so that restoring is cached while only application code changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/LearnHub/LearnHub.csproj src/LearnHub/
RUN dotnet restore src/LearnHub/LearnHub.csproj

COPY src/LearnHub/ src/LearnHub/
RUN dotnet publish src/LearnHub/LearnHub.csproj \
        --configuration Release \
        --no-restore \
        --output /app

# --------------------------------------------------------------- runtime --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Railway supplies PORT. The value below is only a fallback for running the image locally.
# The container keeps the image's default user because a Railway volume is mounted root-owned; /data is created
# on first use by the application itself.
ENV ASPNETCORE_ENVIRONMENT=Production \
    PORT=8080 \
    DATABASE_CONNECTION_STRING="Data Source=/data/learnhub.db" \
    DataProtection__KeysPath=/data/keys \
    Storage__RootPath=/data/storage

EXPOSE 8080

# Migrations, roles, the administrator and the demo catalogue are applied at start-up, so the image needs no
# separate migration step.
ENTRYPOINT ["dotnet", "LearnHub.dll"]
