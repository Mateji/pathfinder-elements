# ROADMAP — Iterative Entwicklung mit Codex

**Version:** 0.2 · **Stand:** 25.11.2026 · **Status:** M1 und M2 abgeschlossen, M3 bis zur Teilnehmerkennzeichnung umgesetzt. Der M0-Dokumentationsabgleich ist offen. [Codex-Übergabe](CODEX_START_PROMPT.md)

**Arbeitstakt:** Jeder Meilenstein in kleine, einzeln reviewbare Aufgaben zerlegen. Vor jedem Meilenstein Repository auditen und offene fachliche Fragen klären. Keine „implementiere das ganze Projekt“-Aufträge.

## M0 — Bestandsaufnahme und Testsockel

- [x] Angular-Projektstruktur, vorhandene `EncounterStore`/Component/Services, Scripts, JSON-Demos und Git-Status prüfen.
- [x] Bestehendes MVP lokal bauen und dokumentieren, was funktioniert und was fehlt.
- [x] Test-/Lint-/Build-Konventionen und Entwickler-Dokumentation festlegen.
- [ ] Dokumentationskonflikte und abweichenden Ist-Zustand benennen, nicht unkontrolliert überschreiben.

**Abnahme:** Reproduzierbarer Frontend-Build, klare Bestandsliste, minimaler M1-Umsetzungsplan.

**Vorbereitung vor M1, geprüft am 23.11.2026.** Angular liegt unter `frontend/`. Dort liefen `npm ci` und `npm run build -- --configuration production` erfolgreich. Der Build benötigt für das eingebundene Schrift-Stylesheet Zugriff auf Google Fonts. Im Chromium-Test des Produktionsbuilds lieferte der Encounter-Request neun Teilnehmer. Die Reihenfolge, die aktive Markierung beim Vor- und Zurückschalten sowie das RK-Symbol aus dem SVG-Sprite wurden geprüft. `docker compose config` validierte den Platzhalter im Root.

**Entwicklerkonventionen, festgehalten am 25.11.2026.** Das README dokumentiert Build, gezieltes Linting und Testläufe für Frontend, Backend und PostgreSQL. Neue Tests decken die Akzeptanzfälle des jeweiligen Features ab. [Build, Lint und Tests](../README.md#build-lint-und-tests)

## M1 — Infrastruktur, Login, Persistenz

- [x] ASP.NET Core 10 Web API als modularen Monolithen initialisieren.
- [x] PostgreSQL + EF Core-Migrationen + Docker Compose + lokale Entwicklung einrichten.
- [x] HTTPS-Reverse-Proxy-Integration mit existierendem Serverkonzept vorsehen.
- [x] Login für 1 administrativ angelegtes Konto, serverseitige Autorisierung, Ownership-Konzept.
- [x] Grundlegende API-/Integrationstests, Healthcheck, Backup-/Restore-Runbook.
- [x] DE/EN-i18n-Technik entscheiden und Grundgerüst etablieren.

**Abnahme:** Private API über Login geschützt; App + DB per Compose reproduzierbar; Beispielentität persistiert über Container-Neustart; i18n-Testschirm in beiden Sprachen.

**Schritt 1, geprüft am 23.11.2026.** Release-Build der Backend-Solution im SDK-Container mit .NET 10.0.404 erfolgreich. Ein Integrationstest startet den API-Host und prüft für `GET /health/live` HTTP 200 mit leerem Antwortkörper. Der Angular-Produktionsbuild lief ebenfalls erfolgreich.

**Schritt 2, geprüft am 23.11.2026.** Compose startete die API und PostgreSQL 17 mit frischem benanntem Volume. Die API wendete die erste EF-Core-Migration an. Der automatisierte Test schrieb einen synthetischen Datensatz und las ihn nach Neuerstellung des DB-Containers wieder aus. Während des Datenbankstopps lieferten Readiness HTTP 503 und Liveness HTTP 200, nach dem Neustart lieferte Readiness wieder HTTP 200. Der Backend-Release-Build, der Liveness-Integrationstest und der Angular-Produktionsbuild waren erfolgreich.

**Schritt 3, geprüft am 24.11.2026.** Die [Caddy-Vorlage](../deploy/caddy/pathfinder.caddy) nutzt den bestehenden API-Loopback-Port. Im lokalen Test mit Caddy, API, PostgreSQL und vertrauenswürdig eingebundener lokaler Caddy-CA lieferte HTTP eine Weiterleitung (308) auf HTTPS. Über HTTPS antwortete Liveness mit 200 und Readiness mit 404. Direkt am lokalen API-Port lieferte Readiness weiterhin 200. Compose-Konfiguration, Backend-Release-Build, Liveness-Integrationstest und Angular-Produktionsbuild waren erfolgreich. Domain, Caddy-Betriebsort und öffentlicher Zertifikatstest bleiben Teil der Servereinrichtung. [HTTPS-Einrichtung](../backend/README.md#https-mit-caddy-auf-dem-linux-host)

**Schritt 4, geprüft am 24.11.2026.** ASP.NET Core Identity schützt die API standardmäßig. Anmeldung und Abmeldung verlangen einen CSRF-Token. Die Cookie-Sitzung läuft spätestens nach zwölf Stunden ab. Ein lokaler HTTPS-Test über Caddy und PostgreSQL bestätigte die Kontoanlage per CLI, die Ablehnung eines zweiten Kontos, die Sitzung nach Neuerstellung des API-Containers und den Widerruf der alten Sitzung nach Passwort-Reset. Alle fünf Backend-Integrationstests und der Angular-Produktionsbuild liefen erfolgreich. Das [Ownership-Konzept](ARCHITECTURE.md#ownership-für-private-ressourcen) dokumentiert die Besitzerprüfungen für kommende Fachendpunkte. Die Angular-Demo hat weiterhin keine Login-Maske. [Konto und HTTP-Vertrag](../backend/README.md#konto-einrichten-und-passwort-zurücksetzen)

**Schritt 5, geprüft am 24.11.2026.** Der [Backup- und Restore-Ablauf](../backend/BACKUP_RESTORE.md) ist dokumentiert. Der Compose-Test stellte einen synthetischen Datensatz und ein Identity-Konto in einem frischen PostgreSQL-Volume wieder her. Liveness und Readiness lieferten danach HTTP 200. Beim vorherigen Ausfall der Quelldatenbank lieferte Readiness HTTP 503 und Liveness HTTP 200. Alle sechs Backend-Integrationstests und der Angular-Produktionsbuild waren erfolgreich. Backup-Zeitplan und Aufbewahrung bleiben in OQ-15 offen.

**Schritt 6, geprüft am 25.11.2026.** Der Initiative-Tracker dient als DE/EN-Testschirm. Im Chrome-Test des Produktionsbuilds wechselten die UI-Texte, der Seitentitel und die Dokumentsprache bei der Sprachwahl. Die neun Demo-Teilnehmer und der aktive Zug blieben beim Umschalten erhalten. Nach einem Neuladen stellte der Browser die Sprachwahl wieder her. Bei 320 px Breite waren beide Sprachen ohne horizontales Scrollen bedienbar. Die Sprachwahl funktionierte über die Tastatur. Der Angular-Produktionsbuild, sieben Angular-Tests und sechs Backend-Integrationstests waren erfolgreich. [Technische Entscheidung](DECISIONS.md#d-027--ui-sprache-zur-laufzeit--25112026--v)

## M2 — Bibliothek

- [x] PC- und Monster-Modelle mit kampfrelevanten Feldern präzisieren. Die vorhandenen [Gesamtwerte](DOMAIN_MODEL.md#bibliotheksvorlagen-in-m2) genügen als Pflichtfelder für M2. OQ-05 folgt mit dem Zustandskatalog in M6.
- [x] CRUD und sicheres Archivieren/Löschen, owner-geschützte Endpunkte.
- [x] Angriffsdefinitionen und Vorlagenwerte; Import aus versioniertem JSON. [D-030](DECISIONS.md#d-030--versionierter-bibliotheksimport--25112026--f)
- [x] Responsive Formular-/Listen-UI in DE/EN, Tests und Datenvalidierung.

**Abnahme:** 3 PCs und Goblins/Ork anlegen, speichern, ändern, importieren, nach Neustart wieder öffnen.

**Grundmodell, geprüft am 25.11.2026.** Die EF-Migration `AddLibraryTemplates` ergänzt je eine Tabelle für PC- und Monstervorlagen. Der PostgreSQL-Test speicherte beide Typen über EF Core, las sämtliche Werte in einem neuen Kontext und prüfte Fremdschlüssel und Max-HP-Constraint. Besitzer, IDs und Kampfwerte blieben nach Neuerstellung des DB-Containers und nach Restore in ein frisches Volume erhalten. Der Angular-Produktionsbuild und sieben Frontend-Tests liefen erfolgreich. Elf Backend-Tests liefen im Standardlauf, der PostgreSQL-Test im Compose-Lauf. [Akzeptanzfälle des Grundmodells](REQUIREMENTS.md#fr-01-bibliothek-von-pcs-und-monstern)

**Bibliotheks-API, geprüft am 25.11.2026.** Beide Vorlagentypen unterstützen Anlegen, Listen, Lesen, Ändern, Archivieren, Wiederherstellen und endgültiges Löschen. Die Tests deckten Cookie-Sitzung, CSRF-Schutz, Besitzerisolation, Versionskonflikte und die Löschsperre mit einem PostgreSQL-Testverweis ab. Das Upgrade vorhandener Vorlagen erhielt IDs und Werte und ergänzte eine Version. 13 Backend-Tests liefen im Standardlauf, acht Tests im isolierten Compose-Lauf. Die Caddy-Konfiguration, der Angular-Produktionsbuild und sieben Frontend-Tests wurden erfolgreich geprüft. Die Anbindung echter Encounter-Referenzen nach D-028 folgt in M3. [HTTP-Vertrag](../backend/README.md#http-vertrag-der-bibliothek)

**Angriffssequenzen, geprüft am 25.11.2026.** Monstervorlagen speichern geordnete Nah-, Fern- und natürliche Angriffe samt Krit- und Schadenswerten. Alte API-Clients können Grundwerte ändern und die Angriffe erhalten. Die Migration bestehender Vorlagen, der PostgreSQL-Containerwechsel und der Restore wurden geprüft. Der Angular-Produktionsbuild, sieben Frontend-Tests, 13 Backend-Tests im Standardlauf und neun Tests im isolierten Compose-Lauf waren erfolgreich. [Angriffssequenzen](../backend/README.md#angriffssequenzen)

**JSON-Import, geprüft am 25.11.2026.** Die API importiert kompakte oder formatierte JSON-Dokumente der Version 1 mit PC- und Monstervorlagen. PostgreSQL verhindert parallele Doppelimporte pro Konto. Automatisierte Tests bestätigten gemischte Importe mit Angriffssequenzen, die Ablehnung ungültiger Dokumente ohne Teildaten und die getrennte Verarbeitung desselben Dokuments für zwei Konten. Der Angular-Produktionsbuild, sieben Frontend-Tests, 14 Backend-Tests im Standardlauf und zwölf Tests im isolierten Compose-Lauf waren erfolgreich. Die Caddy-Konfiguration wurde validiert. [HTTP-Vertrag des Imports](../backend/README.md#versionierter-json-import)

**Bibliotheksoberfläche, geprüft am 25.11.2026.** Der Chrome-Test über HTTPS und Compose legte drei PCs und einen Goblin mit geordneter Angriffssequenz an. Er prüfte Änderungen, das Archiv, Wiederherstellung, die Bestätigung beim Löschen und den JSON-Dateiimport von Ork und Goblin-Späher. Ungültige Dateien und Wiederholungen wurden angezeigt. Beim Sprachwechsel und bei einem Versionskonflikt blieb der Formularentwurf erhalten. Nach Neustart der PostgreSQL-Testdatenbank ließen sich die Vorlagen wieder öffnen. Die Bedienung in DE/EN wurde bei 320 und 768 px sowie auf Laptop-Breite geprüft. Der Angular-Produktionsbuild, 13 Frontend-Tests, 14 Backend-Tests im Standardlauf und das gezielte Biome-Linting waren erfolgreich. [Bibliothek lokal starten](../README.md#bibliothek-mit-anmeldung)

## M3 — Encounter-Vorbereitung

- [x] Encounter aus PCs/Monstern zusammenstellen; Mengenfunktion Default 1.
- [x] Stabile einzelne Teilnehmerinstanzen mit Farbkennzeichnung und festen Nummern. [D-031](DECISIONS.md#d-031--nummern-und-farben-für-encounter-teilnehmer--25112026--f)
- [ ] Bibliotheksreferenzen, Override-Konfliktregel, Kopieren und Wiederverwenden. Die restriktiven Fremdschlüssel nach D-028 sind umgesetzt. Für die Override-Konfliktregel gilt OQ-01. [Offene Fragen](OPEN_QUESTIONS.md)
- [ ] Start-HP je Teilnehmer mit Max-HP als Default; Starten und erneut vorbereiten.

**Abnahme:** 3 PCs + 4 Goblins + 1 Ork; alle Goblins einzeln identifizierbar; Änderung einer Vorlage aktualisiert den vorbereiteten Encounter kontrolliert.

**Zusammenstellung, geprüft am 25.11.2026.** Der Chrome-Test über HTTPS legte einen Encounter mit drei PCs, vier Goblins und einem Ork an. Er prüfte die Standardmenge 1, den Sprachwechsel bei offener Eingabe, das Umbenennen, Wiederöffnen und einzelne Entfernen bei 320, 768 und 1280 px. Die PostgreSQL-Tests prüften acht verschiedene Teilnehmer-IDs, aktuelle Bibliothekswerte, Archivierung und Löschsperre über die realen Fremdschlüssel. Sie prüften außerdem Besitzerisolation, CSRF und konkurrierende Änderungen. 13 Frontend-Tests, Angular-Produktionsbuild, 15 Backend-Tests im Standardlauf, 15 Tests im Compose-Lauf und die Caddy-Validierung waren erfolgreich. [HTTP-Vertrag](../backend/README.md#http-vertrag-der-encounter-vorbereitung)

**Teilnehmerkennzeichnung, geprüft am 25.11.2026.** Chrome zeigte acht verschiedene Standardfarben und die Nummern 1 bis 8. Eine frei gewählte Farbe blieb nach dem Wiederöffnen gespeichert. Beim Entfernen eines Goblins blieben IDs, Nummern und Farben der übrigen Figuren erhalten. Teilnehmer 9 erhielt wieder die erste Farbe. Der Farbeditor war bei 320, 768 und 1280 px auf Deutsch und Englisch bedienbar. PostgreSQL prüfte das Upgrade leerer und befüllter Encounters, die Validierung der Farbe, Besitzerisolation, CSRF und Versionskonflikte. 13 Frontend-Tests, Angular-Produktionsbuild, 15 Backend-Tests im Standardlauf und 15 Tests im Compose-Lauf waren erfolgreich. [HTTP-Vertrag](../backend/README.md#http-vertrag-der-encounter-vorbereitung)

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
