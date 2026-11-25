# AGENTS.md — Pathfinder Elements

## Aufgabe und Kontext

Du arbeitest am Repository **Pathfinder Elements**, zunächst an einem Combat Manager für **Pathfinder First Edition (PF1E)**. Ziel ist ein zuverlässiges Werkzeug für einen GM am Smartphone, Tablet oder Laptop. Führe keine PF2E-Mechaniken ein.

Die [Codex-Übergabe](docs/CODEX_START_PROMPT.md) beschreibt den dokumentierten Arbeitsstand und den Einstieg für eine neue Session.

Lies vor fachlichen oder architektonischen Änderungen:
- `docs/VISION.md`
- `docs/REQUIREMENTS.md`
- `docs/DOMAIN_MODEL.md`
- `docs/ARCHITECTURE.md`
- `docs/DECISIONS.md`
- `docs/OPEN_QUESTIONS.md`
- `docs/ROADMAP.md`

Die aktuelle Repository-Realität hat Vorrang vor Annahmen über existierenden Code. Dokumentiere Abweichungen; passe bestehende Teile kontrolliert an. Lösche vorhandene Implementierungen nicht ungeprüft.

## Arbeitsweise

1. Vor jeder größeren Änderung: tatsächliche Dateien, Build-Skripte und Tests prüfen; anschließend kurzen Implementierungsplan mit betroffenen Dateien, Risiken und Teststrategie erstellen.
2. Arbeite jeweils an einem begrenzten Meilenstein oder einer kleinen Issue. Keine großflächigen Refactors nebenbei.
3. Bei `OFFEN` markierten fachlichen Entscheidungen: keine willkürliche Produktentscheidung treffen; Frage/Blocker dokumentieren und nur nichtblockierende Teile umsetzen.
4. Vorhandene Entscheidungen in `docs/DECISIONS.md` respektieren. Architekturänderungen zuerst begründen und freigeben lassen.
5. Neue oder geänderte Fachregeln samt Akzeptanzkriterien und Testfällen dokumentieren; `docs/ROADMAP.md` nach Abschluss aktualisieren.
6. Tests und Builds tatsächlich ausführen, soweit die lokale Umgebung dies erlaubt; Fehlschläge transparent berichten.
7. Gib nach jedem Arbeitspaket Änderungen, Tests, Risiken und verbleibende Fragen kurz an.

## Tech-Stack

- Frontend: **Angular 21** als Ausgangspunkt, standalone components, Signals/`computed`, moderne Template-Syntax (`@if`, `@for` mit stabilen IDs), `inject()`.
- Backend: **ASP.NET Core 10 Web API**, **EF Core**.
- Datenbank: **PostgreSQL**.
- Betrieb: **Docker Compose** auf Linux, über HTTPS hinter einem Reverse Proxy (vorgesehen: Caddy).
- Authentication: zunächst ein privates Konto, keine öffentliche Registrierung, später mehrere Benutzer.
- UI: **Deutsch und Englisch** von Beginn an. Vermeide hartcodierte UI-Texte ohne Übersetzungskonzept.

## Code-Stil

- **Immer Block-Statements** für Bedingungen, Schleifen und Funktionen, wo sie der Verständlichkeit dienen; keine verkürzten Einzeiler auf Kosten von Lesbarkeit.
- **Aussagekräftige ausgeschriebene Variablennamen** (`index`, `length`, `state`, `combatantId`), nicht `idx`, `len`, `s`, `c`.
- Strenge Typisierung; `any` vermeiden. Kleine, gut benannte Funktionen und klare Modulgrenzen.
- Keine zusätzlichen Frameworks/Libraries ohne konkrete Begründung; keine unnötigen Abstraktionen.
- Keine Spielregellogik in Angular Templates oder HTTP-Controllern.
- Abgeleitete Signals (`computed`) ohne Seiteneffekte; Datenzugriff und Feature-State getrennt halten.

## Unverhandelbare fachliche Grenzen

- Bibliothekseintrag != vorbereitete Encounter-Instanz != laufende Kampf-Instanz.
- Bibliotheksänderungen wirken auf vorbereitete Encounter, soweit keine bewusst gesetzte Override-Regel greift; laufende/abgeschlossene Kämpfe bleiben als Snapshots stabil.
- Gleiche Monster im Kampf haben individuelle HP, Initiative, Zustände, ID und Farbkennzeichnung.
- NPC-Angriff: berechnen, Ergebnis anzeigen, GM kann korrigieren/abbrechen, **erst bei Bestätigung anwenden**. Niemals heimlich direkt Schaden verbuchen.
- PC-Angriff: Spieler würfelt, GM wählt Ziel und gibt Schaden/Zustand ein.
- Komplexe PF1E-Zustände nur dann automatisch berechnen, wenn die dafür nötigen Daten vorliegen; keine vorgetäuschte Regelgenauigkeit.
- Tod/Stabilisierung werden angezeigt, aber nicht ohne GM-Entscheidung vollautomatisch abgewickelt.
- Jede bestätigte Kampfaktion ist dauerhaft nachvollziehbar, sicher wiederholbar/duplikatgeschützt und, soweit spezifiziert, undo-fähig.

## Security und Daten

- Login bei sämtlichen privaten API- und UI-Daten erforderlich; keine öffentliche Selbstregistrierung für Release 1.
- Besitz-/Zugriffsprüfung **serverseitig**, nicht nur im Frontend. Datenbankport nicht öffentlich exponieren.
- Passwörter/Secrets nie committen; HTTPS, sichere Auth-Cookies, CSRF-Schutz und sichere Eingabevalidierung berücksichtigen.
- Zustandsverändernde Operationen atomar speichern; Wiederholung eines Requests darf nicht unbemerkt doppelten Schaden verursachen.
- Keine echten Benutzer-/Charakterdaten in Demo-Seeds ohne explizite Freigabe.

## Definition of Done je Aufgabe

- Akzeptanzkriterien erfüllt und sinnvolle Automatiktests geschrieben.
- Erfolgreicher Frontend-Build und Backend-Build/Tests (falls eingerichtet).
- API-/Datenmodelländerungen dokumentiert; nötige Migrationen vorhanden.
- Responsive Bedienbarkeit und beide UI-Sprachen geprüft, soweit UI betroffen.
- Keine unbeabsichtigten Änderungen außerhalb des Arbeitspakets.
- Offene Punkte und Abweichungen im Abschlussbericht genannt.
