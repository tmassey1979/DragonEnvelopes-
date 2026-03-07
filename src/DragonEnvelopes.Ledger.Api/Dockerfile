FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY DragonEnvelopes.sln ./
COPY src/DragonEnvelopes.Domain/DragonEnvelopes.Domain.csproj src/DragonEnvelopes.Domain/
COPY src/DragonEnvelopes.Application/DragonEnvelopes.Application.csproj src/DragonEnvelopes.Application/
COPY src/DragonEnvelopes.Infrastructure/DragonEnvelopes.Infrastructure.csproj src/DragonEnvelopes.Infrastructure/
COPY src/DragonEnvelopes.Contracts/DragonEnvelopes.Contracts.csproj src/DragonEnvelopes.Contracts/
COPY src/DragonEnvelopes.ProviderClients/DragonEnvelopes.ProviderClients.csproj src/DragonEnvelopes.ProviderClients/
COPY src/DragonEnvelopes.Ledger.Api/DragonEnvelopes.Ledger.Api.csproj src/DragonEnvelopes.Ledger.Api/

RUN dotnet restore src/DragonEnvelopes.Ledger.Api/DragonEnvelopes.Ledger.Api.csproj

FROM restore AS publish
COPY . .
RUN dotnet publish src/DragonEnvelopes.Ledger.Api/DragonEnvelopes.Ledger.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
ARG APP_UID=10001
ARG APP_GID=10001

RUN groupadd --gid ${APP_GID} dragonenvelopes \
    && useradd --uid ${APP_UID} --gid ${APP_GID} --create-home --shell /usr/sbin/nologin dragonenvelopes \
    && apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .
RUN chown -R dragonenvelopes:dragonenvelopes /app

USER dragonenvelopes
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "DragonEnvelopes.Ledger.Api.dll"]
