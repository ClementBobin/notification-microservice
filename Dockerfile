# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Copy solution and build configuration files first
COPY Genkan.slnx Directory.Build.props ./

# 2. Copy all project files maintaining their directory structure (restore layer cache)
COPY src/Genkan.Shared/Genkan.Shared.csproj src/Genkan.Shared/
COPY src/Genkan.Gateway/Genkan.Gateway.csproj src/Genkan.Gateway/

COPY src/Genkan.Auth/Genkan.Auth.Domain/Genkan.Auth.Domain.csproj src/Genkan.Auth/Genkan.Auth.Domain/
COPY src/Genkan.Auth/Genkan.Auth.Data/Genkan.Auth.Data.csproj src/Genkan.Auth/Genkan.Auth.Data/
COPY src/Genkan.Auth/Genkan.Auth.Business/Genkan.Auth.Business.csproj src/Genkan.Auth/Genkan.Auth.Business/
COPY src/Genkan.Auth/Genkan.Auth.Api/Genkan.Auth.Api.csproj src/Genkan.Auth/Genkan.Auth.Api/

COPY src/Genkan.Notification/Genkan.Notification.Domain/Genkan.Notification.Domain.csproj src/Genkan.Notification/Genkan.Notification.Domain/
COPY src/Genkan.Notification/Genkan.Notification.Data/Genkan.Notification.Data.csproj src/Genkan.Notification/Genkan.Notification.Data/
COPY src/Genkan.Notification/Genkan.Notification.Business/Genkan.Notification.Business.csproj src/Genkan.Notification/Genkan.Notification.Business/
COPY src/Genkan.Notification/Genkan.Notification.Messaging/Genkan.Notification.Messaging.csproj src/Genkan.Notification/Genkan.Notification.Messaging/
COPY src/Genkan.Notification/Genkan.Notification.Api/Genkan.Notification.Api.csproj src/Genkan.Notification/Genkan.Notification.Api/

# 3. Restore the entire solution
RUN dotnet restore Genkan.slnx

# 4. Copy the rest of the source code
COPY src/ src/

# 5. Publish the target project (path relative to src/, e.g. Genkan.Auth/Genkan.Auth.Api/Genkan.Auth.Api.csproj)
ARG SERVICE_PROJECT
RUN dotnet publish src/${SERVICE_PROJECT} -c Release -o /app


# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG SERVICE_PROJECT
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# The entry DLL is the project file name without ".csproj"
ENV SERVICE_PROJECT=${SERVICE_PROJECT}
ENTRYPOINT ["sh", "-c", "DLL=\"$(basename \"$SERVICE_PROJECT\" .csproj).dll\"; echo \"Starting service with DLL: $DLL\"; exec dotnet \"$DLL\""]
