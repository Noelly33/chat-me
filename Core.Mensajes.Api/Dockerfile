# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

# Restore dependencies before copying source (layer cache)
COPY Core.Mensajes.Api/Core.Mensajes.Api.csproj Core.Mensajes.Api/
RUN dotnet restore Core.Mensajes.Api/Core.Mensajes.Api.csproj

COPY Core.Mensajes.Api/ Core.Mensajes.Api/
RUN dotnet publish Core.Mensajes.Api/Core.Mensajes.Api.csproj \
    -c Release -o /app --no-restore

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=builder /app .

ENV ASPNETCORE_URLS=http://+:5017
EXPOSE 5017

ENTRYPOINT ["dotnet", "Core.Mensajes.Api.dll"]
