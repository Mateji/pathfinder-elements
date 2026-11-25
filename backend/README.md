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

Vom Repository-Root aus `backend/scripts/test-persistence.sh` ausführen. Der Test benötigt Docker Compose, Bash, curl und OpenSSL. Er startet zwei isolierte Compose-Projekte mit zufälligem Datenbankpasswort und dynamischen API-Ports. Ein .NET-SDK-Container führt den EF-Integrationstest gegen die Testdatenbank aus. HTTP-Integrationstests und Test des Upgrades von bestehenden Vorlagen erhalten jeweils eine eigene Datenbank im selben PostgreSQL-Container. Das Skript prüft Readiness (503) und Liveness (200) während des Datenbankstopps sowie die Wiederherstellung des synthetischen Datensatzes, des Testkontos und zweier Bibliotheksvorlagen. Die Vorlagen werden nach Neuerstellung des DB-Containers und nach Restore in ein frisches Volume samt Besitzer, Kampfwerten, Archivstatus und Version geprüft. Die Testprojekte samt Volumes und der temporäre Dump werden beim Beenden entfernt. [Backup- und Restore-Runbook](BACKUP_RESTORE.md)

Die EF-Migration `AddLibraryTemplates` legt die Tabellen für PC- und Monstervorlagen mit Besitzer-Fremdschlüssel an. `AddLibraryTemplateLifecycle` ergänzt Archivstatus und Version. `AddMonsterAttackSequences` ergänzt die Angriffssequenzen. Bestehende Monstervorlagen erhalten eine leere Sequenzliste. `AddLibraryImportReceipts` ergänzt die kontobezogenen Importbelege. [Bibliotheksmodell](../docs/DOMAIN_MODEL.md#bibliotheksvorlagen-in-m2)

## HTTPS mit Caddy auf dem Linux-Host

Die Caddy-Vorlage setzt voraus, dass Caddy und Docker Compose auf demselben Linux-Host laufen und Caddy den Host-Loopback erreicht. Die Vorlage leitet `/health/live`, `/api/auth/*` und die Bibliotheksrouten unter `/api/characters`, `/api/monsters` und `/api/library/import` weiter. Readiness bleibt für lokale Betriebsprüfungen am API-Port erreichbar. [Caddy-Vorlage](../deploy/caddy/pathfinder.caddy)

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

## HTTP-Vertrag der Bibliothek

Die Basisrouten sind `/api/characters` für PC-Vorlagen und `/api/monsters` für Monstervorlagen. Alle Aufrufe benötigen HTTPS und das Cookie einer gültigen Sitzung. Nach dem Login liefert `GET /api/auth/csrf` den sitzungsgebundenen Token für den Header `X-CSRF-TOKEN` bei `POST`, `PUT` und `DELETE`.

| Methode und Pfad relativ zur Basisroute | Ergebnis bei Erfolg |
| --- | --- |
| `GET` | 200 mit aktiven Vorlagen des Kontos. `?includeArchived=true` nimmt archivierte Vorlagen hinzu. Die Liste ist nach Name und ID sortiert. |
| `GET /{id}` | 200 mit der eigenen Vorlage und ihrer aktuellen Version im `ETag`-Header, auch im Archiv. |
| `POST` | 201 mit aktiver Vorlage, `Location` der Detailroute und `ETag`. |
| `PUT /{id}` | 200 mit aktualisiertem Namen, Kampfwerten und neuem `ETag`. |
| `PUT /{id}/archive` | 200 mit Vorlage und neuem `ETag`. Der Body `{"isArchived":true}` archiviert, `false` stellt die Vorlage wieder her. |
| `DELETE /{id}` | 204 nach endgültigem Löschen einer eigenen Vorlage ohne vorbereitete Encounter-Verweise. |

`POST` und `PUT /{id}` verwenden diese Form. Antworten ergänzen `id` und `isArchived`. Die ID und der Besitzer stammen bei der Anlage vom Server. Schreib-Requests mit zusätzlichen Feldern wie `ownerUserId` ergeben 400.

```json
{
  "name": "Testfigur",
  "statistics": {
    "maxHitPoints": 24,
    "initiativeModifier": -1,
    "armorClass": 17,
    "touchArmorClass": 9,
    "flatFootedArmorClass": 18,
    "fortitudeSave": 4,
    "reflexSave": -2,
    "willSave": 0
  }
}
```

Der Name muss mindestens ein Zeichen außer Leerraum enthalten, darf höchstens 256 Zeichen lang sein und kein Nullzeichen enthalten. Alle acht Kampfwerte sind als Ganzzahlen Pflicht, und Max-HP muss positiv sein. Inhaltliche Validierungsfehler liefern HTTP 400 mit Feldpfaden und Fehlercodes in `errors`. Fehlende oder falsch typisierte Pflichtfelder im Statistikobjekt ergeben HTTP 400 beim Lesen der Anfrage.

Für `PUT` und `DELETE` sendet der Client den aktuellen `ETag` einschließlich Anführungszeichen als einzelnen `If-Match`-Wert. Ein fehlender Wert ergibt 428. Ein abweichender Wert oder ein konkurrierender Schreibzugriff ergibt 412. Eine fehlende Sitzung ergibt 401. Eine fremde oder fehlende ID ergibt 404. Eine durch einen Fremdschlüssel referenzierte Vorlage ergibt beim Löschen 409 mit `code: templateReferenced`. M3 muss die vorbereiteten Encounter-Referenzen mit dieser Datenbanksperre verknüpfen. [HTTP-Semantik von If-Match](https://www.rfc-editor.org/rfc/rfc9110.html#name-if-match) · [Archivierungsregel](../docs/DECISIONS.md#d-028--archivieren-und-löschen-von-bibliotheksvorlagen--25112026--f)

### Angriffssequenzen

Monstervorlagen unterstützen das zusätzliche Feld `attackSequences` im Body von `POST` und `PUT /{id}`. Beim Ändern erhält eine ausgelassene Angabe oder `null` die gespeicherten Sequenzen. Ein Array ersetzt sie vollständig. Mit `[]` lassen sich alle Sequenzen entfernen. Die Detailantwort der Monstervorlage liefert das Feld einschließlich der Reihenfolge.

```json
{
  "attackSequences": [
    {
      "name": "Einzelangriff",
      "attacks": [
        {
          "name": "Biss",
          "mode": "melee",
          "isNatural": true,
          "attackBonus": 7,
          "targetsTouchArmorClass": false,
          "criticalThreatMinimum": 19,
          "criticalMultiplier": 2,
          "damage": [
            { "diceCount": 1, "dieSides": 6, "modifier": 4, "damageType": "piercing", "multipliesOnCritical": true },
            { "diceCount": 1, "dieSides": 4, "modifier": 0, "damageType": "fire", "multipliesOnCritical": false }
          ],
          "notes": "Gift separat abwickeln."
        }
      ]
    }
  ]
}
```

Eine Vorlage erlaubt bis zu 32 Sequenzen, jede Sequenz 1 bis 32 Angriffe. `mode` ist `melee` oder `ranged`. Kritgrenze und Multiplikator liegen in den Bereichen 2 bis 20 und 2 bis 10. `damage` ist eine Liste mit bis zu 16 Anteilen. Für einen festen Schadensanteil setzt der Client `diceCount` und `dieSides` auf 0 und gibt den Wert als `modifier` an. Für Würfel gelten 1 bis 100 Würfel mit jeweils 2 bis 100 Seiten. `notes` ist optional und erlaubt bis zu 1024 Zeichen.

### Versionierter JSON-Import

`POST /api/library/import` akzeptiert ein UTF-8-JSON-Dokument mit `Content-Type: application/json`. Der Aufruf braucht HTTPS, das Sitzungscookie und den nach der Anmeldung geholten Header `X-CSRF-TOKEN`. Ein Dokument der Version 1 enthält insgesamt eine bis 250 Vorlagen und darf bis zu 1 MiB groß sein.

```json
{
  "schemaVersion": 1,
  "characters": [
    {
      "name": "Testfigur",
      "statistics": {
        "maxHitPoints": 24, "initiativeModifier": -1,
        "armorClass": 17, "touchArmorClass": 9, "flatFootedArmorClass": 18,
        "fortitudeSave": 4, "reflexSave": -2, "willSave": 0
      }
    }
  ],
  "monsters": [
    {
      "name": "Testmonster",
      "statistics": {
        "maxHitPoints": 12, "initiativeModifier": 2,
        "armorClass": 15, "touchArmorClass": 12, "flatFootedArmorClass": 13,
        "fortitudeSave": 3, "reflexSave": 2, "willSave": -1
      },
      "attackSequences": []
    }
  ]
}
```

Beide Listen sind optional. Die Felder eines Eintrags entsprechen den jeweiligen Schreib-Anfragen der Bibliothek. Feldnamen sind in der Schreibweise des Beispiels anzugeben. Unbekannte oder doppelte JSON-Felder ergeben einen Validierungsfehler. Beim Erfolg liefert HTTP 201 die Felder `schemaVersion`, `characterIds` und `monsterIds`. Die IDs stehen in der Reihenfolge der jeweiligen Eingabeliste. Der Server vergibt neue IDs und ordnet die Vorlagen dem angemeldeten Konto zu.

Der Import verwendet einen Fingerabdruck der normalisierten Daten. Leerraum und die Reihenfolge von JSON-Feldern spielen dabei keine Rolle. Optionale Listen gelten bei Auslassung, `null` und `[]` als leer. Die Reihenfolge der Einträge innerhalb der Arrays ist Teil des Dokuments. Ein erfolgreicher Import bleibt für dieses Konto vermerkt, auch wenn einzelne importierte Vorlagen später geändert oder gelöscht werden. Gleiche Namen sind erlaubt. [Entscheidung zum Bibliotheksimport](../docs/DECISIONS.md#d-030--versionierter-bibliotheksimport--25112026--f)

| Status | Bedeutung |
| --- | --- |
| 400 | Ungültiges oder nicht unterstütztes Dokument. Die Antwort enthält Feldpfade und Fehlercodes unter `errors`, etwa `schemaVersion → unsupportedVersion` oder `monsters[0].name → required`. Ein fehlender oder ungültiger CSRF-Token ergibt ebenfalls 400. |
| 401 | Keine gültige Sitzung. |
| 409 | Dasselbe Dokument wurde für dieses Konto bereits importiert. Die Antwort enthält `code: importAlreadyProcessed`. |
| 413 | Die Anfrage überschreitet 1 MiB. |
| 415 | Der Medientyp ist kein JSON. |

Die Vorlagen und der Importbeleg werden erst nach erfolgreicher Validierung gemeinsam gespeichert. Bei 400 oder 409 entstehen keine zusätzlichen Vorlagen aus dem Dokument.

## Lokal bauen und testen

Im Verzeichnis `backend/` mit dem .NET-10-SDK ausführen:

```sh
dotnet build PathfinderElements.slnx --configuration Release
dotnet test PathfinderElements.slnx --configuration Release --no-build
```

Die Integrationstests starten den Einstiegspunkt mit `WebApplicationFactory`. Der Standardlauf prüft Healthchecks, Anmeldung, Sitzungsablauf, Passwort-Reset, Validierung und den Abgleich des PostgreSQL-Modells mit dem Migrations-Snapshot. Der isolierte Compose-Test führt zusätzlich EF-Schreib-/Lesezugriffe und die authentifizierten Bibliotheksabläufe mit PostgreSQL aus.

## API starten

```sh
dotnet run --project src/PathfinderElements.Api -- --urls http://127.0.0.1:5080
```

Aus einem zweiten Terminal zeigt `curl -i http://127.0.0.1:5080/health/live` den Statuscode.
