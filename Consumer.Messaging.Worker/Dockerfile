# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

# Restore dependencies before copying source (layer cache)
COPY Consumer.Messaging.Worker/Consumer.Messaging.Worker.csproj Consumer.Messaging.Worker/
RUN dotnet restore Consumer.Messaging.Worker/Consumer.Messaging.Worker.csproj

COPY Consumer.Messaging.Worker/ Consumer.Messaging.Worker/
RUN dotnet publish Consumer.Messaging.Worker/Consumer.Messaging.Worker.csproj \
    -c Release -o /app --no-restore

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=builder /app .

ENTRYPOINT ["dotnet", "Consumer.Messaging.Worker.dll"]
