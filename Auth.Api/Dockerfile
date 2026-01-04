FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared projects first
COPY ["src/Shared/", "Shared/"]

# Copy Auth projects
COPY ["src/Auth/Auth.Domain/", "Auth/Auth.Domain/"]
COPY ["src/Auth/Auth.Data/", "Auth/Auth.Data/"]
COPY ["src/Auth/Auth.Api/", "Auth/Auth.Api/"]

# Restore
RUN dotnet restore "Auth/Auth.Api/Auth.Api.csproj"

# Build
RUN dotnet build "Auth/Auth.Api/Auth.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Auth/Auth.Api/Auth.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Auth.Api.dll"]