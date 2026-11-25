# Pathfinder Elements — Projektspezifikation v0.2

**Stand:** 19.11.2026 · **Status:** Arbeitsfassung, fachliche Anforderungen weitgehend abgestimmt · **Regelwerk:** Pathfinder First Edition (PF1E)

Der Initiative-Tracker und die Bibliothek liegen als Angular-21-Anwendung unter `frontend/`. Unter `backend/` liegt die ASP.NET Core 10 Web API. Die Spezifikation liegt unter `docs/`. Für die lokale Entwicklung startet Docker Compose die API und PostgreSQL. [Backend starten und testen](backend/README.md)

Für einen Rechnerwechsel oder einen neuen Codex-Chat steht die Übergabe mit aktuellem M3-Stand, Startprompt und Hinweisen zur Übernahme lokaler Daten bereit. [Mit Codex weiterarbeiten](docs/CODEX_START_PROMPT.md)

## Frontend lokal starten

Im Repository-Root beginnen und den Entwicklungsserver starten:

```sh
cd frontend
npm ci
npm start
```

Der Initiative Tracker ist unter `http://localhost:4200/` erreichbar. Der Produktionsbuild läuft aus `frontend/` mit `npm run build -- --configuration production`.

Im Tracker lässt sich die UI-Sprache direkt zwischen Deutsch und Englisch umschalten. Die Auswahl bleibt in diesem Browser gespeichert. Ohne gespeicherte Auswahl startet der Tracker in der ersten unterstützten Browsersprache, sonst auf Englisch.

Neue UI-Texte in den DE/EN-Sprachkatalogen unter `frontend/src/app/i18n/` ergänzen. [Lokalisierung](docs/DECISIONS.md#d-027--ui-sprache-zur-laufzeit--25112026--v)

### Bibliothek mit Anmeldung

Starte zuerst API und PostgreSQL im Repository-Root. Bei einem frischen Stack legst du vorher `POSTGRES_PASSWORD` in der lokalen `.env` an. Anschließend lässt sich die Verbindungsquelle des HTTPS-Proxys für die API eintragen. Unter Linux mit Compose-Bridge ermittelst du das Gateway am laufenden API-Container. [Compose starten](backend/README.md#lokale-entwicklung-mit-compose)

```bash
(
    set -eu
    api_container=$(docker compose ps -q api)
    if [ -z "$api_container" ]; then
        printf '%s\n' 'Starte zuerst den Compose-Dienst api.' >&2
        exit 1
    fi
    docker inspect --format '{{range .NetworkSettings.Networks}}{{.Gateway}}{{end}}' "$api_container"
)
```

Trage die ausgegebene IP als `AUTH_TRUSTED_PROXY` in `.env` ein. `docker compose up -d api` übernimmt den Wert. Prüfe die IP erneut, wenn du das Compose-Netz neu erstellst. Bei einer frischen Datenbank legst du nun das Anwendungskonto für die Browser-Anmeldung an. [Konto einrichten](backend/README.md#konto-einrichten-und-passwort-zurücksetzen)

Starte anschließend aus `frontend/` die HTTPS-Konfiguration. Sie leitet API-Aufrufe an `127.0.0.1:5080` weiter. Bei einem abweichenden Compose-Port kann `PATHFINDER_API_PORT` für diesen Start gesetzt werden.

```sh
cd frontend
npm ci
npm run start:library
```

Öffne [https://localhost:4200/library](https://localhost:4200/library) und melde dich mit dem administrativ angelegten Konto an. Wenn du ein lokal vertrauenswürdiges Zertifikat für `localhost` hast, übergib es mit `npm run start:library -- --ssl-cert /pfad/zertifikat.pem --ssl-key /pfad/schluessel.pem`. Im Menü erreichst du auch den Tracker. Der normale Start mit `npm start` eignet sich für die Tracker-Demo. Wenn der Demo-Server schon auf Port 4200 läuft, kannst du die Bibliothek mit `npm run start:library -- --port 4201` starten und Port 4201 im Browser öffnen.

Unter **Encounters** kannst du mit derselben Anmeldung PCs und Monster aus der Bibliothek zu einem vorbereiteten Encounter hinzufügen. Für ein vorhandenes Compose-Projekt baut `docker compose up --build -d api` die API neu. Beim Start ergänzt die Migration die Encounter-Tabellen im vorhandenen Datenbank-Volume. [Encounter-Vorbereitung öffnen](https://localhost:4200/encounters)

In der Bibliothek kannst du PC- und Monstervorlagen anlegen, bearbeiten, archivieren, wiederherstellen, löschen und aus einer JSON-Datei importieren. Die Angriffssequenzen gehören zum Monsterformular. [JSON-Beispiel](backend/README.md#versionierter-json-import)

## Build, Lint und Tests

| Bereich | Verzeichnis | Befehle |
| --- | --- | --- |
| Angular-Tests (Vitest) | `frontend/` | `npm test` |
| Angular-Produktionsbuild | `frontend/` | `npm run build -- --configuration production` |
| Biome-Lint für geänderte Frontend-Dateien | `frontend/` | `npm exec -- biome lint --vcs-use-ignore-file=false <Dateien>` |
| Backend-Release-Build und xUnit-Tests mit .NET 10 | `backend/` | `dotnet test PathfinderElements.slnx --configuration Release` |
| Migration, Persistenz und Restore mit PostgreSQL 17 | Repository-Root | `backend/scripts/test-persistence.sh` |

Frontend-Specs liegen als `*.spec.ts` neben dem getesteten Code. Beim gezielten Biome-Lauf ersetzt die Dateiliste den Ignore-Filter, weil `.gitignore` im Repository-Root liegt. Backend-Tests liegen unter `backend/tests/`. Der Compose-Test verwendet eigene Projekte, Volumes und synthetische Daten. Er gehört zur Prüfung von Änderungen an Migrationen, Persistenz oder Backups. [Backend und Persistenztest](backend/README.md#automatischer-persistenztest)

Tests wachsen mit den Features. Die Bibliothekstests decken das Speichern und Wiederladen von Vorlagen, die Besitzerprüfung, die atomare Ablehnung ungültiger Importe und die Übersetzung des Formulars ab. Neue PF1E-Regeln erhalten gezielte Unit-Tests für die vereinbarten Regelfälle.

## Lesereihenfolge

1. [`AGENTS.md`](AGENTS.md) — dauerhafte Arbeitsanweisungen für Codex
2. [`docs/VISION.md`](docs/VISION.md) — Ziele, Nutzerproblem und Produktgrenzen
3. [`docs/REQUIREMENTS.md`](docs/REQUIREMENTS.md) — prüfbare funktionale und nichtfunktionale Anforderungen
4. [`docs/DOMAIN_MODEL.md`](docs/DOMAIN_MODEL.md) — konzeptionelle Domänenobjekte und Invarianten
5. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — verbindliche technische Entscheidungen und Grenzen
6. [`docs/DECISIONS.md`](docs/DECISIONS.md) — begründete Entscheidungen
7. [`docs/OPEN_QUESTIONS.md`](docs/OPEN_QUESTIONS.md) — fachliche/technische Unklarheiten, die nicht geraten werden dürfen
8. [`docs/ROADMAP.md`](docs/ROADMAP.md) — phasenweise Umsetzung und Definition of Done
9. [`docs/CODEX_START_PROMPT.md`](docs/CODEX_START_PROMPT.md) — aktueller Stand und Startprompt für eine neue Codex-Session

**Statuskennzeichnung:** `FESTGELEGT` = vom Auftraggeber entschieden; `VORSCHLAG` = zu prüfende Umsetzung; `OFFEN` = vor Umsetzung entscheiden. Anforderungen mit `MUSS` sind für den beschriebenen Zielumfang verpflichtend; die Roadmap bestimmt, wann sie umgesetzt werden.

## Ziel für Release 1

Ein mobil bedienbarer, serverseitig persistierter PF1E Combat Manager: PC-/Monsterbibliothek, Encounter-Vorbereitung, vollständige Initiativeabläufe inklusive Ready/Delay/Überraschungsrunde, HP-/Angriffsverwaltung mit GM-Kontrolle, ausgewählte automatische Zustände, Kampfprotokoll, Undo, Login, Deutsch/Englisch und sichere Wiederaufnahme.

## Leitprinzip

> Automatisiere eindeutige PF1E-Regeln; überlasse Sonderfälle und endgültige Entscheidungen dem GM. Jede bestätigte Zustandsänderung muss nachvollziehbar und rückgängig machbar sein.

**Keine Funktion für andere Spielleiter und keine SaaS-Abrechnung in Release 1.** Das Datenmodell soll mehrere Besitzer/Konten später ermöglichen.
