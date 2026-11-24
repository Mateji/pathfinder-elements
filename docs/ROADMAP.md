# ROADMAP — Iterative Entwicklung mit Codex

**Version:** 0.2 · **Stand:** 24.11.2026 · **Status:** M0-Testsockel offen, M1 begonnen.

**Arbeitstakt:** Jeder Meilenstein in kleine, einzeln reviewbare Aufgaben zerlegen. Vor jedem Meilenstein Repository auditen und offene fachliche Fragen klären. Keine „implementiere das ganze Projekt“-Aufträge.

## M0 — Bestandsaufnahme und Testsockel

- [x] Angular-Projektstruktur, vorhandene `EncounterStore`/Component/Services, Scripts, JSON-Demos und Git-Status prüfen.
- [x] Bestehendes MVP lokal bauen und dokumentieren, was funktioniert und was fehlt.
- [ ] Test-/Lint-/Build-Konventionen und Entwickler-Dokumentation festlegen.
- [ ] Dokumentationskonflikte und abweichenden Ist-Zustand benennen, nicht unkontrolliert überschreiben.

**Abnahme:** Reproduzierbarer Frontend-Build, klare Bestandsliste, minimaler M1-Umsetzungsplan.

**Vorbereitung vor M1, geprüft am 23.11.2026.** Angular liegt unter `frontend/`. Dort liefen `npm ci` und `npm run build -- --configuration production` erfolgreich. Der Build benötigt für das eingebundene Schrift-Stylesheet Zugriff auf Google Fonts. Im Chromium-Test des Produktionsbuilds lieferte der Encounter-Request neun Teilnehmer. Die Reihenfolge, die aktive Markierung beim Vor- und Zurückschalten sowie das RK-Symbol aus dem SVG-Sprite wurden geprüft. `docker compose config` validierte den Platzhalter im Root.

**Offene Testinfrastruktur.** Drei Erzeugungstests sind vorhanden. Für deren Ausführung fehlen ein npm-Testskript und ein Angular-Test-Target.

## M1 — Infrastruktur, Login, Persistenz

- [x] ASP.NET Core 10 Web API als modularen Monolithen initialisieren.
- [x] PostgreSQL + EF Core-Migrationen + Docker Compose + lokale Entwicklung einrichten.
- [x] HTTPS-Reverse-Proxy-Integration mit existierendem Serverkonzept vorsehen.
- [ ] Login für 1 administrativ angelegtes Konto, serverseitige Autorisierung, Ownership-Konzept.
- [ ] Grundlegende API-/Integrationstests, Healthcheck, Backup-/Restore-Runbook.
- [ ] DE/EN-i18n-Technik entscheiden und Grundgerüst etablieren.

**Abnahme:** Private API über Login geschützt; App + DB per Compose reproduzierbar; Beispielentität persistiert über Container-Neustart; i18n-Testschirm in beiden Sprachen.

**Schritt 1, geprüft am 23.11.2026.** Release-Build der Backend-Solution im SDK-Container mit .NET 10.0.404 erfolgreich. Ein Integrationstest startet den API-Host und prüft für `GET /health/live` HTTP 200 mit leerem Antwortkörper. Der Angular-Produktionsbuild lief ebenfalls erfolgreich.

**Schritt 2, geprüft am 23.11.2026.** Compose startete die API und PostgreSQL 17 mit frischem benanntem Volume. Die API wendete die erste EF-Core-Migration an. Der automatisierte Test schrieb einen synthetischen Datensatz und las ihn nach Neuerstellung des DB-Containers wieder aus. Während des Datenbankstopps lieferten Readiness HTTP 503 und Liveness HTTP 200, nach dem Neustart lieferte Readiness wieder HTTP 200. Der Backend-Release-Build, der Liveness-Integrationstest und der Angular-Produktionsbuild waren erfolgreich.

**Schritt 3, geprüft am 24.11.2026.** Die [Caddy-Vorlage](../deploy/caddy/pathfinder.caddy) nutzt den bestehenden API-Loopback-Port. Im lokalen Test mit Caddy, API, PostgreSQL und vertrauenswürdig eingebundener lokaler Caddy-CA lieferte HTTP eine Weiterleitung (308) auf HTTPS. Über HTTPS antwortete Liveness mit 200 und Readiness mit 404. Direkt am lokalen API-Port lieferte Readiness weiterhin 200. Compose-Konfiguration, Backend-Release-Build, Liveness-Integrationstest und Angular-Produktionsbuild waren erfolgreich. Domain, Caddy-Betriebsort und öffentlicher Zertifikatstest bleiben Teil der Servereinrichtung. [HTTPS-Einrichtung](../backend/README.md#https-mit-caddy-auf-dem-linux-host)

## M2 — Bibliothek

- [ ] PC- und Monster-Modelle mit kampfrelevanten Feldern präzisieren.
- [ ] CRUD und sicheres Archivieren/Löschen, owner-geschützte Endpunkte.
- [ ] Angriffsdefinitionen und Vorlagenwerte; Import aus versioniertem JSON.
- [ ] Responsive Formular-/Listen-UI in DE/EN, Tests und Datenvalidierung.

**Abnahme:** 3 PCs und Goblins/Ork anlegen, speichern, ändern, importieren, nach Neustart wieder öffnen.

## M3 — Encounter-Vorbereitung

- [ ] Encounter aus PCs/Monstern zusammenstellen; Mengenfunktion Default 1.
- [ ] Stabile einzelne Teilnehmerinstanzen; Farbkennzeichnung und Kurzlabels.
- [ ] Bibliotheksreferenzen/Override-Konfliktregel; Kopieren und Wiederverwenden.
- [ ] Start-HP je Teilnehmer mit Max-HP als Default; Starten und erneut vorbereiten.

**Abnahme:** 3 PCs + 4 Goblins + 1 Ork; alle Goblins einzeln identifizierbar; Änderung einer Vorlage aktualisiert den vorbereiteten Encounter kontrolliert.

## M4 — Kampfablauf und zuverlässige Wiederaufnahme

- [ ] Automatische NPC-Initiative und manuelle PC-Würfe; PF1E-Tiebreak.
- [ ] Aktiver Combatant, Rundenzähler, Jump, manuelle Ausnahmeordnung, Surprise.
- [ ] Delay/Ready inkl. Regel-Testfällen und dynamischer Initiativefolge.
- [ ] Dauerhafte, transaktionale Combat-State-Commands; Wiederaufnahme nach Reload.
- [ ] Protokollgrundlage, Duplikatschutz und Konfliktbehandlung bei parallelen Tabs.

**Abnahme:** Kampfrunden inklusive Ready/Delay korrekt; Tab schließen/neu öffnen setzt am letzten bestätigten Stand fort.

## M5 — Kampfaktionen, HP, Undo

- [ ] Aktuelle/maximale/temporäre/negative HP, Heilung und direkter HP-Editor.
- [ ] PC-Aktion: Zielwahl, Schadens-/Zustandseingabe.
- [ ] NPC-Aktion: Angriff/Sequenz wählen, Wurf, Treffer-/Kritberechnung, Schadenswurf, GM-Bestätigung mit Override.
- [ ] Undo konsistent mit Aktionslog und abhängigen Zustandsänderungen.
- [ ] Regeltests für Angriff, Krits, Schaden und Undo; mobile Bedienung.

**Abnahme:** Gesamter Beispielkampf spielbar, beliebiger NPC-Schadenswert vor Bestätigung korrigierbar; Abbruch ohne Zustandsänderung; Undo stellt konsistent wieder her.

## M6 — Conditions und Zeitautomatik

- [ ] Basiseinzelwerte/Bonustypen ausreichend für den vereinbarten Zustandskatalog.
- [ ] Zustände hinzufügen/entfernen, Dauer automatisch entlang Kampfzeitachse aktualisieren.
- [ ] Regelmodifikatoren korrekt ableiten und manuelle Sonderfälle abbilden.
- [ ] GM-gesteuerte Entscheidungen bei Sterben/Tod.

**Abnahme:** Definierter Zustand verändert einen berechneten Kampfwert testbar und läuft genau zum richtigen Zeitpunkt aus.

## M7 — Spieltisch-Härtung und Release-1-Abnahme

- [ ] Smartphone-/Tablet-UX, kontrastreiche Gegnerkennzeichnung, kompakte Listen, große Touchflächen.
- [ ] DE/EN-End-to-End, sichere Session und Fehlerfeedback.
- [ ] Sichere Docker-Produktionskonfiguration, Backup/Restore-Test und Migrationsstrategie.
- [ ] Umfangreiche End-to-End-Sitzung mit gespeichertem und wieder aufgenommenem Kampf.

**Abnahme:** Vollständiger Durchlauf von Encounter-Vorbereitung bis Kampfende mit Mobilgerät, ohne verlorene bestätigte Aktionen.

## Release 1 anschließend (nicht Bestandteil der Abnahme)

Begegnungsgenerator, Battlemaps/Dungeonmaps, Fremdstatblock-Parser, öffentliches Multi-User-Onboarding, Abrechnung und SaaS-Admin, Offline-Funktion.

## Definition of Done für jedes Arbeitspaket

- [ ] Akzeptanzfälle erfolgreich und ohne Regression.
- [ ] Build/Test-Läufe tatsächlich ausgeführt; Ergebnisse dokumentiert.
- [ ] Fachliche Änderungen in Requirements/Domain Model nachgezogen.
- [ ] Backend-Migrationen/API-Verträge bei Bedarf aktualisiert.
- [ ] Sicherheit, Besitzerisolation, DE/EN und mobile Bedienbarkeit entsprechend Betroffenheit geprüft.
- [ ] Kein irrelevanter Refactor; nachvollziehbarer Diff und Commit.
