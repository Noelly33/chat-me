# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

# Restore dependencies before copying source (layer cache)
COPY SideCar.Auth.Api/SideCar.Auth.Api.csproj SideCar.Auth.Api/
RUN dotnet restore SideCar.Auth.Api/SideCar.Auth.Api.csproj

COPY SideCar.Auth.Api/ SideCar.Auth.Api/
RUN dotnet publish SideCar.Auth.Api/SideCar.Auth.Api.csproj \
    -c Release -o /app --no-restore

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=builder /app .

ENV ASPNETCORE_URLS=http://+:5016
EXPOSE 5016

ENTRYPOINT ["dotnet", "SideCar.Auth.Api.dll"]
