# ARCHITECTURE — Technische Zielarchitektur

**Version:** 0.2 · **Stand:** 19.11.2026

## Verbindlich festgelegt

- **Frontend:** vorhandenes **Angular-21**-Projekt weiterverwenden (mögliche Upgrades nur nach Entscheidung), Standalone Components, Signals und moderne Templates.
- **Backend:** **ASP.NET Core 10 Web API**, modulare Monolith-Architektur (kein Microservicesetup), EF Core.
- **Persistenz:** PostgreSQL mit EF-Core-Migrationen.
- **Hosting:** öffentlich zugänglicher Linuxserver, **HTTPS** über Reverse Proxy (vorgesehen **Caddy**), Docker Compose.
- **Accounts:** Login obligatorisch; zunächst nur ein administrativ eingerichtetes Konto; Daten von Anfang an einem Owner zuordnen; spätere Mehrbenutzer-/SaaS-Fähigkeit.
- **Sprache:** Deutsch und Englisch.

## Vorgeschlagene Laufzeit-Topologie

### Repository-Struktur

Der Angular-Workspace einschließlich npm-Lockfile, Build-Konfiguration und Assets liegt unter `frontend/`. Frontend-Befehle laufen in diesem Verzeichnis. Die ASP.NET-Core-Solution liegt unter `backend/`. Die gemeinsame `compose.yaml` liegt im Repository-Root neben `README.md` und `AGENTS.md`. Die Projektdokumentation liegt unter `docs/`.

### Lokaler Stand in M1

Im Repository-Root startet Docker Compose die API und PostgreSQL 17. Der API-Port bindet standardmäßig an `127.0.0.1:5080`. Der Datenbankport ist ausschließlich im Compose-Netz erreichbar. Ein benanntes Volume speichert die Daten. Für die lokale Entwicklung führt der API-Host offene EF-Core-Migrationen beim Start aus. Der Liveness-Endpunkt prüft die Erreichbarkeit der API, der Readiness-Endpunkt zusätzlich die Datenbankverbindung. [Betrieb und Persistenztest](../backend/README.md)

Für Caddy auf demselben Linux-Host gibt es eine HTTPS-Vorlage für den Liveness-Endpunkt. Die konkrete Domain und der Betriebsort von Caddy bleiben in OQ-15 offen. [HTTPS-Einrichtung](../backend/README.md#https-mit-caddy-auf-dem-linux-host)

### Dienste

```text
Browser (Smartphone / Tablet / Laptop)
        |
        | HTTPS
        v
Caddy Reverse Proxy (Host oder Container, bestehendes Setup prüfen)
        |
        v
ASP.NET Core App Container (REST /api/* + statisches Angular-Build)
        |
        | privates Docker-Netz
        v
PostgreSQL Container -> persistent volume + Backups
```

**Status:** Ein App-Container + ein DB-Container ist der bevorzugte schlanke Start; es ist nicht zwingend, dass Caddy ein dritter Container wird. Vorhandene Host-Proxy-/Domain-Konfiguration zuerst prüfen. Dieselbe Origin für UI und API vermeidet unnötige CORS-Komplexität.

## Backend-Modulgrenzen

- **Identity/Access:** Login, Sitzung, Ownership, Autorisierung.
- **Library:** PC-/Monster-Vorlagen, Angriffsdefinitionen, Import.
- **Encounter Planning:** vorbereitete Encounter, Referenzen, Überschreibungen, Kopien.
- **Combat Engine:** PF1E-Initiative, Ready/Delay, Attack Rolls, Krits, HP, Zustände, deterministische State Transitions.
- **Combat Persistence:** bestätigte Aktionen, Änderungsprotokoll, Undo, Wiederaufnahme.

Keine eigenständigen Deployments pro Modul. Fachlogik sitzt weder im Controller noch in der EF-Entity-Konfiguration. Datenbankmodelle/DTOs/Domain-Modelle bewusst trennen, sofern dies nach Audit verhältnismäßig bleibt.

## Frontend-Verantwortung

- Feature-orientierte Seiten/Komponenten für Bibliothek, Encounter-Vorbereitung, Combat Manager, Login und Einstellungen.
- Feature Stores über Angular Signals: Serverstatus, UI-Zustand, aktive Ansicht, Loading/Error; `computed` nur für Ableitungen.
- Data-Access-Services für API; keine HTTP-Aufrufe in Presentational Components.
- Eingaben für ausstehende Angriffe lokal halten, bestätigte Serverantwort ist autoritativ. Erfolg/Fehler sichtbar anzeigen.
- UI-Texte aus dem typisierten Katalog der aktiven Sprache beziehen. Locale-sensitive Zahlen und Datumsangaben mit der aktiven Sprache explizit formatieren. [D-027](DECISIONS.md#d-027--ui-sprache-zur-laufzeit--25112026--v)
- Die Bibliotheksseite lädt Vorlagen nach erfolgreicher Sitzungsprüfung. Der Angular-HTTPS-Entwicklungsproxy bindet die Compose-API auf derselben Origin ein. Änderungen verwenden das ETag der geöffneten Vorlage. Bei einem Konflikt bleibt der Formularentwurf erhalten und die aktuelle Version kann ausdrücklich neu geöffnet werden. [Lokaler Start](../README.md#bibliothek-mit-anmeldung)

## API-Verträge und Konsistenz

- Vorhandene REST-Ressourcen sind `/api/characters`, `/api/monsters` und `/api/encounters`. Die HTTP-Verträge dokumentieren Felder, Besitzprüfung und Versionsschutz. Die Zielarchitektur sieht zusätzlich eine OpenAPI-Spezifikation vor. Endpunkte für laufende Kämpfe folgen mit der Kampf-Domäne. [HTTP-Verträge](../backend/README.md#http-vertrag-der-bibliothek)
- Der Bibliotheksimport verwendet eine explizite Schemaversion. PostgreSQL speichert importierte Vorlagen und den eindeutigen Importbeleg des Besitzers atomar. [HTTP-Vertrag des Imports](../backend/README.md#versionierter-json-import)
- Autorisierung bei jeder ID-Ressource anhand eingeloggtem Owner prüfen.
- Mutationen am Combat über explizite serverseitige Commands (z. B. `ApplyDamage`, `ConfirmAttack`, `EndTurn`, `UndoAction`), nicht unkontrollierte freie Patches auf alle Felder.
- Jede Command-Anfrage besitzt Idempotency-/Action-ID und Combat-Version/Concurrency-Schutz. Backend schreibt Zustand und Log in **einer** Datenbanktransaktion.
- Nicht gestartete Entwürfe, laufender Combat und abgeschlossene Aufzeichnung werden getrennt modelliert.
- Migrationsprozess, Healthchecks und Log-Ausgabe für Docker dokumentieren.
- PostgreSQL-Backups und Restore in ein separates Compose-Projekt sind dokumentiert. [Runbook](../backend/BACKUP_RESTORE.md)

## Authentication & Security

- ASP.NET Core Identity speichert das administrativ eingerichtete Konto in PostgreSQL. Die sichere Cookie-Sitzung gilt maximal zwölf Stunden. Die API validiert den Security Stamp und verwirft Sitzungen nach einem Passwort-Reset. UI und API sind im Betrieb unter derselben Origin vorgesehen. [D-026](DECISIONS.md#d-026--login-und-wiederherstellung-für-release-1--24112026--f)
- HTTPS-only, `HttpOnly`, `Secure`, geeignete `SameSite`-Einstellung; Anti-CSRF für schreibende Requests; serverseitige Eingabevalidierung.
- Kein Signup-Endpunkt in Release 1. Erstbenutzer über dokumentierten Admin-/Provisioning-Prozess mit Secret außerhalb von Git anlegen.
- Passwort-Rate-Limit/Brute-Force-Schutz, sichere Logs ohne Session-Cookies/Secrets; DB nicht aus dem Internet erreichbar.
- Multi-User-Schutz von Beginn an: `OwnerUserId` an Root-Aggregaten; API filtert und autorisiert serverseitig.
- Eine öffentlich erreichbare Admin-Oberfläche nur mit sicheren Defaults und minimalen Rechten.

### Ownership für private Ressourcen

PC-Vorlagen, Monstervorlagen, vorbereitete Encounter und Kämpfe erhalten beim Anlegen eine verpflichtende `OwnerUserId` aus der serverseitig authentifizierten Benutzeridentität. Die API begrenzt Listen und Einzelzugriffe auf diesen Besitzer. Änderungen und Löschvorgänge verwenden denselben Besitzerfilter. Referenzen auf andere private Ressourcen werden ebenfalls im Besitzkontext geprüft, etwa beim Hinzufügen einer Monstervorlage zu einem Encounter. Untergeordnete Daten wie Kampfaktionen werden über den zugehörigen Kampf autorisiert.

Die Bibliotheks- und Encounter-API filtern nach dem Besitzer aus der Cookie-Sitzung. Für fremde IDs gilt dieselbe Antwort wie für fehlende IDs. Änderungen an gespeicherten Ressourcen setzen die aktuelle Version voraus. PostgreSQL blockiert das endgültige Löschen von Vorlagen mit einem restriktiven Fremdschlüssel aus `encounter_participants`. Die technische Tabelle `application_metadata` dient dem Persistenztest. [HTTP-Vertrag der Bibliothek](../backend/README.md#http-vertrag-der-bibliothek) · [HTTP-Vertrag der Encounter-Vorbereitung](../backend/README.md#http-vertrag-der-encounter-vorbereitung)

**Akzeptanzfälle für private Endpunkte**

- Ohne Sitzung liefert ein Aufruf einer privaten API-Ressource HTTP 401.
- Mit Sitzung kann ein Benutzer eigene Ressourcen im erlaubten Umfang lesen und ändern.
- Ein zweiter synthetischer Benutzer kann fremde Ressourcen weder in einer Liste sehen noch über ihre ID lesen, ändern, löschen oder referenzieren.
- Die Besitzerzuordnung neuer Ressourcen stammt aus der Sitzung. Eine im Request gesendete fremde Besitzer-ID kann die Zuordnung nicht beeinflussen.

## Vorgehen mit dem bestehenden Angular-Code

1. Bestehende Angular-Dateistruktur, Store/Signals, HTTP/Assets, Tests und Abhängigkeiten tatsächlich prüfen.
2. Funktionierenden Seed-/Initiative-MVP zunächst erhalten; schrittweise API-Persistenz anschließen.
3. Fachlogik zentral in der Backend-Domäne abbilden; Angular darf rein darstellende Ableitungen besitzen.
4. Frontend und Backend jeweils separat bauen/testen; dann end-to-end über Compose und mobile Browser prüfen.

## Bewusst offen

- Lokalisierung kuratierter Bibliotheksinhalte und weiterer PF1E-Regelbegriffe (OQ-14).
- Backup-Zeitplan/Retention, Domain/DNS und Caddy-Betriebsort;
- Concurrent-Tab-Policy; Datenmigration bisheriger JSON-Dateien;
- genaue Versionierung und Struktur weiterer Persistenz-DTOs.

**Diese Punkte sind technische ADRs vor dem jeweiligen Meilenstein, nicht stillschweigende Vorgaben.**
