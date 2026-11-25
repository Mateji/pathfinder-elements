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

Vom Repository-Root aus `backend/scripts/test-persistence.sh` ausführen. Der Test benötigt Docker Compose, Bash, curl und OpenSSL. Er startet zwei isolierte Compose-Projekte mit zufälligem Datenbankpasswort und dynamischen API-Ports. Ein .NET-SDK-Container führt gegen die Testdatenbank den EF-Core-Integrationstest für Bibliotheksvorlagen aus. Das Skript prüft Readiness (503) und Liveness (200) während des Datenbankstopps sowie die Wiederherstellung des synthetischen Datensatzes, des Testkontos und zweier Bibliotheksvorlagen. Die Vorlagen werden nach Neuerstellung des DB-Containers und nach Restore in ein frisches Volume samt Besitzer und Kampfwerten geprüft. Die Testprojekte samt Volumes und der temporäre Dump werden beim Beenden entfernt. [Backup- und Restore-Runbook](BACKUP_RESTORE.md)

Die EF-Migration `AddLibraryTemplates` legt die Tabellen für PC- und Monstervorlagen mit Besitzer-Fremdschlüssel an. Die Migration lässt bestehende Daten und Konten bestehen. [Bibliotheksmodell](../docs/DOMAIN_MODEL.md#bibliotheksvorlagen-in-m2)

## HTTPS mit Caddy auf dem Linux-Host

Die [Caddy-Vorlage](../deploy/caddy/pathfinder.caddy) setzt voraus, dass Caddy und Docker Compose auf demselben Linux-Host laufen und Caddy den Host-Loopback erreicht. Die Vorlage leitet `/health/live` und `/api/auth/*` an die API weiter. Readiness bleibt für lokale Betriebsprüfungen am API-Port erreichbar.

1. Die Vorlage als Site-Block in die vorhandene Caddy-Konfiguration übernehmen. `pathfinder.example.com` durch den vorgesehenen DNS-Namen ersetzen. Bei abweichendem `API_PORT` auch den Upstream-Port im Site-Block anpassen.
2. Die DNS-Einträge auf den Server zeigen lassen. Caddy braucht für das hier vorgesehene automatische HTTPS erreichbare TCP-Ports 80 und 443 sowie ein persistentes, beschreibbares Datenverzeichnis für Zertifikate. [Caddy-Dokumentation zu HTTPS](https://caddyserver.com/docs/automatic-https)
3. Den Compose-Stack starten, die vollständige Caddy-Konfiguration mit `caddy validate --config /etc/caddy/Caddyfile` prüfen und Caddy über den vorhandenen Dienst neu laden. Bei einer systemd-Standardinstallation lautet der Reload-Befehl `sudo systemctl reload caddy`.
4. Von einem Client aus prüfen. HTTP soll auf HTTPS umleiten, HTTPS soll mit gültigem Zertifikat HTTP 200 liefern und der öffentliche Readiness-Pfad HTTP 404 zurückgeben.

```sh
curl -i http://pathfinder.example.com/health/live
curl -i https://pathfinder.example.com/health/live
curl -i https://pathfinder.example.com/health/ready
```

Für die Anmeldung braucht die API das HTTPS-Schema von Caddy. Setze `AUTH_TRUSTED_PROXY` in `.env` auf die einzelne Quell-IP, unter der die API Caddys Verbindung sieht. Bei einem nativen Linux-Host mit der Compose-Bridge und dem Loopback-Upstream ist das das Gateway der Bridge. Die folgende Abfrage zeigt die Adresse des laufenden Stacks.

```sh
docker inspect --format '{{range .NetworkSettings.Networks}}{{.Gateway}}{{end}}' "$(docker compose ps -q api)"
```

Nach einer Änderung von `.env` übernimmt `docker compose up -d api` den Wert. Nach einer Neuerstellung des Compose-Netzes die Gateway-Adresse erneut prüfen. Die tatsächliche Domain und der Caddy-Betriebsort sind in [OQ-15](../docs/OPEN_QUESTIONS.md) offen.

## Konto einrichten und Passwort zurücksetzen

Nach dem Start des API-Containers und der Migration die folgenden Befehle im Repository-Root in Bash ausführen. Identity verlangt mindestens zwölf Zeichen, darunter Groß- und Kleinbuchstaben, eine Ziffer und ein Sonderzeichen. `read -s` liest das Passwort ohne Anzeige, die CLI erhält es über stdin.

```bash
read -rsp 'Passwort für das neue Konto: ' pathfinder_password
printf '\n'
printf '%s\n' "$pathfinder_password" | docker compose exec -T api dotnet PathfinderElements.Api.dll account create spielleitung
unset pathfinder_password
```

Für einen Reset denselben Ablauf mit `account reset-password` anstelle von `account create spielleitung` ausführen. Der Reset widerruft die bestehenden Sitzungen und hebt eine etwaige Kontosperre auf. Das benannte Volume `auth_keys` enthält die Schlüssel, mit denen Identity die Sitzungs-Cookies schützt. Erhalte das Volume über Container-Neuerstellungen hinweg.

### HTTP-Vertrag der Anmeldung

Alle Pfade liegen unter `/api/auth` und benötigen HTTPS. Cookies werden durch den Browser für dieselbe Origin mitgeführt.

| Methode und Pfad | Verhalten |
| --- | --- |
| `GET /csrf` | Gibt `{ "token": "…" }` zurück und setzt das CSRF-Cookie. |
| `POST /login` | Nimmt JSON mit `username` und `password` sowie den Token im Header `X-CSRF-TOKEN` entgegen. Antwortet bei Erfolg mit 204 und setzt das Sitzungs-Cookie. Abgelehnte Zugangsdaten ergeben 401. |
| `GET /session` | Liefert `userId` und `username` für die aktive Sitzung, andernfalls 401. |
| `POST /logout` | Löscht das Sitzungs-Cookie und antwortet mit 204. Vorher mit aktiver Sitzung einen neuen CSRF-Token abrufen und im Header mitsenden. |

Ein fehlender oder ungültiger CSRF-Token ergibt bei den POST-Endpunkten HTTP 400. Fünf falsche Passworteingaben sperren das Konto für 15 Minuten. Die API begrenzt zusätzlich Login- und Token-Anfragen und liefert bei Überschreitung HTTP 429. Die Angular-Demo verwendet synthetische Daten und hat derzeit keine Login-Maske.

## Lokal bauen und testen

Im Verzeichnis `backend/` mit dem .NET-10-SDK ausführen:

```sh
dotnet build PathfinderElements.slnx --configuration Release
dotnet test PathfinderElements.slnx --configuration Release --no-build
```

Die API-Integrationstests starten den Einstiegspunkt mit `WebApplicationFactory` und prüfen Liveness, Readiness ohne DB-Konfiguration, Anmeldung, Sitzungsablauf, Passwort-Reset und CSRF-Schutz. Die Modelltests prüfen Validierung und den Abgleich des PostgreSQL-Modells mit dem Migrations-Snapshot. Der Test für EF-Schreib-/Lesezugriffe und Datenbank-Constraints läuft im isolierten Compose-Test.

## API starten

```sh
dotnet run --project src/PathfinderElements.Api -- --urls http://127.0.0.1:5080
```

Aus einem zweiten Terminal zeigt `curl -i http://127.0.0.1:5080/health/live` den Statuscode.
