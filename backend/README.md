# Backend

Die ASP.NET Core 10 Web API stellt einen Liveness-Endpunkt bereit. `GET /health/live` antwortet mit HTTP 200 und leerem Body, sobald der API-Host Anfragen verarbeiten kann.

`GET /health/ready` prüft über EF Core, ob eine PostgreSQL-Verbindung möglich ist. Erfolg ergibt HTTP 200, eine fehlende Konfiguration oder nicht erreichbare Datenbank HTTP 503. Beide Antworten haben einen leeren Body.

## Lokale Entwicklung mit Compose

Im Repository-Root für ein neues Volume einmalig ein Passwort in der ignorierten `.env` hinterlegen. Der folgende Befehl hängt den Eintrag an eine bestehende Datei an. Vorher prüfen, ob sie bereits einen Eintrag für `POSTGRES_PASSWORD` enthält.

```sh
(umask 077 && printf 'POSTGRES_PASSWORD=%s\n' "$(openssl rand -hex 32)" >> .env)
docker compose up --build -d
curl -i http://127.0.0.1:5080/health/ready
```

Compose startet PostgreSQL 17, wartet auf dessen Healthcheck und startet die API. Die API wendet offene EF-Core-Migrationen vor dem ersten HTTP-Request an. Der API-Port ist an Loopback gebunden. PostgreSQL ist über das Compose-Netz erreichbar. Die erste Migration legt die technische Tabelle `application_metadata` mit einem eindeutigen Textschlüssel und einem Textwert an. Der Abnahmetest verwendet sie für einen synthetischen Datensatz.

`docker compose down` beendet die Entwicklungsumgebung und erhält das benannte Volume samt Passwort des initialisierten Datenbankbenutzers. Beim nächsten Start dasselbe Passwort verwenden.

### Automatischer Persistenztest

Vom Repository-Root aus `backend/scripts/test-persistence.sh` ausführen. Der Test benötigt Docker Compose, Bash, curl und OpenSSL. Er erstellt ein eigenes Compose-Projekt mit zufälligem Passwort und dynamischem API-Port. Er prüft die migrierte Datenbank, schreibt einen Datensatz, stoppt PostgreSQL und vergleicht Readiness (503) und Liveness (200). Anschließend erzeugt er den DB-Container neu und prüft Readiness und gespeicherten Wert. Das Testprojekt und sein Volume werden beim Beenden entfernt.

## HTTPS mit Caddy auf dem Linux-Host

Die [Caddy-Vorlage](../deploy/caddy/pathfinder.caddy) setzt voraus, dass Caddy und Docker Compose auf demselben Linux-Host laufen und Caddy den Host-Loopback erreicht. Die Vorlage leitet `/health/live` an die API weiter. Readiness bleibt für lokale Betriebsprüfungen am API-Port erreichbar.

1. Die Vorlage als Site-Block in die vorhandene Caddy-Konfiguration übernehmen. `pathfinder.example.com` durch den vorgesehenen DNS-Namen ersetzen. Bei abweichendem `API_PORT` auch den Upstream-Port im Site-Block anpassen.
2. Die DNS-Einträge auf den Server zeigen lassen. Caddy braucht für das hier vorgesehene automatische HTTPS erreichbare TCP-Ports 80 und 443 sowie ein persistentes, beschreibbares Datenverzeichnis für Zertifikate. [Caddy-Dokumentation zu HTTPS](https://caddyserver.com/docs/automatic-https)
3. Den Compose-Stack starten, die vollständige Caddy-Konfiguration mit `caddy validate --config /etc/caddy/Caddyfile` prüfen und Caddy über den vorhandenen Dienst neu laden. Bei einer systemd-Standardinstallation lautet der Reload-Befehl `sudo systemctl reload caddy`.
4. Von einem Client aus prüfen. HTTP soll auf HTTPS umleiten, HTTPS soll mit gültigem Zertifikat HTTP 200 liefern und der öffentliche Readiness-Pfad HTTP 404 zurückgeben.

```sh
curl -i http://pathfinder.example.com/health/live
curl -i https://pathfinder.example.com/health/live
curl -i https://pathfinder.example.com/health/ready
```

Die tatsächliche Domain und der Caddy-Betriebsort sind in [OQ-15](../docs/OPEN_QUESTIONS.md) offen. Die Freigabe privater API-Routen und die vertrauenswürdige Auswertung von Forwarded Headers gehören zur folgenden Login-Integration.

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
