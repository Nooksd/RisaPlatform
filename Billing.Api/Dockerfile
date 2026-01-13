FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first for better caching
COPY Shared/Shared.Kernel/Shared.Kernel.csproj Shared/Shared.Kernel/
COPY Shared/Shared.Contracts/Shared.Contracts.csproj Shared/Shared.Contracts/
COPY Shared/Shared.BuildingBlocks/Shared.BuildingBlocks.csproj Shared/Shared.BuildingBlocks/
COPY Services/Billing/Billing.Domain/Billing.Domain.csproj Services/Billing/Billing.Domain/
COPY Services/Billing/Billing.Data/Billing.Data.csproj Services/Billing/Billing.Data/
COPY Services/Billing/Billing.Api/Billing.Api.csproj Services/Billing/Billing.Api/

RUN dotnet restore Services/Billing/Billing.Api/Billing.Api.csproj

# Copy everything and build
COPY . .
RUN dotnet publish Services/Billing/Billing.Api/Billing.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5002
EXPOSE 5002

ENTRYPOINT ["dotnet", "Billing.Api.dll"]
