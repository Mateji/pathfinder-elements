# Pathfinder Elements — Projektspezifikation v0.2

**Stand:** 19.11.2026 · **Status:** Arbeitsfassung, fachliche Anforderungen weitgehend abgestimmt · **Regelwerk:** Pathfinder First Edition (PF1E)

Der Initiative Tracker liegt als Angular-21-Anwendung unter `frontend/`. Unter `backend/` liegt die ASP.NET Core 10 Web API mit ihrem Liveness-Endpunkt. Die Spezifikation liegt unter `docs/`. Für die lokale Entwicklung startet Docker Compose die API und PostgreSQL. [Backend starten und testen](backend/README.md)

## Frontend lokal starten

Im Repository-Root beginnen und den Entwicklungsserver starten:

```sh
cd frontend
npm ci
npm start
```

Der Initiative Tracker ist unter `http://localhost:4200/` erreichbar. Der Produktionsbuild läuft aus `frontend/` mit `npm run build -- --configuration production`.

## Lesereihenfolge

1. [`AGENTS.md`](AGENTS.md) — dauerhafte Arbeitsanweisungen für Codex
2. [`docs/VISION.md`](docs/VISION.md) — Ziele, Nutzerproblem und Produktgrenzen
3. [`docs/REQUIREMENTS.md`](docs/REQUIREMENTS.md) — prüfbare funktionale und nichtfunktionale Anforderungen
4. [`docs/DOMAIN_MODEL.md`](docs/DOMAIN_MODEL.md) — konzeptionelle Domänenobjekte und Invarianten
5. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — verbindliche technische Entscheidungen und Grenzen
6. [`docs/DECISIONS.md`](docs/DECISIONS.md) — begründete Entscheidungen
7. [`docs/OPEN_QUESTIONS.md`](docs/OPEN_QUESTIONS.md) — fachliche/technische Unklarheiten, die nicht geraten werden dürfen
8. [`docs/ROADMAP.md`](docs/ROADMAP.md) — phasenweise Umsetzung und Definition of Done
9. [`docs/CODEX_START_PROMPT.md`](docs/CODEX_START_PROMPT.md) — erster Auftrag an Codex in VS Code

**Statuskennzeichnung:** `FESTGELEGT` = vom Auftraggeber entschieden; `VORSCHLAG` = zu prüfende Umsetzung; `OFFEN` = vor Umsetzung entscheiden. Anforderungen mit `MUSS` sind für den beschriebenen Zielumfang verpflichtend; die Roadmap bestimmt, wann sie umgesetzt werden.

## Ziel für Release 1

Ein mobil bedienbarer, serverseitig persistierter PF1E Combat Manager: PC-/Monsterbibliothek, Encounter-Vorbereitung, vollständige Initiativeabläufe inklusive Ready/Delay/Überraschungsrunde, HP-/Angriffsverwaltung mit GM-Kontrolle, ausgewählte automatische Zustände, Kampfprotokoll, Undo, Login, Deutsch/Englisch und sichere Wiederaufnahme.

## Leitprinzip

> Automatisiere eindeutige PF1E-Regeln; überlasse Sonderfälle und endgültige Entscheidungen dem GM. Jede bestätigte Zustandsänderung muss nachvollziehbar und rückgängig machbar sein.

**Keine Funktion für andere Spielleiter und keine SaaS-Abrechnung in Release 1.** Das Datenmodell soll mehrere Besitzer/Konten später ermöglichen.
