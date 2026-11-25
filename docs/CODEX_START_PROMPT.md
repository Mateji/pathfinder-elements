# Mit Codex auf einem anderen Rechner weiterarbeiten

**Stand 25.11.2026.** M1 und M2 sind abgeschlossen. M3 ist bis einschließlich Teilnehmerkennzeichnung umgesetzt. Der zugehörige Code liegt auf `main` ab Commit `ebef34c`. Die [Roadmap](ROADMAP.md#m3--encounter-vorbereitung) enthält den jeweils aktuellen Stand und die Abnahmekriterien.

## Repository übernehmen

Klone das Repository auf dem anderen Rechner oder öffne deinen vorhandenen Checkout. Prüfe bei einem vorhandenen Checkout zuerst `git status --short` und sichere eigene Änderungen, falls welche vorliegen. Aktualisiere anschließend `main`.

```sh
git switch main
git pull --ff-only origin main
git log -5 --oneline
```

Öffne den Repository-Root in Codex. Dort liegen [`AGENTS.md`](../AGENTS.md), `compose.yaml` und die beiden Arbeitsverzeichnisse `frontend/` und `backend/`. Die Lesereihenfolge führt durch Ziele, Anforderungen, Architektur und Arbeitsregeln. [Lesereihenfolge](../README.md#lesereihenfolge)

### Prompt für die neue Codex-Session

```text
Arbeite im Repository Pathfinder Elements weiter. Lies AGENTS.md, die Übergabe
in docs/CODEX_START_PROMPT.md und die dort verlinkten Anforderungen, Entscheidungen,
offenen Fragen und Roadmap. Prüfe mit git status und git log den tatsächlichen Stand.

Beginne mit dem nächsten offenen M3-Punkt zu Bibliotheksreferenzen, lokalen
Overrides sowie dem Kopieren und Wiederverwenden vorbereiteter Encounters. Prüfe zuerst
die bereits implementierten restriktiven Fremdschlüssel und den bestehenden API- und
UI-Code. Formuliere einen kleinen Umsetzungsschritt. Kläre OQ-01 mit mir, bevor
du eine Konfliktregel für lokale Feld-Overrides umsetzt. Setze freigegebene Teile
um, führe die relevanten Tests aus und aktualisiere die Roadmap. Arbeite auf Deutsch.
```

## Umgesetzter Stand

| Bereich | Was bereits funktioniert |
| --- | --- |
| M1 · Infrastruktur | ASP.NET Core 10, EF Core und PostgreSQL 17 über Docker Compose. Migrationen beim API-Start, Liveness und Readiness, HTTPS-Proxy, private Cookie-Anmeldung für ein administrativ angelegtes Konto, CSRF-Schutz und PostgreSQL-Backup mit Restore-Test. Der Angular-Client unterstützt Deutsch und Englisch. |
| M2 · Bibliothek | Unter `/library` verwaltet ein angemeldeter GM PC- und Monstervorlagen samt Kampfwerten und geordneten Monsterangriffen. Unterstützt werden Bearbeiten, Archivieren, Wiederherstellen, Löschen und atomarer JSON-Import der Version 1. Besitzerprüfung und Versionsprüfung greifen serverseitig. |
| M3 · Zusammenstellung | Unter `/encounters` lassen sich Vorbereitungen anlegen, umbenennen und wieder öffnen. PCs werden einzeln hinzugefügt, Monster standardmäßig mit Menge 1. Jede Figur hat eine eigene Teilnehmer-ID und lässt sich einzeln entfernen. Namen, Maximal-TP und Archivstatus werden beim Lesen aus der Vorlage übernommen. |
| M3 · Kennzeichnung | Teilnehmer erhalten feste Nummern je Encounter und eine kräftige Standardfarbe. Die Palette wiederholt sich nach acht Einträgen. Jede Farbe lässt sich einzeln ändern und speichern. Entfernen hinterlässt eine Nummernlücke. Die Migration `AddEncounterParticipantMarkers` vergibt Nummern und Farben auch für Bestandsdaten. |

Archivierte Bibliotheksvorlagen fehlen in der Neuauswahl und bleiben in bestehenden Vorbereitungen nutzbar. Die restriktiven Fremdschlüssel sind in PostgreSQL vorhanden und blockieren endgültiges Löschen, solange ein Teilnehmer die Vorlage referenziert. Schreibzugriffe auf gespeicherte Vorbereitungen erfordern den aktuellen `ETag` als `If-Match` sowie Sitzung und CSRF-Token. [Datenmodell](DOMAIN_MODEL.md#vorbereitete-encounter-in-m3) · [HTTP-Verträge](../backend/README.md#http-vertrag-der-encounter-vorbereitung)

| Stelle im Code | Einstieg |
| --- | --- |
| Backend | [`Encounters/`](../backend/src/PathfinderElements.Api/Encounters), [`Library/`](../backend/src/PathfinderElements.Api/Library), [`Access/`](../backend/src/PathfinderElements.Api/Access), [`Persistence/Migrations/`](../backend/src/PathfinderElements.Api/Persistence/Migrations) |
| Frontend | [`encounters/`](../frontend/src/app/encounters), [`library/`](../frontend/src/app/library), [`i18n/`](../frontend/src/app/i18n) |
| Tests | [`LibraryApiTests.Encounters.cs`](../backend/tests/PathfinderElements.Api.IntegrationTests/LibraryApiTests.Encounters.cs), [`LibraryTemplateTests.cs`](../backend/tests/PathfinderElements.Api.IntegrationTests/LibraryTemplateTests.cs), [Compose-Persistenztest](../backend/scripts/test-persistence.sh) |

Die Code-Commits für die bisherigen M3-Schritte sind `e69a72e` für die Zusammenstellung und die Fremdschlüssel, `e611dae` für den Test stabiler Teilnehmer-IDs und `ebef34c` für Farben, Nummern und das Upgrade vorhandener Encounters. Die fachlichen Entscheidungen sind unter [D-028 bis D-031](DECISIONS.md#d-028--archivieren-und-löschen-von-bibliotheksvorlagen--25112026--f) festgehalten.

## Nächste Arbeit

Der dritte M3-Punkt umfasst Kopieren, Wiederverwenden und die Konfliktregel für lokale Overrides bei späteren Bibliotheksänderungen. Für die Konfliktregel ist **OQ-01** offen. Prüfe die bestehenden Fremdschlüssel als Ausgangspunkt und schneide die Arbeit in kleine Schritte. Start-HP, Kampfstart und erneutes Vorbereiten folgen in M3-Schritt 4. OQ-11 betrifft dabei Kampfende, Abbruch und Neustart. Der Tracker unter `/` dient derzeit als Demo. [Anforderungen](REQUIREMENTS.md#fr-02-encounter-vorbereitung-und-wiederverwendung) · [Roadmap](ROADMAP.md#m3--encounter-vorbereitung) · [Offene Fragen](OPEN_QUESTIONS.md)

## Anwendung am neuen Rechner starten

Der dokumentierte lokale Compose-Start wurde unter Linux geprüft. Für das Frontend wurde Node.js 22.17 mit npm 10.9 verwendet. Der Backend-Container bringt die .NET-10-Laufzeit mit. Für native Backend-Tests braucht der Rechner ein .NET-10-SDK. `npm ci` läuft aus `frontend/`, wo das versionierte Lockfile liegt.

1. Lege im Repository-Root für ein frisches Datenbank-Volume die lokale `.env` mit `POSTGRES_PASSWORD` an und starte Compose. Bei einem vorhandenen Volume verwende dessen Datenbankpasswort. Erst nach erfolgreichem Start lässt sich die Proxy-IP am API-Container ermitteln. [Compose einrichten](../backend/README.md#lokale-entwicklung-mit-compose)
2. Setze für das lokale HTTPS-Frontend `AUTH_TRUSTED_PROXY` auf die Verbindungsquelle aus Sicht des API-Containers. Beim getesteten Linux-Setup ist das das Gateway der Compose-Bridge. Lege ein Anwendungskonto per CLI an, wenn du mit einer frischen leeren Datenbank startest. Das Konto und sein Passwort dienen dem Browser-Login. [Proxy und HTTPS-Frontend](../README.md#bibliothek-mit-anmeldung) · [Konto einrichten](../backend/README.md#konto-einrichten-und-passwort-zurücksetzen)
3. Starte in `frontend/` mit `npm ci` und `npm run start:library`. Öffne [die Bibliothek](https://localhost:4200/library), melde dich an und wechsle im Menü zu [Encounters](https://localhost:4200/encounters). Wenn Compose einen anderen API-Port als 5080 verwendet, übergib denselben Port beim Frontend-Start als `PATHFINDER_API_PORT`.

Wenn `docker compose` wegen `POSTGRES_PASSWORD` abbricht, richte zuerst die lokale `.env` im Repository-Root ein und starte Compose. Danach zeigt `docker compose ps -q api` die Container-ID. Bei `400` von `/api/auth/csrf` über das HTTPS-Frontend prüfe `AUTH_TRUSTED_PROXY` und ob die API nach der Änderung neu gestartet wurde.

### Vorhandene lokale Daten mitnehmen

Git überträgt Quellcode, Migrationen und Dokumentation. `.env`, PostgreSQL-Volume, Sitzungsschlüssel und die Browser-Sprachwahl gehören zur jeweiligen lokalen Umgebung. Wenn du deine angelegten Vorlagen, Encounters und das Anwendungskonto mitnehmen willst, erstelle auf dem bisherigen Rechner einen PostgreSQL-Dump und stelle ihn in einem frischen Compose-Projekt auf dem Zielrechner wieder her. Der Dump enthält auch Passwort-Hashes und gehört in einen geschützten Übertragungsweg. Mit einem neuen Schlüsselvolume meldest du dich erneut an. [Backup und Restore](../backend/BACKUP_RESTORE.md)

Verwende für den wiederhergestellten Stack denselben Projektnamen bei den nachfolgenden Compose-Befehlen. `docker compose -p <Projektname> port api 8080` zeigt den Host-Port. Diesen gibst du beim Frontend-Start als `PATHFINDER_API_PORT` an, falls der Restore einen dynamischen Port verwendet.

## Verifikation

Beim letzten M3-Schritt liefen der Angular-Produktionsbuild, 13 Frontend-Tests, 15 Backend-Tests im Standardlauf und 15 Tests im isolierten Compose-Lauf erfolgreich. Der Browser-Test über HTTPS prüfte drei PCs, vier Goblins und einen Ork, Farbänderung und Wiederöffnen, stabile Nummern nach dem Entfernen sowie Deutsch und Englisch bei 320, 768 und 1280 px.

| Prüfung | Befehl |
| --- | --- |
| Frontend aus `frontend/` | `npm test` sowie `npm run build -- --configuration production` |
| Backend aus `backend/` | `dotnet test PathfinderElements.slnx --configuration Release` |
| PostgreSQL und Restore aus dem Repository-Root | `bash backend/scripts/test-persistence.sh` |

Der Produktionsbuild lädt Schriftdateien von Google Fonts. PostgreSQL-Tests werden im Backend-Standardlauf ohne Testdatenbank übersprungen und im isolierten Compose-Lauf ausgeführt. [Build, Lint und Tests](../README.md#build-lint-und-tests)
