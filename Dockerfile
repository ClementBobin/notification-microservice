# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Copy solution and build configuration files first
COPY Genkan.slnx Directory.Build.props ./

# 2. Copy all project files maintaining their directory structure
COPY src/Genkan.Shared/Genkan.Shared.csproj src/Genkan.Shared/
COPY src/Genkan.Auth/Genkan.Auth.csproj src/Genkan.Auth/
COPY src/Genkan.Gateway/Genkan.Gateway.csproj src/Genkan.Gateway/
COPY src/Genkan.Notification/Genkan.Notification.csproj src/Genkan.Notification/

# 3. Restore the entire solution
RUN dotnet restore Genkan.slnx

# 4. Copy the rest of the source code
COPY src/ src/

# 5. Publish the target project *without* --no-restore
ARG SERVICE_NAME
RUN dotnet publish src/${SERVICE_NAME}/${SERVICE_NAME}.csproj -c Release -o /app


# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG SERVICE_NAME
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENV TARGET_DLL=${SERVICE_NAME}.dll

# Echo out the evaluated DLL to verify it is not empty
ENTRYPOINT sh -c "echo 'Starting service with DLL: ${TARGET_DLL}' && dotnet ${TARGET_DLL}"