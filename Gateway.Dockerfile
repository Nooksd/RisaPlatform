FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Gateway/Gateway.Api/Gateway.Api.csproj", "Gateway/Gateway.Api/"]
COPY ["src/Shared/Shared.Kernel/Shared.Kernel.csproj", "Shared/Shared.Kernel/"]
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.BuildingBlocks/Shared.BuildingBlocks.csproj", "Shared/Shared.BuildingBlocks/"]

RUN dotnet restore "Gateway/Gateway.Api/Gateway.Api.csproj"

COPY src/Gateway/Gateway.Api/ Gateway/Gateway.Api/
COPY src/Shared/ Shared/

WORKDIR "/src/Gateway/Gateway.Api"
RUN dotnet build "Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]