# Backend

Die ASP.NET Core 10 Web API stellt einen Liveness-Endpunkt bereit. `GET /health/live` antwortet mit HTTP 200 und leerem Body, sobald der API-Host Anfragen verarbeiten kann.

## Lokal bauen und testen

Im Verzeichnis `backend/` mit dem .NET-10-SDK ausführen:

```sh
dotnet build PathfinderElements.slnx --configuration Release
dotnet test PathfinderElements.slnx --configuration Release --no-build
```

Der Integrationstest startet den API-Einstiegspunkt mit `WebApplicationFactory` und prüft die HTTP-Antwort.

## API starten

```sh
dotnet run --project src/PathfinderElements.Api -- --urls http://127.0.0.1:5080
```

Aus einem zweiten Terminal zeigt `curl -i http://127.0.0.1:5080/health/live` den Statuscode.
