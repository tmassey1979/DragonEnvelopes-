# 2026-03-05 - grafana loki observability

## Summary
- Added opt-in backend log shipping to Loki using Serilog Loki sink flags.
- Added local observability profile services in compose: Loki, Promtail, and Grafana.
- Provisioned Grafana Loki datasource and a default API logs dashboard.
- Added README setup, saved queries, verification flow, and troubleshooting steps.

## Files Changed
- .env.example
- README.md
- docker-compose.yml
- src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj
- src/DragonEnvelopes.Api/Program.cs
- src/DragonEnvelopes.Api/CrossCutting/Errors/GlobalExceptionHandler.cs
- src/DragonEnvelopes.Api/appsettings.json
- src/DragonEnvelopes.Api/appsettings.Development.json
- infrastructure/observability/loki/config.yaml
- infrastructure/observability/promtail/config.yml
- infrastructure/observability/grafana/provisioning/datasources/loki.yml
- infrastructure/observability/grafana/provisioning/dashboards/dashboards.yml
- infrastructure/observability/grafana/dashboards/dragonenvelopes-api-logs.json

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `docker compose --profile observability config`
- `OBSERVABILITY_ENABLE_LOKI_SINK=true docker compose --profile observability up -d --build`
- `curl -s -o NUL -w "%{http_code}" http://localhost:18088/health/live` -> 200
- `curl -s -o NUL -w "%{http_code}" http://localhost:18088/api/v1/weatherforecast` -> 200
- `curl -s -o NUL -w "%{http_code}" http://localhost:18088/api/v1/does-not-exist` -> 404
- `curl http://localhost:3100/loki/api/v1/label/application/values` -> contains `dragonenvelopes-api`
- `curl http://localhost:3100/loki/api/v1/label/StatusCode/values` -> contains `200`, `404`
